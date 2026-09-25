# BV-E1 — Vorlagenwahl und Textplatzhalter (Protokoll)

Etappe BV-E1 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitt 13). Auftrag #512, Anwenderauftrag vom 25.09.2026: „Starte BV-E1“. Der gültige Stand steht im Konzept
(Rev. 3) und in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md); hier steht, wie es geworden ist. Vorgänger:
[`BV_E0_Grundlagen_Protokoll.md`](BV_E0_Grundlagen_Protokoll.md). Zweig `konzept-berichtvorlagen`, umgesetzt am
25./26.09.2026; Fable 5.1 hat orchestriert, sieben Agenten (Opus 5.5) haben in eigenen Worktrees in drei Wellen
gearbeitet — 28 Commits, 89 Dateien, +27.952/−620 Zeilen (Ressourcen und Designer eingerechnet). Kein Schemaschritt,
kein Rechenweg berührt, keine Basis neu eingefroren.

| Welle | Agenten | Merge auf dem Zweig | Zwischenstand |
|---|---|---|---|
| 1 | A1 Fundament im Kern; B2a Standardvorlage und Lieferwege | `2a173b3e` (B2a), `95bc84a5` (A1) | gepusht mit `764ca21d` |
| 2 | A2 Word-Engine; A3 Prüfer und Vorlagen-Controller | `692012fa` (A2), `330c0694` (A3) | gepusht mit `966a04e5`; Kern-Lauf ubuntu 36128668523 grün auf `b0eb1783`, der Welle 2 enthält |
| 3 | B1b Oberfläche; B1a-Kern Verdrahtung im Kern; B1a-Hülle Hülle und Windows-Schale | `84e61597` (B1b), `a896e425` (B1a-Kern); B1a-Hülle hat diesen Stand übernommen (`4150e64a`) und bis `f6bde5ad` weitergeführt; End-Merge mit `origin/ios_migration_september`: `26f8e6ea` | mit den Papieren dieses Auftrags |

---

## 1 Entscheid BV-E1-1

**Entscheid des Auftraggebers (Orchestrierung) vom 25.09.2026, vom Anwender am 26.09.2026 bestätigt:** kein Übernahmeschritt und kein `[InstallDelete]`.
`Berichtsvorlage.docx` bleibt als **Stilvorlage** ausgeliefert — sie ist der Rückfall des Codes, wenn die Standardvorlage
fehlt, und die Quelle der Bereinigung: Aus ihr erzeugt `Werkzeuge/Berichtsvorlage` Beispiel- und Standardvorlage. Die
Standardvorlage der Etappe ist eine eigene Datei: **`Berichtsvorlage_Standard.docx`**.

Das Konzept (Rev. 2, 6.3 und 10.3) hatte vorgesehen, dass die Standardvorlage `Berichtsvorlage.docx` ablöst, dass der erste
Start eine vom Anwender geänderte Altdatei an ihrer Prüfsumme erkennt und als eigene Vorlage anbietet und dass das Setup
die Altdatei per `[InstallDelete]` entfernt. **Grund der Abkehr:** Eine Datei unter `{app}` hat der Anwender nie gepflegt —
sie ist schreibgeschützt und wird mit jedem Update erneuert —, und ein Vergleich beim ersten Start nach dem Setup sähe
nichts mehr, weil das Setup die Datei bis dahin schon überschrieben hat. Der Übernahmeschritt hätte also nie etwas
gefunden, und das Löschen hätte dem Code seinen Rückfall genommen.

**Folgen:** Beide Dateien stehen in beiden Lieferwegen (`WindowsFormsApplication1.csproj` mit `None Update`,
`EPOS.iOS/EPOS.iOS.csproj` mit `MauiAsset`); der Kommentar zu `[Files]` in `Setup/EPOS-Plan.iss` nennt beide; die
Dateinamen führt der `BerichtsvorlagenCtrl` als `DATEI_STANDARD` und `DATEI_RUECKFALL`. Das Konzept trägt den Entscheid
mit Rev. 3 in 6.3, 8.4, 10.3, 13 und Anhang B.3.

## 2 Welle 1 — Fundament (A1) und Standardvorlage (B2a)

### 2.1 Fundament im Kern (A1)

Ort `EPOS.Kern/Allgemein/Bericht/Vorlagen/`:

- **`Platzhaltersyntax`:** Normierung der Schlüssel (klein, Umlaute gefaltet, geschützter und unsichtbarer Leerraum, NFD),
  `Finde`, `Lies`, `EnthaeltPlatzhalter`, `OffeneKlammern`; die elf Formatangaben aus Konzept 4.8 typisiert samt Bereichen
  und Arten; die Blockmarken `je` und `wenn` werden erkannt, ausgeführt werden sie erst mit BV-E4.
- **Vorlagenfeldkatalog v1** (`Vorlagenfeld`, `Vorlagenfeldkatalog`, `KATALOGFASSUNG = 1`) mit 159 Schlüsseln und einem
  Alias:

  | Bereich | Schlüssel |
  |---|---|
  | `bericht.*` | 9, dazu der Alias `bericht.programmversion` → `ersteller.version` (BV-Q8) |
  | `text.*` | 7 |
  | `ersteller.*` | 3 (`firma`, `programm`, `version`) |
  | `projekt.*` | 8 |
  | `stamm.kennzahl.*` | 44 |
  | `kennzahl.<k>.beschriftung`, `kennzahl.<k>.einheit` | 88 |

  Handgepflegt sind 27 Einträge, je Kennzahl entstehen drei. Die Liste ist in
  `EPOS.Kern.Tests/Messlatten/Vorlagenfeldkatalog_v1.txt` eingefroren; eine neue Kennzahl meldet die Liste, die Zahl 44
  pinnt die Wache nicht zusätzlich.
- **`Berichtswerte.Aus(daten, konfig, englisch, ersteller)`:** der Wertesatz ohne Datenbank, `Platzhalterwert` mit
  Leergrund, Leerwerte nach Konzept 4.10, Formatangaben nach 4.8. Alle 159 Schlüssel lösen sich für die Probe 1030 in beiden
  Sprachen auf.
- **Ressourcen** +45 je Sprache (`VF_*`, `VF_MUSTER_*`, `BV_*`), Designer neu erzeugt; `BerichtTexte` mit `KulturFuer(bool)`
  und `T(string, bool)`; die Konstante `UNTERTITEL` des Deckblatts ist die gemeinsame Quelle mit `bericht.untertitel`.
- **`IPfade.Berichtsvorlagen`** (Konzept 8.4): in `StandardPfade` `{app}\Vorlagen` (`AppContext.BaseDirectory`,
  `WindowsPfade` erbt es), in `IosPfade` `<App-Bundle>/Vorlagen` über `NSBundle.MainBundle.BundlePath` — **Änderung an
  `EPOS.iOS/`, ungebaut**. `WordBerichtGenerator.FindeVorlage` sucht nur noch über `Dienste.Pfade`; die zweite Fundstelle
  (`Allgemein/Bericht/Vorlagen` relativ zum Ausgabeordner) ist entfernt, kein Lieferweg legt die Vorlage dorthin.
- **Tests (Fälle):** `PlatzhaltersyntaxTests` 107, `VorlagenfeldkatalogWacheTests` 19, `BerichtswerteTests` 24; `DiensteTests`
  um `StandardPfade.Berichtsvorlagen` und `FindeVorlage` ergänzt.

### 2.2 Standardvorlage und Lieferwege (B2a)

- **`WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Standard.docx`**, erzeugt mit
  `Werkzeuge/Berichtsvorlage beispiel --sammelanker` aus der bereinigten `Berichtsvorlage.docx`: Rumpf nur der Absatz
  `{{bericht.inhalt}}`, Kopfzeile `{{ersteller.programm}}`, Fußzeile `{{ersteller.firma}}`, `{{bericht.datum}}` (statt
  DATE) und `{{text.seite}}` mit PAGE / NUMPAGES — fünf Platzhalter, jeder in eigenem Run mit `w:noProof`. Validator Office
  2007–2021 0 Fehler; zwei Läufe des Werkzeugs schreiben byte-gleich (Zeitstempel und `core.xml` aus der Quelle).
- **Lieferwege:** in `WindowsFormsApplication1.csproj` eine zweite `None Update`-Zeile (Link, `TargetPath Vorlagen\…`), in
  `EPOS.iOS/EPOS.iOS.csproj` ein zweites `MauiAsset` mit `LogicalName Vorlagen\…` (**iOS-Änderung, ungebaut**);
  `Berichtsvorlage_Beispiel.docx` steht in keinem Lieferweg. Die Windows-Schale ist gebaut (Debug x64, 0 Fehler), ihr
  Ausgabeordner `Vorlagen\` führt Stil- und Standardvorlage, keine Beispielvorlage.
- **iOS-Dateifilter** `.dotx` → `org.openxmlformats.wordprocessingml.template` in `EPOS.iOS/Dienste/Dateifilter.cs`
  (**iOS-Änderung, ungebaut**), damit der Dokumentenwähler Word-Vorlagen nicht auf `public.data` zurückfallen lässt.
- **Wachen (Fälle):** `AuslieferungsvorlagenWacheTests` 9 — jede Datei im Vorlagenordner ist ausgeliefert oder bewusst
  nicht, beide Lieferwege führen Stil- und Standardvorlage (beide csproj als XML gelesen), die Beispielvorlage steht in
  keinem; `BerichtsvorlageDateiWacheTests` 10 → 18 — Stilregeln und Validator für alle drei Vorlagen, die Standardvorlage
  trägt im Rumpf genau `{{bericht.inhalt}}` (ein Abschnitt mit Kopf- und Fußzeile, keine Kommentare), `noProof`,
  PAGE/NUMPAGES ohne DATE und kein „INEKON GmbH“ in Standard- und Beispielvorlage.
- `Werkzeuge/Berichtsvorlage/LIESMICH.md` nennt drei Dateien und den Aufruf für die Standardvorlage — nach jeder Änderung an
  `Berichtsvorlage.docx` neu erzeugen; `Setup/EPOS-Plan.iss`: Kommentar zu `[Files]`.

## 3 Welle 2 — Engine (A2), Prüfer und Controller (A3)

### 3.1 Word-Engine (A2)

- **`WordVorlagenfueller.Fuelle(vorlage, daten, konfig, ersteller, ziel)` → `Fuellergebnis`** (ersetzt, leer geblieben,
  unbekannt, entfernt — die Grundlage der Laufmeldung), über alle Teile: Rumpf, Kopf- und Fußzeilen aller Abschnitte, Fuß-
  und Endnoten, Textfelder, beide Zweige von `mc:AlternateContent`.
- **Run-Normalisierer** je Behälter (`WordVorlagennormalisierer`): fügt einen zerlegten Platzhalter zusammen, nie über
  Tabulator, Umbruch, Feldzeichen oder Bild hinweg; Absatz- und Zeichenformat der Stelle bleiben, mehrzeiliger Text wird
  `w:br` im selben Run.
- **Einfügeanker** für `bericht.inhalt`: Die Bausteine schreiben an den Anker, ihre Bildteile an den Teil des Ankers; ein
  Kapitel ohne Inhalt nimmt den „EPOS Kapitelkopf“ davor mit.
- **Inhaltssteuerelemente:** Ein SDT mit Schlüssel im Tag wird gefüllt und ausgepackt (BV-Q14 a); ein SDT des Anwenders, in
  dem ein getippter Platzhalter gefüllt wurde, verliert `w:showingPlcHdr`, `w:temporary` und `w:dataBinding` (Konzept 6.6);
  eine Liste wird im Block-SDT zu Absätzen, im Satz-SDT ist sie ein Fehler.
- **Unbekannte Platzhalter bleiben gelb markiert stehen** (4.10). Kommentare werden entfernt; `w:updateFields` nur, wenn
  Felder stehen, die Word nicht selbst aktualisiert (TOC, PAGEREF, REF, SEQ, DOCPROPERTY, auch zerlegt), DATE mit Hinweis;
  alle `docPr/@id` werden neu vergeben — das Doppel aus BV-E0 (Kopfzeile und erstes Bild mit 1) ist behoben; externe
  Beziehungen werden entfernt; eine Vorlage ohne Platzhalter bekommt `{{bericht.inhalt}}` an ihr Ende (kein „Rumpf leeren“,
  Konzept 15.3).
- **`WordKontext` mit Rollenauflösung** (`WordVorlagenstile`, Konzept 6.2): Titel, Untertitel, Überschrift 1–3,
  Standardabsatz (über `w:default`), Hinweis, Beschriftung und Abstand werden über `w:name` gefunden; was fehlt, legt die
  Engine als EPOS-Stil an. Die **Inhaltsbreite** kommt aus der Abschnittsangabe des Ankers statt aus `INHALT_B` (18 Stellen
  der Bausteine), **`Abstand()`** ersetzt `Beschriftung(" ")` (10 Stellen).
- **`WordBerichtGenerator.ErzeugeMitVorlage`** führt zur Engine; der alte Weg `Erzeuge` bleibt für die Stilvorlage.
- **Messlatte:** Die Standardvorlage über die Engine trifft den Rumpf der Messlatten 1030 und Gruppe zeilengleich, die
  Kopfzeile gleich; die Fußzeile trägt `{{bericht.datum}}` statt des DATE-Felds (Konzept 11 Nr. 1). **Nebenwirkung auf dem
  alten Weg:** Die Inhaltsbreite aus der Abschnittsangabe der Stilvorlage ist 9.356 statt 9.355 DXA.
- **Tests (Fälle):** `WordVorlagenfuellerTests` 27 — Messlatte, Validator 2007–2021, Beispielvorlage mit gelben `kapitel.*`,
  zerlegte Runs, fettes Wort, Textfelder, AlternateContent, Kopfzeilen zweier Abschnitte, Fußnote, Inhaltssteuerelemente,
  deutsche Stil-IDs, ohne Platzhalter, Kommentare, verknüpftes Bild, Leerwerte, werfende Quelle, Kapitel im Satz,
  zweispaltiger Satz, Platzhalter im Hyperlink; voller Kern-Lauf im Worktree 7.080 grün.
- **Grenzen:** Ein über Tabulator oder Umbruch zerrissener Platzhalter bleibt still stehen — der Prüfer meldet die offene
  Klammer als Warnung; ein Ankerabsatz, der die Abschnittsangabe trägt, bleibt als leerer Absatz stehen.

### 3.2 Prüfer und Vorlagen-Controller (A3)

- **`Vorlagenteile`:** ein Durchlauf durch dieselben Teile wie die Engine, mit zusammengesetztem Absatztext
  (Erkennungstext) und menschlichem Fundort („Tabelle 2, Zeile 3, Zelle 1 beginnt mit …“), mit SDT, Bildern und Feldern.
  Warum Durchlauf und Normalisierer zwei Wege bleiben und wo sie sich unterscheiden, steht im Kopf von `Vorlagenteile.cs`.
- **`Vorlagenpruefer.Pruefe(vorlage, Schnell | Voll, Pruefkontext)` → `Pruefbefund`:** Meldungen als Fehler, Warnung oder
  Hinweis, je mit Kennung (= Ressourcenschlüssel `VF_PRUEF_*`), Fundort, „Was tun“ und Vorschlag; dazu Katalogfassung,
  Sprache, `HatKapitel`, `HatWirtschaftlichkeit` und Prüfsumme. Regeln: unbekannter Schlüssel mit Vorschlag (auch bei
  verlängertem Schlüssel, `projekt.kundename` → `projekt.kunde`); passt nicht an diese Stelle; Werte je Variante oder
  Gebäude (`stand.*`, `gebaeude.*`) erst später, mit dem nahen `stamm`-Wert als Vorschlag; Blöcke noch nicht unterstützt,
  offen, Ebene, Tabellengrenze, Musterzeile; Formatangaben; Datei unlesbar oder zu groß (20 MB, 100 MB unkomprimiert,
  Konzept 8.5), ein Zip ohne Word-Hauptteil ist „kein Word-Dokument“; `.docm`, Makros, nachverfolgte Änderungen (Voll);
  offene Klammer (Warnung „Platzhalter nicht erkannt“); ohne Platzhalter; Sprache abweichend; Überschriftenstile (Voll);
  Normalform; DATE/TIME; Kommentare; Katalogfassung; externe Beziehungen. Der Nachtrag gleicht die Erkennung an die Engine
  an: keine Verbindung über Tabulator, Umbruch, Feldzeichen, Bild oder Behältergrenzen; ein Tag zählt nur mit `{{…}}` oder
  mit einem Schlüssel samt Punkt; Steuerelemente um Tabellenzeilen und -zellen passen nicht.
- **`BerichtsvorlagenCtrl`:** Vorlagenordner (Einstellung `BerichtVorlagenordner`, Vorgabe
  `Dokumente\EPOS-Plan\Berichtsvorlagen`, nur als vollständiger Pfad); Liste mit den Kennungen `standard` und
  `eigen:<datei>` (`*.docx`, `*.dotx`, ohne `~$` und versteckte Dateien); Hinzufügen, Hinzufügen unter anderem Namen,
  Ersetzen, Neue Vorlage, Entfernen — die Datei wandert in den Unterordner `Entfernt`, gelöscht wird nichts; Herkunft und
  Prüfsumme in der Ablagedatei `.berichtsvorlagen.json` des Vorlagenordners; Vorgabe `BerichtVorlageWord`; Abweichung je
  Stammprojekt in der `BerichtsKonfiguration` (`VorlageWordQuelle`, `VorlageWordDatei`); die Kette `VorlageFuer`:
  Abweichung → Vorgabe → Standard → Rückfall; `LiesBytes` liest einmal; `IstInWordGeoeffnet` über die Sperrdatei `~$…`;
  `OriginalGeaendert` vergleicht die Prüfsumme mit dem Herkunftsort; `Ersteller()` mit der Firma aus der Einstellung
  `BerichtFirma`, sonst `LizenzToken.Firma`.
- **Ressourcen** +155 je Sprache (`VF_PRUEF_*`, `BV_VORLAGEN_*`), Designer neu.
- **Tests (Fälle):** `VorlagenprueferTests` 40 (je Regel ein Fall; die Standardvorlage ohne Befund mit fünf Platzhaltern,
  die Beispielvorlage mit acht unbekannten Kapiteln ohne Vorschlag), `BerichtsvorlagenCtrlTests` 20,
  `BerichtsKonfigurationJsonTests` 5 (einer über die Testdatenbank); `Probevorlagen` baut die Vorlagen der Tests per SDK im
  Speicher.

## 4 Welle 3 — Oberfläche (B1b), Verdrahtung im Kern (B1a-Kern), Hülle und Schale (B1a-Hülle)

### 4.1 Oberfläche (B1b, EPOS.UI)

- **Gruppe „Vorlage“** auf der Berichtsseite über den Berichtsbausteinen: Auswahlfeld „Word-Vorlage“ mit Schloss für die
  mitgelieferte und Sperrgrund für eine fehlende Vorlage; „Neue Vorlage…“ über den Namensdialog, „Hinzufügen…“, „Prüfen“,
  „Platzhalter…“; Menü „…“ aus den Handlungen der Hülle, je Handlung mit Kurztext und Rückfrage („Entfernen“ geht erst auf
  Ja hinaus, Vorgabe Nein, im selben Fenster); die Prüfzeile als `Herleitungszeile` mit „anzeigen“; weiche Sperre während
  eines Laufs; Nachladen über `VorlagenNeuLaden`.
- **Erweiterte Startrückfrage** mit den drei Wegen „Mit meiner Vorlage“, „Mit Standardvorlage“, „Abbrechen“
  (`BerichtAuftrag.Vorlagenweg`).
- **Überlagerungen** `PrueflisteDialog` (Meldungen nach Fehler, Warnung, Hinweis, „erklären lassen“) und
  `PlatzhalterkatalogDialog` (Suche; die gewählte Zeile markiert `{{schluessel}}` im nur lesbaren Feld zum Kopieren — über
  `document.querySelector` unter eindeutiger Kennung, weil die JS-Brücke von .NET 10 Pfade nur über Objekte auflöst), je
  mit Daten, Texten, „Schließen“, Kreuz, Esc und InfoKnopf.
- **Einstellungsdialog, Rubrik „Bericht“:** Firma, Vorlagenordner mit Durchsuchen, auf iOS gesperrt mit Grund; der Hinweis
  auf den gemeinsamen Ordner des Büros steht nur, wo der Ordner wählbar ist.
- **Textbündel** `BerichtSeiteVorlagentexte`, `PrueflisteTexte`, `PlatzhalterkatalogTexte`, `EinstellungenBerichtTexte`;
  Hilfeschlüssel `UcBericht.btn_Help_Pruefliste`, `UcBericht.btn_Help_Platzhalterkatalog`,
  `Form_AdminSettings.btn_Help_Bericht`.
- KI-Sichten um Vorlage, Prüfzeile, Katalogsuche, Firma und Vorlagenordner erweitert, Eingabebilanz
  (`KiMaskenabdeckungWacheTests`) nachgezogen; Stilblatt: Block `epos-vorlage-*` mit `forced-colors` vor dem
  Formularraster-Block, unter 600 px klappt das Menü „…“ unter die Knopfleiste.
- **Tests:** 57 bunit-Fälle (`BerichtSeiteVorlagenTests`, `PrueflisteDialogTests`, `PlatzhalterkatalogDialogTests`,
  `EinstellungenBerichtTests`). **Browserprobe** in Chromium bei 1280 und 375 px (Wegwerf-Wirt außerhalb des
  Repositoriums): Gruppe, Schloss, Menü, Prüfzeile, Prüfliste, Katalog, erweiterte Rückfrage, weiche Sperre und Rubrik
  „Bericht“ wie gedacht.

### 4.2 Verdrahtung im Kern (B1a-Kern)

- **`BerichtCtrl.PruefeVorStart(konfig, englisch, sicht, erzwingtWirtschaftlichkeit)` → `Startbefund`:** Schnellprüfung auf
  den einmal gelesenen Bytes; eine Rückfrage braucht es bei Fehler, abweichender Sprache, unpassender Sicht oder — im
  zweiten Einstieg — fehlender Wirtschaftlichkeit; der Rückfragetext nennt die drei Wege.
- **`BerichtCtrl.ErzeugeWord(daten, konfig, start, weg)` → `Berichtslauf`** füllt genau die geprüften Bytes (geprüft =
  gefüllt) über `ErzeugeMitVorlage` mit `Ersteller()`. Der Berichtslauf nennt Vorlage und Grund, Rückfälle, gelbe und
  zusammengefasst leere Platzhalter, entfernte Kommentare, Warnungen und die Prüfsumme; `Laufmeldung` und `Laufabschnitte`
  bilden daraus die Meldung der Hülle, je Abschnitt mit Kennung. Die alte Methode bleibt und ruft `ErzeugeWordLauf`. **Der
  alte Weg** (Stilvorlage, sonst eingebaute Formate) läuft nur noch ohne Standardvorlage, benannt; eine nicht füllbare
  eigene Vorlage fällt benannt auf die Standardvorlage. `FindeVorlage` nimmt `DATEI_RUECKFALL` aus dem Controller.
- **Prüfer und Engine teilen** die Stilsuche (`WordVorlagenstile.Finde` — was die Engine anlegen müsste, meldet die
  Prüfzeile als fehlend) und die Kommentarzählung (`Vorlagenteile.Kommentarzahl`); `Fuellergebnis.Stellen` zählt je
  Schlüssel jede Auflösung (Grundlage von „leer bei n von m Stellen“).
- **KI-Wissen:** `KiMeldungskennung.Berichtsvorlagen` mit 47 Kennungen (36 Regeln `VF_PRUEF_*`, 3 `BV_VORLAGEN_*`,
  6 `BV_LAUF_*`, 2 `BV_START_*`), `HilfeWissenBerichtsvorlagen` je Meldung mit Bedeutung, Ursache, Abhilfe und Wiki-Stelle,
  `KI_FRAGE_*` in beiden Sprachen; der Bereich „Bericht“ fällt aus der Ausnahmeliste der Wissensdeckung. Ressourcen +73 je
  Sprache (`BV_LAUF_*`, `BV_START_*`, `KI_FRAGE_*`, Erklärung des KI-Felds `vorlage`).
- **Tests (Fälle):** `BerichtCtrlVorlagenTests` 12, `BerichtKiKennungenTests` 3 (hält die Kennungen gegen den Quelltext von
  Prüfer, Controller und `BerichtCtrl`); Kern-Suite im Worktree 7.203 grün.

### 4.3 Hülle und Windows-Schale (B1a-Hülle)

- **`BerichtsvorlagenGaben`** in `BerichtSeiteGaben`: stabile Ids je Sitzung; die Wahl wird Abweichung des Stammprojekts
  (die Vorgabe entfernt sie, eine unbekannte Id wird benannt abgelehnt); gesperrter Eintrag für eine fehlende Vorlage; Menü
  „…“ je Plattform und Quelle, mit benannten Fehlschlägen; die Prüfzeile aus der Schnellprüfung (eine volle Prüfung gilt bei
  gleicher Prüfsumme weiter), die Prüfliste aus der vollen Prüfung, der Platzhalterkatalog mit `Seit` ≤ Katalogfassung und
  lesbarer Art und Kontext; beim Hinzufügen unter einem vergebenen Namen ein freier Name „Name (2)“; Namensprüfung der neuen
  Vorlage; der Startbefund der Vorprüfung bleibt bis zum Lauf stehen, die Laufmeldung erscheint in der Seite. Die
  mitgelieferte Vorlage öffnet die Hülle nie zum Bearbeiten — „In Word öffnen“ lehnt sie dort benannt ab.
- **Lauf:** Vorprüfung vor dem Sammeln mit dem gehaltenen Startbefund, `ErzeugeWord` mit dem Weg der Rückfrage; die
  Abweichung reist mit der gespeicherten Konfiguration. **Zweiter Einstieg** `ErzeugeFuerVergleich`
  (Wirtschaftlichkeitsseite) mit erzwungener Wirtschaftlichkeit und ohne Rückfrage: Führt die Vorlage Platzhalter, aber
  keinen der Wirtschaftlichkeit, entsteht dieser eine Bericht mit der Standardvorlage, und die Laufmeldung nennt es; eine
  Vorlage ganz ohne Platzhalter bekommt den Bericht an ihr Ende und bleibt.
- **Naht `Berichtsvorlagenwege`** (`EPOS.UI.Daten`, statisch über `Berichtsvorlagenwege.Plattform`): `ImOrdnerZeigen`,
  `InWordOeffnen`, `SchreibgeschuetztOeffnen`, `OrdnerWaehlbar`. Windows-Adapter `WindowsBerichtsvorlagenwege`: Explorer
  mit `/select`, Word über App Paths mit dem Pfad als Argument (auch `.dotx`), schreibgeschützt als Kopie unter
  `%TEMP%\EPOS-Plan\Vorlagen (nur lesen)`, `false`, wenn Word fehlt; eingehängt in `Program.Main`. iOS bleibt ohne Adapter —
  dort „Teilen…“ über `Dienste.Datei`.
- **`EinstellungenBerichtGaben`** in der Windows-Hülle der Einstellungen: Firma, Vorlagenordner, Meldung bei einem nicht
  erreichbaren Ordner, Sperrgrund ohne Ordnerwahl; im Kern `BerichtsvorlagenCtrl.IstGueltigerName` und
  `HatVerboteneZeichen`.
- **KI-Felder:** Berichtsseite `vorlage` (5 → 6 Felder; Wahl über die stabilen Ids der Hülle, derselbe Weg wie das
  Auswahlfeld), Programmeinstellungen `bericht_firma` und `bericht_vorlagenordner` (6 → 8 benannte Werte).
- **Ressourcen** +108 je Sprache (danach 11.512 Zeichenketten je Sprache), Designer neu erzeugt, ein zweiter Lauf ändert
  nichts.
- **Hilfe:** Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki` (neu, mit Kopfkommentar) mit den
  Ankern `vorlage`, `neue-vorlage`, `erstellen`, `schreibweise`, `pruefliste`, `platzhalterkatalog`, `standardvorlage`,
  `einstellungen` — Tabumuster ohne Treffer, keine Produktdaten; `help_mapping.txt` drei Zeilen
  (`UcBericht.btn_Help_Pruefliste` → `Berichtsvorlagen#pruefliste`, `UcBericht.btn_Help_Platzhalterkatalog` →
  `#platzhalterkatalog`, `Form_AdminSettings.btn_Help_Bericht` → `#einstellungen`); in
  `Programm Dokumentation - Wirtschaftlichkeit.wiki` ein Satz im Abschnitt „Bericht“ mit dem Anker `bericht-vorlage`.
- **Tests (Fälle):** `BerichtsvorlagenHuelleTests` 13 (Kopie der Testdatenbank, Pfade und Einstellungen hereingereicht),
  `BerichtsvorlagenTextbuendelWacheTests` 5 (jeder Schlüssel der vier Bündel in beiden `.resx`, Rückfall = deutscher Text,
  gleiche Platzhalter, mit Gegenprobe); `KiDialogkatalogTests`, Eingabebilanz und ein bunit-Zeuge für das Feld an der
  Maskenbrücke nachgezogen.

## 5 Abweichungen vom Konzept und Entscheidungen der Agenten

**Abweichungen vom Konzept (Rev. 2).** Das Konzept Rev. 3 trägt sie nach.

| Konzept | Umgesetzt | Grund |
|---|---|---|
| 6.3, 10.3, 13: die Standardvorlage löst `Berichtsvorlage.docx` ab; Übernahmeschritt; `[InstallDelete]` | eigene Datei `Berichtsvorlage_Standard.docx`, die Stilvorlage bleibt; kein Übernahmeschritt, kein `[InstallDelete]` | Entscheid BV-E1-1 (Abschnitt 1) |
| 6.3: die Beispielvorlage wird in BV-E1 zur Standardvorlage | die Standardvorlage entsteht aus derselben Quelle mit `--sammelanker`; `Berichtsvorlage_Beispiel.docx` bleibt Anschauung und steht in keinem Lieferweg (Wache) | die Beispielvorlage trägt `kapitel.*`, die erst BV-E2 füllt; ab BV-E2 wird sie zur Standardvorlage (Werkzeug-LIESMICH) |
| 8.4: `FindeVorlage` bleibt | bleibt, sucht aber nur noch über `Dienste.Pfade.Berichtsvorlagen` | die zweite Fundstelle belegte kein Lieferweg |
| 10.2: im zweiten Einstieg bietet die Rückfrage die Standardvorlage an | keine Rückfrage: dieser Bericht entsteht mit der Standardvorlage, die Laufmeldung nennt es | die Wirtschaftlichkeitsseite hat keine erweiterte Rückfrage |
| 10.2: Bündel `BerichtSeiteTexte` | vier Bündel `BerichtSeiteVorlagentexte`, `PrueflisteTexte`, `PlatzhalterkatalogTexte`, `EinstellungenBerichtTexte` mit Bündelwache | je Komponente ein `*Texte`-Bündel (Hausregel `EPOS.UI/CLAUDE.md`) |
| 10.3: gleicher Name → „Ersetzen“ oder „Unter neuem Namen“ | „Hinzufügen…“ vergibt einen freien Namen „Name (2)“ und nennt ihn; ersetzt wird über „Ersetzen…“ im Menü „…“ | — |
| 10.3: Abweichung als `VorlageWord` (Quelle, Dateiname) | `VorlageWordQuelle` und `VorlageWordDatei` in der `BerichtsKonfiguration` | — |
| 12: schmutzige Vorlagen als Dateien unter `EPOS.Kern.Tests/Proben/Berichtsvorlagen/` | im Speicher per OpenXML SDK gebaut (`Probevorlagen`, Helfer der Engine-Tests) | — |
| 13, Abnahme: Anwenderprobe mit Logo | das Logo der Kopfzeile bleibt, wie es ist | offen, vorgesehen für BV-E2 |

**Entscheidungen der Agenten**, wo das Konzept die Einzelheit offenließ:

- **A1:** `Seit` der erzeugten Katalogeinträge fest 1; die `text.*`-Einträge doppeln vier Deckblattbeschriftungen aus
  `BerichtTexte` (offen); die Katalogwache pinnt die Kennzahlzahl nicht, die eingefrorene Liste meldet jede neue Kennzahl.
- **A2:** alter Weg `Erzeuge` bleibt neben `ErzeugeMitVorlage`; `w:updateFields` nur bei Feldern, die Word nicht selbst
  aktualisiert; die zwei Grenzen aus 3.1 bleiben bewusst still, der Prüfer warnt.
- **A3:** Kennung einer Prüfmeldung = ihr Ressourcenschlüssel; Ablagedatei `.berichtsvorlagen.json`; „Entfernen“ legt nach
  `Entfernt`; der Vorlagenordner gilt nur als vollständiger Pfad.
- **B1b:** Rückfrage je Handlung im selben Fenster; Markieren im Katalog über `document.querySelector`; weiche Sperre der
  Gruppe während eines Laufs.
- **B1a-Kern:** Der alte Weg läuft nur ohne Standardvorlage; eine nicht füllbare eigene Vorlage fällt benannt auf die
  Standardvorlage; `KiDialoge.BerichtVorlagenfeld` und `BerichtVorlagenwahl` vorbereitet.
- **B1a-Hülle:** KI-Feld `vorlage` über die Zahlen-Id der Hülle — `KiDialoge.BerichtVorlagenwahl` bleibt im Produkt
  ungenutzt; Text `EIN_BERICHT_ORDNER_FEST` statt eines allgemeinen „nicht verfügbar“; die Naht trägt `OrdnerWaehlbar`;
  `IstGueltigerName`/`HatVerboteneZeichen` im Controller; die mitgelieferte Vorlage wird nie zum Bearbeiten geöffnet.

## 6 Abnahme

In den Agenten-Worktrees, je Welle:

- **Welle 1:** A1 — `PlatzhaltersyntaxTests` 107, `VorlagenfeldkatalogWacheTests` 19, `BerichtswerteTests` 24 Fälle, alle
  159 Schlüssel für 1030 in beiden Sprachen aufgelöst; B2a — Validator 0 Fehler, `BerichtsvorlageDateiWacheTests` 18 und
  `AuslieferungsvorlagenWacheTests` 9 Fälle, Windows-Schale Debug x64 0 Fehler.
- **Welle 2:** A2 — `WordVorlagenfuellerTests` 27 Fälle, voller Kern-Lauf 7.080 grün; A3 — `VorlagenprueferTests` 40,
  `BerichtsvorlagenCtrlTests` 20, `BerichtsKonfigurationJsonTests` 5 Fälle. Die gepushte Welle 2 ist im Kern-Lauf ubuntu
  36128668523 auf `b0eb1783` grün.
- **Welle 3:** B1b — 57 bunit-Fälle, Browserprobe 1280/375 px; B1a-Kern — `BerichtCtrlVorlagenTests` 12,
  `BerichtKiKennungenTests` 3 Fälle, Kern-Suite 7.203 grün; B1a-Hülle — `BerichtsvorlagenHuelleTests` 13,
  `BerichtsvorlagenTextbuendelWacheTests` 5 Fälle, KI-Katalog und Eingabebilanz nachgezogen.

Die Etappe berührt keinen Rechenweg; ein Referenzlauf gehört nicht zu ihrer Abnahme (Konzept 13). Die Änderungen an
`EPOS.iOS/` sind ungebaut (Abschnitt 8).

**Gate auf dem Merge 26f8e6ea** (Zweig `konzept-berichtvorlagen` mit `origin/ios_migration_september` b0eb1783): Kern-Filter 0 Fehler; Windows-Schale (Debug x64) 0 Fehler; Ressourcen-Designer wiederholbar; voller Lauf 14.573 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern.Tests 7.244, EPOS.UI.Tests 6.367, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27); ChartProben 174 Bilder, 0 Verstöße; SQL-Prüfer 1.918 Texte, 0 Fundstellen; Dokumentationswachen grün. Zwischenstände: Welle 1 (764ca21d) voller Lauf 14.259 grün; Welle 2 (966a04e5) voller Lauf 14.350 grün bis auf den fremden Zeitmesstest `TwwMessreihenCtrlTests.Hunderttausend_Zeilen_brauchen_unter_fuenf_Sekunden`, der unter Last rot und allein grün läuft; CI Kern ubuntu 36128668523 grün auf b0eb1783 (enthält Welle 2). Kein Referenzlauf nötig (kein Rechenweg berührt); die Änderungen an `EPOS.iOS/` (MauiAsset, `IosPfade.Berichtsvorlagen`, Dateifilter) sind ungebaut, ein iOS-Lauf nur nach Rückfrage; der Setup-Lauf nach Rückfrage.

## 7 Anwenderprobe (Windows)

Mit dem Anwender-Build des Endstands (`EPOS_Plan.exe`), installiertem Word und einem Stammprojekt mit mindestens einer
Variante. Der Vorlagenordner (Vorgabe `Dokumente\EPOS-Plan\Berichtsvorlagen`) darf fehlen; EPOS-Plan legt ihn beim ersten
Schreiben an („Neue Vorlage…“, „Hinzufügen…“).

| Nr. | Schritt | Erwartet |
|---|---|---|
| 1 | „Berichte & Kosten › Bericht“ öffnen | Gruppe „Vorlage“ über den Berichtsbausteinen; Auswahl „Standard (EPOS-Plan)“ mit Schloss; Prüfzeile „geprüft, 5 Platzhalter, keine Befunde“ |
| 2 | „Neue Vorlage…“ mit einem Namen, dann Menü „…“ › „In Word öffnen“; in Word einen eigenen Absatz `{{projekt.kunde}}` vor `{{bericht.inhalt}}` setzen, speichern, in EPOS-Plan „Prüfen“ | die Kopie liegt im Vorlagenordner und ist gewählt; Word startet über App Paths mit der Kopie; solange sie in Word geöffnet ist, meldet die Prüfzeile „In Word geöffnet – ungespeicherte Änderungen fehlen“, und „Ersetzen…“ und „Entfernen“ sind gesperrt; nach dem Speichern zählt die Prüfzeile sechs Platzhalter |
| 3 | „Hinzufügen…“ einer Word-Vorlage (`.dotx`), deren Name im Vorlagenordner schon vorkommt | sie kommt unter „Name (2)“ hinzu, wird vollständig geprüft und gewählt; die Meldung nennt den Namen |
| 4 | in der eigenen Vorlage `{{projekt.kundename}}` schreiben, speichern, „Prüfen“, dann „Erstellen“ | Prüfzeile mit Befund und „anzeigen“; die Prüfliste nennt Fundstelle, „Was tun“ und den Vorschlag `projekt.kunde`; „Erstellen“ bringt die erweiterte Rückfrage mit „Mit meiner Vorlage“ (die Stelle bleibt gelb im Bericht), „Mit Standardvorlage“ (nur dieser Bericht) und „Abbrechen“; die Laufmeldung nennt Vorlage und Grund |
| 5 | „Standard (EPOS-Plan)“ wählen, Menü „…“ › „Schreibgeschützt öffnen“ | Word öffnet eine Kopie unter `%TEMP%\EPOS-Plan\Vorlagen (nur lesen)`; die Standardvorlage bleibt unverändert |
| 6 | „Administration › Einstellungen“, Abschnitt „Bericht“ | „Firma“ ist mit der Firma der Lizenz vorbelegt, „Standardwerte“ setzt sie wieder darauf; „Vorlagenordner“ mit „Durchsuchen“: ein erreichbarer Ordner wird übernommen, ein nicht erreichbarer mit Meldung abgelehnt; der nächste Bericht trägt die Firma in der Fußzeile |
| 7 | im Hilfe-Assistenten auf der Berichtsseite die eigene Vorlage setzen lassen | der Assistent setzt das Feld `vorlage`; Auswahl und Prüfzeile folgen, die Wahl gilt für das Stammprojekt |

Schritt 2 ist zugleich der einzige Nachweis des Word-Starts über App Paths. Das Logo der Kopfzeile gehört nicht zu dieser
Probe; es folgt mit BV-E2.

## 8 Offen

- **(a) Anwenderprobe unter Windows** in sieben Schritten (Abschnitt 7): Gruppe mit „Standard (EPOS-Plan)“, Schloss und
  Prüfzeile „5 Platzhalter“; „Neue Vorlage…“ und „In Word öffnen“, danach „Prüfen“, in Word geöffnet sperrt „Ersetzen…“ und
  „Entfernen“; „Hinzufügen…“ einer `.dotx` mit Namenskonflikt → „Name (2)“; Tippfehler-Platzhalter → Befunde, Prüfliste,
  Rückfrage mit drei Wegen; „Schreibgeschützt öffnen“; Einstellungen › Bericht mit der Firma aus der Lizenz und der
  Ordnerwahl; der Assistent setzt die Vorlage. Der Word-Start über App Paths ist nur beim Anwender prüfbar.
- **(b) Setup-Lauf** nach Rückfrage: Stil- und Standardvorlage in `{app}\Vorlagen`.
- **(c) iOS-Lauf** nach Rückfrage: MauiAsset, `IosPfade.Berichtsvorlagen` und der Dateifilter `.dotx` sind ungebaut.
- **(d) Wiki-Upload** der neuen Seite „Berichtsvorlagen“ (Neuanlage) und der Seite „Wirtschaftlichkeit“ im Sammel-Upload;
  bis dahin führen die drei neuen Hilfeknöpfe (Prüfliste, Platzhalterkatalog, Einstellungen › Bericht) ins Leere. Der
  Hilfeknopf der Berichtsseite (`UcBericht.btn_Help`) führt weiter auf die Seite „Bericht“, die keine Repo-Quelle hat und
  die Gruppe „Vorlage“ noch nicht nennt.
- **(e) Tippprobe** mit echtem Word (aus BV-E0).
- **(f) „Original geändert – übernehmen?“** ist nicht verdrahtet; `BerichtsvorlagenCtrl.OriginalGeaendert` liegt im Kern.
- **(g) Vorgabe der Installation** (`BerichtVorlageWord`) ohne Bedienung: `SetzeVorgabeWord` liegt im Controller, die Wahl
  der Berichtsseite ist die Abweichung des Stammprojekts.
- **(h) KI-Feld der Katalogsuche** nicht angemeldet; die KI-Sicht der Berichtsseite führt die Suche bereits
  (`BerichtSeiteKiSicht.Katalogsuche`).
- **(i) Vorprüfung im Kern:** Sie stuft eine Vorlage ohne Platzhalter als „ohne Wirtschaftlichkeit“ ein; die Hülle nimmt den
  Fall aus — im Kern angleichen.
- **(j) Startrückfrage:** Sie prüft mit der zuletzt gespeicherten Versionsauswahl; weicht die Zahl der Versionen im Lauf
  ab, prüft der Lauf neu und nennt Befunde in der Laufmeldung.
- **(k) Logo der Kopfzeile** als Bildplatzhalter oder Entfall mit BV-E2 (BV-Q8). Das `docPr`-Doppel aus BV-E0 ist behoben.

**Weitere Befunde für spätere Etappen:** `Seit` der erzeugten Katalogeinträge fest 1 und die Doppelung von vier
Deckblattbeschriftungen zwischen `text.*` und `BerichtTexte`; die Grenzen der Engine aus 3.1; aus BV-E0 weiter
offen der Nachweis mit echtem Word 365 und Excel und die Geräteprobe iPad (iU13).

## 9 Dateien

28 Commits der Agenten (Welle 1 `caca674b` … `f0256d20`, Welle 2 `c52e14de` … `52de26b4`, Welle 3 `749b9f20` …
`f6bde5ad`), belegt mit `git show --name-only` je Commit ohne die fremden Merges:

| Bereich | Dateien |
|---|---|
| Kern, Vorlagen (neu) | `EPOS.Kern/Allgemein/Bericht/Vorlagen/`: `Platzhaltersyntax.cs`, `Platzhalterwert.cs`, `Vorlagenfeld.cs`, `Vorlagenfeldkatalog.cs`, `Berichtswerte.cs` (A1); `WordVorlagenfueller.cs`, `WordVorlagennormalisierer.cs`, `WordVorlagenstile.cs`, `WordVorlagenbereinigung.cs`, `WordVorlagenergebnis.cs`, `WordVorlagentexte.cs` (A2); `Vorlagenteile.cs`, `Vorlagenpruefer.cs`, `VorlagenprueferTypen.cs` (A3); `Berichtslauf.cs` (B1a-Kern) |
| Kern, Bericht | `EPOS.Kern/Allgemein/Bericht/WordBerichtGenerator.cs` (A1, A2, B1a-Kern), `BerichtTexte.cs` (A1, A2), `BerichtsKonfiguration.cs` (A3), `Bausteine/BausteineStandard.cs` (A1), `Bausteine/BausteineProjekt.cs`, `Bausteine/BausteineVergleich.cs`, `Bausteine/BausteineWirtschaftlichkeit.cs` (A2) |
| Kern, Controller und Dienste | `EPOS.Kern/Controller/BerichtsvorlagenCtrl.cs` (neu, A3; B1a-Hülle), `EPOS.Kern/Controller/BerichtCtrl.cs` (B1a-Kern), `EPOS.Kern/Allgemein/Dienste/IPfade.cs`, `EPOS.Kern/Allgemein/Dienste/StandardPfade.cs` (A1) |
| Kern, KI | `EPOS.Kern/Allgemein/KI/KiMeldungskennung.cs`, `HilfeWissen.cs`, `HilfeWissenBerichtsvorlagen.cs` (neu), `Dialoge/KiDialoge.cs`, `Dialoge/KiDialogTexte.cs` (B1a-Kern, B1a-Hülle) |
| Ressourcen | `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` — je Sprache +45 (A1), +155 (A3), +73 (B1a-Kern), +108 (B1a-Hülle) |
| Tests Kern | neu: `EPOS.Kern.Tests/PlatzhaltersyntaxTests.cs`, `VorlagenfeldkatalogWacheTests.cs`, `BerichtswerteTests.cs`, `Messlatten/Vorlagenfeldkatalog_v1.txt` (A1), `AuslieferungsvorlagenWacheTests.cs` (B2a), `WordVorlagenfuellerTests.cs` (A2), `VorlagenprueferTests.cs`, `BerichtsvorlagenCtrlTests.cs`, `BerichtsKonfigurationJsonTests.cs`, `Probevorlagen.cs` (A3), `BerichtCtrlVorlagenTests.cs`, `BerichtKiKennungenTests.cs` (B1a-Kern), `BerichtsvorlagenHuelleTests.cs` (B1a-Hülle); geändert: `DiensteTests.cs` (A1), `BerichtsvorlageDateiWacheTests.cs` (B2a), `KiDialogaufrufTests.cs`, `KiWissensdeckungTests.cs` (B1a-Kern) |
| Oberfläche | `EPOS.UI/Seiten/Berichte/BerichtSeite.razor`, `BerichtDaten.cs`, `BerichtSeiteKiSicht.cs`, `BerichtSeiteVorlagentexte.cs` (neu); neu `EPOS.UI/Dialoge/Berichte/PrueflisteDialog.razor`, `PrueflisteDaten.cs`, `PrueflisteTexte.cs`, `PlatzhalterkatalogDialog.razor`, `PlatzhalterkatalogDaten.cs`, `PlatzhalterkatalogTexte.cs`; `EPOS.UI/Dialoge/Admin/EinstellungenDialog.razor`, `EinstellungenKiSicht.cs`, `EinstellungenBerichtTexte.cs` (neu); `EPOS.UI/wwwroot/epos-ui.css` |
| Tests Oberfläche | neu: `EPOS.UI.Tests/Seiten/BerichtSeiteVorlagenTests.cs`, `Dialoge/PrueflisteDialogTests.cs`, `Dialoge/PlatzhalterkatalogDialogTests.cs`, `Dialoge/EinstellungenBerichtTests.cs`, `Dialoge/BerichtsvorlagenTextbuendelWacheTests.cs`; geändert: `Dialoge/Hilfe/KiMaskenabdeckungWacheTests.cs`, `Dialoge/Hilfe/KiDialogkatalogTests.cs` |
| Hülle | `EPOS.UI.Daten/Bericht/BerichtSeiteGaben.cs`; neu `BerichtsvorlagenGaben.cs`, `Berichtsvorlagenwege.cs`, `EinstellungenBerichtGaben.cs` |
| Windows-Schale | `WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Standard.docx` (neu), `WindowsFormsApplication1.csproj` (Lieferweg), `Dienste/WindowsBerichtsvorlagenwege.cs` (neu), `Program.cs`, `Views/Admin/EinstellungenHuelle.cs`, `Allgemein/Hilfe/help_mapping.txt` |
| iOS (ungebaut) | `EPOS.iOS/EPOS.iOS.csproj` (MauiAsset), `EPOS.iOS/Dienste/IosPfade.cs`, `EPOS.iOS/Dienste/Dateifilter.cs` |
| Setup, Werkzeug | `Setup/EPOS-Plan.iss` (Kommentar zu `[Files]`), `Werkzeuge/Berichtsvorlage/LIESMICH.md` |
| Wiki | `Projekte/Wiki/Programm Dokumentation - Berichtsvorlagen.wiki` (neu), `Projekte/Wiki/Programm Dokumentation - Wirtschaftlichkeit.wiki` |
| Papiere (dieser Auftrag) | dieses Protokoll, Konzept Rev. 3, `Dokumentation/LIESMICH.md`, `Dokumentation/aktuell/Status_iOS_Migration.md`, `Dokumentation/aktuell/Konzept_Hilfesystem_Wikidokumentation.md`, `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` |

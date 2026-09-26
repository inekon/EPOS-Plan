# Konstruktor: Bezug des gespeicherten Tags (26.09.2026)

Protokoll des Postens **#547**. Auftrag: der offene Rest aus ZU25 (Nachtrag N21, Folge (a)) —
„Beim erneuten Öffnen eines gespeicherten Konstruktortags kommen Bezugsart und Bezugsmenge nicht
mit (‚ohne Bezug')." Umsetzungskonzept Zapfprofilgenerator, Nachtrag N30.

**Rahmen.** Worktree `zkon`, Zweig `zkon` von `69cf3ced0` (= `origin/ios_migration_september`,
Schemastand 148), Opus 5.5. Kein Schemaschritt, Testdatenbank unberührt, kein Push.

---

## 1 Ursache

Der Konstruktor bekam seinen Bezug allein aus dem **Entwurf**
(`ZapfprofilAuslegungDialog`: `Bezugsart="@_eingabe.Entwurf?.Bezugsart"`). Ein gespeicherter
Konstruktortag hat keinen Entwurf mehr: Der Schreibweg legt ihn als Katalogzeile
(`Tab_TwwBedarfstag_STAMM`, Status `EIGEN`) samt `Bezugsmenge` und `Bezugsart` (Schritt 124) an, und
die Projektzeile zeigt mit `ID_Bedarfstag` auf ihn. `ZapfprofilCtrl.Lies` las die Konstruktorzeilen
(Schritt 145), aber nicht den Bezug der Katalogzeile, und die Hülle gab ihn nicht an die Eingaben. Der
wieder geöffnete Konstruktor begann deshalb „ohne Bezug"; ein erneutes OK baute einen **unskalierten**
Tag, und die Auslegung rechnete danach anders als vor dem Schließen. Die Auslegung **ohne** erneutes
OK war nicht betroffen — sie liest den Katalogtag samt Bezug über `ID_Bedarfstag`.

## 2 Behebung

| Schicht | Datei | Änderung |
|---|---|---|
| Kern | `EPOS.Kern/Allgemein/Zapfprofil/ZapfprofilStand.cs` | `ZapfprofilStand.KonstruktorBezug` und der Satz `KonstruktorBezugStand(TagGefunden, Bezugsmenge, Bezugsart)` mit `Vollstaendig` |
| Kern | `EPOS.Kern/Controller/ZapfprofilCtrl.cs` | `KonstruktorBezug(projekt, lese)`: bei Quelle Konstruktor mit `ID_Bedarfstag` Bezugsmenge und — wenn die Spalte steht — Bezugsart der Katalogzeile; fehlt die Zeile, `TagGefunden = false`. `Lies` trägt ihn |
| Kern | `EPOS.Kern/Controller/ZapfprofilCtrl.Speichern.cs` | Der zurückgegebene Stand trägt den Bezug: beim Entwurf dessen, sonst den der Katalogzeile (im Vorgang gelesen) |
| Hülle | `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Auslegung.cs` | `KonstruktorBezugSetzen`: an die Eingaben nur bei Quelle Konstruktor ohne Entwurf; unvollständiger Bezug oder fehlende Zeile setzen einen benannten Hinweis |
| DTO | `EPOS.UI/Dialoge/Bedarf/ZapfprofilDaten.cs` | `ZapfprofilAuslegungEingabeDaten.KonstruktorBezugsart`, `KonstruktorBezugsmenge`, `KonstruktorBezugHinweis` (samt `Kopie`) |
| Dialog | `EPOS.UI/Dialoge/Bedarf/ZapfprofilAuslegungDialog.razor` | Bezug des Konstruktors: Entwurf, sonst der gespeicherte (nur Quelle Konstruktor); ein OK leert den gespeicherten Bezug und den Hinweis (der Entwurf trägt ihn) |
| Dialog | `EPOS.UI/Dialoge/Bedarf/BedarfstagKonstruktor.razor` | Parameter `BezugHinweis`, gezeigt als Statuszeile `.epos-zapfausl-konstruktorbezug` unter dem Bezug |
| Texte | `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | `ZPG_AUS_KON_BEZUG_OHNE`, `ZPG_AUS_KON_BEZUG_TAG_FEHLT` in beiden Sprachen; Designer neu erzeugt, zweiter Lauf +0 |

**Alter Datensatz.** Ein Konstruktortag vor Schritt 124 (ohne Bezugsart) oder einer, der bewusst
„ohne Bezug" gebaut wurde, ist an der Katalogzeile nicht zu unterscheiden. Beide öffnen ohne Bezug,
aber nicht still: Der Hinweis sagt, dass der Tag keinen vollständigen Bezug trägt und ein OK ihn
unskaliert baut. Eine Bezugsmenge ohne Bezugsart (Stand vor 124) wird nicht halb übernommen — sie
gehören zusammen (N10 (j)).

## 3 Schemaschritt fachlich nötig?

**Nein.** Bezugsart und Bezugsmenge sind Eigenschaften des **Tags**, nicht des Auslegungssatzes:
Die Auslegung skaliert den Tag auf die Bezugsmenge einer Gruppe derselben Bezugsart, und der Tag
steht mit beiden an seiner Katalogzeile (Schritt 124). Eine Spalte an `Tab_TwwProjekt` oder
`Tab_TwwKonstruktorzeile` hielte dieselbe Angabe ein zweites Mal und könnte gegen die Katalogzeile
auseinanderlaufen. Die Ableitung aus den Zonen des Projekts (Ausweichweg des Auftrags) ist
entbehrlich, weil die Angabe gespeichert vorliegt.

## 4 Projektkopie und Transfer

Kein eigener Weg nötig: Die Projektzeile zeigt nach der Kopie auf dieselbe Katalogzeile
(`ProjektDuplizierenCtrl`, `ID_Bedarfstag` als Katalogverweis), und im `.wpx`-Paket reist der
Katalogkopf mit seiner Bezugsart (Schritt 124, Inhaltsvergleich). Der neue Kern-Fall hält beides:
Nach Kopie und Rundreise liest `Lies` am Ziel denselben `KonstruktorBezug`.

## 5 Tests

- `EPOS.Kern.Tests/ZapfprofilKonstruktorzeilenTests`:
  `Der_Bezug_des_gespeicherten_Tags_kommt_beim_Laden_mit_und_die_Auslegung_bleibt_gleich` —
  Konstruktortag mit Bezug (Personen, 4) bauen und rechnen, speichern, laden: Stand und Hülle tragen
  den Bezug; die Auslegung des geladenen Stands und die eines erneut gebauten Tags aus geladenen
  Zeilen und geladenem Bezug sind exakt gleich der vor dem Schließen (Verfahren, Volumen, Leistung,
  Nenninhalt); Gegenprobe ohne Bezug rechnet ein anderes Volumen; Kopie und Paket tragen den Bezug.
  `Ein_gespeicherter_Tag_ohne_Bezug_beginnt_ohne_Bezug_mit_benanntem_Hinweis` — Katalogzeile ohne
  Bezug, fehlende Katalogzeile (zwei verschiedene Hinweise), andere Quelle ohne Hinweis.
- `EPOS.UI.Tests/Dialoge/ZapfprofilAuslegungDialogTests.Vergleich` (bunit):
  `Ein_gespeicherter_Konstruktortag_oeffnet_mit_seinem_Bezug` — Auswahl „Wohneinheiten" und Menge 4
  stehen, das Mengenfeld ist bedienbar, OK baut mit demselben Bezug;
  `Ein_gespeicherter_Tag_ohne_Bezug_zeigt_den_Hinweis` — Statuszeile mit `role="status"`, nach OK weg.

## 6 Gates

Nach dem Merge von `origin` `ef20793f7` (Merge `648548505`; Code-Commit `2aca628ec`):

- Kern-Filter `-c Release`: 0 Fehler.
- Gefilterte Tests (`Konstruktor|Zapfprofil|Tww|Auslegung|KiMasken|DokumentationLinkWache|WikiProduktdatenWache|RepositoryOrdnungWache`)
  vor dem Merge: EPOS.Kern.Tests 669/669, EPOS.UI.Tests 338/338.
- Voller Lauf 0 Fehler: KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 + 1 übersprungen,
  EPOS.UI.Tests 6 666, EPOS.Kern.Tests 8 258 + 1 übersprungen.
- Windows-Schale mit `-p:EnableWindowsTargeting=true`: 0 Fehler.
- `ResourceDesigner`: zwei neue Schlüssel, zweiter Lauf ohne Diff.
- `SqlDialektPruefer`: 1 996 Texte, 0 Fundstellen.
- Referenzlauf entbehrlich: `Bedarfstag` und der Rechenweg der Bilanz sind unberührt; geändert ist
  allein, womit der Konstruktor öffnet. Kein neuer SQL-Text außer einem `SELECT *` nach `ID`
  (Muster der Nachbarn).

## 7 Offen

- Sichtabnahme unter Windows: Konstruktortag mit Bezug bauen, Auslegung speichern, Dialog schließen,
  wieder öffnen, Konstruktor öffnen — Bezugsart und Menge stehen.
- Kein Logbuch-Satz (Kleinigkeit, Konzept Hilfesystem 13.4), keine Wiki-Änderung (die Bedienung
  bleibt).

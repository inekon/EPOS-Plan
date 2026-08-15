# Lokalisierung — Prüfrezeptur (Paket 9, L8)

Diese Datei ersetzt die im Konzept 13.6 angedachte „Build-Prüfung gegen neue Hardcodings".

**Warum kein Analyzer.** Ein Roslyn-Analyzer müsste je Zeichenkette entscheiden, ob sie
Anzeige, Schlüssel, SQL-Fragment, Spaltenname oder Diagnoseausgabe ist. Diese Entscheidung
ist im Bestand nicht mechanisch ableitbar (siehe die Ausnahmelisten in
[`Paket9_Lokalisierung_Protokoll.md`](Paket9_Lokalisierung_Protokoll.md), Abschnitte 2, 5.3,
9.5, 12.6 und 20.5) — ein Analyzer produzierte entweder Dutzende Fehlalarme pro Build oder
eine so lange Unterdrückungsliste, dass er nichts mehr findet. Statt dessen: **sechs
wiederholbare Prüfungen**, die eine Sitzung vor dem Abschluss eines Pakets einmal laufen
lässt und deren Ergebnis sie gegen den hier festgehaltenen Ist-Stand hält.

Maßgeblich ist die **Drei-Schichten-Regel** (Konzept 13.6, Kurzfassung in
[`../../CLAUDE.md`](../../CLAUDE.md)):
Persistenz → deutsch und eingefroren (`Allgemein/DbWerte.cs`) · Schlüssel → sprachneutral,
ASCII · Anzeige → `MyResource.Resource.*`.

Der **lokalisierte Bereich** meint hier `Views/Simulation/`, `Views/Pufferspeicher/`,
`Allgemein/Simulation/` und `Allgemein/GrafikTools/ChartManager.cs` — ohne `*.Designer.cs`,
ohne die vom Build ausgeschlossenen Dateien (`* - Kopie*`, `Form_Simulation_Kurz.*`).

> **`ChartManager.cs` kam mit der Review-Nacharbeit dazu** (Befund N2). Die Datei liegt zwar in
> `Allgemein/GrafikTools/`, liefert aber die Achsentitel und Mouseover-Texte **aller** Diagramme
> des Simulationsbereichs. Sie hat den lokalisierten Achsentitel der Aufrufer überschrieben —
> ein Fehler, der nur auffällt, wenn die Datei mitgeprüft wird. `ChartManagerNeu.cs` bleibt
> außen vor: vom Build ausgeschlossen (`.csproj`).

---

## P1 — Neue hartkodierte Anzeigetexte

Sucht Zeichenkettenliterale mit deutschen Sonderzeichen. Trifft nicht alles (ein rein
asciischer deutscher Text wie `"Speichern"` entgeht ihr), findet aber zuverlässig den
typischen Fall.

```powershell
rg -n --encoding utf8 -g '*.cs' -g '!*.Designer.cs' -g '!*designer.cs' -g '!* - Kopie*' -g '!Form_Simulation_Kurz*' `
   '"[^"]*[äöüÄÖÜß„“][^"]*"' `
   WindowsFormsApplication1/Views/Simulation WindowsFormsApplication1/Views/Pufferspeicher `
   WindowsFormsApplication1/Allgemein/Simulation `
   WindowsFormsApplication1/Allgemein/GrafikTools/ChartManager.cs
```

> **`-g '*.cs'` ist Pflicht** (mit der Nacharbeit ergänzt). Ohne die Einschränkung durchsucht
> `rg` auch die `.md`-Protokolle und die Formular-`.resx` desselben Ordners und meldet zusätzlich
> rund 110 Treffer, die keine Hardcodings sind. Der in dieser Datei geführte Ist-Stand zählt
> ausschließlich `.cs`.

**Jeder Treffer ist zu klassifizieren.** Zulässig sind nur:

| Klasse | Erkennungsmerkmal |
|---|---|
| Diagnose | Zeile enthält `Console.WriteLine` / `Debug.WriteLine` (Konzept 13.4, in L2 ausdrücklich ausgenommen) |
| Kommentar | Literal steht rechts von `//` |
| SQL / Spaltenname | Literal steht in einem SQL-Fragment oder in `rs.Read("…")` / `r["…"]` |
| Steuerelementname | `"tabPage_…"`, Serienschlüssel, Filter-Token (Schicht 2) |
| Persistenzwert | steht in `DbWerte.cs` oder ist als Bestandstoleranz dokumentiert (`SPEICHERTYP_ALTWERTE_EN`) |

Alles andere ist ein **Befund** und gehört in den Katalog.

## P2 — Anzeigetext als Steuerwert (Muster B0-9 / B0-10 / B0-11)

Der teuerste Fehlertyp: eine lokalisierte Zeichenkette entscheidet über den Rechenweg.

```powershell
rg -n -g '!*.Designer.cs' -g '!* - Kopie*' -g '!Form_Simulation_Kurz*' `
   '\.Text\s*(==|!=)|SelectedItem\.ToString\(\)|\.Text\.Trim\(\)\s*(==|!=)|\.SelectedItem\s*(==|!=)' `
   WindowsFormsApplication1/Views/Simulation WindowsFormsApplication1/Views/Pufferspeicher

rg -n "=\\\\?'" WindowsFormsApplication1/Views/Simulation WindowsFormsApplication1/Views/Pufferspeicher `
   WindowsFormsApplication1/Allgemein/Simulation
```

Zulässig: Vergleiche gegen `""`, und Rückgaben, deren Listeninhalt selbst aus der Datenbank
stammt (`r["Bezeichner"]`). **Nicht** zulässig: Vergleich gegen ein deutsches Literal,
gegen einen Ressourcenwert oder gegen einen Anzeigenamen. Ein SQL-Prädikat darf einen
Erzeugerwert nur über `DbWerte.*` einsetzen.

## P3 — Persistenzwerte als Literal

Jeder Wert aus `DbWerte.cs` darf außerhalb dieser Datei nicht mehr als Literal vorkommen —
außer in den dokumentierten Ausnahmen (Protokoll Abschnitt 2, „Bewusst NICHT ersetzt").

```bash
grep -oE '^\s*public const string [A-Za-z0-9_]+ = "[^"]*"' WindowsFormsApplication1/Allgemein/DbWerte.cs \
  | sed 's/.*= //' | sort -u > /tmp/dbw.txt
while IFS= read -r lit; do
  grep -rn --include='*.cs' -F "$lit" WindowsFormsApplication1/Views/Simulation \
       WindowsFormsApplication1/Views/Pufferspeicher WindowsFormsApplication1/Allgemein/Simulation \
    | grep -v '\.Designer\.cs' | grep -v ' - Kopie' | grep -v 'Form_Simulation_Kurz'
done < /tmp/dbw.txt
```

> **Zur Zählung:** `DbWerte.cs` führt **51** `public const string`; das `sort -u` oben arbeitet auf
> den **Werten** und liefert deshalb **49** Suchbegriffe — zwei Konstantenpaare tragen denselben
> Wortlaut. Beide Zahlen sind richtig, sie zählen nur Verschiedenes.

## P4 — Katalog-Gleichstand

Drei Mengen müssen deckungsgleich sein: Schlüssel in `Resource.resx`, Schlüssel in
`Resource.en-US.resx`, Eigenschaften in `Resource.Designer.cs`. Der Generator läuft **nur
in Visual Studio** — wer einen Schlüssel von Hand ergänzt, muss die Eigenschaft mitpflegen.

```bash
cd WindowsFormsApplication1/MyResource
schl() { grep -oE '<data name="[^"]+"' "$1" | sed 's/.*name="//;s/"//' | sort; }
diff <(schl Resource.resx) <(schl Resource.en-US.resx)
diff <(schl Resource.resx) \
     <(grep -oE '^\s+public static string [A-Za-z0-9_]+ \{' Resource.Designer.cs \
       | sed -E 's/.*public static string ([A-Za-z0-9_]+) \{/\1/' | sort)
```

Vier Scheintreffer sind normal: `Name1`, `Color1`, `Bitmap1`, `Icon1` stehen im
XML-Kommentarkopf jeder `.resx` (Beispielblock des ResX-Schemas) und sind keine Einträge.

## P5 — Kodierung und BOM

`.editorconfig` schreibt für `*.cs`, `*.resx` und die Projektdateien **UTF-8 mit BOM** vor.
Ohne Signatur lesen Werkzeuge die Datei als ANSI und zerstören die Umlaute.

```powershell
Get-ChildItem WindowsFormsApplication1\Views\Simulation, `
              WindowsFormsApplication1\Views\Pufferspeicher, `
              WindowsFormsApplication1\Allgemein\Simulation, `
              WindowsFormsApplication1\MyResource -Include *.cs,*.resx -File -Recurse |
  ForEach-Object {
    $b = [System.IO.File]::ReadAllBytes($_.FullName)
    $bom = ($b.Length -ge 3 -and $b[0] -eq 0xEF -and $b[1] -eq 0xBB -and $b[2] -eq 0xBF)
    $ascii = -not ($b | Where-Object { $_ -gt 0x7F })
    if (-not $bom) { "{0,-14} {1}" -f $(if ($ascii) {"ASCII"} else {"OHNE-BOM"}), $_.FullName }
  }
```

Zusätzlich auf Mojibake prüfen: Die Zeichenfolgen `Ã¤ Ã¶ Ã¼ ÃŸ â€ž` und das Ersatzzeichen
`U+FFFD` dürfen in keiner Quelldatei vorkommen.

## P6 — Die einzige harte Laufzeitprobe: Sprachgleichheit

P1 bis P5 sind statisch und damit fehlbar. Der belastbare Nachweis, dass kein
Anzeigetext als Steuerwert dient, ist der **Referenzlauf in beiden Sprachen**: die
Ergebnisdateien müssen byte-identisch sein.

```powershell
$exe = "…\Referenzlauf\bin\x86\Debug\net8.0-windows\Referenzlauf.exe"
$ids = "1007,1008,1010,1011,1017,1018,1021,1023,1024"

& $exe lauf --ziel <Arbeitsordner>\Lauf_DE --projekte $ids
$env:EPOS_REFLAUF_UICULTURE = "en-US"
& $exe lauf --ziel <Arbeitsordner>\Lauf_EN --projekte $ids
Remove-Item Env:\EPOS_REFLAUF_UICULTURE

# Byte-/MD5-Vergleich DE gegen EN — Erwartung: 208 von 208 gleich
& $exe vergleich <Arbeitsordner>\Lauf_DE <Arbeitsordner>\Lauf_EN
```

Der Toleranzvergleich der Suite reicht dafür **nicht** — er würde eine kleine
Zahlenabweichung durchgehen lassen. Ergänzend MD5 je Datei vergleichen (die Suite meldet
Gleichheit nur wertweise). Und: `EPOS_REFLAUF_UICULTURE` setzt ausschließlich
`CurrentUICulture`; die Zahlformatierung bleibt deutsch — deshalb steht in der englischen
Konsolenausgabe weiterhin `-5,0 °C`.

> **Nicht anfassen:** die Konsolenpräfixe `Simulation Hinweis:` / `Simulation Warnung:` /
> `Simulation FEHLER:`. Sie entstehen in der privaten Sammelmethode von
> `SimulationProtokoll` (`Console.WriteLine("Simulation " + art + ": " + zeile)`, Zeile 210) —
> die öffentlichen Einstiege heißen `Hinweis`, `HinweisEinmal`, `Warnung`, `WarnungEinmal`
> und `Fehlermeldung`. `Referenzlauf/Protokoll.cs:67-68` zählt Warnungen und Fehler über
> genau diese Token; eine Übersetzung setzte die Auswertung der Lauf-Protokolle
> stillschweigend auf null.

---

## Ist-Stand 15.08.2026 — nach der Review-Nacharbeit (unkommittiert auf `97183a2`)

Alle sechs Prüfungen erneut gelaufen, nachdem die Befunde N1–N8 behoben waren.

| Prüfung | Ergebnis |
|---|---|
| **P1** | **86 Treffer** in `.cs` (Bereich jetzt inkl. `ChartManager.cs`): 17 `Console`/`Debug`-Diagnose, 32 Kommentar hinter Code, **37 verbleibend**. Davon in den **Views 8**: 3 Steuerelementnamen (`Form_Simulation_Detail.cs:556,587,667`), 2 SQL-Spaltennamen (`Form_KonfigPufferspeicher.cs:91,93`, `[Rücklauf]`), 2 Kommentartexte hinter Code, die die Filterzeile nicht erwischt (`Form_Simulation_Config.cs:629`, `…Uebersicht.cs:96`), 1 Fortsetzungszeile einer `Console.WriteLine`-Verkettung (`WaermesenkeClass.cs:467`). **Kein benutzersichtbarer Text in den Views, keiner mehr in `ErdreichAuswertung.cs`, `VDI4640Pruefung.cs` und im Anzeigeteil von `ErdreichTemperatur.cs`** (dort vorher 41 Literale, jetzt 0). Es bleiben **28 Engine-Fundstellen** (`SimulationControl.cs` 15, `SimulationBHKW.cs` 6, `SimulationWaermebedarf.cs` 3, `SimulationWaermepumpe.cs`/`SimulationSPK.cs`/`SimulationKanaele.cs`/`WaermequelleClass.cs` je 1) — bei Einzelsicht fast durchweg **Fortsetzungszeilen mehrzeiliger `Console.WriteLine`-Verkettungen**, dazu ein `ArgumentException`-Text, zwei In-Memory-Etiketten und drei Vergleiche gegen den DB-Wert `Einheit`. Nach der L2-Regel sind sie **außerhalb** des Katalogs; der wirkliche Rückstand ist, dass 50 `Console.WriteLine` der Engine am Protokollkanal aus Paket 8 vorbeilaufen (Protokoll, Abschnitt 25.11 a). Dazu **1 neu benannter Rest**: `ErdreichTemperatur.cs:75` `MONATSKUERZEL` (Monatsnamen, im Katalog ausgenommen). |
| **P2** | **6 Treffer, unverändert unbedenklich**: 3 × `.Text == ""` (Leerprüfung), `Form_QuellePufferspeicher.cs:306` (`SelectedItem` einer aus `Bezeichner` gefüllten Liste), `Form_Simulation_Config.cs:586` (Anzeige nach Anzeige), 1 Kommentar. **Kein SQL-Literal** mit deutschem Erzeugerwert. Zusätzlich beseitigt: der `#if DEBUG`-Selbsttest in `VDI4640Pruefung.cs` verglich den Hinweistext gegen ein **deutsches Literal** und wäre auf englischer Oberfläche fehlgeschlagen — er prüft jetzt gegen den Katalogeintrag. |
| **P3** | 51 Konstanten / 49 verschiedene Werte in `DbWerte.cs`. Außerhalb nur die dokumentierten Ausnahmen: 4 × Anzeige-Argument von `SenkeAufHeizkreisZurueck`, 2 × In-Memory-Etikett `sp.Erzeuger`, `rs.Read("Heizung")`, die `#if DEBUG`-Selbsttests von `VDI4640Pruefung`/`ErdreichTemperatur` und Kommentartexte. **Kein neuer Befund** — insbesondere hat die Umstellung des Bodentyp-Katalogs die 13 Persistenzschlüssel (`DbWerte.BODENTYP_*`) unberührt gelassen. |
| **P4** | `Resource.resx` 543 · `Resource.en-US.resx` 543 · `Resource.Designer.cs` 543 · Laufzeit-Eigenschaften 543 — vier Mengen deckungsgleich, 0 Abweichungen. (541 vorher + 4 neu − 2 tot = 543.) |
| **P5** | 83 Dateien geprüft (Bereich plus `MyResource/` und `WindowsFormsApplication1.csproj`): **UTF-8 mit BOM bis auf zwei rein asciische Satelliten** (`Form_PufferSp_Bearbeiten.en-US.resx`, `Form_PufferSp_einlesen.en-US.resx`). Die fünf im Review benannten Dateien tragen jetzt die BOM (inhaltlich byte-gleich, nur drei Bytes vorangestellt). **Kein Mojibake, kein `U+FFFD`.** |
| **P6** | Referenzlauf DE gegen `Referenzlaeufe/2026-08-15_B2`: **9 von 9 PASS**, 2.295.987 Werte, **208 von 208 byte-/MD5-gleich**. Derselbe Lauf mit `EPOS_REFLAUF_UICULTURE=en-US`: **208 von 208 byte-identisch zum deutschen Lauf**, ebenfalls 9 von 9 PASS gegen die Basis. Gerechnet aus einem eigenen git-Arbeitsbaum auf `97183a2` plus ausschließlich den 17 Dateien dieser Nacharbeit. |

---

## Ist-Stand 15.08.2026, Commit `97183a2` (vor der Nacharbeit)

Alle sechs Prüfungen einmal gelaufen (Review zu Paket 9, Etappe 2b).

> **Die P1-Rohsumme (51) und die Rohsumme des Laufs oben (86) sind nicht vergleichbar.** Die
> Klassifikation „Kommentar" wurde damals von Hand vorgenommen (2 Treffer), oben mechanisch
> über die Filterzeile (32). Wie die 51 im Einzelnen zustande kamen, lässt sich aus dem
> Protokolltext nicht rekonstruieren. Aussagekräftig ist allein die **Aufteilung des Rests** —
> und die stimmt in beiden Läufen in dem Punkt überein, auf den es ankommt: kein
> benutzersichtbarer deutscher Text in den Views, der Block liegt in `Allgemein/Simulation/`.
> Für künftige Läufe gilt die Rezeptur oben (mit `-g '*.cs'`) als die verbindliche.

| Prüfung | Ergebnis |
|---|---|
| **P1** | 51 Treffer im lokalisierten Bereich: 15 `Console`/`Debug`-Diagnose, 2 Kommentar hinter Code, **34 verbleibend**. Davon **6 in den Views** — 2 SQL-Spaltennamen (`Form_KonfigPufferspeicher.cs:91,93`, `[Rücklauf]`) und 4 Steuerelementnamen (`Form_Simulation_Detail.cs:556,587,667,895`, `"tabPage_Wärmepumpe…"`). **Kein benutzersichtbarer Text in den Views.** Die übrigen **28 liegen in `Allgemein/Simulation/`** (Engine-Protokoll- und Konsolentexte ohne Katalogschlüssel) und sind der offene Punkt aus Protokoll-Abschnitt 21, Punkt 1. |
| **P2** | 6 Treffer, alle unbedenklich: 3 × `.Text == ""` (Leerprüfung), `Form_QuellePufferspeicher.cs:306` (`SelectedItem` einer aus `Bezeichner` gefüllten Liste — DB-Wert, kein Anzeigetext), `Form_Simulation_Config.cs:586` (Anzeige nach Anzeige), 1 Kommentar. **Kein SQL-Literal** mit deutschem Erzeugerwert mehr. |
| **P3** | 51 Konstanten in `DbWerte.cs`. Außerhalb nur die dokumentierten Ausnahmen: 4 × Anzeige-Argument von `SenkeAufHeizkreisZurueck`, 2 × In-Memory-Etikett `sp.Erzeuger`, `rs.Read("Heizung")` (Spaltenname), die `#if DEBUG`-Selbsttests von `VDI4640Pruefung`/`ErdreichTemperatur` und `SPEICHERTYP_ALTWERTE_EN`. **Kein neuer Befund.** |
| **P4** | `Resource.resx` 541 · `Resource.en-US.resx` 541 · `Resource.Designer.cs` 541 — Mengen deckungsgleich, 0 Abweichungen. |
| **P5** | Alle `.cs` des Bereichs UTF-8 mit BOM, kein Mojibake, kein `U+FFFD`. **Vier `.resx` ohne BOM** — `Views/Simulation/Form_Simulation_Config.en-US.resx`, `…/Form_KonfigPufferspeicher.en-US.resx`, `…/Form_Simulation_Detail.en-US.resx`, `Views/Pufferspeicher/Form_PufferSp.en-US.resx` — dazu drei reine ASCII-Satelliten ohne BOM und `WindowsFormsApplication1.csproj`. Fachlich unkritisch (die XML-Deklaration trägt `encoding="utf-8"`), aber ein Verstoß gegen die eigene `.editorconfig`: Visual Studio zieht diese Dateien beim nächsten Speichern unprotokolliert nach. Nachzuholen. |
| **P6** | Referenzlauf DE gegen `Referenzlaeufe/2026-08-15_B2`: 9 von 9 PASS, 2.295.987 Werte, **208 von 208 Dateien byte-/MD5-gleich**. Derselbe Lauf mit `EPOS_REFLAUF_UICULTURE=en-US`: **208 von 208 byte-identisch zum deutschen Lauf**, ebenfalls 9 von 9 PASS. Konsolenpräfixe unverändert deutsch bei übersetztem Meldungsinhalt. |

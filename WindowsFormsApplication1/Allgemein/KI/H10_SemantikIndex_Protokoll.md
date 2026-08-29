# H10 — Lokaler Embedding-Index für die semantische Doku-Suche (Umsetzungsprotokoll, 30.08.2026)

Fragen und Wiki-Inhalte werden zusätzlich zur Stichwortsuche (H9) über **lokale Text-Einbettungen**
einander zugeordnet. Vollständig offline nach einem einmaligen Modell-Download, wirksam auch in der
Doku-Suche **ohne KI**. Ausgangsstand: `4bf676d` (H9).

Neue Dateien `Allgemein\KI\SemantikModell.cs` und `Allgemein\KI\SemantikIndex.cs`; geändert
`Allgemein\KI\WikiWissen.cs`, `Views\Help\Form_KiChat.cs`, `Views\Help\Form_Lizenz.cs`,
`WindowsFormsApplication1.csproj`, beide `MyResource\Resource*.resx` (+ der von Visual Studio selbst
regenerierte Designer). Harnisch `..\dev\h10probe\` (gitignored). Kein Git-Schreibkommando.

---

## 1. Kodierungsbehandlung je Datei

Vor jeder Änderung strikt als UTF-8 gelesen und auf `U+FFFD` geprüft; **keine** berührte Datei ist
CP1252, das CP1252-Rezept kam nicht zum Einsatz. BOM-Zustand je Datei unverändert.

| Datei | vorher | nachher |
|---|---|---|
| `Allgemein\KI\SemantikModell.cs` | *(neu)* | UTF-8 ohne BOM, CRLF |
| `Allgemein\KI\SemantikIndex.cs` | *(neu)* | UTF-8 ohne BOM, CRLF |
| `Allgemein\KI\WikiWissen.cs` | UTF-8 ohne BOM, CRLF | unverändert (22 Umlaute) |
| `Views\Help\Form_KiChat.cs` | UTF-8 ohne BOM, CRLF | unverändert (205 Umlaute) |
| `Views\Help\Form_Lizenz.cs` | UTF-8 ohne BOM, CRLF | unverändert (137 Umlaute) |
| `WindowsFormsApplication1.csproj` | UTF-8 **+BOM**, CRLF | unverändert |
| `MyResource\Resource.resx` | UTF-8 +BOM, CRLF, ohne Schluss-Umbruch | unverändert (1734 Umlaute, 2650 `<data>`) |
| `MyResource\Resource.en-US.resx` | UTF-8 +BOM, CRLF, ohne Schluss-Umbruch | unverändert (2650 `<data>`) |

Schlussprobe über alle acht Dateien: strikt UTF-8 lesbar, **kein `U+FFFD`**, **keine** reine
LF-Zeile; beide `.resx` zusätzlich als XML geladen und wohlgeformt.

> **Falle, hier einmal hineingetappt und behoben:** Ein PowerShell-Einzeiler über das *Bash*-Werkzeug
> zerlegt Backtick-Folgen (`` `r`n ``) als Kommandosubstitution — aus `KI_SEMANTIK_AKTIV` wurde
> `KI_SEMAnTIK_AKTIV` und aus dem Zeilenumbruch ein literales `n`. Beide `.resx` wurden
> zurückgeschnitten und über das *PowerShell*-Werkzeug neu ergänzt; der Diff gegen HEAD zeigt jetzt
> ausschließlich die neun beabsichtigten Zeilen je Datei. **Für `.resx`-Einschübe nie das
> Bash-Werkzeug verwenden.**

---

## 2. Laufzeit und Lizenzen

| Paket | Fassung | Lizenz | Woher |
|---|---|---|---|
| `Microsoft.ML.OnnxRuntime` | 1.22.1 | **MIT** (Lizenzdatei im Paket gelesen) | nuget.org |
| `Microsoft.ML.OnnxRuntime.Managed` | 1.22.1 (transitiv) | **MIT** | nuget.org |
| `Microsoft.ML.Tokenizers` | 2.0.0 | **MIT** (`<license type="expression">MIT`) | nuget.org |
| `Google.Protobuf` | 3.30.2 (transitiv über Tokenizers) | **BSD-3-Clause** | nuget.org |

`dotnet list package --include-transitive` nachgezogen: außer `Google.Protobuf` kommt nichts Neues
hinzu, `SixLabors.Fonts` bleibt auf 1.0.1. Keine Lizenz-Überraschung im Sinne der SixLabors-Regel —
BSD-3-Clause ist wie MIT freizügig und verlangt nur den Copyright-Vermerk, der in der
Komponentenliste steht (§ 8).

Nur die **Referenzen** kamen in die `.csproj`; sonst wurde an der Projektdatei nichts geändert.

**Native Ausgabe geprüft** (`..\dev\build_h10\`): `onnxruntime.dll` (12 416 032 Byte) liegt im
Ausgabewurzelverzeichnis **und** unter `runtimes\win-x64\native\`; beide sind byte-identisch
(SHA-256 `7788f3f3…`), die x86-Fassung daneben ist eine andere Datei. Der x64-Fallstrick der
ACE-Historie greift hier also nicht — es landet die 64-Bit-Fassung im Ausgabeordner. Zusätzlich
kopiert das Paket `runtimes\{android,ios,linux-*,osx-*,win-arm64,win-x86}` mit; das ist Verhalten
des NuGet-Pakets, nicht der Projektdatei.

---

## 3. Modellwahl — empirisch entschieden, nicht nach Doku

Verglichen wurden zwei quantisierte ONNX-Modelle, **beide mit demselben XLM-R-SentencePiece-
Tokenizer** (die `sentencepiece.bpe.model` beider Verzeichnisse ist byte-identisch,
SHA-256 `cfc8146a…`) und beide unter 150 MB:

| | `intfloat/multilingual-e5-small` | **`sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2`** |
|---|---|---|
| Lizenz | MIT | **Apache-2.0** |
| int8-Datei | `onnx/model_qint8_avx512_vnni.onnx`, 118 346 824 B | **`onnx/model_quint8_avx2.onnx`, 118 453 870 B** |
| cos(Warmwasserbedarf, Brauchwasserbedarf) | 0,9367 | **0,8459** |
| cos(Warmwasserbedarf, Photovoltaikmodul) | 0,8429 | **0,2738** |
| → Abstand | 0,0937 | **0,5721** |
| cos(Akku, Stromspeicher) | 0,8303 | **0,6545** |
| cos(Akku, Heizkessel) | 0,8224 | **0,2393** |
| → Abstand | **0,0080** | **0,4152** |

**Entschieden hat das zweite Paar.** E5 ist auf *Frage gegen Absatz* trainiert und legt kurze
Fachbegriffe alle dicht beieinander — 0,008 Abstand trägt keinen Schwellwert. Gebraucht wird hier
aber genau die **symmetrische** Ähnlichkeit kurzer Begriffe. Der E5-übliche Präfix `query: ` wurde
mitgemessen und half nicht (0,0080 → 0,0085).

Innerhalb des gewählten Verzeichnisses schlägt `model_quint8_avx2` (u8s8) die Variante
`model_qint8_avx512_vnni` (s8s8) sowohl beim Akku-Paar (0,6545 gegen 0,5955) als auch in der
Portabilität: u8s8 läuft auf jeder AVX2-CPU mit voller Geschwindigkeit, s8s8 braucht dafür
VNNI-Befehle.

**Festgelegt im Code** (`SemantikModell`):

```
Verzeichnis  huggingface.co/sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2
Stand        e8f8c211226b894fcb81acc59f3b34ba3efd5f42          (Commit — die Versionsbindung)
Modell       onnx/model_quint8_avx2.onnx   118 453 870 B
             SHA-256 98a01d88b7de996cdea58c32ca71208c09968d143798814b2ea09d3439dc334f
Tokenizer    sentencepiece.bpe.model         5 069 051 B
             SHA-256 cfc8146abe2a0488e9e2a0c56de7952f7c11ab059eca145a0a727afce0db2865
Ablage       %APPDATA%\wp-plan\semantik\modell\{modell.onnx, tokenizer.model}
```

Beide Prüfsummen sind doppelt belegt: einmal aus dem Header `X-Linked-ETag` der Hugging-Face-
Auslieferung, einmal aus der eigenen Berechnung über die geladene Datei. Ausgang des ONNX ist
`last_hidden_state [·,·,384]`, gepoolt wird als **Mittelwert** über alle Stücke
(`1_Pooling/config.json` des Modells) und anschließend auf Länge 1 normiert — das Skalarprodukt
zweier Vektoren ist damit unmittelbar der Kosinus.

---

## 4. Tokenizer-Weg — die eigentliche Hürde

`Microsoft.ML.Tokenizers` 2.0.0 liest die SentencePiece-**Unigram**-Datei von XLM-R
(`SentencePieceTokenizer.Create(stream, false, false)`, 0,7 s Ladezeit). Das allein genügt aber
nicht:

> **XLM-R nummeriert anders als die SentencePiece-Datei.** Die Datei zählt
> `<unk>=0 <s>=1 </s>=2` und dann die Stücke; fairseq/XLM-R zählt
> `<s>=0 <pad>=1 </s>=2 <unk>=3` und dann die Stücke. Jedes gewöhnliche Stück liegt also um
> **genau eins höher**. `SentencePieceTokenizer` liefert die rohen Nummern.

Nachgeprüft gegen die `tokenizer.json` desselben Standes (250 002 Einträge), Stück für Stück über
einen deutschen Probesatz: **53 von 53 Stücken exakt um +1 versetzt, kein Ausreißer, keine
Abweichung.** Der Versatz steht in `SemantikModell.Kennungen` samt Sonderfall `<unk>`, davor `<s>`,
dahinter `</s>`.

Was der Fehler kostet, wurde gegengemessen: mit rohen Nummern sinkt der Abstand des ersten Belegpaars
von **0,58 auf 0,02** und der des zweiten von **0,42 auf 0,02** — das Modell rechnet dann mit
falschen Stücken und liefert lauter ähnlich aussehenden Unsinn. Die `tokenizer.json` (9 MB) wird zur
Laufzeit **nicht** gebraucht und deshalb auch nicht geholt; sie diente nur diesem Beweis
(`..\dev\h10spike\`).

Höchstlänge: 256 Stücke je Text (`MAX_STUECKE`). Das Modell ist laut
`sentence_bert_config.json` auf 128 Stücke trainiert; die Abschnittslänge ist deshalb bewusst auf
900 Zeichen begrenzt (≈ 250 Stücke) statt auf die im Konzept genannten 1200 — längere Abschnitte
würden abgeschnitten und der Rest ginge verloren.

---

## 5. Index

**Grundmenge.** `WikiWissen.SeitenlisteAsync` fragt `api.php?action=query&list=allpages&apnamespace=0`
mit `apfilterredir=nonredirects` (und arbeitet `apcontinue` ab). Am 30.08.2026 sind das **87 echte
Seiten**; mit Weiterleitungen wären es 124 — die 37 Synonymseiten aus H9 tragen keinen eigenen Text
und blieben deshalb ausdrücklich draußen.

**Seitentexte.** Über das neue `WikiWissen.SeitentextAsync` — derselbe Weg und derselbe Tagescache
wie im Prompt (frischer Cache → online → abgelaufener Cache). **Kein neuer Empfänger, keine zweite
Abrufkette.**

**Zerlegung** (`SemantikIndex.Zerlegen`): an den Überschriften des Klartext-Auszugs (`== … ==`), zu
lange Teile an Absatz- und Satzgrenzen, zu kurze (< 200 Zeichen) an den vorigen angehängt.
Eingebettet wird *Seitentitel + Abschnittsüberschrift + Text*.

**Zusätzlich ein Titelabschnitt je Seite** — der Kurzname der Seite (letztes Glied des Titels) als
eigener, ganz kurzer Eintrag. Das ist eine **Ergänzung gegenüber dem Auftrag**, und sie hat einen
gemessenen Grund:

> Das Modell trennt kurze Texte scharf (0,85 gegen 0,27) und lange Fließtexte nur noch matt — über
> alle 600 Textabschnitte lag *alles* zwischen 0,45 und 0,73, weil jeder längere Absatz zum
> Fachgebiet hin mittelt. Ohne Titelabschnitt gewann bei „wie kann der Warmwasserbedarf angelegt
> werden" die **Prosa über das Thema** (Hydraulikschemata, Beispiele, Wärmepumpe), und die
> Bedienseite `Programm Dokumentation/Brauchwasser` stand auf **Rang 129** (0,4988). Mit
> Titelabschnitt steht `Wärmebedarf erfassen` auf **Rang 1** (0,7390).

**Ablage.** `%APPDATA%\wp-plan\semantik\index\`, eine JSON-Datei je Seite (Titel, Quelle, Abrufzeit,
Modellmarke, Abschnitte mit Text und Base64-Vektor). Gültigkeit 24 h. Die **Modellmarke**
(`Name@Stand`) steht in jeder Datei: wechselt das Modell, sind alle Vektoren wertlos und die Dateien
werden verworfen statt verglichen.

**Suche.** Bruteforce-Kosinus über alle Abschnitte, je Seite der stärkste, absteigend, Schwelle
**0,40**. Gewertet wird der **größere** von zwei Kosinus — gegen die Frage im Wortlaut *und* gegen
ihre H9-Stichwortliste. Auch das ist gemessen und nicht geraten:

| „wie kann der Warmwasserbedarf angelegt werden" | Wortlaut | Stichwortkette „warmwasserbedarf angelegt" |
|---|---|---|
| `Programm Dokumentation/Brauchwasser` | Rang 24 (0,6568) | **Rang 1 (0,7249)** |
| `Wärmebedarf erfassen` | **Rang 1 (0,7390)** | Rang 2 (0,7090) |

Die Füllwörter stören also auch die Einbettung — dieselbe Beobachtung wie in H9, nur an anderer
Stelle. Längere Fragen tragen umgekehrt mehr Zusammenhang; deshalb das Maximum beider Lesarten und
nicht die eine oder die andere. Kosten: eine zweite Einbettung, rund 3 ms.

---

## 6. Verschmelzung in `WikiWissen.SucheAsync`

Drei Zusagen, alle im Harnisch belegt:

1. **Die Stichwortsuche bleibt führend.** Die H9-Kaskade läuft unverändert; erst danach füllt
   `SemantikAnfuegen` die Liste auf `MAX_SEITEN` = 3 auf. Ist die Liste voll, trägt die Semantik
   nichts bei — gemessen an der Beispielfrage: `Adressen: 2, Stufe 2, davon rein semantisch: 0`,
   Trefferliste Zeile für Zeile die von H9.
2. **Die Kontextseite bleibt der erste Abschnitt.** Sie steht in der Liste, bevor die Semantik
   überhaupt gefragt wird.
3. **Ohne Index ändert sich nichts.** `SemantikIndex.Suche` gibt eine leere Liste zurück; jede
   Ausnahme wird gefangen und übersprungen.

`SemantikIndex.Anstossen(basis)` steht am Anfang der Suche und kehrt sofort zurück (gemessen 0–11 ms,
im Fehlerfall 100 Aufrufe in 0 ms). **Die Suche wartet nie auf Modell oder Index.** Beim allerersten
Mal wirkt die Semantik deshalb noch nicht — sie wirkt ab dem nächsten Aufruf.

Neue Diagnose: `WikiWissen.LetzteSemantikTreffer` (wie viele Seiten *nur* über die Semantik
hereinkamen). Neuer Prüf-Zugang: die Überladung
`SucheAsync(basis, frage, kontext, bool stichwortsuche, abbruch)` — sie legt die H9-Kaskade still.
Im Programm wird ausschließlich mit `true` aufgerufen; es gibt dafür weder Schalter noch Einstellwert.

---

## 7. Oberfläche

`Form_KiChat`: kein neues Bedienelement. Die vorhandene Statuszeile `_lblStatus` zeigt über die
schon laufende 400-ms-Sperruhr zwei Zustände — `KI_SEMANTIK_VORBEREITUNG` („Semantische Suche wird
vorbereitet …") und `KI_SEMANTIK_AKTIV` („Semantische Suche aktiv"). Geschrieben wird nur bei
Zustands**wechsel**, sonst flackerte die Zeile viermal je Sekunde. Ein **fehlgeschlagener** Bezug
bleibt stumm: die Hilfe sucht dann wie vorher, nur ohne zweite Stufe — kein Ereignis, über das der
Anwender etwas erfahren müsste. Der Tooltip der Zeile nennt Modell, Lizenz und Herkunft
(`KI_SEMANTIK_HERKUNFT`).

Angestoßen wird beim **Öffnen des Assistenten** (Ende des Fensteraufbaus) und zusätzlich bei jeder
Suche — nicht beim Programmstart.

### Neue Ressourcenschlüssel (beide Sprachen, ans Dateiende angehängt)

| Schlüssel | de | en |
|---|---|---|
| `KI_SEMANTIK_VORBEREITUNG` | Semantische Suche wird vorbereitet ... | Preparing semantic search ... |
| `KI_SEMANTIK_AKTIV` | Semantische Suche aktiv | Semantic search active |
| `KI_SEMANTIK_HERKUNFT` | Die semantische Suche arbeitet ausschließlich auf diesem Rechner. Modell: {0} ({1}), einmalig bezogen von {2}. | Semantic search runs entirely on this computer. Model: {0} ({1}), downloaded once from {2}. |

`Resource.Designer.cs` hat **Visual Studio wie bei H1/H2/H4 selbst regeneriert** (drei Eigenschaften,
alphabetisch eingeordnet); eine Hand-Ergänzung fand deshalb nicht statt (kein CS0102). Wer den Stand
ohne laufendes Visual Studio nachbaut, muss die drei Eigenschaften ggf. selbst ergänzen — der Build
ist der Prüfstein.

---

## 8. Fremdkomponenten-Liste

Die Liste wird **im Code** gepflegt: `Views\Help\Form_Lizenz.cs`, Methode `KomponentenFuellen()`
(Registerkarte „Komponenten", RTF-Ausgabe zur Laufzeit, keine Ressourcendatei, keine Wiki- oder
Serverquelle). Ergänzt wurde ein Abschnitt **„Semantische Suche in der Dokumentation"** mit
ONNX Runtime (MIT), Microsoft.ML.Tokenizers (MIT), Google Protocol Buffers (BSD-3-Clause) und dem
Einbettungsmodell (Name, Apache-2.0, Herkunft, Hinweis, dass es nicht zum Lieferumfang gehört und
dass die Zuordnung ohne jede Datenübertragung stattfindet).

---

## 9. Datenschutz

Der Klassenkopf von `WikiWissen` trägt einen neuen Absatz: die zweite Stufe ändert
**datenschutzlich nichts** — Frage und Wiki-Text werden ausschließlich auf diesem Rechner
eingebettet, es entsteht kein neuer Empfänger und keine neue Übertragung; hinaus geht weiterhin nur
die Stichwortliste an `wiki.epos-plan.de`. `SemantikModell` und `SemantikIndex` tragen dieselbe
Zusage im eigenen Kopf. `KiChatService.cs` wurde **nicht** angefasst (parallele Sitzung, siehe § 12).

Der einzige neue Netzverkehr ist der **einmalige Dateiabruf** der beiden Modelldateien von
`huggingface.co` — eine reine GET-Anfrage auf eine feste, versionsgebundene Adresse, ohne
Nutzerdaten, ohne Anmeldung. Quelle und Stand stehen im Code, im Tooltip und in der
Komponentenliste. Kein neuer Einwilligungstext, `KiEinwilligung.FASSUNG` bleibt **2**
(im H4-Regressionslauf mitgeprüft).

---

## 10. Prüfharnisch `..\dev\h10probe\` — Zahlen des Laufs

**61 Prüfungen, 0 Fehler, Rückgabewert 0** („ALLES GRUEN"). Live gegen `wiki.epos-plan.de` und
`huggingface.co`, ohne Modellanbieter und ohne Produktiv-Datenbank (Abschnitt 8 des Harnischs weist
beides nach). Jeder Lauf beginnt mit **gelöschtem** Modell- und Indexordner, holt also wirklich neu.

| Block | Ergebnis |
|---|---|
| **1 Rückfall** (Adresse unbrauchbar, kein Modell) | Anstoß kehrt nach **7 ms** zurück; Zustand `Nichtverfuegbar`; nichts abgelegt; **100 weitere Anstöße zusammen 0 ms**; **100 × (Einbetten + Semantiksuche) zusammen 1 ms**; Suche liefert die H9-Trefferliste, 0 Semantiktreffer, keine Ausnahme |
| **2 Bezug** (live) | 123 522 921 Byte in **13,4 s** geholt, entpackt, geprüft; beide SHA-256 stimmen; keine Teildatei; Vektorbreite 384 |
| **2b Prüfsumme wirkt** | ein Byte in `tokenizer.model` gekippt → Datei wird **verworfen und gelöscht**, Zustand `Nichtverfuegbar`; mit der echten Datei sofort wieder `Bereit` |
| **3 Semantik-Beleg** | 0,8459 / 0,2738 (Abstand **0,5721**) und 0,6545 / 0,2393 (Abstand **0,4152**); Fachpaare über, Fremdpaare unter der Schwelle 0,40; cos(x,x) = 1 |
| **4 Indexaufbau** | Anstoß **0 ms**; während des Aufbaus **5,2 Mio. weitere Aufrufe in 300 ms** und eine vollständige Suche (140–432 ms, 3 Abschnitte); fertig nach **8,4 s** (warmer Textcache) bzw. **16,0 s** (kalt); **87 Seiten, 686 Abschnitte**; kein Abschnitt > 900 Zeichen |
| **5 Nur Semantik** (Kaskade aus) | **0 gesendete Adressen**, Stufe 0 — und trotzdem **3 Abschnitte**: `Wärmebedarf erfassen`, `Programm Dokumentation/Brauchwasser`, `Grundlagen/Hydraulikschemata`. Alle drei rein semantisch |
| **5b Gegenprobe „Akku"** | Das Wort steht in **keiner** Wiki-Seite. Gefunden werden `Grundlagen/Stromspeicher`, `Programm Dokumentation/Stromspeicher`, `Stromspeicher` |
| **6 Hybrid** | Trefferliste identisch zu H9 (`Wärmebedarf erfassen`, `Grundlagen/Wärmebedarfsrechnung`, `Grundlagen/Pufferspeicher`), 0 rein semantische Treffer, Reihenfolge gehalten; mit Kontext „Bereich: Brauchwasser" steht `Programm Dokumentation/Brauchwasser` vorn. **Semantikstufe je Frage: 6,0 ms** bei 686 Abschnitten |
| **7 Zweiter Start** (Adresse unbrauchbar) | Modell aus `%APPDATA%` in **679 ms** bereit; Index aus den Dateien in **233 ms** für 686 Abschnitte; Semantiksuche liefert weiter |
| **8 Empfänger** | alle Such-Adressen auf der Wiki-Basis, keine trägt die Rohfrage; kein Modellanbieter, keine OleDb-Assembly im Prozess; ONNX Runtime geladen |
| **9 Ressourcen** | alle drei Schlüssel in **de-DE und en-US** gefüllt; Herkunftszeile formatiert sauber mit Modell, Lizenz und Quelle |

### Speicher- und Zeitbedarf (Zusammenfassung)

| | Wert |
|---|---|
| Modell + Tokenizer auf Platte | **123 522 921 Byte** (117 MiB) |
| Index auf Platte | **1 876 330 Byte** (1832 KiB) in 86 Dateien für 87 Seiten / 686 Abschnitte |
| Erstbezug (Download + Init, live) | **13,4 s** |
| Aufwärmen aus dem Bestand (jeder weitere Start) | **0,7 s** |
| Erster Indexaufbau (Seitentexte kalt / warm) | **16,0 s / 8,4 s**, im Hintergrund |
| Index aus den Dateien laden | **0,23 s** |
| Semantikstufe je Frage (2 Einbettungen + 686 Vergleiche) | **6,0 ms** |
| Zusatzaufwand im Fehlerfall (Modell nicht verfügbar) | **0,01 ms je Frage** (100 Durchläufe = 1 ms) |

---

## 11. Regressionsläufe und Build

Beide älteren Harnische wurden gegen den H10-Stand gefahren (Kopien unter
`..\dev\h10probe_regress_h9probe\` und `..\dev\h10probe_regress_h9probe_h4regress\`, nur die
Build-Pfade geändert):

| Harnisch | Prüfungen | Fehler |
|---|---|---|
| H9 (Stoppwörter, Kaskade, Live-Suche) | **59** | **0** |
| H4 (Stichwörter, Zuordnung, Cache, Offline-Rückfall, Prompt, Sprache, Ressourcen) | **76** | **0** |
| H10 | **61** | **0** |

```
MSBuild ..\WP-Plan.sln -p:Configuration=Debug -p:Platform=x64 -p:OutDir=C:\Waermeplan\WP_Plan\dev\build_h10\
```

**0 Fehler, 5 Warnungen** — genau die bekannten fünf (`WErzeugerModel` CS0108,
`KlimaregionStammCtrl` CS0109 ×2, `StromverbraucherStammCtrl` CS0108, `MDIMainForm` CS1998).
Keine neue Warnung.

---

## 12. Nicht angefasst

`Allgemein\KI\Aktionen\`, `..\KiKern\`, `KiSchreibschutz.cs`, `KiChatService.cs`,
`SchemaMigration`, `DbWerte`, beide `CLAUDE.md`, alle Designer- und `.resx`-Dateien außer dem
bekannten MyResource-Rezept. `git status` weist genau die sieben geänderten und zwei neuen Dateien
aus, die oben stehen.

Offen aus H9 und weiterhin liegen gelassen, weil die Datei einer parallelen Sitzung gehört: der
Kommentar in `KiChatService.cs` (Zeile ~159) beschreibt die gesendete Kette noch ohne die H10-Stufe.

---

## 13. Offene Prüfpunkte für die Abnahme (Oberfläche)

Nicht maschinell prüfbar, weil dafür das Fenster laufen muss:

1. **Statuszeile beim allerersten Öffnen.** „Semantische Suche wird vorbereitet …" muss erscheinen,
   solange die 118 MB laufen, und danach auf „Semantische Suche aktiv" wechseln. Auf einer langsamen
   Leitung steht der erste Text entsprechend länger.
2. **Statuszeile im Regelfall.** Bei jedem weiteren Öffnen steht binnen einer Sekunde „Semantische
   Suche aktiv"; sie darf die Meldungen „Der Assistent denkt nach…", „Die Online-Dokumentation wird
   durchsucht…" und die Aktionsanzeige **nicht** überschreiben (die Sperrlogik hat Vorrang, im Code
   über `return` gelöst — im Betrieb einmal nachsehen).
3. **Tooltip** der Statuszeile nennt Modell, Lizenz und Herkunft — auch auf Englisch.
4. **Hilfe-Betrieb ohne KI** (`KiEinwilligung.Abgeschaltet`): Die Statuszeile gehört zur Leiste, die
   dort sichtbar bleibt; erwartet wird derselbe Text.
5. **Registerkarte „Komponenten"** in `Form_Lizenz`: der neue Abschnitt steht am Ende, Umlaute und
   Zeilenumbrüche stimmen, auch im RTF-Export.
6. **Erstbezug ohne Netz**: Die Statuszeile darf nicht hängenbleiben — nach dem gescheiterten
   Versuch wird sie geleert und der Assistent arbeitet normal weiter.

---

## 14. Pflegehinweis — was zu tun ist, wenn das Modell wechselt

1. In `SemantikModell` **vier** Konstanten anfassen: `STAND` (der neue Commit im Hugging-Face-
   Verzeichnis), `NAME`, `LIZENZ` und der Eintrag in `DATEIEN` (Adresszusatz, örtlicher Name,
   **Größe in Byte**, **SHA-256**). Größe und Prüfsumme lassen sich vorab ohne Download ablesen:
   `curl -sI https://huggingface.co/<verzeichnis>/resolve/<commit>/<datei>` liefert
   `X-Linked-Size` und `X-Linked-ETag` (letzterer **ist** der SHA-256 der Datei) — beides sollte
   trotzdem nach dem ersten Bezug gegen die eigene Berechnung geprüft werden.
2. **Der Index heilt sich selbst.** Die Modellmarke `Name@Stand` steht in jeder Indexdatei; nach dem
   Wechsel passt keine mehr, sie werden alle verworfen und neu gerechnet. Nichts von Hand löschen.
3. **Ein anderer Tokenizer ändert alles.** Der Versatz +1 gilt für XLM-R/fairseq. Ein Modell mit
   BERT-WordPiece (`vocab.txt`) braucht statt `SentencePieceTokenizer` den `BertTokenizer` und
   **keinen** Versatz — `SemantikModell.Kennungen` ist dann neu zu schreiben und wieder gegen die
   `tokenizer.json` des neuen Modells Stück für Stück zu prüfen.
4. **Die Schwelle 0,40 hängt am Modell.** Sie liegt zwischen dem gemessenen Fachpaar-Niveau
   (0,65–0,85) und dem Fremdpaar-Niveau (0,24–0,27) dieses Modells. Ein anderes Modell hat eine
   andere Verteilung; die vier Belegpaare des Harnischs sind der Maßstab, an dem sie neu zu setzen
   ist.
5. **Verzeichnisse, die nur ONNX anbieten** (z. B. die `Xenova/*`-Spiegel), führen oft keine
   Lizenzangabe. Für die Komponentenliste zählt das Ursprungsverzeichnis — im Zweifel dort holen,
   auch wenn die Datei dieselbe ist.

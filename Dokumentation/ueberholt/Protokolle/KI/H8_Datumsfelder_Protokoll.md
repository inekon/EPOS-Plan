# H8-Nachtrag — Datumsfelder erreichen das Modell als Datum, nicht als Platzhalter

**Stand:** 29.08.2026
**Grundlage:** Befund 3 aus [`H8_ProjektAktiv_Protokoll.md`](H8_ProjektAktiv_Protokoll.md) § 9
(„Das Feld `geaendert` erreicht das Modell als Platzhalter" — dort offen gelassen).
**Ausgangsstand:** sauberer Arbeitsbaum, HEAD `68c0d95` (Worktree-Branch auf der Spitze
von `Pufferspeicher`).

---

## 1. Ursache — im Code nachgesehen

Zwei Schichten, ein Missverständnis über den Typ:

* **`KiHilfe.Datum`** (`Allgemein\KI\Aktionen\KiAktionen.cs:387`, vor der Änderung)
  formatierte das `DateTime` selbst zu einer **Zeichenkette** (`yyyy-MM-dd`).
* **`KiRueckmeldung.WertKnoten`** (`..\..\..\KiKern\KiRueckmeldung.cs:324–343`) ersetzt
  **jede Zeichenkette** eines Feldwertes vollständig durch ihren Platzhalter
  (`case string` → `Ersetze`, Zeile 330/349) — bewusst pauschal, Feldwerte gelten als
  Bezeichner. Ein **`DateTime`** dagegen behandelt `WertKnoten` **bereits richtig**:
  Zeile 338 formatiert es invariant (`yyyy-MM-dd`) und **ohne** Platzhalterung.

Das Datum kam also eine Schicht zu früh als Text an und wurde wie ein Projektname
verborgen — im H8-Prüflauf wörtlich `"geaendert":"Name 2"`. Das Modell konnte kein
Änderungsdatum nennen.

## 2. Fix — eine Stelle, KiKern unangetastet

`KiHilfe.Datum` liefert jetzt das **`DateTime` selbst** (geboxt); nur das leere Datum
(`default(DateTime)`) bleibt wie bisher `""` (sonst stünde `0001-01-01` im JSON — und
die leere Zeichenkette wird von `Ersetze` ohnehin nicht platzgehalten, es entsteht auch
kein Tabelleneintrag). Die invariant­e Formatierung übernimmt der Kern.

Reichweite — **alle** Datumsfelder der Ergebniszeilen laufen über genau diesen Baustein
(vier Aufrufstellen, alle in `KiAktionenProjekt.cs`):

| Aktion | Fundstelle | Feld(er) |
|---|---|---|
| `projekte_auflisten` | `:40` | `geaendert` |
| `projekt_aktiv` | `:134` | `geaendert` |
| `projekt_lesen` | `:336–337` | `erstellt`, `geaendert` |

Andere Datumswege in den Aktionen gibt es nicht (geprüft: kein weiteres
`ToString("yyyy…")`/`ToShortDateString` im Aktionsordner). Außer `KiRueckmeldung.Erzeuge`
liest im Programm nur `Form_KiChat.cs:1094` die Ergebniszeilen — und dort **nur die
Anzahl**; der Typwechsel des Feldwertes hat also keine zweite Wirkung.

**Nachgezogen in `KiKern.Tests`** (der Kern selbst blieb unberührt): Der DateTime-Zweig
von `WertKnoten` war bislang ungetestet. Neuer Test
`KiRueckmeldungTests.EinDatumGehtAlsDatumHinausNichtAlsPlatzhalter` — Datum geht als
`2026-08-29` hinaus, der Name daneben als `Name 1`, die Tabelle führt **einen** Eintrag
(das Datum legt keinen an).

## 3. Kodierungsbehandlung je Datei

Vor jeder Änderung geprüft, danach unverändert (kein `U+FFFD`, keine reine LF-Zeile):

| Datei | Kodierung | Werkzeug |
|---|---|---|
| `Allgemein\KI\Aktionen\KiAktionen.cs` | UTF-8 **+BOM**, CRLF | Edit |
| `..\..\..\KiKern.Tests\KiRueckmeldungTests.cs` | UTF-8 +BOM, CRLF | Edit |
| `..\..\..\Konzept_Hilfesystem_Wikidokumentation.md` | UTF-8 ohne BOM, CRLF | Edit |
| `H8_ProjektAktiv_Protokoll.md` (Vermerk § 9.3) | UTF-8 ohne BOM, CRLF | Edit |

## 4. Beweise

### 4.1 KiKern.Tests

`dotnet test KiKern.Tests -p:ArtifactsPath=C:\Temp\kibartH8` — **447 Tests, 0 Fehler**
(446 Bestand + 1 neuer).

### 4.2 Build

VS-MSBuild, `WP-Plan.sln`, Debug x64, `-restore`, `-p:OutDir=<Worktree>\dev\build_h8d\`
(Umleitung, damit ein laufendes Programm den Build nicht sperrt): **0 Fehler**,
Warnungen **genau die 5 bekannten** (CS0109 ×2 `KlimaregionStammCtrl`,
CS0108 `StromverbraucherStammCtrl`, CS0108 `WErzeugerModel`, CS1998 `MDIMainForm`) —
keine neue.

### 4.3 Prüfharnisch `..\dev\h8dprobe\`

Wegwerf-Konsolenprojekt nach dem Vorbild `dev\h8probe` (gitignored, keine `.cs` unter
`WindowsFormsApplication1`), gebaut gegen `dev\build_h8d\`, `internal` über Reflexion.
**41 Prüfungen, alle grün** (`ALLES GRUEN`, ExitCode 0). Gegen eine **Wegwerf-Kopie**
von `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` unter `dev\h8dprobe\db\` (Settings.DBPath
in-process umgebogen, ohne `Save()`; die Kopie kann jederzeit gelöscht werden). Kein
Aufruf bei Google — Werkzeugrunde über den eingespeisten `KiChatService.Modellkanal`;
die Einwilligung lag auf diesem Rechner nicht vor, wurde für den Lauf eingehängt und
mit `Zuruecknehmen()` nachweislich wieder entfernt.

| # | Block | Kernergebnis |
|---|---|---|
| 0 | Wegwerf-DB | `DBPath` zeigt auf die Kopie, `Program.startfrm == null` |
| 1 | Baustein | `KiHilfe.Datum` liefert **DateTime**; leeres Datum bleibt `""` (kein `0001-01-01`) |
| 2 | Serialisierung (synthetisch) | `"geaendert":"2026-08-29"` **echt**, `"projektname":"Name 1"`, Klarname nirgends, leeres Datum bleibt leer, Tabelle führt **nur** den Namen (kein Datums-Eintrag), Rückweg stimmt |
| 3 | `projekt_aktiv` gegen die Kopie | Zeile führt `geaendert` **lokal als DateTime**; JSON: `"geaendert":"2026-08-29"` **== `Tab_Projekt.Aenderungsdatum`** (id 1042), `"projektname":"Name 1"`, Klarname („Booster-Kette …") nirgends, ID unverändert |
| 4 | `projekte_auflisten` | 30 Projekte; **alle 20** serialisierten Zeilen: `geaendert` ist Datum (20 echte, 0 leer), jeder Projektname `Name n`; **keiner** der 33 gesammelten Klarnamen (Projekt/Kunde/Bearbeiter) im JSON |
| 5 | `projekt_lesen` | `"erstellt":"2026-08-15"`, `"geaendert":"2026-08-29"` (== DB), kein Klarname |
| 6 | Werkzeugrunde Ende zu Ende | 2 Runden über den Modellkanal; **Runde 2 trägt das echte Datum in der `functionResponse`**, dazu weiterhin `Name 1`; **beide** Runden (4628/5009 Zeichen) ohne Klarnamen; `KiAntwort.Text` bleibt platzgehalten (H8 unverändert); Tageszähler unverändert |

Wörtlich, was jetzt an das Modell geht (Block 3):

```json
{"aktion":"projekt_aktiv","status":"ausgefuehrt","anzahl":1,
 "text":"Aktuell geöffnet ist das Projekt Name 1 (ID 1042).",
 "zeilen":[{"id":1042,"projektname":"Name 1","kunde":"","bearbeiter":"","geaendert":"2026-08-29"}]}
```

## 5. Einordnung Datenschutz

Ein Kalenderdatum ist **kein Bezeichner** — es identifiziert weder Projekt noch Kunde
und stand vor der Platzhalterung (als `"Name 2"`) ohnehin in der Tabelle, nur nutzlos.
Die Zusage „Bezeichner verlassen das Programm nur als Platzhalter" gilt unverändert;
geprüft ist sie in Block 2–6 (kein Klarname in keinem gesendeten Rumpf). Zahlen, IDs
und Wahrheitswerte gingen schon immer ungeschützt — das Datum reiht sich dort ein
(Fachkonzept 4.2).

## 6. Offener Prüfpunkt für die Abnahme (nur am laufenden Programm)

Projekt öffnen, Aktionsbetrieb, fragen: **„Wann wurde das aktuelle Projekt zuletzt
geändert?"** → der Assistent ruft `projekt_aktiv` und **nennt das Datum**;
„Was wird gesendet?" zeigt das Datum im Klartext (kein Bezeichner) und die Namen
weiterhin als Platzhalter.

## 7. Nicht angefasst

`..\..\..\KiKern\` (inklusive `KiRueckmeldung`/`KiPlatzhalter` — der DateTime-Zweig
bestand schon), `KiSchreibschutz.cs`, `KiEinwilligung.cs` (FASSUNG bleibt 2, es wird
nichts Neues übertragen — nur ein Feld, das bisher verstümmelt ankam, kommt jetzt an),
`KiChatService.cs`, `Form_KiChat.cs`, Ressourcen. Befund 4 aus H8 § 9 (Klarnamen in
Ablehnungsgründen) bleibt **offen** — eigenes Paket.

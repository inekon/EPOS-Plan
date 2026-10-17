# Die Volumina der Verfahren als Wahl der Füllstandslinie (29.09.2026)

Protokoll des Auftrags **F1** der Sitzung „Zapfprofilgenerator Cloud". Anwenderauftrag vom
29.09.2026: „setze um: die Volumina der einzelnen Verfahren als Wahl der Füllstandslinie". Er
löst die Folge „#594 — Volumina der einzelnen Verfahren als Wahl (Schemaschritt)" aus dem
Umsetzungskonzept Zapfprofilgenerator, Nachtrag N36 (d) ein; Grundlagen 4.7, N10 (k), N11 (d),
N13 (o). Vorgänger: `2026-09-27_Fuellstandslinie_Wochenbild.md` (Auftrag D9, die Wahl an ihren
Platz über dem Wochenbild).

**Rahmen.** Worktree `agent-aa7e3d99f930612d8`, Zweig `worktree-agent-aa7e3d99f930612d8` von
`c38a1ba` (= `origin/ios_migration_september`), Opus 5, Cloud-Umgebung (Linux). Mit
Schemaschritt, ohne Rechenwegwechsel; Mockup unberührt, kein Push.

---

## 1 Befund

| Stelle | Stand vor dem Auftrag |
|---|---|
| Wahl | „Speichergröße der Füllstandslinie" über dem Wochenbild mit vier Einträgen: Nenninhalt des Punkts, Punkt, Nenninhalt des Bands, V_max des Bands — je mit Litern, nicht bestimmbare gesperrt mit Grund |
| Kern | `ZapfFuellstandbezug` 1 … 4; `BezugsvolumenL(art)` und `Fuellstandsperre(art)` lasen vier Ergebnisfelder; der Verfahrensvergleich entstand erst NACH dem Füllstandsblock |
| Speicherung | `Tab_TwwProjekt.Fuellstand_Bezug INTEGER CHECK (… IN (1,2,3,4))` (Schritt 124), Wertemenge `TwwSchema.FUELLSTAND_BEZUG_WERTE` |
| Konzept | N36 (d): „Die Volumina der einzelnen Verfahren stehen nicht zur Wahl — das bräuchte einen Schemaschritt an `Tab_TwwProjekt.Fuellstand_Bezug`." |

Hülle und Dialog waren schon über die Wertemenge des Schemas geführt (`TwwSchema.Werte(…)`):
Sie brauchten **keine** Änderung an ihrer Mechanik, nur die erweiterte Wertemenge und die Texte.

## 2 Umsetzung

| Schicht | Datei | Änderung |
|---|---|---|
| Kern | `EPOS.Kern/Allgemein/Zapfprofil/TwwSpeicherauslegung.cs` | `ZapfFuellstandbezug` um 5 … 8 erweitert (Verfahren + `VERFAHREN_VERSATZ` = 4); der Verfahrensvergleich entsteht VOR dem Füllstandsblock und geht in `Fuellstandbezug(…)`; neu `VerfahrenZuBezug` und `VerfahrensvolumenL`; `BezugsvolumenL` liest für 5 … 8 die Zeile des Vergleichs, `Fuellstandsperre` nennt je Verfahren den Grund |
| Schema | `EPOS.Kern/Allgemein/Update/TwwFuellstandSchema.cs` (neu) | Schritt `KesselHeizgrenzeSchema.SCHRITT + 1`: Neubau von `Tab_TwwProjekt` mit `CHECK ("Fuellstand_Bezug" IN (1,…,8))` nach dem Hausrezept; der Neubau selbst ist `TwwBezugsartSchema.Neubau` (EIN Rezept), `Verletzt` dort auf `internal` gehoben |
| Schema | `TwwSchema.cs`, `SchemaStand.cs`, `Paketanhebung.cs` | `FUELLSTAND_BEZUG_WERTE = "1,…,8"` (Grundschema = Schritt); `Zielversion = TwwFuellstandSchema.SCHRITT`; Stufe `Art.Ddl` |
| Schale | `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` | Konstante `SCHRITT_TWW_FUELLSTAND_VERFAHREN`, Eintrag in `SCHRITTE_SQLITE` hinter 154, Methode `Schritt_TwwFuellstandVerfahren` nach dem Muster von `Schritt_TwwBezugsartZimmer` |
| Werkzeug / Vorrichtung | `Werkzeuge/Testdatenbankschema/Program.cs`, `EPOS.Kern.Tests/TestDatenbank.cs` | Schritt aus derselben Quelle, hinter der Heizgrenze |
| Texte | `EPOS.Kern/MyResource/Resource(.en-US).resx`, `Resource.Designer.cs` | `ZPG_SATZ_BEGRIFF_FUELLSTAND_5…8` (profilbasiert, DIN 4708, Faustwert mit Gleichzeitigkeit, klassischer Faustwert (nachrichtlich)) und vier `ZPG_SATZ_FUELLSTAND_GESPERRT_VERFAHREN_*` — beide Sprachen; Designer neu erzeugt (13 121 Blöcke gleich, 8 neu, zweiter Lauf +0) |
| DTO | `EPOS.UI/Dialoge/Bedarf/ZapfprofilDaten.cs` | Spiegel `ZapfprofilFuellstandbezug` um 5 … 8 erweitert |
| Hülle, Dialog | `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.Auslegung.cs`, `EPOS.UI/Dialoge/Bedarf/ZapfprofilAuslegungDialog.razor` | **keine Mechanik** — nur Kommentare; beide nehmen die Wertemenge des Schemas und die Sätze des Kerns |
| Wiki-Quelle | `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki` | Absatz „Verfahrensvergleich": die Verfahren als Einträge der Wahl, Zusatz „nachrichtlich", Beispiel „DIN 4708 · 450 l", die Vorgabe löst nie auf ein Verfahren auf |

## 3 Entscheide

- **Nummern 5 … 8 mit festem Versatz** statt einer eigenen Zuordnungstabelle: Bezug = Verfahren
  + `VERFAHREN_VERSATZ` (4). Ein neues Verfahren im Vergleich bekommt damit von selbst seinen
  Bezug; die Wertemenge des Schemas bleibt die eine Quelle für DDL, Schreibweg, Hülle und Dialog.
- **Das Volumen kommt aus der ZEILE des Vergleichs**, nicht aus `VolumenProfilL` & Co.: Es ist
  genau die Zahl, die die Tabelle zeigt, samt ihrem Gültigkeitsmerkmal. Dafür entsteht das
  `Verfahren`-Feld im Rechenweg jetzt vor dem Füllstandsblock — dieselben Werte, nur früher.
- **Das klassische Verfahren ist wählbar** (Anwenderauftrag: „die Volumina der einzelnen
  Verfahren"), obwohl es nie im Plausibilitätsband liegt. Damit das am Eintrag sichtbar bleibt,
  trägt sein Begriff den Zusatz „(nachrichtlich)" — in der Klappliste, in der Zeile „angesetzt",
  in der Kachel und in der Warnliste derselbe Wortlaut.
- **Vier eigene Sperrgründe statt eines allgemeinen Satzes:** Warum ein Verfahren kein Volumen
  liefert, ist je Verfahren etwas anderes (D_max = 0; DIN gilt nicht; kein gültiger Normvergleich
  oder keine Personenzahl; keine Personenzahl). Der Kern entscheidet es an seinen Ergebnisfeldern,
  die Hülle übersetzt nur.
- **Die Vorgabe bleibt unverändert** und löst nie auf ein Verfahren auf — sie ist die Regel aus
  N10 (k) und keine Wahl; ein gespeicherter Verfahrensbezug ohne Volumen fällt mit dem
  vorhandenen Hinweis `FUELLSTAND_BEZUG_VORGABE` auf sie zurück.
- **Nur Ergebnisfelder und Darstellung.** Die Speicherauslegung ist nachrichtlich (4.7): Defizit,
  Band, Nenninhalt, Verfahren und Warnliste bleiben von einer Wahl unberührt, geprüft im Kerntest.
- **Der Neubau ist EIN Rezept.** `TwwFuellstandSchema` ruft `TwwBezugsartSchema.Neubau` und
  `…Verletzt`, statt sie ein zweites Mal zu schreiben.

## 4 Schemaschritt 155

`Tab_TwwProjekt` ist eine Projekttabelle (STRICT, `ID_Projekt → Tab_Projekt` mit
`ON DELETE CASCADE`, Kindtabelle `Tab_TwwKonstruktorzeile`). Der Schritt geht das Hausrezept:
Fremdschlüssel aus vor der Transaktion, Ausweichen unter `legacy_alter_table`, Neubau aus dem
GELTENDEN `sqlite_master`-Text mit getauschter Prüfklausel, Zeilen namentlich,
`sqlite_sequence` mit, Indizes und Trigger wieder, `foreign_key_check` für Tabelle und
Kindtabelle noch in der Transaktion. **Die Nummer steht an EINER Stelle**
(`TwwFuellstandSchema.SCHRITT = KesselHeizgrenzeSchema.SCHRITT + 1`), damit die Orchestrierung
beim Zusammenführen umnummerieren kann.

**Grundschema = Schritt:** Eine neu angelegte Datenbank (T1 … T5, Spalten aus T3 und T3
„Typtage") und eine nachgezogene tragen Zeichen für Zeichen denselben CREATE-Text; der Test
hält das gegen die Repo-Testdatenbank.

**Projektkopierer und Projekttransfer** bleiben unberührt: `Fuellstand_Bezug` reist als Spalte
der Projektzeile mit (`ProjektExportImportCtrl.Tww`, Schritt 124), die neuen Werte laufen
durch, und ein älteres Paket trägt nur 1 … 4 oder NULL — deshalb `Art.Ddl` in
`Paketanhebung.STUFEN`.

## 5 Testdatenbank

Arbeitskopie außerhalb des Repositoriums, `Werkzeuge/Testdatenbankschema` gezogen (1 Tabelle
neu gebaut), zweiter Lauf 0 Änderungen, `integrity_check` ok, `foreign_key_check` leer.
Zellvergleich über alle Tabellen gegen die Ausgangsfassung `2e417b36…`:

| Abweichung | Bewertung |
|---|---|
| `Tab_Applikation.SchemaVersion` 154 → 155 | erwartet |
| Schema `Tab_TwwProjekt`: `CHECK ("Fuellstand_Bezug" IN (1,2,3,4))` → `(1,…,8)` | erwartet, einzige Textänderung |
| `sqlite_sequence`: gleiche Werte, `Tab_TwwProjekt` ans Ende gewandert | Folge des Tabellenneubaus (der Zähler wird gelöscht und wieder geschrieben) |

Keine weitere Zelle. `Tab_TwwProjekt` des Referenzprojekts 1045 bleibt zellgleich
(`Fuellstand_Bezug` = NULL) — die Einfrierregel „gesäte Zapfprofil-Eingaben" ist nicht berührt.
Neue Fassung 71 622 656 Byte, LFS-SHA-256
`2b28fb6f4783e5c64df8c6e2d1691ffca53f362bc6a6735ca8d97fc612d6ee44`; committet mit aktivem
LFS-Filter (`git check-attr filter` = `lfs`), getrennt vom Code.

## 6 Tests

| Projekt | Datei | Fälle |
|---|---|---|
| `EPOS.Kern.Tests` | `TwwFuellstandSchemaTests.cs` (neu) | Nummer lückenlos und Ziel, Stufe der Paketanhebung; EINE Wertemenge (Aufzählung, Grundschema, Schritt) und der Versatz je Verfahren; Zieltext tauscht allein die Prüfklausel (fertig, ohne Spalte, fremde Klausel, nicht STRICT, leer); Grundschema und Zieltext zeichengleich samt greifender Klausel (8 ja, 9 und 0 nein; vor dem Schritt wies dieselbe Tabelle 5 ab); Neubau auf der Arbeitskopie (Zeilen, IDs, Zähler, Kindtabelle, `foreign_key_check`, `integrity_check`, kein Hilfsname, zweiter Lauf ohne Wirkung, danach jedes Verfahren schreibbar); Werkzeug-, Migrations- und Vorrichtungswache samt Repo-Datei |
| `EPOS.Kern.Tests` | `SpeicherauslegungTests.cs` | +1: je Zeile des Vergleichs ein Bezug mit derselben Zahl und ohne Grund; Wahl auf DIN 4708 verschiebt allein den Füllstand (Defizit, Verfahren, Band, Nenninhalt, Warnliste gleich); das nachrichtliche Verfahren trägt die Linie; D_max = 0 sperrt allein das profilbasierte; ohne Normvergleich und Personenzahl alle vier benannt gesperrt, die Wahl fällt mit Hinweis zurück |
| `EPOS.Kern.Tests` | `ZapfprofilHuelleZ4Tests.cs` | acht Einträge, die Verfahrenswerte gleich den Zeilen des Vergleichs, drei Sperrgründe wörtlich, Wahl auf das profilbasierte Verfahren, drei Sätze auf Englisch |
| `EPOS.Kern.Tests` | `ZapfprofilAuslegungHuelleTests.cs` | Fall auf der Testdatenbank: acht Einträge, Verfahrenswerte aus dem Vergleich |
| `EPOS.Kern.Tests` | `ZapfprofilHuelleEditorenTests.cs` | die Wertemenge der Hülle ist die des Schemas, die vier Verfahrensnamen wörtlich (mit „(nachrichtlich)") |
| `EPOS.Kern.Tests` | `ZapfprofilSchritt124Tests.cs` | Speichern und Lesen jedes Verfahrenswerts samt Spaltenwert; ein unbekannter Wert (9) wird benannt abgelehnt |
| `EPOS.Kern.Tests` | `TwwSchemaTests.cs` | Wertemenge 1 … 8, 8 geht, 9 nicht |
| `EPOS.UI.Tests` | `ZapfprofilAuslegungDialogTests.Fuellstand.cs` | neun Optionen mit Litern samt Verfahrensnamen; ein Verfahren ohne Volumen gesperrt mit `title` und sichtbarem Grund; +1 Fall: Wechsel auf DIN 4708 zeichnet Bild, Kachel und Herleitung neu, der Assistent setzt auch das nachrichtliche Verfahren |
| `EPOS.UI.Tests` | `ZapfprofilAuslegungDialogTests.Vergleich.cs` | Prüfstand auf acht Einträge erweitert, neun Optionen |

`KiMaskenabdeckungWacheTests`, `KiDialogkatalogTests` und `KiFeldwerteTests` bleiben unverändert
grün — das Feld ist dasselbe, nur mit mehr Einträgen.

## 7 Gate

Commits `c897ec1` (Umsetzung samt Schemaschritt), `cbd6365` (Testdatenbank),
`c3d22ef` (Tests), dazu die Papiere.

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| `dotnet test WP-Plan.Kern.slnf -c Release` (voller Lauf) | siehe Bericht der Welle — Zahlen je Testprojekt |
| `dotnet test Werkzeuge/Auslieferungsvorlage`, `… ZapfprofilValidierung` | grün |
| `SqlDialektPruefer` gegen die Testdatenbank | ohne Fundstelle |
| `designer_neu.py` | 13 121 Blöcke gleich, 0 abweichend, 8 neu; zweiter Lauf +0 (die Byteabweichung LF/CRLF unter Linux ist bekannt) |
| Windows-Schale (`EnableWindowsTargeting=true`) | 0 Fehler |
| Wiki-Gegenlesen (Regex aus `CLAUDE.md`) | ohne Fundstelle; keine Produktdaten |

## 8 Referenzlauf

Fünfzehn Projekte auf Linux vor und nach der Änderung: **460/460 CSV byte-gleich** (allein
`protokoll.txt` unterscheidet sich, es trägt Uhrzeiten). Der „Vorher"-Lauf entstand aus einem
Auszug des Standes `c38a1ba` mit der Testdatenbank auf Schemastand 154, der „Nachher"-Lauf im
Worktree mit der Testdatenbank auf 155. `vergleich` gegen die im Worktree liegende Basis
`2026-09-27_R24_Heizgrenze`: **GESAMT PASS**, alle fünfzehn Projekte (4 899 525 Werte). Den
Vergleich gegen die auf `origin` geltende Basis zieht die Orchestrierung nach dem
Zusammenführen.

## 9 Folgen

- **Windows-Sichtabnahme:** Breite der Klappliste mit den längeren Verfahrensnamen in WebView2,
  gesperrter Eintrag eines Verfahrens auf Touch.
- **Schemanummer:** 155 ist gegen `origin` frei; wird beim Zusammenführen umnummeriert, ändert
  sich allein `TwwFuellstandSchema.SCHRITT`.
- **Umsetzungskonzept:** N36 (d) sagt heute, die Verfahrensvolumina stünden nicht zur Wahl; der
  Nachtrag dazu (N37) gehört der Orchestrierung.
- **Statuszeile** und **Logbuch-Satz** schreibt die Orchestrierung; der Entwurf steht im Bericht.
- **Wiki:** Die Quelle ist fortgeschrieben; hochgeladen wird gebündelt.

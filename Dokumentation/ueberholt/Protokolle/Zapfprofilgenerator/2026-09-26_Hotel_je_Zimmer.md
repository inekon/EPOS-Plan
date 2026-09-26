# Hoteltyp „je Zimmer“: Welle #579 (26.09.2026)

Protokoll des Postens **#579**. Anwenderentscheid vom 26.09.2026 zur Empfehlung der
[Hotel-Durchsicht](2026-09-26_Hotel_Durchsicht_Modellannahmen.md), Punkt (d): **Weg 1** (Name
schärfen) und **Weg 2** (Hilfehinweis) jetzt; **Weg 3** (eigene Bezugsart „Zimmer“) bleibt
Folgeposten. Umsetzungskonzept Zapfprofilgenerator, Nachtrag N34; Kapitel 9 ZU36; Prüfliste ZU21;
Validierungsbericht Abschnitt 9.

**Rahmen.** Worktree `zhot`, Zweig `zhot` von `805add7e6` (Schemastand 150, Testdatenbank
`09b6c523`), Opus 5.5. Kein Schemaschritt, kein Basiswechsel, kein Push.

**Befund vorab.** Der Katalogtyp trägt Bezugsart 3 (Betten), seine Kennwerte 3,2 / 3,9 / 4,9 kWh je
Tag sind je **Zimmer** gebildet (ein Zimmer = ein Bett). Wer die Bettenzahl eines Hauses mit
Doppelzimmern eingibt, liegt um den Faktor 1,5 bis 2 zu hoch.

---

## 1 Name

| Ort | vorher | nachher |
|---|---|---|
| Nutzungsart (Paketteil, Testdatenbank) | „Hotel (aus Messung)“ | **„Hotel (aus Messung, je Zimmer)“** |
| Tagesgangsatz | „Hotel (aus Messung)“ | unverändert — die Form gilt je Haus, der Zusatz betrifft allein die Bezugsmenge |

Quelle des Namens ist `Referenzlaeufe/Skripte/tww_hotel_aus_messung.json` (Schlüssel `nutzungsart`,
neu `tagesgangsatz`); die Regel `hotel_aus_messung_bauen.py` schreibt beide Schlüssel.
`tww_testkatalog_fiktiv.py` nimmt den Satznamen aus `tagesgangsatz` und benennt in der
Testdatenbank eine Zeile unter einem früheren Bezeichner vor dem Nachführen um
(`UMBENANNTE_NUTZUNGSARTEN`) — dieselbe ID samt Zapfkategorien, keine zweite Zeile. Werte,
Bezugsart, Kalender und Provenienz unverändert.

**Wo der Bezeichner Schlüssel ist.** Natürlicher Schlüssel einer Nutzungsart ist Bezeichner und
Katalogversion (Saatskript, Katalogimport). Kein Quelltext des Kerns, der Hülle oder der Oberfläche
fragt den Namen ab. `ZapfprofilReferenzprojektWacheTests` und Projekt 1045 benutzen „Wohnen groß
(abgeleitet)“ — nicht betroffen. Das Validierungswerkzeug sucht die Nutzungsart eines Objekts über den
Namen in `objekt.json` (Objektdateien außerhalb des Repositoriums; Konverter `norwegen.py`
nachgezogen). Nachgezogen: `Werkzeuge/Auslieferungsvorlage.Tests/TwwVorlageTests` (Nutzungsart mit
neuem Namen, Tagesgangsatz mit altem), Kommentare in `KatalogpflegeTests`, `TwwKatalogWacheTests`.

## 2 Testdatenbank und Paketteil — die Befehlsfolge

```
py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite --paketteil-schreiben
py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite
```

(mit `PYTHONIOENCODING=utf-8`; Quellen aus Commit `b5d738f33`). Die Folge setzt sich auf jede
spätere Fassung der Testdatenbank neu auf.

| Schritt | Ergebnis |
|---|---|
| Ausgang | Fassung `09b6c523` (71 557 120 Byte, Schemastand 150) |
| erster Lauf | Paketteil: 1 von 4 Dateien neu (`Tab_TwwNutzungsart_STAMM.csv`, Zeile 7 Bezeichner); Datenbank „0 Zeile(n) angelegt, 1 nachgeführt“ |
| zweiter Lauf | „0 Zeile(n) angelegt, 0 nachgeführt“ |
| Zellvergleich gegen die Ausgangsfassung (alle Tabellen, Schema gleich) | allein `Tab_TwwNutzungsart_STAMM` ID 9, Spalte `Bezeichner`: „Hotel (aus Messung)“ → „Hotel (aus Messung, je Zimmer)“; Zeilenzahlen unverändert, keine andere Tabelle |
| integrity_check / foreign_key_check | ok / 0 |
| LFS | Zeiger `1923b7d7`, 71 557 120 Byte, Filter aktiv, keine `-shm`/`-wal` |

Kein Referenzprojekt benutzt die Zeile; die Einfrierregel „gesäte Zapfprofil-Eingaben“ ist nicht
berührt, die Basis bleibt.

## 3 Hinweis in der Zonenmaske

| Teil | Datei | Änderung |
|---|---|---|
| Kern | `EPOS.Kern/Allgemein/Zapfprofil/Nutzungsart.cs` | `Nutzungsart.BezugsmengeIstZimmerzahl`: Bezugsart Betten und „je Zimmer“ im Namen (`ZUSATZ_JE_ZIMMER`, ohne Groß-/Kleinschreibung) |
| Ressourcen | `Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | `ZPG_HINW_BEZUGSMENGE_ZIMMER` — „Bezugsmenge ist die Zimmerzahl, nicht die Bettenzahl“ / „Reference quantity is the number of rooms, not the number of beds“ |
| Hülle | `EPOS.UI.Daten/Bedarf/ZapfprofilHuelle.cs` | `AlsNutzungsart` setzt `HinweisBezugsmenge` aus der Ressource, sonst leer |
| DTO | `EPOS.UI/Dialoge/Bedarf/ZapfprofilDaten.cs` | `ZapfprofilNutzungsartDaten.HinweisBezugsmenge` (allgemein: Hinweis am Feld der Bezugsmenge) |
| Dialog | `EPOS.UI/Dialoge/Bedarf/ZapfprofilDialog.razor` | Herleitungszeile unter dem Feld der Bezugsmenge, sobald die gewählte Nutzungsart einen Hinweis trägt |
| Tests | `EPOS.Kern.Tests/ZapfprofilHuelleBezugsmengeTests` (neu, 7 Fälle), `EPOS.UI.Tests/Dialoge/ZapfprofilDialogTests` (+1) | Regel, Hülle in beiden Sprachen, leerer Hinweis; bunit: Zeile mit Hinweis, keine ohne |
| Wiki | `Projekte/Wiki/Programm Dokumentation - Brauchwasser-Zapfprofil.wiki` | neuer Name; ein Satz zum Hinweis der Zonenmaske; Tabuwortprüfung ohne Treffer |

Einen Hinweiskanal je Nutzungsart gab es nicht: Die Warnliste trägt Meldungen der Rechnung, die
Hinweise der Stufe Erweitert stehen als feste Texte am Feld. Das DTO-Feld ist deshalb der kleinste
Weg ohne Datenbank in der Oberfläche; die Regel liegt im Kern, der Text in der Hülle.

## 4 Kurzlauf des Validierungswerkzeugs

`dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- C:\Waermeplan\Messreihen_extern\konvertiert\objekte --ziel <Scratch> --katalog <Kopie der neu gesäten Testdatenbank>`
nach dem Umbenennen in den drei Objektdateien `NO-HO1`, `NO-HO2`, `NO-HO4` (außerhalb des
Repositoriums): 21 Objekte, 44 Berichtsdateien, die Hotels finden den Typ.

| Zählung | vierter Lauf | Kurzlauf #579 |
|---|---|---|
| Objekte grün / gelb / rot | 3 / 0 / 18 | 3 / 0 / 18 |
| Band | 8 / 10 / 3 | 8 / 10 / 3 |
| Form | 6 / 0 / 15 | 6 / 0 / 15 |
| Energie | 21 / 0 / 0 | 21 / 0 / 0 |
| √N | +0,44 | +0,44 |

## 5 Gates

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| gefilterte Tests (Zapfprofil, Tww, Katalogpflege, Resource, Wachen) | Kern 896, UI 304 — 0 Fehler |
| voller Lauf `WP-Plan.Kern.slnf` | VOLLLAUF |
| `Werkzeuge/ZapfprofilValidierung.Tests` | 38 / 38 |
| `Werkzeuge/Auslieferungsvorlage.Tests` | 38 / 38 |
| Windows-Schale `WindowsFormsApplication1` | 0 Fehler |
| `designer_neu.py` | +1 Eintrag, zweiter Lauf +0 (der Designer schreibt LF; auf CRLF zurückgesetzt) |
| `SqlDialektPruefer` | nicht gezogen — kein SQL-Text im C#-Bestand geändert (die neue Anweisung steht im Saatskript) |
| Referenzlauf 1030, 1007, 1017, 1045, 1046, 1047, 1049 gegen `2026-09-26_R22_Solarthermie` | 7 / 7 PASS |

## 6 Folgen

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| Hotel | Bezugsart „Zimmer“ (Weg 3): eigene Bezugsart statt Betten mit Namenszusatz | Anwender | auf Zuruf |
| Import | Eine Datenbank, die `FREI-1` unter dem früheren Namen schon trägt, bekäme beim erneuten Einspielen eine zweite Zeile — Katalogversion anheben oder alte Zeile umbenennen | Orchestrierung | vor der nächsten Auslieferung |
| Wiki | Logbuch-Satz unter 1.2.0.5 mit dem Sammel-Upload | Orchestrierung | mit dem Upload |

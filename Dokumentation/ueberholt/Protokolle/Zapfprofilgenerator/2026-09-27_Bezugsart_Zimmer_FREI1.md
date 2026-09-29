# Bezugsart „Zimmer“ und die Import-Dublette FREI-1: Auftrag A2 (27.–29.09.2026)

Protokoll des Auftrags **A2** der Sitzung „Zapfprofilgenerator Cloud“. Anwenderentscheid vom
27.09.2026 („Setze Teil A um“) zu den zwei Folgeposten aus Nachtrag N34 des Umsetzungskonzepts
Zapfprofilgenerator (Folgentabelle): **Weg 3** der Hotel-Durchsicht — die eigene Bezugsart „Zimmer“
statt Betten mit Namenszusatz — und der **Umgang mit dem früheren Namen** in einer Datenbank, die den
freien Paketteil schon trägt. Vorzustand: Protokoll [Hotel je Zimmer](2026-09-26_Hotel_je_Zimmer.md).

**Rahmen.** Worktree `agent-aeaa4f06bdda9aeeb`, Zweig `worktree-agent-aeaa4f06bdda9aeeb` von
`903f926` (Schemastand 151, Testdatenbank `22b1f882…`), Opus 5.5, Cloud (Linux). Die Arbeit brach am
27.09. am Wochenlimit ab (alles uncommittet) und wurde am 29.09. fortgesetzt: Die Testdatenbank im
Worktree wurde zuerst gegen `HEAD` geprüft (Zellvergleich über eine Scratch-Kopie, Befund unten) und
war im gewollten Stand. Schemaschritt 152 war gegen `origin` gemessen frei. Kein Push, kein CI-Lauf.

---

## 1 Entscheide

| Kennung | Inhalt | Umsetzung |
|---|---|---|
| E-A2-1 | `ZapfBezugsart.Zimmer = 8`; „Hotel (aus Messung, je Zimmer)“ trägt Zimmer statt Betten, Name, Werte, Kalender und Tagesgangsatz bleiben; Zimmer verhält sich wo Betten eine Sonderrolle hat wie Betten | `Nutzungsart.cs`; Stellenliste in Abschnitt 2 |
| E-A2-2 | Namensregel `BezugsmengeIstZimmerzahl`/`ZUSATZ_JE_ZIMMER` und Hinweis `ZPG_HINW_BEZUGSMENGE_ZIMMER` entfallen; der Kanal `ZapfprofilNutzungsartDaten.HinweisBezugsmenge` samt Herleitungszeile im Dialog fällt mit (kein Erzeuger mehr) | Kern, Hülle, DTO, `ZapfprofilDialog.razor`, beide `.resx`; Tests umgeschrieben |
| E-A2-3 | Schemaschritt: beide Tabellen mit Bezugsart neu gebaut, CHECK 1..8, Grundschema = Schritt | `TwwBezugsartSchema` (Nummer allein bei `TwwBezugsartSchema.SCHRITT` = `KonditionierungSchema.SCHRITT + 1` = 152) |
| E-A2-4 | FREI-1-Dublette: EINE Kern-Regel „frühere Stände der ausgelieferten Paketzeilen“, wirksam in Schemaschritt, Katalogimport und den übrigen Lesewegen | `PaketteilNachfuehrung` |
| E-A2-5 | Paketteil, Hotel-JSON und Regel wiederholbar nachgezogen | Paketteil-CSV Bezugsart 8, `tww_hotel_aus_messung.json` (`bezugsart: 8`), `hotel_aus_messung_bauen.py`, `tww_testkatalog_fiktiv.py`, `Katalogpaket_frei/LIESMICH.md` |
| E-A2-6 | Validierungswerkzeug auf Zimmer | `Katalogbau` (Regel), `norwegen.py`, LIESMICH, Test |

## 2 Stellenliste der Bezugsart

Jede Stelle, die auf `ZapfBezugsart` verzweigt oder Betten besonders behandelt:

| Stelle | Verhalten bei Betten | Entscheid für Zimmer |
|---|---|---|
| `ZapfBezugsart` (`Nutzungsart.cs`) | Wert 3 | neuer Wert 8 |
| `TwwSchema.BEZUGSART_WERTE`, `SQL_CREATE_NUTZUNGSART`, `SpaltenT3` (Bedarfstag) | CHECK 1..7 | 1..8 aus EINER Konstante; Schreibwege (`TwwNutzungsartCtrl`, Konstruktor, Katalogimport) prüfen über `Enum.IsDefined` bzw. die Wertemenge |
| `Mengengeruest.WohnungstabelleWirksam` (DIN-4708-Wohnungstabelle) | nur Personen und Wohneinheiten | wie Betten: keine Wohnungstabelle |
| `Mengengeruest.FlaecheM2`/`WohneinheitenZahl` | keine Sonderrolle | unverändert |
| `Zapfeinheiten.Anzahl` (Stochastik, n_E) | Vorgabezweig: n_E = Bezugsmenge | wie Betten (Kommentar nennt Zimmer) |
| `Typtagzuordnung.Einheiten` (VDI-4655-Typtage) | nur Personen/Wohneinheiten, sonst benannte Ablehnung | wie Betten: abgelehnt, Begriff „Zimmer“ |
| `ZapfprofilAuslegung.Gruppe`/`Katalogtag` | Mengen je Bezugsart getrennt, nie summiert | Zimmer ist eine eigene Menge: ein Tag der Bezugsart Betten skaliert nicht auf Zimmer (benannte Ablehnung wie bei jeder anderen Bezugsart) |
| Ecodesign-Zapfprofile | Wohneinheiten | unverändert |
| Bedarfstag-Konstruktor (`KonstruktorBezug`, `BedarfstagKonstruieren`, Hülle `Bezugsarten`) | wählbar | Zimmer wählbar (Wertemenge 1..8) |
| `Messkalibrierung.Nichtwohnparameter` | je Bezugsmenge | unverändert |
| Katalogimport (`TwwNutzungsartCtrl.Import`, `ImportKatalogzeilen`) | `Enum.IsDefined` | 8 gültig; Regel früherer Stand vor dem Dublettenscan |
| Projektimport/-export (`ProjektExportImportCtrl(.Tww)`) | Wert reist | Wert reist; Regel früherer Stand vor der Suche über den natürlichen Schlüssel |
| Klappliste `TwwNutzungsartAdminDialog` (Hülle `Enum.GetValues`) | Eintrag „Betten“ | Eintrag „Zimmer“ (acht Einträge) |
| Beschriftung der Bezugsmenge (`ZapfprofilHuelle.Bezugsgroesse`/`Einheit`, `ZapfprofilDialog`) | „Betten“/„Bett“ | „Zimmer“/„Zimmer“, en „rooms“/„room“ (`ZPG_BEZUG_ZIMMER`, `ZPG_EINHEIT_ZIMMER`); kein Hinweis unter dem Feld |
| Begriffe `Bezugsartbegriff`/`Einheitbegriff` | `BEGRIFF_*_3` | `BEGRIFF_BEZUGSART_8`, `BEGRIFF_EINHEIT_8` |
| KI-Wissen (`KiDialoge`, Wahlfelder „bezugsart“) | Optionen aus der Hülle | ohne Änderung; die Erklärtexte zählen offen auf („…“) |
| `Katalogfilterprofil.SpBezugsart` | Anzeigetext der Hülle | ohne Änderung |
| Validierungswerkzeug (`Katalogbau`, `Objektbefund`, Bericht) | Bezugsart als Text | Regel früherer Stand im Katalogbau; Bericht nennt „Zimmer“ |
| Auslieferungsvorlage (`TwwKataloge.DateiEinspielen`) | — | Regel früherer Stand am externen Katalogpaket |

Weitere Tabellen mit einer Bezugsart samt CHECK gibt es nicht: Zone, Wohnungstyp und Projektzeile
tragen eine Bezugsmenge, keine Art; Messreihen und Konstruktorzeilen keine von beiden (am Schema der
Testdatenbank geprüft, `sqlite_master` nach `Bezugsart`).

## 3 Die Regel der früheren Stände (E-A2-4)

`EPOS.Kern/Allgemein/Zapfprofil/PaketteilNachfuehrung.cs`, zwei Einträge: „Hotel (aus Messung)“ mit
Betten und „Hotel (aus Messung, je Zimmer)“ mit Betten (der Zwischenstand #579 bis A2) → „Hotel (aus
Messung, je Zimmer)“ mit Zimmer.

**Die Grenze.** Die Katalogversion des freien Paketteils ist **nicht** `FREI-1`: Der Paketteil führt
keine Katalogversion, seine Zeilen treten der des Katalogs bei, in den sie kommen (in der Vorlage die des
zuletzt angelegten Parameters, sonst `FREI-1`; in der Testdatenbank `TEST-1`). `FREI-1` ist die
Provenienzversion (`Bedarf_Version` usw.). Die Regel trifft deshalb nur Zeilen mit Status
`AUSLIEFERUNG` (in der Datenbank zudem `ReadOnly = 1`), `Bedarf_Version = FREI-1`,
`Bedarf_Herkunftsart = EIGENKONSTRUKTION` und Name samt Bezugsart eines früheren Stands. Eine
Anwenderzeile gleichen Namens (`EIGEN`, `IMPORT`, in jeder Katalogversion) bleibt unberührt.

| Leseweg | Anschluss |
|---|---|
| (a) Schemaschritt | `PaketteilNachfuehrung.Nachfuehren` im Vorgang des Neubaus: dieselbe ID, Name und Bezugsart; führt die Katalogversion den heutigen Namen schon an einer anderen Zeile, bleibt der Name, nur die Bezugsart geht mit — benannt im Protokoll |
| (b) Katalogimport | beim Lesen der Paketzeile, VOR dem Dublettenscan (Status des Pakets, rohe Provenienz vor `ImportHerkunft`); die Berichtszeile trägt `KATALOGIMPORT_FRUEHERER_STAND` bzw. `…_UND` mit dem übrigen Grund, beide Sprachen |
| (c) Projektpaket (.wpx) | `ProjektExportImportCtrl.TwwFruehererStand` an jedem mitreisenden Nutzungsartkopf vor der Suche über den natürlichen Schlüssel; Satz im Importbericht. Paketanhebung: Stufe 152 `Art.Import` |
| (c) Auslieferungsvorlage | `TwwKataloge.DateiEinspielen` am externen Katalogpaket; Berichtszeile „frueherer Stand: …“. Die Quelle selbst steht auf dem Zielstand (Prüflauf), dort hat der Schemaschritt schon gewirkt |
| (c) Validierungswerkzeug | `Katalogbau.Art`, Hinweis im Katalog |
| Testdatenbank | `UMBENANNTE_NUTZUNGSARTEN` in `tww_testkatalog_fiktiv.py` spiegelt die Einträge (Name und Bezugsart vorher/nachher) nach der Regel der Testdatenbank (Status `EIGEN`, `TEST-1`); Wache `PaketteilNachfuehrungTests.Die_Saatliste_spiegelt_die_Regel_des_Kerns` |

## 4 Schemaschritt 152

`EPOS.Kern/Allgemein/Update/TwwBezugsartSchema.cs`: Neubau von `Tab_TwwNutzungsart_STAMM` und
`Tab_TwwBedarfstag_STAMM` nach dem Rezept der Schritte 96/100 (Fremdschlüssel aus vor der Transaktion,
alte Tabelle unter `legacy_alter_table` auf den Hilfsnamen `…_vor_Bezugsart_Zimmer`, neue Tabelle aus dem
**geltenden** `sqlite_master`-Text mit getauschter Prüfklausel, Zeilen namentlich, `sqlite_sequence`,
Indizes und Trigger wieder, `foreign_key_check` für die Tabelle und jede Kindtabelle in der
Transaktion), danach im selben Vorgang die Nachführung. Wiederholbar; eine Tabelle mit fremder
Prüfklausel wird benannt nicht umgebaut.

**Grundschema = Schritt.** `SQL_CREATE_NUTZUNGSART` und die T3-Spalte führen `BEZUGSART_WERTE` (1..8);
der Schritt tauscht im geltenden Text allein `CHECK ("Bezugsart" IN (1,2,3,4,5,6,7))` gegen die neue
Klausel. Geprüft: Eine neu angelegte Datenbank und die nachgezogene Testdatenbank tragen Zeichen für
Zeichen denselben CREATE-Text (`TwwBezugsartSchemaTests`).

Eingetragen in `SchemaStand.Zielversion`, `SchemaMigration` der Schale (`SCHRITT_TWW_BEZUGSART_ZIMMER`,
`Schritt_TwwBezugsartZimmer`), `Werkzeuge/Testdatenbankschema`, `EPOS.Kern.Tests/TestDatenbank.cs` und
`Paketanhebung.STUFEN` (Nummer über die Konstante). Angepasst: die zwei Tests, die den Zielstand gleich
151 setzten (`SolarkollektorTemperaturenTests`, `KonditionierungCtrlTests`: jetzt „mindestens“).

## 5 Testdatenbank

```
dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- <kopie>
python3.12 Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py <kopie> --paketteil-schreiben
python3.12 Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py <kopie>
```

| Schritt | Ergebnis |
|---|---|
| Ausgang | `22b1f882…`, 71 577 600 Byte, Schemastand 151 |
| Schemawerkzeug, erster Lauf | Schritt 152: 2 Tabellen neu gebaut (9 → 9 und 12 → 12 Zeilen), 0 Paketzeilen (die Testdatenbank trägt `EIGEN`); Schemastand 152 |
| Schemawerkzeug, zweiter Lauf | 0 Tabellen, 0 Paketzeilen |
| Saatskript mit Schalter | 1 von 4 Dateien neu (Paketteil-CSV, Hotelzeile Bezugsart 3 → 8); 0 angelegt, 1 nachgeführt |
| Saatskript, zweiter Lauf / dritter mit Schalter | 0/0; 0 Dateien |
| integrity_check / foreign_key_check | ok / 0 |
| Zellvergleich gegen die Ausgangsfassung (alle Tabellen) | Schema: allein die Prüfklausel beider Tabellen; Zellen: `Tab_Applikation.SchemaVersion` 151 → 152, `Tab_TwwNutzungsart_STAMM` ID 9 `Bezugsart` 3 → 8; `sqlite_sequence` als Menge gleich (nur die Reihenfolge der Zeilen durch den Neubau) |
| LFS | neue oid `91136a7b394ddb1c1ac538340cde2297d4dc9315ffaea83979331334f7cbdaa2`, 71 577 600 Byte, Filter aktiv; Commit zeigt nur die Zeigeränderung |

**Befund Python.** Das Saatskript erzeugt die normierten Träger mit `sum()`; Python 3.12 rechnet die
Summe kompensiert, 3.11 nicht. Unter 3.11 weichen die erzeugten Träger in der letzten Stelle ab (auch
schon auf dem Stand vor A2, gemessen): Ein erster Versuch mit Python 3.11 schrieb 17 Tagesgänge und drei
Nutzungsarten in der letzten Stelle neu — darunter den Tagesgangsatz von „Wohnen groß (abgeleitet)“, den
Referenzprojekt 1045 benutzt (Einfrierregel). Dieser Stand wurde verworfen, die Kopie neu aus der
Ausgangsfassung aufgesetzt und mit Python 3.12 gesät. Das Skript bricht seither unter Python < 3.12
benannt ab; `TwwKatalogWacheTests.PythonStarten` nimmt nach `py` erst
`python3.13`/`python3.12`, dann `python3`.

Kein Referenzprojekt benutzt die Hotelzeile (1045 rechnet mit „Wohnen groß (abgeleitet)“, an der
Datenbank geprüft). Die Einfrierregel „gesäte Zapfprofil-Eingaben“ ist nicht berührt, die Basis bleibt.

## 6 Referenzlauf

`EPOS.Referenzlauf` ausdrücklich gebaut, `lauf` über alle fünfzehn Projekte der Basis
`2026-09-26_R23_KesselBereitschaft`, `vergleich` gegen sie:

| Ergebnis | Projekte |
|---|---|
| PASS | 1007, 1017, 1018, 1024, 1030, 1039, 1040, 1041, 1045, 1046, 1047, 1049 — darunter alle sieben der CI |
| FAIL, fremd | 1008 (54 Abweichungen, Pufferladung/-entladung), 1023 (21), 1042 (2, Betriebs- und Vollbenutzungsstunden der Wärmepumpe) |

Die drei FAIL sind **nicht** von A2: Derselbe Lauf auf dem Stand vor A2 (`903f926` mit der
Testdatenbank `22b1f882…`, aus einem Scratch-Export gebaut) zeigt dieselben Abweichungen, und die 460
CSV beider Läufe sind **byte-gleich** (allein `protokoll.txt` weicht ab). Die Basis ist auf Windows
gerechnet; auf dem Linux-Läufer dieser Sitzung weichen die drei Projekte außerhalb der CI-Liste ab.

## 7 Gates

(Abschnitt nach dem vollen Lauf fortgeschrieben.)

## 8 Folgen

| Nr. | Gegenstand | Wer | Wann |
|---|---|---|---|
| A2 | Sichtabnahme auf Windows: Zonenmaske mit „Hotel (aus Messung, je Zimmer)“ (Einheit „Zimmer“ am Feld), Klappliste des Katalogeditors, Importbericht eines älteren Pakets | Anwender | mit dem Merge |
| A2 | Kurzlauf des Validierungswerkzeugs mit den Hotelobjekten neu aus `norwegen.py` (Objektdateien außerhalb des Repositoriums; erwartet 3 / 0 / 18 wie #579 — Bezugsmenge und n_E bleiben) | Anwender (Windows) | nach dem Merge |
| A2 | Auslieferung: Vorlage aus einer Quelle auf Schemastand 152 bauen; ein externes Katalogpaket mit dem früheren Hotelstand wird nachgeführt gelesen | Orchestrierung | vor der nächsten Auslieferung |
| A2 | Linux-Abweichung der Projekte 1008, 1023, 1042 gegen R23 (fremd, vorbestehend) klären oder als Plattformbefund festhalten | Orchestrierung | offen |
| Wiki | Logbuch-Satz zum Sammel-Upload | Orchestrierung | mit dem Upload |

**Entwurf Logbuch-Satz:** „Die Nutzungsart ‚Hotel (aus Messung, je Zimmer)‘ rechnet mit der neuen
Bezugsart ‚Zimmer‘; die Zonenmaske nennt ‚Zimmer‘ als Einheit der Bezugsgröße.“

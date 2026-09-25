# Folgeposten ZU20 und ZU24 — Zapfprofilgenerator (Protokoll, 25.09.2026)

Statuszeile #504 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Nachtrag
**N17** und Kapitel 9 (ZU20, ZU24) im
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md);
Abschnitt 14 der
[Übergabe](../../../aktuell/Zapfprofilgenerator/2026-09-23_Uebergabe_Zapfprofilgenerator.md);
Entscheide im Nachtrag N16, Vorstufe im Protokoll [Z5](2026-09-25_Z5_Kalibrierung.md). Zweig `zu`
von `fec09538`, zwei eigene Commits `c90990e2` und `8b822f98`, Papiere `3184e857`, Merges von
`origin` (`99815b47` in `e4b8d5b8`, `b5cc1a61` in `3bf3a59f`, `b0cff527` in `fc787fa4`).
Die Testdatenbank stand in den ersten drei origin-Ständen unverändert auf der Fassung von
`1009286e` (Schemaschritt 141); der vierte Merge (`f52d38ec`, #505) brachte **Schemaschritt 142**,
und die Fassung dieses Postens ist daraus neu gesät. **Dieser Posten selbst bringt keinen
Schemaschritt** — er ändert nur Katalogdaten. Alle Gates im Worktree, ohne CI-Lauf bis zum Push.

## Auftrag

Zwei Folgeposten der Anwenderentscheide vom 25.09.2026 (N16): **ZU20** die nach ZU19 abgeleiteten
VDI-6002-Katalogtypen in die Auslieferung heben, mit Herkunftsvermerk im Katalog, CSV-Träger im
freien Paketteil, Skript, Wache und Testdatenbank in einem Schritt; **ZU24** eine Paketvorlage für
die Nichtwohn-Typen des Beiblatts A100 der DIN EN 12831-3 **ohne Werte** samt Anleitung, Test und
Wiki-Absatz. Keine Normzahl ins Repositorium, kein Schemaschritt, kein Push.

## Was entstanden ist

| Posten | Inhalt | Commit |
|---|---|---|
| ZU24 | `Referenzlaeufe/Katalogpaket_Vorlage_A100/` mit den vier Dateien des Importformats und je einer Platzhalterzeile, `LIESMICH.md` mit Anleitung und Typnamenliste; Wiki-Quelle Brauchwasser-Zapfprofil am Anker `katalog-import` (Steuerspalte `Gruppe`, Paketvorlage) | `c90990e2` |
| ZU20 | drei Träger im freien Paketteil (Tagesgangsatz 4, Tagesgang 16, Nutzungsart 5 Zeilen), `tww_testkatalog_fiktiv.py` erzeugt und prüft sie (`--paketteil-schreiben`), `TwwKataloge` spielt sie ein, Testdatenbank neu gesät, Wachen und `Auslieferungsvorlage.Tests` (T13) nachgezogen, `Katalogpaket_frei/LIESMICH.md` fortgeschrieben; dazu die zwei Fälle der A100-Vorlage in `TwwKatalogimportTests` (dieselbe Datei) | `8b822f98` |
| Papiere | N17, dieses Protokoll, Statuszeile #504, Übergabe Abschnitt 14, Indexzeilen in `Dokumentation/LIESMICH.md` und `Referenzlaeufe/LIESMICH.md` | Folgecommit |

## Die Entscheidung zur Herkunftsart

N16 hatte für die abgeleiteten Zeilen `Herkunftsart FREI` vorgeschlagen — umgesetzt ist
**`VERFAHREN`**. `FREI` heißt nach der Definition der Wertemenge
(`EPOS.Kern/Allgemein/Zapfprofil/Provenienz.cs`) „frei verfügbare Quelle"; VDI 6002 ist keine, und
die Zeile trüge damit eine falsche Aussage über die Richtlinie — sichtbar im Katalogdialog, im
Bericht und in der KiSicht. `VERFAHREN` heißt „aus einem Verfahren gerechnet", und genau daher kommt
der Wert: aus der Ableitungsregel von `Referenzlaeufe/Skripte/normzahlen_abgeleitet_bauen.py`. Die
Quelle nennt die Herkunft im Klartext („abgeleitet aus VDI 6002 Blatt 1" bzw. „… Blatt 2"), die
Spalte `Ausgabe` den Stand `2014-03`. Folge: Regel 1 des freien Paketteils lässt jetzt zwei
Herkunftsarten zu (`TwwKataloge.PAKETTEIL_HERKUNFT` = `FREI`, `VERFAHREN`); jede Prüfung, jeder
Bericht und die Wachen kennen beide. `FIKTIV` tragen nur noch die drei „Testnutzung A/B/C (fiktiv)".

## Abweichungen vom Auftrag

- **Keine Bild- oder Tabellenangabe im Quellentext.** Der Auftrag nannte „… Blatt n, Bild/Tabelle
  …". Solche Fundstellen liegen nicht vor: Das Ableitungsskript übernimmt Seiten- und
  Tabellenverweise der Quelle bewusst nicht. Der Text nennt Richtlinie und Blatt.
- **Keine Ergänzung von `Tab_TwwZapfkategorie_STAMM.csv`.** Die beiden Vorgabesätze binden über die
  Steuerspalte `Gruppe` von selbst an jede neue Nutzungsart ihrer Gruppe (drei mal Wohnen mit vier,
  zwei mal Nichtwohnen mit zwei Kategorien) — eine eigene Kategoriezeile je abgeleitetem Typ wäre
  eine Dopplung.
- **Herkunftsart `VERFAHREN`** statt des in N16 vorgeschlagenen `FREI` (Begründung oben).
- **Die Bedarfsplatzhalter der A100-Vorlage** stehen in kWh je Einheit und Tag (10/20/30), nicht in
  Litern: Die Spalte führt kWh. Die Werte liegen bewusst außerhalb jeder plausiblen Spanne, damit
  eine unausgefüllte Vorlage auffällt.
- **`Katalogversion` in der A100-Vorlage gesetzt** (`A100-1`): Im Importformat ist sie Pflichtspalte
  (`TwwNutzungsartCtrl.Import`), anders als im freien Paketteil, der keine führt.

## Testdatenbank

Neu gesät aus der origin-Fassung (`git checkout origin/ios_migration_september --
Referenzlaeufe/Kenndaten_Test.sqlite`, dann das Einspielskript). Zellvergleich gegen vorher über
alle 145 Tabellen und 10 507 032 Zellen: **93 Zellen verschieden**, alle in zwei Tabellen —
45 in `Tab_TwwNutzungsart_STAMM` (fünf Zeilen × drei Provenienzgruppen × Quelle, Version,
Herkunftsart), 48 in `Tab_TwwTagesgang_STAMM` (sechzehn Zeilen × drei Spalten). Zeilenzahlen
unverändert: 5 Tagesgangsätze, 20 Tagesgänge, 8 Nutzungsarten, 24 Zapfkategorien, 4 Bedarfstage,
33 Ereignisse, 85 Parameter, 5 DIN-4708-Werte; Status `AUSLIEFERUNG`/`IMPORT` 0 Zeilen;
`integrity_check` ok, `foreign_key_check` leer, Schemastand 141. Zweiter Skriptlauf 0/0, dritter
ebenso. Committet mit aktivem LFS-Filter, keine `-shm`/`-wal`.

## Gates

| Stand | Kern-Build | Tests | Weiteres |
|---|---|---|---|
| vor dem Merge (`8b822f98`) | 0 Fehler | 14 020 grün (2 übersprungen): Kern 6 821, UI 6 237, KiKern 549, Engine 386, Planung 27 | SqlDialektPruefer 1 921/0; Auslieferungsvorlage 35/35; Referenzlauf 5/5 PASS gegen R14 (160 Dateien, 1 805 429 Werte) |
| nach dem Merge `99815b47` (`e4b8d5b8`) | 0 Fehler | 14 033 grün (2 übersprungen): Kern 6 828, UI 6 243, KiKern 549, Engine 386, Planung 27 | SqlDialektPruefer 1 921/0; Auslieferungsvorlage 35/35; Referenzlauf 5/5 PASS gegen R14 (160 Dateien, 1 805 429 Werte); Einspielskript 0/0 |
| nach dem Merge `b5cc1a61` (`3bf3a59f`) | 0 Fehler | 14 062 grün (2 übersprungen): Kern 6 857, UI 6 243, KiKern 549, Engine 386, Planung 27 | SqlDialektPruefer 1 921/0; Auslieferungsvorlage 35/35; Referenzlauf 5/5 PASS gegen R14 (160 Dateien, 1 805 429 Werte); Einspielskript 0/0; keine Konfliktmarker |
| nach dem Merge `b0cff527` (`fc787fa4`) | 0 Fehler | 14 085 grün (2 übersprungen): Kern 6 880, UI 6 243, KiKern 549, Engine 386, Planung 27 | SqlDialektPruefer 1 919/0; Auslieferungsvorlage 35/35; Referenzlauf 5/5 PASS gegen R14 (160 Dateien, 1 805 429 Werte); Einspielskript 0/0; keine Konfliktmarker |
| Abschluss nach der Nachbesserung und dem Merge `f52d38ec` (`919f2ba7`, Schemastand 142) | 0 Fehler; Windows-Schale Debug x64 0 Fehler (der Merge brachte `SchemaMigration.cs`) | 14 093 grün (2 übersprungen): Kern 6 888, UI 6 243, KiKern 549, Engine 386, Planung 27 | SqlDialektPruefer 1 913/0; Auslieferungsvorlage 35/35; Referenzlauf 5/5 PASS gegen R14 (160 Dateien, 1 805 429 Werte); Einspielskript 0/0 (Zellvergleich 93 von 10 506 856); keine Konfliktmarker |

Die Referenzprojekte nutzen den Generator nicht — der Lauf war wie erwartet unverändert. Die
Wächter der Papiere (Doku-Link-, Wiki-Produktdaten-, Repository-Ordnungswache) liefen im vollen
Testlauf mit.

## Folgen

- **ZU21 bleibt offen und betrifft denselben Ordner.** Die neuen Zeilen liegen im freien Paketteil,
  den ZU21 bis zur fachlichen Durchsicht zurückhält. Das Werkzeug spielt den Ordner immer ein; die
  Zurückhaltung ist eine Sache des Anwenders vor der ersten Auslieferung, keine Codeschaltung. Die
  Prüfliste ZU21 betrifft die Setzungen, nicht die abgeleiteten Werte.
- **Sichtabnahme unter Windows:** Der Katalogdialog soll die fünf Typen „… (abgeleitet)" mit
  Herkunft „Verfahren", Quelle „abgeleitet aus VDI 6002 Blatt n" und Stand „Auslieferung" zeigen;
  der Katalogimport soll die A100-Vorlage ohne Ablehnung einspielen.
- **Logbuch:** zwei Sätze vorgeschlagen (Statuszeile #504, N17 Folgen-Tabelle), Version beim
  Anwender zu erfragen.
- Offen bleiben K5 (Messreihen), ZU7 (Referenzprojekt und vierte Einfrierregel) und der Prüfposten
  gegen ein Beispielprojekt mit `Typtage_Aktiv = 1` bei leerer `Tab_TwwTyptag_IMPORT`.

## Gegenprüfung und Nachbesserung

Auf dem Stand `bfa27fb9` hat eine Gegenprüfung des Postens **neun Befunde** ergeben — einer hoch,
vier mittel, zwei gering, zwei zur Kenntnis. Nachgebessert in `5606a566` (Auslieferung) und
`e997ec0a` (Wachen und Wortlaut), die Papiere im Folgecommit.

| Nr. | Gewicht | Befund | Ergebnis |
|---|---|---|---|
| 1 | **hoch** | Die Paketvorlage lag nur im Repositorium: `Setup/EPOS-Plan.iss` nahm sie nicht mit, der Anwender hätte sie nie in Händen gehalten | behoben (`5606a566`): `#define KatalogVorlageA100` samt `DirExists`-Prüfung wie bei den Herstellerdaten, `[Files]` nach `{app}\Vorlage\Katalogpaket_A100` (`Components: programm`, `ignoreversion`); `[UninstallDelete]` deckt den Ordner schon über `{app}\Vorlage` ab. Setup-Konzept (Rechtetabelle, `[Files]`-Zeile, Repo-Baum) und Wiki-Absatz nennen den Ablageort. **Der Installer bleibt ungetestet** — der Setup-Lauf der CI läuft nur auf Zuruf und wurde nicht ausgelöst; Sichtabnahme beim nächsten Setup-Lauf |
| 2 | mittel | Die Platzhalterwache trennte fest bei `;`, prüfte nur Zahlen und hätte eine Datei ohne ein einziges Zahlenfeld als grün gemeldet | behoben (`e997ec0a`): Trenner aus der Kopfzeile wie der Leser (`;`, sonst `,`), Fund bei einer Datei ohne Zahlenfeld, zusätzlich Textprüfung — jede Ziffer in einem Textfeld ist ein Fund außer in der Freiliste (`A100-1` und der Quellentext ohne Tabellennummer). Neuer Gegenfall über eine temporäre Kopie: eine fremde Zahl (37) **und** eine Tabellennummer im Quellentext werden gemeldet |
| 3 | mittel | Der Katalogimport setzt `VERFAHREN` → `IMPORT`, die Auslieferungsvorlage lässt `FREI`/`VERFAHREN` stehen | **so belassen** (entschieden): Ein eingespieltes Paket ist ein Anwenderimport und trägt das auch; die ausgelieferten Zeilen des Paketteils laufen nie durch den Import. Festgehalten in N17 (d) |
| 4 | mittel | N17 hatte keine Folgen-Tabelle, die Logbuch-Sätze standen nur in der Statuszeile | behoben: Folgen-Tabelle in N17 nach dem Muster von N9/N13/N14/N15, mit den Zeilen Setup, ZU21, **Logbuch** (beide Sätze), Wiki und Sicht |
| 5 | mittel (Kenntnis) | Ableitungsregel und Liste der Abweichungen stehen offen im Kopf von `normzahlen_abgeleitet_bauen.py`; mit ZU20 reicht die Rückrechenbarkeit von der Testdatenbank in jede Auslieferung | keine Codeänderung: Das ist die Bedingung von ZU19 (reproduzierbar **und** rückrechenbar), nicht ihr Versehen. Absatz Rückrechenbarkeit in N17 (e) |
| 6 | gering | `LIKE 'abgeleitet aus VDI 6002 Blatt _'` trägt mit dem Unterstrich nur ein Zeichen | behoben (`e997ec0a`): `Blatt %` in `TwwKatalogWacheTests` (4 Stellen) und `Auslieferungsvorlage.Tests` T13 (1 Stelle) |
| 7 | gering | `Katalogpaket_Vorlage_A100/LIESMICH.md` nannte die Quelle mit „NA.", die CSV führt sie ohne | behoben (`e997ec0a`): Wortlaut an die CSV angeglichen |
| 8 | Kenntnis | Die drei Träger des freien Paketteils sind byte-gleich mit dem Erzeugnis, der Prüflauf bricht hart ab | nichts zu tun — so gewollt |
| 9 | Kenntnis | `TwwKataloge.Gleich` ist nach `Bereinigen` unerreichbar | nichts zu tun — kein Fehler, nur ein toter Zweig einer Vorprüfung |

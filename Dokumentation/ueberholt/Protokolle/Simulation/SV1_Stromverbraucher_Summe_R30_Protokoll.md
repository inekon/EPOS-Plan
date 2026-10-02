# SV1 — Stromverbraucher-Zuordnung über die ID: gepflegte Jahressumme, Kopie nur im eigenen Projekt, Basis R30

Stand: 02.10.2026 · Zweig `ios_migration_september` · Opus-Agenten im Worktree: Behebung und Basis am 30.09.2026 auf
dem Parkzweig `stromverbraucher-summe` ab `28778e80` (Merge #632–#634, Schemastand 158, Basis R29), Abschluss auf
`sv1-abschluss`. **Kein Schemaschritt, keine Änderung der Testdatenbank** (`5d59041f…`). Der offene Punkt K1‑O1
stammt aus [`K1_Dreikanal_Protokoll.md`](K1_Dreikanal_Protokoll.md); Vorgänger in diesem Ordner
[`SK8_Kessel_Kennlinie_Abschluss_Protokoll.md`](SK8_Kessel_Kennlinie_Abschluss_Protokoll.md).

Commits:
- `d88acb44` Kern (Zuordnung über die ID, Projektfilter), Controller, Tests, Regel in `EPOS.Kern/CLAUDE.md`
- `ed8d3b5b` Basis R30 eingefroren, `Referenzlaeufe/LIESMICH.md` fortgeschrieben
- `1b240bc5` Merge `origin/ios_migration_september` (`4cfbf017`), konfliktfrei
- `7c89b7df` R29 aus dem Arbeitsbaum, ihr Protokoll nach `Dokumentation/ueberholt/Referenzbasen/`
- `68b3501a` Basisname R30 in `CLAUDE.md`, `kern.yml`, `ios.yml`, `Werkzeuge/Gate/LIESMICH.md` und den Konzepten
- dieses Protokoll samt Indexzeile im Commit danach

## 1 Auftrag und Mangel

Anwenderentscheid vom 30.09.2026, „Stromverbraucher-Mängel beheben, mit neuer Referenzbasis“ (Auftrag SV1). Zwei
Mängel, eine Ursache: Der Stromzweig löste die Zuordnung Projekt ↔ Stromverbraucher über Namen auf.

- **Mangel (a) — die Jahressumme wurde überlesen.** `Z_Projekt_Stromverbraucher.Summe` wurde über den Bezeichner der
  Projektkopie gesucht; die Zuordnungszeile trägt aber den Namen, unter dem sie angelegt wurde, vielfach den
  Katalognamen. In 1017 und 1047 heißt die Kopie „EFH_3_Pers (P1017)“, die Zeile „EFH_3_Pers“ mit der Summe
  15 MWh/a. Die gepflegte Summe griff nicht; gerechnet wurde das volle Profil der Kopie, 672 MWh/a — die Summe ihrer
  zwölf Monatswerte.
- **Mangel (b) — eine fremde Projektkopie konnte gelten.** Kopf- und Typsatz wurden ohne Projektfilter über den Namen
  gelesen; bei gleichnamigen Kopien zweier Projekte galt die erste der Tabelle. In der Testdatenbank rechneten 1024
  und 1040 bis 1045 mit Kopien von 1023, 1046 mit der von 1007 und 1047 mit der von 1017 — zeichengleich zu den
  eigenen, deshalb ohne Wirkung auf die Zahlen. Das ist der offene Punkt K1‑O1 („Projektfilter: Einzelfix offen“); er
  ist damit geschlossen.

## 2 Ursache

Der Lauf holte die Namen seiner Profile aus der Sicht `Abfrage_Monatsstrom`, einem Verbund über
`Z_Projekt_Stromverbraucher.ID_Stromverbraucher`: Die ID war bekannt, weitergereicht wurde nur der Name der Kopie.
Danach ging alles über diesen Namen:

1. **Kopfsatz:** `Tab_Stromverbraucher` über `Bezeichner` ohne `ID_Projekt` — `ProfilQuelle.Strom` setzte
   `ProjektfilterAktiv = false`, anders als Brauchwasser und Prozesswärme. Es galt die erste gleichnamige Zeile der
   Tabelle: Mangel (b).
2. **Jahressumme:** `ProjektJahressumme` las `Z_Projekt_Stromverbraucher` mit `ID_Projekt = ? AND Bezeichner = ?` und
   dem Namen der Kopie. Trägt die Zeile einen anderen Namen, gibt es keinen Treffer, die Summe ist 0, und 0 skaliert
   nicht: Mangel (a).
3. **Wochenprofil:** `Tab_Stromverbrauchertyp` über den Typnamen, ebenfalls ohne Projektfilter.

Dieselbe Namenslesung stand im Sichern der Jahressumme (`Z_ProjektStromverbraucherCtrl.UpdateSumme` suchte die Zeile
über ihren Bezeichner; mit dem Namen der umbenannten Kopie „EFH_3_Pers (P1017)“ traf es keine), im Assistenten und im
Komponentenbestand (beide lasen den Bezeichner der Zuordnungszeile über `ReadAll(sql)` mit zusammengesetztem
SQL-Text).

**Datenlage der Testdatenbank:** Alle 24 Zuordnungszeilen zeigen auf eine Kopie ihres eigenen Projekts; acht tragen
einen anderen Namen als ihre Kopie (1006, 1008 und 1032 je zwei, 1017 und 1047 je eine). Wirksam war Mangel (a)
allein in 1017 und 1047: gepflegt 15, Kopie 672 MWh/a. In 1006, 1008 und 1032 gleicht die gepflegte Summe der Summe
der Kopie (48,5 bzw. 365 MWh/a); von ihnen ist allein 1008 Referenzprojekt.

## 3 Behebung

- **`ProfilQuelle`** (`EPOS.Kern/Allgemein/Simulation/ProfilBedarf.cs`) führt zwei neue Felder, gesetzt allein beim
  Stromverbraucher: `ZuordnungIdSpalte` (`Z_Projekt_Stromverbraucher.ID_Stromverbraucher`, die ID der Projektkopie)
  und `TypKopfIdSpalte` (`Tab_Stromverbrauchertyp.ID_Stromverbraucher`, nur auf den Projektkopien).
  `ProjektfilterAktiv` gilt jetzt auch auf den Projektkopien des Stromverbrauchers; im Katalog entfällt er, die
  `_STAMM`-Tabellen tragen kein `ID_Projekt`.
- **Der Lauf rechnet je Zuordnungszeile** (`ProfilBedarf.Rechnen` mit `ZuordnungenLesen`): die Zeilen des Projekts in
  der Reihenfolge ihrer ID, je Zeile der Kopfsatz über die ID mit Projektfilter (`KopfLesenUeberId`) und die
  Jahressumme aus derselben Zeile. Zwei Zeilen auf dieselbe Kopie rechnen zweimal, jede mit ihrer Summe — so, wie die
  Namensabfrage sie lieferte. Zeigt eine Zeile auf die Kopie eines anderen Projekts, wird sie nicht gelesen: Anteil 0
  und eine Warnung im Laufprotokoll. Meldungen nennen die Projektkopie, nicht den Bezeichner der Zeile.
- **Die Vorschau** (Namen aus dem Dialog): `KopfLesen` nimmt unter gleichnamigen Kopien desselben Projekts zuerst die,
  auf die eine Zuordnungszeile zeigt; `ProjektJahressumme` sucht die Summe über die ID dieses Kopfsatzes. Ein
  Katalogsatz (Rückfall der Projektvorschau) hat keine Zuordnungszeile, dort gilt allein die Vorgabe des Dialogs.
  Vorschau und Lauf zeigen dieselbe Zahl.
- **Das Wochenprofil** (`WochenprofilLesen`) kommt zuerst aus der Typzeile, die zu genau diesem Kopfsatz kopiert
  wurde; erst ohne sie gilt die gleichnamige Typzeile desselben Projekts.
- **Das Sichern der Jahressumme** (`Z_ProjektStromverbraucherCtrl.UpdateSumme`) ändert die Zeilen des Projekts, die per
  `ID_Stromverbraucher` auf die Kopie mit dem Namen zeigen, den der Dialog zeigt.
- **Assistent und Komponentenbestand** (`AssistentCtrl`, `KomponentenBestandCtrl`) lesen die Zuordnung über
  `Z_ProjektStromverbraucherCtrl.LiesProjekt`: Der Name je Zeile ist der der Projektkopie, wie auf der Kachel der
  Startseite.
- **Das Speichern** (`WizardCtrl.Add_Projekt_Stromverbraucher`) bleibt bei der zugeordneten Kopie: Zeigt eine Zeile
  schon auf eine Kopie dieses Projekts mit ihrem Namen, gilt genau diese
  (`StromverbraucherStammCtrl.GetProjektIdUeberId` — ID, Projekt und Name müssen passen, weil eine neu aufgenommene
  Zeile die Katalog-Id trägt); erst sonst wird über den Namen gesucht oder aus dem Katalog kopiert.
- **Entfernt:** `Z_ProjektStromverbraucherCtrl.ReadAll(sql)` und die tote Klasse `StromverbraucherCtrl`
  (`EPOS.Kern/Controller/StromverbraucherCtrl .cs`, Namenslesung ohne Projekt).
- **Regel** in `EPOS.Kern/CLAUDE.md`: Die Zuordnung Projekt ↔ Stromverbraucher gilt über die ID, nicht über den
  Bezeichner; Kopf- wie Typsatz werden nur im eigenen Projekt gelesen.
- **Kein Schemaschritt:** `ID_Stromverbraucher` stand schon in `Z_Projekt_Stromverbraucher` und
  `Tab_Stromverbrauchertyp`.

## 4 Tests

- **`EPOS.Kern.Tests/StromverbraucherZuordnungTests`** (neu, sieben Fälle, je Fall eine eigene Arbeitskopie der
  Testdatenbank):
  1. Die Jahressumme greift über die ID auch bei umbenannter Kopie: 1017 rechnet 15 000 kWh statt 672 000; Kopie
     umbenannt und Summe auf 20 gesetzt, rechnet der Lauf 20 000 kWh.
  2. Die Vorschau findet dieselbe Summe über die ID ihres Kopfsatzes; Vorschau und Lauf sind gleich.
  3. `UpdateSumme` trifft die Zeile über die Projektkopie; die Zeile von 1047 bleibt bei 15.
  4. Die gleichnamige Kopie „test“ von 1007 wird für 1046 nicht gelesen: Monatswerte und Wochenprofil der fremden Kopie
     verzogen, Lauf und Vorschau von 1046 unverändert; Gegenprobe: 1007 selbst rechnet anders.
  5. Eine Zuordnung auf die Kopie eines anderen Projekts rechnet nicht: Anteil 0, je Zeile eine Warnung.
  6. Das Speichern bleibt bei der zugeordneten Kopie (1043 führt zwei Kopien „EFH_3_Pers“, die Zeile zeigt auf die
     zweite); `GetProjektIdUeberId` verlangt passenden Namen und passendes Projekt.
  7. Assistent und Komponentenbestand nennen die Projektkopie „EFH_3_Pers (P1017)“ mit ID 1017103 und Summe 15.
- **`EPOS.Kern.Tests/BedarfsProfilVorschauTests`:** zwei gepinnte Werte auf die gepflegte Summe nachgezogen — Januar
  67,462 → 1,506 MWh (= 67,462 × 15/672), Jahresstrom des Gebäudes 672 → 15 MWh.

## 5 Basis R30 und A/B gegen R29

`Referenzlaeufe/2026-09-30_R30_Stromverbraucher/` — sechzehn Projekte, 487 CSV, **3 080 Skalare**, am 30.09.2026 auf
Linux eingefroren gegen die Testdatenbank `5d59041f…` (Schemastand 158, unverändert). Zweiter Lauf 487/487
byte-gleich; gestört (`--stoerung ulp`) gegen ungestört 16/16 PASS, 480/487 byte-gleich — dieselben sieben Dateien wie
mit R29. Die sieben CI-Projekte gegen R30: GESAMT PASS.

A/B gegen R29 (beide auf Linux): **14/16 PASS**, 477/487 CSV byte-gleich. Abgewichen sind allein 1017 und 1047, je
fünf Dateien (`aggregate.csv`, `strombedarf_viertelstunde.csv`, `reststrom_viertelstunde.csv`,
`ssp_gespeichert_viertelstunde.csv`, `pv_speicherfuellstand.csv`) und je 13 Skalare. 1008 trägt dieselbe
Namensabweichung, aber die Summe der Kopie — byte-gleich.

| Größe | 1017 R29 → R30 | 1047 R29 → R30 |
|---|---|---|
| Strombedarf gesamt MWh/a (`Energiebedarf.Strombedarf_Gesamt`) | 672 → 15 | 672 → 15 |
| größter Strombedarf kW (`Energiebedarf.Strombedarf_Max`) | 312,61 → 6,98 | 312,61 → 6,98 |
| Netzbezug MWh/a (`Energiebedarf.Stromrestbedarf`) | 655,88 → 11,44 | 641,18 → 6,80 |
| Strombedarfsdeckung des BHKW % | 5,31 → 55,15 | 5,30 → 56,20 |
| Reststrom nach dem BHKW MWh/a (`BHKW.Reststrombedarf`) | 635,20 → 7,35 | 636,13 → 7,12 |
| Reststrom nach dem Kessel MWh/a (`Heizkessel.Reststrombedarf`) | 655,32 → 27,47 | 641,19 → 9,25 |
| Stromspeicher, Summe der Reihe `ssp_gespeichert_viertelstunde` | 44 862 → 181 256 | 44 862 → 228 056 |

**Unverändert** bleiben Wärmepumpenstrom, BHKW-Strom und Elektrokessel — Wärmeseite und Erzeugerfahrplan hängen nicht am
Haushaltsstrom. **Plausibel:** 672 MWh/a waren für ein Einfamilienhausprofil nie ein Bedarf, sondern die ungeskalierte
Summe der Kopie. Mit den gepflegten 15 MWh/a deckt das BHKW mit rund 36 MWh/a Strom gut die Hälfte des Bedarfs
gleichzeitig; der Überschuss lädt den Stromspeicher, der Netzbezug fällt auf den Rest. **Gegenprobe:** Die Summe der
Viertelstundenreihe `strombedarf_viertelstunde.csv` ist in beiden Projekten 60 000 = 15 MWh × 4 000 (vorher
2 688 000 = 672 MWh × 4 000). Die Tafel ist aus `aggregate.csv` und `ssp_gespeichert_viertelstunde.csv` beider Basen
nachgerechnet (R29 aus der Git-Geschichte).

**Archiv und Basisname:** R29 per `git rm` aus dem Arbeitsbaum, ihr `protokoll.txt` per `git mv` nach
`Dokumentation/ueberholt/Referenzbasen/2026-09-30_R29_Kesseltakten/`; im
[Wegweiser der Referenzbasen](../../Referenzbasen/LIESMICH.md) Tabellenzeile, Zähler (47 Basen, 48 Dateien) und
Abschnitt „Die Basis R29 im Einzelnen“, Link der lauffähigen Basis und Basenhistorie auf R30. Basisname R30 in
`CLAUDE.md` (Regressionsnetz, dazu: 1017 und 1047 rechnen ihren Strombedarf mit der gepflegten Jahressumme),
`kern.yml` (acht Stellen, Kommentar R30, Plattformnachweis), `ios.yml` (zwei Stellen), `Werkzeuge/Gate/LIESMICH.md`,
im Index und in den Konzepten Gebäudesimulation VDI 6007, Simulationsablauf, Systementwurf Gebäudesimulation,
Zapfprofilgenerator und Wirtschaftlichkeit (Kopfzeile und Ankertafel). Bei R29 bleiben die Geschichte unter
`ueberholt/`, die Statuszeilen, die Übergaben vom 30.09.2026 und die Recherchen, die gegen R29 gemessen haben.

## 6 Einfrierbegründung

Neu eingefroren, weil sich der Rechenweg des Stromzweigs gewollt ändert (Mangel (a)) und die Abnahme der Vergleich
gegen die Basis ist: Die Abweichung von 1017 und 1047 ist der Zweck der Behebung, begründet in
`Referenzlaeufe/LIESMICH.md` (Abschnitt „Aktuelle Basis“). Keine bestehende Einfrierregel ist berührt — die
Testdatenbank ist byte-gleich, kein gesäter Wert hat sich geändert; gewechselt hat allein, welcher gesäte Wert gelesen
wird: die gepflegte Jahressumme der Zuordnungszeile statt der Summe der Kopie. Mangel (b) ändert keine Zahl der Basis.

## 7 Gate

Gate auf dem Merge-Stand: siehe Statuszeile der Hauptsitzung.

## 8 Wiki und Logbuch

Keine Repo-Quelle unter `Projekte/Wiki/` beschreibt die Zuordnung der Stromverbraucher über den Namen; die Seite
„Stromverbraucher“ hat keine Quelle im Repositorium. Logbuch (Vorschlag, Version beim Anwender zu erfragen): „Die
gepflegte Jahressumme eines Stromverbrauchers gilt auch dann, wenn seine Projektkopie anders heißt als der
Katalogeintrag.“

## 9 Offen (Anwenderfrage)

1. **Dieselbe ID-Regel für Brauchwasser und Prozesswärme?** `Z_Projekt_Brauchwasser` und `Z_Projekt_Prozesswaerme`
   führen mit `ID_Brauchwasser` und `ID_Prozesswaerme` dieselbe Spalte auf die Projektkopie. Dort gilt der
   Projektfilter schon, die Jahressumme wird aber weiter über den Bezeichner gesucht (`ZuordnungIdSpalte` ist nicht
   gesetzt). In der Testdatenbank trägt keine ihrer Zuordnungszeilen (19 bzw. eine) einen anderen Namen als ihre Kopie.
2. **Neue Einfrierregel „gesäte Stromverbraucherdaten“?** R30 hängt an der gepflegten Jahressumme 15 MWh/a der
   Zuordnungszeilen von 1017 und 1047; `CLAUDE.md` führt für die Stromverbraucherdaten der Referenzprojekte
   (Zuordnungszeilen, Projektkopien, Wochenprofile) bisher keine Einfrierregel.

# SV2 — Brauchwasser- und Prozesswärme-Zuordnung über die ID: gepflegte Jahressumme, Kopfsatz der Kopie, die zugeordnete unter gleichnamigen Kopien

Stand: 02.10.2026 · Opus-Agent im Worktree, Zweig `sv2-bedarf-id` ab `cce8bfd92` (Arbeitszweig
`ios_migration_september`, Basis R30). **Kein Schemaschritt, keine Änderung der Testdatenbank** (`5d59041f…`),
**keine neue Basis.** Vorbild und Vorgänger in diesem Ordner:
[`SV1_Stromverbraucher_Summe_R30_Protokoll.md`](SV1_Stromverbraucher_Summe_R30_Protokoll.md) — dort Abschnitt 9,
offene Frage 1, die der Anwender am 02.10.2026 mit „dieselbe ID-Regel wie SV1“ entschieden hat.

Commits:
- `27b168bd` Kern (Zuordnung über die ID bei Brauchwasser und Prozesswärme, Namensweg entfernt), Controller, tote
  Klassen `BrauchwasserCtrl` und `ProzesswaermeCtrl` entfernt
- `7ed5e064` Tests (`BrauchwasserZuordnungTests`, `ProzesswaermeZuordnungTests`, 1041 in
  `BedarfsProfilVorschauTests` gepinnt)
- dieses Protokoll samt Indexzeile, Regel in `EPOS.Kern/CLAUDE.md` und zwei Verweisen auf die entfernte Klasse
  (`KONTEXT_Brauchwassertypen_VDI6002.md`, `Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md`) im Commit danach

## 1 Auftrag und Mangel

Anwenderentscheid vom 02.10.2026 (Auftrag SV2): dieselbe ID-Regel wie SV1 für die beiden übrigen Bedarfsarten mit
Monatswert-plus-Wochenprofil-Struktur. `Z_Projekt_Brauchwasser.ID_Brauchwasser` und
`Z_Projekt_Prozesswaerme.ID_Prozesswaerme` zeigen auf die Projektkopie; gelesen wurde die Zuordnung trotzdem über
Namen.

- **Brauchwasser — die Jahressumme wurde überlesen.** Trägt die Zuordnungszeile einen anderen Namen als ihre Kopie
  (den, unter dem sie angelegt wurde), griff die gepflegte Summe nicht; gerechnet wurde das volle Profil der Kopie.
  Nachgespielt an 1026 (Kopie umbenannt): 0,7429 statt 5 MWh/a, in Lauf und Vorschau.
- **Prozesswärme — der Kopfsatz fehlte.** Dieselbe Namensabweichung kostete den Lauf das ganze Profil: Warnung,
  Anteil 0. Nachgespielt an 1041: „Hotel_1“ mit 30 MWh/a entfiele. Die Vorschau fand den Kopfsatz, überlas aber die
  Summe: 365 statt 30 MWh/a.
- **Gleichnamige Kopien desselben Projekts:** Kopf- und Typsatz kamen von der ersten der Tabelle, nicht von der
  zugeordneten; das Speichern wechselte ebenfalls auf die erste.

## 2 Ursache

Der Lauf holte die Namen seiner Profile aus den Sichten `Abfrage_Monatswaerme_Brauchwasser` und
`Abfrage_Monatswaerme_Prozesse` (`sql/schema/002_views.sql`), beide ein Verbund über die ID der Kopie — weitergereicht
wurde nur ein Name, und die zwei Sichten liefern nicht denselben: die Brauchwasser-Sicht
`Tab_Brauchwasser.Bezeichner` (Name der Kopie), die Prozess-Sicht `Z_Projekt_Prozesswaerme.Bezeichner` (Name der
Zuordnungszeile). Danach ging alles über den Namen:

1. **Kopfsatz** über `Bezeichner` mit Projektfilter — die erste gleichnamige Kopie des Projekts; bei der Prozesswärme
   mit dem Namen der Zuordnungszeile, also bei abweichender Kopie gar keine.
2. **Jahressumme** (`ProfilBedarf.ProjektJahressumme`) über `ID_Projekt = ? AND Bezeichner = ?` mit dem Namen der
   Kopie — ohne Treffer 0, und 0 skaliert nicht.
3. **Wochenprofil** über den Typnamen mit Projektfilter — die erste gleichnamige Typzeile des Projekts.

Dieselbe Namenslesung stand im Sichern der Jahressumme (`Z_ProjektBrauchwasserCtrl.UpdateSumme`,
`Z_ProjektProzesswaermeCtrl.UpdateSumme` über den Bezeichner der Zuordnungszeile), im Assistenten
(`AssistentCtrl.LadeProzess`) und im Komponentenbestand (beide über `ReadAll(sql)` mit zusammengesetztem SQL-Text und
dem Bezeichner der Zuordnungszeile), im Speichern (`WizardCtrl.Add_Projekt_Brauchwasser`/`Add_Projekt_Prozess` über
`CopyFromStamm` → `GetProjektId`, die erste gleichnamige Kopie; war die Kopie umbenannt und kam der alte Name aus
dem Assistenten, zog es eine neue Kopie aus dem Katalog) und in der Gleichheitsprobe des Bedarfsprofildialogs
(`Z_ProjektBrauchwasserCtrl.GleichGespeichert`). Der Projektfilter galt bei beiden Bedarfsarten schon — der Mangel (b)
aus SV1 bestand hier nicht.

**Datenlage der Testdatenbank:** 115 Brauchwasser- und 17 Prozesswärmekopien, jede mit genau einer Typzeile;
zugeordnet sind 19 bzw. eine. Alle 20 Zuordnungszeilen zeigen auf eine Kopie ihres eigenen Projekts, keine trägt einen
anderen Namen als ihre Kopie; Namens- und ID-Weg liefern für alle dieselben Werte. 1043 führt zwei Kopien
„EFH Wohnen, 1 Person“ (1933498, 1933499) mit gleichen Werten; die Zuordnung zeigt auf die zweite, der Namensweg nahm
die erste.

## 3 Behebung

- **`ProfilQuelle`** (`EPOS.Kern/Allgemein/Simulation/ProfilBedarf.cs`): `ZuordnungIdSpalte` und `TypKopfIdSpalte`
  auch bei Brauchwasser (`ID_Brauchwasser`) und Prozesswärme (`ID_Prozesswaerme`), der Typkopf nur auf den
  Projektkopien. Damit lösen alle drei Bedarfsarten die Zuordnung über die ID auf, und der SV1-Code trägt sie ohne
  Änderung: Der Lauf rechnet je Zuordnungszeile (`ZuordnungenLesen` → `KopfLesenUeberId` mit Projektfilter, Summe aus
  derselben Zeile, Wochenprofil über die ID), die Vorschau nimmt unter gleichnamigen Kopien die zugeordnete
  (`KopfLesen`) und sucht die Summe über die ID ihres Kopfsatzes. Von der Namenssicht hängt der ID-Weg nicht ab.
- **Der Namensweg entfällt**, weil keine Bedarfsart ihn mehr nimmt: `ProfilQuelle.NamenAbfrage`,
  `ProfilBedarf.NamenLesen` und die Summensuche über den Bezeichner in `ProjektJahressumme`; `ZuordnungIdSpalte` ist
  damit Pflicht. Ohne Namensliste rechnet allein die Projektrechnung. Die drei Sichten `Abfrage_Monats*` liest der Kern
  nicht mehr (sie bleiben im Schema, siehe Abschnitt 8).
- **Sichern der Jahressumme:** beide `UpdateSumme` ändern die Zeilen des Projekts, die per ID auf die Kopie mit dem
  Namen zeigen, den der Dialog zeigt — wie beim Stromverbraucher.
- **Lesen der Zuordnung:** `LiesProjekt` beider Controller in der Reihenfolge der Zuordnungs-ID (der des Laufs);
  `AssistentCtrl.LadeProzess` und `KomponentenBestandCtrl` lesen darüber, also mit dem Namen der Kopie. `ReadAll(sql)`
  samt interner Liste entfällt in beiden Controllern.
- **Speichern:** `BrauchwasserStammCtrl.GetProjektIdUeberId` und `ProzesswaermeStammCtrl.GetProjektIdUeberId` (ID,
  Projekt und Name müssen passen); `Add_Projekt_Brauchwasser` und `Add_Projekt_Prozess` bleiben bei der zugeordneten
  Kopie und suchen erst ohne sie über den Namen bzw. kopieren aus dem Katalog. `GleichGespeichert` vergleicht mit der
  Kopie, auf die das Neuanlegen schriebe.
- **Entfernt:** die toten Klassen `BrauchwasserCtrl` und `ProzesswaermeCtrl` (`EPOS.Kern/Controller/`). Nachweis:
  kein Aufruf im ganzen Repositorium (Wortsuche über alle Projekte samt `WindowsFormsApplication1`, `EPOS.UI`,
  `EPOS.UI.Daten`, `EPOS.iOS`, Werkzeuge und Tests; übrig blieben zwei Kommentare); Kern-Filter und Windows-Schale
  bauen ohne sie. Der Kommentarverweis in `KiSchreibschutz` zeigt jetzt auf `ProzesswaermeStammCtrl`; der in
  `HeizkesselCtrl.Insert` erzählt die Geschichte des behobenen `@@IDENTITY`-Fehlers und bleibt.
- **Regel** in `EPOS.Kern/CLAUDE.md`: Die Zuordnung Projekt ↔ Bedarfsprofil gilt bei allen drei Bedarfsarten über die
  ID; unter gleichnamigen Kopien desselben Projekts gilt die zugeordnete.
- **Kein Schemaschritt:** die ID-Spalten standen schon in den Zuordnungs- und Typtabellen. Keine Signatur der Schale
  ändert sich (`BedarfsProfileHuelle` ruft `UpdateSumme` weiter mit dem Namen der Kopie).

## 4 Tests

- **`EPOS.Kern.Tests/BrauchwasserZuordnungTests`** (neu, sieben Fälle, je Fall eine eigene Arbeitskopie):
  1. Die Jahressumme greift über die ID bei umbenannter Kopie: 1026 rechnet 5 000 statt 742,9 kWh, der volle
     Wärmebedarfslauf weist 5 MWh aus; Summe auf 7,5 gesetzt, rechnet der Lauf 7 500 kWh.
  2. Die Vorschau findet dieselbe Summe über die ID ihres Kopfsatzes; ihre Stundenreihe gleicht der des Laufs.
  3. `UpdateSumme` trifft die Zeile über die Projektkopie; der Name der Zuordnungszeile trifft nichts mehr, 1027 bleibt
     bei 5.
  4. Unter den zwei gleichnamigen Kopien von 1043 gilt die zugeordnete: die erste verzogen, Lauf und Vorschau
     unverändert; Gegenprobe: die zugeordnete verzogen, der Lauf ändert sich.
  5. Speichern und Gleichheitsprobe bleiben bei der zugeordneten Kopie; `GetProjektIdUeberId` verlangt passenden
     Namen und passendes Projekt.
  6. Nie die Kopie eines anderen Projekts: die gleichnamige erste Kopie der Tabelle (1026) verzogen, 1027 unverändert;
     zeigt die Zuordnung von 1027 per ID auf die Kopie von 1026, rechnet sie nicht — Anteil 0 und eine Warnung, statt
     still die eigene gleichnamige Kopie zu nehmen.
  7. Der Komponentenbestand nennt die Projektkopie.
- **`EPOS.Kern.Tests/ProzesswaermeZuordnungTests`** (neu, sieben Fälle):
  1. Der Kopfsatz kommt über die ID bei abweichendem Namen — Kopie umbenannt wie Zuordnungszeile umbenannt: 30 000 kWh
     ohne Warnung, der volle Lauf weist 30 MWh aus.
  2. Die Vorschau rechnet mit der gepflegten Summe (30 statt 365 MWh/a); ihre Stundenreihe gleicht der des Laufs.
  3. `UpdateSumme` über die Projektkopie.
  4. Eine gleichnamige zweite Kopie „Hotel_1“ mit kleinerer ID und verzogenen Werten bleibt in Lauf und Vorschau
     ungelesen.
  5. Das Speichern bleibt neben ihr bei der zugeordneten Kopie.
  6. Eine Zuordnung auf die Kopie eines anderen Projekts (1023) rechnet nicht: Anteil 0, eine Warnung.
  7. Assistent und Komponentenbestand nennen die Kopie; das Speichern des Assistenten bleibt bei ihr und zieht keine
     neue aus dem Katalog.
- **`EPOS.Kern.Tests/BedarfsProfilVorschauTests`:** der Fall „nur im Projekt bekanntes Profil“ (1041, Kopie
  umbenannt) prüft statt `> 0` jetzt 30 000 kWh, 30 MWh und Januar 2,548 MWh (= 31 × 30/365).
- **Gegenprobe:** Gegen den Rechenweg von `cce8bfd92` sind alle 15 Fälle rot, jeder aus dem erwarteten Grund
  (742,9 statt 5 000 kWh, 0 statt 30 000 kWh, 365 statt 30 MWh, Kopie 11 statt 13, unveränderte Summe).
- Testfilter des Auftrags (Zuordnung, Bedarfsprofil, Brauchwasser, Prozess, Stromverbraucher, Assistent, Komponenten,
  Wizard) über `WP-Plan.Kern.slnf`: grün — `EPOS.Kern.Tests` 354, `EPOS.UI.Tests` 442, `KiKern.Tests` 1,
  `SpeicherEngine.Tests` 1.

## 5 Referenzlauf

Erwartet und erreicht: byte-gleich, keine neue Basis. Die sieben CI-Projekte gegen
`Referenzlaeufe/2026-09-30_R30_Stromverbraucher`: **GESAMT PASS**, 226/226 CSV byte-gleich. Weil 1026, 1041 und 1043
nicht in der CI-Auswahl stehen, zusätzlich gerechnet: gegen einen Lauf desselben Rechners auf `cce8bfd92` PASS,
100/100 CSV byte-gleich; 1041 auch gegen R30 PASS, 29/29 byte-gleich (1026 und 1043 führt die Basis nicht). Keine
Einfrierregel ist berührt.

## 6 Gate

Gate auf dem Merge-Stand: siehe Statuszeile der Hauptsitzung.

## 7 Wiki und Logbuch

Keine Repo-Quelle unter `Projekte/Wiki/` beschreibt die Zuordnung von Brauchwasser oder Prozesswärme über den Namen;
die Seiten „Brauchwasser“ und „Prozesswärme“ haben keine Quelle im Repositorium, die Seite
„Brauchwasser-Zapfprofil“ bleibt richtig. Logbuch (Vorschlag, Version beim Anwender zu erfragen): „Die gepflegte
Jahressumme eines Brauchwasser- oder Prozesswärmeprofils gilt auch dann, wenn seine Projektkopie anders heißt als der
Katalogeintrag.“

## 8 Offen

1. **Die drei Namenssichten** `Abfrage_Monatsstrom`, `Abfrage_Monatswaerme_Brauchwasser` und
   `Abfrage_Monatswaerme_Prozesse` haben im Code keinen Leser mehr. Sie zu entfernen wäre ein Schemaschritt — eigener
   Auftrag, wenn gewünscht.
2. **`LiesProjekt` filtert die Kopie nicht auf das Projekt** (bei allen drei Bedarfsarten): Zeigte eine Zuordnung auf
   die Kopie eines anderen Projekts, nennte der Dialog deren Namen; der Lauf rechnet sie nicht (Warnung), das
   Speichern löst sie über den Namen neu auf. In der Testdatenbank kommt der Fall nicht vor.

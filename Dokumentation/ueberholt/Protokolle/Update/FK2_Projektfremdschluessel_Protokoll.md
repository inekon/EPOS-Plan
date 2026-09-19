# Schemaschritt 96 — Fremdschlüssel der Projekttabellen auf `Tab_Projekt`

Protokoll zum Auftrag FK‑2 (19.09.2026). Der gültige Stand steht im Quelltext
(`EPOS.Kern/Allgemein/Update/ProjektFremdschluessel.cs`) und in der Statusdatei; dieses
Papier hält fest, was gefunden, entschieden und gemessen wurde.

## 1 Auftrag und Entscheide

Der Anwender hat in seiner `Kenndaten.sqlite` Fremdschlüssel auf `Tab_Projekt` nachgerüstet
und den Schema-Dump geliefert. Entscheide vom 19.09.2026:

- **Schemaschritt 96 für alle Installationen**, Umfang **vollständig**.
- Fremdschlüssel `REFERENCES Tab_Projekt(ID) ON DELETE CASCADE ON UPDATE CASCADE` für
  **28 Tabellen**; `Tab_Variante` für **beide** Projektspalten.
- **Ohne** Fremdschlüssel bleiben `Tab_Applikation` (die Spalte merkt sich das zuletzt
  geöffnete Projekt, 0 = keines — ein Fremdschlüssel verböte diese 0) und
  `Tab_Kenndaten_Kuehlung_STAMM` (Katalogtabelle der Auslieferung, `ID_Projekt` = 0).
- Index `<Tabelle>_ID_Projekt` überall, wo er fehlt.
- Die Spalte `Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden` aus dem Dump stammt vom
  zweiten Rechner und wird **nicht** angelegt.

## 2 Der Dump — Befund

Der Dump (UTF‑16 LE, nur CREATE-Anweisungen) wurde ohne `sqlite_sequence` in eine
`:memory:`-Datenbank geladen und über `pragma_foreign_key_list` gegen die Testdatenbank
gehalten. Er führt 120 Tabellen und **44** Beziehungen auf `Tab_Projekt`.

**20 davon trägt die Testdatenbank schon:** `Tab_Einstellungen`, `Tab_Energieanlagen`,
`Tab_Klimaregion`, `Tab_Kostenprofil`, `Tab_Preisreihe`, `Tab_ProjektTarif`,
`Tab_ProjektWerte`, `Tab_ProjektWirtschaftlichkeit`, `Tab_Pufferspeicher`,
`Tab_SpeicherAuslegung`, die acht `Z_Projekt*`-Zuordnungen, `energy_price` und
`energy_project_settings`.

**24 sind im Dump neu** — genau die, die der Anwender selbst nachgerüstet hat:
`Berichtskonfiguration`, `Tab_BHKW`, `Tab_Brauchwasser`, `Tab_Brauchwassertyp`,
`Tab_Ergebnis`, `Tab_ErgebnisStromMatrix`, `Tab_ErgebnisWirtSensitivitaet`,
`Tab_ErgebnisWirtschaftlichkeit`, `Tab_Heizkessel`, `Tab_PV`, `Tab_ProjektPhotovoltaik`,
`Tab_Prozesstyp`, `Tab_Prozesswaerme`, `Tab_Quellprofil`, `Tab_Solar`,
`Tab_Solarganglinie`, `Tab_Solarkollektoren`, `Tab_Stromganglinie`, `Tab_Stromspeicher`,
`Tab_Stromverbraucher`, `Tab_Variante` (nur `ID_Projekt`), `Tab_WP`, `Tab_Waermebedarf`,
`Tab_Wechselrichter`.

**Vier Tabellen und eine Spalte kommen hinzu** (Entscheid „Umfang vollständig“): der Dump
führt sie nicht, die Testdatenbank kennt sie mit Projektspalte und ohne Beziehung —
`Tab_Gebaeude`, `Tab_Kenndaten`, `Tab_Klimadaten`, `Tab_Stromverbrauchertyp` und
`Tab_Variante.ID_ProjektRef`.

24 + 4 = **28 Tabellen, 29 Beziehungen.** Die Zahl ist unabhängig gegengerechnet: Die
Testdatenbank führt 50 Tabellen mit einer Projektspalte, 20 tragen die Beziehung, zwei sind
benannt ausgenommen — bleiben 28.

Zwei Beziehungen des Dumps tragen `ON DELETE NO ACTION` (`Tab_Pufferspeicher`,
`Tab_SpeicherAuslegung`). Sie stehen so auch in der Testdatenbank und werden **nicht**
angefasst — der Schritt setzt keine bestehende Regel um.

## 3 Zwei gemessene Befunde, die den Weg bestimmt haben

### 3.1 `defer_foreign_keys` schützt die Kindzeilen nicht

Die Schritte 74 und 81 bauen **Kind**tabellen um; ihr `DROP TABLE` kann keine fremde Zeile
mitreißen. Schritt 96 baut **Eltern**tabellen um: `Tab_WP` trägt drei Kindtabellen mit
`ON DELETE CASCADE`, `Tab_Waermebedarf` und `Tab_Stromganglinie` je ihre Ganglinienpunkte.
Bei eingeschalteten Fremdschlüsseln führt `DROP TABLE` ein implizites `DELETE FROM` aus —
**und das löst die Kaskade aus.** Gemessen (SQLite 3.45): Mit `defer_foreign_keys = ON`
verliert die Kindtabelle ihre Zeilen genauso wie ohne; das PRAGMA verschiebt die *Prüfung*,
nicht die *Aktion*. Auch `legacy_alter_table` hilft nicht — es schützt Sichten und Trigger,
nicht die Fremdschlüsseltexte anderer Tabellen (die schreibt `RENAME` bei eingeschalteten
Fremdschlüsseln mit um).

Es bleibt Schritt 1 des Handbuch-Rezepts: `PRAGMA foreign_keys = OFF` **vor** der
Transaktion. `DbVorgang` beginnt seine Transaktion im Konstruktor, dort war das nicht zu
erreichen. Dafür ist `DataRepository.VorgangOhneFremdschluessel()` entstanden — genau der
Helfer, auf den der Kopfkommentar des SQLite-Werkzeugkastens in `SchemaMigration.cs`
vorausverweist („ein Helfer dafür entsteht ERST, wenn der erste Schritt ihn wirklich
braucht“). Die Klammer schließt sich selbst: `DbVorgang.Dispose` schaltet die
Fremdschlüssel wieder ein, bevor die Verbindung in den Pool zurückgeht.

**Nebenwirkung, bewusst gehandhabt:** Mit abgeschalteten Fremdschlüsseln kaskadiert auch
das Waisen*löschen* nicht mehr. Der Schritt räumt deshalb selbst nach — rekursiv über
`pragma_foreign_key_list`, `SET NULL`/`SET DEFAULT` geachtet — und weist jede Zahl im
Bericht aus.

### 3.2 Waisen werden erst geheilt, dann gelöscht

`Tab_Kenndaten` führt in der Testdatenbank **1.446** Kennlinienzeilen mit `ID_Projekt` 0
oder 1. Alle 1.446 hängen über `ID_WP` an einer echten Wärmepumpe eines echten Projekts —
**165 davon am Referenzprojekt 1045.** Dort ist `ID_Projekt` eine ungepflegte Redundanz und
kein Waisenbefund; sie zu löschen wäre Datenverlust gewesen und hätte die Referenzbasis
gebrochen. Dasselbe gilt für 20 Zeilen in `Tab_Stromverbrauchertyp`, darunter eine an einem
Verbraucher des Projekts 1045.

Der Schritt zieht die Projektspalte deshalb aus dem Elternsatz nach, wo es einen gültigen
gibt (`Tab_Kenndaten` ← `Tab_WP`, die drei Typtabellen ← ihre jeweilige Elterntabelle), und
löscht erst, was danach zu keinem Projekt **und** keinem gültigen Elternsatz gehört. Die
Heilung steht als `Eintrag.ElternTabelle` im Katalog — sie ist Datenpflege, keine Rechnung.

Ein dritter Befund fiel dabei mit an: 48 Zeilen der Ergebnis-Detailtabellen hängen an
verwaisten `Tab_Ergebnis`-Zeilen, und ihre Fremdschlüssel tragen `NO ACTION`. Ohne die
Nachräumung hätte der Commit am `foreign_key_check` gehangen.

## 4 Umsetzung

- **Eine Quelle:** `EPOS.Kern/Allgemein/Update/ProjektFremdschluessel.cs` mit dem Katalog
  der 28 Tabellen, `UmbauNoetig`, `Waisen`, `Umbauen(tabelle, bericht)`, `Alle(bericht)`
  und `Offen()`. Der **Zieltext je Tabelle entsteht aus dem geltenden
  `sqlite_master.sql`** — die Klauseln treten vor das schließende `) STRICT`, sonst bleibt
  der Text Zeichen für Zeichen der Bestand. Keine 28 abgeschriebenen CREATE-Texte, die beim
  nächsten `ADD COLUMN` veralten; wer eine Spalte ergänzt, ergänzt hier nichts.
- **Wächter im Helfer:** Tabelle vorhanden, Spalte vorhanden, kein zweiter Fremdschlüssel
  auf einer Spalte, die schon einen trägt (dann übersprungen — der Fall der
  Anwenderdatenbank mit ihren 24), `) STRICT` am Ende (sonst benannter Abbruch),
  Spaltenliste namentlich statt `SELECT *`.
- **Eigene Transaktion je Tabelle:** Ein Fehler an Tabelle 17 lässt die ersten sechzehn
  stehen; der nächste Lauf setzt dort fort.
- **Umbenennen statt Droppen:** Die alte Tabelle weicht auf `<Tabelle>_alt` aus, die neue
  entsteht sofort unter dem echten Namen. Grund ist der Namensraum der Indizes — sie wandern
  beim Umbenennen mit und geben ihre Namen erst frei, wenn die Hilfstabelle fällt.
- **Index gemessen statt aufgezählt:** Fehlt ein Index, dessen *erste* Spalte die
  Projektspalte ist, legt der Schritt `<Tabelle>_<Spalte>` an. In der Testdatenbank waren
  das die fünf des Dumps (`Tab_Ergebnis`, `Tab_ErgebnisStromMatrix`,
  `Tab_ErgebnisWirtSensitivitaet`, `Tab_ErgebnisWirtschaftlichkeit`, `Tab_Wechselrichter`)
  und `Tab_Variante_ID_ProjektRef`, den der Löschweg der Kaskade braucht.
- **Drei Leser** wie im Haus üblich: `SchemaMigration.Schritt_96_ProjektFremdschluessel`
  (eine Berichtszeile je Tabelle samt Waisenzahl), `Werkzeuge/Testdatenbankschema`
  (`--trocken` zählt nur) und `EPOS.Kern.Tests/TestDatenbank`. `SchemaStand.Zielversion`
  steht **zuletzt** auf 96.
- **`ProjektCtrl` behält seine drei Vorarbeiten.** Sie löschen nicht, sie lösen auf:
  `PvVerguetungAufloesen` schreibt einen Wert zurück und muss vor dem Löschen laufen,
  `VariantenVerknuepfungenEntfernen` löst die Verweise *fremder* Varianten,
  `PufferReferenzenLoesen` räumt Verweise ohne Projektspalte. Die Kaskade fängt nur das auf,
  was an ihnen vorbeiginge; der Kommentar dort nennt den Schritt.
- **Der Dump ist entfernt** (`sql/schema/Kenndaten_FK_2026-09-19.schema.sql`): eine
  Momentaufnahme, keine Quelle. Die Beziehungen stehen im Katalog und in diesem Protokoll.

## 5 Nachweise

**Testdatenbank** `Referenzlaeufe/Kenndaten_Test.sqlite`, erzeugt mit
`Werkzeuge/Testdatenbankschema`: `SchemaVersion` 96, `PRAGMA integrity_check` → ok,
`PRAGMA foreign_key_check` leer, 29 Beziehungen in den 28 Tabellen, 119 von 120 Tabellen
`STRICT` (`sqlite_sequence` ist die Systemtabelle), alle 14 Sichten abfragbar und im
Wortlaut unverändert. Größe 67,5 → 64,5 MB (`VACUUM`). LFS-Zeiger geprüft.

**Waisen der Testdatenbank** — 1.466 geheilt, 129 gelöscht, 131.557 abhängige Zeilen
mitgelöscht:

| Tabelle | geheilt | gelöscht | mitgelöscht |
|---|---:|---:|---:|
| `Tab_Brauchwasser` | 0 | 50 | 50 (`Tab_Brauchwassertyp`) |
| `Tab_Ergebnis` | 0 | 15 | 83 (sieben Ergebnis-Detailtabellen) |
| `Tab_ErgebnisWirtSensitivitaet` | 0 | 4 | 0 |
| `Tab_ErgebnisWirtschaftlichkeit` | 0 | 30 | 0 |
| `Tab_Kenndaten` | 1.446 | 0 | 0 |
| `Tab_Prozesswaerme` | 0 | 3 | 3 (`Tab_Prozesstyp`) |
| `Tab_Stromganglinie` | 0 | 4 | 105.121 (`Tab_StromganglinieDaten`) |
| `Tab_Stromverbraucher` | 0 | 20 | 20 (`Tab_Stromverbrauchertyp`) |
| `Tab_Stromverbrauchertyp` | 20 | 0 | 0 |
| `Tab_Waermebedarf` | 0 | 3 | 26.280 (`Tab_WaermebedarfDaten`) |

Die übrigen 18 Tabellen sind ohne Waise umgebaut worden. `Tab_Brauchwassertyp` und
`Tab_Prozesstyp` zählen bei sich selbst 0, weil ihre Waisen schon mit der Elterntabelle
gefallen sind — die Reihenfolge des Katalogs stellt Eltern vor Kind.

**Referenzlauf** der fünf CI-Projekte (1030, 1007, 1017, 1045, 1046) gegen
`2026-09-18_R9_Kesselbrennstoff`: `GESAMT: PASS`, 1.656.417 Werte, und **byte-gleich**
(`diff -r` ohne Unterschied außer dem Laufprotokoll). Ohne die Heilung hätte Projekt 1045
seine 165 Kennlinienzeilen verloren.

**SQL-Dialekt-Prüfer:** 1.533 Texte, 0 Fundstellen. **Windows-Schale** auf Linux:
0 Fehler. **Kern-Filter:** 0 Fehler.

**Zwölf neue Fälle** in `EPOS.Kern.Tests/ProjektFremdschluesselTests.cs`: Katalogwache,
Zieltext samt `STRICT`-Abbruch, Indextext; Umbau auf einer Arbeitskopie, die dafür erst auf
den Stand 95 zurückgebaut wird — Zeilen, Ids, `sqlite_sequence`, Spalten, Indizes, fremde
Beziehungen und Sichten unverändert, `Tab_Kenndaten` behält seine Beziehung auf `Tab_WP`;
Wiederholbarkeit, teilverknüpfte Datenbank, Waisenfall, Heilung, Cascade-Probe und die
Vorgangsklammer.

**Ein Bestandstest angepasst:** `ProjekttransferTests.P11` setzte `ID_ProjektRef` auf ein
Projekt, das es nicht gibt — genau die Lücke, die der Schritt schließt. Eine
Bestandsdatenbank kann sie bis zur Migration weiter führen, und die Kohärenzprüfung soll sie
dort melden; der Fall stellt die alte Lage deshalb ausdrücklich her, statt sie stillschweigend
nicht mehr zu prüfen.

## 6 Was der Schritt nicht tut

- Er ändert **keine bestehende Beziehung**: Die zwei mit `ON DELETE NO ACTION`
  (`Tab_Pufferspeicher`, `Tab_SpeicherAuslegung`) bleiben, wie sie sind.
- Er legt **keine Spalte** an und entfernt keine — insbesondere nicht
  `Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden` aus dem Dump.
- Er rührt **keinen Rechenweg** an. Entfernt wird ausschließlich, was zu keinem Projekt
  gehört und deshalb kein Rechenweg je gelesen hat.

## 7 Nachtrag GL-1 — die vier Ganglinien-Schreibwege (19.09.2026)

Schritt 96 hinterließ vier Stellen mit einem `OFFEN`-Vermerk: `SolarganglinieCtrl.Insert`,
`StromganglinieCtrl.Insert`, `WaermebedarfCtrl.Insert` und
`StromganglinieDatenCtrl.InsertKompletteGanglinie` schrieben `ID_Projekt` bis dahin still als
Spaltenvorgabe 0 und seither ausdrücklich als `NULL`. Beides ist kein Projekt: Ein Filter nach
`ID_Projekt` findet den Satz nicht, und die neue Kaskade nimmt ihn beim Löschen des Projekts
nicht mit. Der Anwender hat das am 19.09.2026 als eigenen Auftrag herausgelöst („Altfehler
beheben: Vier Ganglinien-Schreibwege").

### 7.1 Befund: die vier Wege haben keinen Aufrufer mehr

Die Suche nach Aufrufern ergab für alle vier Stellen **null Treffer** — auch keine
Instanziierung der Controller. Was einmal an ihnen hing, ist inzwischen zweimal abgelöst
worden:

- **Katalogware** schreibt die AP5-Importkette seit iU9-W12 (Strom) und W9-E-3 (Wärmebedarf)
  über `StromganglinieStammCtrl.ImportGanglinie` bzw. `WaermebedarfStammCtrl.ImportGanglinie`
  in die `_STAMM`-Tabellen. Die Projekttabelle war dafür von Anfang an das falsche Ziel; die
  vier Stromganglinien und drei Wärmebedarfe, die Schritt 96 als Waisen entfernt hat, waren
  genau solche Katalogsätze am falschen Ort.
- **Ins Projekt** kommt eine Ganglinie als Kopie über
  `…StammCtrl.ApplyGanglinieToProjekt` → `CopyGanglinieToProjekt`. Dieser Weg setzt
  `ID_Projekt` seit jeher und ist der, den die Bedienung heute nimmt.

In der Testdatenbank steht nach Schritt 96 kein Satz der drei Projekttabellen ohne Projekt
(23 Stromganglinien, 8 Wärmebedarfe, 0 Solarganglinien — alle mit gültiger Projektnummer).

### 7.2 Umsetzung

Jeder der vier Wege nimmt die Projektnummer als **Pflichtparameter** und schreibt sie:

| Schreibweg | vorher | jetzt |
|---|---|---|
| `SolarganglinieCtrl.Insert` | `…, ID_Projekt, …) VALUES (?, NULL, ?, ?)` | `Insert(int idProjekt)`, `VALUES (?, ?, ?, ?)` |
| `StromganglinieCtrl.Insert` | dito | `Insert(int idProjekt)` |
| `WaermebedarfCtrl.Insert` | dito | `Insert(int idProjekt)` |
| `StromganglinieDatenCtrl.InsertKompletteGanglinie` | dito (Kopfsatz) | dritter Parameter `int idProjekt` |

Zwei Ablehnungen, beide **benannt**:

- `idProjekt <= 0` → `ArgumentOutOfRangeException`, deren Text die `_STAMM`-Tabelle als
  richtiges Ziel für Katalogware nennt. Die Prüfung steht **vor** dem stillen „keine Werte,
  also nichts zu tun" von `InsertKompletteGanglinie`.
- Ein Projekt, das es nicht gibt, weist der Fremdschlüssel aus Schritt 96 ab; der Schreibweg
  meldet den Datenbankfehler über `DataRepository.FehlerMelden` und gibt `false` zurück. Die
  Tabelle bleibt unverändert.

Der Kopfkommentar von `PreisreiheCtrl`, der `StromganglinieCtrl.Insert` als Stolperstelle
nannte, ist nachgezogen.

### 7.3 Nachweis

`EPOS.Kern.Tests/GanglinienProjektSchreibwegTests` — neun Fälle: je Tabelle ein Satz mit
Projekt, der über `ID_Projekt` gefunden wird; Kopf und Werte in einer Transaktion, beide
folgen dem Löschweg des Projekts; die vier Wege weisen `0` und `-1` benannt ab (Theorie mit
zwei Werten); ein unbekanntes Projekt wird vom Fremdschlüssel abgewiesen, drei Meldungen mit
`FOREIGN KEY`, Zeilenzahlen unverändert; Katalogware landet in `_STAMM` und kommt erst als
Kopie mit Projektnummer ins Projekt; und eine Wache, dass keine der drei Projekttabellen einen
Satz ohne Projekt führt.

### 7.4 Was GL-1 nicht tut

- Er **entfernt die vier Controller nicht**, obwohl sie keinen Aufrufer haben. Die Falle ist
  zu; ob der Altbestand fällt, entscheidet der Anwender (offener Punkt).
- Er rührt `ReadAll`, `ReadSingle` und `Delete` dieser Controller **nicht** an. Sie arbeiten
  weiter über alle Projekte hinweg — `Delete(bezeichner)` löscht jeden gleichnamigen Satz,
  gleich welchem Projekt er gehört. Ohne Aufrufer ist das heute folgenlos; mit dem ersten
  Aufrufer wäre es ein eigener Auftrag.
- Er ändert **die Testdatenbank nicht** und **keinen Rechenweg**: Referenzlauf der fünf
  CI-Projekte gegen R9: GESAMT PASS.
- `BrauchwasserCtrl.Insert` und `HeizkesselCtrl.Insert` bleiben, wie sie sind — sie scheitern
  seit jeher und werden von niemandem gerufen; sie gehörten nicht zum Auftrag.

# Nachlese zur Senkenklammer: zwei Schreibstellen, nachgemessen

Untersuchungsauftrag aus Abschnitt 8 des
[`Senke_Speicherklammer_Protokoll.md`](Senke_Speicherklammer_Protokoll.md) („Benachbarte
Stellen derselben Machart"). Arbeitszweig `ios_migration_september`, Arbeitsstand auf
`w-nachlese-schreibwege`.

**Das Ergebnis vorweg: An beiden Stellen bleibt das Verhalten unverändert.** Was sich
geändert hat, sind die Begründungen im Quelltext — eine war überholt, eine war falsch
formuliert — und drei Prüffälle, die den heutigen Stand festhalten. Zwei Fachfragen gehen
an den Anwender.

## 1. Stelle (1): `KomponentenUebernahmeCtrl`, Schritte 8 und 9

### 1.1 Was der Kommentar behauptete

Der Bestandskommentar nannte ZWEI Gründe dafür, dass die Senkenlisten und die
Betriebsführung des Stromspeichers **nach** dem Commit des Hauptvorgangs geschrieben
werden:

1. „`Z_AnlageSenkeCtrl.SchreibenJeAnlage` führt seine eigene Transaktion."
2. „Die Anlagen-ID ist ein AutoWert und steht erst danach fest."

### 1.2 Was davon trägt

**Grund 1 ist überholt.** Seit `iU9-W16a-O-1` wird aus `DataRepository.Vorgang()` unter
einer angemeldeten `Vorgangsklammer` ein Sicherungspunkt auf DERSELBEN Verbindung. Genau
das nutzt die Senkenklammer.

**Grund 2 ist falsch formuliert, hat aber einen wahren Kern.** Die AutoWert-ID entsteht
beim `INSERT`, nicht beim `COMMIT`; der Bestand nutzt das an **derselben**
Einfügeanweisung: `SpeicherFlottenStudieCtrl.EinheitenInProjektUebernehmen` legt
Anlagenzeilen über `AnlagenSql.SQL_ANLAGE_INSERT` an und arbeitet mit der frischen ID über
`DbVorgang.EinfuegenUndId` in derselben Transaktion weiter.

Der wahre Kern ist ein anderer: **Dieser Ablauf beschafft seine IDs über eine ZWEITE
Verbindung.** Schritt 7 schreibt mit `Ausfuehren` und merkt sich keine ID; Schritt 8
(`NeueAnlagenIds`) und Schritt 9 (`AnlageFinden`) lesen sie über `DataRepository` nach.
Vor dem Commit ist die Zeile dort unsichtbar. Der Satz müsste also heißen: *„… und ist
über die zweite Verbindung erst nach dem Commit LESBAR."* So steht er jetzt da.

### 1.3 Welchen Schaden der heutige Zustand anrichtet

Belegt, und er liegt **nicht** an der fehlenden Klammer:

- `SenkenNachziehen` ruft `ctrl.SchreibenJeAnlage(neueAnlage, zeilen)` und **wertet den
  Rückgabewert nicht aus**. `Z_AnlageSenkeCtrl.SchreibenJeAnlage` fängt jeden
  Datenbankfehler selbst ab, rollt zurück, schreibt eine Zeile auf die Konsole und gibt
  `false` zurück.
- Scheitert der Schritt, steht die übernommene Komponente **ohne ihre Senkenkette** da —
  sie rechnet dann mit der Rang-1-Vorbelegung Heizkreis/Beides statt mit der Kette der
  Quelle. `Uebernehmen` meldet trotzdem `true`, und `hinweise` bleibt leer.
- **Schritt 9 macht es anders:** `VariantenNachziehen` meldet jeden Fehlschlag über
  `BK_KOMP_HINW_VARIANTE` in `warnungen`. Die beiden Nachbarschritte behandeln denselben
  Fall also ungleich.
- Eine Nebenbemerkung: `KiAktionenUebernahme` sagt über diese Aktion, die
  Transaktionsklammer des Bestands schließe „einen Teilzustand aus halb übernommenen
  Komponenten" aus. Für die Schritte 8 und 9 stimmt das nicht.

### 1.4 Warum trotzdem nichts umgebaut wird

1. **Die Klammer allein behebt den Schaden nicht.** Zöge man die beiden Aufrufe unter die
   Klammer, bliebe der Rückgabewert weiterhin unausgewertet: Der Sicherungspunkt nähme nur
   die Senkenzeilen zurück, der äußere Vorgang würde festgeschrieben, und die Komponente
   stünde genauso ohne Kette da. Der Gewinn beschränkte sich auf das Zeitfenster zwischen
   Commit und Nachzug (Absturz, Stromausfall).
2. **Die Klammer über den GANZEN Ablauf ist keine Nachlese, sondern ein Umbau.** Der
   Ablauf verlässt sich ausdrücklich darauf, dass die Prüfungen in Schritt 7 AUSSERHALB
   der Transaktion lesen — der Kommentar über `PufferCache` sagt es wörtlich: „NÖTIG, WEIL
   DIE PRÜFUNG AUSSERHALB DER TRANSAKTION LÄUFT". Eine Klammer über den `try`-Block änderte
   damit, was `PufferAbbildung`, `PufferCache` und `Geraete` SEHEN. Das ist der
   Abbruchgrund des Auftrags.
3. **Die Behebung des belegten Schadens ist eine Fachentscheidung** — melden oder
   zurücknehmen —, und sie wird nicht nebenbei getroffen. Sie geht als `NL-Q2` an den
   Anwender.

### 1.5 Was stattdessen geschehen ist

- **Vier Kommentarstellen berichtigt** (Klassenkopf, Lesekommentar Schritt 1, Schritt 8,
  `VariantenNachziehen`): der überholte Grund ist fort, der wahre Grund steht präzise da,
  und die offene Fachfrage ist an der Aufrufstelle benannt. **Keine Codezeile geändert.**
- **Drei Prüffälle** in `EPOS.Kern.Tests/UebernahmeNachzugTests.cs` (siehe Abschnitt 3).

## 2. Stelle (2): `WizardCtrl`, die Stränge des PV-Dialogs

### 2.1 Der Sachverhalt, gemessen

- **Die Stelle liegt in der Klammer.** Sie steht in `Add_WP_Waermeerzeuger`, und diese
  Methode meldet den hereingereichten `DbVorgang` über `Vorgangsklammer.Setzen` am Faden
  an. `AssistentCtrl.Anlegen` und `AssistentCtrl.Fortschreiben` reichen ihn herein.
- **Das `catch` ist nicht die Stelle, die schluckt.** `AnlageStrangCtrl.SchreibenJeAnlage`
  fängt jeden Datenbankfehler selbst ab und meldet ihn über den RÜCKGABEWERT; durch das
  `catch` darüber kommt praktisch nichts. Verschluckt wird der Fehlschlag dadurch, dass
  dieser Rückgabewert **nicht ausgewertet** wird.
- **Der Rückzug findet nicht statt.** Unter der Klammer wird der eigene Vorgang von
  `SchreibenJeAnlage` zum Sicherungspunkt; sein Rücktritt nimmt nur die Strangzeilen
  zurück. Der übrige Lauf wird festgeschrieben — gemessen im Prüffall, der den Vorgang
  selbst führt und `Commit` ruft.
- **Und die Eingabe des Anwenders kehrt sich stillschweigend um:** Die Anlage führt danach
  keine Strangzeile, also trägt `StraengeWiederherstellen` die Liste des VORZUSTANDS wieder
  ein. Der Anwender bekommt seine alten Stränge zurück, ohne dass ihm jemand sagt, dass die
  neuen nicht angekommen sind. `Console.WriteLine` erreicht ihn nicht.

### 2.2 Die „Nachbarn" aus dem Kommentar

Es gibt sie, und sie verhalten sich gleich — alle drei im selben Methodenrumpf:

| Nachbar | Behandlung |
|---|---|
| `KostenProjektPositionenCtrl.ZuordnungReparieren` / `AnkerNachziehen` | `try … catch { }`, „BEST EFFORT — ein gelungenes Speichern scheitert daran nicht" |
| `KostenVorlagenUebernahmeCtrl.PflichtpositionenSicherstellen` | `try … catch { }`, „BEST EFFORT wie die Nachbarn" |
| `GeraeteWaisen.Aufraeumen` | Bericht geht nicht in den Rückgabewert ein |

**Der Unterschied, den der Kommentar nicht macht:** Alle drei sind **Nachsorge**, die der
Lauf selbst anstößt — Kostenanker heilen, Pflichtpositionen ergänzen, Waisen aufräumen.
Was dort ausfällt, holt der nächste Lauf oder ein Migrationsschritt. Die Strangzeile
schreibt dagegen, **was der Anwender gerade eingegeben hat**. Ein „best effort" auf einer
Nachsorge und ein „best effort" auf einer Anwendereingabe sind nicht dasselbe Versprechen.

### 2.3 Was geschehen ist

- **Der Kommentar** nennt die vier Punkte oben jetzt ausdrücklich. **Keine Codezeile
  geändert** — das Verhalten wartet auf `NL-Q1`.
- **Ein Prüffall** hält den heutigen Stand fest (Abschnitt 3).

## 3. Nachweis

Drei neue Fälle in `EPOS.Kern.Tests/UebernahmeNachzugTests.cs`, einer in
`EPOS.Kern.Tests/AnlageStrangTests.cs`. Der Fehlschlag wird jeweils über eine ERZWUNGENE
Beziehung ausgelöst — ein echter Datenbankfehler auf dem echten Schreibweg, im Muster der
Senkenklammer.

1. `Eine_AutoWert_Id_steht_beim_Einfuegen_fest_und_nicht_erst_beim_Commit` — im offenen
   Vorgang liefert `EinfuegenUndId` die ID sofort, und ein Lesebefehl DESSELBEN Vorgangs
   findet die Zeile. Damit ist der Bestandssatz widerlegt.
2. `Ohne_Klammer_sieht_eine_zweite_Verbindung_die_neue_Zeile_nicht` — derselbe Zustand über
   `DataRepository` gelesen: nichts. Unter `Vorgangsklammer.Setzen(v)`: die Zeile. Das ist
   der wahre Kern der Begründung, und die Gegenprobe steht im selben Fall.
3. `Die_Uebernahme_traegt_die_Senkenkette_der_Quelle_nach` — der erste Prüffall des
   Ablaufs `Uebernehmen` überhaupt: Gewerk Wärmepumpe von Projekt 1043 nach 1019; danach
   stehen Ziel, Bedarfsart und Rang je Anlage so da wie in der Quelle.
4. `AnlageStrangTests.Ein_gescheitertes_Schreiben_der_Dialog_Straenge_bleibt_unbemerkt` —
   der Lauf wird mit einem eigenen `DbVorgang` gefahren (der Weg von
   `AssistentCtrl.Speichern`), die Dialogliste zeigt auf einen Wechselrichter, den es nicht
   gibt. `Add_WP_Waermeerzeuger` meldet **`true`**, der Vorgang wird festgeschrieben, und
   gelesen werden hinterher die ALTEN Stränge.

**Gegenproben.** Jeder der drei belastbaren Fälle wurde versuchsweise entwertet und danach
zurückgebaut:

- Fall 3: Schritt 8 (`SenkenNachziehen`) ausgehängt → **rot** („Collections differ") — der
  Fall misst wirklich den Nachzug und nicht den Bestand des Ziels.
- Fall 2: die Anmeldung am Faden ausgehängt → **rot** („Values differ") — es ist die
  Klammer, die den Unterschied macht, nicht die Reihenfolge der Befehle.
- Fall 4: die Strangliste auf einen gültigen Wechselrichter gesetzt → **rot**
  („Collections differ") — der Fall misst den Fehlschlagweg und nicht eine Konstante.
  Die Konsolenspur des grünen Laufs zeigt die Kette wörtlich: „Die Strangliste der Anlage …
  konnte nicht gespeichert werden: FOREIGN KEY constraint failed" → „Strang-Rettung: 2
  Strangzeile(n) … wiederhergestellt" → „Daten erfolgreich aktualisiert."

## 4. Die zwei Fachfragen an den Anwender

### `NL-Q1` — Die Stränge des PV-Dialogs im Speicherlauf des Assistenten

**Frage.** Der Speicherlauf schreibt die im PV-Dialog bearbeiteten Stränge. Scheitert das,
wird der Fehlschlag heute verschluckt: Der Lauf meldet Erfolg, wird festgeschrieben, und
die alten Stränge kehren still zurück. Soll das so bleiben, oder soll der Fehlschlag

- **(a) den Lauf zurücknehmen** — durchreichen, der Vorgang rollt zurück, nichts von diesem
  Speichern steht (der Weg der Senkenklammer und von `AssistentCtrl`), oder
- **(b) als benannter Schritt gemeldet werden** — der Lauf wird festgeschrieben, aber
  `AssistentErgebnis` nennt den Schritt und der Aufrufer zeigt EINE Meldung (der Weg von
  Entscheid `E-4`), oder
- **(c) bleiben wie es ist** — „best effort wie die Nachbarn"?

**Empfehlung: (a).** Drei Gründe. Erstens ist es die Eingabe des Anwenders und keine
Nachsorge — die Nachbarn im selben Rumpf sind Kostenanker, Pflichtpositionen und
Aufräumlauf, und deren Ausfall holt der nächste Lauf nach; eine verlorene Strangliste holt
niemand nach. Zweitens ist (c) heute nicht einmal ehrlich „best effort": Der Lauf
**kehrt die Eingabe um** und stellt den Vorzustand wieder her, was schlimmer ist als sie
liegen zu lassen. Drittens hat der Speicherlauf seit `iU9-W16a-O-1` genau dafür eine
Klammer, und `E-4` verlangt für denselben Lauf, dass ein Fehlschlag gemeldet statt
verschluckt wird — (b) wäre die halbe Antwort: Der Anwender erführe davon, stünde aber vor
einem Projekt, dessen PV-Stränge nicht zu seiner Eingabe passen.

**Umfang bei (a):** eine Zeile in `WizardCtrl` (Rückgabewert auswerten, `false` zurück wie
bei den Nachbarzeilen darüber — `SpVariantenVerwerfen`/`FachspaltenVerwerfen`), dazu der
Prüffall aus Abschnitt 3 umgedreht und eine Gegenprobe.

### `NL-Q2` — Der Nachzug der Senkenlisten in der Komponenten-Übernahme

**Frage.** Die Übernahme zieht die Senkenlisten nach dem Commit nach und wertet den
Rückgabewert nicht aus. Scheitert der Schritt, steht die übernommene Komponente ohne ihre
Senkenkette da, und die Übernahme meldet Erfolg. Soll der Fehlschlag

- **(a) in `hinweise` gemeldet werden** — wie es Schritt 9 für die Speichervarianten schon
  tut (`BK_KOMP_HINW_VARIANTE`); die Übernahme bleibt stehen, der Anwender erfährt, welche
  Komponente ihre Senkenkette nicht bekommen hat, oder
- **(b) die ganze Übernahme zurücknehmen** — dann müssen die Schritte 8 und 9 unter die
  Klammer und vor den Commit, oder
- **(c) bleiben wie es ist**?

**Empfehlung: (a).** Die Übernahme ist ausdrücklich nicht umkehrbar, und ihre Oberfläche
führt bereits einen Hinweiskanal, den der Anwender nach dem Lauf sieht; ein ungleiches
Verhalten der Nachbarschritte 8 und 9 ist ohnehin schwer zu rechtfertigen. (b) verschöbe
eine Transaktionsgrenze in einem Ablauf, der sich an anderer Stelle ausdrücklich darauf
verlässt, außerhalb der Transaktion zu lesen (`PufferCache`) — das wäre eine eigene Welle
mit eigenem Nachweis, nicht die Antwort auf einen Nebenbefund.

**Umfang bei (a):** ein neuer Ressourcenschlüssel in beiden Sprachen, `ResourceDesigner`
ziehen, `SenkenNachziehen` wertet den Rückgabewert aus, ein Prüffall.

## 5. Ein Nebenbefund, nicht mitumgebaut

`StraengeWiederherstellen` bedient jede Anlage, die JETZT keine Strangzeile führt. Eine vom
Dialog übergebene **leere** Liste (`StraengeZuModell` liefert eine leere Liste, nie `null`)
ist ein gültiger Auftrag — sie löscht die Stränge. Danach führt die Anlage keine Strangzeile
mehr, und die Rettung trägt die alten wieder ein. Das Entfernen der letzten Strangzeile
bzw. das Umschalten auf „vereinfacht" könnte sich damit im Del+Add-Speicherweg aufheben.
**Nicht gemessen** — dieser Auftrag hat es beim Lesen gefunden, und es ist eine eigene
Frage.

## 6. Abnahme

| | vorher (`763f7713`) | nachher |
|---|---|---|
| Kern-Filter Release | 0 Fehler, 5 Warnungen | 0 Fehler, 5 Warnungen |
| `EPOS.Kern.Tests` | 3 104 / 3 104 | 3 108 / 3 108 |
| `EPOS.UI.Tests` | 4 530 / 4 530 | 4 530 / 4 530 |
| `SpeicherEngine.Tests` | 370 / 370 | 370 / 370 |
| `KiKern.Tests` | 499 / 499 | 499 / 499 |
| `SpeicherPlanung.Tests` | 27 / 28 (1 übersprungen) | 27 / 28 (1 übersprungen) |
| `SqlDialektPruefer` | 1 462 Texte, 0 Fundstellen | 1 462 Texte, 0 Fundstellen |
| `ChartProben` | 64 Bilder, 0 Verstöße | 64 Bilder, 0 Verstöße |
| Referenzlauf gegen `2026-09-16_R8_Heizkessel_Kaskade` (5 Projekte) | 5/5 PASS, byte-gleich | 5/5 PASS, byte-gleich |

Beide Testläufe zweimal gefahren: einmal in der Standardkultur, einmal unter
`LC_ALL=en_US.UTF-8` — beide Male dieselben Zahlen. **Keine neue Warnung**, kein
Schemaschritt, keine neue Referenzbasis. **Kein iOS-Lauf** (die Änderung trifft die
iOS-Hülle nicht); am Rechenweg ist nichts angefasst, die vier neuen Fälle sind der ganze
Zuwachs.

## 7. Abnahmepunkte auf Windows

Keine — es ist keine Codezeile geändert worden. Nach einer Antwort auf `NL-Q1` bzw. `NL-Q2`
kommen die Abnahmepunkte mit der Umsetzung.

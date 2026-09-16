# Nachlese zur Senkenklammer: zwei Schreibstellen, nachgemessen

Untersuchungsauftrag aus Abschnitt 8 des
[`Senke_Speicherklammer_Protokoll.md`](Senke_Speicherklammer_Protokoll.md) („Benachbarte
Stellen derselben Machart"). Arbeitszweig `ios_migration_september`, Arbeitsstand auf
`w-nachlese-schreibwege`.

**Der Untersuchungsauftrag hat das Verhalten an beiden Stellen unverändert gelassen** und
nur die Begründungen im Quelltext berichtigt — eine war überholt, eine war falsch
formuliert — sowie vier Prüffälle angelegt, die den damaligen Stand festhalten. Die zwei
Fachfragen, die er an den Anwender gestellt hat, sind **am 16.09.2026 beide nach Empfehlung
entschieden und im selben Zug umgesetzt** worden: `NL-Q1` = (a), `NL-Q2` = (a). Was daraus
geworden ist, steht in den Abschnitten 4 und 5; die Abnahme der Umsetzung in Abschnitt 7.

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

### 1.4 Warum der Untersuchungsauftrag nichts umgebaut hat

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
   zurücknehmen —, und sie wird nicht nebenbei getroffen. Sie ging als `NL-Q2` an den
   Anwender und ist **entschieden: melden** (Abschnitt 4).

### 1.5 Was stattdessen geschehen ist

- **Vier Kommentarstellen berichtigt** (Klassenkopf, Lesekommentar Schritt 1, Schritt 8,
  `VariantenNachziehen`): der überholte Grund ist fort, der wahre Grund steht präzise da,
  und die offene Fachfrage war an der Aufrufstelle benannt. **Keine Codezeile geändert.**
- **Drei Prüffälle** in `EPOS.Kern.Tests/UebernahmeNachzugTests.cs` (siehe Abschnitt 3).

> **Stand heute:** Mit dem Entscheid `NL-Q2` = (a) wertet Schritt 8 den Rückgabewert aus
> und meldet. Die zwei Kommentarstellen zu Schritt 8 (Aufrufstelle und Schreibstelle) sagen
> das jetzt; der wahre Grund für die Reihenfolge — die zweite Verbindung — steht
> unverändert daneben. Siehe Abschnitt 4.

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

- **Der Kommentar** nannte die vier Punkte oben ausdrücklich. **Keine Codezeile
  geändert** — das Verhalten wartete auf `NL-Q1`.
- **Ein Prüffall** hielt den damaligen Stand fest (Abschnitt 3).

> **Stand heute:** Mit dem Entscheid `NL-Q1` = (a) wird der Rückgabewert ausgewertet und
> der Lauf zurückgenommen. Der Kommentar trägt kein „BEST EFFORT" mehr, und der Prüffall
> ist umgedreht. Siehe Abschnitt 4.

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
   **Mit `NL-Q1` ist dieser Fall umgedreht** und heißt seither
   `Ein_gescheitertes_Schreiben_der_Dialog_Straenge_nimmt_den_Lauf_zurueck` (Abschnitt 7.2).

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

## 4. Die zwei Fachfragen — entschieden und umgesetzt

Beide Fragen sind am **16.09.2026 nach Empfehlung** entschieden worden. Der Wortlaut der
Fragen, die drei Wahlmöglichkeiten je Frage und die Begründung der Empfehlung stehen in
der Fassung dieses Protokolls, die den Untersuchungsauftrag abschloss; hier steht,
**was gilt**.

### `NL-Q1` = (a) — Der Fehlschlag nimmt den Lauf zurück

**Entschieden.** Scheitert im Speicherlauf des Assistenten das Schreiben der im PV-Dialog
bearbeiteten Stränge, wird der Lauf **zurückgenommen**. Nicht melden-und-weiterlaufen,
nicht lassen.

**Umgesetzt** in `WizardCtrl.Add_WP_Waermeerzeuger`, Block `ST1`:

- Der Rückgabewert von `AnlageStrangCtrl.SchreibenJeAnlage` wird **ausgewertet**. Ein
  `false` verwirft die zwei Sicherungen und steigt mit `return false` aus — Zeichen für
  Zeichen der Weg der Nachbarzeilen darüber (`SpVariantenVerwerfen`,
  `FachspaltenVerwerfen`, `return false`).
- **Der Rückzug kommt an, und der Schritt wird benannt.** `AssistentCtrl.Anlegen` und
  `.Fortschreiben` melden diesen Schritt als `Add_WP_Waermeerzeuger` im
  `AssistentErgebnis`; `AssistentCtrl.Speichern` lässt den Vorgang der Klammer aus
  `iU9-W16a-O-1` zurücktreten, und `Meldungstext` trägt den Namen in die EINE Meldung
  (Entscheid `E-4`). Der Schrittname ist sprechend und bleibt, wie er ist.
- **Die Eingabe kehrt sich nicht mehr um.** `StraengeWiederherstellen` steht HINTER dem
  Block `ST1`. Weil der Lauf vorher endet, kommt der Rettungsweg gar nicht mehr zum Zuge —
  gemessen an der Konsolenmitschrift, in der „Strang-Rettung" nicht mehr vorkommt.
- **Das `catch` bleibt.** Es ist nach wie vor praktisch unerreichbar, weil der Controller
  selbst abfängt; entfernt zu werden verdient es deshalb nicht: Ein entferntes `catch`
  änderte das Verhalten für den Tag, an dem der Controller doch einmal wirft. Statt zu
  schlucken führt es diesen Wurf jetzt auf denselben Weg wie ein `false` — Konsolenzeile,
  dann Rückzug.
- **Der Kommentar** trägt nicht mehr „BEST EFFORT wie die Nachbarn", sondern nennt den
  Unterschied, um den es geht: Die drei Nachbarn im selben Rumpf sind **Nachsorge**, die
  der Lauf selbst anstößt und die der nächste Lauf nachholt; diese Zeile schreibt, **was
  der Anwender gerade eingegeben hat**.

### `NL-Q2` = (a) — Der Senkennachzug meldet sich

**Entschieden.** Scheitert der Nachzug der Senkenliste bei der Komponentenübernahme, wird
das **im Hinweiskanal gemeldet**. Die Übernahme wird **nicht** zurückgenommen; die
Transaktionsgrenze bleibt, wo sie ist.

**Umgesetzt** in `KomponentenUebernahmeCtrl.SenkenNachziehen` (Schritt 8): Der Rückgabewert
von `Z_AnlageSenkeCtrl.SchreibenJeAnlage` wird ausgewertet, ein Fehlschlag geht als neuer
Schlüssel `BK_KOMP_HINW_SENKEN` in `warnungen` — und damit über `hinweise` an denselben
Kanal, den der Anwender nach dem Lauf sieht.

**Die Ungleichheit der zwei Nachbarschritte war der eigentliche Mangel.** Danach verhalten
sie sich gleich:

| | Schritt 8 `SenkenNachziehen` | Schritt 9 `VariantenNachziehen` |
|---|---|---|
| Fehlschlag erkannt an | Rückgabewert `false` | `AnlageFinden <= 0` bzw. `Insert <= 0` |
| Kanal | `warnungen` → `hinweise` | `warnungen` → `hinweise` |
| Form | `string.Format(BK_KOMP_HINW_SENKEN, Bezeichner)` | `string.Format(BK_KOMP_HINW_VARIANTE, Bezeichner)` |
| Benannt über | Bezeichner der Anlage | Bezeichner der Anlage |
| Menge | eine Zeile je betroffener Anlage | eine Zeile je betroffener Anlage |
| Danach | Lauf geht weiter, `Uebernehmen` meldet `true` | Lauf geht weiter, `Uebernehmen` meldet `true` |

Einziger Unterschied in der Schreibweise: Schritt 9 schließt seine Meldung mit `continue`
ab, weil danach noch Anweisungen folgen; in Schritt 8 IST der Aufruf die letzte Anweisung
des Schleifenrumpfes, ein `continue` wäre dort ohne Wirkung.

**Der neue Text nennt die Folge, nicht die Technik** — was jetzt gilt, nicht welche Methode
`false` geliefert hat. Im Zuschnitt des Nachbarn: gleiches Satzgerüst („Die … der …
„{0}" konnte(n) nicht angelegt werden"), dann die Folge hinter dem Gedankenstrich wie bei
`BK_KOMP_HINW_KINDTABELLE` und `BK_KOMP_HINW_PUFFERVERWEIS`:

- **de:** Die Wärmesenken der Anlage „{0}" konnten nicht angelegt werden — die Anlage
  rechnet mit der Vorbelegung Heizkreis (beides).
- **en:** The heat sinks of unit "{0}" could not be created — the unit uses the default
  heating circuit (both).

Der Kommentar an der Aufrufstelle (Schritt 8 im Ablauf von `Uebernehmen`) sagt das jetzt
ebenfalls: Die Übernahme bleibt stehen, der Fehlschlag wird gemeldet, die
Transaktionsgrenze bleibt. Der Satz über die zweite Verbindung — der wahre Kern der alten
Begründung — steht unverändert daneben, denn er gilt weiter.

## 5. Der Nebenbefund — gemessen, BESTÄTIGT, nicht geändert

Der Untersuchungsauftrag hatte ihn beim Lesen gefunden: `StraengeZuModell` liefert bei
leerer Maskenliste eine **leere Liste**, nie `null`; eine leere Liste ist ein gültiger
Löschauftrag; `StraengeWiederherstellen` bedient genau Anlagen ohne Strangzeile. Verdacht:
Das Entfernen der letzten Strangzeile hebt sich im Del+Add-Speicherweg auf.

**Gemessen, nicht mehr vermutet** — Prüffall
`AnlageStrangTests.Eine_geleerte_Dialogliste_traegt_die_Strang_Rettung_heute_wieder_ein`.
Er hält das heutige Verhalten fest und fordert nichts:

1. Die Anlage führt zwei gespeicherte Stränge.
2. Der Dialog gibt eine **leere** Liste mit — der Anwender hat die letzte Zeile entfernt.
3. `SchreibenJeAnlage` löscht die Zeilen und meldet **`true`**: ein GELUNGENES Schreiben.
4. Danach führt die Anlage keine Strangzeile — und `StraengeWiederherstellen` trägt die
   Liste des Vorzustands wieder ein.
5. Gelesen werden hinterher wieder „Alt Ost" und „Alt West".

**`NL-Q1` erledigt den Befund NICHT und verschiebt ihn auch nicht — es bestätigt ihn.**
Der Rückzug aus `NL-Q1` greift bei einem FEHLSCHLAG; hier gelingt das Schreiben. Es gibt
nichts zurückzunehmen, der Lauf endet nicht, und der Rettungsweg kommt sehr wohl zum Zuge.
Der Befund sitzt an einer anderen Stelle als `NL-Q1`, nämlich in der Frage, **woran
`StraengeWiederherstellen` eine „vom Dialog geleerte" Anlage von einer „vom Dialog nicht
angefassten" unterscheiden soll** — heute kann es das nicht, weil beide Fälle dasselbe
Bild ergeben: keine Strangzeile.

**Gegenprobe gefahren:** `StraengeWiederherstellen` versuchsweise ausgehängt → der Fall
wird **rot** („Collections differ"). Er misst also die Rettung und nicht eine Konstante;
danach wieder eingebaut.

**Geändert wurde nichts** — das war der Auftrag. Ob und wie der Befund behoben wird,
entscheidet der Anwender.

## 6. Abnahme des Untersuchungsauftrags

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

## 7. Abnahme der Umsetzung (`NL-Q1` und `NL-Q2`)

### 7.1 Was geändert wurde

| Datei | Änderung |
|---|---|
| `EPOS.Kern/Controller/WizardCtrl.cs` | Block `ST1`: Rückgabewert ausgewertet, `false` verwirft die zwei Sicherungen und steigt aus; `catch` bleibt und führt auf denselben Weg; Kommentar neu. Dazu im Kopf von `StraengeWiederherstellen` der Satz, dass ein gescheitertes Schreiben der Dialogliste diese Methode nicht mehr erreicht |
| `EPOS.Kern/Controller/KomponentenUebernahmeCtrl.cs` | Schritt 8: Rückgabewert ausgewertet, Fehlschlag als `BK_KOMP_HINW_SENKEN` in `warnungen`; zwei Kommentarstellen nachgezogen |
| `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx` | neuer Schlüssel `BK_KOMP_HINW_SENKEN` in beiden Sprachen |
| `EPOS.Kern/MyResource/Resource.Designer.cs` | neu erzeugt (`designer_neu.py schreiben`): 6 331 → 6 332 Einträge, +430 Zeichen, zweiter Lauf **+0 / wiederholbar** |

**Nicht angefasst:** `Vorgangsklammer`, `AssistentCtrl.Speichern`, `WaermesenkeClass`, der
Senkenweg der Senkenklammer, die Transaktionsgrenze von `KomponentenUebernahmeCtrl`. Kein
Schemaschritt, keine neue Referenzbasis.

### 7.2 Die Prüffälle und ihre Gegenproben

| Fall | Was er misst | Gegenprobe |
|---|---|---|
| `AnlageStrangTests.Ein_gescheitertes_Schreiben_der_Dialog_Straenge_nimmt_den_Lauf_zurueck` (umgedreht) | `Add_WP_Waermeerzeuger` meldet `false`, der Vorgang wird NICHT festgeschrieben, die Anlagenzeile trägt hinterher noch ihre ALTE Id samt alten Strängen; die Mitschrift zeigt die Rücknahme und **keine** „Strang-Rettung" | Auswertung ausgehängt → **rot** (`Assert.False() Failure`), danach wieder eingebaut |
| `AssistentCtrlTests.Ein_gescheiterter_Strangschritt_nimmt_den_Lauf_zurueck_und_nennt_ihn` (neu) | Über `AssistentCtrl.Speichern`: Ausgang `Fehlgeschlagen`, `Schritt` = `Add_WP_Waermeerzeuger`, der Name steht in `Meldungstext`; Zählstand, Anlagenbezeichner und der vollständige Zeileninhalt der einundzwanzig projektgebundenen Tabellen sind die von vorher | Auswertung ausgehängt → **rot** (`Assert.Equal() Failure: Values differ`), danach wieder eingebaut |
| `UebernahmeNachzugTests.Ein_gescheiterter_Senkennachzug_meldet_sich_im_Hinweiskanal` (neu) | `Uebernehmen` meldet weiterhin `true` und keinen `fehler`; für jede betroffene Anlage steht `BK_KOMP_HINW_SENKEN` in `hinweise`; das Ziel führt hinterher wirklich keine Senkenzeile | Auswertung ausgehängt → **rot** (`Assert.Contains() Failure`), die zwei `NL-Q1`-Fälle blieben dabei **grün**; danach wieder eingebaut |
| `AnlageStrangTests.Eine_geleerte_Dialogliste_traegt_die_Strang_Rettung_heute_wieder_ein` (neu, BEFUND) | Abschnitt 5 | `StraengeWiederherstellen` ausgehängt → **rot** (`Assert.Equal() Failure: Collections differ`), danach wieder eingebaut |

**Wie die Fehlschläge erzwungen werden — in beiden Fällen an der Wurzel, nie über einen
Haken im Quelltext.** Für `NL-Q1` die ERZWUNGENE Beziehung
`Z_AnlageStrang.ID_Wechselrichter` → `Tab_Wechselrichter` (ein Wechselrichter, den es nicht
gibt). Für `NL-Q2` ein `BEFORE INSERT`-Wächter auf `Z_AnlageSenke`, der jede neue
Senkenzeile des Zielprojekts abweist: Das trifft ausschließlich Schritt 8, denn innerhalb
des Hauptvorgangs wird `Z_AnlageSenke` nur per `UPDATE` und über die Löschweitergabe der
Anlagenzeile angefasst — die EINZIGE `INSERT`-Stelle des ganzen Ablaufs ist der Nachzug.
Der Wächter wird im `finally` wieder entfernt, die Arbeitskopie ist ohnehin eine eigene.

### 7.3 Zahlen

| | vorher (`8831b83c`) | nachher |
|---|---|---|
| Kern-Filter Release | 0 Fehler, 5 Warnungen | 0 Fehler, 5 Warnungen |
| `EPOS.Kern.Tests` | 3 108 / 3 108 | 3 111 / 3 111 |
| `EPOS.UI.Tests` | 4 530 / 4 530 | 4 530 / 4 530 |
| `SpeicherEngine.Tests` | 370 / 370 | 370 / 370 |
| `KiKern.Tests` | 499 / 499 | 499 / 499 |
| `SpeicherPlanung.Tests` | 27 / 28 (1 übersprungen) | 27 / 28 (1 übersprungen) |
| `SqlDialektPruefer` | 1 462 Texte, 0 Fundstellen | 1 462 Texte, 0 Fundstellen |
| `ChartProben` | 64 Bilder, 0 Verstöße | 64 Bilder, 0 Verstöße |
| Referenzlauf gegen `2026-09-16_R8_Heizkessel_Kaskade` (5 Projekte) | 5/5 PASS, byte-gleich | 5/5 PASS, byte-gleich |

Beide Testläufe zweimal gefahren: einmal in der Standardkultur, einmal unter
`LC_ALL=en_US.UTF-8` — beide Male dieselben Zahlen. Bei einem neuen Text in zwei Sprachen
ist das der eigentliche Nachweis. **Keine neue Warnung** (Schranke 7), kein Schemaschritt,
keine neue Referenzbasis, **kein iOS-Lauf** — die Änderung trifft die iOS-Hülle nicht.

## 8. Abnahmepunkte auf Windows

Beide Sprachen prüfen (Menü „Extras → Sprache"), beide Fälle sind Fehlschlagfälle und
brauchen eine Datenbank, in der sich der Fehlschlag erzeugen lässt — am einfachsten auf
einer Arbeitskopie.

**`A-NLQ1-1`** — Der Assistent nimmt einen gescheiterten Strangschritt zurück.
Projekt mit PV-Anlage öffnen, im PV-Dialog Stränge bearbeiten, den benutzten
Wechselrichter zwischendurch aus dem Projekt entfernen (oder die Zeile auf ein Gerät
zeigen lassen, das es nicht mehr gibt), dann speichern. **Erwartet:** EINE Meldung, die
den Schritt `Add_WP_Waermeerzeuger` nennt; das Projekt ist danach **unverändert** — nicht
teils gespeichert, und die Stränge sind nicht stillschweigend auf den alten Stand
zurückgesprungen, während „gespeichert" gemeldet wird.

**`A-NLQ1-2`** — Der Normalfall bleibt, wie er war. Dieselbe Anlage, Stränge bearbeiten,
alles gültig, speichern. **Erwartet:** Speichern gelingt, die bearbeiteten Stränge stehen
nach dem erneuten Öffnen so da, wie sie eingegeben wurden.

**`A-NLQ2-1`** — Die Komponentenübernahme meldet einen gescheiterten Senkennachzug.
Gewerk mit Senkenketten von einem Projekt in ein anderes übernehmen und den Nachzug
scheitern lassen. **Erwartet:** Die Übernahme läuft durch (sie wird **nicht**
zurückgenommen) und der Hinweisbereich nennt **je betroffener Anlage** den Text: „Die
Wärmesenken der Anlage „…" konnten nicht angelegt werden — die Anlage rechnet mit der
Vorbelegung Heizkreis (beides)." Auf Englisch: „The heat sinks of unit … could not be
created — the unit uses the default heating circuit (both)."

**`A-NLQ2-2`** — Die zwei Hinweise klingen nach einem Verfasser. Eine Übernahme, bei der
sowohl der Senkennachzug als auch die Betriebsführung einer Speichervariante scheitert.
**Erwartet:** Beide Zeilen stehen im selben Hinweisbereich, im selben Satzbau und je einmal
pro Anlage — kein Stilbruch zwischen ihnen.

**`A-NLQ2-3`** — Der Normalfall bleibt, wie er war. Eine gelingende Übernahme mit
Senkenketten. **Erwartet:** Der Hinweisbereich zeigt zu den Senken **nichts**, und die
Senkenketten des Ziels sind die der Quelle.

## 9. Nachtrag 16.09.2026 (`#307`) — die Nebenbemerkung berichtigt

Der letzte Punkt aus Abschnitt 1.3 war eine offene Frage: `KiAktionenUebernahme` sagte
über die Komponentenübernahme, die Transaktionsklammer schließe „einen Teilzustand aus
halb übernommenen Komponenten" aus — für die Schritte 8 und 9 galt das nie, sie laufen
ABSICHTLICH nach dem Commit (Abschnitt 1.2). **Anwenderentscheid 16.09.2026:
richtigstellen, nichts am Verhalten ändern.**

**Zwei Stellen behaupteten dasselbe:** der Entwicklerkommentar in
`KiAktionenUebernahme.KomponenteUebernehmen()` und, gewichtiger, die Registertabelle in
[`Konzept_KI-Assistent_Aufgabensteuerung.md`](../../../aktuell/Konzept_KI-Assistent_Aufgabensteuerung.md)
Zeile 601 — dort steht sie in `aktuell/`, also als Arbeitsgrundlage. Beide sind jetzt
berichtigt: Der Hauptteil ist geklammert, Schritt 8 (`SenkenNachziehen`) und Schritt 9
(`VariantenNachziehen`) laufen danach, ein Teilzustand ist möglich und wird seit `NL-Q2`
gemeldet (`BK_KOMP_HINW_SENKEN`, `BK_KOMP_HINW_VARIANTE`). Der mitgeführte Zeilenverweis
`KomponentenUebernahmeCtrl:283` war ebenfalls überholt — er zeigt heute auf die
Senkenleseschleife vor der Transaktion, nicht mehr auf sie — und ist durch den
Methodennamen `KomponentenUebernahmeCtrl.Uebernehmen` ersetzt.

**Dritte Fundstelle, im Quelltext:** Der Klassenkopf von `KomponentenUebernahmeCtrl`
sagte noch „Schritt 9 meldet das über seine Hinweise, Schritt 8 nicht" (Abschnitt 1,
`<para>` „Damit liegen diese beiden Schritte ausserhalb der Klammer"). Das stimmte, bevor
`NL-Q2` umgesetzt wurde (#306) — seither melden beide Schritte gleich, und dieser Satz war
beim Nachziehen der zwei Kommentarstellen in #306 (Abschnitt 7.1) nicht mitgenommen
worden. Berichtigt.

**Kein Anwendertext behauptet dasselbe.** Geprüft: der Wirkungs- und Vorschautext der
Aktion (`KI_REG_WIRKUNG_KOMPONENTE_UEBERNEHMEN`, `KI_REG_VORSCHAU_KOMPONENTE_UEBERNEHMEN`)
sagt nur „nicht umkehrbar" und nennt die Stückzahlen — das stimmt und bleibt unverändert.
Auch `KiAktionsTexte`, `KiKern.Tests/Registerabbild.cs`,
`EPOS.Kern.Tests/KiRegisterS3Tests.cs`, das Wiki (`Projekte/Wiki/*`) und die übrigen
`Dokumentation/aktuell/`-Papiere führen die Behauptung nicht. **Keine Codezeile geändert**,
kein neuer Ressourcenschlüssel. Gate: Kern-Filter 0 Fehler / 5 Warnungen (keine neue,
Schranke 7), `EPOS.Kern.Tests` 3 111/3 111, `EPOS.UI.Tests` 4 530/4 530, SpeicherEngine
370/370, KiKern 499/499, SpeicherPlanung 27/28 (1 übersprungen), `DokumentationLinkWacheTests`
7/7, `RepositoryOrdnungWacheTests` 10/10. Kein Schemaschritt, keine neue Referenzbasis,
kein iOS-Lauf.



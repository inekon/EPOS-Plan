# Der Speicherweg der Wärmesenke bekommt EINE Klammer

Anwenderentscheid 16.09.2026 („Empfehlung"). Arbeitszweig `ios_migration_september`,
Arbeitsstand auf `w-senke-klammer`.

## 1. Befund

`EPOS.UI.Daten/Simulation/WaermesenkeHuelle.cs`, Methode `Schreiben`:

```csharp
bool ok = new Z_AnlageSenkeCtrl().SchreibenJeAnlage(idAnlage, Modelle(idAnlage, zeilen));
if (!AnlagePufferVerbundCtrl.Schreiben(idAnlage, new List<int>(verbund))) ok = false;
return ok;
```

**Zwei Schreibvorgänge, keine gemeinsame Klammer.** `Z_AnlageSenkeCtrl.SchreibenJeAnlage`
öffnet einen eigenen `DbVorgang`, `AnlagePufferVerbundCtrl.Schreiben` schreibt über
`StilleDb` — jeder Weg ist für sich sauber, um beide zusammen lag nichts. Gelang der erste
und scheiterte der zweite, stand die Senkenliste in ihrer NEUEN Fassung und der Verbund in
der alten: ein halber Stand.

**Zweitens gab es keinen frühen Ausstieg.** Die zweite Zeile lief auch dann, wenn die erste
bereits gescheitert war — `ok` wurde nur auf `false` gesetzt. Einer Anlage wurde also noch
ein Verbund geschrieben, deren Senkenliste gar nicht in der Datenbank stand.

Dieselbe Klasse von Befund, die **W16a-O-1** für den Projektassistenten behoben hat.

## 2. Wo die Klammer liegt — und warum dort

**Im Kern**, als neuer Weg `WaermesenkeClass.SenkenlisteUndVerbundSchreiben(int,
List<Z_AnlageSenkeModel>, IList<int>)`. Drei Gründe:

1. **Die Hausregel.** „Jede Fachänderung wird einmal gemacht — im Kern." Eine Transaktion
   über zwei Controller-Schreibwege ist eine Zusage des Kerns, keine der Hüllenschicht.
   Läge sie in `EPOS.UI.Daten`, wäre sie an jedem anderen Aufrufer vorbei zu umgehen.
2. **Die Werkzeuge liegen dort.** `Vorgangsklammer` ist `internal` in `EPOS.Kern` — aus der
   Hülle nicht erreichbar, und das ist Absicht. Ein `InternalsVisibleTo` hätte die Sperre
   aufgehoben, statt den Weg an den richtigen Ort zu legen.
3. **Der Lese-Gegenweg steht schon da.** `WaermesenkeClass.VerbundLesen` löst die
   Mitgliederliste an EINER Stelle auf; der Schreibweg gehört als Gegenrichtung daneben.

`WaermesenkeHuelle.Schreiben` übersetzt seither nur noch die Dialogzeilen in Modelle und
ruft diesen einen Weg.

## 3. Was sich ändert — und was ausdrücklich nicht

**Geändert:**

- EIN `DbVorgang` über beide Schritte, für dessen Dauer über `Vorgangsklammer` am Faden
  angemeldet. Dadurch wird der eigene Vorgang von `SchreibenJeAnlage` zum Sicherungspunkt
  auf derselben Verbindung, und die `StilleDb`-Anweisungen des Verbundwegs leihen sich
  dieselbe Verbindung samt Transaktion. Über die Dauerhaftigkeit entscheidet allein der
  äußere Vorgang.
- **Früher Ausstieg:** Scheitert die Senkenliste, wird der Verbund gar nicht mehr versucht.
- **Rückzug:** Scheitert einer der beiden, ist hinterher nichts von diesem Lauf geschrieben.

**Unverändert:**

- Die Reihenfolge — erst die Senkenliste, dann der Verbund.
- Die Mitgliederliste geht IMMER heraus, auch leer; das ist der Weg, auf dem ein Verbund
  wieder aufgelöst wird.
- Der Rückgabewert: `false` bei Fehlschlag, wie bisher. Der Dialog meldet ihn weiterhin über
  `SpeichernOk`, die Statuszeile zeigt `SIM_STATUS_SENKE_FEHLER`.
- Kein Schemaschritt, keine neue Referenzbasis. `AssistentCtrl`, `WizardCtrl` und
  `Vorgangsklammer` selbst sind nicht angefasst.

## 4. Der Meldungstext

Mit der Klammer stimmte `SIM_STATUS_SENKE_FEHLER` nicht mehr: Es ist nicht „nicht
vollständig" gespeichert, es ist **nichts** gespeichert. Nachgezogen im Geist der
Berichtigung **W16a-O-1-R2**:

| | vorher | nachher |
|---|---|---|
| DE | ⚠ Die Wärmesenke konnte nicht vollständig gespeichert werden | ⚠ Die Wärmesenke wurde nicht gespeichert — die Anlage ist unverändert |
| EN | ⚠ The heat sink could not be saved completely | ⚠ The heat sink was not saved — the system is unchanged |

Gesucht wurde nach weiteren Fundstellen (Quelltext-Rückfall, Prüffall, Doku, Wiki). Gefunden:
`Resource.resx`, `Resource.en-US.resx`, die erzeugte `Resource.Designer.cs` (Summary-Kommentar)
und die Katalogzeile in `Dokumentation/aktuell/Lokalisierung_Katalog.md`. **Einen
Rückfalltext im Quelltext gibt es hier nicht** — anders als beim Assistenten
(`AssistentCtrl.Meldungstext`) liest die einzige Aufrufstelle
(`SimulationKonfigHuelle.WaermesenkeFertig`) den Schlüssel unmittelbar. Kein Prüffall und
keine Wiki-Seite führt den Satz.

`Resource.Designer.cs` neu erzeugt: 2 221 553 → 2 221 562 Zeichen (+9), ein Schlüssel
abweichend (nur der Summary-Kommentar), 0 neu; der zweite Lauf meldet „+0 / unverändert".

## 5. Nachweis

Drei neue Fälle in `EPOS.Kern.Tests/WaermesenkeSpeicherwegTests.cs`, Muster
`AssistentCtrlTests.Ein_Fehlschlag_in_der_Mitte_nimmt_den_ganzen_Lauf_zurueck`. Prüfanlage
ist 14728 (Projekt 1040) — sie führt vorher zwei Senkenzeilen UND eine Verbundzeile, ein
halber Stand wäre also an beiden Tabellen sichtbar. Der Fehlschlag wird über die erzwungene
Beziehung auf `Tab_Pufferspeicher.ID` ausgelöst (ein Puffer, den es nicht gibt) — ein echter
Datenbankfehler auf dem echten Schreibweg.

1. `Ein_Fehlschlag_beim_Verbund_nimmt_die_Senkenliste_zurueck` — der zweite Schreibvorgang
   scheitert erzwungen; danach stehen Senkenliste und Verbund Zeile für Zeile so da wie
   vorher, und der Aufrufer bekommt `false`.
2. `Ein_erfolgreicher_Lauf_schreibt_dasselbe_wie_die_bisherige_Schreibfolge` — zweimal
   derselbe Auftrag auf je einer unberührten Arbeitskopie: einmal geklammert, einmal über
   die beiden Controller nacheinander ohne Vorgang. Verglichen wird der vollständige
   Zeileninhalt beider Tabellen; er stimmt bis auf die vergebenen Ids überein.
3. `Eine_gescheiterte_Senkenliste_laesst_den_Verbund_unberuehrt` — der frühe Ausstieg.
   Gemessen an der Konsolenspur: Beide Listen werden unbrauchbar mitgegeben; meldet sich nur
   die Senkenliste und nicht `StilleDb.NonQuery`, ist der zweite Schreibvorgang nie gelaufen.

**Gegenprobe.** Die Klammer wurde zweimal versuchsweise ausgehängt und danach zurückgebaut:

- *Bestandsweg wiederhergestellt* (beide Controller nacheinander, ohne gemeinsamen Vorgang):
  Fall 1 **rot** — die Senkenliste steht in ihrer neuen Fassung (`253|14728|1|…|1054185`)
  statt im Bestand (`61|14728|1|…|1054187`), genau der halbe Stand; Fall 3 **rot** —
  `StilleDb.NonQuery fehlgeschlagen` steht in der Mitschrift, der Verbund lief also trotz
  gescheiterter Senkenliste; Fall 2 bleibt grün (er misst die Gleichheit mit genau diesem Weg).
- *Nur die Anmeldung am Faden ausgehängt*, äußerer Vorgang behalten: Fall 2 **rot** — die
  Controller holen sich eigene Verbindungen und laufen in die Schreibsperre
  (`SQLite Error 5: 'database is locked'`, 30 s je Fall). Das ist genau der Grund, aus dem es
  `Vorgangsklammer` gibt.

Der bestehende Fall
`EPOS.UI.Tests/Dialoge/WaermesenkeDialogTests.Ein_gescheitertes_Schreiben_kommt_als_SpeichernOk_false`
bleibt grün.

## 6. Abnahme

| | vorher (`32300bc1`) | nachher (`25ed6f19`, nach Merge) |
|---|---|---|
| Kern-Filter Release | 0 Fehler, 6 Warnungen | 0 Fehler, 5 Warnungen |
| Windows-Schale (`EnableWindowsTargeting=true`) | — | 0 Fehler, 5 Warnungen (Bestand) |
| `EPOS.Kern.Tests` | 3 090 / 3 090 | 3 104 / 3 104 |
| `EPOS.UI.Tests` | 4 526 / 4 526 | 4 530 / 4 530 |
| `SpeicherEngine.Tests` | 370 / 370 | 370 / 370 |
| `KiKern.Tests` | 499 / 499 | 499 / 499 |
| `SpeicherPlanung.Tests` | 27 / 28 (1 übersprungen) | 27 / 28 (1 übersprungen) |
| `SqlDialektPruefer` | 1 460 Texte, 0 Fundstellen | 1 462 Texte, 0 Fundstellen |
| `ChartProben` | — | 64 Bilder, 0 Verstöße |
| Referenzlauf gegen `2026-09-16_R8_Heizkessel_Kaskade` (5 Projekte) | 5/5 PASS, byte-gleich | 5/5 PASS, byte-gleich (1 656 417 Werte) |

Beide Testläufe zweimal gefahren: einmal in der Standardkultur, einmal unter
`LC_ALL=en_US.UTF-8` — beide Male dieselben Zahlen. **Kein iOS-Lauf** (die Änderung trifft
die iOS-Hülle nicht).

Die Mehrzahlen nach dem Merge stammen aus dem Zweig, nicht aus dieser Arbeit; eigener Zuwachs
sind die drei Fälle in `EPOS.Kern.Tests`. **Keine neue Warnung.** Die Warnungen des
Zweigkopfs (`CS0108` ×2, `CS0109` ×2, `xUnit2000`, dazu `WFO0003` in der Windows-Schale) sind
Bestand und wurden nicht angefasst; die sechste des Ausgangsstands (`xUnit2029`) hat der Zweig
selbst behoben.

## 7. Abnahmepunkte auf Windows

- **A-SENKE-1** — Senkendialog einer Anlage mit Verbund öffnen, Zeilen ändern, Verbund
  ändern, mit OK speichern: Statuszeile „✔ Wärmesenke gespeichert (…)", Dialog erneut
  öffnen — Senkenliste und Verbund stehen wie eingegeben.
- **A-SENKE-2** — Verbund auflösen (alle Mitglieder abwählen) und speichern: Der Verbund ist
  weg, die Senkenliste steht. Die leere Mitgliederliste geht weiterhin heraus.
- **A-SENKE-3** — Meldungstext in beiden Sprachen: Programm auf Deutsch bzw. Englisch
  stellen; der Fehlertext der Statuszeile lautet „⚠ Die Wärmesenke wurde nicht gespeichert —
  die Anlage ist unverändert" bzw. „⚠ The heat sink was not saved — the system is
  unchanged".
- **A-SENKE-4** — Simulation eines Projekts mit Verbund rechnen: unveränderte Ergebnisse
  (der Referenzlauf ist byte-gleich, dies ist die Sichtprobe an der Oberfläche).

## 8. Benachbarte Stellen derselben Machart

Beim Lesen aufgefallen, **nicht** mitumgebaut:

- `KomponentenUebernahmeCtrl` (um Zeile 430 und 1086): Der Kommentar dort hält ausdrücklich
  fest, dass `Z_AnlageSenkeCtrl.SchreibenJeAnlage` „seine eigene Transaktion" führt — die
  Übernahme schreibt Anlagen, Senken und Stränge nacheinander, jeder Schritt für sich. Ob
  das eine Klammer braucht, ist eine eigene Frage.
- `WizardCtrl.SenkenSichern` (um Zeile 1014 und 1251) schreibt die Senkenlisten je Anlage in
  einer Schleife; hier ist die Klammer bereits da, weil `AssistentCtrl.Speichern` den
  gesamten Lauf klammert — der Weg über das Menü „Projekt → Neu…" läuft geklammert.
- `WizardCtrl` um Zeile 1825 schreibt PV-Stränge in einem `try`-Block ohne Auswertung des
  Rückgabewerts; das ist ein anderer Befund (stiller Fehlschlag), keine fehlende Klammer.

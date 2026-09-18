# B7P — die Nachweise eines Laufs überleben ihn

Protokoll zum Anwenderentscheid **B7-E-1** vom 18.09.2026 („Empfehlung"), der den offenen Punkt
**B7-2** des
[Wirtschaftlichkeitskonzepts](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
(§ 2.6, § 6.3) schließt. Der zweite Teil des ursprünglichen Auftrags — die Pauschalzeile A3 nach
§ 9 KWKG (B7-E-2, offene Punkte B7-3 und BK1-3) — ist auf Anwenderentscheid vom selben Tag aus
diesem Auftrag herausgenommen und wird getrennt gebaut; er kommt hier nicht vor.

---

## 1 Messung vor dem Bau

**Vier Listen entstanden im Rechenlauf und starben mit ihm.** Sie hingen alle an
`WirtschaftlichkeitErgebnis`, und ihre Kommentare sagten es auch so („Nicht persistiert"):

| Liste | Was sie trägt | Wer sie liest |
|---|---|---|
| `KwkgModule` (`KwkgModulNachweis`, 18 Felder) | je BHKW-Modul: Leistung, Vbh, Mengenkette brutto → netto → eigen/einspeisung, Sätze, Kontingent, Herleitungstexte | Unterzeilen A1/A2 der Rubrik, Modultafel in Word und Excel, Gruppe 5 des BHKW-Dialogs |
| `EnergiekostenJeAnlage` (`EnergieAnlageNachweis`) | je Anlage Menge × Preis = Kosten | Herleitungszeilen unter „Energiekosten" |
| `Betriebskosten` (`KostenPositionNachweis`) | je Kostenposition Art, Bemessung, Menge, Satz, Betrag | Betriebskostenzeilen der Rubrik |
| `KohaerenzHinweise` (`KohaerenzHinweis`) | Widersprüche zwischen gebuchter Gutschrift und Steueranteil im Preis | Reiter, Word, Excel, BHKW-Dialog |

Dazu vier Skalare mit demselben Vermerk: `VermiedenMengeMWh`, `VermiedenEntlastung9bJahr`,
`ProduzierendesGewerbe` und `BezugsspitzeKW`.

**Die Folge war messbar und sichtbar.** Wer das Programm schloss und den gebuchten Stand wieder
aufschlug, sah die Summen — aber keine Herleitung darunter. Der Reiter ließ die Unterzeilen weg,
Word und Excel ihre Modultafel (beide prüfen wortgleich `KwkgModule != null && Count > 0`), und
Gruppe 5 des BHKW-Dialogs sagte „Mengenkette: noch kein gebuchtes Ergebnis", obwohl eines gebucht
war. Die Zahlen waren gespeichert, ihre Begründung nicht.

**Zwei Dinge am Bestandsweg machten den Bau riskant:**

- `LadeErgebnisse` führte **einen** `catch { }` um die ganze Projektschleife. Eine Ausnahme aus
  einem Deserialisierer hätte dort alle Ergebniszeilen aller Projekte verschluckt — ohne ein Wort.
- `Persistiere` läuft in einer Transaktion mit äußerem `catch { }`. Ein Wurf im Schreibweg hätte
  den ganzen Lauf gekostet, nicht nur seine Nachweise.

---

## 2 Der Bau

### 2.1 Ein Umschlag statt vier Tabellen

`EPOS.Kern/Allgemein/Wirtschaftlichkeit/ErgebnisNachweisUmschlag.cs` ist die **einzige** Stelle,
die das Format kennt — Schreib- und Leseweg in einer Klasse, damit ein neues Feld nicht an einer
der beiden Seiten vergessen wird.

- **Format:** Präfix `nw1:` + JSON, ohne Einrückung, **ohne GZip**. Anders als beim
  Auslegungsprofil (`gz1:`, Jahresreihen) sind das ein paar Dutzend Zeilen; der Klartext bleibt
  lesbar, wer eine Datenbank in der Hand hat, kann nachsehen.
- **Serialisierung:** `IncludeFields = true` ist Pflicht — alle vier Nachweistypen führen
  ausschließlich Felder; ohne die Option schriebe der Serialisierer leere Objekte und läse sie
  auch wieder ein. Dazu `DefaultIgnoreCondition = WhenWritingNull`.
- **Fassung 1** trägt die vier Listen und die vier Skalare. Eine fremde Fassung wird **nicht halb**
  gelesen: `Lesen` liefert `null`.
- **Warum kein Schemaschritt und keine Nachweistabellen:** Es sind vier Listen mit je einem Dutzend
  Feldern, deren Länge an der Zahl der Anlagen und Kostenpositionen hängt. Als Tabellen wären das
  vier Schemata, vier Schreibwege, vier Lesewege und vier Migrationen — für Daten, die reiner
  Ausweis sind und aus denen nichts gerechnet wird. `Tab_ErgebnisWirtschaftlichkeit` ist ohnehin
  keine Schematabelle: Sie entsteht und wächst seit W1 über `WirtschaftlichkeitCtrl.SpalteSicher`,
  und `Nachweis_Json` ist die einundzwanzigste Spalte auf diesem Weg. **`SchemaStand.Zielversion`
  bleibt 89.**

### 2.2 Schreibweg — der Lauf ist wichtiger als sein Nachweis

`Schreiben` **wirft nie**. Über dem Längenwächter von 4 MiB liefert es `null` und einen fertigen
Hinweistext; die Spalte bekommt dann `DBNull`, und der Grund hängt sich über `Anhaengen` an das
`HinweisText`-Feld derselben Ergebniszeile. Der Anwender erfährt es am Lauf — nicht erst, wenn er
die Unterzeilen vermisst. Die Ergebniszeile selbst wird immer geschrieben.

Die INSERT-Kette wuchs von 52 auf **53 Spalten, 53 Platzhalter und 53 Parameter** (gezählt).

### 2.3 Leseweg — ein enger Fang, sonst nichts

In `LadeErgebnisse` steht der Umschlag **innerhalb** der Zeilenschleife, in einem **eigenen engen**
`try`. Die Spalte wird tolerant geprüft (`r.Table.Columns.Contains`), damit eine nie migrierte
Datei unverändert lädt. Ist der Umschlag vorhanden, aber unlesbar, bleiben die Listen leer und es
entsteht **genau ein** `KohaerenzHinweis` der Schwere `HINWEIS`: „Die Nachweise dieses
gespeicherten Laufs sind nicht lesbar — bitte neu rechnen." Er läuft über den Kohärenzweg, den
Reiter, Word und Excel ohnehin zeigen — keine Meldung, kein Dialog.

### 2.4 Was eingefroren wird

`HerleitungEigen`, `HerleitungEinspeisung` und `KohaerenzHinweis.Text` sind fertig formatierte
Sätze in der Sprache des Laufs. Ein gespeicherter Lauf zeigt sie deshalb in der Sprache, in der er
gerechnet wurde, nicht in der gerade eingestellten. Das ist hingenommen: Die Alternative wäre,
Schlüssel und Argumente einzeln zu führen — also die Formatierung ein zweites Mal zu schreiben.

### 2.5 Testdatenbank

`Werkzeuge/Testdatenbankschema` legt die Spalte als **Konserve** an (kein eigener Schritt, gleiche
Begründung und wortgleiches Muster wie bei `StromsteuerBefreiungModus`): Der `SqlDialektPruefer`
löst das INSERT gegen genau diese Datei auf und meldete ohne die Spalte eine Fundstelle, die in der
Anwendung keine ist. Werkzeuglauf: **1 Spalte angelegt**, Trockenlauf danach **0 Spalten, 0
Tabellen**, Schemastand 89 (Zielstand 89).

---

## 3 Was sich für den Anwender ändert

| Ort | Vorher (gebuchter Stand) | Nachher |
|---|---|---|
| Ergebnisreiter, Rubrik | „davon Einspeisung / davon Eigenstrom" fehlten | beide Unterzeilen stehen |
| Ergebnisreiter, Energiekosten | Herleitungszeile je Anlage fehlte | steht |
| Word, Excel (Rückfall auf den gespeicherten Stand) | keine Modultafel | Modultafel wie im frischen Lauf |
| BHKW-Dialog, Gruppe 5 | „Mengenkette: noch kein gebuchtes Ergebnis" | die zwei Mengenkettenzeilen des gebuchten Laufs |
| Kohärenzzeilen | nur im frischen Lauf | auch beim gebuchten Stand |

Word, Excel und der BHKW-Dialog brauchten **keine** Änderung: Alle drei bevorzugen den frischen
Lauf und fallen sonst auf `LadeErgebnisse` zurück — die Persistenz füllt diesen Rückfall.

---

## 4 Nachweis

**Neu: `EPOS.Kern.Tests/ErgebnisNachweisPersistenzTests` (8 Fälle,
`[Collection("Testdatenbank")]`, Kultur gepinnt), Projekt 1030 „Referenz BHKW-Kaskade".**

1. Der gespeicherte Lauf trägt alle vier Listen mit denselben Schlüsselgrößen und die vier Skalare
   wieder (mit Abbruchmeldung, falls der Lauf gar keine Zeile führt — sonst misst der Fall nichts).
2. Die Unterzeilen `ERL_A1_EINSPEISUNG` und `ERL_A2_EIGEN` erscheinen beim gebuchten Stand.
3. Der Berichtsrückfall von Word und Excel trägt die Modultafel — gemessen an der Bedingung, die
   beide Generatoren wortgleich führen.
4. Ein kaputter Umschlag kostet **nur** die Nachweise: alle Ergebniszeilen stehen, die Listen sind
   leer, genau ein Hinweis.
5. Eine fremde Fassung (`Version: 99`) wird genauso behandelt.
6. Ein Lauf ohne Umschlag lädt unverändert — die Spalte wird dafür tatsächlich entfernt; kein
   Hinweis, kein Lärm.
7. Ein zu großer Umschlag liefert `null` und einen Grund, ohne Wurf.
8. Rundweg ohne Datenbank: Präfix, Fassung und Schrott werden auseinandergehalten.

**Dazu:** `EPOS.UI.Tests/Dialoge/BhkwWirtschaftlichkeitDialogTests` — neuer Fall „Gruppe 5 zeigt
die Mengenkette aus dem gebuchten Stand" (ohne Lauf, mit `ErgebnisseLaden`: zwei Zeilen statt des
Hinweises).

### Gegenproben — jede wurde rot gemacht und zurückgebaut

| Eingriff | Erwartet | Gemessen |
|---|---|---|
| Enger `try` heraus, `Lesen` wirft wieder (nur der äußere Fang) | Persistenzfall fällt | 3 Fälle rot; `Ein_kaputter_Umschlag…` meldet abweichende Zeilenzahl — der äußere Fang hatte alle Ergebniszeilen verschluckt |
| Konservenspalte aus der Testdatenbank entfernt | genau eine Fundstelle | `1 Fundstellen` — `WirtschaftlichkeitCtrl.cs:6609`, „table Tab_ErgebnisWirtschaftlichkeit has no column named Nachweis_Json"; nach dem Zurückbauen wieder `0 Fundstellen` |
| Längenwächter auf 0 Byte | kein Wurf, Ergebniszeile steht, Hinweis steht | kein Wurf; 3 von 3 Ergebniszeilen geschrieben, alle drei mit `Nachweis_Json IS NULL`, alle drei mit dem Satz „Die Nachweiszeilen dieses Laufs konnten nicht gespeichert werden …" am Ende von `HinweisText` |

### Gate

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler, **5 Warnungen** (Bestand, Schranke 7) |
| `dotnet test WP-Plan.Kern.slnf`, Kultur de-DE | 8 814 grün, 1 übersprungen, 0 rot (Kern 3 292, UI 4 618, KiKern 499, SpeicherEngine 378, SpeicherPlanung 27) |
| dieselben Tests unter `LC_ALL=en_US.UTF-8` | dieselben Zahlen, 0 rot |
| `Werkzeuge/SqlDialektPruefer` | 1 490 SQL-Texte, **0 Fundstellen** |
| `Proben/ChartProben` | 64 Bilder, 0 Verstöße |
| Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen `2026-09-16_R8_Heizkessel_Kaskade` | **GESAMT: PASS**, 1 656 417 Werte; alle fünf Projekte zusätzlich **byte-gleich** |

Die Basis bleibt unverändert: Der Umschlag ist reiner Ausweis, keine Einfrierregel ist berührt.

---

## 5 Abnahmepunkte (Windows, Projekt „BHKW Test München")

- **A-B7P-1:** Wirtschaftlichkeit rechnen, Programm schließen, neu starten, die Vergleichsgruppe
  ohne neue Rechnung öffnen — die Rubrik zeigt „davon Einspeisung" und „davon Eigenstrom" sowie die
  Energiekostenzeile je Anlage.
- **A-B7P-2:** Denselben gebuchten Stand als Word- und als Excel-Bericht ausgeben — die Modultafel
  „KWK-Zuschlag je Modul" steht in beiden.
- **A-B7P-3:** BHKW-Wirtschaftlichkeitsdialog öffnen, ohne in dieser Sitzung gerechnet zu haben —
  Gruppe 5 zeigt die Mengenkette des gebuchten Laufs statt „noch kein gebuchtes Ergebnis".

---

## 6 Logbuch-Entwurf (Wiki „Update-Logbuch", Version 1.2.0.2)

> Gespeicherte Wirtschaftlichkeitsläufe zeigen ihre Herleitungen jetzt auch ohne neue Rechnung —
> KWK-Zuschlag je Modul, Energiekosten je Anlage und die Betriebskostenpositionen.

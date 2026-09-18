# B6 — § 9 Abs. 1 Nr. 3 StromStG als Ausweis (Umsetzungsprotokoll)

Etappe B6 des Konzepts
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
(§ 7 Reihenfolge, § 3.8 Befund B‑1, Entscheidung K3 vom 03.09.2026). Anwenderauftrag vom
17.09.2026 („setze B6 um"). Zweig `w-b6` auf `ios_migration_september`.
**Neuer Schemaschritt 88** — der im Konzept genannte Schritt 62 war längst vergeben.

## 1. Messung — was vorher wirklich galt

| # | Frage | Befund |
|---|---|---|
| 1 | Bucht der Rechenweg § 9 Abs. 1 Nr. 3 als Erlös oder nur als Ausweis? | **Als Erlös, immer.** `WirtschaftlichkeitCtrl` hängte die Reihe `STROMSTEUER_BEFREIUNG` unbedingt an und wies denselben Betrag zusätzlich in `StromsteuerBefreiungJahr1` aus. Der Kommentar im Dialog („Vorgabe ‚Ausweis' ist der heutige Rechenstand") war falsch; Befund B‑1 stimmt. |
| 1a | Lässt sich der Befund an Projekt 1024 nachrechnen? | **Nein, nicht mit der Testdatenbank.** Sie führt zu keinem Projekt Stundenreihen; ohne sie ist der KWK-Eigenverbrauch nicht bestimmbar und die Befreiung wird mit Begründung auf 0 gesetzt. Zusätzlich liegt das Öl-BHKW des Projekts 1024 über dem CO₂-Grenzwert von 270 g/kWh. Messbar ist die Sache an Projekt 1030 (zwei Gasmodule, 50 kW und 9 kW) mit flachen Stundenreihen aus den Jahressummen des gespeicherten Laufs. |
| 2 | Lebt die Doppelmeldung § 9b aus `RechneAufschlaege` weiter? | **Nein.** `RechneAufschlaege` ist mit #313 vollständig entfallen (0 Treffer im Bestand). Die § 9b-Hinweise der Kohärenzprüfung schließen einander aus: Fall 2 kehrt zurück, bevor Fall 3 geprüft wird. Nichts zu tun. |
| 3 | Wie viele nackte deutsche Anzeigetexte stehen noch in der Wirtschaftlichkeit? | Die im Konzept genannten 63 Literale der WinForms-Masken sind mit B5b nach Razor gewandert. Gemessen über alle Dateien von `EPOS.UI/Dialoge/Wirtschaftlichkeit/`, `WindowsFormsApplication1/Views/Wirtschaftlichkeit/` und `WirtschaftlichkeitSeite.razor` blieben **7** — vier Parametervorgaben der Seite, die AW-Zeile der PV-Vergütung und zwei Rückfallkonstanten der Verlaufshülle. Die Auflöser-Herleitungen („BHKW ‚Modul 1'", „alle Heizkessel-Module") lagen ebenfalls noch deutsch im Quelltext. |
| 4a | K10 — Hilfsenergie-Bemessung doppelt, Seed gegen Altkatalog | **Erledigt, vor B6.** `PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN` stehen im Seed nur noch mit `FuerBetrieb = false` (Anzeige von Bestandsdaten, keine Neuauswahl); abgelöst hat sie `PROZENT_ENDENERGIEKOSTEN`. Die Eskalation zieht seit FX4‑b gleich (beide mit p_E). In der Testdatenbank: 0 Vorlagenzeilen, 1 Bestandszeile eines Projekts, die weiter gerechnet wird. Nichts zu tun. |
| 4b | Punkt 6 — 1.000‑kg‑Satz bei Literabrechnung, `density` leer | **Klar abgegrenzt, umgesetzt.** `JeTausendEinheiten` lieferte `null`, und Fall 4 schwieg dazu. Neu eine eigene Hinweiszeile. |

## 2. Bau

### M‑3 — der Modus

* **Schemaschritt 88**: `Tab_ProjektWirtschaftlichkeit.Stromst_Befreiung_Modus` (TEXT(20),
  Werte `AUSWEIS`/`ERLOES` in `DbWerte`, **kein DML**). Eine Quelle
  (`SchemaKatalog.Schritt88_StromsteuerModus`), drei Leser (`SchemaMigration`,
  `Werkzeuge/Testdatenbankschema`, `EPOS.Kern.Tests/TestDatenbank`),
  `SchemaStand.Zielversion` 87 → 88.
* **NULL heißt AUSWEIS**, und nur der ausdrückliche Wert `ERLOES` bucht. Eine nicht
  migrierte Datenbank verhält sich damit wie eine migrierte und wie die Vorgabe; ein
  unbekannter Bestandswert gilt als AUSWEIS.
* **Rechenwirkung**: Im Modus AUSWEIS wird die Befreiung gerechnet und in
  `StromsteuerBefreiungJahr1` gezeigt, aber **nicht** als Erlösreihe angehängt. Die Zeile der
  Vergleichstabelle heißt dann „Stromsteuer-Befreiung [€/a] (Ausweis, nicht im Kapitalwert)".
  Im Modus ERLOES bleibt alles wie bis B5, dazu kommt die Kohärenzwarnung zur Doppelzählung.
* Der Modus wandert **mit ins Ergebnis** (`Tab_ErgebnisWirtschaftlichkeit.StromsteuerBefreiungModus`):
  Ein gespeicherter Lauf muss auch nach einer späteren Umstellung des Projekts sagen können,
  wie *er* gerechnet hat.
* **Dialog**: Das Modusfeld der Gruppe 4 ist offen, steht auf AUSWEIS und schreibt mit OK in
  den Parametersatz. Die Herleitungszeile nennt kein Etappenkürzel mehr, sondern sagt, was
  die beiden Modi bedeuten und wann ERLOES richtig ist.

### Kohärenz-Nachträge

* **Fall 1, die positive Nennung** (Punkt 7): neue Schwere `BESTAETIGUNG` und die Zeile
  „Energiesteuer: Wahl und Preisanteil stimmen überein (…)" — ohne Betrag, in der Oberfläche
  grün mit Haken (`WarnStufe.Erfolg`), in der Windows-Liste mit „✓". Die Bedingung ist die
  genaue Umkehrung der Fälle 2 und 3.
* **Doppelzählung bei ERLOES**: eigene Warnung, unabhängig vom Preisanteil — der Einwand gilt
  dem Buchen als Erlös überhaupt.
* **Unvergleichbare Einheit** (Punkt 6): Steht der Katalogsatz je 1.000 kg und rechnet das
  Projekt je Liter, sagt Fall 4 jetzt, dass er ohne Dichte nicht prüfen kann.
* **Eine Zeile entfällt**: die zweite Warnung zu § 9 Abs. 1 Nr. 3 in Fall 2 („wird als Erlös
  gebucht, obwohl der Preis die Stromsteuer nicht ausweist"). Die neue Doppelzählungszeile
  sagt dasselbe und mehr; zwei Warnungen zu einer Sache sind keine doppelte Sorgfalt. Im
  Modus AUSWEIS wäre die alte Zeile ohnehin falsch gewesen.

### Lokalisierung (K11)

Alle sieben verbliebenen Literale und die Auflöser-Herleitungen gehen über `MyResource`
(beide Sprachen). Neu bewacht von `EPOS.Kern.Tests/LokalisierungWirtschaftlichkeitWacheTests`:
Die Wache misst die Quelltexte der beiden Wirtschaftlichkeitsordner, der Seite, des Auflösers
und der Kohärenzprüfung und meldet jede deutsche Zeichenkette, neben der kein Schlüssel steht.
Rückfalltexte in `T("SCHLUESSEL", "…")` und Rückfallkonstanten gelten als lokalisiert, SQL-Texte
und Datumsmuster fallen nicht darunter.

**Nebenbefund**: Die Parametervorgaben des Kapitalwert-Verlaufdialogs folgen seit dieser
Umstellung der Oberflächensprache. `EPOS.UI.Tests/Dialoge/KapitalwertVerlaufDialogTests`
erwartet deutsche Texte und war damit auf dem CI-Läufer (en‑US) rot; die Klasse nutzt jetzt
die Hausvorrichtung `EposBunitContext`, die die Kultur pinnt — wie jede andere Dialogklasse.

## 3. Zahlen — Projekt 1030, Szenario Erwartet

Stundenreihen flach aus den Jahressummen des gespeicherten Laufs: Strombedarf 4.790,09 MWh/a,
BHKW-Erzeugung 432,30 MWh/a (in jeder Stunde unter dem Bedarf, also vollständig
Eigenverbrauch). Zinssatz 3 %, Betrachtungszeitraum 20 a.

| Größe | vorher (Erlös) | nachher (Ausweis, Vorgabe) |
|---|---:|---:|
| Ausweis `StromsteuerBefreiungJahr1` | 8.862,15 €/a | 8.862,15 €/a |
| Kapitalwert | −21.763.530,86 € | −21.895.377,28 € |
| Differenz | | **131.846,41 €** |

Der Ausweisbetrag ist Regelsatz × KWK-Eigenverbrauch × Anteil = 20,50 €/MWh × 432,30 MWh × 1,0.
Die Differenz ist der Rentenbarwert der flachen Reihe: 8.862,15 € × 14,877475 = 131.846,4 €.
Damit ist belegt, dass AUSWEIS die **ganze** Reihe herausnimmt und nicht nur ihr erstes Jahr.

**Bestandswirkung**: Kein gespeicherter Lauf der Testdatenbank bucht diese Reihe (die
Befreiung ist überall 0), und die Referenzbasis führt keine Geldgröße — der Referenzlauf
bleibt unverändert. Ein Projekt mit Stundenreihen, erfüllten Bedingungen und gebuchter Reihe
verliert mit der Vorgabe AUSWEIS genau deren Barwert aus dem Kapitalwert; das ist die im
Konzept angekündigte Ergebniswirkung.

## 4. Prüffälle und Gegenproben

| Prüfstand | Inhalt |
|---|---|
| `EPOS.Kern.Tests/StromsteuerBefreiungModusTests` | 10 Fälle: Schemaschritt 88, leere Spalte im Bestand, Vorgabe bei NULL, unbekannter Wert = AUSWEIS, Speichern/Laden beider Modi, Ausweisbetrag gleich in beiden Modi, Kapitalwert nur bei ERLOES höher, Differenz = Barwert der Reihe, Doppelzählungswarnung nur bei ERLOES, beide Sprachen |
| `EPOS.Kern.Tests/KohaerenzNachtraegeTests` | 5 Fälle: positive Nennung steht (ohne Betrag, ohne Fall 3 daneben), ohne gewählte Entlastung steht Fall 3 statt ihrer, englische Fassung; unvergleichbare Einheit meldet, passende Einheit meldet nicht |
| `EPOS.Kern.Tests/LokalisierungWirtschaftlichkeitWacheTests` | die Wache selbst |
| `EPOS.UI.Tests/Dialoge/BhkwWirtschaftlichkeitDialogTests` | Modusfeld offen und auf AUSWEIS, schreibt in den Arbeitsstand und nicht in den Parametersatz, Herleitungszeile in beiden Sprachen und ohne „B6" |

**Gegenproben** (eingebaut, gemessen, zurückgebaut):

| Eingriff | Erwartung | gemessen |
|---|---|---|
| Reihe im Rechner unbedingt anhängen (`if (true)`) | AUSWEIS-Fälle rot | 2 Fälle rot |
| `DoppelzaehlungBefreiung` aus der Prüfkette aushängen | Warnfälle rot | 2 Fälle rot |
| nacktes deutsches Literal in `BhkwWirtschaftlichkeitTexte` | Lokalisierungswache rot | rot, mit Datei und Zeile |

Dazu zwei Gegenproben, die dauerhaft im Prüfstand bleiben: die positive Nennung verschwindet
ohne gewählte Entlastung, und die Einheitenmeldung verschwindet bei passender
Abrechnungseinheit.

## 5. Abnahme

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler, 5 Warnungen (Schranke 7) |
| `dotnet test WP-Plan.Kern.slnf` | 8.738 Fälle grün, 1 übersprungen — **in beiden Kulturen** (Vorgabe und `LC_ALL=en_US.UTF-8`) |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | 0 Fehler, 5 Warnungen |
| `Proben/ChartProben` | 64 Bilder, 0 Verstöße |
| `Werkzeuge/SqlDialektPruefer` | 1.488 SQL-Texte, **0 Fundstellen** |
| `Werkzeuge/Testdatenbankschema` | 2 Spalten angelegt, zweiter Lauf 0 — wiederholbar; Schemastand 88 |
| `Werkzeuge/ResourceDesigner` | zweiter Lauf +0 |
| Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen `2026-09-16_R8_Heizkessel_Kaskade` | alle fünf PASS, unverändert |

## 6. Offene Punkte

* **Der Befund B‑1 ist an der Testdatenbank nicht nachstellbar**, weil sie zu keinem Projekt
  Stundenreihen führt. Ein Referenzprojekt mit Stundenreihen und erfüllten Bedingungen des
  § 9 Abs. 1 Nr. 3 wäre der einzige Weg, die Vorschrift im Referenzlauf mitzuprüfen; das
  hieße, die Basis neu einzufrieren, und ist deshalb ein eigener Auftrag.
* Die Lokalisierungswache deckt die Wirtschaftlichkeit ab. Die übrigen Seiten des
  Berichtsordners (`BerichtSeite`, `KostenSeite`, `UebersichtSeite`, `BerichteKostenSeite`)
  führen zusammen 35 nackte deutsche Anzeigetexte; sie stammen aus anderen Wellen und kommen
  mit ihrer Welle dazu.

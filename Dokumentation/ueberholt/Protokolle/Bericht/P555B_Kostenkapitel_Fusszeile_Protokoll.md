# Bericht — Kostenkapitel mit Einzelzahl und Fußzeile der Gruppenregel (Protokoll)

Statuszeile #609. Anwenderentscheid vom 29.09.2026, gebaut nach der Fachvorgabe **P555‑B** der Sitzung
„EPOS Plan Wirtschaftlichkeit“
([`P555B_Fachvorgabe_Kostenkapitel_Fusszeile_2026-09-29.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P555B_Fachvorgabe_Kostenkapitel_Fusszeile_2026-09-29.md)):
**Die Einzelzahl bleibt im Kostenkapitel, und dort druckt eine Fußzeile die Gruppenzahl samt Menge.** Damit ist der
Entscheid vom 27.09.2026 (Nach #555 b, umgesetzt mit #591 —
[`BW_Szenario_Gruppenzahl_Protokoll.md`](BW_Szenario_Gruppenzahl_Protokoll.md)) für das Kostenkapitel revidiert; das
Kapitel Wirtschaftlichkeit bleibt bei der Gruppenzahl. Die Gruppenregel gilt **je Lauf** — Stamm, angehakte Versionen,
Referenz —, nicht je Vergleichsgruppe.

Der gültige Stand steht in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md) und in der Wiki-Quelle
„Wirtschaftlichkeit“; hier steht, wie es geworden ist. Ein Opus-5-Agent im Worktree (Zweig
`worktree-agent-a1024794ae2cfdf0b` ab `0e8ea5b`). Kein Schemaschritt, kein Rechenweg berührt, keine Referenzbasis neu
eingefroren, kein neuer Platzhalter, `EPOS.iOS/` und die Windows-Schale nicht berührt.

| Teil | Gegenstand | Commit |
|---|---|---|
| Umbau | Gruppenzahl auf einer Kopie statt Mutation des Standes, Fußzeile je Tafel in Word und Excel, zwei Ressourcen, Tests | `3eeb424` |
| Papiere | Wiki-Quelle, Logbuchsatz, dieses Protokoll, Indexzeile | dieser Commit |

## 1 Befund

- **#591 (`41c30e3e`) stellte den ganzen Berichtsbaum um, nicht nur das Kostenkapitel.**
  `BerichtsDatenSammler.StromGruppenregelAnwenden` setzte am Stand selbst `StromImVergleichBepreisen` und rechnete
  Kosten, Emissionen und Kennzahlen neu. Damit las **jedes** Kapitel, jeder Platzhalter und die Mappe die Gruppenzahl —
  auch die Übersicht, die nach dem Entscheid #555 ausdrücklich bei der Einzelbetrachtung bleibt. Der Bericht wich darin
  von Kostenseite und Übersicht der App ab.
- **Das Kapitel Wirtschaftlichkeit hing nie an dieser Mutation.** Es liest `BerichtsDaten.Wirtschaftlichkeit` bzw. den
  Wertesatz `WirtschaftsBerichtswerte`; deren Zahlen entstehen in `WirtschaftlichkeitCtrl.Szenariodaten` auf einer
  **Kopie** der Variante, die die Gruppenregel schon immer trägt. Die Emissionsbilanz rechnet
  `EmissionsBilanzRechner.Berechne(idProjekt, p)` aus der Datenbank. Beides bleibt ohne die Mutation unverändert — die
  Rücknahme trifft das Kapitel Wirtschaftlichkeit also nicht.
- **Beide Zahlen liegen ohnehin vor** (Fachvorgabe § 2.4): Original am Stand, Gruppenzahl auf der Kopie. Es war nur zu
  entscheiden, welche die Tafel liest und welche die Fußzeile nennt — kein Rechenweg.

## 2 Umsetzung

- **`BerichtsDatenSammler`:** `StromGruppenregelAnwenden` → `StromGruppenzahlErmitteln`. Der Schritt rechnet die
  Gruppenregel auf einer `Kopie()` des Standes — dasselbe Muster wie `WirtschaftlichkeitCtrl.Szenariodaten` — und legt
  allein das Ergebnis als `VariantenDaten.Gruppenzahl` am Stand ab. Der Stand selbst bleibt Zahl für Zahl die
  Einzelbetrachtung: `StromImVergleichBepreisen` steht dort nie mehr, `StrombedarfOhneVerwendungMWh` bleibt gesetzt.
  Mit der Mutation entfallen auch die beiden Nachmeldungen aus #591 (Strommix-Vorgabewert, Leistungspreis ohne
  Bezugsspitze): Sie meldeten Lücken der **geänderten** Standzahlen; die ändern sich nicht mehr. Die zwei dafür
  ausgegliederten Textmethoden bleiben — der Sammler selbst ruft sie.
- **Neue Klasse `StromGruppenzahl`** (in `BerichtsDaten.cs`): Energiekosten [€/a], CO₂ [t/a], Netzbezug [MWh/a] und die
  Anzeigenamen der Stromverwender. `null`, solange die Regel am Stand nicht gewirkt hat.
- **`VariantenDaten.StromGruppenregelHinweis` → `StromGruppenregelFussnote(kultur, gruppe)`**, dazu
  `BerichtsDaten.StromGruppenregelHinweise` → `StromGruppenregelFussnoten(kultur, gruppe)`. Die Fußzeile ist je
  Kennzahlgruppe eine andere: unter der Kostentafel die Energiekosten, unter der Emissionstafel das CO₂; für jede andere
  Gruppe `null`.
- **`Berichtstabellen.Vergleich`:** statt eines Satzes unter beiden Tafeln sammelt der Bauweg je Gruppe deren eigene
  Fußzeilen. Eine Tafel über mehrere Gruppen (`Vergleichsgesamt`, `Vergleichsliste`) trägt beide in Katalogfolge.
- **`BausteineVergleich`:** Die Fußzeilen stehen unter **ihrer** Tafel, nicht mehr gesammelt am Kapitelende.
- **`ExcelBerichtGenerator.BlattVergleich`:** Die Anmerkungszeilen unter dem Blatt „Vergleich“ tragen dieselben zwei
  Sätze; gesammelt wird in der vorhandenen Schleife, gesetzt an der vorhandenen Stelle nach dem Anpassen der Breiten —
  die Änderung bleibt örtlich (Rücksicht auf die parallele Arbeit an der Excel-Vorlage).
- **`KostenEmissionRechner`:** nur Doku — der Berichtslauf setzt das Feld nicht mehr am Stand.

## 3 Wortlaut der Fußzeile

Zwei neue Ressourcen, je vier Stellen (Stand, Stromverwender, Menge, Zahl der Tafel); `Werkzeuge/ResourceDesigner`
nachgezogen. Word und Excel drucken denselben Satz.

**`BV_FUSSNOTE_GRUPPENREGEL_KOSTEN`**

- de: `Strombedarf ohne Verwendung im Stand „{0}“: Im Vergleich mit {1} wird der Netzbezug von {2} MWh/a bepreist und
  bewertet (Gruppenregel) — die Energiekosten betragen dann {3} €/a. Die Tafel weist die Einzelbetrachtung des Standes aus.`
- en: `Electricity demand without use in version "{0}": In the comparison with {1}, the grid purchase of {2} MWh/a is
  priced and assessed (group rule) — the annual energy cost is then {3} €/a. The table shows the individual assessment of
  the version.`

**`BV_FUSSNOTE_GRUPPENREGEL_EMISSION`**

- de: `Strombedarf ohne Verwendung im Stand „{0}“: Im Vergleich mit {1} wird der Netzbezug von {2} MWh/a bepreist und
  bewertet (Gruppenregel) — die CO₂-Emissionen betragen dann {3} t/a. Die Tafel weist die Einzelbetrachtung des Standes aus.`
- en: `Electricity demand without use in version "{0}": In the comparison with {1}, the grid purchase of {2} MWh/a is
  priced and assessed (group rule) — the CO₂ emissions are then {3} t/a. The table shows the individual assessment of the
  version.`

Der Eingangssatz ist der der Hinweiszeile der Wirtschaftlichkeit (`WIRT_HINWEIS_STROM_GRUPPENREGEL`) — dieselbe
Auskunft, um die Zahl der Tafel erweitert. Die Zahlformate sind die der Tafelzeilen: `N0` für die Energiekosten, `N1`
für CO₂ und die Menge.

## 4 Prüfungen

- **Messlatten byte-gleich**, wie in der Fachvorgabe § 3 vorhergesagt: `Bericht_Word_1030.txt`,
  `Bericht_Word_1030_Vorlage.txt`, `Bericht_Excel_1030.txt`, `Bericht_Word_Gruppe.txt`, `Bericht_Word_Gruppe_Vorlage.txt`,
  `Bericht_Excel_Gruppe.txt` — 1030 ist ein Einzelstand, die Gruppenprobe führt keinen Stromverwender; die Regel wirkt in
  beiden nicht. `BerichtVorlagenMesslatteTests` grün, keine Messlatte angefasst.
- **`StromGruppenregelTests`** am Prüfstand aus #555 (1027 ohne Wärmepumpe, 1029 als Variante mit Stromverwendung),
  14 Fälle grün. Die drei Fälle aus #591 sind umgewidmet, nicht gelöscht:
  - *Im_Bericht_zeigt_das_Kostenkapitel_die_Einzelzahl_mit_Fussnote* — Zelle trägt die Einzelzahl und **nicht** die
    Gruppenzahl; Kostentafel und Emissionstafel tragen je eine eigene Fußzeile mit Zahl und Menge; keine andere Gruppe
    trägt eine; Gesamttafel und Blatt „Vergleich“ tragen beide; das Kapitel Wirtschaftlichkeit rechnet auf demselben Baum
    weiter die Gruppenzahl (ausdrücklich `NotEqual` gegen die Standzahl); Englisch aus der Ressource.
  - *Ein_Bericht_mit_einem_Stand_behaelt_die_Einzelzahl_ohne_Fussnote* und
    *Ohne_Stromverwendung_in_der_Gruppe_gibt_es_keine_Fussnote* — keine Gruppenzahl, keine Fußzeile.
  - neu: *Im_Wortbericht_steht_jede_Fussnote_unter_ihrer_Tafel* (echtes `.docx` über `WordBerichtGenerator`, Fußzeile der
    Emissionen vor der der Kosten, dazwischen die Kostentafel) und *Die_Fussnoten_kommen_aus_den_Ressourcen*.
  - Deutsche Texte mit gepinnter `Kulturvorrichtung`; alle Datenbankgriffe an der Arbeitskopie der `TestDatenbank`.
- **Gate:** `dotnet build WP-Plan.Kern.slnf -c Release` 0 Fehler; `EPOS.UI.Tests` vollständig grün; in `EPOS.Kern.Tests`
  die Klassen um Bericht, Excel, Kosten, Emission, Gruppenregel, Wirtschaftlichkeit, Lokalisierung, Kennzahlen und
  Vorlagen sowie die Wachen `DokumentationLinkWache`, `RepositoryOrdnungWache`, `WikiProduktdatenWache` grün.
- **Kein Referenzlauf**: Kosten- und Emissionsrechner sind unverändert; geändert hat sich allein, welche vorhandene Zahl
  gelesen wird.

## 5 Papiere

- **Wiki-Quelle „Wirtschaftlichkeit“**, Abschnitt am Anker `bericht-gruppenregel`: neu gefasst — die Regel gilt je Lauf
  (Stammprojekt, angehakte Versionen, Referenz), die Kapitalwertmethode rechnet mit der Gruppenzahl, das Kapitel
  Variantenvergleich zeigt die Einzelbetrachtung, und die Fußzeile nennt daneben Zahl und Menge.
  „Berichtsvorlagen“ und „Kosten“ beschreiben das Kostenkapitel nicht — beide bleiben unberührt; die Zeile zur
  Szenarioabdeckung (Anker `szenarioabdeckung`) nennt den Lauf bereits richtig.
- **Logbuch** `aktuell/Wiki_Update_2026-09-26.md`, Abschnitt „Version 1.2.0.5“: der Satz aus #591 zur Gruppenregel im
  Kostenkapitel an derselben Stelle ersetzt; die Version bleibt 1.2.0.5 (Anwenderentscheid).
- **Nicht angefasst:** `aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` und das
  Entscheidungsregister — § 6.5 und EZ‑15/EZ‑16 schreibt die Welle P555.

## 6 Offene Punkte

- **Die Statusnummer `#609`** steht in diesem Protokoll, im Logbuchsatz und in der Indexzeile als Platzhalter; sie wird
  mit der Statuszeile vergeben (bei Übergabe waren #602 und #603 belegt). Statuszeile und Nach-Blöcke schreibt die
  Orchestrierung: Der Block „Nach #555“ (b) trägt die Revision vom 29.09.2026 bereits und bekommt das „erledigt mit
  #609“; „Nach #603“ (a) steht auf „in Umsetzung“ und ist auf umgesetzt zu setzen, danach § 6.5 und EZ‑16 durch die
  Wirtschaftlichkeit.
- **Der Wiki-Upload steht aus** (gebündelte Veröffentlichung): geänderte Seite „Wirtschaftlichkeit“, dazu der
  Logbuch-Eintrag unter 1.2.0.5.
- **Nur unter Windows prüfbar:** die Sichtprüfung des gedruckten Berichts in Word und Excel — Absatzstil „Hinweis“ der
  Fußzeile im Wortbericht und Umbruch der Anmerkungszeile unter dem Blatt „Vergleich“ bei schmaler Spalte A. Der
  Linux-Lauf prüft Text und Ort der Zeile, nicht ihr Aussehen.

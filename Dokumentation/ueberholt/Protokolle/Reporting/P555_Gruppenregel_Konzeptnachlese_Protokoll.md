# P555 — Konzeptnachlese Gruppenregel und Szenarioabdeckung je Lauf (Protokoll, 29.09.2026)

Statuszeile folgt (#603 vorläufig) in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Auftrag
[`P555_Auftrag_2026-09-29.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P555_Auftrag_2026-09-29.md) der Sitzung „EPOS Plan
Wirtschaftlichkeit" mit dem Prüfbefund [`p555_befund.md`](../Auftraege_Wirtschaftlichkeit_2026-09/Fakten/p555_befund.md);
Fachvorgabe für die Berichterstellung
[`P555B_Fachvorgabe_Kostenkapitel_Fusszeile_2026-09-29.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P555B_Fachvorgabe_Kostenkapitel_Fusszeile_2026-09-29.md).
Vorgänger: [`VG1_Strom_ohne_Verwendung_Gruppenregel_Protokoll.md`](VG1_Strom_ohne_Verwendung_Gruppenregel_Protokoll.md)
(#555) und [`BW_Szenario_Gruppenzahl_Protokoll.md`](../Bericht/BW_Szenario_Gruppenzahl_Protokoll.md) (#591). Zweig `p555`
ab `01114eabc`.

## Anlass

Die Statuszeile #555 hat die Gruppenregel „Strombedarf ohne Verwendung" nur ins Schwesterpapier
`Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md` geschrieben; das konsolidierte Konzept und das Register kannten weder die
Regel je Stand (#433) noch die Gruppenregel (#555). Der Prüfbefund vom 29.09.2026 hat die Konzeptsätze vorgeschlagen und
zwei Fragen an den Anwender gestellt: den Ausweis im Bericht und die Geltungsmenge.

## Befund

- **Konzepttreue:** kein Widerspruch, eine Lücke — es fehlten Sätze in § 3.5, § 3.8, § 2.11.5, § 2.15, § 6.2, § 6.3,
  § 6.5 und die Registerzeilen.
- **Geltungsmenge — die Seite zählte gruppenweit.** Der Lauf bestimmt die Gruppenregel über seine Stände
  (`WirtschaftlichkeitCtrl.StromGruppenregel` über `daten.Varianten`; die Seite reicht die angehakten Varianten samt
  Referenz an `BerichtsDatenSammler.Sammle`, der den Stamm immer führt), und der Bericht zählt die Szenarioabdeckung über
  dieselbe Menge (`WirtschaftlichkeitBewertung.FuerBericht`). Die Ergebnisseite zählte dagegen über die ganze
  Vergleichsgruppe (`WirtschaftlichkeitSeiteGaben.Szenarioabdeckung`, Schleife über `_gruppe`, der Kommentar sagte „über
  die GANZE Vergleichsgruppe, nicht über die Wahl"), und das Feld stand am Stand, folgte einem Haken also erst beim
  nächsten Laden.
- **Nebenbefund der Analyse:** Den Auslieferungsträger, den die Gruppenregel einem Stand ohne eigene Stromverwendung
  beisteuert, zählte der Ausweis nicht als Stromträger (`SzenarioAbdeckung.IstStromtraeger` kennt nur zugeordnete
  Träger) — sein Leistungspreis fehlte in m, obwohl der Vergleich ihn rechnet.
- **Anker:** keiner bewegt; in der Testdatenbank verwendet jeder Stand jeder Vergleichsgruppe Strom.

## Entscheide

- **EZ‑15** (29.09.2026, nach Empfehlung): Die Gruppenregel gilt **je Lauf** — Stamm, angehakte Varianten und gewählte
  Referenz, nicht die ganze Vergleichsgruppe aus `Tab_Variante`; die Szenarioabdeckung der Ergebnisseite wird angeglichen.
- **EZ‑16** (29.09.2026, nach Empfehlung des Befunds § 5 (c) 1): „Einzelzahl im Kostenkapitel lassen und dort eine
  Fußzeile mit Gruppenzahl und Menge drucken"; das Kapitel Wirtschaftlichkeit behält die Gruppenzahl. Der Entscheid
  revidiert den vom 27.09.2026 (Nach #555 (b), gebaut #591) für das Kostenkapitel. Bis zu diesem Entscheid galt für den
  Auftrag der Stand #591; die Konzeptsätze beschreiben jetzt EZ‑16 als „in Umsetzung".
- **Abgabe:** Den Bau von EZ‑16 übernimmt die Cloud-Sitzung „EPOS-Plan Berichterstellung" nach der Fachvorgabe P555‑B
  (Tafeln Kosten und Emissionen des Variantenvergleichs mit der Einzelzahl, Fußzeile je Stand mit Gruppenzahl, Menge und
  Stromverwender, Word und Excel, eigene Ressourcen de/en; Messlatten byte-gleich). P555 ändert keinen Berichtscode; die
  Wirtschaftlichkeit setzt § 6.5 und EZ‑16 nach dem Push der Berichterstellung auf „umgesetzt".

## Code (Commit `76934a9e9`)

- `EPOS.UI.Daten/Wirtschaftlichkeit/WirtschaftlichkeitSeiteGaben.cs`: `Laufstaende()` — Stamm, angehakte Varianten und
  wirksame Referenz in der Folge der Gruppe; `Szenarioabdeckung()` zählt darüber, mit einem Zwischenspeicher je Sprache
  und Ständen (`Laden` verwirft ihn); der Ausweis entsteht in `Ansicht()`.
- `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitDaten.cs`: `ErgebnisAnsicht.Szenarioabdeckung` statt
  `WirtschaftlichkeitStand.Szenarioabdeckung` — ein Haken tauscht die Ansicht und damit den Ausweis, die Szenario-Klappliste
  nicht; `WirtschaftlichkeitSeite.razor` (beide Orte) und `AnhangEChecklisteKnopf.razor` lesen die Ansicht.
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/SzenarioAbdeckung.cs`: `TraegerMitVerbrauch` meldet den Auslieferungsträger der
  Gruppenregel, er zählt als Stromträger (Leistungspreis immer).
- Kein Rechenweg, kein Schemaschritt, keine neue Ressource, kein Designerlauf.

## Tests

- `EPOS.Kern.Tests/StromGruppenregelTests` +2: `Die_Szenarioabdeckung_zaehlt_den_Stromtraeger_nach_der_Gruppenregel`
  (Prüfstand 1027 ohne Wärmepumpe neben 1029: m im Lauf = m allein + m der Variante − 11 + 3, Gegenprobe ohne
  Stromverwender) und `Die_Szenarioabdeckung_der_Seite_folgt_dem_Haken_der_einzigen_Stromvariante` (Gruppe 1026, Stamm
  und 1027 ohne Stromverwender: alle angehakt gleich `SzenarioAbdeckung.Lesen` der drei Stände, 1029 abgehakt gleich der
  Zählung von zweien und verschieden, Unterschied = Parameter von 1029 + 2 × 3, wieder angehakt gleich dem Laden). Beide
  rot gegen den alten Stand (22 erwartet, 21 gezählt; „0 von 15" erwartet, „0 von 25" gezeigt).
- `EPOS.UI.Tests/Seiten/WirtschaftlichkeitErgebnisansichtTests` +1 (bUnit):
  `Der_Ausweis_folgt_dem_Haken_und_nicht_der_Klappliste` — Klappliste behält den Ausweis, der Haken tauscht ihn, auch in
  Block 4 der ValERI-Bewertung.
- Umgestellt: `ErgebnisansichtTests` (Ausweis an der Ansicht) und die Probendaten der bUnit-Klasse.
- Läufe im Worktree (Debug, Schalter `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`): Kern-Filter
  0 Fehler; EPOS.Kern.Tests gefiltert (SzenarioAbdeckung, StromGruppenregel, BerichtVorlagenMesslatte, Wirtschaftlichkeit,
  Ergebnisansicht, DokumentationLinkWache, RepositoryOrdnungWache, LokalisierungWirtschaftlichkeitWache,
  HuellenTextschluesselWache, WikiProduktdatenWache, AnhangECheckliste) 301/301, darin `BerichtVorlagenMesslatteTests`
  7/7 (sechs Bericht-Messlatten byte-gleich) und `DokumentationLinkWacheTests` 9/9; EPOS.UI.Tests gefiltert
  (Wirtschaftlichkeit, AnhangECheckliste, AnhangEStellen) 271/271; Windows-Schale 0 Fehler.

## Papiere

- Konzept: § 2.11.5 (Zählung über die Stände des Laufs), § 2.13 (5) (Verweis § 6.3 Nr. 37), § 2.15 (Gruppenregel je
  Lauf), § 3.5 (Gruppenregel, Emissionsfaktor), § 3.8 (§ 9b-Menge), § 6.1 (Zeile P555), § 6.2 (kein Anker, Wache),
  § 6.3 Nr. 37–39, § 6.5 (Einzelzahl und Gruppenzahl, Kostenkapitel in Umsetzung).
- Register: Kopf, Familientafel, R‑EZ mit EZ‑13 bis EZ‑16, E31‑A1 (§ 6.3 Nr. 37).
- `Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`: ein Verweissatz auf § 3.5.
- Statusdatei: Nach #555 (b) mit dem Revisionsvermerk, (c) und (d) → § 6.3 Nr. 38/39.
- Wiki-Quelle `Programm Dokumentation - Wirtschaftlichkeit.wiki`, Anker `szenarioabdeckung`: gezählt werden die
  angehakten Versionen samt Referenz, der Stromträger nach der Gruppenregel. Den Absatz `bericht-gruppenregel` schreibt die
  Berichterstellung mit P555‑B fort.

## Offen

- § 6.3 Nr. 37 (Vorlagenweg), Nr. 38 (Leistungspreis des Auslieferungsträgers samt Bezugsspitze), Nr. 39
  (Stromsteuer-Kohärenz).
- Nach einem Haken zeigt die Seite die gespeicherten Ergebnisse des letzten Laufs weiter; hat der Haken die Gruppenregel
  verändert (die einzige Variante mit Stromverwendung abgehakt), tragen Energiekosten und Kapitalwert der übrigen Stände
  bis zum nächsten „Berechnen" noch die Gruppenzahl — die Statuszeile nennt das nicht. Der Ausweis zählt schon die neue
  Menge.
- Ein Stand **mit** eigener Stromverwendung ohne zugeordneten Stromträger zählt im Ausweis keinen Stromträger, obwohl der
  Netzbezug mit dem Auslieferungsträger bepreist wird (Zählregel E9b, `TraegerMitVerbrauch`); in der Testdatenbank etwa
  1026 und 1029. Nicht angefasst.

## Gate

offen

## Commit

offen (Code `76934a9e9` auf `p555`)

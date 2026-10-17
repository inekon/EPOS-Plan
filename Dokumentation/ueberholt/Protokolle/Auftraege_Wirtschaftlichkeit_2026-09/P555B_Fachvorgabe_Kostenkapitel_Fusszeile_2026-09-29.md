# P555‑B Fachvorgabe: Kostenkapitel des Berichts mit Einzelzahl und Fußzeile der Gruppenregel

Stand 29.09.2026, Sitzung „EPOS Plan Wirtschaftlichkeit". Bauplatz: Cloud-Sitzung „EPOS-Plan Berichterstellung", die das
Kostenkapitel mit #591 nach der Gruppenregel umgestellt hat. Fachliche Führung, Konzeptsätze (§ 6.5) und Abnahme bei der
Wirtschaftlichkeit; die Konzeptsätze schreibt die Welle P555 (Statuszeile #603) als „in Umsetzung".

## 1. Anlass und Entscheide

- **#555** (Dialoge und Korrekturen, 26.09.2026): Gruppenregel — verwendet ein Stand des Vergleichs Strom, bepreisen und
  bewerten alle Stände ihren Netzbezug (Kosten wie Emissionen). Die Einzelbetrachtung eines Standes (Kostenseite, Übersicht)
  bleibt je Stand: ohne stromverwendenden Erzeuger kostet der Netzbezug dort nichts („Einzelzahl"); im Vergleich trägt derselbe
  Stand die „Gruppenzahl" (Brennstoff plus bepreister Netzbezug), und nur sie geht in den Kapitalwert ein.
- **#591** (Berichterstellung, Anwenderentscheid 27.09.2026, Nach #555 (b)): Das Kostenkapitel des Berichts weist Kosten und
  Emissionen nach der Gruppenregel aus wie der Variantenvergleich der Wirtschaftlichkeit, samt Hinweis.
- **Anwenderentscheid 29.09.2026** (nach Empfehlung des Prüfbefunds `Fakten/p555_befund.md`, § 5 (c) 1): **Einzelzahl im
  Kostenkapitel lassen und dort eine Fußzeile mit Gruppenzahl und Menge drucken.** Damit ist der Entscheid vom 27.09. für das
  Kostenkapitel revidiert; das Kapitel Wirtschaftlichkeit bleibt bei der Gruppenzahl (#591).

## 2. Regeln

1. **Kostenkapitel:** Die Tafeln Kosten und Emissionen des Variantenvergleichs im Bericht (Kennzahlgruppen „Kosten" und
   „Emissionen", `Berichtstabellen.cs`, `KennzahlenKatalog.cs`) lesen wieder die Einzelzahl je Stand (das Original ohne
   bepreisten Netzbezug) — wie Kostenseite und Übersicht der App. Die Umstellung aus `41c30e3e` wird für diese Tafeln
   zurückgenommen.
2. **Fußzeile:** Hat die Gruppenregel für einen Stand gewirkt (`StromGruppenregelMWh` > 0), steht unter der Kostentafel und
   unter der Emissionstafel je betroffenem Stand eine Fußzeile: Stand, Zahl mit bepreistem Netzbezug (Energiekosten in €/a
   bzw. CO₂ in t/a), Menge des Netzbezugs in MWh und der Stromverwender, der die Regel auslöst — dieselbe Auskunft wie der
   Hinweis `WIRT_HINWEIS_STROM_GRUPPENREGEL`, als eigene Ressourcen de/en (Word und Excel; in Excel als Anmerkungszeile unter
   der Tafel). Wirkt die Regel nirgends, gibt es keine Fußzeile.
3. **Kapitel Wirtschaftlichkeit:** unverändert nach #591 — Gruppenzahl in Kennzahltafel, Mehrjahresübersicht und Verlauf,
   Hinweis benennt den Unterschied. Die App bleibt unverändert.
4. **Kein Rechenweg, kein Schemaschritt.** Die Zahlen sind vorhanden (Original und Kopie je Stand); es ändert sich nur, welche
   Zahl die Tafeln lesen und welche die Fußzeile nennt.

## 3. Prüfungen

- **Messlatten byte-gleich:** `EPOS.Kern.Tests/Messlatten/Bericht_Word_1030*.txt`, `Bericht_Excel_1030.txt`, `Bericht_*_Gruppe*.txt`
  — 1030 ist ein Einzelstand, die Gruppenprobe hat keinen Stromverwender; die Regel wirkt in beiden nicht.
- **Neue Probe** am Prüfstand von `StromGruppenregelTests` (Wärmepumpe aus 1027 entfernt): Kostentafel und Emissionstafel des
  Kostenkapitels tragen die Einzelzahl, je eine Fußzeile nennt Gruppenzahl und Menge; das Kapitel Wirtschaftlichkeit trägt
  die Gruppenzahl; ohne Wirkung der Regel keine Fußzeile. Word und Excel. Deutsche Texte mit gepinnter `Kulturvorrichtung`.
- Linux-Gate komplett (Referenzlauf unberührt).

## 4. Papiere und Zuständigkeiten

- **Berichterstellung:** Statuszeile mit der nächsten freien Nummer nach `git fetch` (#602 Dialog Design, #603 P555 sind
  vergeben); Nach #555 (b) auf „revidiert 29.09.2026: Einzelzahl mit Fußzeile, erledigt mit #6xx"; Logbuch: der Satz aus #591
  zur Gruppenregel im Kostenkapitel wird ersetzt durch „Im Kostenkapitel des Berichts steht je Stand die Kostenzahl der
  Einzelbetrachtung; wo die Gruppenregel wirkt, nennt eine Fußzeile die Zahl mit bepreistem Netzbezug und die Menge" (Version
  nach Anwenderangabe); Wiki-Quellen Wirtschaftlichkeit und Berichtsvorlagen, falls sie das Kostenkapitel beschreiben;
  Protokoll. Push nur `ios_migration_september`.
- **Nicht anfassen:** `Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` und das Entscheidungsregister (P555 schreibt
  § 6.5 und EZ‑15/EZ‑16, die Wirtschaftlichkeit setzt sie nach dem Push auf „umgesetzt").
- **Abnahme (Wirtschaftlichkeit):** Probe am Prüfstand lokal grün, Messlatten byte-gleich, Sichtprüfung der Fußzeile im
  Word-Text der Probe.

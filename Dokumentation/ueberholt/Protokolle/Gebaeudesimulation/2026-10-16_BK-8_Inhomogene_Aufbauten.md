# Protokoll BK-8 — Inhomogene Aufbauten im Bauteilkatalog-Import

Auftrag BK-8 (Anwenderentscheid 16.10.2026) zum [Entwurf Bauteilkatalog](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md)
(Abschnitt 5.3). Arbeitsort Worktree `bk8`, Zweig `bk8`, Basis `85f7272fa`. Kein Schemaschritt, keine Änderung am
Rechenweg des Kerns.

## Befund

- Der echte Import beim Anwender (nur Zählungen) verwarf 50 Aufbauten: 7 mit „inhomogen: Abschnitt ohne Flächenanteil“,
  43 mit „inhomogen: Abschnitte mit verschiedener Schichtzahl“; 209 Aufbauten mit 538 Abschnitten wurden übernommen.
- Beide Gründe entstehen in `BauteilkatalogImport.AbschnittePruefen` (Planung, ohne Datenbank). Die Schichtzahl wurde vor dem
  Flächenanteil geprüft, ein Aufbau mit beiden Lücken zählte also unter der Schichtzahl.
- Der Kern (`InhomogeneSchichten`, Rechenschritte 3.1, B0a–B0d) bildet Ebenen aus der Listenfolge: Schichten mit aufsteigender
  Abschnittsnummer liegen nebeneinander, jede inhomogene Ebene führt jeden Abschnitt genau einmal. Der Katalog speichert dafür
  je Schicht `ID_Abschnitt` und als `Reihenfolge` die Ebene; der Leser sortiert nach Reihenfolge, dann Abschnittsnummer. Der
  Import setzt die Reihenfolge je Abschnitt von 1 bis n — die k-te Schicht jedes Abschnitts ist die Ebene k. Mit verschiedener
  Schichtzahl ist das im Modell nicht darstellbar; deshalb die Ablehnung.
- DIN EN ISO 6946, 6.7 schließt verschiedene Schichtfolgen je Abschnitt nicht aus: Der obere Grenzwert R'_T nimmt je Abschnitt
  die eigene Schichtfolge, der untere R''_T setzt jede Ebene parallel zur Oberfläche isotherm und braucht dafür Schichten, die
  innerhalb eines Abschnitts thermisch homogen sind — eine Schicht wird an den Grenzen der übrigen Abschnitte unterteilt
  (6.7.2.3). Für d/λ-Schichten ist das Teilen exakt: Die Summe der Teilwiderstände ist der Widerstand der Schicht.

## Entscheide

1. **Verschiedene Schichtzahl (a).** Haben alle Abschnitte dieselbe Gesamtdicke, werden die Schichtgrenzen vereinigt und die
   Schichten geteilt; das ergibt ein Modell im bestehenden Ebenenformat, der Kern rechnet es unverändert. Toleranz der
   Gesamtdicken: max(1 mm; 1 % der größten) — die Quelle führt Millimeter, Rundungen je Schicht summieren sich, und 1 %
   entspricht der Toleranz der Flächenanteile. Die Grenzen werden je Abschnitt in der auf seine Gesamtdicke **genormten Lage**
   vereinigt, nicht absolut: Jeder Abschnitt behält Dicke und Widerstand (R'_T unverändert), und eine Folie an der Oberfläche
   eines etwas dickeren Abschnitts erzeugt keine Ebene, die anderen Abschnitten fehlt. Grenzen gelten unter 1e-9 (genormt) als
   gleich; dünne Schichten (Folien) bleiben erhalten. Neue, benannte Ablehnungen: Gesamtdicken außerhalb der Toleranz; eine
   Schicht ohne Dicke (eine Ebene der Dicke null hätte in den übrigen Abschnitten den Widerstand null); eine **ruhende
   Luftschicht** (Luftschicht ohne λ), in die eine fremde Grenze fiele — ihr Widerstand folgt der Tabelle 8 nach der Dicke und
   ist nicht anteilig teilbar. Fällt keine Grenze in sie, bleibt sie ganz und der Aufbau wird übernommen. Eine Luftschicht mit
   äquivalentem λ rechnet d/λ und wird geteilt. Ein Teil trägt den Dickenanteil auch von `R_Quelle`.
2. **Fehlender Flächenanteil (b).** Fehlt er (leer oder 0) bei genau einem Abschnitt und bleiben bis 1 mehr als 0,01 offen,
   wird f = 1 − Σ eingesetzt und als Hinweis gezählt. Bleibt höchstens die Toleranz offen, ist der Anteil unbestimmt (die
   Summe stimmt schon) — weiter Ablehnung, ebenso bei zwei und mehr fehlenden Anteilen. Der Flächenanteil wird jetzt vor der
   Schichtzahl geprüft.
3. **Gleiche Schichtzahl (c).** Unverändert: Schicht k neben Schicht k, auch wenn die Grenzen der Abschnitte versetzt liegen.
   Die 209 übernommenen Aufbauten planen und rechnen damit wie zuvor (Test). Das Teilen versetzter Grenzen auch bei gleicher
   Schichtzahl wäre normnäher, änderte aber diese Aufbauten und bleibt offen.
4. **Kein Schemaschritt.** Die Teilschichten sind gewöhnliche Schichtzeilen mit `ID_Abschnitt` und der Ebene als Reihenfolge.

## Ergebnis

- `EPOS.Kern/Allgemein/Import/Bauteilkatalog/BauteilkatalogImport.cs`: `AbschnittePruefen` mit `AnteilePruefen`,
  `Angleichen` (Vereinigung der Grenzen, Teilen) und `Teil`; Toleranzen `DICKE_TOLERANZ_M`, `DICKE_TOLERANZ_ANTEIL`. Die
  Vorschau zählt `SchichtzahlAngeglichen`, `Teilschichten` und `AnteilErgaenzt`; je Aufbau eine Protokollzeile.
- Texte in beiden Sprachen: `BKI_GRUND_DICKEN`, `BKI_GRUND_OHNE_DICKE`, `BKI_GRUND_LUFTSCHICHT`, `BKI_INHOMOGEN` (Hinweiszeile
  der Vorschau), `BKI_PROT_GETEILT`, `BKI_PROT_ANTEIL`; `Resource.Designer.cs` neu erzeugt.
- Hülle `BauteilkatalogImportHuelle` und Dialog `BauteilkatalogImportDialog`: neue Zeile `Inhomogen` der Vorschau.
- Zählung der Quelle: `Schichten.Neu` zählt weiter Quellzeilen; die zusätzlich geschriebenen Teilschichten nennt die
  Hinweiszeile.
- Doku: Wiki-Quelle „Baustoffe und Bauteilaufbauten“ (Abschnitt „Bauteilkatalog einlesen“), Rechenschritte 3.1 (Absatz
  „Gemeinsame Schichtgrenzen beim Katalogimport“), Entwurf BK 5.3.

**Erwartung für die 50 Fälle:** Die Gründe der 50 können sich nur verschieben, nie vermehren. Übernommen werden von den 7 die
mit genau einem fehlenden Anteil und mehr als 1 % Rest, von den 43 die mit passenden Gesamtdicken, ohne Schicht ohne Dicke und
ohne ruhende Luftschicht über einer fremden Grenze, sofern ihre Flächenanteile stimmen (vorher verdeckt). Verbleibende Fälle
zählen unter den neuen Gründen. Die genaue Verteilung zeigt erst der nächste Import beim Anwender (die Quelle liegt nicht im
Repositorium).

## Tests

`EPOS.Kern.Tests/BauteilkatalogInhomogenTests` (8, synthetische Dateien, runde Werte): Teilen und Handrechnung nach 6.7
(R'_T 3,2029, R''_T 2,9681, e 3,8 %), Dickentoleranz angenommen und verworfen, ruhende Luftschicht verworfen und ungeteilt
angenommen, Schicht ohne Dicke verworfen, fehlender Anteil eingesetzt (rechnet wie 0,8/0,2) samt Hinweiszeile der Hülle,
drei Ablehnungen ohne bestimmbaren Rest und die Summenablehnung, gleiche Schichtzahl unverändert, Schreiben und Lesen über
die Testdatenbank mit denselben Grenzwerten.

## Abnahme

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler; Windows-Schale mit `EnableWindowsTargeting`: 0 Fehler.
- `EPOS.Kern.Tests` mit Filter (Bauteil*, *Abschnitt*, Uwert*, *ReferenzprojektWache*, DokumentationLinkWache,
  WikiProduktdatenWache, RepositoryOrdnungWache): 642 grün, 1 übersprungen (Bestand), 0 rot; `EPOS.UI.Tests` Bauteilkatalog*:
  9 grün.
- SQL-Dialekt-Prüfer: 3 009 SQL-Texte, 0 Fundstellen.
- Referenzlauf der achtzehn CI-Projekte gegen `2026-10-14_R55_KaeltebedarfDezentral` (ohne die zwei Kälte-Deckungsgrade):
  GESAMT PASS, 6 398 669 Werte innerhalb der Toleranz.

## Logbuch (Vorschlag)

„Der Bauteilkatalog-Import übernimmt inhomogene Aufbauten auch mit verschiedener Schichtzahl je Abschnitt und ergänzt einen
einzelnen fehlenden Flächenanteil.“

## Offen

- Gleiche Schichtzahl mit versetzten Grenzen (Schicht k neben Schicht k) bleibt die bisherige Näherung.
- Die Fehlerschranke e = (R'_T − R''_T)/(2 R_T) rechnet der Kern nicht.

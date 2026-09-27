

===== Bericht 1 (2026-09-23T19:25:24.393Z) =====

Phase 1 von E8a ist fertig: alle sechs Punkte gebaut, sieben Commits auf Zweig `e8a` (Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e8a`, HEAD `e71f2d4a`), Arbeitsbaum sauben. Getestet ist noch nichts: Wie vorgegeben lief kein `dotnet test` und kein Referenzlauf, die neuen Tests sind geschrieben, aber nicht gelaufen. Kern-Filter (Release) und Windows-Schale (Debug x64) bauen mit 0 Fehlern, der Designer-Abgleich ist ohne Abweichung, ChartProben ist gelaufen.

**Commits (Reihenfolge wie beauftragt)**

| Commit | Punkt |
|---|---|
| `ddf252bb` E8a/1 | Punkt 2, E6‑Q1/U49 |
| `894c970b` E8a/2 | Punkt 1, Block 2 |
| `836cf54c` E8a/3 | Punkt 3, U46 |
| `6d979cdc` E8a/4 | Punkt 5, U47 |
| `a03b4b5b` E8a/5 | Punkt 6, U48 |
| `e86394ed` E8a/6 | Punkt 4, U41 |
| `e71f2d4a` E8a/7 | Nachbesserung zu Block 2/U46: ein Stand ohne passende Jahresreihen wird im Hinweis genannt statt still „—" |

**Was gebaut ist**
- **Block 4 (E6‑Q1, U49):** Spannenbild und Verlauf stehen jetzt auch in Block 4, mit denselben Bausteinen und derselben Datenseite wie unter „Wie sicher ist das?". Reihenfolge: Bandbreite, Spannenbild, Vorschlag, Hinweistext, Verlauf, Sensitivität. bunit prüft Stellung, gleiches SVG und genau einen Verlauf je Darstellung.
- **Block 2:** je Stand und Szenario eine Jahrestafel mit den sechs Bestandteilen, Netto und Barwert, darunter „Summe nominal" und „Barwert". Zwei Klapplisten wählen Stand und Szenario, Vorgabe ist die Leitversion im Erwartungsfall.
  - Quelle sind die drei Szenarioläufe, die der Verlauf bei „Berechnen" ohnehin rechnet. Neu gerechnet wird nur, wenn der Horizont des Verlaufs verstellt ist; dann über T und gemerkt.
  - Die neue Kern-Klasse `Zahlungsgliederung` sortiert das fertige Zahlungsbild in die sechs Bestandteile und zinst mit dem Faktor des Rechners ab. Sie prüft, dass die Barwerte den Kapitalwert ergeben, und zeigt nur Reihen, deren Kapitalwert zum gespeicherten Ergebnis passt.
  - Ohne Lauf in der Sitzung steht die Hinweiszeile (Text von `WIRT_VALERI_BLOCK_2_HINWEIS` angepasst).
- **U46:** „Woraus entsteht die Zahl?" beginnt mit der Gliederung des Kapitalwerts: je Stand der Barwert mit der Nominalsumme darunter, als letzte Spalte die Differenz Leitversion − Referenz, die sich zur Kapitalwertdifferenz summiert. Sie folgt der Szenario-Klappliste; die bisherige Zeilentafel bleibt darunter unverändert.
- **U47:** Die Tafel „Was daraus im Lauf wird" steht unter dem Hinweistext: je Szenario I₀, die Jahre fälliger Ersatzbeschaffungen und der Restwert (nominal). Ein Kern-Test mit der Prüfgruppe 1040 hält fest, dass die drei Spalten die Szenarioläufe der Bandbreite sind.
- **U48:** Die Fußzeile „Drei Szenarien gerechnet · Annahmen aus Vorgaben, nichts gepflegt" nennt nach einer Pflege die gepflegten Größen. bunit: ohne Pflege „Vorgaben", nach OK im Parameterdialog „gepflegt".
- **U41:** Neues Brückenbild im `ChartRenderer`, Wasserfall nach dem Muster des Spannenbilds, 1240 × 610, deterministisch. Auf der Seite steht es unter der Gliederung, im Wortbericht im Baustein Wirtschaftlichkeit nach den Verlaufsbildern (siehe Abweichungen).
- **Ressourcen:** 39 neue Schlüssel in de und en (`WIRT_GL_*` 16, `WIRT_ZR_*` 5, `WIRT_LW_*` 5, `WIRT_FUSS_*` 5, `WIRT_BR_*` 8), einer geändert. Designer neu erzeugt (8 431 Einträge). Kein SQL.

**Bildprobe:** 153 Prüfungen, 0 Verstöße, 139 Hashes. Gegen den selbst gebauten Basisstand `591229e1` auf demselben Rechner: 132 alte Hashes gleich, 7 neu:
- `kapitalwert_bruecke`
- `kapitalwert_bruecke_unter_referenz`
- `kapitalwert_bruecke_leer`
- `kapitalwert_bruecke_schritt_wirkt_a/_b`
- `kapitalwert_bruecke_reihenfolge_wirkt_a/_b`

Die 111 Zeilen der Gate-Messlatte sind alle enthalten. Diese Liste ist allerdings schon vor E8a veraltet: Die 21 Zapfprofil-Bilder fehlen ihr, deshalb meldet das Gate-Skript auch ohne E8a „ungleich". Die neue Windows-Liste mit 139 Zeilen liegt unter `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e8a\chart_neu\hashes.sha256`. Die Wache für die Zahl der Zeichenmethoden steht auf 32.

**Abweichungen vom Mockup und Auftrag**
- Jahresreihen gibt es erst nach einem Lauf in der Sitzung; gespeicherte Ergebnisse tragen keine. Bis dahin steht ein benannter Hinweis.
- Die Fußzeile U48 steht als eigene Zeile über dem Knopffuß, nicht in derselben Reihe. Das hält Abstand zu e8b, das dort den Knopf „Anhang-E-Checkliste…" (U43) einbaut.
- Die Unterzeilen der Gliederung lauten „nach Zuschussabzug", „einschließlich CO₂-Abgabe" und „zahlungswirksam — Block A" statt der Kategorienummern. Nullwerte stehen als „0", nicht als „—".
- Die Zeile in U47 heißt „Restwert am Ende, nominal".
- Die Brücke hat eine Euro-Achse mit Raster und die Unterzeile im Bild.
- Im Wortbericht steht die Brücke nach den Verlaufsbildern und vor der Mehrjahrestafel, weil der Bericht keine Gliederungstafel hat. Es ist nur eine weitere Bildstelle mit Überschrift 2 im bestehenden Baustein; die Wachen für Überschrift-1-Reihenfolge und Tabellenzahl berührt das nicht.
- Die LIESMICH von ChartProben hat einen Abschnitt „Etappe E8a" bekommen. Papiere, Mockups und Wiki sind nicht angefasst.
- Das Bild ist im eingebauten Browser nicht geprüft, weil die Seite nur in der Anwendung läuft; der Nachweis sind die bunit-Tests.

**Offene Fragen**
- **E8a‑Q1, U42 Zahlungsstrombild (nicht gebaut).** Lesarten: (a) gestapelte Jahresbalken des absoluten Zahlungsstroms einer Version, (b) nur Netto je Jahr mit kumulierter Linie, (c) weglassen, die Tafel in Block 2 genügt. Empfehlung (a): Stand- und Szenariowahl wie Block 2, Reihen wie die Spalten der Mehrjahrestafel (Investition und Ersatz, Betrieb, Energie, CO₂, Einspeisung, KWK-Zuschlag, Steuern, PV), Ausgaben nach unten, Ersatzjahre markiert, keine Differenzdarstellung. Eigener kleiner Schritt mit Bildprobe.
- **E8a‑Q2 Leitversion.** Gebaut ist: größte Kapitalwertdifferenz im Erwartungsfall, in Sicht 2 der Stand B. Alternativen: die vorgeschlagene Version oder eine Auswahlliste. Empfehlung: so lassen.
- **E8a‑Q3 Jahresreihen gleich beim Öffnen.** Das ginge nur, wenn die Jahresreihen mitgespeichert werden, also mit einer neuen Fassung des gespeicherten Nachweises. Empfehlung: nicht in E8, weil E8 keine Speicheränderung vorsieht.
- **E8a‑Q4 Fußzeile U48.** Empfehlung: nach dem Merge mit e8b in dieselbe Reihe wie die Knöpfe setzen.

**Zu erwartende Merge-Stellen mit e8b:** `WirtschaftlichkeitSeite.razor` (Fuß von „Was ist angenommen?"), `WirtschaftlichkeitSeiteTexte.cs`, die beiden `.resx` (meine Schlüssel stehen nach `WIRT_VALERI_KZ_HINWEIS`), `Resource.Designer.cs` (neu erzeugen), `BausteineWirtschaftlichkeit.cs` und `WirtschaftlichkeitEmpfehlung.cs` (`ValeriAusweis`).

**Erledigt-Gründe für die Papiere**
- Register E6‑Q1 gebaut mit E8a; E5b‑4 vollständig (U41, U46, U47, U48).
- Mockup U41, U46, U47, U48, U49 erledigt; U42 bleibt offen (E8a‑Q1).
- Konzept § 2.11.4 V‑C: die fünf Blöcke sind vollständig; offen nur das Zahlungsstrombild.

**Logbuchsätze (Stichwort `bericht`)**
1. Die ValERI-Bewertung zeigt in Block 2 die Zahlungsreihen je Jahr und als Barwert, wählbar nach Stand und Szenario.
2. Block 4 zeigt Spannenbild und Verlauf mit drei Szenarien.
3. „Woraus entsteht die Zahl?" zeigt je Bestandteil Barwert, Nominalsumme und die Differenz zur Referenz, dazu das Brückenbild zur Kapitalwertdifferenz, das auch im Wortbericht steht.
4. „Was ist angenommen?" zeigt die Tafel „Was daraus im Lauf wird" und eine Fußzeile zur Herkunft der Annahmen.

**Phase 2 nach „Tests freigegeben":** bunit- und Kern-Tests (vor allem `ZahlungsgliederungTests`), voller Lauf, Anker-Tests, Referenzlauf 13/13 gegen R12. Keine Simulationsgröße ist berührt.

===== Bericht 2 (2026-09-23T20:09:31.141Z) =====

E8a/8 (U42) ist gebaut und beide Nachzüge sind zusammengeführt. Alle Läufe sind grün: voller Testlauf, Referenzlauf 13/13 gegen R13 und ChartProben. Bei einem gefilterten Testlauf habe ich die tasklist-Regel verletzt, Einzelheiten unter „Tests“. Nichts gepusht; Arbeitsbaum sauber, HEAD `ac51fdb5`.

**Commits auf `e8a`**
- E8a/1–7 (Phase 1): `ddf252bb`, `894c970b`, `836cf54c`, `6d979cdc`, `a03b4b5b`, `e86394ed`, `e71f2d4a`
- `8a586448`: Merge von bba1f1a7
- `5c3cc58e`: E8a/8, Zahlungsstrombild U42
- `51bf6d6b`: E8a/9, Korrektur meines eigenen bunit-Falls aus E8a/1. Er las den Zähler erst nach dem Klick und verglich das SVG samt der Ereigniskennungen, die bunit je Instanz fortzählt. Beides ist behoben, die Aussage des Falls bleibt.
- `ac51fdb5`: Merge von 2edc081e. origin steht inzwischen auf 7a32b6f3; das ist nur ein Protokoll-Commit (Statusdatei und Protokoll) und wie beauftragt nicht mit hereingenommen.

**Konflikte:** In beiden Merges keine. Beide resx, der Designer und `epos-ui.css` wurden automatisch zusammengeführt. Geprüft: XML liest sich, keine Doppel, keine Konfliktmarker, BOM und CRLF, beide Sprachen mit gleichen Schlüsseln. Der Designer ist neu erzeugt und blieb unverändert; der Prüflauf am Ende meldet ebenfalls „unverändert“.

**Schlüssel:** E8a hat zusammen 43 neue Schlüssel: 39 aus Phase 1 plus `WIRT_ZS_TITEL`, `_UNTER`, `_ERSATZJAHR`, `_LEER`. Jede resx hat jetzt 8493 Einträge (59 davon aus #450), der Designer 8494.

**U42 – was gebaut ist**
- **Bild:** Neue Zeichenmethode `ChartRenderer.Zahlungsstrom` (1240 × 620). Die Reihen sind die Positionsspalten der Mehrjahrestafel ohne Summenspalten; „Steuern“ steht deshalb wie in der Tafel als bis zu drei eigene Spalten, dazu die KWKG-Pauschale im Jahr 0.
- **Darstellung:** Ausgaben nach unten, Einnahmen nach oben, feste Farbe je Spalte. Ersatzjahre tragen ein Band mit Dreieck, der Schlüssel steht unter der Achse. Keine Differenz und kein Restwert; die Unterzeile sagt „ohne Restwert“.
- **Seite:** Das Bild steht in Block 2 unter der Jahrestafel und folgt der Wahl von Stand und Szenario.
- **Datenquelle:** Die Werte kommen aus derselben Verlaufslinie wie die Gliederung, also nur dort, wo die Jahresreihen zum gespeicherten Lauf passen.
- **Wortbericht:** Ein passender Baustein ohne Umbau existiert – das Bild steht über jeder Mehrjahrestafel (Szenario „Erwartet“).
- **Wachen:** Die Wache der Zeichenmethoden steht jetzt auf 33 statt 32. Die Wortbericht-SVG-Wache prüft das neue Bild mit.
- **Neue Bilder (7):** `zahlungsstrom`, `zahlungsstrom_ohne_ersatz`, `zahlungsstrom_leer`, `zahlungsstrom_ersatzjahr_wirkt_a/_b`, `zahlungsstrom_vorzeichen_wirkt_a/_b`. Dazu drei SVG-Proben, die die lückenlose Stapelung und die Ersatzjahr-Marken nachmessen.
- **Sichtprüfung:** Die PNGs habe ich angesehen, im Browser ist das Bild nicht geprüft.

**Builds:** Kern-Filter und Windows-Schale (Debug x64) je 0 Fehler.

**Tests**
- **Regelverstoß:** Ein gefilterter Kern-Lauf (Zahlungsgliederung, beide Wachen, Blattstruktur, NachtraegeE5b, Anker-Tests; 68/68 grün) lief parallel zu einem fremden testhost aus dem Worktree `agent-afe1c27a…`. Ich hatte tasklist abgefragt, aber nicht gewartet. Alle späteren Läufe liefen über ein Warteskript mit 60-Sekunden-Prüfung.
- **bunit der Seite nach dem zweiten Merge:** 302/302.
- **Voller Lauf `WP-Plan.Kern.slnf`:** 11.868 bestanden, 1 übersprungen, 0 Fehler (EPOS.Kern.Tests 5331, EPOS.UI.Tests 5600, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 + 1 übersprungen).

**Referenzlauf gegen R13:** 13/13 Projekte gerechnet, GESAMT PASS mit 4.145.687 Werten innerhalb der Toleranz. Zur Information: alle 387 CSV sind bytegleich mit R13.

**ChartProben:** 161 Bilder geprüft, 0 Verstöße. Die Hashliste liegt unter `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e8a\chart_p2\hashes.sha256` und hat 146 Zeilen:
- 132 aus der Basis 591229e1, alle gleich, darunter die 21 Zapfprofil-Bilder;
- 7 Brückenbilder und 7 Zahlungsstrombilder neu.

Die Gate-Messlatte (111 Zeilen) ist vollständig enthalten.

**Testdatenbank:** Die origin-Fassung ist mit dem ersten Merge gekommen, ohne eigenen Commit.
- Prüfung: LFS-SHA-256 `769143e4…`, 67.751.936 Byte, Schema 113, KATALOG_GENERATION 9, `quick_check` ok, Projekt 1017 vorhanden.
- 2edc081e hat sie nicht berührt, und nach allen Läufen ist sie unverändert.
- Die zwei Beidateien (`-shm`/`-wal`) meiner lesenden Probe habe ich entfernt.

Die Fußzeile in der Knopfreihe (Q4) bleibt wie vereinbart für den e8b-Nachzug.
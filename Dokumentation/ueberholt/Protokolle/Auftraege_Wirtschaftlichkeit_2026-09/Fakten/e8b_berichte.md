

===== Bericht 1 (2026-09-23T20:08:15.914Z) =====

## Phase-1-Bericht E8b

Phase 1 ist fertig: alle Punkte gebaut und committet, der Arbeitsbaum ist sauber. Getestet ist noch nichts: `dotnet test` und der Referenzlauf stehen nach Auftrag aus, bis „Tests freigegeben" kommt. Zweig `e8b` im Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e8b`, Basis 591229e1, Stand jetzt 3fa1ec6c. Kein Push, kein Merge, kein CI-Lauf.

### Commits
| Commit | Punkt |
|---|---|
| 4b06bce8 | E8b/0 Blattstruktur-Wache ergänzt, ClosedXML-Befund als eigene Wache |
| 5b6a0125 | E8b/1 Stufe 0 Parameterblock |
| 8cac394b | E8b/2 Stufe 1 Mehrjahrestabelle in Formeln |
| 238511ed | E8b/3 Stufe 2 Kennzahlen NBW/RMZ/IKV |
| a8b05f9c | E8b/4 Stufe 3 Betriebskosten Menge × Satz, Δ%-Block |
| 826b2d4d | E8b/6 Anhang-D-Gegenprobe |
| 4408523a, 3fa1ec6c | E8b/5 U43 Checkliste und Knopf, dazu ein Nachtrag |

### Zwischenbericht nach Stufe 1 (nachgereicht)
Als Unteragent konnte ich nicht anhalten, deshalb steht er hier. Nach 8cac394b waren Stufe 0 und Stufe 1 fertig und der Zellvergleich gleich. Der ClosedXML-Befund und der Entscheid lagen vor (siehe unten). Es gab keinen Grund für einen Stopp.

### Je Stufe: Bau und Zellvergleich
Verglichen wurde mit dem eigenen Messprogramm, immer Wertfassung (Basis) gegen Formelfassung.

- **Stufe 0:** Parameterblock je Szenario mit i, T, p_E, p_B, p_I und den Änderungen (Norm 9 c). Dazu 15 benannte Bereiche, z. B. `Zins_i`, `p_E_Guenstig`. Ergebnis: 11 Zeilen eingeschoben, alle Zellen gleich.
- **Stufe 1:**
  - Energie und CO₂-Abgabe (nur im Rückfallzweig) werden als Jahr 1 × (1+p)^(t−1) fortgeschrieben.
  - Betrieb rechnet in zwei Termen über Hilfsspalten, damit Positionen mit späterem Startjahr eine Stufe bilden.
  - Netto ist eine Zeilensumme, Barwert und Kumuliert (Laufsumme) folgen.
  - Bis 272 Formeln je Mappe, Werte gleich.
- **Stufe 2:**
  - NBW über `NPV` + Jahr 0 + Barwert des Restwerts, Annuität über `PMT`.
  - Differenzreihe Variante − Referenz samt Restwert-Nominaldifferenz.
  - Zinsfuß über `IRR` nur bei genau einem Vorzeichenwechsel.
  - Amortisation über eine Hilfsspalte; ohne Nulldurchgang steht der Satz als Text statt eines Zellfehlers.
- **Stufe 3:**
  - Eine bemessene Position trägt Menge und Satz, der Betrag ist ihr Produkt (bei Prozent geteilt durch 100). Feste, szenariogepflegte und unvollständige Positionen bleiben Werte mit Herleitung.
  - Die Summe ist eine Spaltensumme, der Δ%-Block ein Zellbezug.
- **Endstand, 13 Prüfgruppen:**
  - Formeln je Mappe, z. B. synth 256, 1019/1023/1024 320, hyb1042 429, hybbk 449, prep1030 134; 1046 hat keine Wirtschaftlichkeit, also 0.
  - Jede Zelle der Wertfassung ist gleich.
  - Excel 16 rechnet jede Formelzelle auf den zwischengespeicherten Wert.
  - ClosedXML rechnet gleich, außer bei NPV/PMT/IRR und den Zellen, die darauf verweisen (es kennt die Funktionen nicht).
  - OpenXML-Prüfung 0 Fehler; jede Formelzelle trägt ihren gespeicherten Wert.
  - Wortbericht: nur die Checkliste kommt hinzu.

### ClosedXML-Befund und Entscheid
- **Befund:** ClosedXML 0.105.1 speichert Formeln ohne Ergebniswert, auch nach einer Neuberechnung. NPV/PMT/IRR kann es nicht rechnen (`#NAME?`).
- **Entscheid:**
  - ClosedXML schreibt die Formeln.
  - EPOS rechnet jede Formel vorher in C# nach; stimmt sie nicht, bleibt der Wert stehen.
  - EPOS trägt den Rechenkernwert als Ergebnis in voller Stellenzahl nach.
  - Excel rechnet beim Öffnen neu.
- **Nicht prüfbar:** LibreOffice und openpyxl sind hier nicht installiert. Ersatzweise habe ich das XML so gelesen, wie openpyxl es mit `data_only` lesen würde: Formel und Wert sind überall vorhanden.

### Anhang D (gegen `KapitalwertRechner.Rechne`)
- Die Sollwerte treffen:

| Fall | Rechenkern | Soll |
|---|---|---|
| wahrscheinlichster Fall | 64.479,51 € | 64.480 € |
| Worst | −202.801,57 € | −202.802 € |
| Best | 598.319,65 € | 598.320 € |

  Alle drei liegen innerhalb ±1 €; auch die Jahreswerte D.5 stimmen.
- Sechs Zeilen der Tafel D.6 treffen auf ±0,5 €, samt Steigungen 472 / −57 / 1.121 / −889 / −900 / −354.
- Ausgenommen und dokumentiert:
  - die zwei D.6-Zeilen mit Werten des Pumpenbeispiels;
  - „Gasverbrauch BHKW" (die Zeile trifft den ganzen Energie-Nettostrom, nicht den Gasverbrauch);
  - Tippfehler in D.7: 348.583 statt 349.583 kWh/a.
- Kein STOPP nötig, am Rechenweg nichts geändert.

### U43
- Die Checkliste (15 Punkte, fünf Gruppen) entsteht einmal im Kern. Je Punkt: Anforderung, Stelle im Bericht und ein Stand erfüllt/teilweise/offen, der aus dem Lauf abgeleitet wird.
- Wortbericht: Abschlussseite nach dem Anhang.
- Mappe: letztes Blatt „Checkliste Anhang E", die Notenspalte nimmt nur ganze Zahlen 1–5 an.
- Ergebnisseite: eigener Baustein `AnhangEChecklisteKnopf` im Fuß des Bewertungsblocks, eine Einbindungszeile in `WirtschaftlichkeitSeite.razor`.
- Nachtrag 3fa1ec6c: Die Bandbreite entsteht auch ohne Rechnung, deshalb stand Punkt 9 sonst fälschlich auf „teilweise". Er verlangt jetzt Zahlen in Ungünstig und Günstig. Punkt 4 ist ohne Lauf offen.

### Abnahme Phase 1
- Kern-Filter und Windows-Schale bauen mit 0 Fehlern, die UI-Tests ebenso.
- Ressourcen-Designer: unverändert und wiederholbar.
- SQL-Dialekt-Prüfer: 0 Fundstellen.
- Neue Tests (kompiliert, nicht gelaufen):
  - Kern: `AnhangEChecklisteTests` (7 Fälle), `AnhangDFallstudieTests`, `FormelmappeClosedXmlBefundTests`, Anker in `BerichtBlattstrukturWacheTests`.
  - bunit: `AnhangEChecklisteKnopfTests` (5 Fälle).

### Ressourcenschlüssel
20 × `WIRT_FM_*` und 83 × `WIRT_AE_*`, beide Sprachen vollständig. Eingefügt nach `WIRT_MJ_PROBE`, nicht am Dateiende, damit es mit e8a weniger Konflikte gibt.

### Abweichungen vom Auftrag
- Der Zwischenbericht kommt erst hier, und Punkt 5 hat zwei Commits.
- `Zahlungsbild` hat drei reine Ausweisfelder bekommen (E8b/2). Der Rechenweg ist unverändert; ich nenne es wegen der Regel „keine Rechneranpassung ohne Rückmeldung".
- Die Wache legt drei Zeilen in `Tab_ProjektWerte` der Arbeitskopie an, nach bestehendem Testmuster. Neues SQL im Programmcode gibt es nicht.

### Offene Fragen (Lesarten, Empfehlung)
1. **Kennzahlen Günstig/Ungünstig bleiben Werte**, weil es für diese Szenarien keine Mehrjahrestabelle gibt. (a) so lassen, (b) je Stand zwei weitere Tabellen. Empfehlung: (a).
2. **„fester Betrag" neben einer Menge-×-Satz-Formel.** Das war schon vorher falsch: `WirtschaftlichkeitZeilen.BemessungText` kennt nur 4 von 17 Bemessungsarten. Empfehlung: eigener kleiner Auftrag im Kern.
3. **Betriebskosten-Warnung „Gliederung unvollständig" bei Startjahr-Positionen.** Auch das war schon vorher so. Beispiel hybtest: 2.400 gegen 1.800 €, die Differenz ist genau die Wartung ab Jahr 6. (a) Warnung stehen lassen, (b) nur Positionen vergleichen, die im Jahr 1 laufen, oder das Startjahr in der Warnung nennen. Empfehlung: (b) als eigener Auftrag.
4. **Der Knopf ruft eine reine Kernfunktion direkt auf.** Die UI-Regel sagt „keine Fachklassen des Kerns". (a) so lassen (ohne Datenbank, eine Quelle), (b) die Hülle füllt die Punkte in den Seitenstand. Das berührt e8a-Dateien und ginge erst nach dem Merge. Empfehlung: (a).
5. **Merge mit e8a:**
   - `Resource.resx` und der Designer (Designer neu erzeugen).
   - die Einbindungszeile in `WirtschaftlichkeitSeite.razor`.
   - eine Selektorzeile in `WirtschaftlichkeitErgebnisansichtTests` (e8a ergänzt dort viel).
   - e8a fügt einen Abschnitt `WIRT_BR_TITEL` ins Wirtschaftlichkeitskapitel ein. Danach muss die Abschnittsliste der Wache nachgezogen und die Tabellenzahl geprüft werden.
6. **Referenzbasis:** Der Arbeitszweig ist inzwischen auf R13, e8b steht auf R12. Empfehlung: Phase 2 wie beauftragt gegen R12 (E8b ändert nicht am Rechenweg), R13 dann im Gate nach dem Merge.

### Erledigt-Gründe für die Papiere
Konzept § 2.11.6 Stufen 0–3, V‑G10 (Parameterblock aus echten Zellen), V‑G12 (Anhang-E-Checkliste), Register Q18, Mockup U43.

### Logbuchsätze (Wiki `bericht`, Version beim Anwender erfragen)
- **Stufe 0:** Der Tabellenbericht führt die Parameter der Wirtschaftlichkeit je Szenario in einem eigenen Block mit benannten Zellen.
- **Stufe 1:** Die Mehrjahrestabellen des Tabellenberichts rechnen mit sichtbaren Formeln auf diesen Block.
- **Stufe 2:** Kapitalwert, Annuität, interner Zinsfuß und Amortisation stehen im Tabellenbericht als Formeln.
- **Stufe 3:** Bemessene Betriebskosten zeigen im Tabellenbericht Menge und Satz; der Betrag ist ihr Produkt.
- **U43:** Wort- und Tabellenbericht schließen mit der Checkliste nach DIN EN 17463, Anhang E; die Ergebnisseite zeigt sie über „Anhang-E-Checkliste…".

### Stand und Nächstes
- Nutzung: wöchentlich 42 %, Fable 27 %, 5-Stunden-Fenster 59 %.
- Der Kontext dieser Sitzung ist zu 87 % voll. Für Phase 2 ist eine frische Sitzung sinnvoll.
- Phase 2 (Tests, UI-Tests, Referenzlauf 13/13, Wache vorher/nachher) wartet auf „Tests freigegeben".

Die wichtigsten Dateien:
- `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e8b\EPOS.Kern\Allgemein\Bericht\ExcelFormelmappe.cs`
- `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e8b\EPOS.Kern\Allgemein\Bericht\AnhangECheckliste.cs`
- `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e8b\EPOS.UI\Seiten\Berichte\AnhangEChecklisteKnopf.razor`
- `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e8b\EPOS.Kern.Tests\AnhangDFallstudieTests.cs`

Messprogramm und Messstände liegen unter `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e8b\`: Vergleich in `vergleich.py`, Endstand in `final2`.

===== Bericht 2 (2026-09-23T20:28:25.296Z) =====

Das Zusammenführen ist gemacht und alles ist grün. Der volle Testlauf hat 0 Fehler, der Referenzlauf gegen R13 besteht 13/13. Eine Aussage aus meinem Phase-1-Bericht war falsch und ist berichtigt: ClosedXML rechnet PMT doch.

**Commits** (Zweig `e8b`, Stand 198dcb07, sauber, nichts gepusht)
- d5da3480 „E8b: Zweig e8a ac51fdb5 in e8b zusammengeführt" (Eltern 3fa1ec6c und ac51fdb5)
- 198dcb07 „E8b/8: ClosedXML-Befund berichtigt — PMT rechnet, NPV/IRR nicht"

**Konflikte**
- Nur `Resource.Designer.cs`: aus den zusammengeführten `.resx` neu erzeugt, ein zweiter Designer-Lauf ändert nichts mehr.
- Beide `.resx`, `WirtschaftlichkeitSeite.razor` und `WirtschaftlichkeitErgebnisansichtTests` gingen ohne Konflikt durch; meine Selektorzeile und die Einbindungszeile sind erhalten.
- **Schlüsselzahl:** 8.600 je Sprache, keine doppelten Schlüssel, keine Konfliktmarker. Davon aus E8b: 20 `WIRT_FM_*` und 83 `WIRT_AE_*`.

**Nachzüge im Merge-Commit**
- **E8a‑Q4:** Die Fußzeile U48 steht jetzt links in derselben Reihe wie „Anhang-E-Checkliste…" und „Bericht erzeugen". Dafür gibt es in der Seite eine neue `Fussreihe` statt `BerichtKnopfteil`, die Stilregel `.epos-wirt-abschnitt-fuss` bekam Umbruch und Abstand, und der U48-Test prüft die Reihe.
- **Blattstruktur-Wache:** Der neue Abschnitt „Von der Investition zur Kapitalwertdifferenz" (`WIRT_BR_TITEL`) steht in der Kapitelfolge. Das Zahlungsstrombild ist ein Bild, keine Tabelle, deshalb bleibt die Tabellenzahl bei 11.

**Tests** (erst gestartet, als kein fremder testhost mehr lief)

| Projekt | bestanden |
|---|---|
| EPOS.Kern.Tests | 5.357 |
| EPOS.UI.Tests | 5.605 |
| KiKern.Tests | 524 |
| SpeicherEngine.Tests | 386 |
| SpeicherPlanung.Tests | 27 (1 übersprungen) |
| **Summe voller Lauf** | **11.899, 0 Fehler** |

- Einzelläufe: die vier neuen bzw. geänderten Kern-Klassen 32/32, die betroffenen bunit-Klassen 123/123.
- Blattstruktur-Wache vorher (auf E8b/0, 4b06bce8) 8/8, nachher (auf 198dcb07) 13/13.
- **Befund:** Der erste Einzellauf war rot. `ClosedXML_rechnet_NPV_PMT_und_IRR_nicht` stimmte nicht: ClosedXML 0.105.1 rechnet PMT; nur NPV und IRR ergeben `#NAME?`. Die Fehlerwerte der Annuität in der Mappe kommen daher, dass ihr PMT auf den NBW verweist. Test und Kommentar sind berichtigt, der Entscheid zur Formelmappe bleibt.
- **Regelverstoß:** Beim Vorher-Lauf der Wache lief ein fremder testhost. Ich habe ihn gesehen, aber nicht gewartet. Der Lauf war grün, e8a sollte seinen parallelen Lauf aber mit Blick darauf ansehen.

**Referenzlauf gegen R13:** 13/13 PASS, 4.145.687 Werte innerhalb der Toleranz.

**Weitere Prüfungen**
- Kern-Filter und Windows-Schale bauen mit 0 Fehlern.
- Designer-Prüflauf: unverändert.
- SQL-Prüfer: 1.737 SQL-Texte, 0 Fundstellen.

**Zellvergleich nach dem Merge** (13 Prüfgruppen, Stand vorher gegen nachher)
- Gleich: synth, synthk, 1030, prep1030, hyb1040, hyb1042, hybbk, hybleer, hybtest.
- Nur Werte verschieden in vier Gruppen mit simulierten Daten, in allen Blättern einschließlich Stamm und Varianten: 1019/1023/1024 mit 771 Zellen, 1026/1027/1029 mit 155, 1018/1031 mit 97, 1046 mit 52. Die Ursache ist der neue Rechenstand (R13 Kühlung, Katalog-Generation 9). Keine Zeile ist verloren, die Zahl der Formeln ist gleich.
- Nach dem Merge rechnet Excel 16 in allen 13 Gruppen jede Formelzelle auf den gespeicherten Wert. Die OpenXML-Prüfung meldet 0 Fehler.
- Im Wortbericht kommt nur der Abschnitt zum Brückenbild hinzu.

**Testdatenbank**
- In e8b liegt LFS `769143e4…` (Commit 6ebb26b6, Katalog-Generation 9). Genau diese Prüfsumme nennt `Referenzlaeufe/LIESMICH.md` für R13, und die Arbeitskopie stimmt mit ihr überein. Vorher war es `9df1b7a5…`.
- Die WAL-Datei ist leer.
- **Abweichung von deiner Angabe:** Den genannten Stand 171d8acb gibt es in der Geschichte von e8b nicht. Gerechnet wurde mit 769143e4 aus 6ebb26b6, gegen den R13 gerechnet ist.

**Offen:** Q2 („fester Betrag" bei bemessenen Positionen, `BemessungText` kennt nur 4 von 17 Arten) und Q3 (Summenwarnung bei Positionen mit späterem Startjahr) sind als eigene Aufträge vermerkt. Q1 und Q4 bleiben wie gebaut (Lesart a).
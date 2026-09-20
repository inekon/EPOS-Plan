# E2 — Kleine Kernkorrekturen der Wirtschaftlichkeit

**Datum:** 20.09.2026
**Zweig:** `we2` (Worktree `.claude/worktrees/we2`, abgezweigt von `14a19425`)
**Grundlage:** `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`
§ 3.1 (R4–R6, R10, R11), § 3.3 (A5, A6) und § 5 Zeile E2; dazu
`2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md` § 3.3 („Programm") und § 4 (Q7, Q12, Q19)
**Stand der Messung:** Testdatenbank `Referenzlaeufe/Kenndaten_Test.sqlite`, Schemastand 96

## Aufgabe

E2 ist die Etappe **ohne Rechenwirkung**. Jeder Punkt ist entweder ein **Ausweis** (etwas, das
gerechnet wird, steht nirgends), eine **Beschriftung** oder eine **Bedienung**. Der Maßstab dafür
sind die vier Ankertests aus E1: Betriebskosten 1024 = 99,00 €/a, Kaskade 1042 = ±0,00 €,
Kapitalwert 1024 = −2.896.359,13 €, Kapitalwert 1030 = −21.895.377,28 €. Sie bleiben unverändert
grün, und der Referenzlauf der fünf CI-Projekte bleibt `GESAMT: PASS`.

Wo ein Punkt Rechenwirkung über den Ausweis hinaus gehabt hätte, steht er unten unter
„Was offen bleibt" als Entscheidfrage — nicht im Code.

## Commits

| Kennung | Betreff |
|---|---|
| `35010f10` | W-E2: Kohaerenzzeilen CO2, Strommix und Erlaubnisschwelle |
| `d1795e13` | W-E2: Bezugsmenge, Vergleichsstrenge, Formelzeile N4, Kommentare |
| `841d9e92` | W-E2: Bandbreite mit Spanne, Referenzname, Zeitraumhinweis im Excel |
| `e1d4e8d0` | W-E2: Dialoge, Ressourcen und Hilfeanker der Mockup-Pruefung |
| (folgend) | W-E2: Protokoll, Index, Konzept und Wikiquelle |

---

## 1 Kern und Ausweis

### 1.1 R5 — CO₂ doppelt gebucht

**Befund.** `KohaerenzPruefung` kannte weder `CO2` noch `BEHG`. Der erfasste Arbeitspreis eines
Brennstoffs darf den CO₂-Anteil nach BEHG als Preisbestandteil ausweisen
(`energy_project_settings`, Spalte `Anteil_CO2` mit Aktiv-Schalter); dann steckt die Abgabe bereits
in den Energiekosten. `KapitalwertRechner.Rechne` bucht eine übergebene BEHG-Reihe **zusätzlich**.
Derselbe Betrag steht damit zweimal im Kapitalwert, und nichts sagt es.

**Änderung.** Neuer Fall `Co2DoppelansatzBehg`: aktiver CO₂-Anteil > 0 an irgendeinem Träger des
Projekts **und** gebuchte CO₂-Abgabe > 0 ergeben eine **WARNUNG mit Betrag** — dem tatsächlich
gebuchten Jahresbetrag, nicht einer Zweitrechnung. Die Trägerliste kommt aus der Preistabelle des
Projekts, nicht aus den Steueranlagen: Der Doppelansatz hängt an keinem Steuerpfad, und dass
überhaupt Brennstoff verbrannt wird, sagt die gebuchte Abgabe selbst. `KohaerenzLauf` trägt dafür
das neue Feld `Co2AbgabeEur` (belegt aus `eingabe.Behg`).

Der Rechenweg bleibt, wie er ist — das ist ausdrücklich **Weg (a)** des Anwenderentscheids zu
Frage Q3 der Mockup-Prüfung: die BEHG-Zeile bleibt stehen und wird gekennzeichnet.

**Nachweis.** `EPOS.Kern.Tests/KohaerenzCo2Tests`: Zeile mit Betrag; Gegenprobe mit
abgeschaltetem Anteil; Gegenprobe ohne gebuchte Abgabe; der Fall ohne Steuerpfad; und die Probe,
dass die Prüfung den gebuchten Betrag nicht anrührt.

### 1.2 R6 — wo die Kohärenzzeilen stehen

**Befund.** Die Zeilen erreichten allein die Windows-Seite
(`WirtschaftlichkeitSeiteGaben.KohaerenzZeilen`). Wort- und Excelbericht kannten sie nicht
(`grep Kohaerenz` über `EPOS.Kern/Allgemein/Bericht/` = 0 Treffer), die Erlösrubrik ebenso wenig.
Der Strommix-Rückfall stand als gewöhnlicher Laufhinweis da — **ohne** den Wert, um den es geht.

**Änderung.** Zwei Schritte:

1. Die Kohärenzzeilen stehen im **einen** Zeilenkatalog (`WirtschaftlichkeitZeilen.Kennzahlen`)
   als Textzeilen, so viele, wie das Ergebnis mit den meisten Hinweisen führt. Damit erreichen sie
   Rubrik, Wortbericht und Excelblatt auf einmal — dieselbe Sichtbarkeitsregel, dieselbe
   Reihenfolge, dieselben Marken (⚠ / ✓ / ·, jetzt in `WirtschaftlichkeitZeilen.Kohaerenzmarke`).
   Der zweite Aufbau in der Windows-Hülle entfällt; er war die Stelle, an der die drei Ausgaben
   auseinanderliefen.
2. Der Strommix-Rückfall ist keine Laufbemerkung mehr, sondern eine Kohärenzzeile **mit Wert**
   (435 g CO₂/kWh, aus `KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH`). `KohaerenzLauf` trägt dafür
   `StrommixRueckfallGJeKwh`; der Ressourcenschlüssel `WIRT_CO2_STROMMIX_RUECKFALL` ist durch
   `KOH_CO2_STROMMIX_RUECKFALL` abgelöst.

**Nebenbefund erledigt.** Der Kommentar `WirtschaftlichkeitSeiteGaben.cs:727` („Sie sind nicht
persistiert; ein aus der Datenbank geladener Stand zeigt sie deshalb nicht") war seit B7P falsch.
Er ist mit der Methode entfallen, die er beschrieb.

### 1.3 R4 — Kaskadenrunde 2

**Befund.** Der Auftrag verlangte zu prüfen, ob ein Testfall mit **vertauschter** Zeilenreihenfolge
zweier `PROZENT_ERZEUGERKOSTEN`-Hauptzeilen derselben Komponente existiert.

**Ergebnis: er existiert.** Etappe E1 hat die Runde 2 zweiphasig gemacht
(`InvestKaskade.cs`, `hauptZeilen` vor der Zuweisungsschleife eingefroren) **und** den Fall
gesetzt: `InvestKaskadeTests.Erzeugerkostenzeilen_zaehlen_einander_nicht_mit` ist eine
`[Theory]` mit `[InlineData(ID_E_A, ID_E_B)]` und `[InlineData(ID_E_B, ID_E_A)]` — genau die
vertauschte Einfügereihenfolge, beide Läufe mit demselben Ergebnis (566,00 € und 169,80 € aus
derselben Basis 5.660,00 €). **Keine Änderung nötig.**

### 1.4 V-3 — die PV-Reihe bekommt ihre Spalte

`ErloesReihe.PV_VERGUETUNG` ist eine **Zusatzreihe** des Kapitalwertrechners: Sie steckt in „Netto
nominal", aber nicht in der Spalte „Einspeisung" (die trägt allein den konstanten Einspeiseerlös).
Ohne eigene Spalte fehlte sie in der Summe der Positionsspalten — die Selbstprüfung der
Mehrjahrestabelle ging um genau diesen Betrag daneben. `Mehrjahresbild.Baue` nimmt die Reihe jetzt
auf (`WIRT_REIHE_PV`). Nachweis: `KleinkorrekturenE2Tests` rechnet ein Zahlungsbild mit
PV-Reihe und prüft die Selbstprüfung Jahr für Jahr.

### 1.5 B-7 — die Bezugsmenge trägt ihre eigene Einheit

`BetriebskostenCtrl.MengenEinheit` kannte zwei Arten und beschriftete jede andere Bezugsmenge mit
„€". Die Herleitungszeile las sich dann „500,00 € × 12,000 €/kW·a", wo eine **Leistung** in kW
steht. Die Antwort kommt jetzt aus dem `BemessungKatalog` — wie beim Satz, also EINE Wahrheit für
beide —, samt der gewerkeigenen Bezugsgröße (Pufferspeicher „Ltr.").

Die abgeleitete Regel steht im Katalog: Trägt der **Betriebssatz** „·a", ist die Bezugsgröße ein
BESTAND (Leistung, Fläche, Volumen) und bleibt ohne Jahr; sonst ist sie eine MENGE je Jahr
(„kWh/a", „h/a"). Eine prozentuale Art bemisst sich an einem Betrag und behält „€".

### 1.6 I-5 — Vergleichsstrenge

Gemessen: **74 von 74** Vergleichen gegen `DbWerte.BEMESSUNG_*` im Kern laufen `Ordinal`; bei
`DbWerte.KOSTENART_*` waren es 4 von 5. Die eine tolerante Stelle (`WirtschaftlichkeitCtrl.IstZuschuss`,
`OrdinalIgnoreCase`) stellte dieselbe Frage anders als der Betriebskostenpfad, der sie als
**SQL-Gleichheit** stellt — und SQLite vergleicht TEXT zeichengenau. Eine Zeile mit abweichender
Schreibweise war hier ein Zuschuss und dort keiner: zwei Antworten auf eine Frage.

Beide C#-Stellen (`IstZuschuss` und `SpeicherAuslegungCtrl`) vergleichen jetzt `Ordinal`. Auf den
Daten ändert das nichts: Die Testdatenbank führt in `Tab_ProjektWerte.Bemessung` und
`.Kostenart` ausschließlich die eingefrorenen Großschreibungen (gemessen, alle zwölf bzw. vier
Werte), und geschrieben werden sie nur aus `DbWerte`.

### 1.7 S-3 und S-5

**S-3.** `SteuerAnlage.Klartext` nannte immer die **elektrische** Nennleistung; ein Kessel führt
dort 0, und die Meldung las sich „Heizkessel (0 kW)" — eine Angabe, die es an einem Kessel nicht
gibt. Sie entfällt bei `Stromerzeuger == false`.

**S-5.** `GESETZ_STROMST_ERLAUBNISSCHWELLE` (1.000 kW) war der einzige gesäte
Stromsteuer-Schlüssel ohne Leser. Er bekommt einen: eine **Hinweiszeile** der Kohärenzprüfung, die
Schwelle und betroffene Anlagen nennt und ausdrücklich sagt, dass sie auf die Rechnung nicht wirkt.
Der **Radius 4,5 km** bleibt Meldungstext — die Geometrie fehlt im Datenmodell.

### 1.8 Q7 — die Formelzeile der Trägerkarte

`EnergietraegerPreiskarte.Formel` schrieb den Arbeitspreis mit `N2`, das Ergebnis darunter mit
`N4`. Bei einem Preis von 0,0500 €/Einheit stand „0,05 ÷ 10,50 = 0,0048" — die Formel ging nicht
auf. Der Zähler steht jetzt ebenfalls mit `N4`. Gerechnet wird unverändert mit dem vollen Wert.

### 1.9 Kommentare und Kleinigkeiten

| Ort | Befund | Änderung |
|---|---|---|
| `EegSatzRechner.cs:74` | Zahlendreher „8,09679" für 8,60 × 0,99⁶ | berichtigt auf **8,09673** (E1-EEG-1) |
| `EegSatzRechner` `AusfallvergCt` | die Rundung stand in keinem Papier | beschrieben: Eine Rundung **ist** fachlich vorgesehen — `Math.Round(…, 2)` am Ausgabewert, gerechnet auf der unrundeten Mischung, dieselbe Regel wie bei der Degression. **Keine Zahlenänderung** |
| `StrompreisZerlegungModel.cs:86` | „Saatgeneration 5" | berichtigt auf **7** |
| `WirtschaftlichkeitSeiteGaben.cs:727` | Persistenzaussage seit B7P falsch | mit der Methode entfallen (§ 1.2) |
| `BausteineWirtschaftlichkeit.cs` (U18) | „an genau einem Ort" ohne Vorbehalt | ergänzt: gemeint ist EIN erzeugtes **Bild im Berichtsweg** — der Verlaufsdialog zeichnet dasselbe Bild, und Excel führt den Verlauf als Zahlen |

---

## 2 Bericht — G7, G8, G9

### 2.1 G8 — Bandbreite mit Spanne und Referenzzeile

Die Szenarientafel des Wortberichts führte sechs Spalten (Variante · ΔKW Worst · Erwartet · Best ·
Amortisation · Einstufung) und schloss den Stamm aus. Es fehlten die **Spanne** und die
**Referenzzeile**.

Beides ist gebaut: Die Spanne ist Best − Worst, eine reine Ableitung der beiden Nachbarspalten
(fehlt eine der Zahlen, bleibt sie „—"); die Referenzzeile steht **oben** und nennt den Stand,
gegen den jede Δ-Zahl gerechnet ist — in ihren eigenen Δ-Spalten „(Referenz)", dieselbe Anzeige
wie in der Kennzahlentabelle. Der Referenzstand wird aus der Variantenliste genommen und erscheint
nicht doppelt.

Dieselbe Tafel führt jetzt auch das **Excel-Blatt** (`ExcelBerichtGenerator.BandbreitenTafel`) —
mit derselben Spaltenfolge aus denselben Ressourcen. Die Wertspalten bleiben numerisch
(Divergenz D5): Die Referenzzeile trägt ihren Text nur in der Beschriftungsspalte.

### 2.2 G9 — der Vorschlag nennt die Referenz

Maßstab des Vorschlags ist `KapitalwertDiff`, und diese Größe rechnet seit § 2.9 gegen die
**gewählte** Referenz. `WIRT_EMPF_KEINE` sagte weiterhin „gegenüber dem Stammprojekt" — bei
gewählter Variantenreferenz schlicht falsch. Nachgezogen sind drei Texte: `WIRT_EMPF_KEINE`,
`WIRT_EMPF_SATZ` und die Δ-Fußzeile `WIRT_SZ_DELTA_FUSS`; alle drei tragen den Namen als
Platzhalter. Aufrufer, die die Gruppe nicht kennen, bekommen den sprachlichen Rückfall
„dem Referenzfall" (`WIRT_EMPF_REFERENZ_UNBENANNT`) — nie mehr fest „Stammprojekt".

### 2.3 G7 — Zeitraumhinweis im Excel-Blatt

`NutzungsdauerAbgleich.Hinweis` stand im Wortbericht und auf der Seite, im Excel-Blatt nicht.
DIN EN 17463 verlangt die Begründung des Zeitraums in jeder Ausgabe, und die Zahl T steht im
Parameternachweis ohne Einordnung. Derselbe Aufruf, dieselbe Stelle im Kopf.

### 2.4 Q19 — Kennzahlreihenfolge

`KAPITALWERT_DIFF` steht über `NETTOBARWERT`. Reine Anzeige: Die Differenzkennzahl ist die Zahl,
nach der entschieden wird, der absolute Barwert die Herleitung dazu.

---

## 3 Oberfläche und Schale (P3 der Mockup-Prüfung)

| Punkt | Befund | Änderung |
|---|---|---|
| `ErtragBonus.razor` (03/#29) | Der Knopf „Gesetzesparameter…" stand nur im BHKW-Zweig | auch im PV-Zweig, mit derselben `GesetzeGewuenscht.HasDelegate`-Wache. Der Rückruf war für die ganze Komponente längst verdrahtet — es fehlte allein das Markup |
| `VorlagenZeile.razor` (03/#10) | Die Bemessungs-Klappliste der Neuzeile war bedienbar, der Wirt reicht dort aber weder Wert noch Rückruf durch | `Aktiv="@(Schreibbar && !Neuzeile)"` wie Satz und Nutzungsdauer daneben. Gewählt wurde diese Variante, weil sie die Tests trägt: Kein Fall pinnt eine bedienbare Klappliste in der Abschlusszeile |
| `KostenKomponenteDialog.razor` (03/#26) | „Nutzungsdauern vorbelegen…" stand auf der Betriebsseite dauerhaft gesperrt da | erscheint nur, wo er wirkt (`_stand.NutzungsdauerVorbelegbar`) — ein gesperrtes Bedienelement bleibt nur stehen, wo es seinen Grund erklären kann |
| drei Katalogdialoge (04/B17) | Kostenfaktoren und Emissionen fragten über einen **synchronen** Delegaten (`Dienste.Dialog.Frage`, unter Windows eine MessageBox aus einem Blazor-Ereignis); die Leistungspreis-Reihe fragte **gar nicht** und löschte zwölf Monatssätze auf den Knopfdruck | alle drei nehmen den Baustein `Rueckfrage` mit `VorgabeNein="true"`; der Löschweg ist damit zweistufig (der Knopf fragt, gelöscht wird in der Antwort), und Esc schließt nicht, solange die Frage steht |
| `BhkwWirtschaftlichkeitDialog.razor` (04/B30) | Der OK-Weg schrieb **unbedingt** | `Schreiben(Keiner, nurBeiAenderung: true)` — dieselbe Regel wie bei den Sprungknöpfen: Wer nur nachsieht und mit OK schließt, überschriebe sonst in einer Mehrbenutzerlage fremde Änderungen mit seinem geladenen Stand |
| dito (03/#58, #59) | Kommentar „bis B6 keine Spalte" überholt; Rückfalltext `BHW_G1B` mit einem Zusatz, den die Ressource nicht führt | beides bereinigt — den Modus § 9 Abs. 1 Nr. 3 gibt es seit Schemaschritt 88, und der Rückfall ist jetzt der deutsche Ressourcentext Wort für Wort |
| `PhotovoltaikVerguetungDialog.razor` (Q12) | „Tarif…" sprang wie „Abbrechen" — die Eingaben waren weg | **Annahme, siehe § 4:** Der Knopf nimmt den OK-Weg (prüfen, schreiben, springen) und schreibt **nur bei Änderung**. Gemessen wird am Wertabzug des ganzen Modells, nicht an einem Merkflag; der Abzug entsteht **nach** der Zulässigkeitsregel, damit deren Korrektur den Knopf nicht zu einem Schreibzugriff macht. `PVV_SPRUNG_HINWEIS` ist wortgleich zum Nachbarsatz des BHKW-Dialogs neu gefasst |
| `epos-ui.css` (03/#60) | Kennzahlenzeilen mit Umbruch liefen zu einem Absatz zusammen | `white-space: pre-line` an `.epos-herleitung` |
| `EnergietraegerEinstellungen.razor` (03/#41, #42) | „Saisonale Sätze…" und „Katalogwerte übernehmen" standen in zwei eigenen Leisten; die Erklärzeile stand **vor** dem Verstoßbanner | eine Leiste für beide Knöpfe (die Bedingungen bleiben je Knopf), Banner vor der Erklärzeile — gestaffelt wird von der Meldung zum Grund |
| `KostenKomponenteHuelle.GabenIntern` (03/§ 6.2) | `TitelReiterKosten`/`TitelReiterErtrag` wurden nie belegt; die Reiter blieben in jeder Sprache deutsch | aus `KDLG_TAB_KOSTEN`/`KDLG_TAB_ERTRAG` belegt (beide Schlüssel lagen zweisprachig vor) |

### 3.1 Der Fallstrick, den 04/B30 aufwirft

Die Regel „OK schreibt nur bei Änderung" hat eine Falle, die beim Nachziehen der Tests sichtbar
wurde und **im Code behoben ist**: `Anwenden` legt den Arbeitsstand auf das hereingereichte Objekt,
und zwar **vor** dem Schreibaufruf. Scheitert dieser, gleichen Arbeitsstand und Objekt einander
seither — eine Änderungsprüfung, die nur darauf sähe, hielte den Schritt für erledigt und spränge
ihn beim zweiten OK über. Der Dialog schlösse, ohne dass die Angabe je geschrieben wäre: ein
stiller Datenverlust, schlimmer als der überflüssige Schreibzugriff, den 04/B30 abstellt.

Der Dialog merkt sich deshalb je Anlagenzeile und für die Vorgaben einen **Fehlversuch**; ein
gescheiterter Schritt bleibt fällig, bis er gelingt, und die Ankündigung unter den Sprungknöpfen
zählt ihn mit. Nachweis:
`BhkwWirtschaftlichkeitDialogTests.Ein_gescheiterter_Schritt_bleibt_faellig_ohne_erneute_Bedienung`.

Eine zweite, bewusste Folge: **Ein OK ohne Änderung meldet nicht mehr „gespeichert"**
(`Gespeichert == false`). Das ist richtig — der Wirt hat dann keinen Grund, neu zu rechnen; die
Wurzel- und Seitenfälle sind darauf nachgezogen.

### 3.2 Ressourcen

| Schlüssel | Änderung |
|---|---|
| `KOH_CO2_DOPPELT`, `KOH_CO2_STROMMIX_RUECKFALL`, `KOH_STROMST_ERLAUBNIS` | **neu**, beide Sprachen (§ 1.1, § 1.2, § 1.7) |
| `WIRT_REIHE_PV` | **neu**, beide Sprachen (§ 1.4) |
| `WIRT_SZ_SP_SPANNE`, `WIRT_SZ_BANDBREITE_TITEL`, `WIRT_EMPF_REFERENZ_UNBENANNT` | **neu**, beide Sprachen (§ 2.1, § 2.2) |
| `KDLG_LPR_LOESCHFRAGE` | **neu**, beide Sprachen — die Löschfrage der Leistungspreis-Reihe |
| `WIRT_EMPF_KEINE`, `WIRT_EMPF_SATZ`, `WIRT_SZ_DELTA_FUSS` | parametriert (Referenzname) |
| `PVV_SPRUNG_HINWEIS` | neu gefasst, beide Sprachen |
| `KDLG_ERTRAG_G_PV` | ohne „(V4/F7)" |
| `KDLG_ERTRAG_PV` | mit „(eine Vergütungswahrheit je Projekt)" |
| `ETV_BTN_SPEICHERN` | Emoji entfernt; die Verwendung zeigt jetzt auf `ADM_BTN_SPEICHERN` |
| `WIRT_CO2_STROMMIX_RUECKFALL`, `WIRT_ENK_KOPF` | **gestrichen** — ohne Leser |
| Standardknöpfe | 17 Verwendungsstellen der Bereiche Kosten und Wirtschaftlichkeit zeigen auf `ALLG_BTN_OK` / `ALLG_BTN_ABBRECHEN` / `ADM_BTN_SPEICHERN`; die alten, gleichlautenden Schlüssel bleiben in der `.resx` stehen (die Prüfmuster der Formularkarte verweisen darauf) |

`Resource.Designer.cs` ist mit `Werkzeuge/ResourceDesigner` neu erzeugt. **`python3` gibt es auf
diesem Rechner nicht, der Windows-Starter `py` schon** (Python 3.12.10); der Aufruf lautet
`PYTHONIOENCODING=utf-8 py Werkzeuge/ResourceDesigner/designer_neu.py schreiben`. Die
Umgebungsvariable ist nötig, weil das Werkzeug beim Vergleich Zeichen wie „Δ" ausgibt und die
Konsole sonst cp1252 spricht. Ein Handeintrag war damit nicht nötig.

### 3.3 Hilfe und Wiki

`help_mapping.txt`: Die Kostendialoge sprangen auf den Seitenkopf, der BHKW-Dialog hatte **gar
keine** Zeile. Neu bzw. auf Anker umgehängt:

`Form_BhkwWirtschaftlichkeit` → `Wirtschaftlichkeit#bhkw-wirtschaftlichkeit` (Anker neu angelegt) ·
`Form_PhotovoltaikVerguetung` → `Wirtschaftlichkeit#pv-verguetung` ·
`Form_Tarifstruktur` → `Wirtschaftlichkeit#strombezug` ·
`Form_WirtschaftlichkeitParameter` → `Wirtschaftlichkeit#parameter` ·
`Form_WirtschaftlichkeitVerlauf` → `Wirtschaftlichkeit#verlauf` (Anker neu angelegt) ·
`Form_KostenKomponente` → `Kosten#komponentenliste` ·
`Form_Energietraeger` → `Kosten#energietraegerverwaltung` ·
`Form_Nutzungsdauer` → `Kosten#nutzungsdauern` ·
`Form_VorlagenUebernahme` → `Kosten#uebernahme-vorlage`.

Alle Ziele bestehen in den Repo-Quellen; die beiden fehlenden Anker (`bhkw-wirtschaftlichkeit`,
`verlauf`) sind dort angelegt — ohne Inhaltstext, der nicht stimmt.

**Wiki-Quelle Wirtschaftlichkeit:** Der Abschnitt „Kohärenzprüfung" bekommt drei Sätze zu den
beiden neuen Zeilen und dazu, dass die Kohärenzzeilen jetzt auf der Seite, im Word-Bericht und im
Excel-Blatt stehen. **Kein Logbuch-Eintrag** — E2 sind Kleinigkeiten (Regel Konzept Hilfesystem
13.4). Der Entwurf ist mit der Tabuwort-Regex aus `CLAUDE.md` gegengelesen: kein Treffer.

---

## 4 Annahmen

Drei Punkte laufen auf der **Empfehlung** der Mockup-Prüfung, ohne dass der Entscheid vorliegt:

| # | Frage | Angenommen |
|---|---|---|
| **Q7** | Formelzeile der Trägerkarte auf `N4`? | **ja** — eine Zeile, sonst steht die Formel dauerhaft falsch da |
| **Q12** | PV-Sprungknopf „Tarif…": schreiben und springen oder verwerfen und es sagen? | **schreiben und springen** wie im BHKW-Dialog, `nurBeiAenderung: true`; `PVV_SPRUNG_HINWEIS` neu gefasst |
| **Q19** | Nettobarwert unter die Differenz? | **ja**, reine Anzeige |

Fällt ein Entscheid anders aus, ist jeder der drei Punkte eine Zeile zurück.

---

## 5 Was offen bleibt

| Nr | Punkt | Warum |
|---|---|---|
| 1 | **Hi/Ho am CO₂-Grenzwert** (R11) | Der Katalog führt zum Erdgas einen heizwert- (200,9 g/kWh) und einen brennwertbezogenen Faktor (181,4 g/kWh) samt Umrechnung; gelesen wird der Schlüssel der Anlage, die beiden Ho-Zeilen haben keinen Leser. Ist der Grenzwert 270 g/kWh des § 2 StromStG brennwertbezogen, fällt ein heizwertbezogener Zähler rund 10 % zu hoch aus. **Die Wahl der Bezugsgröße ändert den gebuchten Befreiungsbetrag und damit den Kapitalwert** — Rechenwirkung über den Ausweis hinaus, also E7. Das heutige Verhalten ist gepinnt (`KleinkorrekturenE2Tests`), die Frage steht am Code und in Konzept § 6.3 Punkt 29 |
| 2 | **Trägergenauigkeit des CO₂-Doppelansatzes** | Die Warnung nennt alle Träger des Projekts mit aktivem CO₂-Anteil. Welcher Träger die gebuchte Abgabe **treibt**, wüsste erst eine trägerscharfe BEHG-Menge im Lauf; sie liegt im `KohaerenzLauf` nicht vor. Der genannte Betrag ist deshalb der ganze gebuchte Jahresbetrag — richtig als Größenordnung des Doppelansatzes, nicht als Aufteilung |
| 3 | **Die übrigen 22 gleichlautenden Knopfschlüssel** | KI, Simulation, Import, Flotte, Wizard und Optimierung führen dieselbe Doppelung. Sie liegen außerhalb des Feldes dieser Etappe und sind nicht angefasst |
| 4 | **`WIRT_ENK_ANLAGE`** | derselbe Fall wie `WIRT_ENK_KOPF` (kein Leser), im Auftrag aber nicht genannt — stehen gelassen und hier vermerkt |
| 5 | **B-4 Rest, B-6, S-2, V-1, V-2, K-1, R-1, R-2** | Rechenwirkung; sie gehören zu E7 und brauchen je einen A/B-Nachweis |

---

## 6 Gate

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | **0 Fehler**, 5 Warnungen (Bestand) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | **0 Fehler**, 9.813 erfolgreich, 1 übersprungen (Bestand) |
| — `KiKern.Tests` | 499 |
| — `SpeicherEngine.Tests` | 378 |
| — `SpeicherPlanung.Tests` | 27 (+1 übersprungen, Bestand) |
| — `EPOS.UI.Tests` | **4.890** (vorher 4.888) |
| — `EPOS.Kern.Tests` | **4.019** (vorher 3.985) |
| `dotnet build WP-Plan.sln -c Debug -p:Platform=x64` | **0 Fehler**, 10 Warnungen (Bestand) |
| Ankertests (`WirtschaftlichkeitAnkerTests`, 9 Fälle) | **unverändert grün** — 99,00 €/a · ±0,00 € · −2.896.359,13 € · −21.895.377,28 € |
| Referenzlauf 1030, 1007, 1017, 1045, 1046 | 5 von 5 erfolgreich |
| Vergleich gegen `2026-09-19_R10_BhkwWirkungsgrad` | **GESAMT: PASS** (1.656.417 Werte in Toleranz) |
| Dokumentationswachen (`DokumentationLinkWache`, `RepositoryOrdnungWache`, `WikiProduktdatenWache`) | grün |
| Tabuwort-Regex über den Wiki-Entwurf | kein Treffer |
| `git status` | sauber |

Der Vergleich läuft — wie `kern.yml` — gegen eine Kopie der **fünf** Basisprojekte. Gegen die
vollständige Basis (13 Projekte) meldet er `FAIL` mit „im Vergleichslauf nicht vorhanden" für die
acht nicht gerechneten Projekte; die fünf gerechneten sind auch dort `PASS`.

### Neue und geänderte Testklassen

| Klasse | Fälle |
|---|---:|
| `EPOS.Kern.Tests/KohaerenzCo2Tests.cs` (neu) | 11 |
| `EPOS.Kern.Tests/KleinkorrekturenE2Tests.cs` (neu) | 23 |
| `EPOS.Kern.Tests/BerichtBlattstrukturWacheTests.cs` | Zeilen nachgezogen, Bandbreitentafel und Zeitraumhinweis gepinnt |
| `EPOS.Kern.Tests/ValeriLueckenTests.cs` | G9 mit und ohne genannte Referenz |
| `EPOS.Kern.Tests/EnergietraegerPreiskarteTests.cs`, `EnergietraegerHuelleTests.cs` | drei Formelzeilen auf `N4` |
| `EPOS.UI.Tests/Dialoge/BhkwWirtschaftlichkeitDialogTests.cs` | vier Fälle auf den geänderten OK-Weg, zwei neue Fälle |
| `EPOS.UI.Tests/Dialoge/*KatalogDialogTests.cs`, `LeistungspreisReiheDialogTests.cs` | zweistufiges Löschen samt „Nein löscht nicht" und Esc-Wache |
| `EPOS.UI.Tests/Dialoge/VorlagenZeileTests.cs`, `KostenKomponenteDialogTests.cs` | gesperrte Bemessung der Neuzeile, Knopfsichtbarkeit |
| `EPOS.UI.Tests/Seiten/AppWurzelTests.cs`, `WirtschaftlichkeitSeiteTests.cs` | Schreibzahl des OK-Wegs |

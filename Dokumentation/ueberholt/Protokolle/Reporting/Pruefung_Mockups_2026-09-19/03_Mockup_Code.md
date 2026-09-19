# Prüfung 03 — Mockup gegen Code: Ausgabefelder, Eingabefelder, Aktionen

Prüfgegenstand: `Dokumentation/aktuell/Mockups/Dialog_Formel_Zahlenprobe.html` (Kategorien 1–8,
Anhang Umsetzungsstand) und `Dokumentation/aktuell/Mockups/Ergebnis_Bandbreite_Herkunft.html`
gegen die Razor-Komponenten, die Hüllen und die Kern-Controller.
Stand 19.09.2026. **Nur gelesen** — keine Datei geändert, kein git, kein Build, keine Tests.

---

## 1 · Kurzbefund

Die Ressourcentafeln des Mockups sind seit #362 sauber: alle 369 als vorhanden geführten Schlüssel
stehen in beiden Sprachen im Kern, keiner der 49 als „geplant" markierten existiert vorzeitig —
drei der 369 sind allerdings Karteileichen, die im Produktcode niemand liest, darunter die beiden
Reiterbeschriftungen des Kostendialogs, die dadurch in en-US deutsch bleiben.
Alle zwanzig als „erledigt" durchgestrichenen Umsetzungspunkte halten in der Sache (bei fünf weicht
der gezeichnete Wortlaut oder eine Begleitzahl ab), und alle neunzehn offenen sind zu Recht offen —
kein einziger ihrer Schlüssel existiert außerhalb der Mockup-Datei.
Die schwersten Abweichungen sind keine Beschriftungsfragen, sondern Stellen, an denen das Mockup
einen anderen Rechen- oder Anzeigeweg beschreibt als der Code ihn geht: die innere Gliederung der
Erlösrubrik nach Komponente samt Teilsummen (U6), die getrennten Energiesteuerzeilen § 53a/§ 54 (U7),
die vertauschte Semantik von Gedankenstrich und Null in den Ergebniszellen, und die Reihenfolge der
Kennzahltafel, in der der Nettobarwert entgegen der ausdrücklichen Mockup-Begründung vor der
Kapitalwertdifferenz steht.
Sieben gezeichnete Aktionen haben im Code kein Gegenstück — „Sätze und Herkunft…", „Wahl und
Herkunft…", „Übernehmen" der Überlagerung (alle U22), „Verlauf nach Excel…" (U13) sowie
„Anhang-E-Checkliste…", „Bericht erzeugen" und „Schließen" im Fuß der Schnellwahl, von denen die
letzten drei weder eine U-Nummer noch eine Anhangzeile tragen; ein achter, „Gesetzesparameter…",
existiert im Code, wird aber im PV-Blatt nie gezeichnet.
Insgesamt 97 Abweichungen — 10 schwer, 43 mittel, 44 gering —, davon 47 reine Mockup-Pflege,
33 Code-Arbeit, 12 Konzeptentscheide und 5 gemischte.

---

## 2 · Methode

1. **Ressourcentafeln mechanisch.** Alle 109 Zellen `<td class="rs">` des HTML ausgelesen
   (458 Schlüsselnennungen, 418 verschiedene Schlüssel), getrennt nach normalen Nennungen und
   `<span class="rs-geplant">`. Gegengehalten gegen die Schlüsselmengen von
   `EPOS.Kern/MyResource/Resource.resx` und `EPOS.Kern/MyResource/Resource.en-US.resx`
   (je 6 532 Schlüssel) sowie gegen alle Großbuchstaben-Bezeichner des Produktcodes
   (`EPOS.UI`, `EPOS.Kern`, `EPOS.UI.Daten`, `WindowsFormsApplication1`, beide Testprojekte,
   ohne `Resource.Designer.cs`).
2. **Mockup-Elemente gezählt** über die CSS-Klassen der Zeichnung: `.f-eingabe` (editierbar),
   `.f-eingabe.gesperrt` (grau), `.f-combo`, `.f-check`, `.f-radio`, `.f-knopf`, `.f-zknopf`
   (Rasterknöpfe), `.f-vzeile` (Vorschau), `.f-herleitung`, `.f-warn`/`.f-hinweis`, je Fenster
   bzw. Seitenabschnitt.
3. **Code-Elemente gezählt** über die Bausteintags der Razor-Dateien (`<Zahlenfeld>`, `<Datumsfeld>`,
   `<Auswahlfeld>`, `<Schalter>`, `<Optionsgruppe>`, `<button>`, `<Vorschlagszeile>`,
   `<Herleitungszeile>`, `<Kohaerenzzeile>`, `<Warnbanner>`).
4. **Feld für Feld** je Kategorie: Existenz, Ressourcenschlüssel, Feldart (Bindung `@bind`/`WertChanged`
   gegen reine Anzeige), Einheit, Reihenfolge der Gruppen und Spalten, Knopf → Handler → Schreibweg.
   Vier parallele Teilprüfungen (Kat. 1+2, 3+4, 5+6, 7+8); die tragenden Befunde habe ich
   nachgemessen.
5. **Marker** U1–U40 gegen `class="gestrichen"` der Anhangstafel und gegen den Code.
6. **Zahlenprobe nicht nachgerechnet** — das hätte einen Lauf gebraucht, und Bauen war untersagt.
   Geprüft wurde stattdessen, ob die Beispielzahlen in den Tests gepinnt sind: 205 884, 137 256,
   640,50, 5,5667 und 32 022 sind es, 24 750, 21 203, 2 885,7, 28 344 und 77 975 nicht.

**Methodischer Hinweis:** Die Textabzüge im Scratchpad führen HTML-Entitäten
(`Fu&szlig;leiste`); Suchen mit Umlauten laufen dort ins Leere. Die Umlautsuche gehört ins HTML.

---

## 3 · Zähltabellen je Dialog

Gezählt sind bedienbare Felder je Bauart (Rasterzellen einfach, nicht je Zeile), gerechnete bzw.
gesperrte Anzeigen einschließlich Herleitungs-, Warn-, Status- und Summenzeilen, und Aktionen
einschließlich ✕ und ⓘ, ohne die je Rasterzeile wiederholten Zeilenknöpfe.

| Dialog / Blatt | Eingaben M / C | gerechnete Ausgaben M / C | Aktionen M / C | Abweichungen |
|---|---|---|---|---|
| KostenKomponenteDialog · Investitionskosten (Kat. 1) | 7 / 8 | 16 / 18 | 14 / 14 | 7 |
| KostenKomponenteDialog · Betriebskosten (Kat. 2) | 6 / 7 | 17 / 16 | 14 / 15 | 6 |
| VorlagenPositionDialog (Zeileneditor, Stift) | 5 / 6 | 0 / 1 | 3 / 3 | 2 |
| CaseEingabeDialog (Worst/Best, ±) | 5 / 7 | 0 / 1 | 3 / 3 | 2 |
| VorlagenUebernahmeDialog | 4 / 8 | — / 2 | 2 / 2 | 2 |
| KostenKomponenteDialog · Photovoltaik Invest (Kat. 3) | 8 / 8 | 9 / 14 | 12 / 14 | 3 |
| KostenKomponenteDialog · Photovoltaik Betrieb (Kat. 3) | 8 / 8 | 9 / 14 | 12 / 14 | 3 |
| ErtragBonus.razor · Reiter Ertrag/Bonus (Kat. 3) | 2 / 2 | 2 / 2 | 2 / 1 | 3 |
| EnergietraegerDialog · Trägerkarte (Kat. 4) | 24 / 24 | 15 / 23 | 17 / 17 | 2 |
| BrennstoffBestandteile · Überlagerung Schnellwahl (Kat. 4) | 0 / 0 | 16 / 8 | 6 / 5 | 2 |
| BhkwWirtschaftlichkeitDialog (Kat. 5) | 16 / 24 | 16 / 15 | 9 / 10 | 12 |
| BhkwWirtschaftlichkeitDialog · Überlagerung „Sätze und Herkunft" (Kat. 5) | 30 / 0 | 7 / 0 | 3 / 0 | vollständig offen (U22) |
| PhotovoltaikVerguetungDialog (Kat. 6) | 19 / 19 | 18 / 11 | 7 / 7 | 4 |
| Erlösrubrik (Kat. 7, `WirtschaftlichkeitZeilen`) | 0 / 0 | 29 / 31 | 0 / 0 | 9 |
| WirtschaftlichkeitSeite (Kat. 8) | 9 / 8 | 17 / 11 | 9 / 11 | 19 |
| WirtschaftlichkeitParameterDialog (Kat. 8) | 6 / 26 | 4 / 16 | 2 / 5 | 9 |
| KapitalwertVerlaufDialog (Kat. 8) | 3 / 2 | 5 / 5 | 1 / 3 | 7 |

**Zwei Zahlen zum Einordnen.** Der BHKW-Dialog zeichnet acht Wahlfelder als graue Anzeigezeile
(Zielzustand nach U22), führt im Code aber acht Klapplisten — die Zeichnung ist insoweit korrekt
vorgreifend, die Prosa daneben spricht von „sechs Wahlfeldern" und „elf editierbaren Feldern" und
ist in beiden Zahlen falsch. Der Parameterdialog wird im Mockup mit sechs Eingaben gezeichnet,
trägt im Code aber 20 Zahlenfelder, drei Klapplisten und einen Schalter in sechs Gruppen — die
Zeichnung ist ein Ausschnitt, kein Dialog.

---

## 4 · Befundtabelle

Ziel: **M** = Mockup nachziehen · **C** = Code bauen · **K** = Konzeptentscheid nötig.
Zeilenangaben „Kat. n · Z." beziehen sich auf den Textabzug `SP\Dialog_Formel_Zahlenprobe.txt`,
„2. Mockup · Z." auf `SP\Ergebnis_Bandbreite_Herkunft.txt`.

### Kategorie 1 · Investitionskosten

| Nr | Mockup-Element | Code-Stelle | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|---|
| 1 | Herleitung der Prozentzeilen „**von** 196.080,00 € …" (Kat. 1 · Z. 259, 279) | `EPOS.Kern/Controller/KostenHerleitung.cs:361-367` | Der Kern kennt nur eine Vorlage und stellt auch Prozentzeilen ein „×" voran (`KDLG_HERL_BASIS` = „× {0} · {1} · Runde {2}"). Die Schlüsseltafel des Mockups nennt selbst nur die „×"-Fassung — die Zeichnung widerspricht der eigenen Tafel | Zeichnung auf „×" ziehen; alternativ zweite Ressource `KDLG_HERL_BASIS_PROZENT` und Verzweigung an `prozent` wie in `Kurztext` | M | mittel |
| 2 | „· **Hauptposition** ·" als vierter Teil der Runde-1-Zeile (Kat. 1 · Z. 249) | `KostenHerleitung.cs:361-376` | Das Kennzeichen `IsMainComponent` wird nirgends in die Zeile geschrieben; keine Ressource dafür | Zeichnung streichen oder Segment im Kern ergänzen | M | mittel |
| 3 | „Satz = Betrag · **keine Bezugsgröße · Runde 1**" (Kat. 1 · Z. 269) | `KostenHerleitung.cs:357` (`KDLG_HERL_ABSOLUT` = „Satz = Betrag") | Zusatz existiert nicht; die Schlüsseltafel nennt korrekt nur „Satz = Betrag" | Zeichnung kürzen | M | gering |
| 4 | Zuschusszeile „🔗 6.000,00 · Kostenart Zuschuss · Erlös/Zuschuss an · nicht in der Kaskade" (Kat. 1 · Z. 289) | `KostenHerleitung.cs:129-130, 357` | Kein Sonderfall für `KOSTENART_ZUSCHUSS`; auch hier nur „Satz = Betrag" | Entscheid: Zeichnung kürzen oder Sonderzeile bauen (Kostenart + Erlöskennzeichen + Kaskadenausschluss) | K | mittel |
| 5 | „Die Zeile steht im Raster **und zusätzlich im Werkzeugtipp des Betragsfeldes**" (Kat. 1 · Z. 209) | `EPOS.UI/Dialoge/Kosten/VorlagenZeile.razor:91, 302-307`; Test `EPOS.UI.Tests/Dialoge/VorlagenZeileTests.cs:301-312` | Der Werkzeugtipp trägt `BetragKurztext` (`KDLG_TT_BETRAG_BASIS_MENGE`), nicht die Herleitungszeile — zwei verschiedene Sätze. U28 behauptet „wortgleich" | Satz im Mockup richtigstellen oder `Herleitung` an `BetragTitel` anhängen | M | mittel |
| 6 | „Stufe: **Anlage BHKW 1**" (Kat. 1 · Z. 279) | `KDLG_HERK_STUFE_ANLAGE` = „Stufe Anlage" | Kein Doppelpunkt, kein Anlagenname | Zeichnung anpassen | M | gering |
| 7 | „von 196.080,00 € · **Hauptpositionen der Komponente**" (Kat. 1 · Z. 259) | `KDLG_HERK_HAUPT` = „Hauptpositionen" | „der Komponente" fehlt im Ressourcenwert | Zeichnung kürzen oder Ressourcenwert ergänzen | M | gering |
| 8 | Tafel „Ersatz und Restwert" zeigt **zwei** Komponenten und die Summenzeile „Variante 3 — beide Anlagen" (Kat. 1 · Z. 407-430) | `WindowsFormsApplication1/Views/Kosten/KostenKomponenteHuelle.cs:504-519, 527-546`; `EPOS.Kern/Controller/ErsatzRestwertTafel.cs` | Der Dialog zeigt nur die gewählte Komponente plus deren Summe; eine variantenweite Zeile gibt es nicht | Zeichnung auf eine Komponente reduzieren (die Variantenzeile gehört auf die Ergebnisseite) | M | mittel |
| 9 | Prosa „Die Tafel steht in der Anwendung **nirgends** … Sie **gehört** als Gruppe unter das Raster" (Kat. 1 · Z. 404-406) bei gleichzeitigem Marker „umgesetzt U30" | `EPOS.UI/Dialoge/Kosten/KostenKomponenteDialog.razor:239-295` | Vorschlagston im Futur, obwohl die Gruppe gebaut ist — Widerspruch im Mockup selbst | Absatz in Ist-Form umschreiben | M | mittel |
| 10 | Abschlusszeile: Zellen Bemessung/Satz/Betrag/Nutzungsdauer **leer** (Kat. 1 · Z. 297-299) | `VorlagenZeile.razor:72-75` gegen `:78-79`; `KostenKomponenteHuelle.cs:1005-1008` | **Echter Code-Fehler:** Das Satzfeld der Neuzeile ist mit `Aktiv="@(Schreibbar && !Neuzeile)"` gesperrt, die Bemessungs-Klappliste daneben mit `Aktiv="@Schreibbar"` **nicht** — sie ist bedienbar, aber wirkungslos, weil `PositionNeu` immer `BEMESSUNG_BETRAG` setzt | `Aktiv="@(Schreibbar && !Neuzeile)"` auch an der Klappliste, oder Auswahl an `PositionNeu` durchreichen | C | mittel |
| 11 | „die Hauptposition (`Tab_Kostenfaktor.IsMainComponent`) **kommt aus dem Positionskatalog**" (Kat. 1 · Z. 364) | `EPOS.Kern/Controller/KostenfaktorCtrl.cs:49-65` (`WHERE IsMainComponent = False`) | Der Positionskatalog liest und löscht ausschließlich Nicht-Hauptpositionen; das Kennzeichen ist dort weder sichtbar noch pflegbar. Die Aussage ist falsch | Satz richtigstellen (Kennzeichen wird beim Ausrollen gesetzt) oder Pflegeort schaffen | M / K | mittel |
| 12 | Zeileneditor zeichnet **vier** Felder (Kat. 1 · Z. 340-351) | `EPOS.UI/Dialoge/Kosten/VorlagenPositionDialog.razor:60-68` | Das Feld **„Positionsart:"** samt Erklärzeile fehlt in der Zeichnung, obwohl die Schlüsseltafel `VPOS_LBL_POSITIONSART` führt. Codereihenfolge: Bezeichnung → Kostenart → Erlös → Positionsart → Empfehlung von/bis | Feld in die Zeichnung aufnehmen | M | mittel |
| 13 | Worst/Best-Überlagerung: nur Best-/Worst-Betrag, Dauer, Startjahr (Kat. 1 · Z. 400) | `EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor:43-92` | Zusätzlich vorhanden: Optionsgruppe „Eingabeart" (absolut / % vom Erwartungswert), Umrechnungszeile, Zuschuss-Schalter. Schlüssel `KCASE_BEST_A`, `KCASE_WORST_A`, `KOSTEN_CASE_ABSOLUT`, `KOSTEN_CASE_PROZENT`, `KCASE_G_*`, `KCASE_ERLOES_HINWEIS` fehlen im Mockup | Mockup ergänzen | M | gering |
| 14 | Übernahme-Überlagerung: „Zielprojekt · Vorlage/Variante · Quellprojekt · Quellanlage" (Kat. 1 · Z. 357) | `EPOS.UI/Dialoge/Kosten/VorlagenUebernahmeDialog.razor:74-147`; `KostenKomponenteDialog.razor:1179-1192` | Zusätzlich Klappliste Komponente, Optionsgruppe Kategorie und Positionsvorschau-Raster (#363); die Kategoriewahl schaltet nach der Übernahme die gezeigte Kategorie um — das Mockup erwähnt es nicht | Mockup ergänzen | M | gering |
| 15 | Summenfuß Investition: **drei** Zeilen (Kat. 1 · Z. 303-309) | `KostenKomponenteHuelle.cs:966-972`; `EPOS.Kern/Controller/KostenSummenCtrl.cs:455-468` | Vierte Zeile „spezifisch {0} €/kWp" (`KDLG_KENN_EUR_KWP`, U34) — am gezeichneten BHKW leer, in Kategorie 1 aber nirgends erwähnt | Querverweis in Kategorie 1 ergänzen | M | gering |
| 16 | Nutzungsdauer-Zelle zeigt nur „15" / „20" / leer (Kat. 1 · Z. 250 ff.) | `VorlagenZeile.razor:147-150`; `KostenKomponenteHuelle.cs:750-753` | Darunter steht immer die Herleitungszeile „15 a · Vorgabe der Technik" (`ND_ZEILE_HERLEITUNG`); die Schlüsseltafel führt sie, die Zeichnung nicht | Zeichnung ergänzen | M | gering |
| 17 | Spaltenkopf „Nutzungsdauer [a]" (Kat. 1 · Z. 240) | `KostenKomponenteDialog.razor:557`; `KostenKomponenteHuelle.cs:230` | Zur Laufzeit korrekt (Ressource `KDLG_SP_NUTZUNG` gewinnt); nur die deutschen Rückfallwerte lauten „Nutzung [a]" | Rückfallwerte angleichen | C | gering |

### Kategorie 2 · Betriebskosten

| Nr | Mockup-Element | Code-Stelle | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|---|
| 18 | Gruppe „Endenergie je Komponente" mit **sechs** Spalten: Komponente · Energieträger · Endenergie · Arbeitspreis · Endenergiekosten · Anmerkung (Kat. 2 · Z. 809) | `KostenKomponenteDialog.razor:210-216` (**vier** `<th>`); `KostenKomponenteDaten.cs:176`; `EPOS.Kern/Controller/KostenBetriebsstand.cs:95-114` | Die Spalten **Energieträger** und **Arbeitspreis** existieren nicht. Die Schlüsseltafel des Mockups (Kat. 2 · Z. 991) nennt selbst nur vier Spalten — die Zeichnung widerspricht der eigenen Tafel **und** dem Code | Entscheid: Zeichnung auf vier Spalten kürzen oder `EndenergieZeile` um Träger und Arbeitspreis erweitern (die Daten liegen im `EndenergieAufloeser` vor) | K | **hoch** |
| 19 | Betriebszeile „× 1.650.000 kWh **Stromerzeugung · BHKW 1** · **aus dem** Lauf" (Kat. 2 · Z. 712, 722, 732) | `KostenHerleitung.cs:196-210`; `KDLG_HERK_LAUF` = „Lauf", `KDLG_HERK_INVEST` = „Investitionssumme" | Der Anlagenname steht nicht in der Zeile; die Einheit ist „kWh", nicht „kWh Stromerzeugung"; die Herkunft heißt „Lauf", nicht „aus dem Lauf" | Zeichnung an die Ressourcenwerte ziehen | M | mittel |
| 20 | „… → **21.710 kWh Strom**" (rückgerechnete Menge, Kat. 2 · Z. 732 und Rechenschritt 6, Z. 946) | keine Entsprechung; `EPOS.Kern/Allgemein/Wirtschaftlichkeit/HilfsstromRechner.cs:298-301` verwirft ausdrücklich „eine zweite, zurückgerechnete Menge daneben" | Der Dialog zeigt die Menge nicht, und eine Bestandsentscheidung spricht dagegen | Zeichnung streichen oder Entscheid neu fassen | M / K | mittel |
| 21 | Pflichtzeilen tragen im Positionsfeld das Abzeichen „**Pflicht nach VDI 2067**" (Kat. 2 · Z. 709, 719, 729) | repoweit 0 Treffer; `VorlagenZeile.razor:52-57` kennt nur das 🔒 | Abzeichen existiert nicht, keine Ressource; die Schlüsseltafel nennt es ebenfalls nicht | Zeichnung streichen oder Abzeichen samt Ressource bauen | M | mittel |
| 22 | Empfehlungszeile steht im **Positionsfeld** (Kat. 2 · Z. 709) | `VorlagenZeile.razor:77-89` — unter dem **Satzfeld** | Die Zeichnung widerspricht der eigenen Prosa (Kat. 2 · Z. 773) und dem Code | Zeichnung verschieben | M | gering |
| 23 | Wortlaut „Empfehlung**:** 0,02 **–** 0,04 €/kWh" (Kat. 2 · Z. 709) | `KDLG_EMPF_ZEILE` = „Empfehlung {0} **bis** {1} {2}"; `KDLG_TT_EMPFEHLUNG` = „Empfehlung: {0} – {1} {2}" | Die Zeichnung zeigt an der sichtbaren Zeile den Werkzeugtipp-Wortlaut | Zeichnung auf `KDLG_EMPF_ZEILE` ziehen | M | gering |
| 24 | Doppelpflege-Warnung **unter** dem Raster (Kat. 2 · Z. 765) | `KostenKomponenteDialog.razor:118-124` — **über** Laufstand und Raster | Reihenfolge weicht ab; die Code-Stelle ist im Kommentar begründet | Mockup nachziehen | M | gering |
| 25 | Gruppen „Endenergie" und „Ersatz und Restwert" **außerhalb** des Fensters, nach der Fußleiste | `KostenKomponenteDialog.razor:203-295` — im Reiterblatt **vor** der Knopfleiste (`:297`) | Reihenfolge Gruppen ↔ Knopfleiste weicht ab | Zeichnung als Gruppe in das Fenster ziehen | M | gering |
| 26 | Knopfleiste der Betriebskosten **ohne** „Nutzungsdauern vorbelegen…" (Kat. 2 · Z. 785) | `KostenKomponenteDialog.razor:297-310`; `KostenKomponenteHuelle.cs:186-187, 498` | Der Knopf ist auf der Betriebsseite **sichtbar, nur gesperrt** — das Mockup zeigt ihn gar nicht. Hausregel „ohne Wirkung kein Knopf" | Sichtbarkeit an `_stand.NutzungsdauerVorbelegbar` koppeln | C | gering |
| 27 | Gründe ohne Bezugsgröße: sechs Texte in der Prosa, acht `KDLG_BASIS_GRUND_*` in der Schlüsseltafel (Kat. 2 · Z. 322-328, 617) | `KostenHerleitung.cs:272-304` — **neun** Steuerwerte, zusätzlich `KDLG_BASIS_GRUND_STROMPREIS` (#365) | Der neunte Grund fehlt im Mockup vollständig | Grund in Prosa und Schlüsseltafel aufnehmen | M | mittel |
| 28 | Marker „umgesetzt U40" an der Vorlagenzeile (Kat. 2 · Z. 732) | `VorlagenZeile.razor:115-133`; `KostenHerleitung.cs:156-184`; `KostenKomponenteHuelle.cs:733-740` | Funktion belegt, aber **U40 hat keine Zeile in der Anhangstafel** — die Tafelregel verlangt für Erledigtes eine durchgestrichene Zeile mit Grund. Im Code fehlt außerdem die U-Marke im Kommentar (anders als bei U8/U28–U31/U33–U35) | Anhangzeile U40 nachtragen (durchgestrichen, mit Grund); U40 als Kommentarmarke ergänzen | M + C | mittel |

### Kategorie 3 · Kosten der Photovoltaik

| Nr | Mockup-Element | Code-Stelle | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|---|
| 29 | Knopf „Gesetzesparameter…" im Reiter Ertrag/Bonus der Photovoltaik (Kat. 3 · Z. 1339, Schlüsseltafel Z. 1458) | `EPOS.UI/Dialoge/Kosten/ErtragBonus.razor:54-62` (nur im `@if (IstBhkw)`-Zweig) gegen `:65-111` (PV-Zweig ohne den Knopf) | **Der Knopf wird im PV-Blatt nie gezeichnet.** Rückruf (`KostenKomponenteDialog.razor:329`) und Text (`ErtragBonusGaben.cs:65`) liegen bereit — der im Mockup zugesagte Weg in den Gesetzeskatalog fehlt | Denselben Knopf in den PV-Zweig aufnehmen, mit `GesetzeGewuenscht.HasDelegate`-Wache wie beim BHKW | C | **hoch** |
| 30 | Papierkorb 🗑️ an den beiden PV-Pflichtzeilen, „tragen denselben Papierkorb wie jede andere Zeile" (Kat. 3 · Z. 1275) | `VorlagenZeile.razor:52-63` | Der Code zeigt an Pflichtzeilen ein **🔒** (U31). Kategorie 2 desselben Mockups beschreibt genau das — Kategorie 3 widerspricht sich intern | Kategorie 3 auf 🔒 und den Wortlaut aus Kategorie 2 ziehen | M | mittel |
| 31 | Klappliste Bemessung im PV-**Betriebs**raster: „vier Arten" (Kat. 3 · Z. 1171, Abnahme Z. 1479) | `EPOS.Kern/Controller/KostenVorlagenCtrl.cs:665, 676, 817-830`; `TechnikPlanwertCtrl.cs:893-898`; Test `EPOS.Kern.Tests/BemessungsauswahlJeGewerkTests.cs:139-146` | Der Code bietet **fünf** Arten — zusätzlich „je kW elektrisch" (`BM_KW_ELEKTRISCH`, `FuerBetrieb = true`). Der Test pinnt die fünf | Mockup auf fünf Arten ergänzen | M | mittel |
| 32 | Fußleiste des Rasters: „drei Knöpfe" (Kat. 3 · Z. 1025, 1152, 1289) | `KostenKomponenteDialog.razor:301-316`; `KostenKomponenteHuelle.cs:186-187, 254` | Vierter Knopf „Nutzungsdauern vorbelegen…" steht auch an der Photovoltaik | Vierten Knopf in Kategorie 1 und 3 ergänzen | M | mittel |
| 33 | Erklärzeile „eigene Werte dieser Variante — **Variante Photovoltaik**" (Kat. 3 · Z. 1335) | `ErtragBonusGaben.cs:247-248`; `KDLG_ERTRAG_PV_HERK_EIGEN` = „eigene Werte dieser Variante" | Der Variantenname fehlt im Code | Mockup kürzen oder Ressource um `{0}` erweitern | M | gering |
| 34 | Gruppenkopf „PV-Vergütung (EEG) — eine Vergütungswahrheit **je Projekt**" (Kat. 3 · Z. 1325) | `KDLG_ERTRAG_G_PV` = „… — eine Vergütungswahrheit (V4/F7)" | Der Ressourcentext trägt die interne Merkkennung „(V4/F7)" statt der lesbaren Fassung; im Erklärsatz `KDLG_ERTRAG_PV` fehlt der Zusatz „(eine Vergütungswahrheit je Projekt)" | Ressourcentexte auf die Mockup-Fassung ziehen (beide Sprachen) | C | gering |
| 35 | Schlüsseltafel nennt die Klappliste „**Stammprojekt:**" (Kat. 3 · Z. 1458), die Zeichnung „Projekt:" (Z. 1337) | `ErtragBonusGaben.cs:68`, `KDLG_ERTRAG_PV_PROJEKT` = „Projekt:" | Mockup-interner Widerspruch; der Code folgt der Zeichnung | Schlüsseltafel korrigieren | M | gering |
| 36 | Reiter Ertrag/Bonus zeichnet Optionsgruppe **und** Projekt-Klappliste zugleich (Kat. 3 · Z. 1330-1337) | `ErtragBonus.razor:74-83` gegen `:97-105`; `ErtragBonusGaben.cs:203-209` | Im Code schließen sie sich aus (`ProjektlisteZeigen = !projektModus`) | Anmerkung am Bild auf „entweder / oder" verschärfen | M | gering |

### Kategorie 4 · Energiekosten

| Nr | Mockup-Element | Code-Stelle | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|---|
| 37 | Nachweisumschlag: „Die **Fassung ist 1**", „vier Nachweislisten und **vier** Skalare" (Kat. 4 · Z. 1873-1876) | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/ErgebnisNachweisUmschlag.cs:53, 72-116` | `FASSUNG = 3`; neben den vier Listen und vier Skalaren führt der Umschlag `KwkgPauschaleEur` (Fassung 2, U17), `PvVerguetungUebernommen` und `PvVerguetungQuelle` (Fassung 3, U38). Präfix `nw1:` und Grenze 4 MiB stimmen | Mockup auf Fassung 3 und sieben Skalare ziehen | M | mittel |
| 38 | Fuß der Überlagerung „Schnellwahl aus Katalog": Statuszeile „Ein Satz schreibt in das Feld seines Bestandteils …" **und** Knopf „Schließen" (Kat. 4 · Z. 1706-1707) | `EPOS.UI/Dialoge/Kosten/BrennstoffBestandteile.razor:96-106`; `EPOS.UI/Bausteine/Ueberlagerung.razor:45-71` | **Beides fehlt.** Geschlossen wird nur über ✕ im Kopf oder Esc; es gibt keinen Fußtext und keine Ressourcenschlüssel dafür | Fußzeile mit Statustext und „Schließen" bauen (zwei Schlüssel neu, de/en) | C | mittel |
| 39 | Überlagerung Schnellwahl mit **fünf** Spalten: Satz · Katalogwert · Jahr · Herkunft · in der Abrechnungseinheit (Kat. 4 · Z. 1661, 1685) | `BrennstoffBestandteile.razor:255-266`; `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs:2378-2383` | Der Code rendert drei Teile je Zeile; Katalogwert und Jahr stecken im Herkunftstext `BB_QUELLE` = „{0} {1} (ab {2}, {3})" | Mockup auf drei Spalten ziehen oder `Schnellwahlsatz` in drei Felder zerlegen | M | gering |
| 40 | Summenzeile der Emissionen als **Tabellenzeile** mit Wert- und Einheitenspalte (Kat. 4 · Z. 1590) | `EPOS.UI/Dialoge/Kosten/EnergietraegerEinstellungen.razor:273` | Der Code rendert sie als `<Kohaerenzzeile>` **unter** dem Raster — inhaltlich gleich (`KDLG_EM_SUMME`), aber kein Rasterfuß | Mockup auf die Textzeile ziehen | M | gering |
| 41 | Block A: „Saisonale Sätze…" und „Katalogwerte übernehmen" in **einer** Knopfzeile (Kat. 4 · Z. 1531) | `EnergietraegerEinstellungen.razor:110-113` und `125-128` | Zwei getrennte `epos-leiste`-Blöcke untereinander, je mit eigener Bedingung | Beide Knöpfe in eine Leiste ziehen, Bedingungen belassen | C | gering |
| 42 | Block D: „Regelblock · Regel hinzufügen · Verstoßbanner · **darunter** die Hinweiszeile" (Kat. 4 · Z. 1619) | `EnergietraegerEinstellungen.razor:333-345` | Die Hinweiszeile (`ETV_REGELN_HINWEIS`, Z. 340) steht **vor** dem Verstoßbanner (Z. 342-345) | Reihenfolge im Code tauschen oder Mockup-Satz umformulieren | C | gering |
| 43 | U32-Markertext nennt Schemaschritt „(92 und aufwärts, nach Vergabe)" | `EPOS.Kern/Allgemein/Update/SchemaStand.cs:234-250` | 92, 93 und 94 sind inzwischen vergeben; nächste freie Nummer ist 95 | Markertext auf „95 und aufwärts" ziehen | M | gering |

### Kategorie 5 · Vergütungen BHKW

| Nr | Mockup-Element | Code-Stelle | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|---|
| 44 | Prosa „Die **sechs** Wahlfelder — Anlagenart, Tatbestand, Energiesteuer, Aufteilung, Unternehmensart, Modus" (Kat. 5 · Z. 1954) und U22-Abnahme „keine Klappliste im Formular" | `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor:139, 144, 198, 203, 297, 302, 325, 340` | Es sind **acht** `<Auswahlfeld>` — „Aufteilung" kommt zweimal vor (anlagenbezogen `:203`, projektweit `:302`). Die Zeichnung zeigt korrekt acht graue Anzeigezeilen, die Prosa zählt sechs | Prosa und U22-Abnahme auf acht korrigieren | M | mittel |
| 45 | Abnahmesatz „seine **elf** editierbaren Felder" (Kat. 5 · Z. 2893) und U22-Abnahme „die **elf** Zahlen- und Datumsfelder" | `BhkwWirtschaftlichkeitDialog.razor` — 9 `<Zahlenfeld>`, 4 `<Datumsfeld>`, 3 `<Schalter>` | Der Satz zählt selbst **neun** Zahlenfelder auf und nennt die vier Datumsfelder und drei Schalter getrennt danach; „elf" stimmt an keiner Lesart. Zahlen-+Datumsfelder = **dreizehn** | „elf editierbare Felder" → „neun Zahlenfelder"; U22-Abnahme → „dreizehn Zahlen- und Datumsfelder" | M | mittel |
| 46 | Gruppe 1b **ohne** Knopf am Feld; „Vorschlag übernehmen" nur in der Überlagerung (Kat. 5 · Z. 1991-2048) | `BhkwWirtschaftlichkeitDialog.razor:167, 175, 183` (`<Vorschlagszeile Knopftext="@_t.BtnVorschlagFeld">`) | Der Code hat **drei** Knöpfe direkt am Feld (Einspeisung, Eigenstrom, Kontingent); für den Jahresdeckel gibt es keinen, obwohl die Überlagerung vier fordert | Zielzustand nach U22; bis dahin zeigt das Mockup den Bestand nicht | K (U22) | mittel |
| 47 | Herleitung unter Satz Einspeisung/Eigenstrom: „§ 7 Abs. 1 KWKG 2025, **Stichtagsjahr** 2026: 50 × 8,00 + … = 1.670 ÷ 300" (Kat. 5 · Z. 2006, 2010) | `WIRT_KWKG_HERLEITUNG_TRANCHEN` = „{0} kW nach Leistungsanteilen: {1} → Mischsatz {2} ct/kWh ({3}, Stand {4})."; erzeugt in `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KwkgSatzRechner.cs:263-267` | Der Ressourcentext nennt je Tranche „50,0 kW × 8,00", keine Zwischensumme und „Stand" statt „Stichtagsjahr" | Mockup auf den Ressourcentext ziehen (oder Ressource ändern und U26-Wächter nachziehen) | M | mittel |
| 48 | Herleitung Eigenstrom „§ 7 Abs. 2 **mit** § 6 Abs. 3 Nr. 2" (Kat. 5 · Z. 2010) | `KwkgSatzRechner.cs:213` `NormEigen` = „§ 7 Abs. 2 **i.V.m.** § 6 Abs. 3 Nr. 2 KWKG 2025" | Wortlaut abweichend | Mockup nachziehen | M | gering |
| 49 | Herleitung unter Vbh-Kontingent „§ 8 Abs. 1, neue Anlage" (Kat. 5 · Z. 2014) | `WIRT_KWKG_KONTINGENT_NEU` = „neue Anlage → {0} Vbh ({1}, Stand {2}); eine Kostenschwelle gibt es hier nicht." | Gekürzt, andere Wortstellung | Mockup nachziehen | M | gering |
| 50 | Anzeigezeile unter „Anteil Neuherstellungskosten": „wählt mit der Anlagenart die Kontingentstufe … 0 = nicht gepflegt" (Kat. 5 · Z. 2025) | `BhkwWirtschaftlichkeitDialog.razor:194-196` — danach folgt keine Zeile | Die Zeile existiert nicht und hat keinen Schlüssel, auch nicht in der Schlüsseltafel des Mockups | Zeile bauen (neuer Schlüssel `BHW_A_KOSTENANTEIL_HINWEIS`) oder aus dem Mockup streichen | K | gering |
| 51 | Schlusszeile Gruppe 2 endet auf „… Katalogvorschlag und Wahl in der Überlagerung ‚Sätze und Herkunft'." (Kat. 5 · Z. 2083) | `BHW_P_NUR_PROJEKTWEIT` endet auf „… mit einem Knopf für den Katalogvorschlag am Feld." | Das Mockup führt bereits den U22-Wortlaut, die Ressource den Bestand — so in der U22-Zeile auch angekündigt | mit U22 nachziehen | K (U22) | gering |
| 52 | Gruppentitel „Σ Vorschau — zuletzt gebuchter Lauf · **Jahr 1 (2026)**" (Kat. 5 · Z. 2162) | `BHW_G6` = „Vorschau — zuletzt gebuchter Lauf" | Zusatz fehlt im Code | Mockup nachziehen | M | gering |
| 53 | Standzeile „Stand **des Laufs vom** 18.09.2026 — …" (Kat. 5 · Z. 2180) | `BHW_V_STAND` = „Stand: {0} — nach dem Speichern neu berechnen." | Wortlaut abweichend | Mockup nachziehen | M | gering |
| 54 | Vorschau zeigt nur den Block Blockheizkraftwerk und eine projektweite Zeile (Kat. 5 · Z. 2164-2180) | `BhkwWirtschaftlichkeitDialog.razor:1144-1167`; Zeilenquelle `WirtschaftlichkeitZeilen.cs:349-600` | Der Code zeigt die **ganze** Rubrik beider Blöcke samt Blockköpfen, VBH-Zeile, U23-Satzzeilen, PV-Zeilen und vermiedenen Kosten. `WIRT_ERL_TEILSUMME` und `WIRT_ERL_PROJEKTWEIT` existieren nicht | mit U6 bauen oder Mockup auf die Bestandsrubrik ziehen | K (U6) | **hoch** |
| 55 | Vorschauzeilen „Zuschlag Kraft-Wärme-Kopplung", „Energiesteuer-Gutschrift Brennstoff", „Summe Blockheizkraftwerk" (Kat. 5 · Z. 2164, 2170, 2174) | `WIRT_ZEILE_KWK_ZUSCHLAG` = „KWK-Zuschlag (§ 7 KWKG) [€/a]", `WIRT_ERL_A_ENERGIESTEUER` = „Energiesteuer-Entlastung (§ 53/§ 53a bzw. § 54 EnergieStG) [€/a]", `WIRT_ERL_A_SUMME` = „Summe Erlöse und Vorteile, zahlungswirksam [€/a]" | Drei andere Titel | mit U6 vereinheitlichen | K (U6) | mittel |
| 56 | Kohärenz-Prosa nennt **fünf** Abweichungsfälle (Kat. 5 · Z. 2136-2139) | `Resource.resx` führt neun `KOH_FALL*`-Schlüssel (u. a. `KOH_FALL2_STROMST_9B`, `KOH_FALL3_STROMSTEUER`, `KOH_FALL4_EINHEIT_UNVERGLEICHBAR`) | Aufzählung unvollständig | Mockup nachziehen | M | gering |
| 57 | Gruppe 5 Hilfsstrom (Kat. 5 · Z. 2145-2156) | `BhkwWirtschaftlichkeitDialog.razor:404-407` (`BHW_H_KESSEL`) | Bei Heizkessel steht eine zusätzliche Hinweiszeile, die das Mockup nicht kennt (im kesselfreien Beispiel unsichtbar) | Bedingten Fall im Mockup ergänzen | M | gering |
| 58 | Veralteter Rückfalltext `BHW_G1B` | `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitTexte.cs:36` | Der deutsche Rückfall „… leer bzw. 0 = Projektvorgabe" widerspricht seit BK1 der Regel „Das Feld ist der Satz"; die Ressource trägt den richtigen Text | Rückfalltext angleichen | C | gering |
| 59 | Veralteter Kommentar im Schreibweg | `BhkwWirtschaftlichkeitDialog.razor:1214-1215` | Der Kommentar behauptet, der Modus des § 9 Abs. 1 Nr. 3 werde nicht geschrieben; tatsächlich schreibt `BhkwWirtschaftlichkeitDaten.cs:468-471` ihn, die Spalte existiert (U20) | Kommentar streichen | C | gering |

### Kategorie 6 · Vergütungen Photovoltaik

| Nr | Mockup-Element | Code-Stelle | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|---|
| 60 | PV-Kennzahlen als **zwei** Herleitungszeilen (Kat. 6 · Z. 3068, 3070) | `EPOS.UI/Dialoge/Wirtschaftlichkeit/PhotovoltaikVerguetungDialog.razor:207`; Text aus `PvKennzahlenRechner.cs:148-170` mit `Environment.NewLine`; `epos-ui.css:925-928` ohne `white-space` | Der Umbruch fällt in HTML zusammen — im Dialog steht eine durchlaufende Zeile | `white-space: pre-line` auf `.epos-herleitung` oder zwei Zeilen rendern | C | gering |
| 61 | Herkunftszeile „Vergütung dieser Variante: eigene Werte **— Variante Photovoltaik**" (Kat. 6 · Z. 2939) | `PVW_HERKUNFT_EIGEN` = „Vergütung dieser Variante: eigene Werte"; erzeugt in `WindowsFormsApplication1/Views/Wirtschaftlichkeit/PhotovoltaikVerguetungHuelle.cs:239` | Zusatz mit Variantennamen existiert nicht | Mockup nachziehen oder Zusatz bauen | M | gering |

### Kategorie 7 · Erlösrubrik

| Nr | Mockup-Element | Code-Stelle | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|---|
| 62 | Innere Gliederung nach Komponente mit Zwischensummen und Block „projektweit" (Kat. 7 · Z. 3419-3487) | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs:349` (nur Kopf A), `:535` (nur Kopf B), `:504-521` (eine Summe) | Es gibt nur die zwei Blockköpfe, keine Komponentenblöcke, keine Teilsummen, kein „projektweit". `WIRT_ERL_KOMPONENTE`, `WIRT_ERL_PROJEKTWEIT`, `WIRT_ERL_TEILSUMME` existieren repoweit nicht | U6 bauen (braucht Eigenverbrauchsmengen je Anlage aus der Strommatrix) | C (U6) | **hoch** |
| 63 | „Eine Zelle ohne Betrag trägt einen Gedankenstrich **und den Grund**; eine 0 steht nur, wo null gerechnet wurde" (Kat. 7 · Z. 3512-3515) | `WirtschaftlichkeitZeilen.cs:151` und `:159-166` | **Vertauschte Semantik:** ohne Wert ⇒ bares `„—"` ohne Grund; Wert 0 mit `Grundtext` ⇒ `„0 — ‹Grund›"`. Die gezeichneten Zellen (`— keine KWK-Anlage`) entstehen im Code als `0 — keine KWK-Anlage`. Die Zahlen selbst sind nicht betroffen (`ExcelWert` bleibt numerisch) | `Wert` für diese Zeilen nullbar führen und `Anzeige` so ändern, dass `Grundtext` bei `!v.HasValue` greift und die gerechnete 0 grundlos bleibt | C | **hoch** |
| 64 | § 53/53a („Gutschrift Brennstoff") und § 54 („Entlastung Heizstoff") als **zwei** Zeilen bei ihren Komponenten (Kat. 7 · Z. 3438, 3462) | `WirtschaftlichkeitZeilen.cs:414-426` mit ausdrücklichem Kommentar „stehen in EINER Zahl"; Ursache `SteuerGutschriftRechner.cs:171` (`EnergiesteuerEur` als Summe, obwohl `:37, 41` `SchluesselSatz53a` und `SchluesselSatz54` getrennt aufgelöst werden) | Eine kombinierte Zeile; `WIRT_ERL_ENERGIEST_54` fehlt | U7 bauen: `SteuerErgebnis` um einen zweiten Betrag erweitern, zwei Zeilen | C (U7) | **hoch** |
| 65 | Gründe der Nullzeilen „kein Kesselbrennstoff", „keine Bezugsspitze gerechnet" (Kat. 7 · Z. 3462, 3487) | `WirtschaftlichkeitZeilen.cs:359, 424, 434, 450` — nur vier `Grundtext`-Zeilen | `WIRT_ERL_GRUND_KEIN_KESSELBRENNSTOFF` und `WIRT_ERL_GRUND_KEINE_BEZUGSSPITZE` fehlen | mit 62 und 64 bauen | C | mittel |
| 66 | Block B: nur „Vermiedene Stromkosten wirksam" je Komponente (Kat. 7 · Z. 3494-3505) | `WirtschaftlichkeitZeilen.cs:549-568` | Der Code führt zusätzlich „brutto", „davon Arbeit", „davon Leistung" und „abzüglich § 9b". Das zweite Mockup (`Ergebnis_Bandbreite_Herkunft.txt:844-878`) zeigt brutto + Abzug + wirksam — **die beiden Mockups widersprechen einander** | Kategorie 7 an das zweite Mockup angleichen | M | mittel |
| 67 | „Vermiedene Stromkosten — Leistungsanteil" als eigene **projektweit**-Zeile mit negativem Betrag (Kat. 7 · Z. 3505) | `WirtschaftlichkeitZeilen.cs:553` (`WIRT_ZEILE_VERMIEDEN_LEISTUNG` als Einzugszeile unter „brutto", positiv) | Ort und Vorzeichen weichen ab | mit 62 bauen | C | gering |
| 68 | Blockköpfe „A — zahlungswirksam, geht in den Kapitalwert" / „B — Ausweis, nie in der Summe" (Kat. 7 · Z. 3417, 3489) | `WIRT_ERL_KOPF_A` = „Erlöse und Vorteile — zahlungswirksam (Jahr 1)", `WIRT_ERL_KOPF_B` = „Ausweis — nicht in der Summe" | Gleiche Schlüssel, anderer Text | Zeichnung auf die Ressourcentexte ziehen | M | gering |
| 69 | Fußhinweis „Block B wird nicht summiert — die Beträge überschneiden sich" unter dem Block (Kat. 7 · Z. 3507) | kein Fundort | Der Hinweis fehlt als eigene Fußzeile; `WIRT_ERL_KOPF_B` trägt ihn nur sinngemäß im Kopf | Zeile ergänzen oder Mockup kürzen | C / M | gering |
| 70 | Spalte „Grundlage" in der Erlösrubrik (2. Mockup · Z. 768) | `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor:243-257` (Kopf „Kennzahl" plus je Version eine Spalte) | Die Matrix führt keine Grundlagenspalte; Herleitungen stehen als eigene Textzeilen | Entscheid: Spalte bauen oder Mockup nachziehen | K | mittel |

### Kategorie 8 · Wirtschaftlichkeit über die Nutzungsdauer

| Nr | Mockup-Element | Code-Stelle | Befund | Vorgeschlagene Änderung | Ziel | Schwere |
|---|---|---|---|---|---|---|
| 71 | Kennzahltafel: ΔKW, Annuität, Amortisation, IRR, Gestehungskosten, **Nettobarwert zuletzt** — mit ausdrücklicher Begründung (Kat. 8 · Z. 3860-3890) | `WirtschaftlichkeitZeilen.cs:604` (`NETTOBARWERT`) **vor** `:607` (`KAPITALWERT_DIFF`), dann `:612, 617, 622, 629` | Die Codereihenfolge widerspricht der im Mockup begründeten Ordnung („Der absolute Nettobarwert steht bewusst unter der Differenz") | Zeilenreihenfolge tauschen (reine Anzeige, Referenzlauf unberührt) | C | mittel |
| 72 | Bandbreitentafel auf der **Seite**, Spalten Version/Ungünstig/Erwartet/Günstig/**Spanne**/Einstufung, Stammzeile „Referenz" (Kat. 8 · Z. 3899-3925) | nur im Wortbericht `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs:922-981`; Spalten `:943-948` mit **Amortisation** statt Spanne; Stamm ausgeschlossen `:926` | Auf der Seite gar nicht; im Bericht fehlen „Spanne" und die Referenzzeile | U4 / U14 bauen | C | **hoch** |
| 73 | Gliederung des Kapitalwerts: sechs Bestandteile, **Barwert groß und Nominalsumme klein**, Differenzspalte (Kat. 8 · Z. 4116-4180) | `WirtZeile` (`WirtschaftlichkeitZeilen.cs:28-173`) hat **kein** Nominalfeld | Nominalsummen und Differenzspalte fehlen auf der Seite | Code bauen | C | **hoch** |
| 74 | Verlauf als eigener **Seitenabschnitt**, drei Szenarien in einem Bild, Strichart je Szenario (Kat. 8 · Z. 3975-4030) | `EPOS.UI/Dialoge/Wirtschaftlichkeit/KapitalwertVerlaufDialog.razor:47-49` (eine Szenario-Klappliste, ein Szenario), Aufruf als Überlagerung `WirtschaftlichkeitSeite.razor:452-464` | Ein Szenario je Lauf, hinter einem Knopf | U3 bauen | C (U3) | **hoch** |
| 75 | Annahmentafel, acht Zeilen × drei Szenarien + Herkunft, Spalten **Ungünstig / Erwartet / Günstig** (Kat. 8 · Z. 4566-4600) | Seite: nur Fließtext-Herleitungen `WirtschaftlichkeitSeite.razor:171, 173, 181, 185`; Bericht `BausteineWirtschaftlichkeit.cs:1012`; Parameterdialog-Tafel `WirtschaftlichkeitParameterDialog.razor:126-129` in der Folge **Größe / Erwartet / Best / Worst** | Tafel fehlt auf der Seite; wo es sie gibt, ist die Spaltenfolge anders und die Köpfe heißen „Best"/„Worst" bzw. „Best Case"/„Worst Case" statt „Günstig"/„Ungünstig" | Tafel bauen, Spaltenfolge und Benennung vereinheitlichen | C | **hoch** |
| 76 | Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf (Kat. 8 · Z. 3710-3760) | `WirtschaftlichkeitSeite.razor:35-93` — kein Umschalter | `WIRT_UMSCH_KENNZAHLEN`/`WIRT_UMSCH_VALERI` existieren nicht | U2 bauen | C (U2) | mittel |
| 77 | Vier Abschnittsköpfe „Lohnt es sich / Wie sicher / Woraus / Was ist angenommen" (Kat. 8 · Z. 3647) | keine Fundstelle | `WIRT_ABS_LOHNT/_SICHER/_WORAUS/_ANNAHMEN` existieren nicht | U2 bauen | C (U2) | mittel |
| 78 | Fußleiste **vier** Knöpfe, ohne „Verlauf…" (Kat. 8 · Z. 3702-3704) | `WirtschaftlichkeitSeite.razor:363-392`; Test `EPOS.UI.Tests/Seiten/WirtschaftlichkeitSeiteTests.cs:340` pinnt fünf | **Fünf** Knöpfe (Photovoltaik, BHKW, Strombezug, **Verlauf**, Berechnen), im Lauf sechs. Das Mockup zeichnet den Zustand nach U2/U3 — insoweit gewollt | mit U2/U3 zusammen | C (U2/U3) | mittel |
| 79 | Zweites Mockup: „die Fußleiste trägt **sieben** Knöpfe" (2. Mockup · Z. 1957) | ebd. | Stand veraltet: „Parameter…" steht seit #325 in der Szenariozeile (`:161`), „Tarifstruktur…" entfiel | Satz auf fünf korrigieren | M | gering |
| 80 | Empfehlungskarten je Version mit Einstufung (Kat. 8 · Z. 3818-3843) | `WirtschaftlichkeitSeite.razor:86-93`; `WirtschaftlichkeitSeiteGaben.cs:763-807` | Vier Kennzahlkacheln der **besten** Variante, keine Karte je Version. `WirtschaftlichkeitEmpfehlung.Einstufungen`/`StufeText` werden nur im Wortbericht benutzt (`BausteineWirtschaftlichkeit.cs:939, 976`) | U5 bauen | C (U5) | mittel |
| 81 | Spannenbild (Balken je Variante) (Kat. 8 · Z. 3935-3960) | keine Fundstelle in `ChartRenderer.cs` | fehlt ganz | U4 bauen | C (U4) | mittel |
| 82 | Verlaufsbedienung: Haken je Variante und je Szenario (Kat. 8 · Z. 4037-4041) | `KapitalwertVerlaufDialog.razor:44-52` (Ganzzahlfeld + Klappliste + „Aktualisieren") | fehlt | U3 bauen | C (U3) | mittel |
| 83 | Brückenbild „Von der Investition zur Kapitalwertdifferenz" (Kat. 8 · Z. 4190-4215) | keine Fundstelle | fehlt ganz; keine U-Nummer, keine Anhangzeile | Anhangzeile anlegen, dann bauen | K | mittel |
| 84 | Zahlungsstrombild je Jahr (Kat. 8 · Z. 4335-4470) | nur die Tabelle `BausteineWirtschaftlichkeit.cs:294-320`, `ExcelBerichtGenerator.cs:899` | Bild fehlt | Anhangzeile anlegen, dann bauen | K | mittel |
| 85 | Klappblock „Bewertung nach DIN EN 17463" mit sechs Absätzen (Kat. 8 · Z. 4637-4680) | `WirtschaftlichkeitSeite.razor:309-347` — der Block enthält nur das Freitextfeld „Nicht monetäre Wirkungen" und den Speichernknopf; Zeitraum- und Vereinfachungszeile stehen **außerhalb**, oben `:179-186` | Blockinhalt und -ort stimmen nicht; die Deklarationszeilen fehlen (`WIRT_DEKL_NOMINAL/_STEUERN/_RESTWERT/_RISIKO` existieren nicht) | Zeilen in den Block ziehen, Deklarationen mit U2 ergänzen | C (U2) | mittel |
| 86 | Fußzeile des Bewertungsblocks „Parameter… · **Anhang-E-Checkliste…** · **Bericht erzeugen**" (Kat. 8 · Z. 4684-4687) | `WirtschaftlichkeitSeite.razor:344` nur „Speichern"; Berichterzeugung auf eigener Seite `EPOS.UI/Seiten/Berichte/BerichtSeite.razor:133` („Erstellen") | Zwei der drei Knöpfe existieren nirgends im Repo — **ohne U-Nummer und ohne Anhangzeile**. Zugleich zeichnet das Mockup an zwei Stellen zwei verschiedene Fußleisten derselben Seite (vier Knöpfe bei Z. 3702, drei hier) | Konzeptentscheid: gehört die Berichterzeugung auf diese Seite? Anhangzeile anlegen; die beiden Fußleisten des Mockups zusammenführen | K | mittel |
| 87 | Hinweistext „Was ein Szenario variiert" unter der Annahmentafel (Kat. 8 · Z. 4602) | keine Fundstelle | `WIRT_SZEN_HINWEIS` fehlt | U10 bauen | C (U10) | gering |
| 88 | Knopf „Verlauf nach Excel…" (Kat. 8 · Z. 4042) | keine Fundstelle | `WIRT_BTN_VERLAUF_EXCEL` fehlt | U13 bauen | C (U13) | mittel |
| 89 | Tabellenbericht „je Szenario eine eigene Spaltengruppe" (Kat. 8 · Z. 4830) | `BausteineWirtschaftlichkeit.cs:240` und `ExcelBerichtGenerator.cs:663` holen den Verlauf nur für `ERWARTET` | fehlt | U13/U15 bauen | C | mittel |
| 90 | Sensitivitätstafel im Block 4 der ValERI-Bewertung (Kat. 8 · Z. 4790) | nur Bericht `BausteineWirtschaftlichkeit.cs:866`, `ExcelBerichtGenerator.cs:723` (nur Erwartet) | auf der Seite nicht vorhanden | mit U2 | K | gering |
| 91 | Parameterdialog: Klappliste „Vergleichsprojekt" mit Herleitungszeile „Referenz der Differenzrechnung …" (Kat. 8 · Z. 3670-3673) | `WirtschaftlichkeitParameterDialog.razor:64-97` — kein solches Feld; die Referenzwahl steht als Optionsspalte auf der **Seite** (`WirtschaftlichkeitSeite.razor:117-125`), Modellfeld `IdReferenzprojekt` in `WirtschaftlichkeitSeiteGaben.cs:326, 368` | Eingabestelle weicht ab; der Seitenort ist in Konzept § 2.9 begründet (U37) | Mockup nachziehen | M | mittel |
| 92 | Parameterdialog: zwei Gruppen „Rahmen" und „Preissteigerungen" (Kat. 8 · Z. 3667, 3676) | `WirtschaftlichkeitParameterDialog.razor:64` — eine Gruppe „Allgemein" (`WPAR_G_ALLGEMEIN`) | Gruppenschnitt weicht ab | Mockup nachziehen | M | gering |
| 93 | Parameterdialog zeigt nur sechs Eingaben und einen zugeklappten Szenarienblock (Kat. 8 · Z. 3667-3700) | `WirtschaftlichkeitParameterDialog.razor:64, 120, 293, 322, 332, 367` — **sechs Gruppen**, 20 Zahlenfelder, drei Klapplisten, ein Schalter | Die Zeichnung lässt die Szenarienmatrix (zwölf Felder), die Gruppen Strom/BHKW/Brennstoff mit sechs Eingaben und die Bilanzgruppe ganz weg; 42 der 44 `WPAR_*`-Schlüssel nennt sie nicht | Mockup ergänzen oder den Dialog ausdrücklich als Ausschnitt kennzeichnen | M | mittel |
| 94 | Parameterdialog-Fuß „Abbrechen / **Übernehmen**" plus Statuszeile „Drei Szenarien werden in einem Lauf gerechnet" (Kat. 8 · Z. 3700) | `WirtschaftlichkeitParameterDialog.razor:398` mit `_t.BtnSpeichern` = **„Speichern"** (`WirtschaftlichkeitParameterDaten.cs:22`); keine Statuszeile im Fuß | Beschriftung weicht ab (Hausform ist „Speichern"); Statuszeile fehlt | Mockup nachziehen bzw. Statuszeile ergänzen | M / C | gering |
| 95 | Knopf „BHKW…" der Fußleiste (Kat. 8 · Z. 3702) | `WirtschaftlichkeitSeite.razor:371`, Beschriftung `:624` = „BHKW-Wirtschaftlichkeit…" | Andere Beschriftung | Mockup nachziehen | M | gering |
| 96 | Zweites Verlaufsbild „an genau einem Ort — im Wortbericht" (U18-Zeile) | `BausteineWirtschaftlichkeit.cs:264-268` (Kommentar) gegen `KapitalwertVerlaufDialog.razor:59-60` (beide `<ChartBild>`) | **Kein U18-Verstoß:** Die U18-Zeile erlaubt ausdrücklich, dass der Dialog „bis zur Ergebnisansicht (§ 2.13, K8) unverändert beide Bilder" zeigt. Falsch ist allein der absolute Kommentar im Kern | Kommentar um den Dialogvorbehalt ergänzen | C | gering |
| 97 | Hinweiszeile „Ersatz und Restwert / ohne Nutzungsdauer" plattformfrei (2. Mockup · Z. 1941-1945) | `WirtschaftlichkeitSeiteGaben.cs:404` (`Zeitraumzeile()`) liegt in `WindowsFormsApplication1` | Die Zeile füllt nur die Windows-Schale; die Blazor-Seite hat kein plattformfreies Gegenstück — genau so in U39 beschrieben | U39 bauen | C (U39) | mittel |

---

## 5 · Aktionen ohne Funktion

Jeder `<button>` in den geprüften Razor-Dateien trägt ein `@onclick` — es gibt keinen toten Knopf
im Markup. Die Lücken liegen woanders:

**A · Im Mockup gezeichnet, im Code nicht vorhanden**

| Knopf | Ort im Mockup | Befund | U-Nummer |
|---|---|---|---|
| „Sätze und Herkunft…" | Kopf Gruppe 1b, BHKW (Kat. 5 · Z. 1992) | 0 Treffer repoweit; auch die Vorbedingung fehlt — `EPOS.UI/Bausteine/Gruppenkopf.razor:12-38` kennt keinen Parameter für eine Kopfaktion. Alle 14 `BHW_UEB_*`-Schlüssel fehlen | U22 (Vorschlag) |
| „Wahl und Herkunft…" | Kopf Gruppe 3, BHKW (Kat. 5 · Z. 2092) | wie oben | U22 (Vorschlag) |
| „Übernehmen" der Überlagerung | Kat. 5 · Z. 2402 | nicht gebaut | U22 (Vorschlag) |
| „Verlauf nach Excel…" | Verlaufsabschnitt (Kat. 8 · Z. 4042) | kein Element, `WIRT_BTN_VERLAUF_EXCEL` fehlt | U13 (geplant) |
| **„Anhang-E-Checkliste…"** | Fuß des Bewertungsblocks (Kat. 8 · Z. 4685) | kein Element, kein Schlüssel, **keine U-Nummer, keine Anhangzeile** | **fehlt** |
| **„Bericht erzeugen"** | ebd. (Kat. 8 · Z. 4686) | auf dieser Seite nicht vorhanden; nächstes Gegenstück ist „Erstellen" auf `BerichtSeite.razor:133` | **fehlt** |
| Umschalter „Kennzahlen / ValERI-Bewertung" | Kopf der Ergebnisseite | kein Element | U2 (geplant) |
| Haken je Variante und je Szenario im Verlauf | Kat. 8 · Z. 4037 | keine; stattdessen eine Szenario-Klappliste | U3 (geplant) |
| „Schließen" im Fuß der Schnellwahl-Überlagerung | Kat. 4 · Z. 1707 | existiert nicht; geschlossen wird über ✕ oder Esc | **fehlt** |

**B · Im Code vorhanden, aber nie gezeichnet**

| Knopf | Code-Stelle | Befund |
|---|---|---|
| „Gesetzesparameter…" im PV-Reiter Ertrag/Bonus | `ErtragBonus.razor:54-62` | Rückruf und Text sind gesetzt, der Knopf steht aber nur im BHKW-Zweig — im PV-Blatt wird er **nie** gerendert (Befund 29) |
| Bemessungs-Klappliste der Abschlusszeile | `VorlagenZeile.razor:72-75` | bedienbar, aber wirkungslos: `PositionNeu` setzt immer `BEMESSUNG_BETRAG` (Befund 10) |
| „Nutzungsdauern vorbelegen…" auf der Betriebsseite | `KostenKomponenteDialog.razor:302-310` | sichtbar, dauerhaft gesperrt (Befund 26) |

**C · Vorhanden, aber mit anderem Verhalten oder an anderem Ort**

| Knopf | Abweichung |
|---|---|
| „Vorschlag übernehmen" | Mockup: je Größe in der Überlagerung. Code: drei Knöpfe direkt am Feld (`BhkwWirtschaftlichkeitDialog.razor:167, 175, 183`), für den Jahresdeckel keiner |
| „Parameter…" | Mockup zeichnet ihn zweimal (unter der Bandbreite, im Bewertungsfuß). Code: einmal, in der Szenariozeile (`WirtschaftlichkeitSeite.razor:161`) |
| „Verlauf…" | Code hat ihn (`:383`), das Mockup verlangt seinen Wegfall (U2) |
| 🗑️ an PV-Pflichtzeilen | Code zeigt 🔒, derselbe Handler, andere Beschriftung |
| „BHKW…" | Code beschriftet „BHKW-Wirtschaftlichkeit…" |
| „Aus Vorlage übernehmen…" | Zusätzlich zum Mockup: wechselt nach der Übernahme die gezeigte Kategorie (`KostenKomponenteDialog.razor:1179-1192`) |

**Geprüft und in Ordnung** (Handler, Ziel, Schreibweg, Schließverhalten, Rückfrage decken sich mit
dem Mockup): „Aus Vorlage übernehmen…", „Positionskatalog…", „Nutzungsdauern vorbelegen…",
„übernehmen" am Vorlagenhinweis (U40), Stift, Papierkorb, Schloss, ±, ＋, „Saisonale Sätze…",
„Schnellwahl aus Katalog…", „In Arbeitspreis übernehmen", „Katalogwerte übernehmen",
„Rest übernehmen", „Emissionsarten & Katalog verwalten…", „Regel hinzufügen", „💾 Speichern" der
Preishistorie, „Löschen" je Preisstand mit Vorgabe „Nein", „Strombezug…", „BHKW-Tarif…"
(schreiben nur bei Wertänderung und schließen), „eigene Werte" (U38), „Marktwerte importieren…",
„Einspeise-Tarif…", „Photovoltaik…", „Berechnen", Tauschknopf ⇄ (dreht das Vorzeichen, gesperrt
außerhalb Sicht 2).

---

## 6 · Ressourcen

### 6.1 Zählung

| Größe | Zahl |
|---|---|
| Zellen `<td class="rs">` | 109 |
| Schlüsselnennungen insgesamt | 458 |
| verschiedene Schlüssel | **418** |
| davon als vorhanden geführt | 369 |
| davon als „geplant · Un" markiert (`rs-geplant`) | 49 |
| Zeilen „— (ohne Ressource: …)" | 2 |
| Schlüssel in `Resource.resx` / `Resource.en-US.resx` | je 6 532 |

**Abgleich:**

- **Fehlend: 0.** Alle 369 als vorhanden geführten Schlüssel stehen in **beiden** Sprachdateien.
- **Vorzeitig vorhanden: 0.** Keiner der 49 „geplant"-Schlüssel existiert in einer `.resx`.
- Die Statuszeile #362 nennt „76 von 413 Schlüsseln fehlten, 25 umgestellt, 2 ohne Ressource,
  49 geplant". Die 49 und die 2 stimmen heute exakt; die Grundgesamtheit ist seither von 413 auf
  418 gewachsen (#363: fünf `KUEB_*` neu, `KUEB_LBL_QUELLVORLAGE` entfernt; #366: drei
  `KDLG_VORLAGE_*` neu). **Die Nachmessung bestätigt den gemeldeten Stand.**

### 6.2 Überzählig — im Mockup genannt, in der `.resx` vorhanden, im Code nie gelesen

| Schlüssel | Wert (de) | Befund |
|---|---|---|
| `KDLG_TAB_KOSTEN` | „Kosten Invest/Betrieb" | **Karteileiche.** `KostenKomponenteDialog.razor:546` trägt den deutschen Text als `[Parameter]`-Vorgabewert; niemand setzt `TitelReiterKosten` — weder `KostenKomponenteHuelle.cs` noch eine andere Stelle. In en-US bleibt der Reiter deutsch |
| `KDLG_TAB_ERTRAG` | „Ertrag/Bonus" | dasselbe für `TitelReiterErtrag` (`:549`). Auch `ReiterBezeichnung` (`:552`, „Kostenverwaltung") ist ein unlokalisierter Vorgabewert ohne Schlüssel |
| `WIRT_ENK_KOPF` | „Energiekosten je Anlage [€/a]" | Die Schlüsseltafel führt ihn als „Zeilenkatalog (vorhanden)". Tatsächlich entstehen die Zeilen als Einzugszeilen mit `WIRT_ENK_ZEILE` (`WirtschaftlichkeitZeilen.cs:291`) — **eine Gruppenüberschrift gibt es nicht**. Auch `WIRT_ENK_ANLAGE` ist unbenutzt |

**Empfehlung:** In `KostenKomponenteHuelle.GabenIntern` ergänzen:
`["TitelReiterKosten"] = T("KDLG_TAB_KOSTEN", "Kosten Invest/Betrieb")` und
`["TitelReiterErtrag"] = T("KDLG_TAB_ERTRAG", "Ertrag/Bonus")`; für `WIRT_ENK_KOPF` entscheiden,
ob die Überschrift gebaut oder der Schlüssel samt Tafelzeile gestrichen wird.

### 6.3 Geplant-Marker ohne U-Nummer

`WIRT_KWKG_KONTINGENT_LEER` (HTML Z. 2713) ist als `rs-geplant` markiert, trägt aber **keine
U-Nummer** — entgegen der eigenen Legende des Mockups („markiert *geplant* einen Schlüssel,
dessen beschriftete Größe zu einem Punkt dieser Anhangtafel gehört"). Die Sache selbst ist
außerdem gebaut: `KwkgKontingentRechner.cs:85, 123, 131` liefert für leere Kontingente
`WIRT_KWKG_KONTINGENT_OHNE_ART`, `_ANTEIL_FEHLT` und `_ZU_KLEIN`.
**Empfehlung:** Tafelzeile auf den vorhandenen Schlüssel ziehen (Mockup nachziehen), nicht bauen.

### 6.4 Im Mockup nicht genannt, im Dialog aber geführt

Für die dialogeigenen Präfixe (die anderen Präfixe reichen über die geprüften Masken hinaus und
sind deshalb nicht aussagekräftig):

| Präfix | in `.resx` | im Mockup | nicht genannt |
|---|---|---|---|
| `WPAR_` (Parameterdialog) | 44 | 2 | **42** — u. a. alle vierzehn `WPAR_SZ_*` der Szenarienmatrix, die vier Gruppenköpfe `WPAR_G_*`, `WPAR_NICHT_MONETAER*`, `WPAR_REFKESSEL*`, `WPAR_TITEL`, `WPAR_ZINS` |
| `BHW_` (BHKW-Dialog) | 101 | 87 | **35** — im Wesentlichen die Listeneinträge `BHW_W_*` der acht Klapplisten, dazu `BHW_TITEL`, `BHW_KNOPF`, die fünf Vorschauzeilen `BHW_V_*`, `BHW_PARAM_*`, `BHW_MELD_GESPEICHERT` |
| `PVW_` (PV-Vergütung) | 69 | 62 | **7** — `PVW_BTN_EIGENE_WERTE`, `PVW_HERKUNFT_*` (3), `PVW_IMPORT_FILTER`, `PVW_KNOPF`, `PVW_MELD_GESPEICHERT` |
| `KKOMP_` | 7 | 2 | 5 — die vier Werkzeugtipps und `KKOMP_BTN_JA`/`_NEIN` |
| `KCASE_` (Worst/Best) | 8 | 3 | 5 — `KCASE_BEST_A`, `KCASE_WORST_A`, `KCASE_G_KOSTEN`, `KCASE_G_NUTZUNGSDAUER`, `KCASE_ERLOES_HINWEIS` |
| `KUEB_` | 10 | 8 | 2 — `KUEB_LBL_QUELLANLAGE`, `KUEB_MSG_UEBERNOMMEN` |
| `VPOS_` | 10 | 9 | 1 — `VPOS_MSG_NAME_FEHLT` |

Die `WPAR_`-Lücke ist die einzige, die eine ganze Maske betrifft (Befund 93). Die `BHW_W_*`-Lücke
ist folgerichtig: Das Mockup zeichnet die Klapplisten als Anzeigezeilen (U22) und nennt deshalb
ihre Listeneinträge nicht.

---

## 7 · Offene Fragen an den Anwender, mit Empfehlung

1. **Erlösrubrik nach Komponente (U6, Befunde 54, 62, 65, 67).** Die innere Gliederung mit
   Teilsummen je Anlage und einem Block „projektweit" ist die Kernaussage der Kategorie 7 und hängt
   an den Eigenverbrauchsmengen je Anlage aus der Strommatrix.
   **Empfehlung: bauen.** Ohne sie bleibt die Rubrik eine flache Liste, und die Zahlenprobe des
   Mockups (293.245,6 + 22.914,0 = 316.159,6 €/a) lässt sich im Programm nicht nachvollziehen.
   Vorher zu klären: Wird der Leistungsanteil wirklich projektweit geführt, wie das Mockup zeichnet?

2. **Gedankenstrich oder Null (Befund 63).** Heute steht in einer Zelle ohne Grundlage
   `0 — kein Kesselbrennstoff`, im Mockup `— kein Kesselbrennstoff`.
   **Empfehlung: dem Mockup folgen.** „0" behauptet eine gerechnete Null, wo nichts gerechnet
   wurde; die Excel-Zelle bleibt über `ExcelWert` in beiden Fällen numerisch. Der Eingriff ist
   klein (Wert nullbar führen), berührt aber jede Ergebnistafel — also mit Referenzlauf abnehmen.

3. **Endenergietafel: vier oder sechs Spalten (Befund 18).** Die Zeichnung zeigt sechs, die
   Schlüsseltafel desselben Abschnitts vier, der Code vier.
   **Empfehlung: bei vier bleiben und die Zeichnung kürzen** — Energieträger und Arbeitspreis
   stehen dem Anwender in der Energieträgerverwaltung zur Verfügung, und die Gruppe ist eine
   Kontrollanzeige, keine zweite Preisliste. Wenn sie doch hinein sollen: die Daten liegen im
   `EndenergieAufloeser` vor, der Aufwand ist gering.

4. **„Bericht erzeugen" und „Anhang-E-Checkliste…" auf der Wirtschaftlichkeitsseite (Befund 86).**
   Beide Knöpfe existieren nirgends und haben keine Anhangzeile; die Berichterzeugung liegt heute
   auf einer eigenen Seite.
   **Empfehlung: Konzeptentscheid und eine neue Anhangzeile.** Ein zweiter Einstieg in denselben
   Bericht ist vertretbar; die Anhang-E-Checkliste ist dagegen ein eigenes Stück Arbeit und sollte
   nicht stillschweigend in einer Fußleiste mitlaufen.

5. **Reihenfolge der Kennzahltafel (Befund 71).** Das Mockup begründet ausdrücklich, warum der
   absolute Nettobarwert **unter** der Differenz steht; der Code stellt ihn darüber.
   **Empfehlung: Code nachziehen** — reine Anzeige, der Referenzlauf bleibt byte-gleich.

6. **Parameterdialog im Mockup (Befund 93).** Gezeichnet sind sechs von 26 Eingaben.
   **Empfehlung: die Zeichnung ergänzen** (mindestens die Szenarienmatrix und die Gruppe
   „Bewertung"), oder den Ausschnitt im Bild als solchen kennzeichnen. Sonst liest sich der
   Abschnitt als vollständige Maskenbeschreibung, die er nicht ist.

7. **U22 „Sätze und Herkunft" (Befunde 44–46, 51).** Das Mockup zeichnet durchgehend den
   Zielzustand, die Prosa zählt dabei falsch (sechs statt acht Wahlfelder, elf statt neun
   Zahlenfelder).
   **Empfehlung: die Zählungen sofort korrigieren**, unabhängig davon, wann U22 gebaut wird —
   sie sind zugleich die Abnahmekriterien des Punkts.

8. **Mockup-Pflege ohne Entscheidbedarf.** U40 braucht eine durchgestrichene Anhangzeile
   (Befund 28), `WIRT_KWKG_KONTINGENT_LEER` einen vorhandenen Schlüssel statt des Markers
   (Abschnitt 6.3), der U32-Text die nächste freie Schemanummer 95 (Befund 43), der
   Nachweisumschlag die Fassung 3 (Befund 37), und die beiden Fußleisten der Ergebnisseite
   müssen zusammengeführt werden (Befund 86).

---

## 8 · Marker gegen Codestand

Die Anhangstafel führt 39 Zeilen (U1–U39), davon 20 durchgestrichen. **U40 hat keine Zeile** —
er steht nur als Inline-Marker in Kategorie 2 (Befund 28).

**Als erledigt durchgestrichen — 19 von 20 vollständig belegt:**

U8, U16, U17, U18, U19, U20, U21, U23, U24, U26, U28, U29, U30, U31, U33, U34, U35, U36, U37, U38.

- **U18** ist der Sonderfall: Das Feld `Gestrichelt` und das Bild im Wortbericht sind gebaut
  (`ChartRenderer.cs:106, 574-586, 684-703`; `BausteineWirtschaftlichkeit.cs:269-272`). Der
  Verlaufsdialog zeigt weiterhin beide Bilder — das ist **kein Verstoß**, die U18-Zeile lässt es
  bis zur Ergebnisansicht (§ 2.13, K8) ausdrücklich zu. Falsch ist nur der absolute Kommentar
  „an GENAU EINEM Ort" im Kern (Befund 96).
- **U21** hält, weicht aber im Mockup-Text ab (Fassung 3 statt 1, sieben statt vier Skalare,
  Befund 37).
- **U28, U30, U31** halten in der Sache, nicht in jedem gezeichneten Wortlaut (Befunde 5, 8, 9,
  18, 19, 21).

**Als geplant/Vorschlag geführt — alle 19 zu Recht offen:**

U1, U2, U3, U4, U5, U6, U7, U9, U10, U11, U12, U13, U14, U15, U22, U25, U27, U32, U39.
Für jeden ist nachgewiesen, dass kein zugehöriger Ressourcenschlüssel außerhalb der Mockup-Datei
existiert und keine Code-Stelle die Funktion trägt. Stichproben:
U1 (repoweit 0 Treffer „Abwärmeabfuhr"), U11 („Vorschlagswerte" nur als Modellkommentar in
`StrompreisZerlegungModel.cs:25, 98`), U22 (0 Treffer „Sätze und Herkunft"; die acht Klapplisten
stehen unverändert), U25 (`BHW_A_DECKEL_STAFFEL`, `BHW_W_DECKELANTEIL` fehlen, obwohl die
Katalogwerte 3.300/3.100/2.900/2.700/2.500 in `GesetzKatalog.cs:1109-1116` bereitliegen),
U27 (alle acht `PVV_V_*`/`PVV_HERL_MARKTWERT` fehlen), U32 (`EnergietraegerPreisCtrl.cs:114-140`
legt weiterhin `ID_Umrechnung = -1` ab), U39 (`Zeitraumzeile()` liegt nur in der Windows-Schale).

Zu U14 und U15 ließ sich im Repository keine eigene Definition finden; das Urteil „nicht gebaut"
stützt sich allein auf den Anhangtext und darauf, dass Verlauf, Mehrjahrestabelle, Sensitivität und
Emissionsbilanz durchgehend nur `ERWARTET` rechnen.

Ebenfalls nicht in der Tafel, aber im Mockup gezeichnet und nirgends gebaut: das Brückenbild
(Befund 83), das Zahlungsstrombild (Befund 84), „Anhang-E-Checkliste…" und „Bericht erzeugen"
(Befund 86) sowie der Fuß der Schnellwahl-Überlagerung (Befund 38). **Für diese fünf Stücke
fehlt die Anhangzeile** — die Tafel ist damit nicht, wie sie von sich behauptet, „die einzige
Stelle für Nichtgebautes".

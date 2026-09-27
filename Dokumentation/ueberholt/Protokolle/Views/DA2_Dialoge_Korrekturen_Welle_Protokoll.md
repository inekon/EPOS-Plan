# DA-2 — Dialoge und Korrekturen der Welle #567–#582 (Protokoll, 26.09.2026)

Statuszeilen #567, #571, #572, #573, #574, #575, #576, #577, #582 in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md). Zweig
`ios_migration_september` im Hauptbaum; Merge `8eed15436`. Kein Schemaschritt in dieser Welle;
Testdatenbank unberührt außer durch die Referenzbasis R23 (siehe
[`SK2_Kessel_Bereitschaft_Stunden_R23_Protokoll.md`](../Simulation/SK2_Kessel_Bereitschaft_Stunden_R23_Protokoll.md),
eigenes Protokoll, nicht Teil dieser Welle).

## 1. Photovoltaik: Wechselrichter-Klappliste, OND-Import, Leerhinweis (#567)

Commits `8d3a65d5b`, `3a75514d9`, `ec631a61e`. Die Klappliste „Wechselrichter aus dem Katalog“
folgt jetzt derselben Bewertung und Reihenfolge wie „Wechselrichter vorschlagen“
(`WechselrichterVorschlag`, mit den Auslegungstemperaturen des Projekts): geeignete Geräte
zuerst mit DC/AC-Verhältnis und Gerätezahl, danach bedingte/ungeeignete mit Kurzgrund. Der
`.OND`-Import füllt Stränge je Tracker aus der Datei und zeigt in der Importzeile zusätzlich
`I_Sc_Max`. Fehlt im Wechselrichterkatalog Anzahl MPPT, Stränge je MPPT oder max.
Kurzschlussstrom, nennt ein Leerhinweis, womit dann gerechnet wird; ein solches Gerät stuft der
Vorschlag höchstens „bedingt“ mit Grund „Katalogwerte unvollständig“. Tests:
`WechselrichterVorschlagTests`, `StrangAuslegungTests`, `OndImportTests`, `PvStraengeFelderTests`,
`WechselrichterDialogTests`. Konzept `Doku_PV_Strangauslegung_EPOS-Plan.md`, Wiki Photovoltaik und
Gerätekataloge nachgezogen.

## 2. Wochenend-/Feriensollwert: eine Regel, Herleitungssatz (#571)

Commits `1c4ac3cb3`, `c54872cdb`, `dddcf90e1`. 0 heißt überall keine Absenkung: Wochenendsollwert
wirksam über 5 °C, Feriensollwert wirksam ab 1 °C (`Gebaeudefestwerte.cs`), der Wochenendwert
gilt absolut und ganztägig, Rangfolge Ferien > Wochenende > Tag/Nacht
(`SollwertAbsenkungTests`). Katalogeditor und Stammblatt zeigen eine Herleitungszeile
(`Sollwertzeile`) unter den Raumtemperaturen mit Platzhalter „keine“; Felder umbenannt in „Soll am
Wochenende (ganztägig)“/„Soll in Ferien (ganztägig)“. Der Hilfe-Assistent kappte lange Wiki-Seiten
am Anfang (6 000 Zeichen); `WikiWissen.Kappen(text, frage)` nimmt jetzt Seitenanfang plus die zur
Frage passenden Abschnitte (`WikiWissenAuszugTests`). Wiki „Gebäude“ neuer Abschnitt mit Ankern
`temperaturen-und-ferien`, `wochenendabsenkung`, `ferienabsenkung`, `gebaeude-und-huelle`;
Berechnung/Wärmebedarf um die Regel ergänzt; `help_mapping.txt` berichtigt. Referenzlauf R22 PASS.

## 3. Überlagerung steht im Fenster, Fuß haftet (#572)

Commit `d5ce3e0cf`; Konzeptpapier N35 in `fed6c6136`. Ursache: `transform:
translate(-50%, -50%)` an `.epos-ueberlagerung` machte das Element zum umschließenden Block
jedes `position: fixed`-Nachfahren — eine innere Überlagerung stand im Kasten der äußeren,
breiter als sie, von deren `overflow` beschnitten. Behebung: Zentrierung über `inset: 0`,
`margin: auto`, `height: fit-content`; der Wirt rollt nicht, solange eine Überlagerung steht;
Fußleiste und Diagramm-Zeigerzeile eines eingebetteten Dialogs haften am unteren Rand; nur die
innerste Überlagerung zeigt ihre Hilfepillen. Gemessen in Chromium: Querüberlauf vorher 165/43 px,
nachher 0; gezeichnete Hilfepillen vorher 5, nachher 2. Tests: `StilblattTests.U572_*`,
`UeberlagerungstitelTests`. Der Strukturfehler bleibt (Zapfprofil-Weg als Überlagerung in
Überlagerung in Überlagerung); Konzeptpapier N35 bewertet drei Alternativen (Blattwechsel,
Seitenwechsel, Aufklapper) mit Empfehlung Blattwechsel — Anwenderentscheid offen.

## 4. Gebäudebedarf: Warmwasser des Projekts als Auskunftszeile (#573)

Commit `aa23a0161`. Der Wärmebedarfsdialog eines Gebäudes zeigt nach VDI 6007 nur die Raumwärme;
neu steht unter den Kennzahlen das Warmwasser des Projekts mit dem Hinweis, dass es im Kanal
Warmwasser läuft und nicht Teil der Gebäudesimulation ist (`GebaeudeBedarfCtrl.WarmwasserDesProjektsMwh`,
dieselbe Weiche wie der Lauf). Rechenweg unverändert. Tests gegen den Lauf (1007 Bestandsweg, 1045
Generator, 1030 ohne Profil) und bunit.

## 5. Wärmebedarf-Ergebnis und Bedarfsprofil (#575)

Commits `8b9271d07`, `502a5abf0`. Der Grafikreiter des Bedarfs-Ergebnisdialogs zeigte bei der
Wärme nur Monatssäulen; `SimulationWaermebedarf` führt jetzt `Waermebedarf_Prozess_Stunde` und
`Waermebedarf_Heizkanal_Stunde` als Stundenreihen (Rechenweg unverändert, Referenzlauf R22 PASS),
der Dialog zeigt Prozesswärme und Gebäude als Jahresverlauf mit Woche/Tag, Brauchwasser Woche/Tag.
Zweiter Befund: eine vorgegebene Beckenwasseraufheizung von 150 000 kWh/a zeigte die Simulation
als 365 000 kWh, weil `ProfilBedarf.Rechnen` den Jahresverbrauch nur aus der gespeicherten
Zuordnung las, nicht aus der Dialogzeile vor dem Speichern; `ProfilQuelle.Jahressummen` geht jetzt
vor. „Monatlicher Verlauf…“ (öffnete denselben Dialog ein zweites Mal) entfällt, ein Knopf
„Simulation“ rechnet und zeigt. Tests: `ProzesswaermeVorschauGanglinieTests`,
`BedarfErgebnisDialogTests`, `BedarfsProfileDialogTests`; Wiki Berechnung/Prozesswärme,
Berechnung/Strombedarf, Brauchwasser-Zapfprofil nachgezogen.

## 6. Wärmelast-Ganglinie: Bedarfsarten gestapelt (#576)

Commit `bf0b0875c`. Heizung, Brauchwasser und Prozesswärme liegen im Bild „Wärmelast
Jahresganglinie“ als gestapelte Flächen übereinander, die Summe Wärmebedarf als Linie auf der
Oberkante; 100 % ist der Jahreshöchstwert der Summe, auch bei abgewählten Reihen
(`ChartRenderer.GanglinieNormiertModell`, Reihen mit `Stapelart.Flaeche` kumuliert gestapelt,
Dauerlinie ungestapelt, ohne Flächenreihe byte-gleich). ChartProben-Probe
`ganglinie_normiert_gestapelt`; Wiki Simulationsergebnisse (Anker `waermelast`).

## 7. Gebäudedialog: Wärmebedarf aus dem Arbeitsstand vor dem Speichern (#577)

Commits `b1da52502`, `b6c222a6b`. Anwendermeldung: Ein eben übernommenes, noch nicht
gespeichertes Gebäude rechnete im Wärmebedarfsdialog nicht. `GebaeudeBedarfCtrl.Arbeitsstandgebaeude`
bildet das Projektgebäude aus dem Arbeitsstand (Projektkopie oder Katalogsatz nach der Suchregel
von `CopyFromStamm`, Leser `ProjektGebaeudeCtrl.AusZeile`); die neue Überladung
`Rechnen(int, int, ProjektGebaeudeModel, string)` rechnet darüber, geschrieben wird nichts; eine
Importzeile mit ausstehender Zone wartet benannt auf das OK. Tests:
`GebaeudeBedarfArbeitsstandTests`, `GebaeudeDialogTests`; Wiki „Gebäude“ (Anker `waermebedarf`)
nachgezogen.

## 8. Wirtschaftlichkeit: „Zum Bericht ›“ statt eigenem Berichtsweg (#582)

Commit `1c95a71f2`. Anwenderentscheid 26.09.2026: Der Knopf „Bericht erzeugen“ heißt „Zum Bericht
›“ und wechselt im Rahmen `BerichteKostenSeite` in den Bereich „Bericht“; eine
`BerichtVorbelegung` reicht Baustein Wirtschaftlichkeit, angehakte Versionen und Szenario
einmalig durch, die Berichtsseite nennt sie in einer leisen Zeile und merkt sich die Auswahl erst
danach; erzeugt wird allein dort mit „Erstellen“. Ohne gespeicherte Ergebnisse ist der Knopf
weich gesperrt („Erst berechnen“). Entfallen: eigene Rückfrage und Erzeugungsweg der
Wirtschaftlichkeitsseite; neue Ressourcen `WIRT_BTN_ZUM_BERICHT`, `WIRT_ZUM_BERICHT_KURZ`,
`WIRT_ZUM_BERICHT_GESPERRT`, `BK_BER_VORBELEGT`. Wiki Wirtschaftlichkeit (Anker
`bericht-erzeugen`, `szenariofuss`) und Berichtsvorlagen (Anker `erstellen`) nachgezogen. Das
Szenario der Wirtschaftlichkeit steuert den Bericht nicht — offen, ob der Bericht dem Szenario
folgen soll.

## Offene Punkte

Siehe „Nach #567“ (keiner), „Nach #572“, „Nach #574“ und „Nach #582“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md): Anwenderentscheid zur
Zapfprofil-Struktur (N35, Vorschlag Blattwechsel), Anwenderentscheid BN-Q1–Q3 zum Konzept
Berichte & Kosten (Vorschlag Variante A), die Frage, ob der Bericht dem Szenario der
Wirtschaftlichkeit folgen soll, und der Wiki-Upload der Seiten Gebäude, Berechnung/Wärmebedarf
und der übrigen Kandidaten dieser Welle mit dem nächsten Sammel-Upload. Gate der Welle auf
`8eed15436`: ausstehend (wird nachgetragen); kein iOS-Lauf (iOS-Hülle in keiner dieser Änderungen
berührt).

## 9. Nachtrag 27.09.2026: #584–#586 (Restwelle, nicht Teil der Welle #567–#582)

Drei weitere Statuszeilen mit demselben Restwelle-Gate #584–#587 auf `3299fbada` (siehe PT-2 für
#587): thematisch unabhängig, hier nur kurz nachgetragen.

- **#584 — Werkzeug ResourceDesigner** (Commit `4233f7a53`): `designer_neu.py` verglich
  `Resource.Designer.cs` im Textmodus, darum zeigte der Trockenlauf „unveraendert“, während
  „schreiben“ CRLF auf LF kippte. Jetzt byte-genauer Vergleich gegen die Datei auf der Platte,
  Schreiben in Binärbytes (BOM erzwungen, LF→CRLF); neue Probe
  `Werkzeuge/ResourceDesigner/probe_zeilenenden.py`. Kein Logbuch-Satz (Werkzeug).
- **#585 — Wochenende/Ferien-Flag, Paketanhebung-Ressourcen Stufe 1** (Commits `f31eefe02`,
  `c805a5843`): `GebaeudeArbeitsstand.Ableiten` folgt jetzt derselben Schwelle wie der Kern
  (Wochenendsollwert wirksam über 5 °C, Feriensollwert wirksam ab 1 °C,
  `Gebaeudemodellvorgaben.WochenendsollwertWirksam`/`FeriensollwertWirksam`); die Detailzeilen der
  Paketanhebung-Schritte 98–148 (#580) gehen jetzt über Registerschlüssel
  `TRANSFER_ANHEBUNG_S<Nr>` durch beide Ressourcendateien statt als feste deutsche
  Zeichenketten. Offen: dieselbe Umstellung fehlt noch für die Stufe-2-Schritte 62–92 (#587).
- **#586 — Heizkessel-CSV-Export** (Commit `bd5344c0c`, Nachzug zu #568): `CsvHeizkessel` führte
  weiter die alten Spalten `Kesselleistung_stuendlich`/`Restwaerme`; der Export nimmt jetzt
  dieselbe Quelle wie das Kesselbild (`SimulationErgebnisCtrl.KesselbildReihen`) mit den
  Legendentexten als Spaltenköpfe. Test `KesselCsvExportTests` gegen einen unabhängig
  gerechneten Lauf (1030, 1045). Offen: zwei alte Ressourcenschlüssel
  `CHART_CSV_HEIZKESSEL`/`CHART_CSV_RESTWAERME` bleiben ungenutzt in den `.resx`.

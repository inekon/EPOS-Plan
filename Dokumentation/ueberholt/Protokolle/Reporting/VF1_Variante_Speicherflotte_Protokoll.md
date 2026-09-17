# VF-1 — Variante und Speicherflotte: Anlagen-Ids mitversetzen, Vergleichsspalten benennen

Stand: 17.09.2026 · Zweig `ios_migration_september` (Arbeitszweig `w-vf1`) · Referenzbasis
`2026-09-16_R8_Heizkessel_Kaskade` · Auftrag **#320**.

Anlass ist der Anwenderbefund vom 17.09.2026: „Der Speicher wird bei ‚Simulation durchführen'
berücksichtigt, nicht aber in der Wirtschaftlichkeit im Vergleich mit einer Variante." Gemessen
wurde er mit **MV-1** (Abschnitt 1), behoben mit **VF-1** (Abschnitte 2 bis 4).

Ein eigenes Protokoll zur Speicherflotte oder zur Wirtschaftlichkeit des Speichers gab es unter
`ueberholt/Protokolle/` nicht; dieses ist der Anfang. Verwandt sind
`Simulation/FS1_N1_AnkerNachziehen_Protokoll.md` (derselbe Mechanismus eine Schicht höher: ein
Nachzug nach der Projektkopie) und `Reporting/W4_E5_Tarife_Strombezug_Protokoll.md`.


## 1. Messbericht MV-1 (17.09.2026) — Kurzfassung

Geprüft mit `EPOS.Kern.Tests/VarianteSpeicherflotteTests` gegen die Testdatenbank, Stammprojekt
1046 (das Prüfprojekt der Mehrspeicherrechnung, es führt `@Projektflotte`).

| Was der Duplizierlauf tut | Befund |
|---|---|
| `Tab_SpeicherAuslegung` samt Zeile `@Projektflotte` (`Daten` = `gz1:` + Base64 + gzip) | **wandert mit**, Zeile in der Kopie aktiv |
| `FlotteImProjektAktiv`, Betriebsziel, Verteilung, Peak-Ziel | **wandern mit**, wertgleich |
| Speicheranlagen (`Tab_Energieanlagen`), `Tab_StromspeicherVariante` | **kopiert**, Ids versetzt |
| Kostenzeilen `Tab_ProjektWerte` (Spalte `ProjektID`, `ID_Anlage`) | **kopiert**, Ids versetzt |
| Bezugsspitze des Laufs (Stamm gegen frische Kopie) | **gleich** — die Flotte wirkt in der Kopie |
| `FlottenEinheit.AnlageId` **innerhalb** des Flotten-JSON | **nicht versetzt** — zeigte auf Anlagen des Quellprojekts |

Die Folge des letzten Punktes ist nicht rechnerisch: Die Einheiten tragen Physik und Kosten
selbst, und der Flottenlauf liest `AnlageId` nicht. Sichtbar wurde der fremde Zeiger in der
Übernahme: `SpeicherFlottenStudieCtrl.Uebernahme.EinheitenInProjektUebernehmen` fand die fremde
Anlage im eigenen Projekt nicht (`Geraetezeile` = 0) und legte eine **zweite** Anlage an, statt
die vorhandene zu ändern.

Der zweite Teil des Befundes betraf die Anzeige. In der Vergleichsgruppe der Wirtschaftlichkeit
stand stumm eine Spalte „mit Flotte" neben einer „ohne"; der Simulationsreiter sagte es
(`SIM_SP_HERKUNFT`), die Wirtschaftlichkeit nicht. Dazu kam, dass
`WirtschaftlichkeitSeiteGaben` den frischen Lauf allein am **Stamm** entschied
(`mitZeitreihen = tarif.Aktiv || KwkgBonus || StromLeistungspreisGepflegt(_idStamm)`) und ein
vorhandenes, gegebenenfalls veraltetes Ergebnis ohne Hinweis nahm.


## 2. Teil B — die Kennungen im JSON mitversetzen

**Welche JSON-Stände Ids tragen.** Gesucht wurde nach `AnlageId`, `ID_Anlage` und `ProjektId` in
allen Modellen, die als JSON in der Datenbank landen. Gefunden wurden genau zwei Bezüge, beide in
`Tab_SpeicherAuslegung.Daten`:

1. `SpeicherEngine.FlottenEinheit.AnlageId` — die vertretene Speicheranlage, als Text;
2. `SpeicherAuslegungKonfiguration.Strompreisprofil` (`KostenprofilModel`) — die **eingefrorene
   Kopie** eines Kostenprofils mit `ID` und `ID_Projekt`. Die Werte des Profils stehen im Stand
   selbst; nur sein Anker zeigte auf das Quellprojekt.

Nicht betroffen: `Berichtskonfiguration.KonfigJson` trägt zwar mit `VariantenIds` Projekt-Ids,
steht aber in `ProjektDuplizierenCtrl.AUSNAHME_TABELLEN` und wird nicht kopiert. Die übrigen
JSON-Serialisierungen des Bestands (`StromspeicherAuslegungCtrl`, `SpeicherFlottenStudieCtrl`)
bilden Hashschlüssel und werden nicht gespeichert.

**Die Naht.** `ProjektDuplizierenCtrl.Duplizieren` reicht nach dem Commit — neben dem schon
vorhandenen `KostenProjektPositionenCtrl.AnkerNachziehen` — einen **benannten**
`ProjektkopieVersatz` (Zielprojekt, Versatz `Tab_Energieanlagen`, Versatz `Tab_Kostenprofil`) an
`SpeicherAuslegungCtrl.KopieBezuegeNachziehen`. Das Offset-Wörterbuch des Duplizierers bleibt bei
ihm; der zuständige Controller zieht seine eigenen Bezüge nach. Fehlt einer Tabelle ein Versatz,
gilt 0 — dann hatte die Quelle dort keine Zeile, die Kopie hat sie auch nicht, und der Bezug ist
zu Recht unauflösbar.

**Unauflösbar heißt `null`.** Ergibt der Versatz keine Zeile des Zielprojekts, wird der Bezug
gelöscht und protokolliert (`Console.WriteLine`, Muster `KostenPositionCtrl.Protokoll`). Eine
Einheit ohne Anlagenbezug ist ein gültiger Zustand — eine nur im Studienstand geführte Einheit;
ein Bezug auf ein fremdes Projekt ist keiner.

**Kein Schemaschritt**, keine neue Referenzbasis: Der Nachzug schreibt nur die `Daten`-Spalte der
frischen Kopie.


## 3. Teil A — der Speicherkontext je Spalte

`SpeicherAnzeigeCtrl.SpeicherKontextText(idProjekt)` beantwortet die Frage je Projekt in drei
Fällen: aktivierte Projektflotte → `mit Speicherflotte: <Ziel> · <n> Einheiten [· Peak-Ziel <x> kW
(<Modus>)]`; sonst die aktive Speichervariante → `Einzelspeicher: <Berechnungsart>`; sonst
`ohne Stromspeicher`. Die Angaben einer Flotte baut `FlottenKontextText(flotte, kopfformat)`.

**Zusammengezogen statt verdoppelt.** Der Simulationsreiter hatte dieselbe Herleitung in
`StromspeicherReiter.razor` stehen; er ruft jetzt `FlottenKontextText` mit seinem eigenen
Kopfsatz (`SIM_SP_HERKUNFT`, „Gerechnet mit Speicherflotte: …"). Verschieden ist nur der
Kopfsatz — der Reiter spricht über den angezeigten LAUF, die Wirtschaftlichkeit über eine SPALTE.

**Zwei Anzeigeorte, eine Quelle.** Der Text steht als Spalte *Stromspeicher* der Vergleichsgruppe
(`VarianteZeile.Speicher`, gefüllt in `WirtschaftlichkeitSeiteGaben.Laden`) und als erste Zeile
der Kennzahlentabelle (`WirtschaftlichkeitZeilen.Kennzahlen`, Schlüssel `SPEICHER_KONTEXT`).
Über die Zeilendefinition bekommen ihn Seite, Word-Baustein und Excel-Blatt aus derselben Stelle.
Die Zeile entfällt, wo kein Kontext lesbar ist — so bleiben die Zeilenproben ohne Datenbank grün.

**Stamm und Varianten dürfen verschiedene Flotten führen** (Anwendereinwand 17.09.2026). Nichts
im Programm erzwingt Gleichheit; der Kontexttext ist genau die Antwort darauf. Die Prüffälle sind
entsprechend zugeschnitten: Zugesichert ist der **Kopiermoment**, ein eigener Fall belegt die
Unabhängigkeit danach in beiden Richtungen.

Neue Ressourcenschlüssel (beide Sprachen): `SP_KONTEXT_FLOTTE`, `SP_KONTEXT_EINZEL`,
`SP_KONTEXT_OHNE`, `WIRT_ZEILE_SPEICHER`, `WIRT_PARAM_GESPEICHERT`.


## 4. Teil C — gespeicherte Läufe und der Leistungspreis der Gruppe

**Der Hinweis.** Die Herleitung, ob ein Lauf Stundenreihen braucht, steht jetzt einmal in
`WirtschaftlichkeitSeiteGaben.MitZeitreihen` und wird vom Rechenlauf und vom Ausweis gemeinsam
gerufen — zwei Fassungen wären eine Zeile gewesen, die „nicht neu gerechnet" sagt, während
gerechnet wird. Ist sie `false` und liegt mindestens ein gespeicherter Lauf vor, hängt die
Parameterzeile an: *Ergebnisse aus gespeicherten Läufen vom &lt;Datum&gt;; nicht neu gerechnet*.
Genannt wird der **älteste** Stand der Gruppe — er begrenzt, wie frisch die Tabelle im
schlechtesten Fall ist.

**LS-E-2.** `KostenEmissionRechner.StromLeistungspreisGepflegt(idStamm, versionen)` fragt Stamm
**oder** eine Version der Gruppe. Der Leistungspreis ist eine Projektübersteuerung
(`energy_project_settings.custom_price_power`); entschied allein der Stamm, rechnete die Gruppe
ohne Reihen, und einer Variante, die ihn als Einzige führt, fiel der Leistungsanteil ihrer
Energiekosten still weg, weil ihre Bezugsspitze nie eingesammelt wurde. Die Gruppenfassung gilt
auch im Berichtslauf (`BerichtSeiteGaben`) und im Kapitalwertverlauf (`KapitalwertVerlaufHuelle`).


## 5. Nachweis

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler, 5 Warnungen (Bestand, Schranke 7) |
| Windows-Schale auf Linux (`EnableWindowsTargeting=true`) | 0 Fehler, 4–5 Warnungen (Bestand) |
| `dotnet test WP-Plan.Kern.slnf` (Gate, Sammlungen seriell) | 8 650 bestanden, 0 rot, 1 übersprungen |
| `Werkzeuge/SqlDialektPruefer` | 1 478 SQL-Texte, 0 Fundstellen |
| Referenzlauf 1030, 1007, 1017, 1045, 1046 gegen die Basis | `GESAMT: PASS`, 1 656 417 Werte; alle fünf zusätzlich **byte-gleich** |

**Neue und geänderte Prüffälle.**

* `EPOS.Kern.Tests/VarianteSpeicherflotteTests` — drei Fälle: der Kopiermoment (Zeile, aktiv,
  wertgleich, gleiche Bezugsspitze), der Versatz der `AnlageId` **samt** der Folge (die Übernahme
  ändert zwei Anlagen und legt keine an), und die Unabhängigkeit von Stamm und Variante in beiden
  Richtungen.
* `EPOS.Kern.Tests/SpeicherKontextTextTests` — Flotte, Einzelspeicher, ohne Speicher, ohne
  Projekt, beide Sprachen, die fünf Ressourcenschlüssel in beiden Katalogen, und das Peak-Ziel mit
  seiner Gegenprobe.
* `EPOS.Kern.Tests/StromLeistungspreisTests` — LS-E-2: Satz nur an der zweiten Version, Stamm
  allein weiter `false`, Gruppe `true`.
* `EPOS.UI.Tests/Seiten/WirtschaftlichkeitSeiteTests` — die Spalte samt Kopf, die leere Zelle ohne
  Kontext, der Hinweis in der Parameterzeile.

**Gegenproben.** Versatz ausgehängt (`Anlagen = 0`) → `Variante_versetzt_die_AnlageId_der_
Flotteneinheiten` rot („Expected 14946, Actual null"). Kontextspalte auf ein anderes Feld
gelegt → `Die_Vergleichsgruppe_nennt_je_Zeile_den_Speicherkontext` rot. Beides zurückgebaut, beide
Fälle wieder grün.


## 6. Offene Punkte

* **Die Speicherspalte auf den Nachbarseiten — erledigt mit US-2 (17.09.2026).** Siehe
  Abschnitt 7.
* **`Tab_Berichtskonfiguration.KonfigJson` trägt Projekt-Ids** (`VariantenIds`). Heute unkritisch,
  weil die Tabelle nicht mitkopiert wird und `BerichtCtrl.Lade` ohne Zeile auf die
  Standardkonfiguration fällt. Wer die Ausnahme je aufhebt, braucht denselben Nachzug.
* **Der Nachzug läuft nur beim Duplizieren.** Ein Projekt, das über `ProjektExportImportCtrl`
  ein- und ausgeht, trägt seine Ids aus dem Paket; dort ist nicht geprüft, ob `AnlageId` im
  Flotten-JSON mitwandert.

---

## 7. Nachtrag US-2 (17.09.2026) — die Speicherspalte auf allen Seiten von „Berichte & Kosten"

**Anlass:** Nebenbefund 1 dieses Protokolls, vom Anwender ohne Einspruch übernommen.

### 7.1 Befund: es gibt keine gemeinsame Quelle — und keine drei Tabellen

Der Auftrag ging davon aus, dass die Vergleichsgruppe („Art / Bezeichner / Projektname /
Simulation") auf *Übersicht*, *Kosten* und *Wirtschaftlichkeit* dieselbe Tabelle ist.
Gemessen ist beides anders:

1. **Als Tabelle steht sie nur noch auf *Wirtschaftlichkeit* und *Bericht*.** *Übersicht*
   hat sie mit `W5-E-1` (05.09.2026) durch ein **Auswahlfeld** ersetzt — „die Tabelle
   brauchte vier bis fünf Zeilen Höhe für eine Angabe, die in eine passt"; was sie sonst
   noch sagte, steht seither in der leisen Statuszeile darunter. *Kosten* zeigt sie mit
   `W5-B-5` (08.09.2026) als **Vergleichswahl** (Kästchen je Version) über einer Matrix,
   die je Version eine **Spalte** führt.
2. **Eine gemeinsame Quelle gibt es nicht.** `VarianteZeile` (`EPOS.UI/Seiten/Berichte/
   BerichtDaten.cs`) ist zwar dieselbe Zeilenklasse für alle vier Seiten, aber gefüllt wird
   sie **viermal getrennt**: `WirtschaftlichkeitSeiteGaben`, `BerichtSeiteGaben`,
   `UebersichtSeiteGaben`, `KostenSeiteGaben`. Es sind also drei gleichartige Änderungen,
   nicht eine.

### 7.2 Umsetzung: derselbe Text, je Seite an ihrer Stelle

Der Text kommt überall aus `SpeicherAnzeigeCtrl.SpeicherKontextText(idProjekt)` und trägt
die vorhandene Ressource `WIRT_ZEILE_SPEICHER` (beide Sprachen, bereits in
`SpeicherKontextTextTests` gewacht). Gezeigt wird er dort, wo die jeweilige Seite ihre
Versionen gegenüberstellt:

| Seite | Ort | Warum dort |
|---|---|---|
| *Bericht* | **Spalte** „Stromspeicher" in der Variantenliste, neben dem Projektnamen | dieselbe Tabelle wie auf der Wirtschaftlichkeit; hier wählt der Anwender, welche Versionen in den Bericht kommen |
| *Übersicht* | **Satz** „Stromspeicher: …" in der leisen Statuszeile unter dem Auswahlfeld | dort steht seit `W5-E-1` alles, was die Tabelle einmal sagte (Simulationsstand samt „⚠") |
| *Kosten* | **erste Zeile** der Gegenüberstellung, vor der ersten Geldzeile | dieselbe Stelle wie die Zeile `SPEICHER_KONTEXT` der Kennzahlentabelle; die Matrix führt je Version eine Spalte, aus der Spalte wird hier die Zeile |

Ohne lesbaren Kontext bleibt die Zelle leer, der Satz weg, die Zeile aus — die Seite
behauptet nichts. Auf *Kosten* liefert die Hülle die Zeile gar nicht erst, wenn keine
einzige Version einen Text trägt.

Die Statuszeile der *Übersicht* trennt beide Angaben mit einem Mittepunkt
(`.epos-simstand-speicher::before`), damit „Simulation: …" und „Stromspeicher: …"
nicht ineinanderlaufen.

### 7.3 Nachweis

Fünf neue bunit-Fälle: *Bericht* (Spaltenkopf an Stelle 4, drei Texte in der Tabelle),
*Übersicht* (Satz in der Statuszeile für Stamm und Variante) und *Kosten* (erste Zeile der
Matrix, beide Texte) — dazu je eine **Gegenprobe** „ohne Kontext": die Spalte bleibt leer,
der Satz entfällt samt seinem Element, die Zeile fehlt und die Matrix zählt wieder drei
Zeilen. Die bestehenden Zählfälle (`thead th`, `tbody tr`) sind mitgezogen.
`EPOS.UI.Tests` 4 575 → 4 580.

### 7.4 Logbuch (Entwurf, Version beim Anwender zu erfragen)

> Die Vergleichsgruppe in „Berichte & Kosten" nennt jetzt auf allen Seiten, womit die
> jeweilige Version ihren Stromspeicher rechnet.

Ein zweiter Satz, **nur falls der Anwender die doppelten Katalogzeilen gesehen hat**:

> Die Liste der gesetzlichen Parameter führt jeden Schlüssel je Stichjahr wieder nur
> einmal.

Die Entdoppelung selbst (Schemaschritt 87) ist Wartung und bekommt sonst keinen Eintrag.

# Konzept — Kältemittel als Stammdatum (KM4)

**Stufe 4 der Kälteanlagen-Empfehlung** · **Stand 10.10.2026 — Fassung 3 (Entscheide E123 eingetragen), Fachkonzept zur Abnahme
durch den Anwender; Fragen KM4‑Q1 bis KM4‑Q17 entschieden (E123, alle nach Empfehlung; Abschnitt 9)** · Codestand `d21de06f9` (Zweig
`ios_migration_september`, Schemastand 212 `KaelteRangSchema`, Referenzbasis R51
`2026-10-10_R51_FreieKuehlung` mit 29 Projekten; 213 bei der Sitzung „Dialoge und Korrekturen“ angemeldet (K1),
KM4 meldet die nächste freie Nummer vor dem Bau an) · Vorarbeit: [Recherche Kälteanlagen](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md)
(Abschnitte „Kältemittel“, „Stufe 4“, „Offene Klärungen“), [Konzeptprüfung Katalog und Import](2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md)
(Stufe K‑A, Frage KKP‑Q3), [Übergabe Kälteanlagen](2026-10-10_Uebergabe_Kaelteanlagen.md) und
[Übergabe Teil Programm](2026-10-10_Uebergabe_Kaelteanlagen_Programm.md), Entscheid E118 im
[Register](../Status_Gebaeudesimulation_VDI6007.md) und der [Entwurf Split/VRF/Rückkühlwerk](2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md).
**Umsetzung:** [`Umsetzungskonzept_Kaeltemittel_Stammdatum_EPOS-Plan.md`](Umsetzungskonzept_Kaeltemittel_Stammdatum_EPOS-Plan.md).

Ziel: Das Kältemittel einer Kältemaschine ist ein freier Text ohne Bedeutung für Rechnung, Prüfung und Bericht;
die Wärmepumpe führt es als Code einer Klappliste, der allein ihre Gerätegrenzen vorbelegt; Stufe K‑A hat an der
Kältemaschine ein GWP und eine Füllmenge als nackte Zahlen ergänzt. Dieses Papier legt fest, wie das Kältemittel ein
**Stammdatum mit eigener Tabelle** wird (normierter Code, GWP mit Quelle und Bezug, F‑Gas‑, PFAS‑ und
Natürlich‑Kennzeichen, Sicherheitsgruppe), wie der **Textbestand** daran angebunden bleibt, und wie daraus — als
neue, vom Anwender zu entscheidende Festlegung — eine **Zulässigkeitsprüfung nach den Stichtagen der
F‑Gas‑Verordnung** entsteht, die **warnt und nie sperrt**. Ohne Rechenwirkung: Jedes Bestandsprojekt und die
Referenzbasis rechnen unverändert.

Eigene Ableitungen sind mit „(Abl.)“ gekennzeichnet. Normen und Verordnungen werden nur genannt, ihre Tabellen und
Texte stehen nicht im Repositorium. **Dieses Papier ist kein Rechtsrat.** Rechtsangaben tragen den Vertrauensgrad
der Recherche dieser Sitzung: **S+P** (Verordnungswortlaut als Suchauszug gesehen), **S‑amtl** (amtliche
Sekundärquelle), **S** (Fach‑ oder Herstellerquelle), **n.b.** (nicht belegt). Keine Primärquelle wurde im Volltext
geöffnet; drei Berichtigungen des deutschen Verordnungstexts sind inhaltlich nicht ermittelt. **Vor dem Säen einer
Regel oder eines Werts ist der EUR‑Lex‑Volltext zu prüfen** (offene Klärung, Abschnitt 11).

---

## 1 Ziel und Abgrenzung

**Gegenstand** sind das Kältemittel der Kältemaschine (Anlagenart 13, `Tab_Kaeltemaschine(_STAMM)`) und der
Wärmepumpe (Anlagenart 1, `Tab_WP(_STAMM)`) als Stammdatum, eine Kältemitteltabelle mit ID‑Verweis, die Zuordnung des
Textbestands und eine Zulässigkeitsprüfung mit Warnung in Lauf, Karte, Dialog und Bericht. E118 (KKP‑Q3 b) hat den
Ausschluss des Kühlkonzepts §14 für das Stammdatum (Kältemittel, GWP, Füllmenge) aufgehoben; der Entwurf
Split/VRF/Rückkühlwerk setzt darauf auf.

**Nicht Gegenstand:**

| Bereich | Bleibt bei | Grund |
|---|---|---|
| Treibhauswirkung als Rechengröße (Leckagerate, direkte Emission, Bilanz) | Ausschluss des Kühlkonzepts §14 | E118 hebt den Ausschluss nur für das Stammdatum auf; die Rechengröße ist nicht entschieden |
| Vorgabeklassen der Wärmepumpe je Kältemittel (`Bivalenzvorgaben`, Gerätegrenzen), Klappliste, `Tab_WP(_STAMM)` | Übergabegrenze und Bivalenz (UB), Sitzung Gebäudesimulation | Übergabe §1; jede Änderung ist eine Schnittstelle mit Abstimmungspflicht |
| Split, Multisplit, VRF, Absorption als Geräteart mit eigener Prüfung | K‑D, K‑E (Entwurf Split/VRF/Rückkühlwerk) | hier nur als künftige Geräteklasse mitgedacht |
| Katalogauswahl des Anlagendialogs (Stufe 5 der Katalogauswahl, E117 F6) und Katalogdialog als Überlagerung | Sitzung „Dialoge und Korrekturen“ | KM4 meldet seine Felder dort an |
| Bereich „Kälte“ der Simulationskonfiguration, Komponente `KaeltemaschineKonfiguration` (KB‑B, #928), Kältefolge `Kaelte_Rang` (KB‑D, Schritt 212, #927; Pfeile KB‑D2, #929) | Sitzung Gebäudesimulation, gebaut | Lesezeile und Kältebahn‑Warnung sind nicht mehr blockiert |
| Rückkühlung, Teil‑Freikühlung | K‑F | — |

**Warum jetzt.** Die Verbote des Inverkehrbringens nach Anhang IV der Verordnung (EU) 2024/573 greifen gestaffelt
ab 2025 (Nr. 9 a, S), 2027 (S+P, S‑amtl), 2029, 2030, 2032, 2033 und 2035 (S‑amtl); ein Planer, der heute eine
Anlage mit Inbetriebnahme 2027 auslegt, braucht den Hinweis jetzt. K‑A hat das GWP‑Feld als Zahl ohne Quelle
gebracht; erst eine Tabelle macht aus dem Text einen Code, dem Wert, Bezug und Quelle anhängen, und gibt Wärmepumpe
und Kältemaschine eine gemeinsame Wahrheit.

## 2 Ausgangslage im Code

**2.1 Kältemaschine.** `KaeltemaschineSchema` (Schritt 182) führt `Kaeltemittel` als `TEXT` ohne CHECK unter den
`Grundspalten`, die über `Fachspalten` an Katalogabgleich, Katalogpaket und Katalogfassung (Register „KM“, Stufe 3)
gehen; `KaelteKatalogfelderSchema` (Schritt 211, K‑A) hat `Geraeteart` (CHECK‑Wertemenge `KWS_LUFT`, `KWS_WASSER`,
`KWS_FREIKUEHLUNG`, `SPLIT`, `MULTISPLIT`, `VRF`, `ABSORPTION`; Bestand aus der Rückkühlart nachgetragen),
`Kaeltemittel_GWP` (≥ 0), `Kaeltemittel_Fuellmenge_kg` (> 0), `Saisonkennzahl_Art` und `Saisonkennzahl` ergänzt.
`KaeltemaschineModel.Kaeltemittel` ist `string`; `KaeltemaschineStammCtrl` liest und schreibt den Text (`Wert(o)`
wandelt einen Leerstring nicht in NULL), `Pruefen` arbeitet ohne Datenbank und prüft das Kältemittel nicht;
`KaeltemaschineCtrl.AusKatalogUebernehmen` (in `KaeltemaschineStammCtrl.cs`) kopiert über `Laden` und
`KopfSchreiben` nur die `Grundspalten` samt `TeillastSchreiben` — eine neue Spalte braucht einen eigenen
Schreibschritt. Der Rechenweg (`Simulation/Kaelte/*`, `SimulationControl.Kaelte.cs`) liest das Kältemittel nicht;
`SimulationControl.Kaelte` meldet je Maschine mit `Protokoll.WarnungEinmal`/`HinweisEinmal` (Schlüssel `kuehl-…`).
`ParameterVerwendung.Kaeltemaschine` stuft die Spalte als `DLG` („Beschreibung; F‑Gase ausgeschlossen, Kühlkonzept
6.3“) und muss jede Stammspalte führen (`ParameterVerwendungTests`).

**2.2 Wärmepumpe (UB).** `Tab_WP(_STAMM).Kaeltemittel` ist `TEXT` ohne CHECK (Schritt 205). Die Werteliste führt
der Kern als Klappliste in `EPOS.Kern/Allgemein/Simulation/Bivalenz/Bivalenzvorgaben.cs` (`internal`, zehn Codes
`R410A`, `R32`, `R290`, `R744`, `R134a`, `R1234ze(E)`, `R407C`, `R454C`, `R455A`, `R1233zd(E)` und der Auffang
`SONSTIGES`); fünf Codes tragen eine Vorgabeklasse, die `Geraetegrenzen.Bilden` über `Bivalenzvorgaben.Vorgabe` in
Spreizung, Mindestvolumenstrom und Rücklaufgrenze übersetzt (`SIM`). Abgleich `OrdinalIgnoreCase` mit Trim
(`IstBekannt`, `Vorgabe`). Lesen und Schreiben: `WaermepumpeGeraeteCtrl.KaeltemittelLesen/-Schreiben`,
`GeraetegrenzWerte.WpAusZeile/WpSchreiben/WpKopieren` (`EPOS.Kern/Controller/`); Anzeige `WPA_OPT_KAELTEMITTEL_*`,
Klappliste `WaermepumpeGeraetegrenzenFelder.razor` (Bestandswert außerhalb der Liste bleibt mit Platz −1 stehen),
Liste aus `EPOS.UI.Daten/Erzeuger/BivalenzAbbildung.Kaeltemittelliste`. Die Einfrierregel „gesäte
Übergabegrenzdaten“ nennt `Kaeltemittel` der Wärmepumpe eines Referenzprojekts und die Vorgabewerte in
`Bivalenzvorgaben`. **Folge:** Liste, Vorgabeklassen, Abgleich und `Tab_WP(_STAMM)` bleiben in KM4 unverändert; die
Tabelle trägt die Codes, die Klassen bleiben am Code (3.2, KM4‑Q9). **Grenze:** Eine Wärmepumpe mit R454B, R1234yf
oder R513A fällt auf `SONSTIGES` und bleibt „nicht prüfbar“, bis UB die Klappliste erweitert.

**2.3 Import (K‑C, gebaut).** `KaeltemaschineImportLeser` liest die Kopfzeilen `KAELTEMITTEL` (nur Trim), `GWP`
und `FUELLMENGE`; `OekodesignPunkteLeser` die Teillastpunkte; Typkennfelder setzen das Kältemittel leer; der
VDI‑3805‑Import der Wärmepumpe liest keines, schreibt aber `Bauart` „Kompakt“ oder „Split“. Eine Herstellerdatei im
VDI‑Datenordner führt Kältemittel, GWP, Füllmenge und CO₂‑Äquivalent — ungenutzt, nie als Quelle in Papier oder Wiki.

**2.4 Zeitbezug fehlt.** Es gibt kein Inbetriebnahme‑ oder Simulationsjahr an Projekt oder Anlage;
`StromPreisCtrl.Stichtag` nennt das als offenen Punkt und nimmt das Jahr der gewählten Preisreihe, sonst das
laufende Kalenderjahr („Planungsjahr“ — der Name ist damit belegt). Vorhanden: `Tab_WP(_STAMM).Baujahr`
(Gerätemodell), die KWKG‑ und Photovoltaik‑Inbetriebnahmen. Jahresabhängige Gesetzeswerte liegen in
`Tab_Gesetzesparameter` (`Schluessel` ≤ 60, `Klasse`, `JahrVon`, `Wert`, `Einheit`, `Status` ≤ 12, `Quelle` ≤ 120;
eindeutig über Klasse, Schlüssel und Jahr). `GesetzKatalog` liest sie (`AlleDerKlasse`, `WertMitHerkunft`: jüngste
Zeile mit `JahrVon ≤ jahr`), sät sie über `Vorbelegung()` nach **Generationen** (Markerzeile `KATALOG_GENERATION`,
Testdatenbank Generation 9; `StelleKatalogSicher` sät beim Start nur Zeilen höherer Generation nach, dieselbe
Vorbelegung ist Rückfall bei leerer Tabelle), kennt `KlassenVorrat()` und `KlasseAnzeige()` (Ressourcen
`GESETZ_KLASSE_ANZ_*`); `GesetzkatalogSaatWacheTests` hält jede Saatzeile in den Schranken des Schemas; die
Pflegemaske `GesetzeskatalogDialog.razor` zeigt und ändert die Zeilen je Klasse. **Regel:** kein `DateTime.Now`
im Rechenpfad (`Allgemein/Simulation/Init.cs`).

**2.5 Meldungswege.** `Warnkriterien.PruefeProjekt` liefert `Warnbefund` (`Kriterium`, `Hart`, `ID_Anlage`,
`ID_Puffer`, `Steuerwert`, `Text`), jüngstes Muster `KaeltespeicherPruefen` mit `SIMWARN_*`‑Texten. Aufrufer: der
Laufstart (`SimulationControl.WarnkriterienMelden` → `Protokoll.WarnungEinmal("warnkriterium-…")`) und die Karte
(`SimulationKonfigHuelle.WarnbefundeSammeln` → `WarnChip`). **Der Chip hängt an den Wärmeerzeuger‑Kacheln der
Kaskade** (`WaermeerzeugerGruppe`, `WErzeugerCtrl.AnlagenMitWp`): Jede Wärmepumpe bekommt ihn, **eine Kältemaschine
(Typ 13) hat dort keine Kachel** — ihr Befund wäre auf der Karte unsichtbar. Sichtbar ist sie als Knoten der
Kältebahn, den der Kern in `EPOS.Kern/Allgemein/Simulation/SchemaModell.cs` anlegt (`KaelteBahnAnlegen`, KB‑A);
die Knoten tragen `Warnung` und `Warntext` (Muster Kanal ohne Versorger mit `Warnkriterien.KanalOhneVersorgerText`),
die Hülle reicht sie durch, `Schema.razor` zeigt sie im Kurzhinweis. Dazu die Kachel „Kühlung und Kälteanlagen“
(`EPOS.UI.Daten/Erzeuger/KuehlungKachelBau.cs`, Name und Anzahl je Kältemaschine) und der Bereich „Kälte“
(`KaeltebereichBau`, KB‑A). Der Bericht übernimmt Protokollwarnungen und ‑hinweise von selbst (`BerichtsDatenSammler`
→ `bericht.warnungen`); alles Weitere, was der Bericht zeigt, läuft über `BerichtsDaten` (Wache
`BerichtSchreiberOhneDatenbankWacheTests`). Dialogseitig gilt das Muster `Bivalenzpruefung.Pruefen`: weiche Befunde,
„Speichern bleibt immer möglich“.

**2.6 Katalogregister, Paket, `Katalog_Ausgelaufen`.** `Katalog_Ausgelaufen` (26 Katalogtabellen) bedeutet „in
einer späteren Auslieferung entfallen, bleibt stehen“; nur der Katalogabgleich setzt es und setzt es zurück; es ist
Metaspalte ohne Anzeige und Filter. **Eine Wiederverwendung als „regulatorisch unzulässig“ kollidiert** mit Abgleich
und Paket — Zulässigkeit wird berechnet, nie gespeichert. Ein `Katalogverweis` stellt eine ID‑Spalte in Prüfsumme und
Paket über den stabilen Namen des Ziels dar und löst sie beim Schreiben wieder auf (Muster
`Tab_Bauteilschicht_STAMM.ID_Baustoff` → `Tab_Baustoff_STAMM`); `KatalogabgleichTests` verlangt, dass die
Zieltabelle **vor** der verweisenden im Register steht, und zählt die `_STAMM`‑Tabellen (51). Das **Projektpaket**
löst Spalten aus `KATALOG_SPALTE_ZU_TABELLE` über `LoeseKatalogAuf` auf und **legt eine am Ziel fehlende
Katalogzeile mit allen Spalten an**; Katalogverweise der Kältemaschine (`ID_Stamm`) reisen dagegen nicht und werden
am Ziel über `KaeltemaschineSchema.SqlNachtragProjekt` nachgeschlagen. `Tab_Brennstoff_Stamm` zeigt die
Metaspalten eines Registerkatalogs (es hat eine Projektkopie `Tab_Brennstoff` und keine gesperrte Zeile).

**2.7 Bericht, Dokumente, Wiki.** Berichtssatz `HINWEIS_KAELTEMITTEL` („… Kältemittelverluste sind nicht
enthalten.“, `BausteineProjekt`, englisch in `BerichtTexte`); Kälteerzeugertabelle `tabelle.kaelteerzeuger`
(`Berichtstabellen.Projekt`) ohne Kältemittelspalte. Kühlkonzept §14 und 6.3 sind nach E118 für das Stammdatum
nachzuziehen (KM4‑Q14). Wiki: Seite Kühlung („Keine Kältemittelemissionen“; von KB und den Dialogsitzungen
mitbelegt), Wärmepumpe (Gerätegrenzen, Kältemittel‑Auswahl), Gerätekataloge; `WikiProduktdatenWacheTests` trifft
Codes wie `R290` und `R1234ze` als Typcode‑Muster und lässt nur `R744` als Normbezeichnung zu. Zwei GWP‑Stellen:
`emissionsart.co2_aequivalent` (GWP₁₀₀ nach IPCC AR6, [Emissionsarten](../Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md))
und das Kältemittel.

**2.8 Testdatenbank (Schemastand 211).** `Tab_Kaeltemaschine`: drei Projektkopien (1055 und 1063 je 20 kW, 1059
10 kW), alle „R513A“, `Geraeteart` `KWS_WASSER`, `Kaeltemittel_GWP` leer; `Tab_Kaeltemaschine_STAMM`: 37 Sätze
(12 `KWS_LUFT`, 25 `KWS_WASSER`), davon 34 Typkennfelder ohne Kältemittel und die Beispielgeräte 1 „R32“, 2 „R513A“,
3 „R1234ze“ (ohne Isomerangabe); `Tab_WP` (43) und `Tab_WP_STAMM` (51) ohne Kältemittel; `Tab_WP_STAMM.Bauart` 45
leer, 5 „Split“, 1 „Monoblock“, `Typ` = Wärmequelle. `Tab_Einstellungen` hat 40 Zeilen bei 43 Projekten — keine
Zeile je Projekt. Keine Tabelle führt Kältemittelstammdaten.

## 3 Fachmodell

### 3.1 Die Kältemitteltabelle

Ein Kältemittel ist ein **globales Stammdatum** (eine Zeile je Code, keine Projektkopie — eigene Festlegung):
ausgelieferte Zeilen gesperrt (`ReadOnly = 1`, Katalogschlüssel), Anwenderzeilen frei. Der **Code** ist das
Kurzzeichen nach ISO 817 bzw. ASHRAE 34 **mit Isomerangabe** (`R1234ze(E)`, nicht `R1234ze`) und deckt die zehn
Codes der Wärmepumpen‑Klappliste Zeichen für Zeichen; `SONSTIGES` ist kein Code, sondern das Fehlen eines Codes.
Die Bezeichnung nennt Stoff oder Gemischart neutral — kein Handelsname.

Je Zeile: GWP **mit Quelle und Bezug** (Anhang I der Verordnung rechnet GWP₁₀₀ nach AR4, Anhang II und VI nach
AR6; ein Gemisch ist die massengewichtete Summe seiner Bestandteile, S‑amtl — deshalb läuft die Quelle je Wert mit,
sonst stehen Alt‑ und Neuwerte unerkannt nebeneinander), das Kennzeichen **F‑Gas** (fluoriertes Treibhausgas im
Sinn der Verordnung, Anhang I oder II — auch HFO mit GWP unter 150, Abl. aus Art. 3 Nr. 1, nicht am Wortlaut
geprüft), **natürlich** (für den Hinweis nach KM4‑Q11), die **Sicherheitsgruppe** (A1, A2L, A2, A3, B1, B2L, B2, B3
nach ISO 817 / EN 378‑1 — nur Spalte, Werte nach KM4‑Q3), der **PFAS‑Status** (dreiwertig: Stoff ist PFAS · enthält
einen PFAS‑Bestandteil · kein PFAS; Herleitung über die Strukturformel, S; Verfahrensstand „geplant, nicht in Kraft“
als Text, nie als Verbot, KM4‑Q10) und ein **Status** mit dem Vokabular von `Tab_Gesetzesparameter` (`GESICHERT`
erst nach Prüfung am EUR‑Lex‑Volltext, bis dahin `VORLAEUFIG`).

**Ob und woraus gesät wird, entscheidet der Anwender (KM4‑Q2, KM4‑Q3).** Dieses Papier enthält keine Wertetafel.
Zur Einordnung zwei Beispielwerte mit Vertrauensgrad: R32 GWP 675 (Anhang I, S+P); R1234ze(E) GWP 1,37 (Anhang II,
S). Der Unterschied zu Altwerten der Fachliteratur (R1234ze = 7 nach AR4) zeigt, warum der Bezug je Zeile steht.

### 3.2 Zwei Verweise, eine Wahrheit

**Kältemaschine — ID‑Verweis (neu).** `Tab_Kaeltemaschine(_STAMM)` bekommt `ID_Kaeltemittel` (nullbar) nach der
Hausregel „neue Beziehungen über IDs“. Die Textspalte `Kaeltemittel` **bleibt** — sie trägt den Bestand, den Import,
das Projektpaket und die Anzeige eines nicht zugeordneten Werts. **Leserichtung:** Die Prüfung und die Lesezeilen
nehmen die ID; fehlt sie, den Text per Code‑Abgleich (alte Pakete, Importe vor KM4, später angelegte Codes) — wie
bei der Wärmepumpe. Ohne Treffer zeigt der Dialog den Text als „nicht zugeordnet“, die Prüfung liefert „nicht
prüfbar“. **Schreibregel:** Die Klappliste schreibt ID **und** Text (Text = Code); bei Freitext (Import, Dialog vor
der Klappliste) setzt der Schreibweg des Controllers die ID per Abgleich, nur bei genau einem Treffer, und wandelt
Leer in NULL. Der Code einer verwendeten Anwenderzeile ist unveränderlich.

**Zuordnung des Bestands (Schemaschritt, einmalig, wiederholbar):** Je Zeile mit leerer ID und gesetztem Text wird
der Text mit Trim und `OrdinalIgnoreCase` gegen die Codes gehalten — derselbe Abgleich wie
`Bivalenzvorgaben.IstBekannt`; trifft genau ein Code, wird die ID gesetzt, der Text bleibt; ohne Treffer bleibt die
ID leer (nie raten). **Mehrdeutigkeit** wie „R1234ze“ ohne Isomer: Der Auslieferungssatz
`KM:KAELTEMASCHINE_500_KW_WASSERGEKUEHLT_MIT_NASSKUEHLER` wird über seinen Katalogschlüssel auf `R1234ze(E)`
gesetzt — die Zeile ist unser Beispielgerät, die ID war leer (Muster „nachtragen, nur wo leer“ aus Schritt 210);
Anwendertexte ohne Isomer bleiben unzugeordnet (KM4‑Q8). Die Auslieferungssätze 1 bis 3 bekommen damit eine ID und
eine neue Prüfsumme über `KatalogSchluesselSaat.Ausfuehren` (Muster `KaeltemaschinenTypkennfelder.ErgaenzenMitZaehlung`);
die Projektkopien 1055, 1059 und 1063 („R513A“) bekommen die ID, Text und Rechenweg ändern sich nicht.

**Wärmepumpe — Abgleich über den Code (UB‑Schnittstelle).** `Tab_WP(_STAMM).Kaeltemittel` bleibt die Textspalte
der Klappliste; KM4 legt dort **keine** ID‑Spalte an und ändert weder Liste noch Vorgabeklassen noch Abgleich. Die
Prüfung findet die Tabellenzeile über den Code; `SONSTIGES`, leer oder ein Bestandswert außerhalb der Liste heißt
„nicht prüfbar“. **Schnittstelle:** Eine Wache hält die zehn Codes als Teilmenge von `Tab_Kaeltemittel_STAMM.Code`;
wer die Klappliste erweitert, erweitert die Tabelle — und ein neuer Tabellencode bleibt ohne Vorgabeklasse
(allgemeine Vorgabe), bis die Sitzung Gebäudesimulation ihn in UB aufnimmt. Ob die Klappliste später aus der Tabelle
gespeist wird, entscheidet UB (KM4‑Q9).

### 3.3 Zulässigkeitsprüfung (neue Festlegung, KM4‑Q1)

**Rechtlicher Rahmen (Abl., kein Rechtsrat).** Anhang IV der Verordnung (EU) 2024/573 verbietet das
**Inverkehrbringen** bestimmter Erzeugnisse ab einem Stichtag, gestaffelt nach Geräteart, Nennleistung und GWP bzw.
„jedes F‑Gas“, meist mit der Ausnahme „Sicherheitsanforderungen am Standort“ (S+P, S‑amtl); Durchführungsrechtsakte
nach Art. 11 Abs. 5 schaffen weitere Ausnahmen; Betrieb und Wartung bestehender Anlagen sind nicht betroffen;
Altware ist ab einem Jahr nach dem Stichtag nur mit Nachweis weiterzugeben (Art. 11 Abs. 1 UAbs. 5; S). Nr. 8 und
9 erfassen Klimageräte **und Wärmepumpen** unabhängig vom Kühlbetrieb (S‑amtl). **Daraus folgt:** Das Jahr der
geplanten Inbetriebnahme ist nur ein **konservativer Stellvertreter**; die Prüfung kennt die Ausnahmen nicht, sie
**warnt, nie sperrt**, ihr Text nennt Regel, Stichtag und benutztes Jahr, und sie prüft jede Wärmepumpe mit Code,
nicht nur die im Kühlbetrieb (KM4‑Q16).

**Eingangsgrößen.**

| Größe | Kältemaschine | Wärmepumpe | leer bedeutet |
|---|---|---|---|
| Geräteklasse | `Geraeteart`: `KWS_*` → Kühler (Nr. 7); `SPLIT`, `MULTISPLIT`, `VRF` → in KM4 „nicht prüfbar“, die Split‑Regeln greifen mit K‑D/K‑E; `ABSORPTION` → kein F‑Gas‑Kreis, kein Befund | `Bauart` „Monoblock“ oder „Kompakt“ → Nr. 8; „Split“ mit `Typ` Luft‑Wasser → Split Luft/Wasser (Nr. 9); Abgleich Trim/`OrdinalIgnoreCase`; Sole‑ und Wasser‑Wasser n.b. (KM4‑Q7) | nicht prüfbar |
| Nennleistung [kW] | `Nennkaelteleistung_kW` **je Gerät** (nicht × `Kaeltemaschine_Anzahl`: Anhang IV bemisst das Erzeugnis) | `Nennleistung` (Heizleistung, KM4‑Q6; rechtlich n.b., welche Leistung einer reversiblen Wärmepumpe zählt) | nicht prüfbar |
| Kältemittel, GWP, F‑Gas | ID, sonst Text per Code → Tabelle; GWP nach KM4‑Q17 (Tabellenwert vor Geräte‑GWP `Kaeltemittel_GWP`) | Code → Tabelle | nicht prüfbar |
| Jahr | `Inbetriebnahmejahr` des Projekts (KM4‑Q5) | dasselbe | **keine Prüfung** |
| Regeln | gesäter Regelsatz (KM4‑Q4) | derselbe | — |

**Ergebnisklassen.** (1) **zulässig** — keine Regel trifft; (2) **ab Stichtag nur Restbestand oder Ausnahme** —
Warnung, mit Regel, Stichtag, Grenze, benutztem Jahr und benutzter Leistung; (3) **Bestandsanlage — Anhang IV nicht
anwendbar** — vorgesehen für ein Bestandskennzeichen an der Anlage (KM4‑Q12; in dieser Fassung tritt die Klasse
nicht ein); (4) **nicht prüfbar** — **nur bei gesetztem Jahr**, wenn eine andere Eingangsgröße fehlt; Hinweis, keine
Warnung, mit der fehlenden Größe im Text. **Ohne Jahr prüft und meldet der Lauf nichts** — Bestandsprojekte und
Referenzprotokolle bleiben unverändert.

**Regelsatz (Form, nicht Werte).** Eine Regel ist das Tupel (Geräteklasse, Leistungsband, Kriterium, Stichtag,
Status, Quelle) mit Kriterium „GWP ≥ Grenze“ oder „jedes F‑Gas“. Anhang IV kennt die Geräteklassen Kühler (Nr. 7),
steckerfertige, Monoblock‑ und in sich geschlossene Geräte (Nr. 8) und Split (Nr. 9, darunter Luft/Wasser und
Luft/Luft), die Bänder bis 12 kW, über 12 kW, über 12 bis 50 kW und über 50 kW, die Grenzen 150 und 750 und
Stichtage zwischen 2025 und 2035 (S+P, S‑amtl; die Buchstaben unter Nr. 9 und die 750‑Grenze bei
Sicherheitsausnahme unter Nr. 8 b sind nicht bestätigt; Nr. 9 a hängt an einer Füllmenge unter 3 kg — das
Regelschema kennt dieses Kriterium nicht, die Zeile wird **nicht gesät**). **Auswertung (Abl.):** Eine Geräteklasse
erbt die Regeln ihrer Oberklasse (`SPLIT_LUFTWASSER`, `SPLIT_LUFTLUFT` → `SPLIT`). Das Band ist ein Intervall
(untere Grenze ausschließlich, obere einschließlich); eine Regel gilt, wenn die Nennleistung darin liegt. Je
Schlüssel gilt die jüngste Zeile mit `JahrVon ≤ Jahr` (Zeitreihe wie `GesetzKatalog.WertMitHerkunft`), über die
Schlüssel hinweg gelten alle; trifft eine, ist das Ergebnis Klasse 2, und der Text nennt die treffende Regel mit dem
frühesten Stichtag. **Die Zahlen werden beim Bau aus dem EUR‑Lex‑Volltext aufgenommen, nicht aus diesem Papier und
nicht aus der Recherche**; bis zur Prüfung tragen sie `VORLAEUFIG`.

**Zwei Beispiele aus der Testdatenbank (Abl., zur Veranschaulichung; Werte S+P/S‑amtl).** Die Kältemaschine von
1063 (20 kW, R513A, GWP rund 630, S) fällt unter „Kühler über 12 kW: GWP ≥ 750 ab 2027“ nicht — zulässig. Die auf
10 kW skalierte Maschine von 1059 fällt unter „Kühler bis 12 kW: GWP ≥ 150 ab 2027“ — mit Inbetriebnahmejahr 2027
eine Warnung, mit 2026 zulässig, ohne Jahr keine Prüfung. Kein Referenzprojekt führt ein Jahr; beide Fälle werden
Proben, nicht Teil der Basis (Abschnitt 8).

### 3.4 Bestandsanlagen und Art. 13

Für eine Anlage, die schon in Verkehr gebracht ist, gelten nicht die Verbote des Anhangs IV, sondern die
Wartungs‑ und Nachfüllverbote nach Art. 13 (Frischware mit GWP ≥ 2 500 für Kälteanlagen ab 2025 und für ortsfeste
Klimaanlagen und Wärmepumpen ab 2026, GWP ≥ 750 für ortsfeste Kälteanlagen ohne Kühler ab 2032, Ausnahmen für
aufbereitete Ware; S+P/S; ob ein Gebäude‑Kaltwassersatz unter Abs. 3 oder Abs. 4 fällt, ist n.b.). EPOS‑Plan kennt
kein Bestandskennzeichen an der Anlage; diese Fassung sieht die Ergebnisklasse vor und führt Kennzeichen und
Art.‑13‑Regeln als Ausbau (KM4‑Q12): Die 2 500‑Grenze trifft von den gängigen Kältemitteln nur R404A (S), kein
Referenzprojekt bildet einen Bestand ab.

### 3.5 Hinweise ohne Rechtsfolge

- **PFAS:** Lesezeile „enthält PFAS — Beschränkung unter REACH in Vorbereitung, nicht in Kraft“ (endgültige
  SEAC‑Stellungnahme bis Ende 2026 erwartet, Inkrafttreten unbelegt; S); nie Warnung, nie Verbot (KM4‑Q10).
- **Natürliches Kältemittel:** Lesezeile ohne Förderaussage; die Förderregeln (BEG EM für Wärmepumpen ab 2028, BEG
  NWG für Raumkühlung ab 2030; nur Sekundärquellen, uneins) allenfalls im Wiki in Worten (KM4‑Q11).
- **CO₂‑Äquivalent der Füllung** (kg × GWP / 1 000) und die Schwellen der Dichtheitskontrolle (Art. 5: ab 5 t CO₂e
  bzw. 1 kg für Anhang‑II‑Stoffe; S) berühren E118 (Treibhauswirkung) und sind deshalb Frage (KM4‑Q13).

### 3.6 Was unverändert bleibt

Rechenweg, Kältefolge, Gerätegrenzen der Wärmepumpe (UB), Ergebnistabellen, Kennzahlen, Referenz‑CSV und der
Berichtssatz `HINWEIS_KAELTEMITTEL` (er bleibt wahr).

## 4 Eingaben

### 4.1 Neue Tabelle `Tab_Kaeltemittel_STAMM` (Vorschlag)

`STRICT`, im Katalogregister (Stufe 3, Kürzel `KMT`, **vor** „KM“ eingereiht, Anzeigeschlüssel; keine Projektkopie,
kein Kind), mit den Metaspalten `ReadOnly`, `Katalog_Schluessel`, `Katalog_Pruefsumme`, `Katalog_Ausgelaufen` wie
`Tab_Brennstoff_Stamm`; Namensspalte `Code`, Schlüssel bereinigt nach `Katalogfassung.Schluesselstamm`
(`R1234ze(E)` → `KMT:R1234ZE_E`). Wertemengen als Konstanten in `DbWerte`. Alle Fachspalten außer `Code` nullbar;
**leer = unbekannt**, nie eine stille Vorgabe.

| Spalte | Typ | Bereich / CHECK | Vorgabe bei NULL | Bedeutung |
|---|---|---|---|---|
| `Code` | TEXT NOT NULL UNIQUE COLLATE NOCASE | `length ≤ 20` | — | Kurzzeichen mit Isomer (`R1234ze(E)`), identisch mit den Codes der Wärmepumpen‑Klappliste |
| `Bezeichnung` | TEXT | `length ≤ 120` | leer | Stoff‑ oder Gemischname, neutral |
| `Gruppe` | TEXT | `CHECK (Gruppe IN ('HFKW','HFO','HFCKW','GEMISCH','NATUERLICH','SONSTIGE'))` | leer | Stoffgruppe (Anzeige, Sortierung) |
| `GWP` | REAL | `CHECK (GWP >= 0)` | nicht prüfbar | GWP₁₀₀ |
| `GWP_Quelle` | TEXT | `length ≤ 120` | leer | Quelle und Bezug des Werts („Anhang I (AR4)“, „Anhang II (AR6)“, „gewichtet nach Anhang I/VI“) |
| `F_Gas` | INTEGER | `CHECK (F_Gas IN (0,1))` | nicht prüfbar für „jedes F‑Gas“ | fluoriertes Treibhausgas im Sinn der Verordnung |
| `Natuerlich` | INTEGER | `CHECK (Natuerlich IN (0,1))` | kein Hinweis | natürliches Kältemittel |
| `Sicherheitsgruppe` | TEXT | `CHECK (… IN ('A1','A2L','A2','A3','B1','B2L','B2','B3'))` | leer | ISO 817 / EN 378‑1 (nur Anzeige) |
| `PFAS_Status` | TEXT | `CHECK (… IN ('PFAS','BESTANDTEIL','KEIN'))` | kein Hinweis | dreiwertig (3.1) |
| `Status` | TEXT | `CHECK (Status IN ('GESICHERT','VORLAEUFIG','PROGNOSE','ABGEKUENDIGT'))` | `VORLAEUFIG` | Belegstand der Zeile |
| `Quelle` | TEXT | `length ≤ 120` | leer | Quelle der übrigen Kennzeichen |
| `Sortierung` | INTEGER | — | Code | Reihenfolge der Klappliste |

### 4.2 Verweis an `Tab_Kaeltemaschine_STAMM` und `Tab_Kaeltemaschine`

| Spalte | Typ | Bereich / CHECK | Vorgabe bei NULL | Bedeutung |
|---|---|---|---|---|
| `ID_Kaeltemittel` | INTEGER | nullbar, ohne Fremdschlüsselzwang (Löschen einer verwendeten Anwenderzeile sperrt der Controller; Auslieferungszeilen sind unlöschbar) | Text gilt (Code‑Abgleich), sonst nicht zugeordnet | Verweis auf `Tab_Kaeltemittel_STAMM.ID`; in `KaeltemaschineSchema.Fachspalten`, in Prüfsumme und Katalogpaket über `Katalogverweis("ID_Kaeltemittel", "Tab_Kaeltemittel_STAMM", "Code")`; eigener Eintrag in `ParameterVerwendung.Kaeltemaschine` (`DLG`) |

`Kaeltemittel` (TEXT) bleibt; der Schreibweg wandelt Leer in NULL (heute nicht: `Wert(o)` lässt den Leerstring
stehen).

### 4.3 Wärmepumpe

Keine neue Spalte an `Tab_WP(_STAMM)` (KM4‑Q9). Der Code der Textspalte ist der Verweis.

### 4.4 Inbetriebnahmejahr

| Spalte | Typ | Bereich / CHECK | Vorgabe bei NULL | Bedeutung |
|---|---|---|---|---|
| `Tab_Projekt.Inbetriebnahmejahr` (KM4‑Q5 a; b: `Tab_Einstellungen`) | INTEGER | `CHECK (Inbetriebnahmejahr BETWEEN 2000 AND 2100)` | keine Prüfung | geplantes Jahr der Inbetriebnahme des Projekts; in KM4 liest es allein die Zulässigkeitsprüfung (Preisversion, KWKG‑Jahr sind eigene Entscheide mit Basiswechsel) |

### 4.5 Regelsatz

Vorschlag (KM4‑Q4): Zeilen in `Tab_Gesetzesparameter` mit `Klasse = 'FGAS'` (`DbWerte.GESETZ_KLASSE_FGAS`, Einheit
`GESETZ_EINHEIT_OHNE`), `Schluessel` nach festem Vokabular `FGAS_IV_<KLASSE>_<BAND>_<KRIT>` (Klasse `KUEHLER`,
`MONOBLOCK`, `SPLIT`, `SPLIT_LUFTWASSER`, `SPLIT_LUFTLUFT`; Band `BIS12`, `UEBER12`, `UEBER12BIS50`, `UEBER50`;
Kriterium `GWP` mit `Wert` = Grenze oder `FGAS` mit `Wert` = 0; Konstanten in `DbWerte`), `JahrVon` = Stichtag,
`Status`, `Quelle` mit Anhang, Nummer und Vertrauensgrad; Art.‑13‑Regeln mit Präfix `FGAS_13_…` (KM4‑Q12). **Saatweg:**
als neue Generation in `GesetzKatalog.Vorbelegung()` (Testdatenbank 9 → 10; `StelleKatalogSicher` sät beim Start
nach, `GesetzkatalogSaatWacheTests` misst die Schranken), **nicht im Schemaschritt**; Klasse in `KlassenVorrat()`
und `KlasseAnzeige()` (Ressource `GESETZ_KLASSE_ANZ_FGAS`); die Pflegemaske „Gesetzeskatalog“ zeigt und ändert die
Zeilen. Alternativen: Code‑Festwerte mit Quelle (keine Pflege ohne Auslieferung) oder eine eigene Regeltabelle
(ausdrücklicher, mehr Schema).

### 4.6 Import und Paket

| Weg | Was neu gelesen wird |
|---|---|
| CSV‑Vorlage der Kältemaschine (`KaeltemaschineImportLeser`, K‑C) | Kopfzeile `Kältemittel` wie heute als Text; der Schreibweg setzt die ID per Code‑Abgleich, ohne Treffer bleibt sie leer mit Info‑Meldung |
| Copper‑ und Typkennfelder, VDI‑3805‑Wärmepumpe | unverändert (leer bzw. ohne Kältemittel) |
| Projektpaket | `ID_Kaeltemittel` **reist nicht** (Ausnahme wie `ID_Stamm`); am Ziel setzt der Import sie über den Text: Code‑Abgleich, nur bei genau einem Treffer (Muster `KaeltemaschineSchema.SqlNachtragProjekt`); ohne Treffer bleibt sie leer, der Text trägt den Wert; `Paketanhebung` mit `Art.Import` |

## 5 Prüfweg

### 5.1 Reihenfolge je Anlage (reine Klasse `Kaeltemittelzulaessigkeit`, ohne Datenbank)

1. Jahr fehlt → keine Prüfung, kein Befund.
2. Eingang bilden: Geräteklasse, Nennleistung, Kältemittelzeile (GWP, F‑Gas), Geräte‑GWP, Jahr, Regelzeilen.
3. Fehlt eine Größe → Klasse 4 mit der fehlenden Größe; Absorption → kein Befund.
4. Regeln der Klasse samt Oberklasse im Band sammeln, je Schlüssel die jüngste mit Stichtag ≤ Jahr; Kriterium
   anwenden (GWP nach KM4‑Q17; „jedes F‑Gas“ nur mit `F_Gas = 1`).
5. Trifft keine → Klasse 1; sonst Klasse 2 mit der treffenden Regel des frühesten Stichtags.

Die Klasse liefert ein Ergebnis mit Klasse, Regelschlüssel, Grenze, Stichtag, benutztem Jahr und benutzter
Leistung; Texte entstehen aus Ressourcen in `Warnkriterien` (Drei‑Schichten‑Regel: Schlüssel neutral, Text
lokalisiert).

### 5.2 Meldungen

- **Laufstart und Karte:** `Warnkriterien.PruefeProjekt` ruft `KaeltemittelPruefen(idProjekt, befunde)` nach
  `KaeltespeicherPruefen`: je Kältemaschine und je Wärmepumpe mit Code (KM4‑Q16) ein `Warnbefund` (Kriterium
  `KAELTEMITTEL_STICHTAG`, `Hart = false`, `ID_Anlage`) nur für Klasse 2. Der Laufstart meldet ihn über
  `WarnkriterienMelden` (`WarnungEinmal`), der Bericht nimmt ihn über `bericht.warnungen` auf. Die Karte zeigt ihn
  an jeder Wärmepumpe als Chip (`WarnChip` greift dort); für die Kältemaschine gibt es keine Kachel (2.5) — **neue
  Stelle:** `SchemaModell.KaelteBahnAnlegen` setzt `Warnung` und `Warntext` am Knoten der Kältemaschine aus den
  Befunden (nach dem Push von KB‑D, der dieselbe Stelle berührt), dazu eine Hinweiszeile in der Kachel „Kühlung und
  Kälteanlagen“ („n Kälteerzeuger mit Kältemittelhinweis“).
- **„Nicht prüfbar“:** nur bei gesetztem Jahr — `HinweisEinmal("kuehl-km-kaeltemittel-<id>")` am Haken in
  `SimulationControl.Kaelte` mit der fehlenden Größe. Ohne Jahr prüft und meldet der Lauf nichts.
- **Dialog (weich, Muster `Bivalenzpruefung`):** Lesezeile „Zulässigkeit“ in der Komponente
  `KaeltemaschineKonfiguration` (KB‑B) und im Anlagendialog der Kältemaschine (nach Stufe 5) sowie im Anlagendialog
  der Wärmepumpe (dort ist das Projekt bekannt); im Katalogdialog nur die Stammwerte (GWP, F‑Gas, Sicherheitsgruppe,
  PFAS, natürlich). Speichern bleibt immer möglich.

### 5.3 Bericht, KI‑Sicht, Export

`BerichtsDatenSammler` sammelt je Kälteerzeuger Code und Ergebnisklasse nach `BerichtsDaten`; die
Kälteerzeugertabelle `tabelle.kaelteerzeuger` bekommt die Spalte „Kältemittel“ (Code) und bei Klasse 2 einen Stern
mit Fußnote, Spaltentitel und Fußnote über `BerichtTexte`; Warnungen laufen über `bericht.warnungen` — kein neues
Vorlagenfeld, die Vorlagen‑Katalogfassung bleibt. KI‑Sichten der Katalog‑ und Anlagendialoge führen Code und
Lesewerte mit den Spaltennamen als Schlüssel (Wachen `KiMaskenabdeckungWacheTests`, `KiDialogkatalogTests`).
CSV‑Export unverändert.

## 6 Schema

- **Ein Schritt**, Klasse `KaeltemittelSchema`, Nummer **nächste freie zur Bauzeit** (213 ist für K1 Kältebedarf vergeben) — vor dem Bau in der Kopfzeile
  „Schemaschritt angemeldet“ der [Statusdatei](../Status_iOS_Migration.md) mit dem Vermerk „Sitzung Kälteanlagen“
  anmelden und allein pushen; `SCHRITT = <Vorgängerklasse>.SCHRITT + 1` an der Klasse, die zur Bauzeit die höchste
  Nummer trägt (KB‑D, 212, liegt auf `origin`; K‑F1 läuft vor KM4‑E1, siehe Umsetzungskonzept 3.0).
- Inhalt: `Tab_Kaeltemittel_STAMM` (4.1) mit Registereintrag und Katalogschlüsseln; `ID_Kaeltemittel` an beiden
  Kältemaschinentabellen (4.2) und in `KaeltemaschineSchema.Fachspalten`; `Inbetriebnahmejahr` (4.4); Saat der
  Kältemittelzeilen (KM4‑Q2, Q3, Q10, Q11) mit `Status`; Zuordnung des Textbestands (3.2) mit neuer Prüfsumme über
  `KatalogSchluesselSaat.Ausfuehren`. **Die Regelzeilen sät nicht der Schritt, sondern `GesetzKatalog.Vorbelegung()`
  (4.5).** DDL in einem Vorgang mit abgeschalteten Fremdschlüsseln, Saat und Zuordnung in eigenen; ein zweiter Lauf
  ändert nichts. Tabellen bleiben `STRICT`; Boolean als 0/1 mit CHECK.
- Eintragsstellen wie Schritt 211: Klasse unter `EPOS.Kern/Allgemein/Update/`, `SchemaStand.Zielversion`,
  `Paketanhebung.STUFEN` (Art Import), `SchemaMigration` der Windows‑Schale, `Werkzeuge/Testdatenbankschema`,
  `EPOS.Kern.Tests/TestDatenbank.cs`; Nachzüge: LFS‑Testdatenbank, [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md),
  `KaeltemittelSchemaTests`, `TestdatenbankSchemastandWacheTests`; Spaltenzahlen in `KaeltemaschineSchemaTests`
  (32 → 33, 30 → 31), Registerzählung in `KatalogabgleichTests` (51 → 52) und — nur bei KM4‑Q5 b — die
  Einstellungsspalten in `AufheizSchemaTests`.
- Katalogabgleich und Auslieferungsvorlage kennen die Tabelle über das Register (Anwenderzeilen fallen in der
  Auslieferung, gesperrte Zeilen bleiben — zu prüfen in `Werkzeuge/Auslieferungsvorlage`).
- `SqlDialektPruefer` nach jeder neuen Anweisung ([BETRIEB_SQLITE](../BETRIEB_SQLITE.md) § 6,
  [ADR‑001](../ADR-001_Schema-Ausrollung.md)).

## 7 Oberfläche und Wiki

**7.1 Katalogdialog Kältemaschine** (`KaeltemaschineKatalogDialog.razor`, Daten, Texte, KiSicht, Hülle
`KaeltemaschineKatalogHuelle`): Das Textfeld „Kältemittel“ wird eine Klappliste aus der Tabelle (Code und
Bezeichnung, Reihenfolge `Sortierung`); ein Bestandstext ohne Treffer erscheint als Eintrag „<Text> — nicht
zugeordnet“ (Muster Platz −1 der Wärmepumpe) und bleibt wählbar; darunter die Lesezeile GWP (mit Herkunft „Tabelle“
oder „Gerät“ und Abweichungshinweis nach KM4‑Q17), F‑Gas, Sicherheitsgruppe, PFAS‑Hinweis, natürlich; Schnellwahl
„GWP aus Kältemittel übernehmen“ füllt `Kaeltemittel_GWP` (K‑A). Ausgelieferte Sätze lesend. **Der Dialog ist die
Überlagerung der Katalogauswahl, die Stufe 5 (E117 F6, Sitzung „Dialoge und Korrekturen“) im Anlagendialog
umbaut** — KM4 baut dort erst nach Abstimmung (KM4‑Q15).

**7.2 Anlagendialog und `KaeltemaschineKonfiguration`:** Lesezeile „Zulässigkeit“ nach dem Muster der
KM3‑Lesewerte (Ergebnisklasse, Regel, Stichtag, Jahr; bei „nicht prüfbar“ die fehlende Größe) — in der Komponente
`KaeltemaschineKonfiguration` (KB‑B, gebaut), im Anlagendialog nach Stufe 5. Keine neue Eingabe.

**7.3 Wärmepumpe:** In der Gruppe „Gerätegrenzen“ nur eine Lesezeile unter der Klappliste (GWP, F‑Gas,
Sicherheitsgruppe, PFAS, natürlich aus der Tabelle; leer bei `SONSTIGES`); Lesezeile „Zulässigkeit“ im Anlagendialog
jeder Wärmepumpe (KM4‑Q16). Klappliste, Schnellwahlknöpfe und Vorgabeklassen unverändert (UB).

**7.4 Inbetriebnahmejahr:** mit KM4‑Q5 a Eingabe in `ProjektKopfSeite.razor` (Projektdaten; nicht von KB‑B belegt),
Platzhalter „leer: keine Zulässigkeitsprüfung“; mit KM4‑Q5 b auf der Simulationskonfigurationsseite (KB‑B, gebaut).

**7.5 Karte und Kachel:** Kältebahn‑Knoten mit `Warnung`/`Warntext` aus `SchemaModell.KaelteBahnAnlegen` (KB‑D,
gebaut), Kachel „Kühlung und Kälteanlagen“ mit Hinweiszeile (5.2).

**7.6 Verwaltung der Kältemittel:** eine einfache Liste (Anzeige, Anwenderzeile anlegen, bearbeiten, löschen
sofern unbenutzt; Code einer verwendeten Zeile gesperrt) im Menü „Daten & Import“ — Muster der Katalogdialoge,
lesend für Auslieferungszeilen; Seitenschlüssel, Hilfeziel, Menüband; Texte in `MyResource.Resource.*` (beide
Sprachen), danach `designer_neu.py schreiben`. KI‑Sichten für alle neuen Felder.

**7.7 Wiki** (Repo‑Quellen, gebündelter Upload; die Seite Kühlung ist von KB und den Dialogsitzungen mitbelegt —
abstimmen): `Programm Dokumentation - Gerätekataloge.wiki` (Kältemittel als Auswahl mit Lesewerten, Verwaltung),
`Programm Dokumentation - Kühlung.wiki` (Inbetriebnahmejahr, Zulässigkeitshinweis in Worten: was geprüft wird,
dass gewarnt und nicht gesperrt wird, dass Ausnahmen nicht geprüft werden; der Absatz „Keine Kältemittelemissionen“
bleibt), `Programm Dokumentation - Wärmepumpe.wiki` (Lesezeile). Keine Stichtagstafel, keine Werte; Codes nur, wenn
`WikiProduktdatenWacheTests.Normbezeichnungen` sie zulässt (die gesäten Codes sind Normbezeichnungen nach ISO 817,
keine Produkte — die Wache wird um sie erweitert). **Logbuch‑Entwurf** (Version beim Anwender erfragen), je ein
Satz: „Kältemaschine und Wärmepumpe führen ihr Kältemittel aus einer Kältemittelliste mit GWP und Kennzeichen; die
Liste ist pflegbar.“ · „Mit einem Inbetriebnahmejahr weist EPOS‑Plan hin, wenn ein Stichtag der F‑Gas‑Verordnung
Geräteart und Kältemittel betrifft.“ Gegenlesen mit dem Muster aus `CLAUDE.md`.

## 8 Regressionsnetz

**8.1 Ergebnisneutral.** Der Referenzvergleich (`Referenzlauf/Vergleich.cs`, von `EPOS.Referenzlauf` geteilt) hält
CSV‑Vektoren und Skalare in Toleranz; `protokoll.txt` wird geschrieben, nicht verglichen. Das Kältemittel hat keine
Rechenwirkung, das Inbetriebnahmejahr ist in der Testdatenbank leer, ohne Jahr prüft der Lauf nichts — kein Vektor,
kein Skalar und keine Protokollzeile ändert sich. Erwartung: alle Projekte der zur Bauzeit aktuellen Basis
byte‑gleich.

**8.2 Einfrierregeln.** „Gesäte Kältemaschinendaten“ nennt das Kältemittel der Projektkopie nicht; „gesäte
Übergabegrenzdaten“ nennt `Kaeltemittel` der Wärmepumpe und `Bivalenzvorgaben` — beides bleibt unberührt. Die
Zuordnung setzt an den Projektkopien 1055, 1059 und 1063 nur die neue ID; der Text „R513A“ bleibt. **Keine Basis
wird neu eingefroren, keine Einfrierregel kommt hinzu**; die Testdatenbank wird um den Schemaschritt und die
Generation des Gesetzeskatalogs gehoben.

**8.3 Proben statt Referenzprojekt.** Die Fälle 1059/1063 mit gesätem Jahr 2027 sind Tests gegen eine
Arbeitskopie (`KaeltemittelZulaessigkeitLaufTests`); ein Referenzprojekt mit Jahr brächte keine neue Rechengröße in
die Basis.

## 9 Fragen an den Anwender

| Kennung | Gegenstand | Optionen | Empfehlung | Entscheid |
|---|---|---|---|---|
| **KM4‑Q1** | Zulässigkeitsprüfung als Warnung (neue Festlegung über E118 hinaus) | a ja — Warnung in Lauf, Karte, Dialog, Bericht, nie sperrend · b nur Stammdatum, keine Prüfung · c Prüfung nur als Lesezeile im Dialog | **a** — der Nutzen der Stufe liegt in der Warnung vor dem Stichtag; die Ausnahmen bleiben beim Anwender, deshalb nie sperrend | **a** (E123) |
| **KM4‑Q2** | Säen der Kältemittelzeilen und GWP‑Werte | a Codes und GWP aus Anhang I/II der Verordnung (amtliches Werk) mit Quelle und Bezug je Zeile, `VORLAEUFIG` bis zur Prüfung am EUR‑Lex‑Volltext · b nur Codes, Werte leer · c nichts säen | **a** — ohne Werte prüft nichts; das Haus sät Gesetzeswerte mit Quelle und Status bereits im Gesetzeskatalog (KWKG, EEG); Normtexte bleiben draußen (Abl., kein Rechtsrat) | **a** (E123) |
| **KM4‑Q3** | Sicherheitsgruppen (ISO 817 / EN 378‑1 / ASHRAE 34) | a säen aus frei zugänglichen Klassifikationen (Sicherheitsdatenblätter) · b Spalte anlegen, leer lassen, pflegbar · c keine Spalte | **b** — nur Anzeige, keine Prüfung braucht sie; Normtexte nicht eingesehen (Hausregel) | **b** (E123) |
| **KM4‑Q4** | Ablage, Saatweg und Pflegbarkeit des Regelsatzes (Anhang IV, später Art. 13) | a Code‑Festwerte mit Quellenvermerk, nicht pflegbar · b Zeilen im Gesetzeskatalog (Klasse `FGAS`, festes Schlüsselvokabular), gesät als neue Generation über `GesetzKatalog.Vorbelegung()`, sichtbar und änderbar in der Pflegemaske „Gesetzeskatalog“ (Stichtag, Grenze, Status) · c eigene Regeltabelle mit eigener Maske | **b** — jahresabhängige Gesetzeswerte mit `Status` und `Quelle`, Nachsaat beim Start und Pflegemaske sind das Hausmuster; keine neue Tabelle, keine Saat im Schemaschritt | **b** (E123) |
| **KM4‑Q5** | Jahr der Prüfung (`Inbetriebnahmejahr`) | a an den Projektdaten `Tab_Projekt` (Eingabe in `ProjektKopfSeite`; jede Projektzeile vorhanden; umgeht KB‑B und `AufheizSchemaTests`) · b in `Tab_Einstellungen` (Simulationskonfiguration nach KB‑B; 40 Zeilen bei 43 Projekten) · c je Anlage (Ausbau mit KM4‑Q12) | **a** — eine Eingabe am Projekt, reproduzierbar (kein `DateTime.Now`), ohne Wartezeit auf KB‑B | **a** (E123) |
| **KM4‑Q6** | Nennleistung der reversiblen Wärmepumpe | a `Nennleistung` (Heizleistung, Herstellerangabe) · b `Kuehlleistung` · c die kleinere von beiden (strengere Stufe) | **a** — Herstellerangabe des Erzeugnisses (S‑amtl: Nennleistung unter Normbedingungen); der Warntext nennt die benutzte Leistung; rechtlich n.b. | **a** (E123) |
| **KM4‑Q7** | Geräteklasse der Wärmepumpe | a nur Luft‑Wasser mit `Bauart` Monoblock oder Kompakt (Nr. 8) bzw. Split (Nr. 9) prüfen, sonst „nicht prüfbar“ · b Sole‑ und Wasser‑Wasser als „in sich geschlossen“ (Nr. 8) prüfen (Abl.) · c Wärmepumpe ganz ausnehmen | **a** — `Bauart` ist meist leer, Raten verbietet sich; b nach Prüfung am Wortlaut als Ausbau | **a** (E123) |
| **KM4‑Q8** | Zuordnung des Textbestands | a exakte Treffer zuordnen; Auslieferungssatz „R1234ze“ über seinen Katalogschlüssel auf `R1234ze(E)`; Anwendertexte ohne Isomer bleiben unzugeordnet · b Regel „ohne Isomer → (E)“ auch für Anwendertexte · c keine Zuordnung im Schemaschritt | **a** — nie raten; das Beispielgerät ist unseres, sein Fall ist bekannt | **a** (E123) |
| **KM4‑Q9** | Wärmepumpe und Tabelle | a Abgleich über den Code, keine Schemaänderung an `Tab_WP`, Klappliste und Vorgabeklassen bei UB, Wache „Codes ⊆ Tabelle“ · b zusätzlich `ID_Kaeltemittel` an `Tab_WP(_STAMM)` (UB‑Abstimmung) · c Wärmepumpe außen vor | **a** — die Beziehung besteht schon als Code (UB‑Entscheid); KM4 schafft keine zweite und fasst UB nicht an | **a** (E123) |
| **KM4‑Q10** | PFAS‑Status | a dreiwertige Spalte mit Verfahrensstand als Lesezeile, nie Warnung · b nicht aufnehmen | **a** — Hinweis ohne Rechtsfolge; Status aus der Strukturformel (S), Text nennt „nicht in Kraft“ | **a** (E123) |
| **KM4‑Q11** | Hinweis „natürliches Kältemittel“ | a Kennzeichen und Lesezeile ohne Förderaussage · b dazu die Förderdaten (2028/2030) als Text · c nicht | **a** — Förderregeln nur aus Sekundärquellen und uneins; Daten allenfalls im Wiki | **a** (E123) |
| **KM4‑Q12** | Bestandsanlagen und Art. 13 | a Kennzeichen `Bestand` an der Anlage und drei Art.‑13‑Regeln jetzt · b als Ausbau nach Bedarf | **b** — kein Referenzprojekt bildet Bestand ab, die 2 500‑Grenze trifft von den gängigen Kältemitteln nur R404A (S); die Ergebnisklasse bleibt vorgesehen | **b** (E123) |
| **KM4‑Q13** | CO₂‑Äquivalent der Füllung (kg × GWP / 1 000) und Dichtheitsprüf‑Schwellen | a Lesewert mit Schwellenhinweis (Art. 5) · b Lesewert ohne Schwellen · c nichts (E118: Treibhauswirkung nicht entschieden) | **c** in dieser Fassung — der Lesewert ist nah an der ausgeschlossenen Rechengröße; a oder b erst mit einem Entscheid zu E118 | **c** (E123) |
| **KM4‑Q14** | Wortlaut‑Nachzug Kühlkonzept §14 und 6.3, Berichtssatz, Wiki‑Absatz | a KM4‑E4 zieht §14/6.3 um einen Satz nach (Stammdatum und Prüfung zugelassen, Treibhauswirkung weiter ausgeschlossen), Berichtssatz und Wiki‑Absatz bleiben, Wiki ergänzt · b Sitzung Gebäudesimulation zieht §14 mit E118 nach · c nur Wiki | **a** — Kältemittel ging mit der Übergabe an diese Sitzung; Abstimmung mit der Gebäudesimulation vor dem Nachzug | **a** (E123) |
| **KM4‑Q15** | Katalogdialog (Überlagerung der Katalogauswahl, die Stufe 5 bei „Dialoge und Korrekturen“ umbaut) und Lesezeile | a Klappliste und Lesewerte im Katalogdialog nach Abstimmung mit der Dialogsitzung; Lesezeile „Zulässigkeit“ in `KaeltemaschineKonfiguration` nach KB‑B und im Anlagendialog nach Stufe 5 · b vorher minimal (Lesezeile neben dem Textfeld), Klappliste später | **a** — kein doppelter Umbau; bis dahin tragen Protokoll, Bericht, Kachel und Kältebahn den Befund | **a** (E123) |
| **KM4‑Q16** | Prüfumfang Wärmepumpe | a jede Wärmepumpe mit Code (Anhang IV Nr. 8/9 gilt unabhängig vom Kühlbetrieb) · b nur im Kühlbetrieb · c keine | **a** — der häufigste Fall (Heiz‑Wärmepumpe Split Luft/Wasser bis 12 kW ab 2027) bliebe sonst ungewarnt; nur lesend, Befund am Chip der Wärmepumpen‑Kachel; Abstimmung mit UB | **a** (E123) |
| **KM4‑Q17** | GWP‑Vorrang an der Kältemaschine (Tabelle gegen Geräte‑GWP `Kaeltemittel_GWP`) | a Tabellen‑GWP, wenn ID und Tabellenwert gesetzt; Geräte‑GWP nur ohne ID oder ohne Tabellenwert (Gemischvariante, Anwendercode); Abweichung über 1 % als Hinweis in der Lesezeile · b Geräte‑GWP vor Tabellen‑GWP · c nur Tabellen‑GWP | **a** — die Prüfung läuft gegen das GWP nach den Anhängen; eine Datenblattangabe hat unbekannten Bezug und friert ein, eine Berichtigung der Tabelle erreicht sie nicht | **a** (E123) |

**Technische Festlegungen zur Kenntnis (Widerspruch möglich):** Zulässigkeit wird berechnet, nie gespeichert;
`Katalog_Ausgelaufen` wird nicht benutzt. Ohne Jahr keine Prüfung; „nicht prüfbar“ ist ein Hinweis, keine Warnung.
Die Nennleistung gilt je Gerät, nicht je Anlagenzeile. Absorption ergibt keinen Befund; Split, Multisplit und VRF an
Anlagenart 13 sind in KM4 „nicht prüfbar“. Der Code‑Abgleich ist Trim und `OrdinalIgnoreCase` wie
`Bivalenzvorgaben.IstBekannt`. Regelzeilen und Kältemittelzeilen tragen `VORLAEUFIG`, bis sie am EUR‑Lex‑Volltext
geprüft sind; n.b.‑Regeln (Nr. 9 a, Buchstaben unter Nr. 9, 750 bei Sicherheitsausnahme, Sole‑/Wasser‑Wasser)
werden nicht gesät. Die Tabelle steht im Katalogregister (Stufe 3, `KMT` vor `KM`) ohne Projektkopie;
`ID_Kaeltemittel` ohne Fremdschlüsselzwang, reist nicht im Projektpaket. Die Kältemittelcodes gelten als
Normbezeichnungen (ISO 817) und werden in `WikiProduktdatenWacheTests.Normbezeichnungen` aufgenommen. Kein neues
Vorlagenfeld, keine neue Vorlagen‑Katalogfassung.

## 10 Abnahmekriterien

| Prüfung | Messbar erfüllt, wenn |
|---|---|
| Schema (`KaeltemittelSchemaTests`) | Tabelle, Register (`KMT` vor `KM`), Schlüssel nach `Schluesselstamm`, `ID_Kaeltemittel` an beiden Tabellen, `Inbetriebnahmejahr`, CHECK‑Proben, Wiederholprobe; Zuordnung: drei Projektkopien tragen die ID von `R513A` bei unverändertem Text, Auslieferungssätze 1 bis 3 tragen ihre ID mit neuer Prüfsumme (Satz 3 `R1234ze(E)`), die 34 Typkennfelder bleiben leer, ein unbekannter Text bleibt ohne ID; Spaltenzahlen 33/31, Registerzählung 52; `SqlDialektPruefer` grün |
| Regelsaat (`GesetzkatalogSaatWacheTests`, `KaeltemittelregelnTests`) | Generation 10 sät die `FGAS`‑Zeilen in den Schranken; `KlassenVorrat` und `KlasseAnzeige` kennen die Klasse; Vokabular, Oberklasse, Band und Zeitreihe je Schlüssel |
| Prüfklasse (`KaeltemittelZulaessigkeitTests`, ohne Datenbank) | Klasse 1 für 20 kW Kühler mit GWP unter 750 bei 2027; Klasse 2 für 10 kW Kühler mit GWP ≥ 150 bei 2027, Klasse 1 bei 2026; Split Luft/Wasser 10 kW trifft die Split‑Regeln der Oberklasse (2035), Split 14 kW die Regeln über 12 kW (2029/2033); Klasse 4 ohne Leistung, ohne Kältemittel, bei `SONSTIGES`, bei `Bauart` leer; ohne Jahr kein Ergebnis; „jedes F‑Gas“ nur mit `F_Gas = 1`; GWP‑Vorrang nach KM4‑Q17; treffende Regel des frühesten Stichtags; Absorption ohne Befund |
| Meldungsweg (`KaeltemittelZulaessigkeitLaufTests`, Arbeitskopie; Muster `KaeltespeicherDatenbankTests`) | 1059 mit Jahr 2027: ein `Warnbefund` mit `ID_Anlage`, eine Protokollwarnung am Laufstart, Zeile in `bericht.warnungen`; 1063: kein Befund; ohne Jahr: kein Befund, kein Hinweis; `SchemaModellTests`: Kältebahn‑Knoten trägt `Warnung` und `Warntext` |
| Wachen | zehn Codes der Klappliste ⊆ Tabellencodes; Register‑Wache der Katalogfassung; `ParameterVerwendungTests`; `TestdatenbankSchemastandWacheTests`; `KiMaskenabdeckungWacheTests`, `KiDialogkatalogTests`; `WikiProduktdatenWacheTests`; `DokumentationLinkWacheTests`; `BerichtSchreiberOhneDatenbankWacheTests` |
| Referenzlauf | alle Projekte der aktuellen Basis `GESAMT: PASS`, Projektdateien byte‑gleich, Referenzprotokolle ohne neue Zeile; keine neue Basis |
| Projektpaket (`ProjektpaketAnhebungTests`) | `ID_Kaeltemittel` reist nicht; der Import setzt sie am Ziel aus dem Text; ohne Treffer leer, Text erhalten; Projektkopie trägt die ID nach `AusKatalogUebernehmen` |
| Oberfläche (bunit) | Klappliste mit Bestandseintrag „nicht zugeordnet“, Lesezeilen mit GWP‑Herkunft und Abweichungshinweis, Schnellwahl GWP, Lesemodus der Auslieferung, Feld Inbetriebnahmejahr, Hinweiszeile der Kachel, Verwaltungsdialog mit Seitenschlüssel, Hilfeziel und Menüband; Windows‑Schale baut auf Linux |
| Gate | `dotnet test WP-Plan.Kern.slnf` grün nach dem Merge jeder Etappe |

## 11 Quellen

- [Recherche Kälteanlagen](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md) — Abschnitte
  „Kältemittel“, „Stufe 4“, „Offene Klärungen“ (dort: `Katalog_Ausgelaufen` für unzulässige Sätze — hier verworfen).
- [Konzeptprüfung Katalog und Import](2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md) — Stufe K‑A,
  Frage KKP‑Q3; [Übergabe Kälteanlagen](2026-10-10_Uebergabe_Kaelteanlagen.md) und
  [Übergabe Teil Programm](2026-10-10_Uebergabe_Kaelteanlagen_Programm.md) — Zuständigkeiten, UB‑Schnittstelle,
  Stand, Arbeitsregeln; [Entwurf Split/VRF/Rückkühlwerk](2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md) — K‑D,
  K‑E, K‑F; [Entwurf Kältebereich](../Gebaeudesimulation/2026-10-10_Entwurf_Kaeltebereich.md) — KB‑A/KB‑B, Abschnitt 4
  „Wer baut was“; [Konzept Projektdialoge und Katalogauswahl](../Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md)
  — Stufe 5 am Anlagendialog der Kältemaschine.
- [Konzept Kühlung Gebäudesimulation](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) — 6.3 und §14 (Ausschluss).
- [Konzept Emissionsarten](../Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md) — GWP₁₀₀ der Emissionsarten (AR6).
- [Umsetzungskonzept Übergabegrenze und Bivalenz](../../ueberholt/Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md) —
  Entscheid U‑2 (Kältemittel der Wärmepumpe als Klappliste im Kern).
- [Konzept Simulationsablauf](../Konzept_Simulationsablauf_EPOS-Plan.md) — Katalogspalten, `Katalog_Ausgelaufen`.
- Vorlage für Aufbau und Entscheide: [Konzept Teillast und Takten (KM3)](../../ueberholt/Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md).
- Rechtsquellen, nur genannt, nicht im Volltext geöffnet: Verordnung (EU) 2024/573 (Art. 3, 5, 6, 11, 12, 13;
  Anhänge I, II, IV, VI; drei Berichtigungen des deutschen Texts, Inhalt nicht ermittelt); Verordnung (EU) 517/2014
  (Altwerte); Durchführungsverordnungen 2024/2174, 2026/286, 2026/1975; REACH‑Beschränkungsvorschlag PFAS (ECHA,
  RAC 02.03.2026, SEAC‑Entwurf 10.03.2026); BEG EM und BEG NWG (Sekundärquellen); ISO 817, EN 378‑1, ASHRAE 34;
  IPCC AR4 und AR6. **Offene Klärung vor dem Säen:** Prüfung jeder Regel und jedes Werts am EUR‑Lex‑Volltext — der
  Anwender stellt den Text bereit (die Umgebung erreicht EUR‑Lex nicht).

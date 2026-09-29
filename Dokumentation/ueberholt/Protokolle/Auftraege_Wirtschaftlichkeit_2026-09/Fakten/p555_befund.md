# Befund: Statuszeile #555 „Strombedarf ohne Verwendung gilt je Vergleichsgruppe" gegen das Wirtschaftlichkeitskonzept

Prüfung eines Opus-Agenten der Sitzung „EPOS Plan Wirtschaftlichkeit", 29.09.2026, auf dem Stand `2907bed1a`
(Konzept, Register und Statusdatei dort unverändert gegenüber `2bd992c66`). Nur gelesen, nichts geändert; die
Testdatenbank nur lesend geöffnet (Fassungen `22b1f882` heute und `217a519b` aus der Zeit von #555). Befund ohne
Anwenderentscheid; die Vorschläge sind Empfehlungen.

## 1. Konzepttreue: kein Widerspruch, aber eine Lücke

Das konsolidierte Konzept ist die führende Fassung (Z. 64–76), das Register die eine Stelle für Entscheide (Register
Z. 9–12). Keines von beiden kennt die Regel je Stand (#433) oder die Gruppenregel. § 3.5 Z. 2034 bepreist jeden
Netzbezug ohne Ausnahme, R‑EZ endet mit EZ‑12 (Register Z. 1020). #555 hat die Regel nur ins Schwesterpapier
`Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md` Z. 244–259 geschrieben, das Szenarien behandelt, keine Energiekosten.
#555 erweitert also eine Regel, die im Konzept fehlt, und stellt für den Vergleich die Formel aus Z. 2034 wieder her.
E26/E27/E29/E30 (Z. 2887–2891) bleiben stimmig: Sie regeln Mengen und Ausweis, bepreist wird der geklemmte Netzbezug
≥ 0, ein Hilfsenergieanteil zählt als Stromverwendung. § 2.9 (Z. 722–755) und § 5 Elektrokessel (Z. 2762–2770) bleiben
unberührt.

Vorschlag je Stelle:

- **§ 3.5 nach Z. 2049:** „Führt ein Stand Netzbezug, aber keinen Erzeuger, der Strom verwendet (`BrauchtStromTraeger`),
  gehen Kosten und Emissionen dieses Netzbezugs in der Einzelbetrachtung mit 0 ein; im Vergleich bepreist und bewertet
  ihn jeder Stand, sobald ein Stand des Laufs Strom verwendet (`GruppeVerwendetStrom`) — ohne zugeordneten Stromträger
  mit dem Auslieferungsträger des Katalogs, mit Arbeits-, Grund- und Leistungspreis, Rollentarif und § 9b-Menge wie jeder
  Stand mit Stromverwendung (→ Register R‑EZ)."
- **§ 3.5 Z. 2125–2127:** „Wird der Netzbezug eines Standes nur nach der Gruppenregel bewertet, nimmt er den
  Emissionsfaktor des Trägers, der ihn bepreist; der Strommix-Rückfall greift erst, wenn dieser Träger keinen Faktor führt."
- **§ 3.8 Z. 2536–2537:** „Bemessen wird der bewertete Netzbezug — ohne Stromverwendung 0, unter der Gruppenregel der
  volle Netzbezug des Standes (`NetzbezugFuerStromsteuer`)."
- **§ 2.11.5 Z. 957–960:** „Ein Stand ohne eigene Stromverwendung zählt seinen Stromträger, ohne Zuordnung den
  Auslieferungsträger, als Träger mit Verbrauch, sobald ein anderer der gezählten Stände Strom verwendet."
- **§ 2.15 Z. 1502–1514:** „Die Gruppenregel des § 3.5 bestimmt jeder Lauf einmal über alle seine Stände (Stamm, angehakte
  Varianten, Referenz), unabhängig von Referenzwahl und Sicht; Sicht 2 rechnet A und B mit derselben Regel."
- **§ 6.2 nach Z. 2941:** „Die Gruppenregel bewegt keinen Anker: Jeder Kapitalwert-Anker rechnet einen Stand allein, und in
  der Testdatenbank verwendet jeder Stand jeder Vergleichsgruppe Strom; ihre Wache ist `StromGruppenregelTests`."
- **§ 6.5, Tafel Z. 3161–3172, neue Zeile:** „Energiekosten und Emissionen eines Standes ohne eigene Stromverwendung — die
  Einzelbetrachtung (Kostenseite, Übersicht, Berichtskapitel ‚Ergebnisse je Variante' und ‚Variantenvergleich') ohne,
  die Wirtschaftlichkeit mit bepreistem Netzbezug | benannt, nicht gekoppelt: der Hinweis `WIRT_HINWEIS_STROM_GRUPPENREGEL`
  nennt Stand, Stromverwender und Menge; den Ausweis im Bericht regelt § 6.3 Nr. 37."
- **§ 6.3 nach Nr. 36 (Z. 3105–3125), drei neue Punkte:** Nr. 37 Gruppenzahl im Bericht (Anwenderentscheid offen); Nr. 38
  Leistungspreis des Auslieferungsträgers an Ständen der Gruppenregel prüfen; Nr. 39 Stromsteuer-Kohärenz (§ 3.9
  Z. 2584–2585): bei Ständen ohne zugeordneten Stromträger schweigt die Prüfung, obwohl § 9b gebucht sein kann
  (`KohaerenzPruefung.cs` Z. 1153–1161).
- **Register R‑EZ, zwei neue Zeilen:** EZ‑13 (#433), Anwender 22.09.2026, Wortlaut „Energiekosten sollen allgemein nur
  anfallen, falls sie auch Verwendung finden", Ort § 3.5. EZ‑14 („Nach #550" (g)), ValERI-Anwenderentscheid 26.09.2026,
  „je Vergleichsgruppe statt je Stand", Ort § 3.5, umgesetzt #555.

## 2. Code (fünf Sätze)

1. Im Kern-Controller `ProjektEnergietraegerCtrl` fragt `GruppeVerwendetStrom` (Z. 548) je Stand die Anlagenkonfiguration
   ab (`BrauchtStromTraeger` Z. 479: WP, PV, SP, BHKW, Heizstab, Elektrokessel, Hilfsenergieanteil), und
   `StromTraegerImVergleich` (Z. 570) liefert den Auslieferungsträger ohne die Vorbedingung der elektrischen Welt.
2. `WirtschaftlichkeitCtrl.StromGruppenregel` (Z. 2137) bestimmt einmal je Lauf (`Berechne` Z. 1557, `BerechneVerlauf`
   Z. 1741) aus `daten.Varianten` (mindestens zwei Stände) die Stände ohne eigene Stromverwendung. `Szenariodaten`
   (Z. 2098–2121) rechnet für sie in jedem Szenario, auch in Erwartet, eine Kopie mit `StromImVergleichBepreisen`; damit
   tragen Kennzahlen, Sensitivität, Bandbreite, Verlauf und die gespeicherte Ergebniszeile die Gruppenzahl, während das
   Original je Stand bleibt.
3. `KostenEmissionRechner` lässt mit dem Merker die Auslassung fallen (Z. 599). Der Netzbezug trägt dann Arbeits-, Grund-
   und Leistungspreis des zugeordneten Trägers, sonst des Auslieferungsträgers (Z. 467, vermerkt als Rückfallträger), und
   CO₂ mit dem Faktor desselben Trägers (Z. 751–777); die Menge steht in `StromGruppenregelMWh`. Weil die Kopie keine
   „Menge ohne Verwendung" mehr führt, greifen für sie auch Rollentarif (`WirtschaftlichkeitCtrl` Z. 2287–2291) und
   § 9b-Menge (Z. 4290–4292).
4. Der Hinweis `WIRT_HINWEIS_STROM_GRUPPENREGEL` (`Resource.resx` Z. 10915, en-US Z. 10908) hängt an `erg.Hinweis`
   (Z. 6605–6613) und erscheint in Warnband, Vergleichstabelle sowie Word- und Excelbericht; `SzenarioAbdeckung` (Z. 240,
   324) zählt den Stromträger solcher Stände mit.
5. Hülle und Berichtsgeneratoren sind unberührt: Der Commit enthält außer Dokumentation nur `EPOS.Kern` und
   `EPOS.Kern.Tests`; `git grep` findet in `EPOS.UI.Daten` und `EPOS.UI` nichts. Der Bericht zeigt die Gruppenzahl nur im
   Kapitel Wirtschaftlichkeit (`WirtschaftlichkeitZeilen.cs` Z. 450); die Kennzahlgruppen „Kosten" und „Emissionen"
   (`KennzahlenKatalog.cs` Z. 491–506, `Berichtstabellen.cs` Z. 51) lesen das Original.

## 3. Anker

- `e2592cb4b` und `0a07ea2e1` ändern dieselben zehn Dateien; keine davon ist eine Anker-Testdatei oder Messlatte.
- Die Testdatenbank enthält vier Gruppen mit Varianten: 1018/1031 (BHKW), 1019/1023/1024 (WP, Heizstab), 1026/1027/1029
  (WP), 1042/1044 (WP). 1030 steht allein (BHKW-Kaskade). Alle 15 Basisprojekte verwenden Strom, in `217a519b` ebenso.
  Es gibt keinen Stand, an dem die Regel greift; nur der Prüfstand in `StromGruppenregelTests.cs` Z. 221 baut einen
  (WP aus 1027 entfernt).
- Die ValERI-Gruppe 1071–1073 steht in keiner der beiden Fassungen (höchste ID 1048 bzw. 1049). Die Angabe
  „Testdatenbank" in Statuszeile #555 und im Protokoll VG1 ist falsch; die Gruppe liegt offenbar in der Datenbank des
  Anwenders.
- Die grünen Gates belegen die Anker als unverändert, aber strukturell, nicht als Probe der Regel: Jeder Anker rechnet
  einen einzelnen Stand (`WirtschaftlichkeitAnkerTests.cs` Z. 130–148, `KapitalwertAnkerZerlegungTests.cs` Z. 61–94,
  `PvAusweisStromMatrixTests.cs` Z. 226–229), und bei weniger als zwei Ständen liefert die Regel nichts
  (`WirtschaftlichkeitCtrl.cs` Z. 2140). Die Messlatte 1030 ist ein einzelner Stand; die Messlatte „Gruppe" nutzt
  synthetische Kennungen ab 9101 ohne Anlagen (`Berichtsdatenproben.cs` Z. 67–104), also ohne Stromverwender.
- Der Referenzlauf (R21 bei #555, R23 danach) vergleicht nur die CSV der Simulation, keine Wirtschaftlichkeit; er belegt
  allein, dass der Simulationskern unberührt ist.

## 4. Bericht

Die „Gruppenzahl" sind Energiekosten und CO₂ eines Standes ohne eigene Stromverwendung nach der Gruppenregel, also
Brennstoff plus bepreister Netzbezug (`EnergiekostenJahr` der Kopie). Nur sie geht in den Kapitalwert ein (Konzept § 1.1
Z. 113). Die „Einzelzahl" ist `Energiekosten`/`StromkostenNetz`/`CO2Gesamt` des Originals ohne Netzbezug. Am Prüfstand:
50 €/a gegen 50 + 16,12 MWh × 0,35 €/kWh = 5.692 €/a. Heute druckt derselbe Bericht im „Variantenvergleich" die
Einzelzahl und im Kapitel Wirtschaftlichkeit die Gruppenzahl; der Hinweis erklärt den Unterschied nur im Kapitel
Wirtschaftlichkeit. Ein Kostenkapitel regelt das Konzept nicht; „Endenergiekosten"/„Betriebskosten" (§ 3.4 Z. 1882–1891,
Tabelle Z. 1976–2002) betreffen Bezugsgrößen und die Betriebskostentabelle, die Gruppenregel ändert beide nicht. Die
Gruppenzahl passt zu § 1.1 und zu „keine dritte Wahrheit" (§ 2.9 Z. 745, § 2.15 Z. 1536–1541). Offen ist die zweite Zahl
im selben Bericht: § 6.5 Z. 3159 verlangt, jede solche Doppelung zu benennen und zu begründen.

## 5. Empfehlung: (b) und (c)

- (b) Konzeptsätze sind nötig (Abschnitt 1). Pflicht: § 3.5, § 6.5, § 6.3 Nr. 37–39 sowie EZ‑13 und EZ‑14; § 3.8, § 2.11.5,
  § 2.15 und § 6.2 schärfen nach. Der Absatz im VALERI-Papier sollte auf § 3.5 verweisen.
- (c) Zwei Rückfragen an den Anwender:
  1. „Nach #555" (b): Soll der Bericht die Gruppenzahl ausweisen? Empfehlung: Einzelzahl in „Ergebnisse je Variante"
     belassen, im „Variantenvergleich" (Kosten und Emissionen) eine Fußzeile mit Gruppenzahl und Menge ergänzen, wo die
     Regel gewirkt hat.
  2. Geltungsmenge: Die Regel gilt je Lauf über Stamm, angehakte Varianten und Referenz (`BerichtsDatenSammler.cs`
     Z. 436 ff., `WirtschaftlichkeitSeite.razor` Z. 2093–2095), nicht über die ganze Gruppe aus `Tab_Variante`. Wer die
     einzige Stromvariante abhakt, ändert damit Energiekosten und Kapitalwert des Stamms. Die Szenarioabdeckung der Seite
     zählt dagegen über die ganze Gruppe (`WirtschaftlichkeitSeiteGaben.cs` Z. 585–601). Empfehlung: „je Lauf" bestätigen
     und so ins Konzept schreiben, die Abdeckung angleichen oder den Unterschied benennen.

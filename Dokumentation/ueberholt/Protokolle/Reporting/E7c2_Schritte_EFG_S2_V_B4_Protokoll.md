# E7c2 — Schritte E, F, G, Sperre S‑2, EEG V‑1/V‑2, B‑4 Rest und die Reste aus E7c1 (Protokoll, 23.09.2026)

Statuszeile #446 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E7 (Teil c2) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E7, § 6 Schritte E, F und G); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.2, § 2.5, § 2.13 (3), § 3.1, § 3.4, § 3.5, § 3.6, § 3.7, § 3.9, § 4, § 5 und § 6.3 Nr. 9h; die Entscheide im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
unter R‑A (A3, A4, A6, A9), R‑D (ET‑D‑3, U‑1), R‑Q (Q2) und R‑E7c1 (E7c1‑Q1, E7c1‑Q2 Lesart b, E7c1‑Q7), die acht
Fragen dieser Etappe unter R‑E7c2. Anlass: der Anwender, „fahre fort" und „fahre fort auf diesem account"
(23.09.2026) — Etappe E7, Teil c2, nach den Entscheiden E7c1‑Q1 bis E7c1‑Q8. Zweig `e7c2` von `4971556a` (`origin`,
Schemastand 105), zwei Phasen; Phase 1: `e924834d` (E7c2/1), `66620b70` (E7c2/2), `8854ba56` (E7c2/3), `657abb4a`
(E7c2/4), `4354b009` (E7c2/5), `2f0fc21f` (E7c2/6), `a339a633` (E7c2/7), `3b6f54fe` (E7c2/8), `dcc875ac` (E7c2/9a),
`ed4b3395` (E7c2/9b), `bd866b4c` (E7c2/9c); nach den Entscheiden `af450ed6` (E7c2/10, E7c2‑Q4); Phase 2: Merge
`c3eb2cfe` (Arbeitszweig `13fff671` mit den Schemaschritten 106 und 107, dabei die Umnummerierung 107/108/109 →
108/109/110), `84b1effd` (E7c2/11), `b943f534` (E7c2/12, Testdatenbank, vom Orchestrator committet). Merge
`51c49577` auf dem Hilfszweig `pm2` (Basis `origin/ios_migration_september` = `13fff671`; 60 Dateien, +6 022/−232;
der Baum gleicht `b943f534`). Opus 5.5 im Worktree `.claude/worktrees/e7c2`. Die Commit-Betreffs E7c2/1 bis
E7c2/10 und der Phase‑1-Bericht nennen die Schrittnummern vor der Umnummerierung (107, 108, 109); dieses Protokoll
nennt die gültigen (108, 109, 110).

## Befund vor der Welle

- **S‑2 (A3) — die Mischlage war nur ein Hinweis.** Standen im Projekt § 53 / § 53a Abs. 5 und § 54 EnergieStG
  nebeneinander, rechnete `SteuerGutschriftRechner` beide Entlastungen, und `KohaerenzPruefung.MischlageEnergiesteuer`
  meldete den Fall 5 als Hinweis (`KOH_FALL5_MISCHLAGE`). A3 (20.09.2026, ausdrücklich bestätigt): Sperre mit
  Begründungszeile — solange R‑U1 offen ist, ist die Kombination nie zulässig.
- **V‑1 und V‑2 (A4) — Rundung und Satz der EEG-Vergütung.** Der EV-Mix wurde über dem schon gerundeten AW-Mix ein
  zweites Mal auf zwei Stellen gerundet, der Erlös blieb ungerundet; § 51a bewertete die Ausfallarbeit auch bei
  fester Vergütung mit dem anzulegenden Wert. A4: bei fester Vergütung mit der Einspeisevergütung, eine Zeile,
  eigener Testfall; der Weg war mit `PvErloesRechnerEegTests` gepinnt (#380).
- **B‑4 — zwei Prozentarten nie frisch.** `PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN` nahmen ihre
  Bezugsgröße allein aus der gepflegten Menge (`Tab_ProjektWerte.Menge`, Konserve); `EUR_PRO_H` und die
  `EUR_PRO_KWH_*` sind seit FX2 frisch.
- **Schritt E (A6, U39) — Ersatz und Restwert hingen aneinander.** Einziger Schalter war die Nutzungsdauer: n ≥ 1
  schaltete Ersatz und Restwert zugleich ein, n < 1 beide aus. A6: ein Kennzeichen je Position, nullbar, NULL = wie
  bisher.
- **Schritt F (ET‑D‑3 Rest, U32) — der Kartenzustand hing an der Regelkennung.** Die Preisbasis der Trägerkarte
  ging als `ID_Umrechnung` mit; führte der Brennstoff keine Regel nach kWh, wurde −1 abgelegt, und die Wahl kWh fiel
  beim nächsten Öffnen auf die Abrechnungseinheit zurück.
- **Schritt G (U‑1, A9) — der Einheitenbruch der Gase.** `Tab_Brennstoff_Stamm.Einheit` führte bei den fünf Gasen
  (Brennstoffe 1, 2, 3, 14, 25) „m³", `energy_carrier.billing_unit` seit Schritt 26a „Nm³"; die
  Identitätsregel-Ableitung lieferte deshalb −1. A9: als DML-Schritt vor dem nächsten Vorlagenbau.
- **Die Reste aus E7c1:** E7c1‑Q1 (Rundungsgrund in der Herleitung), E7c1‑Q2 Lesart b (Vollbenutzungsstunden aus
  dem KWK-Strom), E7c1‑Q7 (Rest der Überlagerung „Sätze und Herkunft", KI-Feldkatalog, Berichtsspalten zu Fall 2).
- **Im Bestand:** Kein Basisprojekt trägt eine Mischlage, das Kennzeichen „Vorrichtung zur Abwärmeabfuhr" oder den
  PV-Vergütungsdialog; die einzige Zeile der zwei Prozentarten (1018) trägt keinen Satz; 15 Projekte der
  Testdatenbank nutzen die fünf Gase über ihre Träger, dazu Gerätezeilen in BHKW und Kessel.

## Gebaut — Phase 1 (E7c2/1 bis E7c2/9c)

- **Schemaschritt 108 — Ersatz und Restwert je Position (E7c2/1, Schritt E).**
  `SchemaKatalog.Schritt108_ErsatzRestwertKennzeichen` legt die nullbaren Kennzeichen `ErsatzFuehren` und
  `RestwertAnsetzen` (`CHECK (… IN (0,1))`) an `Tab_ProjektWerte` und `Tab_KostenVorlagePosition` an — vier Spalten,
  reines DDL, wiederholbar, ergebnisneutral (`SCHRITT_108_ERSATZ_RESTWERT_KENNZEICHEN`). `KapitalwertRechner`:
  „Ersatz nein" streicht die Kette der Ersatzbeschaffungen (die letzte Beschaffung bleibt die erste), „Restwert nein"
  den Restwert — entkoppelt, eine nicht ersetzte Position trägt ihren Restwert aus der ersten Beschaffung weiter;
  leer und „ja" rechnen Zeichen für Zeichen wie vorher. Ein Lese- und Schreibweg im Kern
  (`ErsatzRestwertKennzeichen`), gelesen in `InvestKaskade` und in Vorlagen- und Projektzeilen, mitgetragen bei
  Vorlagenübernahme, Projektkopie und „Speichern unter". Der Zeileneditor „Position bearbeiten" der Kostenverwaltung
  (Projekt und Vorlage) führt auf der Investitionsseite, sofern die Datenbank die Spalten führt, zwei
  Dreiwerte-Klapplisten „Ersatzbeschaffung führen:" und „Restwert ansetzen:" (leer — wie bisher · ja · nein) mit einer
  Herleitungszeile; die Tafel „Ersatz und Restwert" nennt eine Abwahl als Grund („— nein (Kennzeichen der
  Position)"); der KI-Feldkatalog der Vorlagenposition kennt `ersatz_fuehren` und `restwert_ansetzen`. Tests
  `ErsatzRestwertKennzeichenTests`, bunit `VorlagenPositionDialogTests`.
- **Schemaschritt 109 — die Preisbasis als eigener Kartenzustand (E7c2/2, Schritt F).**
  `SchemaKatalog.Schritt109_Preisbasis` legt die nullbare Textspalte `Preisbasis` an `energy_project_settings` an;
  der einmalige Datenteil (`PreisbasisUebernahme`) setzt „kWh", wo `ID_Umrechnung` eine Regel nach kWh trägt, sonst
  die Abrechnungseinheit des Trägers — genau die Basis, die die Karte bis dahin beim Öffnen zeigte
  (`SCHRITT_109_PREISBASIS`). `EnergietraegerPreisCtrl` liest und schreibt die Spalte, die Karte nimmt ihre Basis aus
  ihr; der Rückfall auf −1 entfällt für die Karte, `ID_Umrechnung` bleibt die Regel der Einheitenprüfung. Fehlt die
  Spalte (Datenbank vor 109), zeigt die Karte die Abrechnungseinheit und nennt den Grund
  (`ETV_PREISBASIS_OHNE_SPALTE`), statt still zurückzufallen; die Versionskopie trägt die Basis mit. Neue Zuordnungen
  aus Wizard, Katalog und Variantenträger schreiben keine Preisbasis — leer heißt Abrechnungseinheit (E7c2‑Q3). Test
  `PreisbasisSchrittTests`, dazu `EnergietraegerHuelleTests`.
- **Schemaschritt 110 — der Stammtext der Gase (E7c2/3, Schritt G; erweitert mit E7c2/10).** `GaseNormkubikmeter`,
  reines DML nach dem Muster von Schritt 26a (`SCHRITT_110_GASE_NM3`): `Tab_Brennstoff_Stamm.Einheit` „m³" → „Nm³"
  und `PreisEinheit` → „€/Nm³" an den Brennstoffen 1, 2, 3, 14 und 25 (Stadtgas, Erdgas LL, Erdgas E, Biogas,
  Wasserstoff), dazu jede Preiszeile ihrer Träger, die noch „m³" führt (in der Testdatenbank eine: Projekt 1039,
  Erdgas E). Vorher gemessen: Eine Umrechnung hängt allein bei der nächsten Zuordnung am Stammtext
  (Identitätsregel, vorher −1, danach Nm³ → Nm³); kein Rechenweg liest ihn. Der Vorlagenbau auf der migrierten
  Kopie läuft ohne Auffälligkeit, die Vorlage trägt Nm³. Test `GaseNormkubikmeterTests`.
- **S‑2 — die Mischlage ist gesperrt (E7c2/4, A3).** `SteuerGutschriftRechner.Mischlage` ist eine Prüfung für Sperre
  und Kohärenzzeile: angesetzt an der wirksamen Wahl je Anlage mit Brennstoffeinsatz (Anlagenwert, sonst
  Projektwert); auf der § 53-Seite zählt nur eine Anlage mit Stromerzeugung — ein Kessel mit § 53-Wahl rechnet ohnehin
  0 und begründet keine zweite Entlastungswelt (E7c2‑Q1). Stehen beide Seiten, wird der § 54-Betrag verworfen (0 €;
  der Sockel entfällt, die § 54-Posten verlassen den Nachweis), die § 54-Zeile trägt die Begründung
  (`STEUER_ENERGIEST_54_MISCHLAGE`), und die Kohärenzprüfung meldet eine Warnung (`KOH_FALL5_MISCHLAGE_SPERRE`) an
  Stelle des gestrichenen Hinweises `KOH_FALL5_MISCHLAGE`; der § 53-Teil bleibt. Ohne Mischlage ist der Rechenweg
  der von vorher. Tests `MischlageSperreTests`, `SteuerGutschriftRechnerTests` (Zahlenprobe U7 gesperrt, der Nachweis
  der zwei Beträge je eigenem Projekt).
- **B‑4 Rest (E7c2/5).** `PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN` holen ihre Bezugsgröße frisch aus dem
  jüngsten Lauf (`IstProjektkostenArt`, `RueckfallMenge`): die projektweiten Brennstoffkosten — Σ Verbrauch ×
  Arbeitspreis aller Brennstoffmodule (BHKW und Brennstoffkessel; Elektrokessel bleiben in der Stromwelt), derselbe
  Weg wie Weg A — bzw. die Stromkosten — Netzbezug × Arbeitspreis des Projekt-Stromträgers. Bezugsgröße sind die
  Arbeitskosten ohne Grund- und Leistungspreis (E7c2‑Q2). Die Konserve gilt nur, wo frisch nichts ermittelbar ist;
  der Grund nennt dann Lauf, Menge oder Preis (`EndenergieAufloeser.GrundOhneProjektkosten`) statt „Konserve". Tests
  `ProjektkostenArtenTests`, `BetriebskostenBaugroesseTests` (Grund KONSERVE → LAUF).
- **V‑1 und V‑2 (E7c2/6, A4).** V‑1: `PvErloesRechner` rechnet den EV-Mix ungerundet — der ungerundete Mix des
  anzulegenden Werts (bzw. der Override) abzüglich des Abschlags —, gerundet wird allein der Erlös, auf Cent (neues
  Feld `PvErloesErgebnis.EvCt`; die Herleitung nennt den ungerundeten Satz). V‑2: Fährt die Anlage feste Vergütung,
  bewertet § 51a die Ausfallarbeit mit der Einspeisevergütung, in der Direktvermarktung weiter mit dem anzulegenden
  Wert; der § 51a-Betrag bleibt ungerundet (E7c2‑Q6). `PvErloesRechnerEegTests` ist neu gepinnt, alt und neu im
  Kommentar, mit eigenem Testfall für V‑2; die Pinnung der Marktprämie (#380) bleibt alt = neu. Der Satz der
  Speicherbewertung (`VpvCtKwh`) nimmt weiter den gerundeten Mix (E7c2‑Q5, Lesart b folgt mit E7c3).
- **E7c1‑Q2 Lesart b — Vollbenutzungsstunden aus dem KWK-Strom (E7c2/7).** Trägt eine Anlage das Kennzeichen und ist
  σ bestimmbar, zählen ihre Vollbenutzungsstunden aus dem KWK-Strom (Vbh = KWK-Strom ÷ P_el) statt aus dem ganzen
  Modulstrom; Kontingentverbrauch und Jahresdeckel laufen über diese Stunden. Auf dem Ersatzweg ebenso für die
  Gesamtanlage (KWK-Strom der Gesamtanlage ÷ Σ P_el). Je eine Hinweiszeile (`WIRT_KWKG_FALL2_VBH`,
  `WIRT_KWKG_FALL2_VBH_ERSATZ`); der Nachweis führt die Stunden in `VbhElektrisch`. Bindet der Jahresdeckel, entfällt
  die Kürzung des Falls 2 bis auf den Mischsatz (Zuschlag = Deckel × P_el × Satz, Reihe 12 Jahre; E7c2‑Q7). Ohne
  Kennzeichen (Fall 1) bleibt alles, wie es war. `KwkgFall2Tests` auf die neuen Werte, alt und neu im Kommentar.
- **E7c1‑Q1 — der Rundungsgrund (E7c2/8).** Die Formel des zweiten Falls rechnet weiter ohne Toleranz; eine Kürzung
  unter 0,01 MWh bleibt stehen, und die Herleitung nennt ihren Grund — die Rundung von σ bzw. der Mengen auf
  0,01 MWh (`WIRT_KWKG_FALL2_RUNDUNG` als eigener Satz auf dem Regelweg, `WIRT_KWKG_FALL2_RUNDUNG_KURZ` als Klammer je
  Anlage auf dem Ersatzweg). Reiner Text, keine Zahl.
- **E7c1‑Q7 — KI-Feldkatalog und Berichtsspalten (E7c2/9a).** Der KI-Feldkatalog der Maske BHKW-Wirtschaftlichkeit
  kennt `anlage_abwaermeabfuhr` (Wahrheitswert) und `anlage_stromkennzahl` (Zahl, leer erlaubt; ≤ 0 = kein eigener
  Wert), gebunden über `BhkwWirtschaftlichkeitKiSicht` an den Arbeitsstand der gewählten Anlage; neu sind allein die
  zwei Erklärungen `KI_DLG_BHW_ABWAERME_ERL` und `KI_DLG_BHW_SIGMA_ERL`. Word- und Excel-Modultafel: Rechnet ein Modul
  Fall 2, kommen fünf Spalten dazu — Fall, Stromkennzahl σ, Nutzwärme, KWK-Strom, Kürzung (`WIRT_KWKG_SP_*`; Kopf,
  Werte und Stellenzahl einmal in `KwkgFall2Spalten`); Fall 1 neben Fall 2 zeigt die Nettostromerzeugung als
  KWK-Strom; ohne Kennzeichen bleibt die Tafel bei elf Spalten, und die Blattwache mit zehn Word-Tabellen hält; Excel
  bleibt numerisch (leere Zelle statt Strich). Test `KwkgFall2SpaltenTests`.
- **E7c1‑Q7 — der Rest der Überlagerung „Sätze und Herkunft" (E7c2/9b, Mockup U22).** Die Überlagerung trägt alle
  Größen des Mockups: in der Gruppe „KWK-Zuschlag — diese Anlage" die Anlagenart (§ 8, Wirkung je Wahl = das
  Kontingent aus `KwkgKontingentRechner`), den Tatbestand des § 6 Abs. 3 (Wirkung = der Eigenstromsatz aus
  `KwkgSatzRechner`), Fall 1 / Fall 2 mit der Stromkennzahl, die Satztafel Einspeisung · Eigenstrom · Vbh-Kontingent
  · Jahresdeckel mit Vorschlag, Herkunft, eigenem Wert (leer = Vorschlag) und „gilt" und die Zeile „Wirkung Jahr 1";
  die Gruppe Energiesteuer (Geltung „Projektvorgabe für alle Anlagen" oder „nur diese Anlage", Entlastung, Aufteilung,
  Herkunft und die Positionen der Anlage aus dem Lauf); die Gruppe Stromsteuer des Projekts (Unternehmensart,
  Hocheffizienz, räumlicher Zusammenhang, Modus, Entlastung und Befreiung aus dem Lauf). Der zweite Knopf „Wahl und
  Herkunft…" steht in der Gruppe Energiesteuer. Der Zwischenstand sind Kopien von Anlagen- und Projektstand;
  „Übernehmen" legt nur Geändertes auf den Arbeitsstand, geschrieben wird im OK-Weg. Kern: Der Jahresbetrag des
  KWK-Zuschlags steht einmal in `KwkgJahresbetrag` (aus dem Schleifenrumpf beider KWKG-Reihen ausgelagert); Lauf
  und „Wirkung Jahr 1" rufen denselben Ausdruck, die Ergebnisse sind bitgleich. 44 Schlüssel `BHW_UEB_*`; Tests
  `KwkgJahresbetragTests` (sieben, samt Quellwache), bunit `BhkwSaetzeHerkunftTests` (acht Fälle mehr); zwei
  E7c1-Tests sind auf die jeweilige Wahlgruppe umgestellt.
- **Ankertests (E7c2/9c).** Kein Anker bewegt sich: 1024 −2.896.359,13 €, 1030 −21.895.377,28 € (auf der migrierten
  Kopie gemessen); Kopfabsatz und Zeilenkommentare an beiden Kapitalwertankern halten alt = neu fest.

## Nach den Entscheiden: E7c2/10 (E7c2‑Q4)

Der Anwender hat E7c2‑Q4 am 23.09.2026 abweichend von der Empfehlung entschieden: Brennstoff 24 „Sonstige" in kWh
statt m³. Schritt 110 zieht deshalb auch dessen Stammtext — Einheit „m³" → „kWh", Preiseinheit → „€/kWh", wie bei
Strom (13) und Fernwärme (23). Vorher gemessen: Weder die Testdatenbank noch die Anwenderdatenbank dieses Rechners
führt einen Träger, eine Preiszeile, eine Projektzuordnung oder eine Umrechnungsregel des Brennstoffs 24; sein Stamm
trägt H_i = H_s = 0 — also reiner Einheitentext, keine Preisumrechnung (ohne Heizwert auch nicht möglich). Träger,
Preiszeilen und Projektzuordnungen des Brennstoffs in anderen Datenbanken bleiben unverändert und werden im
Migrationsprotokoll gezählt; die Nachprobe zählt den Brennstoff 24 mit. A/B: Die Migration von 105 aus meldet
„Brennstoff 24: 1 Einheit, 1 Preiseinheit; genutzt von 0/0/0", die dreizehn Basisprojekte 9.195 von 9.195 Werten
gleich, der Vorlagenbau ohne Auffälligkeit. `GaseNormkubikmeterTests` um den Brennstoff 24 und einen Fall mit Nutzer
(Träger und Preis bleiben) ergänzt.

## Phase 2 (Merge `c3eb2cfe`, E7c2/11, E7c2/12)

- **Merge `c3eb2cfe`** holt den Arbeitszweig `13fff671`: Schemaschritt 106 der Welle #444 (fremde Ergebnisverweise der
  Wirtschaftlichkeit werden NULL), Schritt 107 der Gebäudesimulation (Entscheid E30, die Ergebnistabelle je Gebäude),
  die Administrationsdialoge Stufe 2 (#445), die Papiere des Zapfprofilgenerators; die Testdatenbank auf 107 (LFS
  `36e693ad…`). Weil `origin` 106 und 107 belegt hatte, sind die drei Schritte dieser Etappe umnummeriert, vom
  Koordinator bestätigt: E 107 → **108**, F 108 → **109**, G 109 → **110**; `SchemaStand.Zielversion` 110, die Kette
  105 → 106 → 107 → 108 → 109 → 110 an allen vier Stellen (`SchemaStand`, `SchemaMigration`, `TestDatenbank`,
  `Werkzeuge/Testdatenbankschema`); `SchemaKatalog.Schritt108_ErsatzRestwertKennzeichen`, `Schritt109_Preisbasis`,
  die Migrationstests, Nachproben und Kommentare sind nachgezogen, `ETV_PREISBASIS_OHNE_SPALTE` nennt „vor 109".
  Die Konflikte sind inhaltlich aufgelöst — `SchemaStand`, `SchemaMigration` (Konstanten, Schrittliste, Methoden in
  der Folge 106 bis 110), `TestDatenbank`, `Werkzeuge/Testdatenbankschema` (zuerst 106 und 107 aus `origin`, dann
  E7c2) und die zwei Ressourcendateien (beide Seiten, je 8 003 Einträge ohne Doppel); der Designer ist neu erzeugt
  und im Zweitlauf unverändert. Kern-Filter und Windows-Schale 0 Fehler.
- **E7c2/11 `84b1effd` — Testfix.** Der Fall „Brennstoff 24 wandert nur im Stammtext" legte seinen Probeträger mit
  `new DbParam("@a", 0)` an; die Konstante 0 wählt in C# den Konstruktor `DbParam(string, DbParamTyp)`, der Wert
  blieb NULL, das INSERT scheiterte an NOT NULL, und die Zählung fand 0 Träger. Jetzt `(object)0`; die Klasse läuft
  4/4.
- **E7c2/12 `b943f534` — die Testdatenbank auf Schemastand 110** (vom Orchestrator committet):
  `Werkzeuge/Testdatenbankschema` zieht die Repo-Testdatenbank von 107 auf 110 — 108 legt die vier Kennzeichenspalten
  an; 109 legt `Preisbasis` an und füllt sie (5 Zeilen „kWh" über die Regel, 23 mit der Abrechnungseinheit); 110
  setzt Einheit und Preiseinheit der fünf Gase auf Nm³ und des Brennstoffs 24 auf kWh, dazu die eine Preiszeile.
  `Tab_Applikation` trägt 110, `quick_check` ok, die 1030-Anlagen behalten die Anlagenart NEUANLAGE; LFS-Zeiger,
  SHA-256 `8225443a…`, 67 739 648 Byte. Der Nachtrag „Schemastand 110" in `Referenzlaeufe/LIESMICH.md` steht mit den
  Papieren zu #446.

## Fragen aus der Etappe

Zwischen- und Phase‑1-Bericht nannten acht Fragen; sie stehen im Entscheidungsregister als **R‑E7c2**. Der Anwender
hat alle acht am 23.09.2026 entschieden, im Wortlaut: „E7c2: 4. Brennstoff: kWh statt m³ — alles andere:
Empfehlung".

| Frage | Stand |
|---|---|
| **E7c2‑Q1** — S‑2, die § 53-Seite: (a) nur Anlagen mit Stromerzeugung zählen — ein Kessel mit § 53-Wahl löst keine Sperre aus, der alte Fall‑5-Hinweis entfällt dort; (b) jede § 53/§ 53a-Wahl sperrt wie im alten Fall 5. Empfehlung a | entschieden 23.09.2026: a (nach Empfehlung) — gebaut E7c2/4 |
| **E7c2‑Q2** — B‑4, die Bezugsgröße: (a) die Arbeitskosten wie Weg A (Menge × Arbeitspreis; Strom = Netzbezug × Arbeitspreis); (b) die Gesamtkosten samt Grund- und Leistungspreis. Empfehlung a. Hinweis zu 1030: Der gespeicherte Lauf stammt von vor B‑1, der Kessel führt keinen Verbrauch — die Basis nimmt den aus der Wärme abgeleiteten Brennstoff (#363, wie Weg A) und liegt über dem Brennstoffanteil der gebuchten Energiekosten | entschieden: a (nach Empfehlung) — gebaut E7c2/5 |
| **E7c2‑Q3** — F: Neue Zuordnungen aus Wizard, Katalog und Variantenträger schreiben keine Preisbasis; NULL heißt Abrechnungseinheit (Vorgabe, ohne Herleitungszeile), die Herleitungszeile erscheint nur bei fehlender Spalte. Empfehlung: so lassen | entschieden: so lassen (nach Empfehlung) — gebaut E7c2/2 |
| **E7c2‑Q4** — G: Brennstoff 24 „Sonstige" bleibt m³ (der Entscheid U‑1 nennt die fünf Gase). Empfehlung: so lassen | entschieden abweichend (Anwender): kWh statt m³ — gebaut E7c2/10 |
| **E7c2‑Q5** — V‑1: Der Satz der Speicherbewertung (`VpvCtKwh`) nimmt weiter den gerundeten Mix; (b) auch dort ungerundet (bewegt keine Basis). Empfehlung b, klein, in E7c3 | entschieden: b (nach Empfehlung) — Bau in E7c3, gebaut ist a |
| **E7c2‑Q6** — V‑1: (a) Der § 51a-Betrag bleibt ungerundet, nur der EV-Erlös wird gerundet; (b) auch § 51a auf Cent. Empfehlung a | entschieden: a (nach Empfehlung) — gebaut E7c2/6 |
| **E7c2‑Q7** — E7c1‑Q2 b bei bindendem Jahresdeckel: (a) die Kürzung des Falls 2 verschwindet (Zuschlag = Deckel × P_el × Satz, Reihe 12 Jahre); (b) den Deckel auf die Bruttostunden beziehen. Empfehlung a (Wortlaut des Entscheids E7c1‑Q2) | entschieden: a (nach Empfehlung) — gebaut E7c2/7 |
| **E7c2‑Q8** — Überlagerung, Energiesteuer: (a) Satz und Betrag nur für die gebuchte Wahl, die übrigen Wahlen als Text; (b) eine Vorschau je Wahl im Kern. Empfehlung b, in E7c3 | entschieden: b (nach Empfehlung) — Bau in E7c3, gebaut ist a (E7c2/9b) |

## A/B-Nachweis

Gemessen an Kopien der Testdatenbank im Scratchpad des Agenten: vorher der Basiscode auf der Datenbank mit Schemastand
105, nachher der neue Code auf der je Punkt migrierten Kopie; nach jedem Punkt die dreizehn Basisprojekte.

**Die dreizehn Basisprojekte:**

| Projekt | Größe | vorher | nachher | Grund |
|---|---|---|---|---|
| alle dreizehn | Anker, frischer Lauf, KWKG- und Ersatzreihen, Energiekosten, Emissionen — nach jedem der Punkte E7c2/1 bis E7c2/10 | — | 9.195 von 9.195 Werten gleich | kein Projekt trägt ein Kennzeichen, eine Mischlage, den PV-Vergütungsdialog oder einen Satz der zwei Prozentarten; die Preisbasis ist eine Eingabehilfe; den Stammtext der Gase rechnet niemand |
| 1024 | Kapitalwert (Anker) | −2.896.359,13 € | gleich | — |
| 1030 | Kapitalwert (Anker) | −21.895.377,28 € | gleich | — |

**Proben** (Kapitalwert bzw. Betrag; die V‑Proben mit fester Vergütung, § 51 mit 20 %, § 51a an):

| Punkt | Probe | vorher | nachher | Grund |
|---|---|---|---|---|
| E (108) | 1024 mit Nutzungsdauern (Wärmepumpe 15 a, Kessel 25 a), ohne Kennzeichen | −2.897.442,20 € | — | Ausgangslage der Probe |
| E | Wärmepumpe „Ersatz nein" | −2.897.442,20 € | −2.895.805,46 € | keine Ersatzbeschaffung |
| E | Kessel „Restwert nein" | −2.897.442,20 € | −2.897.995,88 € | kein Restwert |
| E | beide Positionen, Ersatz und Restwert „nein" | −2.897.442,20 € | −2.896.359,13 € | gleich dem Anker (ohne Dauer weder Ersatz noch Restwert) |
| E | „ja" | −2.897.442,20 € | gleich | „ja" rechnet wie leer |
| F (109) | Migration der Testdatenbank | — | 5 Zeilen „kWh" (Regel nach kWh), 23 Abrechnungseinheit | die Basis, die die Karte beim Öffnen zeigte |
| F | 1030 (Erdgas E, Regel 67 nach kWh) und 1024 (Stadtgas ohne Regel) mit Basis kWh: Preise und Kosten | — | gleich | Eingabehilfe; gerechnet wird der Basiswert je Abrechnungseinheit |
| F | U32: Öffnen, Speichern, Öffnen (Stadtgas ohne Regel nach kWh, Basis kWh) | Abrechnungseinheit | kWh | eigene Spalte statt Regelkennung |
| G (110) | Stammtext der Testdatenbank | „m³" | 5 Einheiten, 5 Preiseinheiten und 1 Preiszeile (1039) auf Nm³; Brennstoff 24 auf kWh | Energiekosten, Emissionen und frischer Lauf der Basisprojekte gleich; Vorlagenbau ohne Auffälligkeit |
| S‑2 | 1030 (produzierendes Gewerbe, BHKW § 53, Kessel § 54): § 54 Jahr 1 | 7.987,41 € | 0 € | Sperre; Kohärenzzeile Warnung |
| S‑2 | ebenso, Kapitalwert | −20.388.846,98 € | −20.507.679,50 € | — |
| S‑2 | Zahlenprobe U7 (§ 53a und § 54 im selben Projekt, `SteuerGutschriftRechnerTests`) | 24.088,43 €/a | 21.202,71 €/a | der § 54-Betrag 2.885,72 € ist verworfen |
| B‑4 | 1030, eine Zeile „% der Brennstoffkosten" 3 %, Konserve 10.000 € | 300,00 €/a · −21.900.695,28 € | 15.483,29 €/a · −22.169.844,81 € | Bezugsgröße 516.109,60 €/a aus dem Lauf |
| B‑4 | dieselbe Zeile als „% der Stromkosten" | 300,00 €/a · −21.900.695,28 € | 32.683,35 €/a · −22.474.745,07 € | Netzbezug × Arbeitspreis |
| V‑1 | 1040, Erlös Jahr 1 | 139,832 € | 139,83 € | Erlös auf Cent |
| V‑2 | 1040, § 51a | 18,39 € | 17,48 € | Einspeisevergütung statt anzulegender Wert |
| V‑2 | 1045, § 51a | 6,48 € | 6,16 € | ebenso |
| V‑1/V‑2 | 1046, Erlös Jahr 1 · § 51a | 54,824 € · 7,21 € | 54,82 € · 6,85 € | ebenso; der Kapitalwert ist dort nicht bestimmbar (Fehlgrund des Bestands) |
| V‑1/V‑2 | Rechner, 100 kWp: Jahr 1 · § 51a | 3.207,96 € · 427,60 € | 3.209,02 € · 401,13 € | ungerundeter EV-Mix; Einspeisevergütung statt anzulegender Wert |
| E7c1‑Q2 b | 1030, σ gepflegt 0,5: Vollbenutzungsstunden | 7.475,69 h/a | 6.055,2 h/a | aus dem KWK-Strom |
| E7c1‑Q2 b | ebenso: KWKG Jahr 1 · Kapitalwert | 6.137,94 € · −21.904.948,06 € | 7.316,03 € · −21.895.376,67 € | unter dem Staffeldeckel wird der gedeckelte KWK-Strom bezahlt (E7c2‑Q7); frisch gerechnet 6.138,92 → 7.316,95 € |
| E7c1‑Q2 b | σ berechnet, dazu 100 MWh Wärmeüberschuss: KWKG Jahr 1 | 6.448,21 € | 7.316,03 € | ebenso |
| E7c1‑Q2 b | Ersatzweg, σ 0,5: KWKG Jahr 1 | 6.395,35 € | 7.316,00 € | ebenso |
| E7c1‑Q2 b | ohne Deckel: Jahr 5 · Kapitalwert | 1.057,55 € · −21.900.597,13 € | 12.458,43 € · −21.890.762,63 € | das Kontingent reicht länger, die Reihe wird länger |
| E7c1‑Q1 | 1030, σ berechnet 50 ÷ 81: Kürzung 0,002 MWh | ohne Grund | die Herleitung nennt den Rundungsgrund | reiner Text |
| E7c1‑Q7 | Basis und alle Fall-2-Proben nach dem Umbau auf `KwkgJahresbetrag` | — | zahlengleich | derselbe Ausdruck für Lauf und „Wirkung Jahr 1" |
| E7c2‑Q4 | Migration von 105 aus, Brennstoff 24 | — | 1 Einheit, 1 Preiseinheit; 0 Träger, 0 Preiszeilen, 0 Zuordnungen | reiner Einheitentext |

## Zahlen und Abnahme

- **Im Worktree `e7c2`** (Phase 2, nach dem Merge `c3eb2cfe`): Kern-Filter und Windows-Schale 0 Fehler; gefiltert
  Kern 214 und Oberfläche 500 grün; voller Lauf `WP-Plan.Kern.slnf` auf der Repo-Datenbank (Stand 107): EPOS.Kern
  5 054 von 5 055 — rot allein die Schemastand-Wache —, EPOS.UI 5 403, KiKern 524, SpeicherEngine 386,
  SpeicherPlanung 27 und 1 übersprungen; `Auslieferungsvorlage.Tests` 14 von 26 rot („107, erwartet 110");
  Gegenprobe mit einer außerhalb des Repos auf 110 gebrachten Kopie: Schemastand-Wache 1/1, Auslieferungsvorlage
  26/26 (dabei lief ein fremder Testprozess aus einem anderen Worktree, beide Läufe grün). Migrationsprotokoll
  107 → 110: 108 vier Spalten; 109 fünf Zeilen kWh, 23 Abrechnungseinheit; 110 fünf Einheiten und Preiseinheiten,
  eine Preiszeile, Brennstoff 24 Einheit und Preiseinheit (0 Nutzer). Referenzlauf aller dreizehn Projekte auf der
  110-Kopie gegen `2026-09-23_R12_Gebaeudemodell`: 13/13 PASS, 4 250 839 Werte, 399/399 CSV byte-gleich — die Basis
  bleibt R12. SQL-Prüfer 1 705 Texte, 0 Fundstellen; Designer unverändert; Ressourcen je 8 003 Einträge.
- **Gate #446** auf dem Baum von `51c49577` (Worktree `e7c2`, 15:30–15:31): Kern-Filter (Release) 0 Fehler;
  ChartProben 135 Bilder geprüft, 0 Verstöße, alle grün; Dokumentationswachen 26/26; Windows-Schale 0 Fehler; der
  volle Testlauf ist der aus Phase 2 (oben) — mit der Datenbank auf 110 sind Schemastand-Wache (1/1) und
  Auslieferungsvorlage (26/26) grün.
- **Ressourcen** (de und en): **72 neu.** Aus den Punkten 1 bis 5 (17): `ERK_HERLEITUNG`, `ERK_INFO`, `ERK_JA`,
  `ERK_LBL_ERSATZ`, `ERK_LBL_RESTWERT`, `ERK_LEER`, `ERK_NEIN`, `ERK_WIE_BISHER`; `ND_TAFEL_ERSATZ_AUS`,
  `ND_TAFEL_RESTWERT_AUS`; `KI_DLG_VOP_ERSATZ_NAME`, `KI_DLG_VOP_ERSATZ_ERL`, `KI_DLG_VOP_RESTWERT_NAME`,
  `KI_DLG_VOP_RESTWERT_ERL`; `ETV_PREISBASIS_OHNE_SPALTE`; `KOH_FALL5_MISCHLAGE_SPERRE`,
  `STEUER_ENERGIEST_54_MISCHLAGE`. Aus den Punkten 6 bis 9 (55): `WIRT_KWKG_FALL2_VBH`, `WIRT_KWKG_FALL2_VBH_ERSATZ`,
  `WIRT_KWKG_FALL2_RUNDUNG`, `WIRT_KWKG_FALL2_RUNDUNG_KURZ`; `KI_DLG_BHW_ABWAERME_ERL`, `KI_DLG_BHW_SIGMA_ERL`;
  `WIRT_KWKG_SP_FALL`, `WIRT_KWKG_SP_SIGMA`, `WIRT_KWKG_SP_NUTZWAERME`, `WIRT_KWKG_SP_KWK_STROM`,
  `WIRT_KWKG_SP_KUERZUNG`; 44 `BHW_UEB_*` — die sieben im Mockup geplanten unter ihrem geplanten Namen
  (`BHW_UEB_KNOPF_STEUERN`, `BHW_UEB_G_ENERGIEST`, `BHW_UEB_G_STROMST`, `BHW_UEB_WIRKUNG`, `BHW_UEB_WIRKUNG_JAHR1`,
  `BHW_UEB_SCOPE_PROJEKT`, `BHW_UEB_SCOPE_ANLAGE`) und 37 weitere. **1 gestrichen:** `KOH_FALL5_MISCHLAGE`. Kein
  bestehender Text geändert. Je Sprache 7 932 → 8 003 Einträge.
- **Schemaschritte 108, 109, 110** (`SCHRITT_108_ERSATZ_RESTWERT_KENNZEICHEN`, `SCHRITT_109_PREISBASIS`,
  `SCHRITT_110_GASE_NM3`), `SchemaStand.Zielversion` = 110; der nächste freie Schritt ist **111**.

## Abnahme am Gerät (A‑E7c2‑1, Windows und iPad)

(1) Kostenverwaltung, Investitionskosten, Zeileneditor „Position bearbeiten": die Klapplisten „Ersatzbeschaffung
führen:" und „Restwert ansetzen:" (leer — wie bisher · ja · nein) mit Herleitungszeile; „nein" ändert die Tafel
„Ersatz und Restwert" („— nein (Kennzeichen der Position)") und nach „Berechnen" den Kapitalwert; dasselbe in einer
Kostenvorlage und nach „Aus Vorlage übernehmen…". (2) Energieträger-Karte eines Brennstoffs ohne Umrechnungsregel
nach kWh: Preisbasis kWh wählen, speichern, schließen, wieder öffnen — die Karte zeigt kWh. (3) Brennstoffkatalog:
die fünf Gase in Nm³ und €/Nm³, „Sonstige" in kWh und €/kWh. (4) BHKW-Dialog mit § 53 oder § 53a am BHKW und § 54
am Kessel (produzierendes Gewerbe): die Kohärenzzeile ist eine Warnung und nennt die Sperre, die § 54-Zeile der
Erlösrubrik steht auf 0 € mit Begründung. (5) Betriebskosten mit „% der Brennstoffkosten" bzw. „% der Stromkosten":
Bezugsgröße und Herleitung aus dem jüngsten Lauf, ohne Lauf der Grund. (6) PV-Vergütungsdialog mit fester Vergütung
und § 51a: der Erlös auf Cent, die Herleitung nennt den ungerundeten EV-Satz, § 51a mit der Einspeisevergütung.
(7) BHKW-Dialog, „Sätze und Herkunft…" und „Wahl und Herkunft…": die Überlagerung mit Anlagenart, Tatbestand, Fall 1
/ Fall 2, Satztafel, „Wirkung Jahr 1", Energie- und Stromsteuer; Übernehmen, Abbrechen, OK, Wiederöffnen.
(8) Word- und Excelbericht eines Projekts mit Fall 2: die KWKG-Modultafel mit 16 Spalten — im Wortbericht 7 pt, den
Umbruch fünfstelliger MWh prüfen. (9) Englisch.

## Befunde nebenbei

- **H_i = H_s = 0 beim Brennstoff 24.** Der Stamm führt für „Sonstige" keinen Heizwert; eine Preisumrechnung ist
  nicht möglich, Schritt 110 zieht nur den Einheitentext. Offen: H_i = H_s = 1,0 wie bei Strom (13) und Fernwärme
  (23), in einer späteren Stufe (E7c3).
- **Word-Tafel mit 16 Spalten.** Mit Fall 2 trägt die Modultafel im Wortbericht 16 Spalten in 7 pt; fünfstellige
  MWh-Werte können umbrechen — Abnahme am Gerät.
- **„Leer = Vorschlag" in der Überlagerung.** Bei Kontingent und Deckel lässt „Übernehmen" das Feld leer, der Lauf
  leitet selbst ab; bei den Sätzen schreibt es den Vorschlag ins Feld. Ein leeres Satzfeld des Formulars (0 = kein
  Zuschlag) erscheint in der Überlagerung als eigener Wert 0, damit „Übernehmen" nicht still den Vorschlag schreibt.
- **Die Klapplisten bleiben im Formular.** Die Überlagerung trägt alle Wahlen von U22; die acht Klapplisten des
  Formulars und der Knopf „Vorschlag übernehmen" am Feld stehen daneben weiter (R‑Q, Q2: Vorschlag am Feld und
  Überlagerung gelten beide) — die Zeichnung von U22 sah im Formular Anzeigezeilen vor.
- **Vollbenutzungsstunden in Fall 2 netto, in Fall 1 brutto.** In Fall 2 zählen die Vollbenutzungsstunden aus dem
  KWK-Strom und damit nach dem Hilfsstromabzug; in Fall 1 bleiben sie brutto (Etappe B3). Eine Anlage mit Kennzeichen
  bekommt deshalb bei bindendem Deckel auch ohne Kürzung einen höheren Jahresbetrag als ohne Kennzeichen — am
  Beispiel des Rechenwegs 05 (Hilfsstrom 86,8 MWh) 33.800,2 statt 32.022,2 € im Jahr 1 (von Hand gerechnet, nicht
  gemessen). Das folgt aus dem Wortlaut von E7c1‑Q2 b; ob es so gewollt ist, wäre eine Frage an den Anwender.
- **Schrittnummern.** Gebaut sind die drei Schritte als 107, 108 und 109 mit einer Lücke bei 106 bis zum Nachzug;
  der Merge `c3eb2cfe` hat sie auf 108, 109 und 110 umnummeriert. Die Commit-Betreffs E7c2/1 bis E7c2/10 und der
  Phase‑1-Bericht nennen die alten Nummern.
- **Schlüsselzahl der Merge-Nachricht.** `51c49577` nennt 86 neue Schlüssel; die Ressourcendateien tragen 72 neue
  (die 17 aus den Punkten 1 bis 5 sind darin enthalten).

## Offen

- **Abnahme am Gerät** A‑E7c2‑1 (neun Punkte oben).
- **Nächste Etappe: E7c3** — E7c2‑Q5 b (`VpvCtKwh` ungerundet), E7c2‑Q8 b (Energiesteuer-Vorschau je Wahl im Kern),
  B‑6 (Robustheit, geschluckte Fehler), der Kapitalwert 1024 (−676.036,81 € gegen den früheren Konzeptwert), der
  Rest von § 6.3 Nr. 9h (geräteeigene Dauerspalten, Anschluss der Speicherflotte — A7 und A8 binden beides an ND‑S3,
  die Zuordnung ist mit dem Auftrag zu klären), die Katalogzeilen ohne Leser (E7c1‑Q8) und H_i = H_s = 1,0 für den
  Brennstoff 24; danach E8.
- **Push** nach der Regel des Anwenders ohne Rückfrage aus dem Hilfszweig `pm2`, sobald diese Papiere dort gemergt
  sind (`ios_migration_september` und `main`).
- **Papiere mit der Statuszeile:** Register (A3, A4, A6, A9, ET‑D‑3, U‑1, Q2, R‑E7c1 Q1/Q2/Q7, EZ‑9, EZ‑10, neue
  Familie R‑E7c2), Konzept (Kopf mit Schemastand 110, § 2.2, § 2.5, § 2.13 (3), § 3.1, § 3.4, § 3.5, § 3.6, § 3.7,
  § 3.9, § 3.10, § 4, § 5, § 6.1, § 6.2, § 6.3 Nr. 9h, § 7, Anhang), Protokoll der Entscheidwege (§ 8.9, § 8.10, § 0.5
  und Kopf), Analysepapier (Kopf, Nachtrag, § 0, § 5, § 6), Rechenwege 01, 02, 04, 05, 06, 07 und 08, Mockup
  (Ressourcentafeln der Kategorien 2, 4 und 5, Berechnungsgrundlagen der Kategorien 5 und 6, U22 und U32 erledigt,
  U39 teilweise, Stand-Absatz), Logbuch-Sätze und die Wiki-Quellen der Seiten Wirtschaftlichkeit und Kosten; der
  Nachtrag „Schemastand 110" in `Referenzlaeufe/LIESMICH.md`; die Köpfe des Szenarienkonzepts, der Mockup-Prüfung und
  des Nutzungsdauer-Konzepts auf Schemastand 110.

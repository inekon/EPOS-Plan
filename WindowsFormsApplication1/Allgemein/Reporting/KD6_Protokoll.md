# KD6 — Anlagendialoge, Berichte, Startjahr (Protokoll)

Etappe KD6 des Konzepts Kostendialoge (Rev. 1.2, §§ 9–11, Entscheidungen
FK8/FK10), umgesetzt 26.08.2026 auf Branch `kostenformulare`. Erste Etappe mit
GEWOLLTER Ergebnisänderung: das Startjahr je Position wirkt in der
Kapitalwertrechnung (bei nicht gepflegten Startjahren bleibt jedes Ergebnis
bitgleich — nachgewiesen, s. u.).

## Umgesetzt

### § 9 — Anlagendialog-Integration (Serienbaustein `KostenKnoepfe`)

**EIN Helfer** (`Views/Kosten/KostenKnoepfe.cs`, internal static), damit jeder
weitere Dialog ein Einzeiler bleibt:

- `Leiste(eigner, komponente, projektId, carrierId, fk8Hinweis)` baut den
  Block „Kosten" mit den drei Aufrufen **Investitionskosten… /
  Betriebskosten… / Energiekosten…** (Folien 6/7). Projekt- und Träger-ID
  werden zur KLICKZEIT ausgewertet (Projektdialoge setzen ihr Projekt erst
  nach dem Konstruktor). Invest/Betrieb öffnen im Projektkontext den
  reduzierten Kosteneditor (`Form_Kosten` + neue API
  `WaehleKomponente(name, betrieb)` — springt auf Reiter und rollt den
  Komponentenblock ins Bild), im Stammkontext die Vorlage
  (`Form_KostenKomponente`). Energiekosten führen in die
  Energieträgerverwaltung, im Projektkontext vorgefiltert
  (`Form_Energietraeger.WaehleTraeger`; Träger der Komponente via
  `TraegerDerKomponente` aus `Tab_Energieanlagen`).
- **FK8:** `Sperren(...)` setzt die alten eingebetteten Kostenfelder eine
  Version lang schreibgeschützt (TextBoxen bleiben kopierbar/ReadOnly, Rest
  Enabled=false) statt sie zu entfernen — Hinweis „Gepflegt wird im
  Kostendialog." rechts in der Leiste (Kurzform; Vollsatz als Tooltip).

Angebunden an **vier Dialoge**: `Form_Heizkessel_Bearbeiten` (Kessel-Stamm),
`Form_DBBHKW` (BHKW-Stamm), `Form_Heizkessel` (Kessel im Projekt, nicht im
Wizard), `Form_BHKWEing` (BHKW im Projekt). Gesperrt sind jeweils
Investitionskosten, Wartungskosten (+ Einheit), Nutzungsdauer, Raumbedarf
bzw. die fünf BHKW-Einzelposten.

### § 10 — Berichte & Kosten (`UcBkKosten`)

- Komponentenübersicht: dritte Spalte **„Betrieb [€/a]"** aus derselben
  Leselogik (`Form_Kosten.LiesKomponentenSummen`, Kategorie 2); Komponenten,
  die NUR Betriebskosten führen, bekommen eine eigene Zeile („— / Betrag");
  Summenzeile dreispaltig. *Dokumentierte Abweichung vom Wortlaut: dritte
  Spalte statt einer zweiten Tabelle — gleiche Information, ein Raster.*
- Trägertabelle: Spalte **„Leistungspreis [€/(kW·a)]"** — dieselbe
  Vorrangkette wie der `KostenEmissionRechner` (custom_price_power vor
  price_power, 0 = nicht gepflegt/Befund D5, Monatsmodus × 12); Strom zeigt
  „—" (Tarifwelt, keine zweite Wahrheit).

### § 11 — `Form_CaseEingabe` + Startjahr-Rechenwirkung (FK10)

- **%-Eingabe:** Umschalter absolut/„% vom Erwartungswert" (nur bei
  Betrag ≠ 0), Live-Umrechnungszeile „ergibt: Best … Worst …",
  bidirektionales Umschalten; gespeichert werden weiterhin Beträge (kein
  neues Persistenzformat).
- **Startjahr je Position** (0–50; 0/1 = sofort): neue nullable Spalte
  `Tab_ProjektWerte.StartJahr` (Migrationsschritt-38-Spaltenmechanik,
  `StelleSpaltenSicher`), Schreibweg `Form_Kosten.StartjahrSichern`
  (NULL bei ≤ 1), Leseweg `KostenPositionCtrl.Zusatz.StartJahr`.
- **Rechenwirkung `KapitalwertRechner`:** `InvestPosition.StartJahr` —
  Erstzahlung im Jahr X (abgezinst), Ersatzkette ab X + n·ND, Restwert ab
  letzter Beschaffung; Start > T ⇒ Position erscheint nur im Ausweis
  `Zahlungsbild.InvestitionVerschoben`. Neuer Parameter `betriebAbJahr`:
  Betriebskosten einer Position laufen erst ab ihrem Startjahr (inkl.
  Preissteigerung ab t). KEINE Preissteigerung auf Investitionen
  (Konzeptregel).
- **Lesewege** (`WirtschaftlichkeitCtrl`): `LiesInvestitionen` trägt
  StartJahr nur, wenn die Spalte existiert (`StartjahrSpalteVorhanden`-Probe
  — Alt-DB-sicher, Liste bleibt sonst unverändert);
  `LiesBetriebskosten`-Überladung splittet sofort/verschoben, die
  Bestandssignatur liefert unverändert die Gesamtsumme. Ergebnistext nennt
  gesetzte Startjahre („Startjahre gesetzt (FK10): …").
- **Vereinfachung (dokumentiert):** ENERGIEkosten laufen unabhängig von
  Startjahren weiter in der Gesamtrechnung — die Verbrauchssimulation kennt
  keine Teiljahre je Anlage; der Ergebnishinweis benennt das.

11 neue Ressourcenschlüssel de + en (`KDLG_KNOPF_*`, `KDLG_FK8_HINWEIS`,
`KDLG_FK8_KURZ`, `KOSTEN_CASE_*`, `BK_KOSTEN_SP_*`).

## Nachweise (kd6-Smoke, 19/19 PASS)

- **S1–S3 Handrechnung:** Invest 10.000 (ND 20, Start 3), Betrieb 500 ab
  Jahr 3, i = 3 %, T = 20 — Kapitalwert, Barwert der verschobenen
  Erstzahlung (10.000·1,03⁻³) und Betriebs-Teilsumme jahrgenau gegen
  unabhängige Handrechnung (Toleranz 1 ct).
- **S4 Ersatzkette:** ND 5, Start 3 ⇒ Beschaffungen in 3/8/13/18, Restwert
  600 (3/5 der letzten) in der Kapitalwert-Handrechnung.
- **S5 Start > T:** keine Zahlung in der Reihe, Ausweis in
  `InvestitionVerschoben`.
- **S6 Bitgleichheit:** StartJahr 0 und 1 liefern ein BITGLEICHES Ergebnis
  zur Rechnung ohne Startjahr (Ergebnisneutralität für Bestandsdaten).
- **Z1–Z4 Lesewege** (Reflection auf die internal-API, Projekt 1030):
  StartJahr kommt in `LiesInvestitionen` an; Betriebs-Split ab Jahr 4;
  Bestandssignatur = Sofort + verschoben; `LiesZusatzNachId` liefert das
  Startjahr; Testdaten via UPDATE/finally-NULL rückstandsfrei.
- **C1–C3 CaseEingabe-Roundtrip:** absolut 1200/850, %-Modus (±20 % ⇒
  Umrechnung), StartJahr 5 — Handler per Reflection gefeuert, Werte im
  `CaseDaten`-Objekt.
- **K1–K8 Dialoganbindung:** FK8-Sperren greifen in allen vier Dialogen
  (ReadOnly/Disabled nachgemessen), Leiste vorhanden, `WaehleTraeger(63)`
  wählt den Träger, `WaehleKomponente` springt auf den Wartungsreiter.
- **Regression:** kd2 23/23, kd3 20/20, kd4 21/21, kd5 14/14;
  Layout-Sweep 120 Formulare, 0 Befunde.
- **Sichtbeleg:** `kd6_kessel_fk8.png` (Administration Heizkessel: Felder
  gesperrt, Leiste + roter Hinweis).

## Bewusst offen

- § 9-Serie auf die übrigen Gerätedialoge (Wärmepumpe, Solar, Speicher …) —
  dank `KostenKnoepfe` je ein Einzeiler; auf Zuruf.
- Ä2 (BEHG-Block-Verlagerung aus dem Kesseldialog) wartet auf E1/E2 des
  Emissionsfaktoren-Konzepts.
- Entfernen der gesperrten Felder (zweiter FK8-Schritt) in einer
  Folgeversion.
- KD1 (Namensglättung) auf Zuruf des Nutzers.

---

## Nachtrag KD6a (26.08.2026, Nutzerabnahme)

Die Sichtabnahme ergab vier Punkte; alle am selben Tag umgesetzt:

1. **Projektmodus der Kostenverwaltung** (der in KD3 auf KD6 verschobene
   dritte Kontext des § 5): `Form_KostenKomponente.SetProjekt` zeigt die
   Tab_ProjektWerte-Positionen im Vorlagen-Raster. Neuer UI-freier
   `KostenProjektPositionenCtrl` über die Bestands-Schreibwege
   (`SetzeBetragMitZusatz`, `StammIdSicher`, Betrag aus
   `BetriebskostenCtrl.Betrag`); `ucVorlagenZeile` bekam injizierbare
   Sicherungswege (SpeichernWeg/NeuWeg), berechnete Betragsanzeige und den
   ±-Weg in `Form_CaseEingabe` (Worst/Best + Startjahr, § 11).
   Einstiege umgehängt: „Kostenverwaltung öffnen…" (Berichte & Kosten)
   und die drei Anlagendialog-Knöpfe (§ 9) öffnen den neuen Dialog;
   `Form_Kosten` ist kein Einstieg mehr (bleibt Logikträger).
2. **Energieträger-Direkteinstieg** auf der Kosten-Seite (Knopf neben der
   Kostenverwaltung, Projektkontext).
3. **Nutzungsdauer-Spaltenkopf** war abgeschnitten → Kopf „Nutzungsdauer
   [a]" mit angepasster Breite (780/92, bündig zur Worst/Best-Spalte).
4. **Wirtschaftlichkeitsübersicht** in der Kartensprache der Kosten-Seite:
   vier Kennzahl-Kacheln (Kapitalwert ggü. Stamm, Annuität, Amortisation,
   IRR der besten Variante; Kachel-Control jetzt geteilt).

**Fehlerbild „Für mindestens einen erforderlichen Parameter…" an der Wurzel
behoben:** Der P6-Strompreisleser fragte `custom_price`/`price` an — Spalten,
die nur auf der Testkopie existieren; der Produktivbestand führt
`custom_price_work`/`price_work`. Dazu war die StartJahr-Spaltenprobe
wirkungslos (`ExecuteScalar` wirft nie, er meldet selbst) — jetzt stille
Probe über den EngineModus (`SpalteVorhanden`). Nachweis: neuer
Runner-Modus **Fehlerjagd** (alle neuen UI-/Rechenwege im
DataRepository-EngineModus gegen eine frische Kopie der Produktiv-DB):
**0 Befunde**.

Smoke kd6 37/37 (neu B0–B9 + B7b: Anlegen/Speichern/Case/Startjahr/Löschen
rückstandsfrei, Dialogaufbau, Variantenzeile aus, Träger-Knopf); Regression
kd2–kd5, pv1–pv6 grün; Layout-Sweep 120 Formulare 0 Befunde.

---

## Nachtrag Ä8 (26.08.2026, Nutzerentscheid)

- **Schreibschutz der Auslieferungsvorlagen aufgehoben** — für Investitions-
  UND Betriebskostenvorlagen. Zentral in `KostenVorlagenCtrl.IstNurLesen`
  (immer false; das DB-Flag bleibt Herkunftsmarker der Saat); das rote
  Banner und alle Sperren entfallen damit überall. Restschutz: die
  STANDARD-Vorlage einer Komponente bleibt unlöschbar (Quelle von
  „Speichern unter…" und der Übernahme; die KD1-Saat läuft nicht erneut)
  — Varianten sind löschbar. kd2-Smoke umgestellt: Schreiben auf die
  Auslieferung ERLAUBT und spurenfrei (Urzustand wird zurückgeschrieben),
  Standard-Löschung abgelehnt.
- **Energieträgerverwaltung, Katalogkontext:** Der Menüeinstieg zeigte einen
  leeren Dialog — `Form_Kosten.GetAllCarriers` las nur die
  Projektzuordnungen (`energy_project_settings`), bei Projekt 0 also
  nichts. Neuer Katalogzweig listet alle Katalogträger (`energy_carrier`
  direkt); der erste Träger ist vorgewählt, damit der Detailbereich nie
  leer wirkt. Sichtbeleg `ae8_et_katalog.png` (Produktiv-Kopie, 20 Träger).
  Weiter offen (KD4-Vermerk): Katalogpreis-Schreibweg des Katalogkontexts.

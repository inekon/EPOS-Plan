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

---

## Nachtrag Ä9 (26.08.2026, Nutzerauftrag)

**Energieträgerverwaltung als echte Katalogpflege** (der offene KD4-Punkt):

- Katalogleiste **Neu… / Variante / Löschen** (neuer UI-freier
  `EnergietraegerKatalogCtrl`; Löschen mit Verwendungsschutz — Projekt-
  zuordnungen und Anlagenverweise halten den Träger, der Grund wird
  benannt). Die **Variante** kopiert die komplette Katalogzeile — genau der
  Weg zu abweichenden Emissionswerten je Träger.
- **Stammkopf editierbar**: Bezeichnung + Gruppen-Klappliste (vorhandene
  Gruppen, freie Eingabe erlaubt), „Übernehmen“ schreibt die Katalogzeile.
- **„Speichern“ im Katalogkontext** schreibt jetzt die KATALOGwerte
  (Arbeits-/Grund-/Leistungspreis, Hi/Hs, CO2/SO2/NOx) direkt in
  `energy_carrier` — die KD4-Lesesperre ist Geschichte.
- **Saat**: Schritt 42 Flüssiggas (kg; Hi 12,87 / Hs 14,00 kWh/kg; CO2
  239 g/kWh aus dem BEHG-Faktor 0,0663 t/GJ; Brennstoff-Stammverweis per
  [Bezeichner]-Suche), Schritt 43 VDI-3805-Feststoffe: Steinkohle (340),
  Braunkohlebrikett (400) [Gruppe Kohle], Scheitholz, Holzpellets,
  Holzhackschnitzel [Gruppe Holz; biogen CO2 = 0 wie Biogas-Bestand].
  Je Träger idempotent; Preise 0 = nicht gepflegt.
- **Befundfixe**: Der 42er-Brennstoff-Lookup riet Spaltennamen und löste
  beim App-Start drei Fehlerboxen aus (ExecuteScalar meldet selbst; die
  Migration wirkte gescheitert) → fester `[Bezeichner]`-Lookup, still im
  EngineModus, plus Nachzug für ein bereits ohne Verweis gesätes
  Flüssiggas. `DataRepository`-Fehlermeldungen tragen jetzt die ABFRAGE
  im Text — jede künftige Box verortet sich selbst.

Nachweise: kd6-Smoke 47/47 (T1–T9 + T1b: Saat, Neu/Umbenennen/Gruppe/
Variante/Katalog-Speichern/Löschguard/spurenfrei); Migration 41→43 auf
frischer Produktiv-Kopie ohne Boxen; Fehlerjagd (erweitert um
Anlagendialoge, Ertragsreiter, Übernahme-Dialog, Wirtschaftlichkeitsseite,
Projektmodus) 0 Befunde; Sichtbeleg `ae9_et_katalogpflege.png`.
Feststoffe ohne Brennstoff-Stammtreffer führen ID_Brennstoff 0
(Katalogwerte tragen Preis/Emission; Verknüpfung auf Zuruf).

---

## Nachtrag Ä10 (26.08.2026, Nutzerauftrag)

Katalog-Übernahmen in die PROJEKT-Dialoge:

- **Energieträgerverwaltung (Projektkontext):** Leiste „Aus Katalog
  übernehmen…“ (Mehrfachauswahl der noch nicht zugeordneten
  Katalogträger; die Zuordnung entsteht mit leeren custom-Feldern — es
  gelten die KATALOGwerte, eine Wahrheit) und „Entfernen“ (Anlagen des
  Projekts schützen ihren Träger; projektbezogene Preishistorie wird mit
  gelöst). Damit ist der offene KD4-Punkt § 7.2 (Träger-Übernahme)
  ERLEDIGT.
- **Kostenverwaltung (Projektmodus):** Die Komponenten-Klappliste bietet
  jetzt ALLE sieben Anlagen-Komponenten an — auch ohne Anlage/Positionen;
  nur so lässt sich „Aus Vorlage übernehmen…“ (§ 8) für eine noch leere
  Komponente nutzen.
- **Kopfzeilen-Fix:** „Betrag netto [€]“ (688 + 110) überdeckte den
  Nutzungsdauer-Kopf (780) — Köpfe jetzt spaltenbündig zu den
  Rasterfeldern (688/108, 800/72, zweizeilig „Nutzungs-/dauer [a]“).

Nachweise: kd6-Smoke 51/51 (neu T10–T13: Flüssiggas zuordnen → Liste
führt den Träger → lösen; Entfernen-Guard am verbauten Erdgas 63);
Fehlerjagd auf frischer Produktiv-Kopie 0 Befunde; Sichtbeleg
`ae10_et_projekt.png`. Beobachtung aus der Jagd: Die Produktiv-DB stand
bereits auf 43 — der stille 42/43-Lauf beim Nutzer war erfolgreich.

---

## Nachtrag Ä11 (26.08.2026, Nutzerauftrag)

**Übernahme-Dialog: Katalogvorlagen als wählbare Quelle.** Die Quelle
„Aus Vorlage/Variante“ ist jetzt eine Klappliste ALLER Vorlagen und
Varianten des Admin-Katalogs (Komponente + Kategorie; Standard zuerst,
Vorauswahl = die im Stammkontext geöffnete Variante). Vorher war die
Quelle auf die „aktuelle“ Variante fixiert — im Projektmodus, der keine
Variantenzeile mehr hat, ließ sich damit überhaupt keine Katalogvorlage
wählen. Beim Aufruf aus dem Projektmodus steht das ZIELPROJEKT fest
(vorbelegt und gesperrt); im Stammkontext bleibt die freie Zielwahl.

Nachweise: kd6-Smoke 53/53 (B10 Ziel fest/gesperrt, B11 Vorlagenliste
gefüllt), kd3-Regression (Übernahme-Mechanik) grün, Layout-Sweep 120/0,
Fehlerjagd auf frischer Produktiv-Kopie 0 Befunde.

---

## Nachtrag Ä12 (26.08.2026, Nutzerauftrag)

Projektmodus der Kostenverwaltung — Editieren, Speichern, Abbrechen
(Investitions- UND Betriebskosten):

- **Zeilen-Editor (Stift) aktiv:** öffnet `Form_VorlagenPosition` mit der
  Projektzeile; das OK des Editors schreibt sofort über den
  Projekt-Schreibweg.
- **Speichern-Knopf:** übernimmt alle Zeilenfelder und schreibt sie in
  das AKTUELLE Projekt; die Fußsumme bestätigt mit Uhrzeit.
- **Abbrechen OHNE Datenübernahme:** Dafür ist der Autosave des
  Projektmodus abgeschaltet (`ucVorlagenZeile.NurExplizitSpeichern`) —
  Feldänderungen leben bis „Speichern“ nur im Objekt; „Abbrechen“ (oder
  das Schließen-X) verwirft sie. Bewusst sofort geschrieben bleiben
  Anlegen („+ Position“/Neu-Zeile), Löschen (Papierkorb mit Rückfrage),
  der Zeilen-Editor und ± (Worst/Best/Startjahr) — sie haben eigene
  Bestätigungen. Auch der Komponenten-/Kategorienwechsel verwirft
  ungespeicherte Feldänderungen (dokumentiertes Verhalten).
  Der Stammkontext behält das Sofort-Speichern des Bestands; Speichern/
  Abbrechen sind dort verborgen.

Nachweise: kd6-Smoke 56/56 (B12 Stift sichtbar; B13 Feldänderung lässt
die DB unberührt, erst `JetztSpeichern` schreibt — danach Urzustand
wiederhergestellt; B14 Knöpfe nur im Projektmodus); kd2-Regression
(Stamm-Sofortspeichern) grün; Layout-Sweep 120/0.

## Nachtrag Ä18 (26.08.2026) — Tarifstruktur komponentenbezogen, Strom-Leistungspreis

Nutzerauftrag: Die Tarifstruktur gehört nicht in den Energieträgerdialog,
sondern KOMPONENTENBEZOGEN in die Wirtschaftlichkeit (BHKW, PV, Wärme-
pumpe …); im Energieträgerdialog soll stattdessen ein Leistungspreis mit
Jahr/Monat-Auswahl pflegbar sein.

- Migrationsschritt 44 (ZIEL_VERSION 44): `pricing_model.has_powerprice`
  für ELECTRICITY — der Stromträger läuft im ET-Dialog durch denselben
  Leistungspreis-Zweig wie Gas (Satz + Jahres-/Monatsmodus + Saison-
  reihen, FK6/FK6a). Der KD4-Sonderfall (Feld gesperrt, „über die
  Tarifstruktur“) und der Ä16-Tarifknopf sind entfernt; § 7.1 gilt
  damit wie ursprünglich konzipiert. Die Leistungspreis-Spalte der
  Kosten-Seite (§ 10) zeigt den Strom-Satz (Kurzschluss „—“ entfernt).
  GRENZE: In der trägerbezogenen Energiekosten-Schiene hat Strom keine
  Anschlussleistung aus Gerätedaten — der Flat-Satz wird gepflegt und
  angezeigt, eine Rechenwirkung wäre eine eigene Etappe.
- `Form_Tarifstruktur`: neues enum `TarifSicht` (Komplett, Strombezug,
  Bhkw, Photovoltaik). EIN Tarifsatz je Stamm bleibt die Wahrheit; eine
  Sicht BAUT nur ihre Blöcke (kein Ein-/Ausblenden — Verrutsch-Falle),
  der Speichervorgang übernimmt nur gebaute Felder (Rest bleibt, per
  Smoke belegt). Geteilte Felder tragen den Zusatz „geteiltes Feld“,
  jede Teilsicht eine Hinweiszeile.
- Einstiege: `UcWirtschaftlichkeit` baut neben dem PV-Knopf
  „BHKW-Tarif…“ (sichtbar bei BHKW in der Gruppe) und „Strombezug…“
  (sichtbar bei Wärmepumpe in der Gruppe ODER aktiver Tarifstruktur —
  sonst ließe sie sich nicht mehr abschalten); `ErzeugerFlags` führt
  dazu neu `Waermepumpe` (Tab_WP). `Form_PhotovoltaikVerguetung`
  erhält den Fußknopf „Einspeise-Tarif…“ (PV-Sicht) — Designer-Datei
  ergänzt. Drei neue MyResource-Schlüssel de+en.

Nachweise: kd6 67/67 (U1 Feldpräsenz je Sicht, U2 Teilsicht-Speichern
erhält den fremden Marker und räumt die Testzeile weg, U3 ET-Strom
aktiv ohne Tarif-Einstieg, U4 Seiten-Knöpfe); kd4 21/21 (E3 auf das
neue Strom-Verhalten umgestellt); pv6 28/28; Sweep 115/0/5; Fehlerjagd
0 Befunde inkl. zweier neuer Stationen (Tarifsichten bauen, ET-Karte
Strom) auf frisch migrierter Produktivkopie (41→44); Sichtbelege
ae18_tarif_bhkw/pv/strom.png und ae18_et_strom.png (FormDump um
DUMP_SICHT/DUMP_TRAEGER erweitert).

## Nachtrag Ä19 (26.08.2026) — Anlagenbezug und Kostenzugang der Anlagendialoge

Nutzerauftrag (5 Screenshots): Variantenwechsel zeigte fremde Komponenten,
die Kosten-Seite soll je ANLAGE listen und den Träger der gewählten Anlage
kennzeichnen, der Kostendialog gehört in die Anlagendialoge (statt
Modulkosten), und der Admin-Kontext braucht dieselbe Fußleiste.

- Kosten-Seite: Überschrift „Anlagenkomponenten“; Zeile je Anlagenzeile
  (`AnlagenMitTraeger`), Komponentensumme an der ersten Anlage (FK2 bleibt
  Pflegeebene), gelbe Warnzeile für Positionen ohne verbaute Anlage
  (Variantenkopie-Befund — Kachel und Tabelle bleiben deckungsgleich),
  Träger-Kennzeichnung über Row-Tags, „Kostenverwaltung öffnen…“ mit der
  Komponente der Auswahl.
- Varianten-Anlegen aus Übersicht/Variantentest ruft jetzt
  `Program.startfrm?.VariantenAnzeigeAktualisieren()` — die Klappliste des
  Projektkopfs kennt die neue Variante sofort.
- Wizard_WPItem: Modulkosten-Zeile AUSGEHÄNGT (nicht nur unsichtbar — der
  Offscreen-Weg zeichnet Visible=false in den Alt-Dialogen weiter); Ersatz:
  Summenzeile + „Kosten bearbeiten…“ (auch im leeren Zustand, Knopf dann
  gesperrt), btn_WP heißt „Parameter Bearbeiten…“ (beide resx).
  Form_WP ohne Modulkosten-Zeile; Speicherwege unverändert (Feldwert
  läuft mit).
- Form_KostenKomponente: Fußleiste OK/Speichern/Abbrechen in BEIDEN
  Kontexten; `ZeileBauen` setzt `NurExplizitSpeichern` zentral — die
  Ä12-Semantik gilt damit auch im Adminkontext (kd2 blieb grün, die Suite
  testet die Controller-Schreibwege).

Offen als Folgerunde: analoge Umstellung PV/Stromspeicher/Solar
(BHKW/Kessel tragen die §-9-Leiste seit KD6), echte Instanzkosten je
Anlage (Datenmodell), Bereinigungsangebot für kopierte Positionen beim
Varianten-Anlegen.

Nachweise: kd6 72/72 (V1–V5), kd2/kd4/pv6 grün, Sweep 115/0/5,
Fehlerjagd 0 Befunde (frische Produktivkopie); FormDump kann jetzt auch
UserControls (Trägerform + SetzeProjekt via DUMP_PROJEKT); Sichtbelege
ae19_anlagenliste/wp_detail/wp_datenbank/admin_fuss.png.

## Nachtrag Ä20 (26.08.2026) — Kosten je Anlage

Nutzerauftrag: „Die Kostenverwaltung soll einer Anlage (z. B. Wärmepumpe
CS5800i) jeweils zugeordnet sein … Dieses Schema sollte für alle Anlagen
gelten.“ — die in Ä19 angekündigte Datenmodell-Etappe.

- `Tab_ProjektWerte.ID_Anlage` (nullable) + Vorsorge in
  `KostenPositionCtrl.StelleSpaltenSicher`; Migrationsschritt 45
  (ZIEL_VERSION 45) ordnet den Bestand der jeweils ERSTEN verbauten
  Anlage der Komponente zu — auf der Testkopie 81 Positionen, 3
  Komponenten ohne Anlage bleiben „ohne Anlagenzuordnung“ (NULL).
- Projektduplizierer: FK_MAP-Eintrag ID_Anlage → Tab_Energieanlagen —
  Variantenkopien zeigen auf die KOPIE-Anlagen (W5-belegt); ohne den
  Eintrag stünden alle kopierten Positionen als verwaist da.
- Controller: `KostenProjektPositionenCtrl.Lies/Neu(…, idAnlage)`
  (0 = NULL-oder-verwaist-Pflege, -1 = alle/Bestandssignatur),
  `AnlageZuordnen`; `Form_Kosten.LiesAnlagenSummen` (Komponente +
  ID_Anlage); `KostenVorlagenUebernahmeCtrl.AusVorlage(…, idAnlage)`
  mit anlagenbezogenem NurAnlegen-Check.
- UI: Kostenverwaltungs-Projektmodus listet ANLAGEN (AnlagenWahl;
  Titel „Kostenverwaltung BHKW — BHKW EW M 50 S [K] Erdgas — …“),
  Ä10-Einträge „(keine Anlage im Projekt)“ bleiben; Kosten-Seite
  summiert je Anlage (gelb: ohne Anlagenzuordnung, zählt mit; rot nur
  Komponenten gänzlich ohne Positionen); Anlagenauswahl öffnet die
  Kostenverwaltung mit der Anlage; Wizard_WPItem zeigt und pflegt die
  Summen seiner Anlagenzeile (item.ID).
- Rechenkerne unverändert (Aggregation je Projekt — Kapitalwert,
  Betriebskosten, Energiekosten lesen ID_Anlage nicht).

Nachweise: kd6 79/79 (W0–W6), kd2/kd4/pv6 grün, Sweep 115/0/5,
Fehlerjagd 0 Befunde auf frisch migrierter Produktivkopie (41→45);
Sichtbelege ae20_kostenverwaltung_anlage.png, ae20_anlagensummen.png.

## Nachtrag Ä21 (27.08.2026) — wizardfeste Anlagenkosten, Übernahme von Anlagen

Nutzerbefunde: WP-Positionen in der Heizkessel-Variante („warum steht die
dort? ändere“), Übernahme soll die BEREITS VORHANDENE Wärmepumpe als
Quelle anbieten, „Zuordnung funktioniert nicht“.

- URSACHENKETTE: Der Anlagen-Wizard löscht Anlagenzeilen und legt sie mit
  neuen IDs an — eine reine ID-Zuordnung (Ä20) verwaist dabei. Deshalb
  GERÄTEANKER `ID_AnlageGeraet` (Schritt 46) + `ZuordnungReparieren`
  (Selbstheilung vor jedem UI-Aufbau; ohne wiedergefundenes Gerät wird die
  Zuordnung ehrlich gelöst statt still falsch zu bleiben).
- Einzel-Anlage-Löschen (`WizardCtrl.Del_Projekt_ID_Waermeerzeuger`)
  nimmt die Positionen der Anlage mit; die Typ-/Alle-Löschwege bleiben
  unberührt (das ist auch der Wizard-Neuaufbau — dort heilt der Anker).
  BESTAND: Doppelklick auf die gelbe Zeile der Kosten-Seite löscht die
  losen Positionen nach Rückfrage (Einzellöschung, keine Subquery).
- Übernahme: „Aus Projekt/Anlage“ — Quell-Anlagen-Klappliste je
  gewähltem Projekt (auch das EIGENE), `AusProjekt(…, quellAnlage,
  zielAnlage)` mit zielanlagenbezogenem NurAnlegen-Check und Anker auf den
  Kopien; Vorschau zählt je Ziel-Anlage (vorher komponentenweit — genau
  das ließ die Übernahme „nicht funktionieren“: „führt bereits 7
  Positionen“ bei leerer zweiter Anlage).
- ACE-LEKTION (neu gemessen): Parameter + Unterabfrage binden in falscher
  Reihenfolge — auch SELECT liefert still 0 Zeilen (Literal 1 /
  Parameter 0). Alle Subquery-Stellen der Anlagenkosten nutzen
  Int-Literale; Memory-Eintrag präzisiert.
- Gemessen: `Tab_Energieanlagen` führt UNIQUE (ID_Projekt, ID_WP/…) je
  Gewerk — die Duplikat-Meldung beim Zweitverbau desselben Geräts ist
  Bestandsschutz (Gerätekopie nach Rückfrage ist der reguläre Weg).

Nachweise: kd6 85/85 (X1–X6), kd2/kd4/pv6 grün, Sweep 115/0/5,
Fehlerjagd 0 Befunde (frische Produktivkopie 41→46); Beleg
ae21_uebernahme_anlage.png.

### Ä21-Nachfix „ID 67" (27.08.2026)

Die WP-Detailansicht überschrieb beim Öffnen (programmatische Vorwahl in
SetControls) die PROJEKT-Geräte-Id des Listeneintrags mit der
Stammkatalog-Id — das zweite „Ändern.." der Verwaltung meldete dann
„Datensatz (ID 67) nicht gefunden". Fix: stille Füllung
(m_bStilleFuellung); nur echte Nutzerauswahl wechselt die Id, der
Neu-Fluss bleibt unverändert. kd6-X7 sichert das Verhalten ab (86/86).

### Ä22 (27.08.2026) — WP-Verwaltung zweistufig, Kosten-Knopf bei Neuanlage

Nutzerbefunde: „Kosten bearbeiten…" bei Neuanlage ausgegraut; „ID 67"
erneut beim Ändern.

- Ein FRISCH angelegter Listeneintrag trägt bis zum Verwaltungs-OK
  designbedingt die STAMM-Id (der Wizard materialisiert erst beim
  Speichern). „Ändern.."/Aufbau lasen aber nur Tab_WP → Box bzw.
  items[0]-Crash. Jetzt liest `GeraetedatenFuellen` zweistufig
  (Projektgerät vor Stammkatalog) an allen drei Stellen der Verwaltung;
  die Meldung kommt nur noch, wenn die Id in KEINER der beiden Tabellen
  steht (kd6-X8).
- „Kosten bearbeiten…" hängt an der ANLAGENZEILE (item.ID) — bei einer
  ungespeicherten Neuanlage gibt es sie nicht: Der Knopf bleibt gesperrt,
  erklärt das aber im Tooltip (erst OK/speichern, dann über „Ändern..");
  die Summenzeile zeigt „Invest — · Betrieb —" statt einer falschen 0.

Nachweise: kd6 87/87, Sweep 115/0/5.

### Ä23 (27.08.2026) — Leistungsspalte der WP-Verwaltung zeigte 0 kW

Nutzerbefund: „Leistung [kW]" der Wärmepumpen-Verwaltung steht auf 0,
obwohl die Wärmepumpe mit 12 kW gepflegt ist.

- Die Liste speiste die Spalte aus drei inkonsistenten Quellen: Der
  AUFBAU nutzt `item.Nennleistung` (seit Ä22 korrekt), die
  ZEILEN-AKTUALISIERUNG nach „Neu"/„Ändern" schrieb aber
  `item.maxPTherm` — das Feld ist am Listenobjekt nie gefüllt und
  überschrieb den korrekten Wert mit 0. Beide Stellen schreiben jetzt
  die Nennleistung.
- Der NEU-Fluss übernahm die Stammdaten gar nicht erst ins Listenobjekt:
  vor dem Einreihen läuft jetzt `GeraetedatenFuellen` (zweistufig —
  ID_WP ist dort noch die Stamm-Id, siehe Ä22).
- Die ECHTE WP-Wahl in der Detailansicht (listBox-Handler) wechselte nur
  die Id; Nennleistung & Co. blieben am Listenobjekt auf dem alten
  Stand. Der Wahlzweig übernimmt jetzt alle acht Stammfelder ins item —
  die stille Ä21-Vorwahl (m_bStilleFuellung) bleibt unverändert außen
  vor (kd6-X9).
- Der DOPPELKLICK-Leser der Verwaltung (Alt-Logik mit eigener „nicht
  gefunden"-Box) läuft ebenfalls zweistufig über `GeraetedatenFuellen`.

Nachweise: kd6 88/88 (neu X9: echte Wahl übernimmt die
Stamm-Nennleistung ins Listenobjekt), Sweep 115/0/5.

### Ä24 (27.08.2026) — „Wärmepumpe ohne Anlagenzuordnung" trotz verbauter WP

Nutzerbefund: Die Kosten-Seite zeigt eine gelbe Zeile „Wärmepumpe — ohne
Anlagenzuordnung", obwohl die Wärmepumpe im Projekt verbaut ist. Frage:
Trifft das auch andere Komponenten?

Wurzel (gemessen an Projekt 1037: 7 Positionen mit Eingabewerten hingen an
Geräteanker 1672032, den es nicht mehr gab):

- Der Del+Add-Speicherweg materialisiert Geräte über CopyFromStamm per
  BEZEICHNER. Wechselt die Gerätekopie einer Anlage (Geräte-Neuwahl,
  Umbenennung, Duplikat-Gerätekopie), blieben die Kostenanker auf der
  alten Kopie; GeraeteWaisen.Aufraeumen räumte sie ab, die
  Ä21-Selbstheilung löste die Zuordnung „ehrlich" → gelbe Zeile.
- Der Projektduplizierer versetzte ID_Anlage (FK_MAP), den
  komponentenabhängigen Anker ID_AnlageGeraet aber nicht — Variantenkopien
  ankerten an den Geräten des QUELLprojekts und verloren die Zuordnung
  beim ersten Anlagen-Wizard-Lauf der Variante (1038/1039).
- Anzeige: Die Erfassungsgruppen ohne Anlagenbezug (Wärmezentrale,
  Bauliche Anlagen, Stromeinspeisung) liefen seit Ä20 fälschlich in den
  gelben „ohne Anlagenzuordnung"-Topf.

Reparaturen:

- WizardCtrl.KostenAnkerUmziehen im EINEN Schreibweg aller Erzeuger:
  Anker der Positionen DIESER Anlagenzeile (item.ID) ziehen beim
  Gerätetausch auf die neue Kopie um; die frische Anlagen-Id läuft
  zurück ans Listenobjekt (Session-Liste bleibt an ihrer Zeile).
- KostenProjektPositionenCtrl.AnkerNachziehen: leitet die Anker eines
  Projekts aus der GÜLTIGEN ID_Anlage neu ab — läuft nach jedem
  Duplizieren; Migrationsschritt 47 (ZIEL_VERSION 47) einmalig für den
  Bestand (Produktivlauf: 118 Positionen).
- Kosten-Seite: nicht anlagenfähige Erfassungsgruppen (Ä7-Prädikat
  IstWaehlbar) wieder als reguläre weiße Komponentenzeile; ihr Zeilen-Tag
  öffnet die Kostenverwaltung im Komponentenmodus.

Bewusst NICHT repariert: Bestandsleichen mit totem Anker (z. B. die
7 Invest-Positionen 14000/3000/4000 in Projekt 1037) — bei zwei
WP-Anlagen ist die richtige Zuordnung nicht beweisbar; der
Ä21-Doppelklick-Löschweg besteht, die Werte sind im Protokoll genannt.

Antwort auf die Nutzerfrage: Die Tausch-Mechanik traf alle 7
anlagenfähigen Komponenten (ein gemeinsamer Schreibweg), der
Anzeige-Fehler die 3 Erfassungsgruppen.

Nachweise: kd6 92/92 (X10 AnkerNachziehen, X11 Erfassungsgruppen-Zeile,
X12/X12b Gerätetausch end-to-end über Del+Add samt Selbstheilung),
kd2/kd4/pv6 grün, Sweep 115/0/5, Migration 41→47 auf frischer
Produktivkopie OK, Fehlerjagd 1019 + 1037 je 0 Befunde.

**Nachtrag Ä24 — Bestandsbereinigung (27.08.2026, Nutzerentscheid):** Die im
Ä24-Protokoll genannten Altlast-Leichen wurden nach Rückfrage auf der
Produktiv-DB entfernt (App geschlossen, Sicherung
`K:\backup_kenndaten_2026-08-27_vor_ae24_bereinigung.accdb`):
Projekt 1037 „Wärmepumpe WG" 7 lose Invest-Positionen über 21.000 €
gelöscht (Entscheid: löschen, NICHT der CS7800iLW zuordnen — sie behält
ihre 14.000 €); Varianten 1038/1039 je 16 kopierte WP-Positionen
gelöscht. Vorab-Verifikation traf exakt die gemessene Lage (7/21.000,
16, 16); Nachmessung: 0 lose WP-Positionen in 1037–1039, Invest 1037 =
19.000 (CS5800i) + 14.000 (CS7800iLW) = 33.000 €. Ältere lose Reste in
1007 (Solarthermie 1, BHKW 1) und 1011 (BHKW 2) blieben unangetastet —
löschbar jederzeit per Doppelklick auf die gelbe Zeile.

**Nachtrag Ä24 — Alt-Reste 1007/1011 (27.08.2026, Nutzerfreigabe):** Auch die
letzten vier losen Positionen des Bestands wurden auf Nutzeranweisung
entfernt (App geschlossen, Sicherung
`K:\backup_kenndaten_2026-08-27_vor_altreste.accdb`): Projekt 1007 je eine
Solarthermie- und BHKW-Position (0 €), Projekt 1011 zwei BHKW-Positionen
(30 € Invest + 30 € Betrieb). Vorab-Zählprüfung traf exakt 4; verwaiste,
noch heilbare Positionen gab es keine. Nachmessung über ALLE Projekte:
0 lose Positionen anlagenfähiger Komponenten — der Bestand ist bereinigt.

### Projekttransfer T1+T2 (28.08.2026) — Importfehler Ergebnisfamilie behoben

Nutzerbefund: Import der `Booster-Kette mit Kombi-Speicher.wpx` brach mit
„INSERT Tab_ErgebnisEnergiebedarf … ID_Ergebnis=206 -> Tab_Ergebnis[ID]:
FEHLT" ab.

- WURZEL (B1): Die Import-Umschlüsselung fragt `ErmittleZieltabelle` des
  Duplizierers; dessen Beziehungswissen (`_echteFks`) lud nur `ErmittlePlan`
  (Duplizieren/Export). Der reine Import lief auf leerem Wissen — jede
  Beziehung außerhalb der FK_MAP (`ID_Ergebnis → Tab_Ergebnis`) blieb
  unversetzt. Deshalb traf es die App (nur Import), nie den Prüfstand
  (Export davor füllte das Wissen am selben Objekt).
- FIX: `BeziehungenLaden`-Extrakt im Duplizierer, Aufruf im Import;
  Namenskonventions-Gürtel in `Umschluessele` (ID_<X> → Tab_<X>, nur wenn
  die Tabelle im Paket reist); Randfall „Elterntabelle ohne Zeilen" löst den
  Verweis ehrlich. Dazu B2 (Manifest trägt echten Schemastand, Import lehnt
  fremde Stände klar ab) und B3 (`AnkerNachziehen` nach dem Commit — vorher
  8 fremde WP-Kostenanker im Testimport).
- NEBENBEFUND: Die Rechner waren auseinander (Paket Schemastand 54, hiesige
  App 47) — der heutige Sync glich die App an; die Produktiv-DB migriert beim
  nächsten Start auf 54.

Nachweise: neuer Runner-Modus `transfer` (Export Kopie A → Import Kopie B)
15/15 PASS — darunter der Import der ECHTEN Nutzerdatei mit korrekt
verdrahteter Ergebnisfamilie und die Ablehnung eines Pakets mit
verfälschtem Schemastand; kd6 92/92, Sweep 114/0/5.

### Projekttransfer T3+T4+T5 (28.08.2026) — Varianten, Vorschau, Prüfstand

- T3 VARIANTEN: Export-Dialog führt eine Häkchenliste der Varianten des
  gewählten Projekts (vorbelegt alle an). Paketformat V2: Varianten als
  eigene Projektbäume unter projects/<i>/data/, Verknüpfungen als
  variantLinks im Manifest (Tab_Variante reist bewusst NICHT als
  Tabellenzeile — ID_ProjektRef wäre über Paketgrenzen nicht versetzbar);
  der Import schreibt die Verknüpfung neu, in EINER Transaktion über alle
  Bäume (BaumEinfuegen je Projekt). V1-Pakete bleiben lesbar.
- T4 DIALOG: Paketvorschau nennt die Varianten; Abschlussbericht in der
  Erfolgsmeldung und als <paket>.importbericht.txt; Sicherungs-Haken
  (vorbelegt an) kopiert die DB vor dem Import mit Zeitstempel.
- T5 PRÜFSTAND: Runner-Modus `transfer` dauerhaft (Soll 17/17) — B1-Kern
  (frischer Controller), Nutzerpaket-Realfall Booster-Kette, Roundtrip-
  Zählungen, Kostenanker, Schemastand-Ablehnung, Variantenpaket.

Nachweise: transfer 17/17 PASS; kd6 und Sweep nach dem Umbau grün
(Nachtrag folgt der Zahl nach dem Lauf).

### Projekttransfer T6 — Artefakt-Runde (28.08.2026, Nutzerbefunde)

Zwei Befunde nach der Sichtprüfung des Nutzers:

- B5 IMPORTIERTE ALTLASTEN: Die importierte Booster-Kette zeigte gelbe
  Zeilen „Solarthermie/Pufferspeicher — ohne Anlagenzuordnung" (3.775 +
  3.000,50 EUR). Paketanalyse: Die Positionen waren SCHON IN DER QUELLE
  lose (ID_Anlage NULL, tote Anker) — kein Import-Riss. Fix: Der Export
  lässt lose Positionen anlagenfähiger Komponenten zurück (Filter im
  Baum-Schreiber mit Ä20/Ä21-Spalten-Guard; Erfassungsgruppen ohne
  Anlagenbezug reisen unverändert). Beweis transfer-T6 am echten
  Booster-Projekt (Quelle 11/2 lose, Paket 9/0 lose).
- B6 ANLAGE-VERGLEICH MISCHT GEWERKE: Die Unterschiedsansicht der
  Übersicht las für den Konfigurationsblock „Anlage" je Projekt die ERSTE
  Tab_Energieanlagen-Zeile — WP-Stamm gegen BHKW-Variante ergab
  Scheinunterschiede (35→85 Grad C, Heizstab Ja→Nein, ...), Referenzanlagen
  zählten mit. Fix im AbweichungsErmittler (eine Wahrheit für Übersicht,
  Unterschiedsliste, Übernahme, Bericht): ErsteEchteAnlage (Referenz-Typen
  5–9 nie), AnlagenVergleichbar (Diff nur gewerkgleich),
  AnlagenEinheitlich (Gegenüberstellung nur bei einheitlichem Gewerk).
  Beweis transfer-T7 (WP↔BHKW liefert 0 Anlage-Zeilen; selbst↔selbst
  bleibt vergleichbar).

Nachweise: transfer 19/19 PASS; kd6/Sweep siehe Regressionslauf.

### Übersicht-Gegenüberstellung: eine Zeile je Komponente (28.08.2026)

Nutzerbefund: Bei zwei Wärmepumpen im Stamm zeigten die Merkmalszeilen
(Hersteller, Typ, Bauart, …) nur die ERSTE Komponente — die zweite fehlte
komplett. Nutzervorschlag umgesetzt: nur die Komponenten darstellen,
Merkmale per Mouse-over und bei Auswahl.

- ProjektDetails führt je Gewerk jetzt ALLE Komponentenzeilen
  (KomponentenAlle, Anlagenreihenfolge); AbweichungsErmittler liefert
  KomponenteZeile(gewerk, index), BezeichnerMerkmal und MerkmaleText
  (deklarative Feldliste ohne das Bezeichner-Merkmal) — eine Wahrheit
  für Tooltip und Auswahlanzeige.
- Gegenüberstellung je zählbarem Gewerk: Anzahlzeile wie bisher, dann
  EINE ZEILE JE KOMPONENTE („Komponente 1/2/…", bei einer schlicht
  „Komponente") mit dem Bezeichner je Version; die früheren
  Merkmalszeilen entfallen dort. Mouse-over einer Zelle zeigt die
  Merkmale mehrzeilig; die Auswahl einer Zelle zeigt sie in der
  Statuszeile („CS5800i (Stamm) — Hersteller: Bosch · Typ: … ").
  Konfigurationsblöcke Anlage/Gebäude bleiben Merkmalszeilen (inkl.
  B6-Gewerk-Guard).
- Drei-Schichten-Regel: neue Keys BK_SP_KOMPONENTE(_N) de+en über
  lokalen ResourceManager-Helfer (TUeb) mit deutschem Fallback.

Nachweise: transfer-T8/T8b (2 WPs vollzählig, Merkmalstext ohne
Bezeichner, mit Hersteller), Gesamtlauf + kd6 + Sweep siehe Zahlen des
Regressionslaufs.

**Nachtrag Booster-Bereinigung (29.08.2026, Nutzerfreigabe):** Die zwei mit
dem Import eingeschleppten Quell-Altlasten der Booster-Kette (Projekt 1043:
Solarthermie 3.775 EUR, Pufferspeicher 3.000,50 EUR, beide ohne
Anlagenzuordnung) wurden auf ausdrückliche Freigabe von der Produktiv-DB
gelöscht (Sicherung K:\backup_kenndaten_2026-08-29_vor_booster_bereinigung
.accdb; Vorab-Verifikation 2/6.775,5 exakt getroffen). Nachmessung über
alle Projekte: 0 lose Positionen anlagenfähiger Komponenten — der Bestand
ist wieder vollständig sauber; der T6-Exportfilter verhindert künftige
Einschleppungen.

### Projektdialoge: Löschen mit Mehrfachauswahl, Öffnen-Handhabung, Neues Projekt (02.09.2026)

Nutzerauftrag mit drei Screenshots: (1) der winzige Lösch-Dialog (ComboBox +
OK) durch einen Dialog analog zum Öffnen ersetzen — mit Mehrfachauswahl;
(2) den Öffnen-Bereich in der Handhabung verbessern, insbesondere
waagerecht blättern in der Projektliste; (3) die Seite „Neues Projekt"
(administrative Projektdaten) verbessern.

- EINE LISTE FÜR ALLES: `ProjektAuswahl` (UserControl hinter Öffnen,
  „Zuletzt geöffnet" und der Assistenten-Spalte) trägt jetzt beide Dialoge.
  Neu darin: (a) waagerechter Bildlauf — die Namensspalte wird so breit wie
  der längste Eintrag statt auf Sichtbreite gekappt; (b) Tooltip je Zeile
  mit Name, Kunde, Änderungsdatum und „Variante von …"; (c) Tastaturweg:
  Enter im Suchfeld übernimmt, Pfeil-ab springt in die Liste, Enter in der
  Liste wirkt wie Doppelklick; (d) Gruppierung bei Namenssortierung: jeder
  Stamm, darunter eingerückt („↳") seine Varianten (Tab_Variante einmal je
  Laden gelesen); (e) Mehrfachauswahl-Modus per Häkchen mit Stamm→Varianten-
  Kopplung, `GewaehlteProjekte` (Varianten vor Stämmen), `AlleSichtbaren`,
  Zählzeile „n von m Projekten · k ausgewählt".
- LÖSCHDIALOG `Form_ProjektDelete` neu (Code-Layout, Ressourcen PDLG_*
  de/en): Hinweiszeile, Liste im Häkchenmodus, „Alle sichtbaren auswählen"/
  „Auswahl aufheben", Sicherungs-Haken (vorbelegt an), „Löschen…" nur bei
  Auswahl aktiv, Rückfrage mit vollständiger Liste (Varianten gekennzeichnet,
  ab 12 gekürzt). `MenueCtrl.ProjektDelete` löscht je Projekt über den
  bewährten Weg (Anlagen, Projektzeile samt Kaskaden), zusätzlich die
  gespeicherten Ergebnisse (bisher Rückstand), setzt das aktive Projekt
  zurück, wenn es dabei war, und meldet die Anzahl (Fehler je Projekt
  gesammelt). Rückgabewert kompatibel zu Form_Start/MDIMainForm.
- NEUES PROJEKT `Wizard_Projekt`: Pflichtfeld-Sterne (Projektname,
  Klimaregion) + Hinweis im Sektionsbalken; Live-Hinweis unter dem
  Namensfeld bei Namensdoppel (Bestand einmal gelesen, keine DB-Zugriffe
  beim Tippen); Vorbelegung Bearbeiter (Windows-Benutzer) und Klimaregion
  (zuletzt aktives Projekt) — nur in leere Felder; Platzhaltertext in der
  Beschreibung; Fokus im Namensfeld. `Pruefe()` wird vom Assistenten beim
  „Weiter" gerufen (leer/doppelt/Klimaregion fehlt) — bisher erst beim
  Speichern viele Seiten später.
- UMGEBUNG: VS ist jetzt Version 18 (MSBuild-Pfad …/18/Community/…), die
  App heißt EPOS_Plan (Assembly/Prozess), Zielframework .NET 10 — der
  Prüfstand-Runner wurde entsprechend umgestellt.
- PRÜFSTAND NACH SQLITE-CUTOVER: Die Produktiv-DB ist seit 02.09. die
  `Kenndaten.sqlite` (GetDBPath biegt .accdb-Namen um) — Access-Testkopien
  sind für den Runner unbrauchbar; Testkopien liegen jetzt als SQLite
  (kd1test, kd6, transferA/B). Der Runner brauchte zusätzlich einen
  Resolver für die native `e_sqlite3.dll` (runtimes\win-x64\native), sonst
  scheiterte jeder DB-Zugriff still im Typinitialisierer von
  Microsoft.Data.Sqlite — Sweep/kd6 liefen dann zwar „grün", prüften aber
  eine alte DLL bzw. gar keine Datenbank. Sichtbeleg des Löschdialogs:
  K:\dump_Form_ProjektDelete.png (Liste im Offscreen-Dump leer, da das
  Laden erst beim Anzeigen läuft — Dump-Mechanik, kein Fehler).

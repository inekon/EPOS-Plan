# KD5 — Reiter „Ertrag/Bonus" (Protokoll)

Etappe KD5 des Konzepts Kostendialoge (Rev. 1.2, § 6, Entscheidungen FK5/FK7),
umgesetzt 26.08.2026 auf Branch `kostenformulare`.

## Umgesetzt

**`ucErtragBonus`** (Designer-fähig, Ä6) füllt den KD2-Platzhalterreiter des
Komponenten-Kostendialogs — reine ANZEIGE vorhandener Wahrheiten, keine
Zweitpflege:

- **BHKW (§ 6.1):** KWKG-Zuschlagstabelle nach Folie 10 (Einspeise-Tranchen
  8,0/6,0/5,0/4,4/3,4 · nachgerüstet 3,1; Sonderregel neue Anlagen ≤ 50 kWel
  16/8; Eigenverbrauchs-Staffel 4,0/3,0 mit Tatbestandshinweis) — die Werte
  kommen aus EXAKT den Katalogschlüsseln, die der `KwkgSatzRechner` liest;
  Förderdauer (30.000 Vbh) samt Jahresdeckel-Reihe (2021: 5.000 → 2030: 2.500)
  aus `GesetzKatalog.Reihe`; Steuerblock (§ 9 Abs. 1 Nr. 3 StromStG,
  § 53a Abs. 5 EnergieStG 4,42/40,35/19,60, § 9b mit Sockel 250 €/a);
  Sprungknopf `Form_Gesetzesparameter` (Anzeige aktualisiert nach Rückkehr).
  **FK7-Vermerk** im Reiter: Strompreis-Teil der Einspeisung bleibt
  Tarifstruktur (`Einsp_*` = KWK); projektbezogene Schalter (Tatbestand,
  Anlagenart, Pauschalmodus § 9, Kontingent-Override) bleiben je Anlage in der
  Wirtschaftlichkeit — eine Wahrheit je Größe.
- **Photovoltaik (§ 6.2):** Der Reiter öffnet DASSELBE
  `Form_PhotovoltaikVerguetung` wie der Knopf der Wirtschaftlichkeit
  (eine Vergütungswahrheit, V4/F7); im Admin-Kontext wählt eine Klappliste das
  Stammprojekt (`KostenVorlagenUebernahmeCtrl.Projekte`).
- **FK5:** Bei allen übrigen Komponenten wird die Reiterseite ENTFERNT
  (`Form_KostenKomponente.ErtragReiterSteuern`), nicht nur geleert.

24 neue `KDLG_ERTRAG_*`-Schlüssel de + en.

## Nachweise (kd5-Smoke, 14/14 PASS)

- R1–R4: `KwkgSatzRechner.Vorschlag` konsistent zur Anzeige-Quelle —
  50 kW (modernisiert) = Katalogwert 8,00; 100 kW = 7,00 (marginale Tranchen);
  300 kW = 5,5667; Neuanlage 30 kW = 16 (Sonderregel).
- A1–A6: Anzeige trägt alle Tabellenwerte, Sonderregel, Dauer/Deckel-Reihe,
  Steuerwerte; PV-Modus mit gefüllter Projektauswahl; Leerhinweis sonst.
- F1–F4: Reiter existiert nur bei BHKW/Photovoltaik (Wärmepumpe/Stromspeicher:
  entfernt), Wechsel in beide Richtungen.
- Regression kd2 23/23, kd3 20/20; Layout-Sweep 120 Formulare, 0 Befunde.
- Sichtbelege: `kd5_ertrag_bhkw.png`, `kd5_ertrag_pv.png` (Prüfstand-Dump).

## Bewusst offen

- Sichtprüfung Philipp (Reiterinhalt, Wortlaut der Hinweise).
- Die gemeinsame PV-Abnahme (KD5 ⇄ PV-Etappen) läuft mit der P6-Runde
  (INEKON-Referenzfall).

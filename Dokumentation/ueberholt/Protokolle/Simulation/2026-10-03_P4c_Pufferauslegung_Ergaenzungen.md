# Protokoll P4c — Pufferspeicher-Auslegung: Sitzungseingaben, Katalogverweis, Warntexte, Berichtsplatzhalter (Schemaschritt 179, 03.10.2026)

> **Berichtigung (03.10.2026, Anwenderentscheid):** Der in P4c gebaute Verweis `Tab_Gebaeude.ID_Konditionierungsvorlage` ist mit `30a9c31f` wieder entfernt; Schritt 179 trägt sechs Spalten, die Nutzung kommt allein aus `Tab_Konditionierungskalender.Nutzung` (Schritt 176). Testdatenbank neu aus 176 (`6d0b5d3c`). Einzelheiten im Protokoll P4d (#704), Abschnitt 3; die Angaben unten beschreiben den Stand des Agentenberichts.

Anwenderauftrag 03.10.2026 (Folgeaufträge 5 bis 8). Opus-Agent im Worktree, Zweig `claude/p4c-pufferauslegung` auf
`4793d349`; Merge `96f84243`, Kette `46178493`, Testdatenbank `a4d555a9`, Nachzüge `3766779a`. Statuszeile **#703**.

## 1 Was gebaut ist

| Commit | Inhalt |
|---|---|
| `b25cf77d` | `PufferAuslegungErgaenzungSchema` (179, `ProzessNutzungSchema.SCHRITT + 1`), reines DDL: `Tab_PufferAuslegung.Kriterien_Aktiv` (Bitmaske 0 … 511, Bit 0 = K1, Reihenfolge K1 K2 K3 K4 D1 D2 K9 K10 KV), `Sperrzeit_Expertenweg` 0/1, `Auslegungsheizlast_kW` ≥ 0, `Wohneinheiten` ≥ 0, `Anzeigestufe` SCHNELL/STANDARD/EXPERTE; `Tab_Gebaeude.ID_Konditionierungsvorlage` → `Tab_Konditionierungsvorlage_STAMM` SET NULL; `Tab_Pufferspeicher.ID_Stamm` → `Tab_Pufferspeicher_STAMM` SET NULL; Registrierung an allen Stellen; `KATALOG_SPALTEN` beim Duplizieren; Export/Import setzt die Katalogverweise leer; `PufferAuslegungErgaenzungSchemaTests` |
| `7b09e415` | (5) `Speichern(…, anzeigestufe)` schreibt abweichende Werte (sonst NULL); `Vorbelegen` liefert `Kriterien`, `KriterienBasis`, `Anzeigestufe`; `ParameterMitKriterien` im Kern; Bericht rechnet mit gespeicherten Schaltern; Hülle und Seite übernehmen Kriterien, Stufe, Heizlast, Wohneinheiten |
| `7a3c00ac` | (8) `KonditionierungCtrl.StandSchreiben` setzt `ID_Konditionierungsvorlage` bei neuer Vorlagenherkunft; Ableitung liest den Verweis (nach dem Merge: nach der Nutzung an der Kalenderkopie); `Uebernehmen(…, katalogsatz = true)` schreibt `ID_Stamm`; Schalter „Katalogsatz übernehmen“ (sichtbar mit Vorschlag, Vorgabe an); Pufferdialog zeigt „aus Katalog: <Bezeichner>“ |
| `3a17e8f5` | (7) `PufferWarnung` mit Klartext als `Textbaustein` `PA_<CODE>[_VARIANTE]_TEXT` (21 Schlüssel; PRAXISGRENZE und KEINE_REIHE je drei Varianten), `Text` bleibt deutscher Rückfall zeichengleich; Tooltip und Bericht lösen in der Sprache auf; `PufferWarntexteTests` |
| `d24ce8be` | (6) Vorlagenfeld `tabelle.pufferauslegung` (Kontext Stamm, Kapitel Projekt, Katalogfassung 11 → 12, Schalter `hat.tabelle.pufferauslegung`, `VF_TABELLE__PUFFERAUSLEGUNG`); gemeinsame `PufferauslegungPaare`; `WordKontext.Vorlagenfelder`: steht der Platzhalter in der Vorlage, schreibt der Baustein am Standardort nichts; alle zehn Vorlagen mit `Werkzeuge/Berichtsvorlage … alle` neu gebaut (zweiter Lauf byte-gleich), Messlatte `Vorlagenfeldkatalog_v12.txt`, `LIESMICH.md` nachgezogen |

## 2 Festlegungen

- `ID_Konditionierungsvorlage` gilt wie `ID_Gebaeude_Stamm` als Katalogverweis, nicht als Fachspalte
  (`GebaeudeRundlaufTests`, `ProjektkopienKatalogeTests`); kein Referenzgebäude bekommt einen Wert.
- Die Warnliste des Berichts zeigt den Klartext mit Zahlen; der Kurztext des Codes bleibt für Warnungen ohne Baustein.
- Merge-Nachzüge (`3766779a`): `PufferAuslegungHuelle.ProbelaufImHintergrund` über `Kulturweitergabe.Starten`
  (`ParallelitaetWacheTests`), `Tab_Nutzungsprofil_STAMM` in `Katalogfassung.Ausgenommen` (44 Stammtabellen, 12
  Ausnahmen), `PufferAuslegungSeite` 28 Eingabestellen (beide Übernahmeschalter), `PufferUebernahmeDaten(Neu, Bezeichner,
  Katalogsatz = true, SperrprofilSchreiben = false)`.

## 3 Testdatenbank 179

Ausgang 176 (origin), `tww_testkatalog_fiktiv.py` (24 Zeilen), Schemawerkzeug 176 → 179 (3 Tabellen, 7 Spalten);
`--trocken` „Schemastand 179“, alle Schritte „steht bereits“; `integrity_check` ok, `foreign_key_check` leer; Saat
`Tab_Nutzungsprofil_STAMM` 5, `Z_Nutzungsprofil` 26, Pufferparameter 148; STRICT-Tabellen 160 → 163
(`VorlageTests`). **82 345 984 Byte, LFS-SHA-256 `93d4066ad8af6e7fb6bdc7c86e312963a688385d521acf882097946263f71a57`**;
Nachtrag in `Referenzlaeufe/LIESMICH.md`; Basis R33 bleibt.

## 4 Nachweise

Agent: Kern-Filter, Windows-Schale 0 Fehler; Pufferauslegung 105/105, Hülle/Konditionierung/Parität/Duplizieren 52/52,
Vorlage/Bericht 943/943 nach Messlatte v12, UI Puffer 193/193, UI Vorlagenfeld/Bericht 283/283, Schema/Duplizieren/
Transfer/Konditionierung/Gebäude/Paketanhebung/Wachen 1842/1842 nach Kette und Testdatenbank; Referenzlauf CI-Sieben 7/7
PASS, 226 Dateien byte-gleich; SQL-Prüfer 2 295 Texte, 0 Fundstellen (Merge-Agent); `Auslieferungsvorlage.Tests` 47/47.
Wiki: Seite Pufferauslegung (Katalogsatz, Anzeigestufe, Kriterienschalter), Seite Berichtsvorlagen (Platzhalter
`{{tabelle.pufferauslegung}}`); je ein Logbuch-Satz (1.2.0.7).

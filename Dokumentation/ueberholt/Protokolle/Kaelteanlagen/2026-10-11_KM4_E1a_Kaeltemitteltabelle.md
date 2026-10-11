# Protokoll KM4‑E1‑a — Schemaschritt 216, Kältemitteltabelle und Zuordnung

**11.10.2026 · Sitzung Kälteanlagen · Welle E1‑a des Kältemittels als Stammdatum.** Grundlage:
[Fachkonzept](../../../aktuell/Kälteanlagen/Konzept_Kaeltemittel_Stammdatum_EPOS-Plan.md) 3.1, 3.2, 4.1, 4.2, 4.4, 4.6, 6,
9 (E123, KM4‑Q1 bis Q17 nach Empfehlung), 10; [Umsetzungskonzept](../../../aktuell/Kälteanlagen/Umsetzungskonzept_Kaeltemittel_Stammdatum_EPOS-Plan.md)
3.1. Schema, Register, Saat, Zuordnung, Testdatenbank; keine Oberfläche, kein Rechenweg.

## Was gebaut ist

- **Schritt 216 `KaeltemittelSchema`** (`EPOS.Kern/Allgemein/Update/`, `SCHRITT = VorheizenSchema.SCHRITT + 1`), drei
  Vorgänge (DDL ohne Fremdschlüssel, Saat, Zuordnung), wiederholbar; Leser: `SchemaMigration.Schritt_Kaeltemittel`,
  `Werkzeuge/Testdatenbankschema`, `TestDatenbank`; `SchemaStand.Zielversion` = 216; `Paketanhebung` Stufe 216 als Import.
- **`Tab_Kaeltemittel_STAMM`** (STRICT): `Code` (TEXT NOT NULL UNIQUE COLLATE NOCASE, 1–20 Zeichen), `Bezeichnung` (≤ 120),
  `Gruppe` (`HFKW`/`HFO`/`HFCKW`/`GEMISCH`/`NATUERLICH`/`SONSTIGE`), `GWP` (≥ 0), `GWP_Quelle` (≤ 120), `F_Gas` und
  `Natuerlich` (0/1), `Sicherheitsgruppe` (A1 … B3), `PFAS_Status` (`PFAS`/`BESTANDTEIL`/`KEIN`), `Status`
  (`GESICHERT`/`VORLAEUFIG`/`PROGNOSE`/`ABGEKUENDIGT`), `Quelle` (≤ 120), `Sortierung`, `ReadOnly`, dazu die Katalogspalten
  und der Teilindex. Wertemengen als `DbWerte.KAELTEMITTEL_*`; alle Fachspalten außer `Code` nullbar.
- **Verweis und Jahr:** `ID_Kaeltemittel` (INTEGER, nullbar, ohne Fremdschlüssel) an `Tab_Kaeltemaschine_STAMM` und
  `Tab_Kaeltemaschine`, letzte Stelle von `KaeltemaschineSchema.Fachspalten`; `Tab_Projekt.Inbetriebnahmejahr` (2000–2100).
- **Register:** `KMT` in Stufe 3 vor `KM`, Name `Code`, Schlüssel nach `Schluesselstamm` (`KMT:R1234ZE_E`); `KM` trägt
  `Katalogverweis("ID_Kaeltemittel", "Tab_Kaeltemittel_STAMM", "Code")`. Die Schritte der Kältemaschine prüfen und stempeln
  allein ihren eigenen Katalog (sonst fänden sie eine Datenbank vor 216 unvollständig). `KatalogRegistry`: Eintrag
  `KAELTEMITTEL` mit Verwendungsprüfung über die ID an beiden Kältemaschinentabellen, Sperre, nicht im Dublettendialog.
- **Saat:** achtzehn Codes, ausgeliefert, `VORLAEUFIG`, Sicherheitsgruppe leer.
- **Zuordnung:** Auslieferungssatz `KM:KAELTEMASCHINE_500_KW_WASSERGEKUEHLT_MIT_NASSKUEHLER` („R1234ze“) über den Schlüssel auf
  `R1234ze(E)`, danach exakte Treffer (Trim, ohne Groß/Klein); neue Prüfsumme für jeden zugeordneten Satz, dessen Summe
  vorher stimmte.
- **Nachzüge:** `ParameterVerwendung` (`ID_Kaeltemittel`, Dialog), Ressourcen `KABG_KATALOG_KAELTEMITTEL` und
  `KM_LBL_ID_KAELTEMITTEL` in beiden Sprachen, Vorlagenzahl P6 190 → 191.

## Testdatenbank

Gehoben aus 215 (`cc9222a8…`) mit `Werkzeuge/Testdatenbankschema`: 18 Codes, Katalogsätze mit ID 3, Projektkopien mit ID 3.
Zellvergleich der Bestandsspalten: nur `Tab_Applikation.SchemaVersion` und die Prüfsummen der Sätze 1 bis 3. Stand
**216**, `integrity_check` ok, `foreign_key_check` leer, **95 735 808 Byte, SHA-256 `a63c8e78…64e94`**.

## Abweichungen

- PFAS-Wert „enthält einen Bestandteil“ heißt nach Fachkonzept 4.1 `BESTANDTEIL` (die Saatliste schrieb `ENTHAELT`).
- `Gruppe`, `Bezeichnung` (neutraler Stoffname) und `Quelle` (Recherche und Vertrauensgrad) sind aus der Saatliste
  abgeleitet; R1233zd(E) steht als chlorhaltiges Olefin in der Gruppe `HFO`.

## Offen für E1‑b

Modell und Controller der Kältemaschine (`ID_Kaeltemittel` lesen, schreiben, Leer → NULL, `AusKatalogUebernehmen`),
`KaeltemittelCtrl`, Projektpaket (Ausnahme wie `ID_Stamm`, Nachtrag aus dem Text), Wache „Codes ⊆ Tabelle“,
`WikiProduktdatenWacheTests.Normbezeichnungen`.

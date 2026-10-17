# Protokoll KU3-4a — Kältemaschine als Anlage (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KU3-4a (E67, E68), ein Opus-Auftrag (154 Aufrufe), Commits `d220792`, `67965b6`, `b22535a`, `bab9e9b`, `0b43651`, `2060f71`, Merge `18f72df`. Statuszeile #716.
**Entscheid:** keiner neu beim Anwender; Entscheid der Orchestrierung zum Kaskadenplatz (Abschnitt 4). Schemaschritt 183, Basis R35 unverändert, Referenzlauf byte-gleich.

## 1 Auftrag

Die Kältemaschine (Katalog aus KU3-1, Rechenweg aus KU3-2) als Anlage führen: Typ, Anlagenzeile mit Anzahl, Rechenweg je Anlagenzeile, Wirtschaftlichkeit, Ergebnis je Maschine, Bericht. Der Erzeugerdialog folgt in KU3-4c.

## 2 Vorgehen

Ein Opus-Auftrag in sechs Commits (Schema 183, Controller und Rechenweg, Ergebnis, Wirtschaftlichkeit, Bericht, Tests); Merge nach `18f72df`. Die Testdatenbank wurde vom Anwender mit Stand 181 gepusht (`ff43435`, OID `2eea4775`) und von der Orchestrierung auf 182 (`e0813c0`, 84 836 352 Byte) und 183 (`18f72df`, 84 844 544 Byte, OID `3a87fb93`) gezogen; die LFS-Objekte gehen als Bundle an den Anwender.

## 3 Ergebnis

- **Schritt 183 `KaeltemaschineAnlageSchema`** (hängt an 182): `Tab_Energieanlagen.ID_Kaeltemaschine` (Verweis auf `Tab_Kaeltemaschine`, SET NULL) und `Kaeltemaschine_Anzahl` (INTEGER NOT NULL DEFAULT 1, CHECK ≥ 1); `Tab_Kaeltemaschine.Kuehl_Vorlauf` (REAL, −20…30) und `Kuehl_Hilfsstromanteil` (REAL, 0 ≤ x < 1); `Tab_ErgebnisKaeltemaschine` (STRICT, 13 Spalten). Saat wiederholbar (12 Zeilen): Typ 13 „Kältemaschine“ (`WizardItemClass.KM_TYP`), Kostenkomponente 11, Nutzungsdauer Gerät 15 a, Wartung 1,5 %, Instandsetzung 1,5 %, Standardvorlage Investition „Kältemaschine (Aggregat)“ 300 €/kW (`EUR_PRO_KW_LEISTUNG`) mit Rückkühlung/Zubehör, MSR, Montage, Planung, Standardvorlage Betrieb Wartung und Instandhaltung je 1,5 %; die Saat stellt den Katalogstempel `Kostenkatalog_Geaendert` zurück. Zielversion 183; STRICT-Zahl der Auslieferungsvorlage 167 → 168; Spaltenzahl `Tab_Kaeltemaschine` 15 → 17; Duplizieren versetzt `ID_Kaeltemaschine`, Ergebnistabellen reisen nach Präfix.
- **Rechenweg:** `KaeltemaschineAnlageCtrl` (Liste, Laden, Anlegen = Projektkopie + Anlagenzeile Typ 13, Pruefen, Speichern, Loeschen; die Projektkopie verschwindet mit, wenn keine andere Anlage sie führt). `SimulationControl.KaeltemaschinenVorbereiten` liest nur Anlagenzeilen: Anzahl teilt die Last gleich auf (Kälte, Strom, Kapazität als Vielfaches); Kaltwasser aus `Kuehl_Vorlauf`, sonst kleinste Stützstelle; Hilfsstromanteil nach K23 (ungültig verworfen mit Meldung); Kühlträger und Abrechnungsart wie die Wärmepumpe (E34). Projektkopie ohne Anlagenzeile rechnet nicht (Hinweis); gelöschtes Gerät einer Anlage gibt eine Warnung. Ergebnis je Maschine in `Tab_ErgebnisKaeltemaschine` geschrieben und gelesen (`ErgebnisModel.Kaeltemaschinen`, `ErgebnisCtrl`).
- **Wirtschaftlichkeit:** `TechnikPlanwertCtrl` mit Kältemaschine (je kW: Nennkälteleistung × Anzahl; `Modulkosten` × Anzahl, wenn gepflegt); `EndenergieAufloeser` Komponente 11 mit Kältestrom aus `Tab_ErgebnisKaeltemaschine` zum Strompreis des Projekts bzw. des Kühlträgers; Kostenvorlagen wählbar; Komponente 11 und Typ 13 zählen nicht zur Wärmegestehung. `ParameterVerwendung`: `Modulkosten` gerechnet.
- **Bericht:** sieben Kennzahlen `kaelte.km.*` („(sensibel)“), Block Kältemaschinen im Kälteabschnitt (`BausteineProjekt`, `BerichtTexte`), Erzeugertafel `tabelle.kaelteerzeuger` mit „(n ×)“.
- **Tests:** `KaeltemaschineAnlageSchemaTests` (3), `KaeltemaschineAnlageDatenbankTests` (2), `KaeltemaschineBerichtTests` (2); angepasst `KaeltemaschineDatenbankTests`, `KuehlungOberflaecheTests` (18 Kältekennzahlen), `KaeltemaschineSchemaTests`, `ErgebnisverweisKopieTests`.

## 4 Entscheide der Orchestrierung

- **Kaskadenplatz:** `Tool_1…4` ordnen nur die Wärmeseite, daher bleibt die Reihenfolge der Kälte nach Kühlkonzept 5.5 — freie Kühlung, Wärmepumpen im Kühlbetrieb, Kältemaschinen nach Anlagenzeilen; die Meldung `SIMENG_KAELTE_KM_REIHENFOLGE` ist angepasst. So belassen.

## 5 Nachweise

Kern-Filter 1 630 grün, 5 rot (Datenbank-Wachen der Datei 180 im Worktree; am Hauptbaum mit Datenbank 183 im Gate neu bewertet); UI 515 grün; Referenzlauf 8 CI-Projekte gegen R35 PASS byte-gleich; SQL-Dialekt 2 339/0; zwölf Ressourcen; Windows-Schale 0 Fehler.

## 6 Offenes

- Erzeugerdialog (Welle KU3-4c, läuft parallel).
- KU3-4d: Kühlträger mit eigenem Zähler in `Kaeltestromabrechnung` für die Kältemaschine (Anteile, Grund- und Leistungspreis, Emissionen je Zähler, Szenario-Mengen); Kostenstempel-Trigger an `Tab_Energieanlagen` kennt `Kaeltemaschine_Anzahl` nicht (eigener Schritt mit DROP/CREATE des Triggers); zwei Anlagenzeilen desselben Katalogsatzes teilen eine Projektkopie.
- KU3-4b: Referenzprojekt, Einfrierregel, Basis R36, Wiki, Logbuch, Hilfeanker; KU3-5 Kältespeicher.
- Beim Anwender: Bundle mit Testdatenbank 182/183 einspielen und pushen (LFS), Sichtabnahme Kostenvorlagen und Bericht; Gate und CI-Kennung nachtragen.

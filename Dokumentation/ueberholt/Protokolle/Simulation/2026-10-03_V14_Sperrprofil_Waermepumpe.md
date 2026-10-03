# Protokoll V14 — Sperrprofil der Wärmepumpe im Kern (Schemaschritt 177, 03.10.2026)

Anwenderentscheid 03.10.2026: „V14 Sperrprofil im Kern: nur für Wärmepumpe“, ohne Dimmung und Rechtsbezug (E-P12).
Opus-Agent im Worktree, Zweig `claude/v14-sperrprofil` auf `d9b37b11`; Merge `c13c263e`; Kette umgehängt in
`46178493`. Statuszeile **@@N2@@**.

## 1 Was gebaut ist

| Commit | Inhalt |
|---|---|
| `cd013f68` | `WaermepumpeSperrprofilSchema` (177, `KonditionierungNutzungSchema.SCHRITT + 1`): STRICT-Tabelle `Tab_Sperrfenster` (`ID_Energieanlage` FK CASCADE, `Von_h` 0 … 24, `Dauer_h` > 0 ≤ 24, `Wochentage` Bitmaske 1 … 127 Vorgabe 127, `Heizstab_gesperrt` 0/1 Vorgabe 1, `Reihenfolge`, Index); Registrierung an allen Stellen; `KINDER`-Eintrag über `ID_Energieanlage IN (SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = …)` und `FK_MAP`; Export über denselben Plan; Rettung der Fenster beim Speicherweg Löschen + Neuanlegen in `WizardCtrl` (`SperrfensterSichern/Wiederherstellen`) |
| `67908136` | `EPOS.Kern/Allgemein/Simulation/Sperrprofil.cs`: Stundenmaske je Modul aus Altfenster (Regel wie bisher, kein Übertrag) und Tabellenzeilen (Übertrag in den Folgetag, 31.12. in den 1.1., Wochentage aus `WochentagJan1` der Wärmerechnung); `SimulationWaermepumpe` und `Kaeltekaskade` fragen die Maske; Protokollzeile nur mit Fenstern |
| `6ace28e5` | Dialoggruppe „Sperrzeiten“ im Wärmepumpen-Anlagendialog (Fensterliste, Wochentag-Schalter, Heizstab mitgesperrt, Vorlagen keine / 2 × 2 h / 3 × 2 h); Altfenster als erste Zeile, beim Speichern in die Tabelle überführt (`Sperrung = 0`, `Heizstab_gesperrt = 0`); `SperrfensterCtrl`, plattformfreie `SperrfensterAbbildung` in `EPOS.UI.Daten`; KI-Feld `sperrfenster` („11-13; 17-19“); Pufferauslegung liest Tabelle vor Altfenster, Schalter „Sperrprofil an die Wärmepumpe schreiben“ (Vorgabe aus, schreibt an alle Wärmepumpen des Projekts); 21 Schlüssel de/en |
| `24016e94` | `SperrprofilTests` (15: Altfenster gleich, Übertrag, Wochentage, zwei Fenster, halbe Stunde, Wache „keine Sperre in den 16 Referenzprojekten“, Schreibweg, Duplizieren, Löschen + Neuanlegen, Lauf auf Kopie von 1007), `WaermepumpeSperrzeitenTests` (bunit), Schematest |

## 2 Festlegungen

- **Heizstab:** Beim Altfenster rechnete der Heizstab in der Sperrzeit weiter (`Heizstabphase` prüfte die Sperre nicht);
  das bleibt. Neue Zeilen mit `Heizstab_gesperrt = 1` sperren ihn mit; ein überführtes Altfenster wird mit 0 angelegt.
- Kein Referenzprojekt trägt eine Sperre; der Rechenweg ist für den Bestand byte-gleich (226 CSV mit `cmp`).
- Die Wärmepumpen-Hülle liegt in der Windows-Schale; die plattformfreie Abbildung liegt in `EPOS.UI.Daten`.

## 3 Nachweise

Agent: Kern-Filter, Windows-Schale, Schemawerkzeug 0 Fehler; Tests grün bis auf die erwarteten Wachen zur Kette
(176/177) und zur Testdatenbank, beide mit `46178493`/`a4d555a9` erledigt; Referenzlauf CI-Sieben 7/7 PASS, byte-gleich.
Wiki anzupassen: Seite des Wärmepumpen-Anlagendialogs („Sperrzeiten“), Seite der Pufferauslegung (Schalter). Logbuch
(1.2.0.7): „Für Wärmepumpen lassen sich mehrere Sperrfenster mit Wochentagen und Heizstabsperre pflegen.“

# Protokoll Etappe KD3 — Übernahme-Mechanik Stamm → Projekt (§ 8)

Stand: 25.08.2026 · Branch `kostenformulare` · Konzept: `Konzept_Kostendialoge_EPOS-Plan.md` Rev. 1.2, § 8/§ 14 · setzt auf KD1/KD2 auf

## 1. Umfang

| Baustein | Inhalt |
|---|---|
| `Controller\KostenVorlagenUebernahmeCtrl.cs` | UI-freie Übernahme-Mechanik: `AusVorlage` (materialisiert Vorlagenpositionen als Projektzeilen in `Tab_ProjektWerte` über die BESTEHENDEN Schreibwege `SetzeBetrag`/`SetzeBetragMitZusatz` — Ergebnisgleichheit zur Handeingabe per Konstruktion), `AusProjekt` (feldgleiche Kopie inkl. Best/Worst und Nutzungsdauern, Quelle Stammprojekt/Variante), `Projekte()`, `VorhandeneImProjekt()`. Regeln: vorhandene Zeilen bleiben IMMER unberührt (NurAnlegen-Muster); Herkunftsvermerk `VorlageID` je Zeile (§ 4.2, nie stille Kopplung); Nutzungsdauer der Vorlage füllt alle drei Szenariospalten. `StammIdSicher` legt fehlende Lexikoneinträge mit expliziter MAX+1-StammID an — bewusst NICHT `StammIdNeben` (dessen INSERT ohne StammID schreibt 0, dokumentierter Altbefund) |
| `Views\Kosten\Form_VorlagenUebernahme` (.cs/.Designer.cs) | Übernahme-Dialog (Designer-fähig, App-Design § 12): Zielprojekt-Klappliste, Quelle „aktuelle Vorlage/Variante" oder „anderes Projekt", Klartext-Vorschau (Quelle n Positionen / Ziel führt m — vorhandene bleiben unberührt), Ergebnis-Meldung; der Dialog rechnet und schreibt nicht selbst (Hausmuster `Form_BkUebernahme`) |
| `Form_KostenKomponente` | Fuß-Knopf „In Projekt übernehmen…" öffnet den Dialog mit Komponente/Kategorie/aktueller Variante |
| `Controller\BetriebskostenCtrl.cs` | **Befund + Fix:** `Betrag()` las jede Nicht-BETRAG-Bemessung ohne Menge+Satz als 0 — die neue absolute Art `JAHRESBETRAG` wäre mit 0 in die Rechnung gegangen. Erweitert: JAHRESBETRAG rechnet wie BETRAG (EingegebenerWert); die neuen %-Arten (Erzeuger-/Stromkosten) und €-je-Einheit-Arten (kWh th/el, kW, kWp, kWh Kapazität, m²) sind in die m×s- bzw. m×s/100-Gruppen aufgenommen (ergebnisneutral für Bestandsdaten — sie tragen diese Bemessungen nicht). `SatzEinheit()` liest die KD-Einheiten aus dem `BemessungKatalog` (eine Wahrheit) |
| Ressourcen/HilfeKontext | 3 neue `KDLG_UEB_*`-Schlüssel (de+en); `Form_VorlagenUebernahme` → Bereich „Kosten und Preise" |

Feldsemantik der Materialisierung (aus `BetriebskostenCtrl`/`ucKostenItem` abgeleitet):
absolute Bemessungen (BETRAG/JAHRESBETRAG) → Satz in `EingegebenerWert`, `Einheitpreis` leer;
satzbasierte → Satz in `Einheitpreis`, `EingegebenerWert` 0, `Menge` NULL (Betrag entsteht, wenn
die Bezugsgröße im Projektfluss gepflegt wird); Gruppe = „Betriebskosten VDI 2067" (Kategorie 2)
bzw. „Allgemein" (Kategorie 1); Kostenart/IstErloes aus der Vorlage.

## 2. Prüfungen (Testkopie, Projekte 19 „Wöhler WP" und 1006)

Runner-Smoke `kd3` — **20/20 PASS**: Variante mit gepflegten Sätzen (Vollwartung 0,05 €/kWh ·
Instandhaltung 5 % · Reserve 1.200 €/a) → Übernahme in Projekt 19: 11 Positionen
(angelegt+übersprungen), Herkunft `VorlageID` an allen Zeilen, Feldsemantik je Referenzzeile
bestätigt, Gruppe/Kostenart korrekt · Rechenweg-Äquivalenz (Reflection auf
`BetriebskostenCtrl.Betrag`): Reserve = 1.200 wie Handeingabe, %-Position ohne Basis = 0 ·
Zweitlauf 0/11 (Idempotenz) · Projekt→Projekt-Kopie 19→1006 feldgleich, Zweitlauf 0 ·
Dialog-Konstruktion · Aufräumen vollständig (Projekt 1006 wieder auf Ausgangsbestand).

Nebenprodukt: Der versehentliche Migrationslauf vor dem Smoke bestätigte die Idempotenz
ALLER 39 Schritte auf Stand 39 („bereits erledigt", Abschlussprüfungen „nichts zu tun").

## 3. Abweichung vom Konzeptwortlaut

§ 8 nennt als Anlass „erstes Öffnen des Komponenten-Kostendialogs im Projekt ohne Positionen".
Der Projektmodus des Komponenten-Dialogs entsteht erst mit KD6 (Anlagendialog-Aufrufe); bis dahin
ist der Fuß-Knopf „In Projekt übernehmen…" der Einstieg — die Mechanik (Quellenauswahl,
Klartext-Vorschau, Nur-Anlegen, Herkunft) ist vollständig die des § 8.

## 4. Offen / Übergabe

- UI-Sichtabnahme Philipp (Dialog + Knopf; übernommene Positionen erscheinen im bestehenden
  Kosteneditor `Form_Kosten` und in „Berichte & Kosten").
- Voller Wirtschaftlichkeits-Referenzlauf (Simulation) bleibt Teil der KD-Gesamtabnahme.
- **KD4** benötigt für den Emissionsteil die Etappen E1/E2 des (noch zur Abnahme stehenden)
  Emissionsfaktoren-Quellenwahl-Konzepts; Menü/Leistungspreis/Kostenprofil-Verlagerung sind
  davon unabhängig. **KD5** setzt die PV-Etappen P2–P5 voraus (nicht begonnen).

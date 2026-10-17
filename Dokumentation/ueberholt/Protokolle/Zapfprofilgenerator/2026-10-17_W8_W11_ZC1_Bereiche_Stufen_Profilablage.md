# Zapfprofil: Nutzungsbereiche, zwei Stufen, Profilablage (W8, W11, ZC1, 17.10.2026)

Protokoll der Wellen **W8** (ZC2, ZC3), **W11** (ZC6) und des Vorschlags **ZC1** des
[Konzepts Zapfprofil — Bereiche und Methoden](../../../aktuell/Konzept_Zapfprofil_Bereiche_und_Methoden_EPOS-Plan.md) (Statuszeile #1194).

**Rahmen.** Sitzung Zapfprofilgenerator Cloud, drei Worktree-Agenten, Integration auf dem Arbeitszweig
(Merges `1b7e7b02c`, `1c315f961`, `7dc8c2bcb`, `e9eb79aa2` von origin; Ressourcenkonflikte als Vereinigung).
Kein Schemaschritt, Testdatenbank unverändert, Referenzbasis R58 bleibt (kein Rechenwegeingriff). Annahmen bis
zum Anwenderentscheid: ZC‑Q1 B, ZC‑Q2 A, ZC‑Q3 B, ZC‑Q6 B.

## 1 Auftrag

Anwenderauftrag vom 17.10.2026 (Wortlaut im Konzept, Abschnitt „Anlass“): Nutzungszonen und Zone zu einem
Bereich machen, Nutzungsart, Personen und Bedarfsniveau direkt in der Zeile wählen, nur die Stufen Einfach
und Experte, das erzeugte Profil zum Katalog oder Projekt hinzufügen (Warnung bei Projekt).

## 2 Befund (Kurzfassung aus dem Konzept)

- F1 Kalender: kein Fehler im Datenfluss, die Wirkung nur nirgends benannt.
- F2 Topologie: wirkt allein in der Auslegung, so gebaut.
- F3 Eingaben der Stufe Erweitert: keine ist tot, Wirkung unterschiedlich und unbenannt.
- F4 „Zapfprofil erzeugen“: keine Ablage als Katalog- oder Projektsatz.
- F5 Stufen: reiner Ansichtszustand, Start immer Einfach.
- F6 Zonenblock: Tabelle und Karte „Nutzung“ getrennt.
- F7 Begriff: „Zone“ und „Nutzungszone“ in rund 212 Ressourcenwerten je Sprache.

## 3 Umsetzung

**W8 — ZC3 zwei Stufen** (`2e713d1b8`). Stufenwahl nur Einfach und Experte; der Altwert Erweitert bleibt im Enum
lesbar und wird zu Experte. Marker `AbExperte`, `ErstAbExperte`, `VergleichNurExperte`; Hinweisschlüssel
`ZPG_HINW_WEITERE_EXPERTE`, `ZPG_HINW_WEITERE_ERWEITERT` gelöscht; `ZPG_GRUND_DAUERLINIE`,
`ZPG_VERGL_NUR_ERWEITERT`, `KI_DLG_ZPG_STUFE_ERL` u. a. nennen Experte. Neuer Test „Die Stufenwahl hat genau zwei
Einträge“; Rasterprobe Z1/Z2 klickt Experte.

**W8 — ZC2 ein Block** (`42fa6ae6f`, `d4608966a`). Eine Zeile je Bereich: Wahlknopf, Klappliste Nutzungsart
(Bezugsart im Eintrag), Zahlenfeld Bezugsgröße mit Einheit, Klappliste Bedarfsniveau, MWh/a mit Zustand
„unvollständig“; bei Wohnungstabelle ist die wirksame Menge grau. Detailzeile unter der markierten Zeile mit
Name (Vorgabe „Bereich n“), „Katalog…“ (Experte), Herleitung, nächstem Schritt und Meldungen; die Karte
„Nutzung“ entfällt. `@key` je Zone; Fokus nach „hinzufügen“ in die Nutzungsart der neuen Zeile; eine
Fehleingabe hält das OK und nennt „Bezugsgröße, Bereich 2“. Spalten `table-layout: fixed` (Wahl = Touchziel +
12 px, Bezugsgröße 24 %, Niveau 20 %, MWh/a 4,5em, Rest Nutzungsart). Topologie, Anteil und Rechenweg stehen
bis W9 als leise Zeile in der Detailzeile (Experte). Messung im Browser bei 1088 und 400 px: Tabelle 478 bzw.
326 px, kein Querrollen, Felder 44 px, Zeilen 77 px. Neue Schlüssel `ZPG_FELD_BEREICH`, `ZPG_SP_NIVEAU`; neue Tests
`ZapfprofilDialogStufenTests.Bereiche.cs` (5).

**W11a — Kern und Hülle** (`df294e03c`, `237697c4b`, `885fb6e7d`, `0301f7116`). `ZapfprofilProfilsatz.cs` im Kern
bildet das Ergebnis auf zwölf Monatssummen (kWh/MWh/%) und ein Wochenprofil 168 h (Montag 0 Uhr zuerst,
Wochentag aus dem Raster der Klimaregion, Mittel je Wochenstunde, kW und %) ab, mit und ohne Zirkulation, die
Jahressumme bleibt erhalten. `ZapfprofilHuelle.Profilsatz.cs`: `ProfilsatzVorschau`, `AlsKatalogsatzSpeichern`
(Anwendersatz `Tab_Brauchwasser_STAMM`, ReadOnly 0, Kopf über `BrauchwasserStammCtrl.SaveHead`, Woche über
`TypProfilCtrl.SpeichernUnter`, eine `Vorgangsklammer`, Typname automatisch eindeutig) und `ZumProjektHinzufuegen`
(Projektkopie `Tab_Brauchwasser` ohne `ID_Stamm`, Zuordnung über `WizardCtrl.Add_Projekt_Brauchwasser`, Eingabeart
„woche“, Summe MWh, Name „Zapfprofil <Projektname>“ mit „ (2)“ …); der Rechenweg des Projekts bleibt.
`BestandswegHatZirkulation` aus `KonfigurationCtrl.NetzverlustvorgabeLesen` mit Hinweis `ZPG_PS_HINWEIS_ZIRK_DOPPELT`.
20 Schlüssel `ZPG_PS_*`; Tests `ZapfprofilProfilsatzTests.cs` (15) gegen 1045; die Trennungswache ordnet die
Datei als Bilanz ein (`106e82d8a`).

**W11b — Dialog** (`dfeaa46b8`, `ae0ad8b53`, Ressourcenfix `22ca531e3`). In der Schlussleiste des Generator-Blatts
„Als Katalogsatz speichern…“ und „Zum Projekt hinzufügen“, weich gesperrt ohne Vorschau oder bei Fehleingabe, ohne
Delegat kein Knopf. Überlagerung mit Schalter „mit Zirkulation“ (aus mit Hinweis bei Bestandszirkulation),
Jahressumme, Spitzenstunde. Katalog: Name (Vorgabe `NameProjekt`), Typ folgt dem Namen, Beschreibung. Projekt:
erst `WarnungProjekt`, dann „Weiter“. Erfolg als verfallender Banner, Ablehnung bleibt in der Überlagerung; leise
Zeile zum geltenden Rechenweg über der Leiste. Der Wirt `BedarfsProfileDialog` liest die neue Zeile beim Schließen
des Blatts (`ProjektzeileAngelegt`, Parameter `ZapfprofilProjektzeile`, Leseweg `ZapfprofilHuelle.Projektzeile`),
sonst löschte sein OK sie. Delegaten in `ZapfprofilHuelle.Gaben()`. Zehn neue Schlüssel (`ZPG_PS_TITEL_KATALOG`,
`_LBL_*`, `_BTN_SPEICHERN`, `_BTN_WEITER`, `_GRUND_FEHLEINGABE`); Tests `ZapfprofilDialogTests.Profilsatz.cs` (11),
`BedarfsProfileDialogTests` (+1), `KiMaskenabdeckungWacheTests` 56 statt 52 Eingaben.

**ZC1 — Begriff** (`24b40c126`, `2f1fa93b1`, `47e6b25e0`, `a8efb43a9`, `439a0921e`). 192 deutsche und 197 englische
Ressourcenwerte „Bereich/Nutzungsbereich“ statt „Zone/Nutzungszone“ (`ZPG_LBL_ZONENNAME` = „Bereichsname“). Bewusst
unverändert: Klimazone, die Zonentabelle des VDI-4655-Pakets (`ZPG_SATZ_NORMVEKTOR_*`, `ZPGT_*`), „Brauchwasserzone“
der Prozessauswertung (`PAUS_*`). Feste Rückfalltexte in `ZapfprofilTexte`, `ZapfprofilAuslegungTexte`, acht
Hüllendateien und `SimulationWaermebedarf.cs` („Bereich „…“ trägt 0“). Wiki-Quellen Brauchwasser-Zapfprofil und
Grundlagen ohne „Erweitert“, der Block als eine Zeile je Bereich beschrieben, Anker unverändert; drei Logbuch-Sätze
„Seit 17.10.2026“ in `Werkzeuge/WikiUpload/logbuch.wiki`. Schlüsselnamen, Bezeichner und Schema unverändert.

## 4 Abnahme

Vorprüfung auf dem Integrationsstand: 433 UI-Tests (Zapfprofil, Stilblatt, BedarfsProfile, KiMasken) und 761
Kern-Tests (Zapfprofil, Tww, Brauchwasser, Ressource) grün; nach ZC1 354 UI- und 836 Kern-Tests grün. Rasterprobe
Z1–Z8 erfüllt. Gate: <GATE>. Kein Rechenwegeingriff, deshalb kein neuer Referenzlauf.

## 5 Offen

- Beide Schreibwege schreiben sofort; eine Projektzeile bleibt auch nach Abbrechen des Bedarfsprofil-Dialogs, die
  Warnung nennt das nicht (Anwenderfrage).
- Bei 400 px sind die Klapplisten stark gekürzt (eigener Schritt).
- Anwenderentscheide ZC‑Q1 bis ZC‑Q6; bis dahin gelten die Annahmen nach Empfehlung.
- Welle W9 (ZC5, ZC4 a/c), W10 (ZC4 b), W12.
- Windows-Sichtabnahme (Statusdatei 4.1) und Wiki-Upload (Statusdatei 4.2).

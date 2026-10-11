# Protokoll KM4‑E3 — Oberfläche des Kältemittels: Klappliste und Lesezeilen, Inbetriebnahmejahr, Zulässigkeitszeile, Kachelhinweis, Verwaltung

**11.10.2026 · Sitzung Kälteanlagen · Wellen E3‑a, E3‑b und E3‑c des Kältemittels als Stammdatum.** Grundlage:
[Fachkonzept](../../Konzept_Kaeltemittel_Stammdatum_EPOS-Plan.md) (E123, E126);
[Umsetzungskonzept](../../Umsetzungskonzept_Kaeltemittel_Stammdatum_EPOS-Plan.md), Stufe E3.
Ohne Schemaschritt (`Tab_Projekt.Inbetriebnahmejahr` und `Tab_Kaeltemittel(_STAMM)` stammen aus Schritt 216, KM4‑E1);
Testdatenbank unverändert; Basis unverändert.

## Freigabe des Anwenders (11.10.2026)

- E3‑a wird **ohne Abstimmung mit der Sitzung „Dialoge und Korrekturen“** gebaut, im bestehenden Aufbau des
  Katalogdialogs der Kältemaschine aus KM5; der Wirt `KaeltemaschineKonfiguration` bleibt bis auf die Lesezeile
  „Zulässigkeit“ unverändert.
- **Hervorhebung der gewählten Zeile** im Verwaltungsdialog „Kältemittel“ auf Wunsch des Anwenders nachgezogen
  (`8067c4c8a`).

## Was gebaut ist

### E3‑b — Inbetriebnahmejahr, Lesezeilen, Zulässigkeitszeile, Kachelhinweis (`416f5c37a`, `870b4be29`, `612ffce6b`)

- **Kern:** `ProjektCtrl` liest und schreibt `Tab_Projekt.Inbetriebnahmejahr`; Kopfregel `JahrAusserhalb` (2000 bis 2100,
  benannte Meldung, nie gekappt); der Assistent schreibt das Jahr im Vorgang. `KaeltemittelProjektpruefung.PruefenAnlage`
  prüft eine Anlage (mit Ersatzcode der Wärmepumpe); `KaeltemittelZulaessigkeitszeile` liefert die Lesezeile, die
  Stammwerte eines Kältemittels und die Kachelzählung.
- **Oberfläche:** `ProjektKopfSeite` mit Feld „Inbetriebnahmejahr“ (KI‑Sicht ergänzt); Lesezeile der Stammwerte unter
  der Klappliste der Gerätegrenzen der Wärmepumpe (`WaermepumpeGeraetegrenzenFelder`, `WaermepumpeBivalenz`);
  Zeile „Zulässigkeit“ im `WaermepumpeAnlageDialog` und in `KaeltemaschineKonfiguration`; Hinweiszeile der Kachel
  „Kühlung und Kälteanlagen“ (`ErzeugerReiter`, `KuehlungKachelDaten`).
- **Neue Texte (de):**
  - Feld: „Inbetriebnahmejahr“, Platzhalter „leer: keine Zulässigkeitsprüfung“; Meldung „Das Inbetriebnahmejahr muss
    zwischen {0} und {1} liegen; leer bleibt die Zulässigkeitsprüfung der Kältemittel aus.“ (Titel „Inbetriebnahmejahr
    ungültig“).
  - Zeile „Zulässigkeit“: „Zulässigkeit des Kältemittels: ohne Inbetriebnahmejahr nicht geprüft“ · „… ohne Kältemittel
    nicht geprüft“ · „… nicht geprüft“ · „… Absorption — nicht geprüft“ · „… Bestandsanlage — Anhang IV nicht
    anwendbar“ · „Zulässigkeit des Kältemittels {0}: zulässig für das Inbetriebnahmejahr {1}“ · „Zulässigkeit des
    Kältemittels {0}: Warnung — nach {1} ab {2} nur Restbestand oder Ausnahme ({3}; Inbetriebnahmejahr {4})“ ·
    „Zulässigkeit des Kältemittels: für das Inbetriebnahmejahr {0} nicht prüfbar — {1}“.
  - Lesezeile der Stammwerte: „GWP {0}“, „F-Gas“ / „kein F-Gas“, „natürlich“, „Sicherheitsgruppe {0}“, „PFAS“ / „mit
    PFAS-Bestandteil“ / „kein PFAS“, „Status {0}“ (gesichert, vorläufig, Prognose, abgekündigt).
  - Kachel: „Kältemittel: {0}“ mit „{0} mit Hinweis“ und „{0} nicht prüfbar“.

### E3‑c — Verwaltung „Kältemittel“ unter „Daten & Import“ (`da1006f41`, `c9a5dbb6f`, `8067c4c8a`)

- **Kern:** Pflege im `KaeltemittelCtrl` nach Fachkonzept 7.6 — Auslieferungszeilen gesperrt bis „Schloss aufheben“
  (Katalogschlüssel und Prüfsumme bleiben), Code eindeutig ohne Groß/Klein, Code einer verwendeten Zeile fest, Löschen
  nur ohne Verwendung (`ID_Kaeltemittel` an `Tab_Kaeltemaschine` und `_STAMM`), Wertemengen aus `DbWerte` mit benannter
  Meldung.
- **Oberfläche:** `KaeltemittelDialog` (Liste mit neun Spalten und Schloss, Satzbereich im Formularraster, Fundstellen
  als Lesetext, Löschen nur ohne Verwendung), Hülle `KaeltemittelHuelle` (DTO `KaeltemittelDaten`), Fenster `KaeltemittelFenster` unter Windows,
  `AppWurzel` und `IosProjektQuelle` für iOS, Menüpunkt „Kältemittel…“ (`KMT_MENUE`), Seitenschlüssel `KAELTEMITTEL`,
  Hilfeziel Gerätekataloge, KI‑Sicht `KaeltemittelKiSicht` mit dreizehn Feldern. Die gewählte Zeile ist über eine
  dialoggebundene Regel `table.epos-kmt-liste` mit den Tokens der Fokuszeile der Katalogliste hervorgehoben (Fläche,
  linker Balken, Umriss im Kontrastmodus); die globalen `.epos-raster`-Regeln sind unberührt.
- **Neue Texte (de):** Titel „Kältemittel“; Einleitung „Die Kältemittelliste der Kältemaschinen und Wärmepumpen: GWP
  mit Fundstelle, Kennzeichen nach der F-Gas-Verordnung, Sicherheitsgruppe und PFAS-Status. Ausgelieferte Zeilen sind
  gesperrt; eigene Zeilen lassen sich anlegen, ändern und löschen, solange keine Kältemaschine sie verwendet.“; Spalten
  Code, Bezeichnung, Gruppe, GWP, Fundstelle GWP, F-Gas, natürlich, Sicherheitsgruppe, PFAS, Status, Fundstelle
  Kennzeichen, Sortierung; Knöpfe Neu, Löschen, Schließen; „Verwendet von {0} Kältemaschine(n) — der Code bleibt, die
  Zeile lässt sich nicht löschen.“, „Von keiner Kältemaschine verwendet.“, „Kältemittel „{0}“ löschen?“, „„{0}“ wird von
  {1} Kältemaschine(n) verwendet und lässt sich nicht löschen.“, „Den Code „{0}“ gibt es schon (Groß- und
  Kleinschreibung zählen nicht).“, „Die Datenbank ist nur lesbar — die Kältemittel lassen sich nicht ändern.“, „Die
  Kältemittel stehen auf diesem Gerät noch nicht zur Verfügung.“

### E3‑a — Klappliste und Lesezeilen im Katalogdialog der Kältemaschine (`1e4a58ae0`)

- Klappliste „Kältemittel“ aus `KaeltemittelCtrl.Liste()` mit „Code — Bezeichnung“; ein Bestandstext ohne Tabellenzeile
  erscheint als „nicht zugeordnet: …“. Lesezeilen unter der Klappliste: GWP mit Herkunft nach KM4‑Q17, Stammwerte,
  Abweichungshinweis ab 1 % und Schnellwahl „GWP aus Kältemittel übernehmen“; im Lesemodus (Auslieferungssatz) als
  Nachsatz ohne Knopf. Die Hülle liefert die Einträge, die ID setzt der Schreibweg des Kerns per Code-Abgleich; das
  KI‑Feld ist eine Wahl über den Code. Kern: `LesewerteOhneGwp`, `GwpWeichtAb`.
- **Neue Texte (de):** „— kein Kältemittel —“, „nicht zugeordnet: {0}“, „GWP {0} — aus der Kältemitteltabelle“, „GWP {0}
  — Geräteangabe“, „Das Geräte-GWP {0} weicht vom Tabellenwert {1} ab; die Zulässigkeitsprüfung nimmt den
  Tabellenwert.“, „Das Kältemittel steht nicht in der Kältemitteltabelle; die Prüfung nimmt das Geräte-GWP.“, Knopf „GWP
  aus Kältemittel übernehmen“ (Tipp „Setzt das Geräte-GWP auf den Wert der Kältemitteltabelle.“).

## Befund (nur gemeldet)

- `PlatzhalterkatalogDialog` trägt an der gewählten Zeile kein `aria-selected`; dort nicht geändert, gehört der Sitzung
  „Dialoge und Korrekturen“.

## Gate

- **Vor der Zusammenführung** (Zweig `8067c4c8a`): EPOS.UI.Tests 8968 grün, EPOS.Kern.Tests gefiltert 2737 grün.
- **Erste Zusammenführung** mit origin (`343f53179`, #983 Aufräumen, BOM-Hotfix; ohne Konflikt), Gate
  `gate_rest_linux.sh`: EPOS.UI.Tests 8968, KiKern.Tests 553, SpeicherEngine.Tests 397, SpeicherPlanung.Tests 27
  (+1 übersprungen), Dokumentationswachen 35, Windows-Schale 0 Fehler, Designer unverändert, SQL-Dialekt 0
  Fundstellen, Werkzeugtests grün (Formularkarte 124, Auslieferungsvorlage 62, Gebäudevergleich 24,
  Zapfprofilvalidierung 39), BOM in Markdown keine, Konfliktmarker keine, Referenzlauf 30/30 gegen
  `2026-10-11_R52_Vorheizen` `GESAMT: PASS` (CSV byte-gleich 979/979); EPOS.Kern.Tests voll 13 671 grün (7 übersprungen).
- **Zweite Zusammenführung** mit origin (`7481c743a`, #984 bis #989): Konflikte in beiden `.resx` (beide Seiten,
  Designer neu erzeugt), Menüzählung (Fernwartung und Kältemittel addiert: 80 Punkte, 65 handelnde, Anwenderbaum
  72/58, auch `EntwicklerbereicheWacheTests`), `Seitenschluessel.Alle`, Stilblatt (Kontrastregel: Selektor der
  Kältemittelliste vor dem der Zweispaltenauswahl), `WaermepumpeAnlageFelder.razor` (Umzug AW2, Zulässigkeitszeile
  mitgenommen). Danach EPOS.UI.Tests 9002, KiKern.Tests 553, EPOS.Kern.Tests gefiltert 2397 grün, SQL-Dialekt 0,
  Kern-Filter und Windows-Schale 0 Fehler.
- **Dritte Zusammenführung** mit origin (`555db3074`, #990 bis #992, SA2 Schemaschritt 217, Testdatenbank von
  origin): Konflikt in `WaermepumpeAnlageFelderTests` (Testblöcke KM4‑E3‑b und KT‑8a, beide behalten). Danach
  EPOS.UI.Tests 9067, KiKern.Tests 553, EPOS.Kern.Tests gefiltert 3292 grün (1 übersprungen), SQL-Dialekt 0,
  Kern-Filter und Windows-Schale 0 Fehler, Designer unverändert. Der Referenzlauf aller Projekte wurde nach der
  ersten Zusammenführung gefahren; E3 berührt den Rechenweg nicht.

## Offen

- **E4:** Wiki, Update-Logbuch, Konzepte „wie gebaut“.
- **Windows-Sichtabnahme** nach der Liste in der Statusdatei (Offen, „Nach #993 (KM4‑E3)“).

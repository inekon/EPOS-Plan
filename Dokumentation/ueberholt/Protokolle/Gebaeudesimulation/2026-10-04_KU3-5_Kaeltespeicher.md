# Protokoll KU3-5 — Kältespeicher (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KU3-5 (E68), ein Opus-Auftrag (126 Aufrufe), Commits `bb1fd2a`, `3fe623f`, `f27193b`, `adfe61c`, Merge `191100c`. Statuszeile #718.
**Entscheid:** keiner neu beim Anwender; offen ist der Entscheid zur freien Kühlung über die Sole der Wärmepumpe (Abschnitt 5). Entscheid der Orchestrierung: Die Entladung läuft in jeder Stunde mit Vorrat, nicht nur außerhalb der Ladephase.
**Schemaschritt 184 nicht gebraucht:** `Tab_Pufferspeicher.Verwendung` hat keine Werteliste, `Entladung_Kuehlung` bestand. Basis R35 unverändert.

## 1 Auftrag

Den Pufferspeicher als Kältespeicher der Kaskade rechnen (Kühlkonzept 4.6): Persistenzwert, Lade- und Entladeweg im Kältekreis, Ergebnis, Warnkriterien, Dialog.

## 2 Vorgehen

Ein Opus-Auftrag in vier Commits (Rechenweg, Datenbank und Ergebnis, Dialog und Hülle, Papiere); ein konfliktfreier Merge; Statuszeile und Papiere aus der Orchestrierung.

## 3 Ergebnis

- **Persistenz:** `DbWerte.PSP_VERWENDUNG_KAELTE = "Kaelte"` (Lesen hebt „Kälte“ darauf). Jeder Projektpuffer mit dieser Verwendung ist Kältespeicher der Kaskade, ohne Senkenzeile; alle Kälteerzeuger laden ihn. Er steht nicht in der Wärmeordnung (`SimulationControl.Kaeltespeicher()` statt `AlleSpeicher()`; die Registry nimmt ihn mit Warnung aus einer Wärmesenke).
- **Kapazität:** Volumen · 1,16 · (Rücklauf − Vorlauf) / 1000; bei leerem oder vertauschtem Paar Vorgabe 6/12 °C mit Hinweis. Einschichtig ohne Temperaturweg (`T_oben_*` NULL); der Bereitschaftsverlust des Bestands ist ein Wärmeeintrag anteilig zum Füllstand.
- **Entladung:** in jeder Stunde mit Vorrat nach der freien Kühlung und vor Wärmepumpe und Kältemaschine, auf `Kanal.KUEHLUNG`.
- **Ladung:** nur an Kühltagen in der Ladephase (Hysterese `Schwelle_Ein`/`Schwelle_Aus` wie beim Wärmepuffer, der Lauf beginnt leer); Ladewunsch bis zur Abschaltschwelle, begrenzt durch die Ladeleistung, als Zusatzlast hinter dem Raumbedarf — nur Überschusskälte lädt. Die Ladekälte steht in Kälte und Strom des Erzeugers, nicht in der Deckung. Deckungsprobe: Erzeugerkälte + Entladung − Ladung = Deckung. Ohne Speicher bleibt die Stundenschleife unverändert.
- **Ergebnisse:** `Tab_ErgebnisPufferspeicher` je Kältespeicher (Verwendung `Kaelte`, Ladung, Entladung, Verluste, Vollzyklen; `Entladung_Kuehlung` nur beim Kältespeicher belegt, zurückgelesen); `aggregate.csv` `Kaeltespeicher[i].ID_Pufferspeicher`, `.KapazitaetKwh`, `.EntladungMwh`, `.LadungMwh`, `.VerlusteMwh`, `.Vollzyklen` nur mit Kältespeicher; Laufprotokoll mit Hinweis und Warnungen (ohne Kälteerzeuger; Kühlung aus → nicht gerechnet); Ergebnisanzeige Rolle „Kältespeicher“.
- **Warnkriterien:** `KAELTESPEICHER_OHNE_ERZEUGER`, `KAELTESPEICHER_OHNE_KUEHLUNG` (weich), `Set_BedientKanal(KUEHLUNG)`; `SpeicherPruefen` überspringt den Kältespeicher.
- **Dialog** `PufferSpProjektDialog`: vierte Nutzung „Kälte“ (Id 3, schließt die Wärmenutzungen aus), Felder „Kaltwasser-Vorlauf/-Rücklauf“ (kalt unter warm oder leer), Hinweiszeile, keine Entladeposition; die Hülle schreibt `Kaelte` ohne Wärmeflag. KI: `nutzung_kaelte`; 15 Ressourcen.
- **Behobener Bestandsfehler:** `PufferSpCtrl.KlassenSetBestimmen` hob einen Puffer ohne Wärmeflag still auf Heizung; neu `KlassenSet.Kaelte`. Der Sperrtext „Ein Kältespeicher wird nicht gerechnet“ existierte im Dialog nicht.
- **Kühlkonzept:** 4.6, 5.5 und 7.4 vom Agenten nachgezogen.

## 4 Nachweise

Tests `KaeltespeicherRechenwegTests` (6), `KaeltespeicherDatenbankTests` (2: Projekt 1017 mit knapper Kältemaschine und 20 000 l — Entladung > 0, Unterdeckung sinkt, Deckungsprobe; Warnkriterien an 1030), drei bunit. Kern-Filter 1 254 grün, 4 rot (`FachspaltenRettungTests` aus KU3-4a, Nachzug läuft); UI 938/938; Schale 0 Fehler; SQL-Dialekt 2 341/0; Designer ohne Befund; Referenzlauf der 8 CI-Projekte gegen R35 byte-gleich.

## 5 Abweichungen und Offenes

- Berichtsabschnitt Kältespeicher offen: Er hängt an `tabelle.speichertemperaturen` und fällt ohne `T_oben` heraus.
- Füllstandsganglinie offen: Der Navigator liest `AlleSpeicher()`.
- Keine Lade-Prioritäten; die Reihenfolge folgt `Entladeprio`.
- **Offen beim Anwender:** Sichtabnahme des Speicherdialogs (Nutzung Kälte); Entscheid, ob die freie Kühlung über die Sole der Wärmepumpe gebaut wird.
- Folgewellen: KU3-4a-Nachzug (27 rote Tests der Saat 183 und Kennzahlen), KU3-4d (Kältestromabrechnung, Stempeltrigger, geteilte Projektkopie, Berichtsabschnitt und Füllstandsganglinie des Kältespeichers), KU3-4b (Referenzprojekt, Basis R36, Wiki, Logbuch, Hilfeanker).

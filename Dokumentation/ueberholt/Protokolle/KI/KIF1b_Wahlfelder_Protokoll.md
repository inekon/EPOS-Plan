# KI‑F1b — Jedes Eingabefeld ist setzbar: Feldtyp „Wahl" und tolerante Feldnamen (Protokoll, 21.09.2026)

Statuszeile #420 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(KI‑D‑Q6, Stufe S4); Vorgänger [`KIF1_Erzeugermasken_Protokoll.md`](KIF1_Erzeugermasken_Protokoll.md) und
[`KIF2_Simulationskonfiguration_Protokoll.md`](KIF2_Simulationskonfiguration_Protokoll.md).
Zweig `ki-f1b`, Commits `b194119a`, `6c24ac37`, `40dd3633`, `0e840139`, `82e802db`, `d23b4420`; Merge `0efd7dc0`.

## Anlass und Entscheid

Auf der Maske „Heizkessel" lehnte `feld_setzen` den vom Modell geratenen Feldnamen
„vorlauftemperatur" ab („bekannt sind anlage, ruecklauf, vorlauf"); erst der zweite Versuch mit
„vorlauf" setzte den Wert. Außerdem fehlten den freigegebenen Masken alle Auswahlfelder, weil der
Katalog nur Ganzzahl, Zahl, Text und Wahrheitswert kannte. Der Anwender entschied (KI‑D‑Q6): Jedes
Eingabefeld einer freigegebenen Maske ist setzbar, Auswahlfelder über den angezeigten Text.

## Mechanik

**Feldtyp `Wahl`** (`KiParameterTyp.Wahl`, KiKern). Ein Wahl-Feld deklariert nur seine Art; die
Einträge (`KiWahleintrag` mit Schlüssel und Text) liefert die Maske bei jedem Zugriff über einen
Delegaten am `KiFeldzugang` bzw. an der `KiFeldsammlung` — Gerätelisten hängen am Hersteller,
Energieträger am Gewerk. Zwei Wege zur Eintragsquelle, aufgelöst in `KiMaskenanmeldung`: die
Begleiteigenschaft `<Eigenschaft>Wahl` am Daten-Objekt oder an der Sichtklasse (Regelfall, kostet
im Dialog keine Zeile), ersatzweise ein in `Fuer(…, params (Feld, Eintraege)[])` übergebener
Lieferant für Listen, die nur der Dialog kennt. `Pruefe(maske, typ, wahlquellen)` und der
Katalogwächter melden jedes Wahl-Feld ohne Quelle. Ein Aktionsparameter kann keine Wahl sein.

**Eine Namensregel** (`KiKern/KiWahl.cs`) für den Wert einer Wahl wie für den Namen eines Feldes:
exakter Schlüssel → gefalteter Schlüssel → Anzeigetext → eindeutiger Anfang in beide Richtungen →
eindeutig enthaltener Teil; gefaltet werden Groß- und Kleinschreibung, Umlaute (ae, oe, ue, ss),
Unterstrich und Leerraum. Die erste Stufe mit Treffern entscheidet; mehrere Treffer bleiben
mehrdeutig und werden nicht geraten — die Absage nennt die Kandidaten. Die Suche der gemeinten
Maske läuft weiter zuerst buchstabengetreu und erst dann tolerant.

**Setzen und Lesen.** `KiFeldwandler` setzt den getroffenen Schlüssel im Typ der Zieleigenschaft
(int, long, short, decimal, double, string, enum, nullable); kein Treffer und Mehrdeutigkeit sind
zwei benannte Absagen (Aufzählung höchstens 30 Einträge, sonst die Zahl). `dialog_lesen` zeigt
„Text (Schlüssel)" und die Einträge, `dialog_parameter_erklaeren` die Einträge, der
Bestätigungsblock beide Seiten als Text. Die Ergebnis- und Protokollzeile vermerkt die Auflösung
(„Feldname aufgelöst: vorlauftemperatur → vorlauf"). Wahrheitswerte nehmen auch an/ein/aus,
wahr/falsch.

## Nachzug der Masken

Katalog 181 → 218 Felder: 37 neu (19 Zahl, Text, Schalter; 18 Wahl), 18 bestehende Felder von
Aufzählung, Ganzzahl oder Text auf Wahl umgestellt — 36 Wahl-Felder über 19 Masken. Energieträger
an Heizkessel, BHKW, Stromspeicher, Photovoltaik, Wärmepumpe Anlage und Komponentenkonfiguration;
Bodentyp (Katalogschlüssel, nicht Listenplatz), Puffer, Profil, Speicher (Gruppenköpfe gefiltert),
Preisreihe; Betriebsarten, Typ, Leistungsstufen, Aufstellung, Baujahr, Klimazone, Ziel,
Bedarfsart, Ladeprioritäten, Entladepriorität, Betriebsziel, Bemessung als Wahl; Katalogmasken um
Name, Hersteller, Beschreibung, Brennwert, Speichertyp, Bereitschaftsverluste; die Nutzung der
Pufferverwaltung als drei Wahrheitswerte, ihre drei Entnahmehöhen mit neuen
Beschriftungsressourcen (der Befund aus #419 ist damit erledigt). Feste Listen stehen je einmal
öffentlich (`WaermepumpeStammFelder.TYPEN` und andere, `WaermepumpeKonfiguration.BETRIEBSARTEN`,
`SpeicherFlottenBetriebEditor.Betriebsziele`), Klappliste und Assistent können nicht
auseinanderlaufen.

## Außen vor, mit Grund

Tabellen mit eigenem Editor (Kennlinien der Wärmepumpe, Monats-, Tages- und Stundenwerte des
Quellprofils, Rainflow- und Kostenfelder je Speichereinheit, Endenergie- und Ersatzraster der
Kostenverwaltung); Ladevorgänge statt Feldwerten („Aus Katalog" der Pufferverwaltung, Komponente,
Kategorie und Variante der Kostenverwaltung); Modul und Wechselrichter eines PV-Strangs (die Zeile
trägt Id und Name, gesetzt wird über den Übernahmeweg des Dialogs); drei Projekteinstellungen aus
Anlagenmasken (Extrapolationsschalter der Wärmepumpe, Auslegungstemperatur kalt und heiß der
Photovoltaik — private Felder, die in `Tab_Einstellungen` schreiben; Objektname zu entscheiden);
die Stationen 2–4 der Stromspeicher-Auslegung (eigener Auftrag; `suchmethode` bleibt nur lesbar,
`feinraster` steht ohne Bedienelement im Katalog); Mengen von Verweisen und Bedienhandlungen
(Parallelverbund, Rang, Zeilen-, Reiter- und Schrittwahl, Herstellerfilter, Aufklapper „Alle
Daten").

## Zahlen und Abnahme

39 neue Ressourcenschlüssel je Sprache, 50 neue Tests. Worktree: Kern-Filter 0 Fehler, 10 158
Tests grün (1 übersprungen), Windows-Schale 0 Fehler. Gate im Hauptbaum auf `0efd7dc0`: siehe
Statuszeile #420.

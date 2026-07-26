# Hydraulikschemata: BHKW + Spitzenlastkessel mit Pufferspeicher

Übersicht der Einbindungsvarianten für Blockheizkraftwerke (BHKW) mit
Spitzenlastkessel. Erstellt für die Software Wärmeplan.
Stand: 17.07.2026 · Schemadarstellungen, keine Ausführungsplanung.
Schemata 5–9 nach ASUE-Broschüre „Einbindung von kleinen und mittleren
Blockheizkraftwerken / KWK-Anlagen" (Hydraulik – Elektrik – Regelung).

Allgemeine Auslegungsgrundsätze:

- Das BHKW deckt die **Grundlast** (Ziel: 5.000–7.000 Vollbenutzungsstunden/Jahr, mind.
  4.000 h/a) und wird dafür bewusst kleiner als die Gebäudeheizlast dimensioniert
  (häufig 10–30 % der Heizlast, ca. 50–70 % der Jahreswärmearbeit; Faustwert ASUE:
  ca. 1/3 der Gesamtwärmeleistung).
- Der **Spitzenlastkessel** übernimmt Lastspitzen und dient als Redundanz; er wird meist auf
  die volle Heizlast ausgelegt. **Brennwertkessel immer parallel** zum BHKW einbinden,
  nie in Reihe dahinter (sonst geht der Brennwerteffekt verloren).
- Der **Pufferspeicher** verlängert die BHKW-Laufzeiten, reduziert Taktung und ermöglicht
  stromorientierten Betrieb. Faustwert: 60–100 l je kW thermischer BHKW-Leistung
  (Mindestanforderung KWKG-Förderung beachten). Der Puffer darf **nicht vom Kessel
  geladen** werden.
- Der **BHKW-Rücklauf** sollte unter ca. 70 °C liegen, damit die Motorkühlung die Wärme
  vollständig auskoppeln kann; typische Netztemperaturen 70–80 °C Vorlauf / 50–60 °C Rücklauf.
- **Pumpen- vs. Ventillösung** (ASUE): Pumpen setzen sich nicht fest; Ventile sparen
  Pumpenstrom, können aber festsitzen. Oft sind drehzahlgeregelte Pumpen sinnvoll.

---

## Schema 1 – Parallelschaltung über Pufferspeicher

**Datei:** `BHKW_S1_Parallelschaltung_Puffer.svg`

BHKW und Spitzenlastkessel laden beide den Pufferspeicher; sämtliche Verbraucher werden
ausschließlich aus dem Puffer versorgt. Der Puffer wirkt als zentrale hydraulische
Entkopplung und Drehscheibe.

- **Vorteile:** einfachste Regelung (Speicher-Solltemperatur), robuste Hydraulik, Erzeuger
  vollständig entkoppelt, gute Erweiterbarkeit (z. B. Solarthermie).
- **Nachteile:** der gesamte Kesselvolumenstrom läuft durch den Puffer — höhere
  Speicherverluste und Gefahr der Schichtungszerstörung bei Spitzenlast.
- **Typischer Einsatz:** kleinere Anlagen (Mehrfamilienhaus, Gewerbe), wenn Einfachheit und
  Betriebssicherheit im Vordergrund stehen.

## Schema 2 – BHKW mit Puffer, Kessel direkt eingebunden

**Datei:** `BHKW_S2_Kessel_direkt_parallel.svg`

Das BHKW arbeitet auf den Pufferspeicher (lange Laufzeiten); der Spitzenlastkessel speist
über Rückschlagventile direkt in den Netzvorlauf und wird nur bei Bedarf zugeschaltet. Das
Kesselwasser durchströmt den Puffer nicht. (Entspricht der indirekten Einbindung über
einen Pufferspeicher, ASUE Varianten 5/6.)

- **Vorteile:** Puffer bleibt dem BHKW vorbehalten (saubere Schichtung, maximale
  BHKW-Laufzeit), geringe Speicherverluste, Kessel reagiert schnell auf Spitzen.
- **Nachteile:** etwas aufwendigere Regelung (Zuschaltkriterium Kessel, Rückschlagklappen
  erforderlich), Volumenstromabgleich zwischen den Einspeisepunkten nötig; träge
  Wärmeübergabe, da alle BHKW-Wärme erst durch den Speicher läuft.
- **Typischer Einsatz:** häufigste Praxislösung in mittleren und größeren Heizzentralen,
  Nahwärmenetzen und Objekten mit ausgeprägten Lastspitzen.

## Schema 3 – Reihenschaltung: Kessel als Nachheizstufe

**Datei:** `BHKW_S3_Reihenschaltung_Nachheizung.svg`

BHKW und Puffer liegen im Rücklauf/Vorlaufpfad vor dem Kessel: Der Netzvorlauf wird zunächst
aus dem Puffer entnommen und durchströmt anschließend den Spitzenlastkessel, der bei Bedarf
auf Solltemperatur nachheizt. Ein 3-Wege-Ventil führt den Volumenstrom bei ausreichender
Puffertemperatur am Kessel vorbei (Bypass). (Entspricht ASUE Variante 3 –
Rücklauftemperaturanhebung mit Pufferspeicher.)

- **Vorteile:** BHKW-Wärme wird immer vollständig genutzt (Vorrang systembedingt),
  Kessel arbeitet nur als "Booster" mit kleiner Temperaturspreizung; ideal bei hohen
  geforderten Netzvorlauftemperaturen.
- **Nachteile:** Kessel muss für den vollen Netzvolumenstrom ausgelegt/durchströmbar sein,
  Druckverlust im Vorlaufpfad, Bypassregelung erforderlich; **nicht für Brennwert- und
  Niedertemperaturkessel geeignet** (Reihenschaltung hebt das Kessel-Temperaturniveau an).
- **Typischer Einsatz:** Netze mit hoher Vorlauftemperatur (Prozesswärme, Altbaunetze,
  Krankenhäuser), wenn das BHKW die Solltemperatur allein nicht sicher erreicht.

## Schema 4 – Einbindung über hydraulische Weiche

**Datei:** `BHKW_S4_Hydraulische_Weiche.svg`

BHKW (mit Puffer) und Spitzenlastkessel speisen über eine gemeinsame Vorlaufleitung auf eine
hydraulische Weiche; dahinter versorgen die Sekundärpumpen die Heizkreise. Erzeuger- und
Verbraucherseite sind vollständig entkoppelt.

- **Vorteile:** Erzeuger- und Netzvolumenströme völlig unabhängig, unkritisch bei vielen
  bzw. wechselnden Verbraucherkreisen, einfache Erweiterung um weitere Erzeuger.
- **Nachteile:** Gefahr der Rücklauftemperatur-Anhebung durch die Weiche bei schlechtem
  Abgleich (schadet BHKW-Auskopplung und Puffer-Schichtung), zusätzliche Mischverluste.
  Beim Einsatz eines Puffers als Weiche: Kurzschlussströmung Vorlauf→Rücklauf führt zum
  Takten des BHKW (ASUE-Hinweis).
- **Typischer Einsatz:** größere Heizzentralen mit mehreren Erzeugern und vielen
  Heizkreisen, Sanierungen mit unklaren Netzverhältnissen.

## Schema 5 – Reihenschaltung ohne Pufferspeicher (Rücklaufanhebung)

**Datei:** `BHKW_S5_Reihenschaltung_ohne_Puffer.svg` · ASUE Variante 1

Einfachste BHKW-Einbindung: Das BHKW liegt im Rücklauf vor dem Heizkessel und hebt die
Rücklauftemperatur an; der Kessel heizt in Reihe auf Solltemperatur nach. Es gibt keinen
Pufferspeicher — das BHKW kann nur laufen, wenn gleichzeitig Strom und Wärme gebraucht
werden (bzw. Strom eingespeist wird). Rückschlagklappen verhindern Fehlzirkulation bei
Stillstand.

- **Vorteile:** minimaler hydraulischer Aufwand, geringe Investition, BHKW-Wärme wird
  systembedingt immer zuerst genutzt.
- **Nachteile:** kein zeitlicher Ausgleich zwischen Strom- und Wärmebedarf (geringere
  Laufzeiten), **nicht für Brennwert-/NT-Kessel** (Rücklaufanhebung verhindert
  Brennwertnutzung), Kessel-Vorlaufsollwert mind. 5 K unter BHKW-Vorlauf einstellen,
  Kessel wird auch im Stillstand durchströmt (Bereitschaftsverluste).
- **Typischer Einsatz:** Bestandsanlagen mit konventionellem Kessel und hoher
  Gleichzeitigkeit von Strom- und Wärmebedarf.

## Schema 6 – Parallelschaltung ohne Pufferspeicher

**Datei:** `BHKW_S6_Parallelschaltung_ohne_Puffer.svg` · ASUE Variante 2/2a

BHKW und Brennwert-/NT-Kessel speisen parallel direkt in den Netzvorlauf; beide saugen aus
dem gemeinsamen Rücklauf. Die Regelung ist so einzustellen, dass der größtmögliche Teil der
Wärme vom BHKW erzeugt wird. Variante 2a der ASUE-Broschüre steuert die Stränge statt über
Einzelpumpen über Motorabsperrventile und einen Kessel-Bypass.

- **Vorteile:** brennwerttauglich (Kessel bleibt vom BHKW-Vorlauf getrennt), einfacher
  Aufbau, kein Pufferverlust.
- **Nachteile:** ohne Puffer nur bei gleichzeitigem Strom- und Wärmebedarf wirtschaftlich
  (fallen Strom- und Wärmebedarf zeitlich auseinander, ist ein Puffer nachzurüsten →
  Schemata 2/7/8); stabile Hydraulik der Gesamtanlage erforderlich (bei Ventillösung 2a).
- **Typischer Einsatz:** Objekte mit kontinuierlichem Wärmebedarf (z. B. Gewerbe mit
  Prozesswärme, Schwimmbäder), Brennwert-Bestandsanlagen.

## Schema 7 – Umschaltung über Ventile (Puffer laden/entladen)

**Datei:** `BHKW_S7_Umschaltung_Ventile_Puffer.svg` · ASUE Variante 3a/4a

Die Wärmeströme werden über motorische Ventile statt über Einzelpumpen geführt: Ein
Umschaltventil hinter dem BHKW speist entweder direkt ins Netz oder lädt den Puffer
(bei Strombedarf ohne Wärmebedarf). Die Entladung erfolgt über ein Motorabsperrventil,
das im Ladebetrieb die unkontrollierte Wärmeabgabe des Speichers (Naturumlauf) verhindert.
Der Kessel speist parallel.

- **Vorteile:** nur eine zentrale BHKW-Pumpe (geringerer Stromverbrauch als
  Pumpenlösungen), definierte Wärmeströme, stromgeführter Betrieb möglich.
- **Nachteile:** Ventile können sich festsetzen (ASUE-Hinweis), umgelenkte Massenströme
  müssen etwa gleich groß sein, aufwendigere Regelung/Verdrahtung.
- **Typischer Einsatz:** Anlagen mit stromgeführter Fahrweise und Wunsch nach geringer
  Pumpenanzahl.

## Schema 8 – Puffer im Nebenschluss (Laden/Entladen über Pumpen)

**Datei:** `BHKW_S8_Puffer_Nebenschluss_Pumpen.svg` · ASUE Variante 4

Das BHKW speist direkt in den Netzvorlauf; der Pufferspeicher liegt im Nebenschluss und
wird über eine eigene Ladepumpe (P4) geladen, wenn Strom, aber keine Wärme gebraucht wird,
und über eine Entladepumpe (P7) entladen, bevor der Kessel startet. Alternativ kann statt
der Entladepumpe ein Motorabsperrventil eingesetzt werden. Brennwert-/NT-Kessel parallel.

- **Vorteile:** direkte, schnelle Wärmeübergabe des BHKW ans Netz (keine Speicherträgheit
  wie bei Schema 2), Puffer nur bei Bedarf im Spiel, brennwerttauglich.
- **Nachteile:** mehr Pumpen und Regelaufwand, Rückschlagklappen in allen Strängen
  erforderlich, Puffer darf nicht vom Kessel geladen werden (Regelung sicherstellen).
- **Typischer Einsatz:** wärmegeführte Anlagen mit zeitweiligem Stromvorrang
  (Mittagsspitzen), Standard-Empfehlung der ASUE für Brennwertanlagen mit Puffer.

## Schema 9 – Praxisbeispiel: BHKW-Nachrüstung in einer Bestandsanlage

**Datei:** `BHKW_S9_Praxisbeispiel_Bestandsanlage.svg` · ASUE-Praxisbeispiel (S. 14–17)

Nachträgliche Einbindung eines BHKW (520 kW th / 350 kW el) mit zwei Wärmespeichern
(je 15 m³) in eine Bestandsanlage mit zwei Heizkesseln (je 1.750 kW). Die Gruppe
BHKW/Speicher wird über einen **Einbauverteiler** (zwei T-Abzweige mit Armaturen,
Absperrung dazwischen geschlossen) in Reihe in den Gesamtrücklauf eingebunden und wirkt
als Vorwärmstufe; die Kessel heizen im Winter nach. Im Sommer versorgt die Gruppe die
Verbraucher über den **Sommer-Bypass** (Motorklappe) direkt, die Kessel sind gesperrt.

- **Regelung (übergeordnete Steuerung):** Speicher oben < 70 °C (leer) → BHKW EIN;
  Speicher unten 90 °C (voll) → BHKW AUS; Kesselfreigabe witterungsgeführt über die
  Gesamtvorlauftemperatur (TIC 1, 70–95 °C) erst nach Speicherentladung.
- **Vorteile:** minimaler Eingriff in Bestandshydraulik und -regelung, Gruppe jederzeit
  absperrbar, Versorgung während der Installation gesichert, BHKW fährt mit vollem
  Anlagenvolumenstrom und niedrigem Rücklauf.
- **Typischer Einsatz:** Contracting/Nachrüstung in größeren Bestands-Heizzentralen.

## Schema 10 – Erweiterung: Solarthermie auf dem Pufferspeicher

**Datei:** `BHKW_S10_Erweiterung_Solarthermie.svg`

Erweiterung der Puffer-Einbindung um ein Solarkollektorfeld: Die Solarthermie lädt über
einen internen Wärmetauscher die **untere** (kalte) Pufferzone — dort ist der
Kollektorertrag am höchsten. Das BHKW lädt die obere Zone, der Kessel speist parallel
direkt ins Netz.

- **Vorteile:** Solarertrag reduziert im Sommer die BHKW-/Kessellaufzeiten (Konkurrenz
  beachten!), gemeinsamer Puffer, einfacher nachrüstbar bei Schema 1/2/8.
- **Nachteile:** Solarthermie und BHKW konkurrieren im Sommer um den Puffer — BHKW-Laufzeit
  und Wirtschaftlichkeit sinken; Regelstrategie (Solarvorrang, BHKW-Sperrzeiten) nötig;
  Solarkreis mit Frostschutz, eigener Sicherheitsgruppe und MAG auszuführen.
- **Typischer Einsatz:** Objekte mit hohem Sommer-Warmwasserbedarf, EE-Anforderungen
  (z. B. kommunale Vorgaben, GEG-Erfüllungsoptionen).

---

## Auswahlhilfe (Kurzfassung)

| Kriterium | Empfohlenes Schema |
|---|---|
| Einfachheit / kleine Anlage | Schema 1 |
| Maximale BHKW-Laufzeit, Standardlösung | Schema 2 |
| Hohe Netzvorlauftemperatur erforderlich | Schema 3 |
| Viele Kreise / mehrere Erzeuger / Erweiterung | Schema 4 |
| Minimalinvestition, konventioneller Kessel, ohne Puffer | Schema 5 |
| Brennwertkessel, ohne Puffer, hohe Gleichzeitigkeit | Schema 6 |
| Stromgeführt, wenige Pumpen (Ventillösung) | Schema 7 |
| Schnelle Netzreaktion + Puffer im Nebenschluss (Brennwert) | Schema 8 |
| Nachrüstung in Bestands-Heizzentrale (Einbauverteiler) | Schema 9 |
| Kombination mit Solarthermie | Schema 10 |

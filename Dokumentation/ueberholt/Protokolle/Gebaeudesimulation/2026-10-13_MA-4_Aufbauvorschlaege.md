# Protokoll MA-4 — Aufbauvorschläge vertieft

Welle MA-4 aus dem [Entwurf Geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md)
(Abschnitt 2.3, Wellenplan Abschnitt 4 Zeile MA-4) auf der Modellprüfung aus
[MA-1a](2026-10-13_MA-1a_Modellpruefung_Kern.md), dem geschlossenen Hüllweg aus [MA-3](2026-10-13_MA-3_Huelle_geschlossen.md)
und den Klassenvorgaben aus [MA-7](2026-10-13_MA-7_Einzonen_Vorgaben.md). Grundlage ist Entscheid E137 (F2 Übernahme mit
Rücknahme, F3 Rangfolge der Aufbauvorschläge). Leitziel: bessere Vorgabe ohne Mehrklicks.

## Befund

- Die Rangfolge Projektaufbau → Katalogaufbau → Typaufbau stand seit MA-1a, aber der Katalogschritt wählte nur nach Bauteilart
  und U-Nähe. Der Name des Bauteils, die Aufbauklasse des Katalogs (BK-2) und die Stoffgruppen der Katalogschichten zählten nicht.
- Der Namensabgleich Schichtsatz → Typgruppe (Konzept Bauteilaufbau beim Import 5.3) war der offene Rest von BA-2.
- Ein Bauteil der Stufe C (nur Geometrie) bekam ohne Projekt- oder Katalogaufbau keinen Vorschlag: Der Typaufbau braucht ein
  U. Die Baualtersklasse des Gebäudes blieb dabei ungenutzt.
- Der Katalog der Testdatenbank trägt neun Typaufbauten (fünf Außenwände, zwei Dächer, zwei Bodenplatten) mit Gruppe und Klasse.
  Keine Spalte nennt eine Baualtersklasse. „Passende Baualtersklasse“ heißt deshalb: das U der Klasse ist das Ziel-U im U-Band.

## Rangfolge

Für jedes opake Bauteil ohne Aufbau, Stufe B (nur U) und Stufe C (nur Geometrie):

1. **Projektaufbau** gleicher Bauteilart und Randbedingung mit der meisten Fläche im Gebäude (unverändert).
2. **Katalogaufbau** gleicher Bauteilart, geeignet, ohne Schicht ohne Stoffwert. Gibt es ein Ziel-U, zählt nur das Band ±25 %.
   Danach entscheidet in dieser Reihenfolge:
   - (a) die Typgruppe, die der Name des Bauteils trifft;
   - (b) die Aufbauklasse: Außenwand bzw. Außenwand am Erdreich, Innenwand, Flach- oder Steildach (Grenze 10°), Bodenplatte,
     Geschossdecke bzw. Decke oben/unten — erst die gleiche Klasse, dann ohne Klasse, dann eine andere;
   - (c) die U-Nähe;
   - (d) die Katalogfolge.
3. **Typaufbau**, auf das Ziel-U abgeglichen. Trifft der Name die Typgruppe Holz, gilt die leichte Bauart (Holzleichtbau,
   Sparrendach).

**Ziel-U:** in Stufe B das U des Bauteils; es bleibt (E95 Nr. 1). In Stufe C ist es das U der Baualtersklasse des Gebäudes über
`GebaeudeVorgaben.Wert` (Energiestandard vor Klasse, sonst der freie Wert der Klasse). Das Zielfeld je Bauteil:

- Erdreich und Boden: U Grund;
- Dach und Decke nach oben: U Dach;
- Außenwand: U Außenwand;
- übrige Hüllbauteile: U Sonstige.

Dann trägt der Vorschlag `UBauteil`, und eine Übernahme schreibt U und Aufbau in einem `UPDATE`. Eine Rücknahme stellt beide
zurück. Ohne Klasse und ohne Standard bleibt Stufe C beim bisherigen Weg (Katalog in Katalogfolge, kein Typaufbau).

Jeder Vorschlagstext beginnt mit Stufe und Herkunft, zum Beispiel „Stufe 2, Katalogaufbau: …“ bzw. „Level 2, catalogue
construction: …“. Hängt die Wahl am Namen, folgt „der Name „…“ trifft die Gruppe …“. Hängt sie an der Klasse, folgt „mit U … aus
der Baualtersklasse …“.

## Synonymtafel (Namensabgleich Schichtsatz → Typgruppe)

`Aufbaugruppenabgleich` normalisiert den Namen wie der Baustoffabgleich (N1/N2) und ordnet ihn einer der vier **Typgruppen** zu.
Typgruppen sind die tragenden Stoffgruppen des Bauteilkatalogs:

| Typgruppe | Wörter (deutsch) | Wörter (englisch) |
|---|---|---|
| Mauerwerk | Mauerwerk, Mauer, Ziegel, Backstein, Klinker, Kalksandstein, KS, Porenbeton, Gasbeton, Leichtbetonstein, Bims, Naturstein, Bruchstein, Sandstein, Lehmstein | masonry, brick(s), brickwork, blockwork, stone, sand lime, calcium silicate, aerated concrete, concrete block, clay brick |
| Beton | Beton, Stahlbeton, StB, Ortbeton, Leichtbeton, Betonfertigteil | concrete, reinforced concrete, precast concrete |
| Holz | Holz, Holzständer, Holzrahmen, Holztafel, Holzbau, Holzbalken, Fachwerk, Brettsperrholz, Massivholz, Sparren | timber, wood(en), timber frame, cross laminated timber, CLT, rafter |
| Metalle | Stahl, Metall, Trapezblech, Sandwichpaneel, Aluminium | steel, metal, sandwich panel |

Die Gegenwörter Holzfaser, Holzwolle und Steinwolle (englisch wood fibre/fiber, wood wool, stone wool, rock wool) sind Dämmstoffe.
Sie lassen den Namen ohne Typgruppe.

Die Stufen folgen dem Muster des Baustoffabgleichs:

- **N4:** das Synonym als ganzes Wort bzw. ganze Wortfolge.
- **N5:** ein Synonym ab fünf Zeichen als Wortanfang oder Wortende (Hochlochziegel, Ziegelwand, Stahlbetonwand).

Je Stufe gewinnt das längste Synonym (Porenbeton vor Beton, Stahlbeton vor Stahl). Gleich lange Synonyme verschiedener Gruppen
machen den Namen mehrdeutig, er bleibt ohne Gruppe. Ein Schichtsatz trifft die Gruppe der meisten Schichten; bei Gleichstand
bleibt er ohne Gruppe. Die Typgruppen eines Katalogaufbaus sind die Gruppencodes seiner Schichtbaustoffe, zusammen mit der
Typgruppe seines Namens.

Die Tafel enthält nur Wörter: keine Kennwerte, keine Hersteller- oder Markennamen, kein Schema.

## Dateien

- `EPOS.Kern/Allgemein/Import/Gebaeude/Aufbaugruppenabgleich.cs` (neu): Synonymtafel, Abgleich Name und Schichtsatz,
  Typgruppen eines Aufbaus.
- `EPOS.Kern/Allgemein/Simulation/Gebaeude/Modellpruefung/Modellvorschlagsregeln.cs`: Rangfolge mit Typgruppe und Aufbauklasse,
  `Aufbauklasse`, `KlassenU`, `KlassenZielfeld`, Typaufbau mit Bauart aus dem Namen, Texte mit Stufe.
- `Modellpruefung.cs`: `Aufbaukennung` um `Klasse` und `Typgruppen`, Prüfstand um `Energiestandard`.
- `Modellpruefungstypen.cs`: `Pruefvorschlag.UBauteil`.
- `EPOS.Kern/Controller/ModellpruefungCtrl.cs`: liest Energiestandard, Katalogbaustoffe → Gruppencode und Klasse bzw.
  Typgruppen der Katalogaufbauten. `AufbauAnwenden` schreibt bei `UBauteil` U und Aufbau mit gemeinsamer Rücknahme.
- `EPOS.Kern/MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs`: drei Texte mit Stufe, zwei Zusätze
  (`MP_VORSCHLAG_ZUSATZ_NAME`, `MP_VORSCHLAG_ZUSATZ_KLASSE`). Der Designer-Lauf ergab +750 Byte, der zweite Lauf +0.
- `EPOS.Kern.Tests/ModellvorschlagRangfolgeTests.cs` (neu).

Keine Oberfläche: Der bestehende Vorschlagsweg zeigt den Text. Kein Schemaschritt. Keine Leseregel des Rechenwegs ist geändert,
der Importweg ist unverändert. Die neuen SQL-Texte sind zwei `UPDATE` mit `?`-Parametern, der Leseweg des Katalogs ist der
vorhandene. Der SQL-Dialekt-Prüfer war grün.

## Tests

`ModellvorschlagRangfolgeTests` umfasst 25 Fälle:

- Namensabgleich: 11 Treffer N4/N5 zweisprachig, 4 ohne Typgruppe (darunter Gegenwörter), Mehrdeutigkeit, Schichtsatz.
- „Nur U, Name trifft Mauerwerk“ → Katalogaufbau der Gruppe, mit zwei Gegenproben: ohne Namen gewinnt die U-Nähe; außerhalb des
  Bands nicht.
- Je Stufe ein Fall mit Gegenprobe:
  - Projekt vor Katalog, mit Gegenprobe andere Randbedingung;
  - Flach- und Steildach nach Klasse;
  - Holzleichtbau aus dem Namen, mit Gegenprobe massiv.
- Stufe C mit Klasse: Typaufbau mit U der Klasse; Katalogaufbau im Band des Klassen-U; Gegenproben ohne Klasse.
- Zielfeld je Bauteil.
- Datenbankfall (Arbeitskopie, Projekt 1052): Übernahme schreibt U und Aufbau, Rücknahme stellt beide auf NULL zurück.

Filter „Modellvorschlag|Modellpruefung“: 50/50 grün; voller Abnahmefilter (Modellpruefung, Modellvorschlag, Bauteilkatalog, Baustoff, Baualter, GebaeudeImport, Gbxml, Sqproj, Modultrennung, Dokumentation, Repository, Resource): 822/822 grün. SQL-Dialekt-Prüfer: 2924 Texte, 0 Fundstellen.

## Offen

- Der Katalog trägt keine Baualtersklasse je Aufbau. Eine eigene Spalte wäre ein Schemaschritt und ist nicht gebaut; die Klasse
  wirkt über das Ziel-U.
- Der Namensabgleich liest den Bauteilnamen und den Aufbaunamen. Die Schichtnamen der Quelldatei liegen nach dem Import nicht am
  Bauteil. `Abgleichen(IEnumerable<string>)` steht bereit, sobald der Importweg sie mitgibt; den Importweg ändert MA-4 bewusst
  nicht.
- Die Wahl von Baualtersklasse und Bauart „einmal in Gruppe 2“ (Entwurf 2.3, große Lücken) bleibt Oberfläche. Der Kern nimmt die
  Klasse des Gebäudes.

## Logbuch-Vorschlag

Die Modellprüfung schlägt Aufbauten nach Namen und Baualtersklasse vor. Bauteile ohne U-Wert erhalten U-Wert und Aufbau in einem
Schritt.

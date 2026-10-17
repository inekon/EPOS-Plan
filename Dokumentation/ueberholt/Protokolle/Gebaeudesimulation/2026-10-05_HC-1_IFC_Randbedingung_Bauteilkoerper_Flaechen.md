# Protokoll HC-1 — IFC: zweiseitige Randbedingung, Bauteilkörper, Rahmenanteil, Flächenklassifikation (05.10.2026)

**Sitzung:** IFC-Ganglinie-Beispiele, Bau durch einen Opus-Agenten im Worktree `agent-a545d1c0434622a4f` (Zweig `worktree-agent-a545d1c0434622a4f`, Basis `5c49496f`), Abnahme durch einen Opus-Agenten. Commits der Welle: `68411baf` Teil 1 zweiseitige Randbedingung, `e6f7dce1` Teil 2 Rahmenanteil, `add95232` Teil 3 Körper der Hüllbauteile, `4670d6d7` Teil 4 Flächenklassifikation; Merge `3c37b6da`. Kein Schemaschritt, kein SQL, Basis unverändert; das Ergebnis liegt am `AbbildGebaeude`, nicht in der Datenbank, und ist keine Rechengröße.
**Entscheid:** E87 (F2, F3, F5); Konzept [HottCAD-Verbund](../../../aktuell/Gebaeudesimulation/2026-10-05_Konzept_HottCAD_Verbund_IFC_Projektdatei_Viewer.md) 3 und 4, Welle 6.3; Datenaustauschkonzept 15.1.

## 1 Auftrag

Die IFC-Datei aus HottCAD wird als Quelle der Hülle ausgereizt: beide Seiten der Randbedingung je Bauteil, die Körper der Hüllbauteile für die Ansicht, der Rahmenanteil der Fenster und eine Gruppe R0–R7 für jede Fläche eines Raumkörpers samt Bilanz und Gegenprobe gegen den Mengensatz.

## 2 Befund

- **Teil 1:** `AbbildBauteil` trägt `RandbedingungSeiteA/B`, `OrientierungSeiteA/B`, `RandbedingungWirksam` und einen Beleg (Kellerdecke, oberste Geschossdecke) aus `HSETU_Bauteilreferenzen`; die wirksame Seite (nicht beheizt) geht vor der Angrenzung des allgemeinen Satzes, der Rückfall bleibt. Orientierungslesart „botS (180)“ mit Dezimalkomma, Platzhalter −987654321,99 wird null. Probe `ifc4_z6_cad.ifc` um zweiseitige Sätze ergänzt.
- **Teil 2:** `AbbildBauteil.Rahmenanteil` (0–1) mit `RahmenanteilHerkunft` `Ifc` und Beleg aus `FractionOfFrame`; Werte über 1 gelten als Prozent (der CAD-Export schreibt 30), sonst als Anteil. Der g-Wert bleibt Vorgabe. Probe `ifc2x3_enthaltensein.ifc` um zwei Sätze ergänzt.
- **Teil 3:** `AbbildBauteil.Koerper` für `IfcWall`, `IfcSlab`, `IfcRoof`, `IfcWindow`, `IfcDoor` über `IfcRaumkoerper`. Dreiecksgrenze gemeinsam mit den Räumen: Bauteilkörper nur, wenn die Räume unter 200 000 Dreiecken bleiben und Räume plus Bauteile höchstens 300 000 ergeben (`BAUTEILKOERPER_RAUMANTEIL` = 2/3). Meldungen `IMP_IFC_PROT_BAUTEILKOERPER_GELESEN`, `_GRENZE`, `_ART` in beiden Sprachen. Neue Probe `ifc4_koerper_bauteile.ifc` (15 887 Byte).
- **Teil 4:** Klasse `Flaechenklassifikation` (`Import/Gebaeude/`) mit `Flaechengruppe` und `Flaechengruppenzeile`: Raumgrenze vor Körperpaar vor Bauteil des Raumbezugs, Rückfall `IMP_IFC_PROT_FLAECHE_OHNE_BAUTEIL`; Paarung ab 50 % der Fläche je ebener Fläche; Bilanz je Gruppe, R7 aus der Öffnungsfläche des Mengensatzes; Gegenprobe gegen den Mengensatz mit Hinweis über 5 % (`IMP_IFC_PROT_FLAECHENGRUPPE_ABWEICHUNG`), kein Fehler. Die Schlüssel stehen als volle Konstanten in der Klasse.

## 3 Zahlen der sechs Anwenderdateien unter `Quellen/` (Abnahmelauf im Worktree, Stand `4670d6d7`)

Gruppenbilanz in m², „Körperfläche/Mengensatz“ (R0 nur Körper, R7 Öffnungsfläche); letzte Spalte: Gruppen mit Abweichung über 5 % laut Gegenprobe.

| Datei | R0 | R1 | R2 | R3 | R4 | R5 | R6 | R7 | > 5 % |
|---|---|---|---|---|---|---|---|---|---|
| MFH-Klein 1964 | 1097 | 262/307 | 72/0 | 184/218 | 119/107 | 136/175 | 70/48 | 45 | R1–R6 |
| MFH mittel 1984 | 908 | 353/298 | 90/54 | 112/178 | 33/38 | 125/148 | 0/0 | 57 | R1–R5 |
| Produktion | 29505 | 5429/5395 | 115/36 | 9818/10093 | 0/0 | 9807/11460 | 0/0 | 1322 | R2, R5 |
| Sportheim | 2134 | 881/1086 | 613/160 | 521/589 | 494/353 | 537/775 | 0/8 | 136 | R1–R6 |
| Verwaltung | 11583 | 2673/2818 | 775/77 | 2063/1935 | 275/134 | 1303/1126 | 91/0 | 1009 | R1–R6 |
| WG-EH55 | 1120 | 432/400 | 0/4 | 109/242 | 0/0 | 159/205 | 0/0 | 98 | R1, R2, R3, R5 |

Die großen Abweichungen bei R2 und R4 (Sportheim, Verwaltung) kommen daher, dass gepaarte Flächen gegen unbeheizte Räume in der Körperbilanz zählen, im Mengensatz aber oft als innen geführt sind — Hinweis, kein Fehler.

Bauteile je wirksamer Randbedingung:

| Datei | außen | Erdreich | innen | unbeheizt | ohne Seitenangaben |
|---|---|---|---|---|---|
| MFH-Klein 1964 | 84 | 5 | 49 | 15 | 128 |
| MFH mittel 1984 | 101 | 17 | 45 | 16 | 107 |
| Produktion | 229 | 45 | 118 | 1 | 227 |
| Sportheim | 208 | 34 | 66 | 54 | 168 |
| Verwaltung | 323 | 42 | 239 | 15 | 468 |
| WG-EH55 | 103 | 22 | 41 | 1 | 80 |

Körper und Rückfälle:

| Datei | Bauteilkörper | Öffnungskörper | Flächen ohne Bauteil |
|---|---|---|---|
| MFH-Klein 1964 | 26 | 28 | 4 |
| MFH mittel 1984 | 40 | 33 | 70 |
| Produktion | 61 | 107 | 3 |
| Sportheim | 32 | 81 | 10 |
| Verwaltung | 68 | 143 | 135 |
| WG-EH55 | 50 | 31 | 41 |

Probe `ifc4_koerper_bauteile.ifc`: R0 12, R1 78, R2 12, R3 36, R5 36, R7 1,8 m².

## 4 Tests und Abnahme

- Abnahmefilter im Worktree: `EPOS.Kern.Tests` 337 grün, 2 übersprungen (Erzeuger-Tests), `EPOS.UI.Tests` 1 grün; `FlaechenklassifikationTests` 9 Fälle; alle Proben byte-gleich.
- Behobener Zwischenbefund: Der Wächter `GebaeudeZuordnungTests.Jeder_Schluessel_steht_in_beiden_Sprachen` las die neuen Schlüssel mit dem Präfix `IMP_GBXML_PROT_`; die Schlüssel stehen jetzt als volle Konstanten.
- Merge `3c37b6da` auf den Stand `b9c92076` konfliktfrei; `designer_neu.py` meldet den Designer unverändert und wiederholbar.
- Gate 740 im Gate-Worktree auf `3c37b6da` (Kern-Filter Release, voller Lauf): 19 717 Tests, 19 705 grün, 4 übersprungen, 8 rot — alle fremd und auf diesem Rechner schon vor HC-1 rot (Stand `b64a5133`): sieben Fälle `GebaeudeEinzonennetzTests.Die_Einzonenreihen_des_Bauteilwegs_bleiben_bitgleich` und `ZonenuebergabeRechenwegTests.Ideal_an_einer_Zone_rechnet_ohne_Heizkreis` (Rundung in der neunten Stelle); beide Klassen liefen im grünen Kern-Lauf `37352895205` auf ubuntu. Laufzeiten: `EPOS.Kern.Tests` 50 min 49 s, die IFC-Klassen höchstens 23 s je Klasse (`IfcQuelldateienDurchgangTests`), keine Klasse `GebaeudeImportAnsicht*` über 10 min. Wachen grün.

## 5 Offen

- Nur 50 Bauteilkörper am Wohngebäude WG-EH55 statt der erwarteten 92 — nicht untersucht.
- Hohe Zahl „ohne Seitenangaben“ — vermutlich Trennbauteile aus Körperpaaren.
- Paarung je ebener Fläche ab 50 %, nicht je Dreieck.
- Sonderregel „unbekannte Beheizung → R2 mit Vermerk“ nicht umgesetzt.
- Nordbezug der HottCAD-Orientierungen ungeprüft.
- Die Regel gilt wörtlich auch für unbeheizte Räume: die Außenwand eines unbeheizten Raums ist R1.
- Rahmenanteil: `FractionOfFrame` über 1 wird als Prozent gelesen.
- Fremder Befund auf Windows: die acht roten Fälle aus Abschnitt 4 (Bitgleichheit der Einzonenreihen, Rundung der Zonenübergabe) — gegen die CI auf ubuntu klären.
- Wiki und Logbuch erst mit HC-2 (Farbmodus Randbedingung in der Ansicht).

# Hydraulikschemata: Wärmepumpe ohne Booster-Wärmepumpe

Übersicht der Varianten für Wärmepumpenanlagen ohne separate Booster-WP.
Erstellt für die Software Wärmeplan. Stand: 17.07.2026 · Schemadarstellungen,
keine Ausführungsplanung.

## Funktionsprinzip

Die zentrale Wärmepumpe (z. B. Luft/Wasser) übernimmt Heizung und — je nach Variante —
auch die Warmwasserbereitung selbst. Ohne Booster-WP muss die Wärmepumpe für das
Warmwasser zeitweise (Variante 1) oder dauerhaft (Variante 2) auf hohem Temperaturniveau
(55–60 °C) arbeiten; die Jahresarbeitszahl liegt dadurch bei hohem WW-Bedarf unter der
des Booster-Konzepts (siehe Ordner „WP mit Booster"). Dafür entfallen zweiter
Kältekreis, Investition und Wartung der Booster-WP.

---

## Variante 1 – Warmwasser über Umschaltventil

**Datei:** `Schema_WP_ohne_Booster_WW_Umschaltventil.svg`

Klassische Lösung: Ein motorisches 3-Wege-Umschaltventil hinter der WP bedient
wechselweise den Heizungs-Pufferspeicher (35–45 °C) und den WW-Speicher mit innenliegendem
Glattrohr-Register (Ladung 55–60 °C, Speicher 50–55 °C). Warmwasser hat Vorrang.

- **Vorteile:** einfach, bewährt, nur eine Umwälzpumpe im Erzeugerkreis, getrennte
  Temperaturniveaus für Heizen und WW.
- **Nachteile:** Heizbetrieb pausiert während der WW-Ladung; WP taktet im Sommer;
  Register groß dimensionieren (kleine Grädigkeit nötig, sonst schafft die WP die
  Speichertemperatur nicht); Trinkwasserbevorratung → Legionellenprogramm
  (opt. Heizstab) einplanen.
- **Typischer Einsatz:** Ein-/Zweifamilienhaus, kleine Mehrfamilienhäuser.

## Variante 2 – Kombispeicher mit Frischwasserstation

**Datei:** `Schema_WP_ohne_Booster_Kombispeicher_FriWa.svg`

Die WP lädt einen Kombi-/Schichtspeicher, dessen oberer Bereitschaftsteil auf ~60 °C
gehalten wird. Die Frischwasserstation erwärmt das Trinkwasser hygienisch im
Durchflussprinzip direkt aus dem Speicher; die Heizkreise werden aus der Speichermitte
versorgt.

- **Vorteile:** hygienische WW-Bereitung ohne Trinkwasserbevorratung (kein
  Legionellenrisiko im Speicher), ein einziger Speicher, gut mit Solarthermie/PV
  kombinierbar.
- **Nachteile:** WP muss dauerhaft ~60 °C liefern → deutlich reduzierte JAZ (ggf.
  Hochtemperatur-/Propan-WP erforderlich); Schichtung im Kombispeicher entscheidend;
  Zirkulation erhöht den Primär-Rücklauf der FriWa.
- **Typischer Einsatz:** Sanierung mit vorhandenem Kombispeicher, Objekte mit
  Hygiene-Anforderungen, aber ohne Platz/Budget für das Booster-Konzept.

## Variante 3 – Puffer nur für Spitzenlast (Nebenschluss)

**Datei:** `Schema_WP_ohne_Booster_Puffer_Spitzenlast.svg`

Die Wärmepumpe versorgt die Heizkreise **direkt** — der Pufferspeicher liegt im
Nebenschluss und dient ausschließlich der Spitzenlastabdeckung: Bei Schwachlast lädt die
Ladepumpe (P3) den Speicher; bei Spitzenlast, während EVU-Sperrzeiten oder zur
Abtauunterstützung speist die Entladepumpe (P4) die gespeicherte Wärme zusätzlich in den
Vorlauf. Rückschlagklappen trennen die Zweige. (Hydraulisch analog zu BHKW-Schema 8,
„Puffer im Nebenschluss".)

- **Vorteile:** keine Speicher- und Mischverluste im Normalbetrieb (direkte Versorgung =
  beste Effizienz), WP kann kleiner ausgelegt werden (Puffer deckt Spitzen), Speicher
  überbrückt Sperrzeiten und liefert Abtauenergie.
- **Nachteile:** direkte hydraulische Kopplung erfordert ausreichenden Mindestvolumenstrom
  über die Heizkreise (Überströmventil ggf. vorsehen), zusätzliche Lade-/Entladepumpe und
  Regellogik (Lade-/Entladekriterien), Puffer entlädt mit Mischtemperatur.
- **Typischer Einsatz:** Flächenheizungen mit hoher Systemträgheit, Anlagen mit
  EVU-Sperrzeiten, knappe WP-Dimensionierung mit gelegentlichen Lastspitzen.
  Das Prinzip ist auch mit dem Booster-Konzept kombinierbar (Puffer im Nebenschluss des
  Heizkreises, Booster-WP weiterhin quellseitig am Puffer).

---

## Typische Temperaturniveaus (in den Schemata eingetragen)

| Kreis | Vorlauf | Rücklauf |
|---|---|---|
| WP Heizbetrieb → Puffer/Heizkreise | 35–45 °C | 28–30 °C |
| WP WW-Ladung (Variante 1) | 55–60 °C | — |
| WP Dauerbetrieb Kombispeicher (Variante 2) | 55–60 °C | 28–35 °C |
| WW-Speicher / Bereitschaftsteil | 50–55 / ~60 °C | — |
| Frischwasserstation primär (Variante 2) | ~60 °C | 25–30 °C |
| Warmwasser / Kaltwasser | 50–55 °C | ~10 °C (KW) |
| Fußbodenheizung / Heizkörper | 35 / 45 °C | 28 / 35 °C |

Alle Angaben sind typische Auslegungs-Richtwerte und projektspezifisch anzupassen.

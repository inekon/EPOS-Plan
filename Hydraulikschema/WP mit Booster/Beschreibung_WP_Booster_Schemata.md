# Hydraulikschemata: Wärmepumpe mit Booster-Wärmepumpe und Frischwasserstation

Übersicht der Varianten zur Warmwassererzeugung mit Booster-Wärmepumpe.
Erstellt für die Software Wärmeplan. Stand: 17.07.2026 · Schemadarstellungen,
keine Ausführungsplanung.

## Funktionsprinzip

Die zentrale Wärmepumpe (z. B. Luft/Wasser) liefert Wärme auf niedrigem Temperaturniveau
(Vorlauf 35–45 °C) in einen Pufferspeicher und bleibt damit im effizienten Betriebsbereich.
Eine Booster-Wärmepumpe (Wasser/Wasser) nutzt den Puffer als Quelle (30–40 °C) und hebt die
Temperatur auf 60–65 °C an, um einen Warmwasser-Pufferspeicher (55–60 °C) zu laden. Eine
Frischwasserstation erwärmt das Trinkwasser hygienisch im Durchflussprinzip — es wird kein
Trinkwasser bevorratet, was das Legionellenrisiko konstruktiv minimiert und den Betrieb mit
moderaten Speichertemperaturen erlaubt.

Vorteile des Konzepts: hohe Jahresarbeitszahl der Hauptwärmepumpe (kein Hochtemperaturbetrieb
für Warmwasser), hygienische Trinkwassererwärmung, gute Quellentemperatur für den Booster
(hohe Booster-Effizienz), klare hydraulische Trennung von Heizung und Warmwasser.

---

## Variante 1 – Standard mit Heizkreisen

**Datei:** `Schema_WP_mit_Booster_WP_mit_Heizkreisen.svg`

Vollständiges Anlagenschema: Der Pufferspeicher versorgt sowohl die Heizkreise
(Fußbodenheizung 35/28 °C mit 3-Wege-Mischer, Heizkörperkreis 45/35 °C) als auch die
Booster-Wärmepumpe quellseitig. Warmwasserpfad über Booster-WP → WW-Puffer →
Frischwasserstation.

- **Typischer Einsatz:** Standardfall Wohngebäude/Mehrfamilienhaus mit zentraler
  Heizungs- und Warmwasserversorgung.

## Variante 2 – Warmwasser-Pfad

**Datei:** `Schema_WP_mit_Booster_WP_nur_Warmwasser.svg`

Reduziertes Schema ohne Heizkreise; zeigt ausschließlich die Kette
Wärmepumpe → Pufferspeicher → Booster-WP → WW-Puffer → Frischwasserstation.

- **Typischer Einsatz:** Detail-/Prinzipdarstellung der Warmwassererzeugung, z. B. für
  Erläuterungen, Angebote oder wenn die Heizverteilung separat geplant wird.

## Variante 3 – Mit Heizkreisen und Zirkulation

**Datei:** `Schema_WP_mit_Booster_WP_mit_Zirkulation.svg`

Wie Variante 1, zusätzlich mit Warmwasser-Zirkulationsleitung und Zirkulationspumpe zurück
zur Frischwasserstation (Zirkulationsrücklauf ~50 °C).

- **Typischer Einsatz:** größere Gebäude / Mehrfamilienhäuser mit langen Leitungswegen, in
  denen nach DIN 1988-200 bzw. aus Komfortgründen eine Zirkulation erforderlich ist.
  Hinweis: Die Zirkulation erhöht die Rücklauftemperatur am Primärkreis der
  Frischwasserstation — bei der Auslegung des WW-Puffers und der Booster-WP berücksichtigen.

## Variante 4 – Mit Solarthermie

**Datei:** `Schema_WP_mit_Booster_WP_mit_Solarthermie.svg`

Wie Variante 1, zusätzlich mit Solarkollektorfeld, das über einen internen Wärmetauscher
die **untere** (kalte) Zone des Pufferspeichers lädt — dort ist der Kollektorertrag am
höchsten. Die Solarthermie entlastet die Hauptwärmepumpe vor allem in Übergangszeit und
Sommer und verbessert zugleich die Quellentemperatur für die Booster-WP.

- **Vorteile:** höhere Systemeffizienz (Solarertrag ersetzt WP-Strom), gute Synergie mit
  dem Booster-Konzept (Solar hebt die Booster-Quelle an), moderate Kollektortemperaturen
  durch kalte untere Pufferzone.
- **Hinweise:** Solarkreis mit Frostschutzmedium, eigener Sicherheitsgruppe und MAG
  ausführen (im Schema nicht dargestellt); Solarvorrang regeln (Solarpumpe PS über
  Temperaturdifferenz Kollektor/Puffer unten); sommerliche Stagnation bei
  Auslegung des Kollektorfelds berücksichtigen.
- **Typischer Einsatz:** Neubau/Sanierung mit EE-Anforderungen, Objekte mit hohem
  Sommer-Warmwasserbedarf.

## Variante 5 – Puffer nur für Spitzenlast (Nebenschluss)

**Datei:** `Schema_WP_mit_Booster_WP_Puffer_Spitzenlast.svg`

Die Wärmepumpe versorgt die Heizkreise **direkt**; der Pufferspeicher liegt im
Nebenschluss und dient ausschließlich der Spitzenlastabdeckung: Die Ladepumpe (P7) lädt
bei Schwachlast, die Entladepumpe (P8) speist nur bei Spitzenlast, EVU-Sperrzeit oder zur
Abtauunterstützung zusätzlich in den Vorlauf. Die Booster-WP bezieht ihre Quelle in dieser
Variante aus dem Heizungsrücklauf (~30 °C); alternativ kann sie quellseitig am Puffer
bleiben. WW-Kette unverändert: Booster-WP → WW-Puffer → Frischwasserstation.

- **Vorteile:** keine Speicher- und Mischverluste im Normalbetrieb (direkte Versorgung =
  beste Effizienz der Haupt-WP), WP kann knapper dimensioniert werden, Puffer überbrückt
  Sperrzeiten und liefert Abtauenergie.
- **Nachteile:** Mindestvolumenstrom der WP über die Heizkreise sicherstellen (ggf.
  Überströmventil), zusätzliche Lade-/Entladepumpe und Regellogik; Booster-Quelle aus dem
  Rücklauf ist etwas kälter als aus dem Puffer (geringfügig niedrigere Booster-Effizienz,
  dafür sinkt die Rücklauftemperatur — gut für die Haupt-WP).
- **Typischer Einsatz:** Flächenheizungen mit hoher Systemträgheit, EVU-Sperrzeiten,
  knappe WP-Dimensionierung; hydraulisch analog zu „WP ohne Booster" Variante 3 und
  BHKW-Schema 8.

---

## Typische Temperaturniveaus (in den Schemata eingetragen)

| Kreis | Vorlauf | Rücklauf |
|---|---|---|
| Wärmepumpe → Puffer | 35–45 °C | 28–30 °C |
| Puffer (Schichtung) | oben ~45 °C | unten ~30 °C |
| Solarkreis (Variante 4) | 60–90 °C | — |
| Booster-WP Quelle | 30–40 °C | ~25 °C |
| Booster-WP Senke → WW-Puffer | 60–65 °C | 50–55 °C |
| Frischwasserstation primär | ~60 °C | 25–30 °C |
| Warmwasser / Kaltwasser | 50–55 °C | ~10 °C (KW) |
| Fußbodenheizung / Heizkörper | 35 / 45 °C | 28 / 35 °C |

Alle Angaben sind typische Auslegungs-Richtwerte und projektspezifisch anzupassen.

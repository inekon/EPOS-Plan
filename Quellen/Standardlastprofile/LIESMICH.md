# BDEW-Standardlastprofile Strom 2025

Quelle der Katalogsaat „Datenbank Strombedarf" (Dialog „Standard Stromprofil"): die repräsentativen
Standardlastprofile Strom 2025 des BDEW. Abgeleitet werden daraus die drei Katalogsätze H25, G25 und L25;
Ableitung und Prüfung: [`Werkzeuge/Standardlastprofile`](../../Werkzeuge/Standardlastprofile/LIESMICH.md).

## Herkunft

| Datei | Inhalt | Originalname | SHA-256 |
|---|---|---|---|
| `2025-03-17_BDEW_Aktualisierte_SLP_Strom_2025.pdf` | Veröffentlichung des BDEW „Aktualisierte Standardlastprofile Strom 2025" vom 17.03.2025, 6 Seiten: Anwendungsregeln, Typtage, Normierung, Dynamisierungsfunktion | `2025-03-17_AWH_Aktualisierte_SLP_Strom_2025_Veröffentlichung.pdf` | `724d72f2c3f98e11538b78417eecb08cbfbc3de07c3abf5ed034777d6beecf37` |
| `BDEW_Repraesentative_Profile_H25_G25_L25_P25_S25.xlsx` | die Profilwerte H25, G25, L25, P25, S25 und ein Blatt zur Dynamisierung | `Kopie_von_Repräsentative_Profile_BDEW_H25_G25_L25_P25_S25_Veröffentlichung.xlsx` | `1803d4c612693563a784eb61001e7c58ffd6bd18a6bca3780f774f3c3459b845` |

- Herausgeber: BDEW Bundesverband der Energie- und Wasserwirtschaft e. V., Veröffentlichung vom 17.03.2025.
- Abruf: 06.10.2026 (Ablage INEKON `40-Daten/Standardlastprofile`); beim Einchecken nur umbenannt (ohne
  Umlaute und Zusätze), der Inhalt ist unverändert (Prüfsummen oben).
- **Lizenz:** Weder die Excel noch das PDF tragen eine Lizenzangabe. Der BDEW stellt die Profile den
  Netzbetreibern und der Branche zur Anwendung bereit (PDF S. 3). Ausgeliefert werden nicht die Dateien,
  sondern allein die abgeleiteten Werte — je Profil 168 Wochenstunden und 12 Monatswerte für H25, G25 und
  L25.

## Aufbau der Excel

- Je Profilblatt **12 Monate × 3 Typtage × 96 Viertelstunden** = 3 456 Werte, ohne Formeln und Lücken:
  Zeile 3 Monatskopf, Zeile 4 Typtag in der Folge SA (Samstag), FT (Sonn- und Feiertag), WT (Werktag),
  Spalte B Viertelstunde „00:00-00:15" bis „23:45-00:00", Werte in den Zeilen 5 bis 100, Spalten C bis AL.
- Einheit **kWh je Viertelstunde**, normiert auf **1 Mio. kWh = 1.000 MWh** Jahresverbrauch.
- **Dynamisierung:** H25, P25 und S25 sind entdynamisiert; beim Ausrollen gilt je Tag des Jahres t der Faktor
  F(t) = −3,92·10⁻¹⁰ t⁴ + 3,2·10⁻⁷ t³ − 7,02·10⁻⁵ t² + 2,1·10⁻³ t + 1,24 (auf 4 Nachkommastellen, der Wert
  danach auf 3 Nachkommastellen gerundet; PDF S. 4). G25 und L25 werden nicht dynamisiert.
- Feiertage zählen wie Sonntag (FT) nach dem Kalender des Bundeslands (PDF S. 3).

## Verwendung in EPOS-Plan

| Profil | Katalogsatz (`Tab_Stromverbraucher_STAMM`) | Typprofil (`Tab_Stromverbrauchertyp_STAMM`) |
|---|---|---|
| H25 Haushalt | `BDEW_H25_Haushalt` | `BDEW_H25` |
| G25 Gewerbe allgemein | `BDEW_G25_Gewerbe` | `BDEW_G25` |
| L25 Landwirtschaftsbetriebe | `BDEW_L25_Landwirtschaft` | `BDEW_L25` |

- Die 12 Monatswerte [MWh] summieren auf 1.000 MWh; das Wochenprofil führt 168 Stundenwerte (Mo–Fr aus dem
  Werktag, Sa aus dem Samstag, So aus dem Sonn- und Feiertag). Rechenregeln im
  [Werkzeug](../../Werkzeuge/Standardlastprofile/LIESMICH.md).
- **P25 und S25 werden nicht verwendet:** Sie beschreiben den Netzbezug von Haushalten mit Photovoltaik bzw.
  mit Photovoltaik und Stromspeicher (PDF S. 4–5). EPOS-Plan rechnet Photovoltaik und Speicher selbst; mit
  diesen Profilen zählten sie doppelt.

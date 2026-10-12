# AB — Farbmodi „Aufbau“ und „Befund“, Bauteilnamen im Import „nur Projektdatei“

Kurzprotokoll, 11.10.2026. Grundlagen: [Konzept Bauteilaufbau Import](../../../aktuell/Gebaeudesimulation/2026-10-06_Konzept_Bauteilaufbau_Import.md) 5.2, 5.4 (a), 5.5.

## Befund

| Meldung | Ursache | Beleg |
|---|---|---|
| „Aufbau“ und „Befund“ wirken aktiv, tun aber nichts | Die Knöpfe trugen `disabled`, aber `.epos-diagramm-knopf` hatte keine Regel für den gesperrten Zustand. Sie sahen aus wie wählbar, und ihr `title` erschien nie. | `EPOS.UI/Bausteine/GebaeudeAnsicht.razor`, `Farbmodusschalter`; `epos-ui.css`, `.epos-diagramm-knopf` |
| „Aufbau ist gesperrt … (kein Bauteilvorschlag)“ | Der Bauteilvorschlag wird nur abgelehnt, wenn der Zonenplan Räume ohne Zone hat. Beispiel: Simulationszonen der Projektdatei ohne „Rest nach Regel“, an der echten Datei 3 Räume. Der Text nannte diesen Grund nicht. | `GebaeudeBauteilvorschlag.BauenMehrzonig`, `Abschlusspruefung` |
| GUID in der Spalte „Bauteil“ | Ohne CAD-Namen verwendet der Vorschlag die Kennung als Namen. Die Projektdatei trägt an den Hüllflächen keinen Namen. | `GebaeudeBauteilvorschlag.Name(AbbildBauteil)` |
| „500 von 500 Sätzen“ | Das ist keine Kappung. Weder Liste noch Hülle noch Kern begrenzen die Zahl; es ist die echte Zeilenzahl des Weges „Simulationszonen der Datei, Rest nach Regel“. | Zählung unten |

Mit allen Räumen in einer Zone wird der Vorschlag gebildet. Das gilt für die Regel allein, für die DIN-Zonen der Datei und für Simulationszonen samt „Rest nach Regel“. Stufe A kommt aus dem Aufbau der Hüllfläche, U aus der Datei. Die Sperre bei Räumen ohne Zone bleibt bestehen und wird in GD-3 behoben.

## Behebung

- Die Farbmodusknöpfe sind weich gesperrt: `aria-disabled` und `title` mit Grund. Ein Klick meldet den Grund in einer Statuszeile, der Modus bleibt gleich. Eine Stilregel deckt beide Sperrarten ab (leise Schrift, gestrichelter Rahmen). Die Leiste gehört zu beiden Reitern, deshalb ist der Zustand in Grundriss und Körper gleich.
- `GebaeudeAnsichtDaten.Vorschlagsgrund`: Die Importhülle trägt die Fehler des abgelehnten Vorschlags ein. Der Grund für „Aufbau“ und „Befund“ nennt ihn dann (`GANS_AUFBAU_OHNE_VORSCHLAG`, `GANS_BEFUND_OHNE_VORSCHLAG`).
- Dreiecke ohne Bauteilbezug: Sie zeichnen in der neutralen Farbe „ohne Bauteil“ wie bisher. Neu ist eine eigene Legendenzeile „ohne Bauteilbezug (n)“ in Aufbau und Befund (`DreieckeOhneBauteilbezug`, `GANS_OHNE_BAUTEILBEZUG`).
- Lesbarer Bauteilname (`GebaeudeBauteilvorschlag.Lesename`): Gilt für alle Formate. Vorrang hat der CAD-Name, sonst gilt „Art Raum Ausrichtung“ in acht Sektoren (`GIMP_BT_RICHTUNGEN`). Der Raum kommt aus der Raumgrenze, sonst aus dem Nachbarraum. Waagerechte Flächen bekommen keine Richtung. Ohne Raum und Richtung bleibt die Kennung. Die Kennung bleibt in `Quellkennung` und im Steckbrief.
- Das JS ist unverändert. Das Körperfeld trug Stufen- und Befundbytes für Körper aus Flächen bereits, und die neutrale Farbe steht in der Farbtafel. Nachweis: bunit (Knopfzustand, Meldung, Legendenzeile) und `GebaeudeImportFarbmodiTests` (Stufen- und Befundabschnitte des Körperfelds je Format).

## Zählungen an der echten Datei (Sportheim, nur Projektdatei)

| Größe | Regel X1 | Simulationszonen + Rest nach Regel |
|---|---|---|
| Bauteilzeilen | 509 | 500 |
| Hüllflächen (Zeilen mit Summenfeld) | 371 | 380 |
| davon mit Aufbau aus der Datei | 287 | 296 |
| Aufbauten | 8 | 8 |
| Stufe A außen / innen | 287 / 138 | 296 / 120 |
| Stufe B außen | 4 | 4 |
| Stufe C | 0 | 0 |
| transparent | 80 | 80 |
| Befund „ohne“ | 509 (100 %) | 500 (100 %) |
| Raumdreiecke gesamt / ohne Bauteilbezug | 1 140 / 464 (40,7 %) | 1 140 / 464 (40,7 %) |
| Bauteilnamen mit GUID | 0 (vorher 340) | 0 (vorher 331) |

Simulationszonen ohne „Rest nach Regel“: 3 Räume ohne Zone, kein Vorschlag. „Aufbau“ und „Befund“ sind gesperrt, und der Grund nennt die Räume.

## Prüfung

- Kern-Filter und Windows-Schale (`EnableWindowsTargeting=true`): 0 Fehler.
- Kern, Filter GebaeudeAnsicht|GebaeudeImport|Sqproj|Gbxml|Flaechenklassifikation|Bauteil|Befund|Wache|Ressource|GebaeudeAufbau: 1 608 bestanden, 1 übersprungen. Der eine Fehlschlag im ersten Lauf war eine erwartete Namensänderung (`Innere_Trennflaechen_zaehlen_mit_beiden_Seiten`) und ist angepasst. Danach liefen `GebaeudeBauteilvorschlagTests` und `GebaeudeImportFarbmodiTests` grün.
- UI, Filter GebaeudeAnsicht|GebaeudeAufbau|GebaeudeBefund|GebaeudeImport|Stilblatt|Wache: 370/370.
- `designer_neu.py`: 4 neue Schlüssel, zweiter Lauf ohne Befund.

## Für die Windows-Sichtabnahme

1. Reiter „Körper“ und „Grundriss“: Gesperrte Farbmodusknöpfe sehen gesperrt aus (gestrichelt, leise). Der Tooltip nennt den Grund, und ein Klick zeigt die Meldung.
2. Simulationszonen ohne „Rest nach Regel“: Der Grund nennt die Räume ohne Zone.
3. Mit allen Räumen zugeordnet: Der Dateikörper färbt in „Aufbau“ (A grün, B gelb, transparent grau) und „Befund“. Graue Dreiecke ohne Bezug stehen in der Legendenzeile „ohne Bauteilbezug (464)“.
4. Flächenliste: Die Spalte „Bauteil“ zeigt „Außenwand <Raum> S“ usw. statt der GUID, und der Steckbrief zeigt die Kennung.

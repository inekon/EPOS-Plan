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

## AB2 — Räume über die Legende zuordnen

Wunsch des Anwenders vom 11.10.2026: „Die Zuordnung der Nutzung zu den Räumen sollte nicht über das Dropdown erfolgen,
sondern über das Anklicken der Legendensymbole (mit Hinweis der Nutzung).“

**Umsetzung.**

- Das Auswahlfeld „Räume zuordnen zu“ entfällt samt Schlüssel `GIMP_DLG_ZUORDNEN_ZU`.
- Im Farbmodus „Zonen“ ist jeder Legendeneintrag ein Knopf (`button`, `aria-pressed`). Der Wirt reicht die Einträge als
  `Zuordnungsziele` (`GebaeudeAnsichtZiel`) herein: je Zone des Plans einen, auch für eine angelegte Zone ohne Raum im
  Bild (gepunktetes Farbfeld), dazu „als eigene Zone“.
- Ein Klick wählt das Ziel, ein zweiter hebt die Wahl auf. Gemeldet wird über `ZielChanged`, der Dialog hält die Wahl.
  Ohne Wahl wählt der Dialog keine erste Zone mehr vor.
- Mit Wahl ordnet ein Raumklick im Grundriss oder im Körper den Raum zu. Der Körperklick ordnet zu, statt eine Zone zu
  wählen. Ohne Wahl ist der Raum reine Anzeige (`role="img"`), sein Kurztext sagt, wie man zuordnet. „Umhängen“ auf dem
  Weg ohne Grundriss ist dann weich gesperrt (`aria-disabled` mit Grund).
- Jeder Eintrag nennt die Nutzung: „Nutzung: Profil · Kategorie“ aus der Herleitung, sonst den Text der Nutzung, ohne
  Nutzung das Profil der Projektdatei (DIN-Nummer) oder „ohne Nutzung“. Der `title` nennt Herkunft und „Klicken, dann
  Räume anklicken, um sie dieser Zone zuzuordnen“.
- „nicht zugeordnet (n Räume)“ ersetzt den Eintrag „ohne Zone“, in der grauen Farbe der Räume ohne Zone. Er ist kein
  Ziel. Ein Klick hebt diese Räume im Grundriss hervor (`epos-gebansicht-raum--hervor`, Token `--epos-grundriss-hervor`).
- In den anderen Farbmodi gibt es keine Zielknöpfe. Ist ein Ziel gewählt, steht dort „Zuordnen nur im Modus Zonen“, und
  ein Raumklick ordnet nicht zu.
- „Rest nach Regel zuordnen“, Zone anlegen und Zone löschen bleiben unverändert. Die Ansicht im Gebäudedialog („neu
  lesen“) ordnet nichts zu und bleibt, wie sie ist.
- Neue Schlüssel in beiden Sprachen: `GIMP_ANS_ZIEL_NUTZUNG`, `GIMP_ANS_ZIEL_OHNE_NUTZUNG`, `GIMP_ANS_ZIEL_KLICK`,
  `GIMP_ANS_ZIEL_AUFHEBEN`, `GIMP_ANS_ZIEL_FEHLT`, `GIMP_ANS_ZIEL_NUR_ZONEN`, `GIMP_ANS_NICHT_ZUGEORDNET`,
  `GIMP_ANS_NICHT_ZUGEORDNET_TITEL`, `GIMP_ANS_ZIEL_LEGENDE`. `GIMP_DLG_UMHAENGEN_HINWEIS` nennt jetzt die Legende.
- Die KI-Feldkarte führte die Zielwahl nicht. Die Eingabebilanz des `GebaeudeImportDialog` sinkt von 31 auf 30, der
  Vermerk ist nachgezogen.

**Prüfung.**

- Kern-Filter und Windows-Schale (`EnableWindowsTargeting=true`): 0 Fehler.
- UI, Filter GebaeudeAnsicht|GebaeudeImport|Zonen|Stilblatt|KiDialog|KiMasken|Wache: 746/746, darunter 8 neue Proben.
- Kern, Filter GebaeudeImport|Zonenplan|Ressource|Wache: 858/858.
- `designer_neu.py`: zweiter Lauf ohne Befund.
- Dialogkopf, Schlussleiste und Rasterregeln sind nicht berührt, deshalb keine Rasterprobe.

**Für die Windows-Sichtabnahme.**

1. Gebäudeimport, Reiter „Grundriss“, Modus „Zonen“: Über dem Bild steht kein Auswahlfeld mehr. Die Legende zeigt
   Knöpfe mit Farbfeld, Name und Nutzungszeile. Der gewählte Knopf ist hellblau mit Markenrand.
2. Ein Raumklick ohne Wahl tut nichts. Mit Wahl ordnet er zu, auch im Reiter „Körper“. Ein zweiter Klick auf den Knopf
   hebt die Wahl auf.
3. „nicht zugeordnet (n Räume)“: Ein Klick rahmt diese Räume orange.
4. „Randbedingung“, „Aufbau“ oder „Befund“ mit gewähltem Ziel: Der Hinweis „Zuordnen nur im Modus Zonen“ steht da.
5. Eine neu angelegte Zone steht sofort als Knopf in der Legende, mit gepunktetem Farbfeld.

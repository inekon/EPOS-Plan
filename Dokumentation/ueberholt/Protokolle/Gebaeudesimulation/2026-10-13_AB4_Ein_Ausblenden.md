# AB4 — Ein- und Ausblenden in der Körperansicht

Kurzprotokoll, 13.10.2026. Grundlagen: [Übergabepapier Gebäudesimulation](../../../aktuell/Gebaeudesimulation/2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md) Abschnitt 4 Punkt 2a, [Entwurf geführter Modellaufbau](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md) (MA-5 baut darauf auf), Vorläufer [AB/AB2](2026-10-11_AB_Farbmodi_Aufbau_Befund.md) und [AB3](2026-10-12_AB3_Baustoffe_im_Aufbau.md).

Wunsch des Anwenders vom 12.10.2026: einzelne Komponenten der Körperansicht (Zonen, Randbedingungen, Aufbau) und einzelne Bauteilgruppen ein- und ausblenden, etwa über eine Werkzeugwahl „Zuordnen | Ein-/Ausblenden“.

## Befund

| Stand vor AB4 | Folge für den Bau |
|---|---|
| Die Legenden der Farbmodi „Randbedingung“, „Aufbau“ und „Befund“ schalten ihre Einträge schon (`aria-pressed`, Schalter je Gruppe, Stufe bzw. Befund; Bauteilkörper eigener Schalter). | Sie bleiben in beiden Werkzeugen Schalter. Die Werkzeugwahl betrifft nur den Farbmodus „Zonen“, in dem die Legende seit AB2 das Ziel der Zuordnung wählt. |
| Die Legende „Zonen“ hatte keinen Schalter; ausblenden ließ sich dort nichts. | Neue Sichtlegende im Werkzeug „Ein-/Ausblenden“. |
| Die Bauteilart steht nur an den Platten des Exportmodells (Schlüssel je Kante, Boden, Decke). Raumdreiecke kennen ihr Bauteil (`Dreiecksbauteile`), nicht dessen Art. Bauteilkörper kennen nur die Randgruppe. | Die Daten tragen neu die Art je Bauteilkennung, das Bytefeld die Bauteilgruppe je Raumdreieck. |
| Die Legenden-Schalter überlebten einen Wechsel des Farbmodus. | Der Wechsel setzt sie zurück, wie gewünscht. |
| Der Wirt der Rasterprobe trägt die Gebäudeansicht nicht. `legendenprobe.mjs` misst die Ringlegende der Ergebnisübersicht, `diagrammprobe.mjs` das `DiagrammSvg`. | Für das Modul der Körperansicht gibt es eine eigene Browserprüfung mit Gegenprobe (unten). Sie ist nicht eingecheckt. |

## Bau

- **Werkzeugwahl** (`GebaeudeAnsicht.razor`, `Werkzeugleiste`): über der Legende der Körperansicht. Im Farbmodus „Zonen“ stehen zwei Umschaltknöpfe „Zuordnen“ (Vorgabe) und „Ein-/Ausblenden“, in jedem Farbmodus der Knopf „Alle zeigen“. Mit „Zuordnen“ bleibt AB2 unverändert.
- **Sichtlegende** im Werkzeug „Ein-/Ausblenden“: je Zone mit Körpern ein Knopf, dazu „nicht zugeordnet (n)“. `aria-pressed` heißt sichtbar. Ein Klick schaltet die Körper der Zone. Ausgeblendet steht der Eintrag gedimmt und durchgestrichen, `title` und `aria-label` sagen „… einblenden“ bzw. „… ausblenden“. Der Klick wählt kein Ziel und ordnet nichts zu. Ein Körperklick ordnet in diesem Werkzeug auch nicht zu (`OrdnetZu` ist falsch).
- **Bauteilgruppen**: eine Kästchenliste neben der Legende, unabhängig vom Farbmodus. Gruppen: Außenwand, Innenwand, Dach, Decke, Bodenplatte (Boden), Fenster, Tür, Vorhangfassade, Sonstiges (`Bauteilartgruppe`, `GebaeudeAnsichtBauteilartgruppen.Von`). Es stehen nur Gruppen da, die vorkommen, mit Zahl in Klammern. Mit Bauteilarten zählt die Liste Bauteile, im Exportmodell ohne Klassifikation die Platten. Abgewählt fallen alle Körper und Flächen der Gruppe aus dem Bild: Bauteilkörper, Platten des Exportmodells und Raumdreiecke. Das gilt auch, wenn ihr Legendeneintrag sichtbar ist. Sichtbarkeit ist **Legende UND Gruppe** (`BauteilSichtbar`, `RaumSichtbar`).
- **Reine Anzeige**: Nichts wird gespeichert. Ein Wechsel des Farbmodus setzt die Sichtbarkeit der Legende zurück (Zonen, Gruppen R0–R7, Stufen, Befunde, Bauteilkörper), die Gruppenwahl bleibt. Export und Zuordnung bleiben unberührt. Der Grundriss bleibt, wie er ist.
- **Daten** (`GebaeudeAnsichtDaten.cs`): `Bauteilarten` (Kennung → Art des Kerns). Die Importhülle füllt sie aus dem Abbild (`GebaeudeImportAnsicht.Randgruppen`). Dazu `Bauteilgruppenzahlen()` und `Bauteilartbyte()`. Teil 6 des Bytefelds trägt je Raum mit `Dreiecksbauteile` ein Byte je Dreieck (255 = ohne Bauteil, immer sichtbar), Offset `BauteilgruppenAb`. Ohne Bauteilarten ist das Feld byteweise das bisherige.
- **Szene**: `Szene` und `SzeneDatei` nehmen `zonenAus` und `bauteilgruppensichtbar` an. Räume ausgeblendeter Zonen und Bauteilkörper ausgeblendeter Gruppen fehlen in der Szene. Platten einer abgewählten Gruppe kommen ohne Art. Die Szene trägt das Schalterfeld `bauteilgruppensichtbar` und je Dateikörper `bauteilgruppenAb`.
- **Modul** (`epos-gebaeude-koerper.js`): `bauteilgruppenFilter` liest die Bytes. `randaufbau` nimmt einen vierten, wahlfreien Dreiecksfilter. In jedem Farbmodus fallen Dreiecke einer abgewählten Gruppe aus dem Index. Im Modus „Zonen“ trägt das Netz dann `ursprung`. Was nicht gebaut ist, trifft der Raycaster nicht: Der Klick trifft den nächsten sichtbaren Körper.
- **Ressourcen**: 8 neue Schlüssel in beiden Sprachen (`GANS_WERKZEUG`, `GANS_WERKZEUG_ZUORDNEN`, `GANS_WERKZEUG_AUSBLENDEN`, `GANS_ALLE_ZEIGEN`, `GANS_EINBLENDEN`, `GANS_AUSBLENDEN`, `GANS_BAUTEILGRUPPEN`, `GANS_BAUTEILGRUPPE_ZAHL`). Die Gruppennamen kommen aus den vorhandenen `BTA_ART_*`. Designer neu, zweiter Lauf ohne Befund. Hilfeschlüssel oder Zählwachen der Ansicht gibt es nicht.
- **Stil** (`epos-ui.css`): Werkzeugleiste, Legendenblock (Legende und Gruppenliste nebeneinander, umbrechend), Zustand „aus“, Kästchenliste mit Touchziel, Regeln für erzwungene Farben.
- `GebaeudeDialog.razor` ist nicht angefasst. Der Baustein braucht keinen neuen Parameter.

## Tests

- bunit `EPOS.UI.Tests/Bausteine/GebaeudeAnsichtAusblendenTests.cs`, 7 Fälle, alle grün: Umschalter mit Vorgabe Zuordnen (in Gruppenmodi nur „Alle zeigen“); Klick im Werkzeug Ein-/Ausblenden setzt das Flag, wählt kein Ziel, ordnet nicht zu, und die Szene trägt den Raum nicht mehr; „Alle zeigen“ setzt zurück; Gruppenliste nur mit vorkommenden Gruppen und Zahl, ohne Arten nach Platten; Legende UND Gruppe am Fensterkörper; Farbmoduswechsel setzt die Legende zurück und lässt die Gruppen; Bytefeld Teil 6 und Szene.
- UI-Filter `GebaeudeAnsicht`: 58/58. `GebaeudeAusblenden|GebaeudeAufbau|GebaeudeBefund|GebaeudeImportZonen|GebaeudeDialog`: 207/207. `Stilblatt|Wache|Ressource`: 202/202.
- Kern `GebaeudeImportAnsicht|GebaeudeAufbauHuelle`: 38/38.
- Bau: Kern-Filter 0 Fehler, Windows-Schale (`EnableWindowsTargeting=true`) 0 Fehler.

## Probe

- **Rasterprobe, Legendenprobe** (`legendenprobe.mjs`, Wirt Release auf Port 5299, Chromium headless): 0 Verstöße in allen Breiten, die Gegenprobe mit der alten Regel ist wie vorgesehen rot (102). Die Probe misst die Ringlegende der Ergebnisübersicht, nicht die Gebäudeansicht. Der Wirt trägt die Gebäudeansicht nicht. `diagrammprobe.mjs` prüft das Modul der Ansicht nicht und lief deshalb nicht.
- **Browserprüfung des Moduls** (Chromium headless mit SwiftShader-WebGL; das echte `epos-gebaeude-koerper.js` mit three.js, ein Raumkörper aus zwei Dreiecken, Bauteilgruppen Außenwand und Boden): je Farbmodus „randbedingung“ und „zonen“ vier Fälle, **8/8 erfüllt**. Alle sichtbar: 6 Indizes, Fläche im Bild. Außenwand aus: 3 Indizes. Außenwand und Boden aus: 0 Indizes und 0 Pixel in Körperfarbe, also keine sichtbare Fläche. Gegenprobe wieder an: 6 Indizes und dieselbe Pixelzahl wie vorher. Seite und Läufer lagen im Scratchpad und sind nicht eingecheckt.

## Offene Punkte

- Windows-Sichtabnahme: Werkzeugwahl, gedimmte und durchgestrichene Einträge, Kästchenliste neben der Legende bei schmaler Breite, Klick durch eine ausgeblendete Zone auf den Körper dahinter.
- Eine eingecheckte Probe für das Körpermodul fehlt. Kandidat ist eine Seite im Rasterprobe-Wirt mit `GebaeudeAnsicht` und Dateikörper, nach dem Muster der Prüfung oben.
- MA-5 kann an der Sichtlegende ansetzen (Übernehmen/Verwerfen je Eintrag). Die Werkzeugwahl ist dafür als dritte Stellung erweiterbar.
- Im Exportmodell bleiben die Prismen der Räume bei abgewählten Gruppen stehen. Ausgeblendet werden nur ihre Platten, denn ein Prisma ist ein Raum, kein Bauteil.

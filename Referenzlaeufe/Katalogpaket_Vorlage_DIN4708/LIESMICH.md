# Paketvorlage für die Katalogwerte nach DIN 4708

**Zweck.** Die Kennzahl nach DIN 4708 braucht Katalogwerte, die in einer kostenpflichtigen Norm
stehen und deshalb weder im Repositorium noch in der Auslieferung liegen: die **Belegung je
Raumzahl**, die **Ausstattungsklassen** (Zapfstellenbedarf einer Wohnung, Σ v·w_v in Wh) und die
**Parameter des Verfahrens** (Koeffizienten, Zapfperiode, Einheitswohnung, Kappung, Zapfblöcke des
DIN-4708-Profils). Ohne sie bleibt in der Wohnungstabelle des Zapfprofils die Klappliste
„Ausstattungsklasse“ leer, die Belegung nach Raumzahl findet nichts, und der Normvergleich nach
DIN 4708 ist nicht rechenbar. Der Anwender trägt die Werte aus seinem **eigenen Normexemplar** ein
und spielt sie über den Katalogimport ein. Dieser Ordner ist die **Vorlage** dafür: zwei Dateien
des Importformats mit vollständigen Kopfzeilen und Beispielzeilen aus **Platzhaltern**.

**Keine Normzahl.** Jede Spalte `Wert` ist leer; Schlüssel wie „Klasse 1“ und Raumzahlen wie 1, 2,
3 sind Beispiele, keine Werte der Norm. Die Quelle trägt „Tabelle …“ bzw. „Abschnitt …“ ohne
Nummer. Der Wächter `EPOS.Kern.Tests/TwwKatalogimportDin4708Tests` hält die Dateien so.

**Die unveränderte Vorlage spielt nichts ein.** Jede Zeile ohne Wert ist im Bericht benannt
abgelehnt („Die Angabe Wert fehlt.“), und der Katalog bleibt, wie er ist. So kann eine halb
ausgefüllte Kopie keinen leeren oder erfundenen Wert in den Katalog schreiben.

**Die gefüllte Datei gehört nie ins Repositorium.** Sie bleibt beim Anwender. In der
Entwicklungsumgebung liegt eine gefüllte Kopie unter
[`Referenzlaeufe/Normzahlen/Katalogpaket_DIN4708/`](../Normzahlen/LIESMICH.md) und bleibt lokal:
`.gitignore` schließt den Ordner aus. Das Setup liefert allein diese Vorlage nach
`{app}\Vorlage\Katalogpaket_DIN4708` aus.

## Die zwei Dateien

| Datei | Zeilen der Vorlage | Inhalt |
|---|---|---|
| `Tab_TwwDin4708Wert_STAMM.csv` | 5 | je Zeile `Art`, `Schluessel`, `Wert` und die Herkunft (`Quelle`, `Ausgabe`, `Version`, `Herkunftsart`). Art `BELEGUNG`: Schlüssel ist die Raumzahl, Wert die Personenzahl. Art `AUSSTATTUNG`: Schlüssel ist ein neutraler Klassenname, Wert der Zapfstellenbedarf in Wh |
| `Tab_TwwParameter_STAMM.csv` | 11 | die Parameter des Verfahrens mit ihrer Einheit: `DIN4708.a1`, `DIN4708.a2` (1/h), `DIN4708.z` (h), `DIN4708.p_b` (P), `DIN4708.w_b`, `DIN4708.W_b` (Wh), `DIN4708.Kappung` (-), `DIN4708.Profil.Bloecke` (-) und je Zapfblock `DIN4708.Profil.Block.<k>.Beginn`, `.Dauer` (min) und `.Anteil` (-) |

Beide Dateien sind wahlfreie Dateien des Katalogimports. Ein Paket, das
`Tab_TwwDin4708Wert_STAMM.csv` und keine Datei der Nutzungsarten (Tagesgangsatz, Tagesgang,
Nutzungsart, Zapfkategorie) führt, ist ein **Kennwertpaket** und braucht
`Tab_TwwNutzungsart_STAMM.csv` nicht.

## Schritt für Schritt

1. **Den Ordner kopieren** — außerhalb des Repositoriums, etwa nach
   `…\EPOS-Katalogpakete\DIN4708\`. In diesem Ordner hier wird nichts ausgefüllt.
2. **Belegung je Raumzahl:** je Raumzahl eine Zeile der Art `BELEGUNG`. Die Raumzahl ist größer
   als 0 und steht in halben Schritten, mit Dezimal**punkt** (`2`, `2.5`); der Import speichert sie
   so, wie das Zapfprofil sie sucht (`2.0` wird `2`).
3. **Ausstattungsklassen:** je Klasse eine Zeile der Art `AUSSTATTUNG` mit einem eigenen, neutralen
   Namen; dieser Name steht danach in der Klappliste „Ausstattungsklasse“ der Wohnungstabelle.
4. **Parameter:** die Werte des eigenen Normexemplars in die Spalte `Wert` eintragen, Einheit wie
   vorgegeben. `DIN4708.Profil.Bloecke` sagt, wie viele Zapfblöcke das Profil hat; je Block die drei
   Zeilen `DIN4708.Profil.Block.<k>.…` anlegen (`k` ab 1).
5. **Herkunft füllen:** `Quelle` mit der Fundstelle (Tabelle, Abschnitt), `Ausgabe` (Ausgabestand,
   darf leer bleiben), `Version` (Stand des Pakets) und `Herkunftsart` — für ein Normpaket des
   Anwenders `IMPORT`.
6. **Erst prüfen:** in EPOS-Plan die Verwaltung der Brauchwasser-Nutzungsarten öffnen,
   **Import…** wählen, den Schalter **Nur prüfen, nichts schreiben** setzen und den Ordner (oder
   eine Datei darin oder ein ZIP-Archiv) wählen. Der Bericht zeigt je Zeile „würde anlegen“,
   „würde ersetzen“, „würde überspringen“ oder „würde ablehnen“ samt Grund.
7. **Einspielen:** denselben Import ohne den Schalter wiederholen.

**Die Katalogversion ist wahlfrei.** Ohne die Spalte `Katalogversion` treten alle Zeilen der
Katalogversion des Katalogs bei — dann sieht das Zapfprofil die Werte sofort. Wer eine eigene
Katalogversion führen will, ergänzt die Spalte `Katalogversion` in **beiden** Dateien; führt nur eine
der beiden sie, ist das Paket abgelehnt.

**Ersetzen statt Doppeln.** Eine Zeile ist über Art, Schlüssel und Katalogversion bestimmt, ein
Parameter über Schlüssel und Katalogversion. Steht sie mit gleichem Wert schon im Katalog, wird sie
übersprungen; mit anderem Wert trägt die vorhandene Zeile danach den neuen — an ihrem Platz, damit
eine Wohnung, die die Ausstattungsklasse gewählt hat, weiter darauf zeigt. Der Bericht nennt alten
und neuen Wert.

**Ablehnungsgründe.** Das Paket wird als Ganzes abgelehnt (und nichts geändert), wenn eine Spalte
unbekannt ist, eine Pflichtspalte fehlt, eine Zeile eine andere Feldzahl als die Kopfzeile hat oder
eine Zahl keine ist (Komma statt Punkt); der Bericht nennt Datei, Zeile und Spalte. **Nur die
einzelne Zeile** wird abgelehnt, wenn ihr der Wert oder eine Herkunftsangabe fehlt, die Art weder
`BELEGUNG` noch `AUSSTATTUNG` ist, die Raumzahl nicht über 0 oder nicht in halben Schritten steht,
der Wert nicht größer als 0 ist oder — beim Parameter — Schlüssel, Einheit oder Bereich nicht
passen.

# DHWcalc-Referenzdateien aus OpenDHW (nur Testumfang)

Testorakel des Zapfprofilgenerators, Stufe Z3 (Umsetzungskonzept Zapfprofilgenerator 4.4 Ende und
Kapitel 6: „OpenDHW (MIT) nur als Testorakel mit Attribution im Testordner, nicht im
Auslieferungspaket"). Gelesen allein vom Test `EPOS.Kern.Tests/ZapfprofilDhwcalcVergleichTests.cs`;
kein Rechenweg, kein Katalog, keine Auslieferung liest diese Dateien.

## Quelle

- Repository: <https://github.com/RWTH-EBC/OpenDHW>, Ordner `DHWcalc_Files/`
- Stand: Commit `b62ac8b46961e428d8075a6f12ac50135e564914` (21.09.2026), geholt am 23.09.2026 mit
  `git clone --depth 1`
- Die Dateien sind Ausgaben des Programms DHWcalc (Universität Kassel, U. Jordan und K. Vajen), die
  OpenDHW als Vergleichsdaten mitliefert.

## Dateien

| Datei | Inhalt |
|---|---|
| `200L_1min_4cat_sf_nods_max1200.txt.gz` | Zapfprofil eines Einfamilienhauses über 365 Tage in Minutenschritten: 525 600 Zeilen, je Zeile der Volumenstrom der Minute in l/h (mittlere Tagesmenge 200 l/d, vier Zapfkategorien, ohne Sommerzeit, größter Volumenstrom 1200 l/h). Unverändert, nur mit `gzip -9 -n` gepackt (die entpackte Datei ist 4,2 MB groß). SHA-256 der entpackten Originaldatei: `0e870c76c5fec3791471dd4ad1ee1cf76a613031d2d83e787d78e645d17d9105` — der Test prüft ihn. |
| `200L_1min_4cat_sf_nods_max1200_log.txt` | Die Protokolldatei von DHWcalc zu diesem Profil, unverändert: Tagesmenge, Zeitschritt, die vier Kategorien (mittlerer Volumenstrom, Dauer, Anteil, Streuung), kleinster und größter Volumenstrom, der Tagesgang als Stufenfunktion für Werktage und Wochenendtage, das Verhältnis Wochenende/Werktag und die jahreszeitliche Schwankung. Der Test liest die Parametrik daraus; sie steht nicht im Quelltext. |

Die Parametrik ist die frei dokumentierte Jordan/Vajen-Parametrik (Kapitel 6: frei); keine Normzahl,
keine Hersteller- oder Produktdaten.

## Lizenz

Die Dateien stammen aus OpenDHW und stehen unter der MIT-Lizenz:

```
MIT License

Copyright (c) 2024 RWTH Aachen University - E.ON Energy Research Center - Institute for Energy Efficient Buildings and Indoor Climate

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Vergleich

Kein Bitvergleich — DHWcalc zieht anders als der Generator (Wahrscheinlichkeit je Zeitschritt,
Kappung der Summe). Verglichen werden Verteilungsgrößen: die mittlere Tagesmenge (±5 %), das
Verhältnis der Minutenspitze eines Tages zum Tagesmittel und die Zahl der Zapfminuten je Tag; der
Median des Generators muss in der mittleren Hälfte (P25 bis P75) der Tage der Datei liegen. Die
Korridore leitet der Test aus der Datei ab; Begründung im Test.

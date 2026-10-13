# Paketvorlage „Typtage nach VDI 4655“

**Zweck.** Der Jahresgang einer Zone kann über Typtage nach VDI 4655 laufen. Die Werte dafür stehen
allein in der kostenpflichtigen Richtlinie; das Programm bringt keinen davon mit, und sie gehen weder
in die Auslieferung noch in einen Projekttransfer. Der lizenzierte Anwender baut sich aus seinem
**eigenen Exemplar** ein Paket und spielt es im Dialog „Typtage nach VDI 4655“ ein. Dieser Ordner ist
die **Vorlage** dafür: die sechs CSV-Dateien des Paketlesers mit vollständigen Kopfzeilen und
Beispielzeilen aus **Platzhaltern**.

**Keine Werte der Richtlinie.** Jede Zahl, die aus der Richtlinie käme — Kalendertage, Faktoren,
Grenzen, Anteile —, ist leer. Codes („T01“ …), Gebäudeart („Gebaeudeart_A“) und Zone („1“, „Zone A“)
sind Platzhalter, keine Bezeichnungen der Richtlinie. Auch abgeleitete Werte gehören nicht hierher.
Der Wächter `EPOS.Kern.Tests/TwwTyptagVorlageTests` hält die Dateien so.

**Die unveränderte Vorlage spielt nichts ein.** Schon die Prüfung nach „Paket wählen…“ lehnt sie
benannt ab: Die Zahl der Kalendertage fehlt (Datei `typtage_je_zone.csv`, Zeile 2, Spalte `anzahl`).
„Einspielen“ bleibt damit gesperrt, und die Datenbank bleibt, wie sie ist.

**Die gefüllte Kopie gehört nie ins Repositorium.** Sie bleibt beim Anwender. In der
Entwicklungsumgebung liegt sie unter
[`Referenzlaeufe/Normzahlen/Typtagpaket_VDI4655/`](../Normzahlen/LIESMICH.md) und bleibt lokal;
`.gitignore` schließt den Ordner aus. Das Setup liefert allein diese Vorlage nach
`{app}\Vorlage\Typtagpaket_VDI4655` aus.

## Die sechs Dateien und ihre Fundstellen

Die Fundstellen nennen die Stelle im eigenen Exemplar (Ausgabe Juli 2021); Werte stehen hier keine.

| Datei | Spalten | Inhalt | Fundstelle in der Richtlinie |
|---|---|---|---|
| `typtage.csv` | `code;jahreszeit;tagart;bewoelkung` | die Typtagkategorien: `jahreszeit` `uebergang`, `sommer` oder `winter`; `tagart` `werktag` oder `sonntag`; `bewoelkung` `heiter`, `bewoelkt` oder `ohne`. Je Jahreszeit und Tagart entweder eine Kategorie `ohne` oder je eine `heiter` und `bewoelkt` | Abschnitt 2 (Begriffe), Tabelle 1 (ohne PV) bzw. Tabelle 2 (mit PV) |
| `klimazonen.csv` | `zone;bezeichnung` | die Klimazonen, `zone` ganzzahlig ab 1, `bezeichnung` wahlfrei | Abschnitt 5, Tabelle 3 und Bild 2 |
| `typtage_je_zone.csv` | `zone;gebaeudeart;typtag;anzahl` | die Zahl der Kalendertage je Zone, Gebäudeart und Typtag; ganzzahlig, Summe je Zone und Gebäudeart genau 365 | Tabelle 4 (Bestand), Tabelle 6 (Niedrigenergiehaus); mit PV Tabelle 9 und 11 |
| `f_twe_tt.csv` | `gebaeudeart;zone;typtag;faktor` | der Faktor der Tagesenergie Trinkwassererwärmung; darf negativ sein | Abschnitt 6.4, Tabellen 17 bis 31 (Bestand) und 32 bis 46 (Niedrigenergiehaus) |
| `kennwerte.csv` | `schluessel;wert;text` | `quelle` (Text, Pflicht), `ausgabe` (Text), `wintergrenze`, je Gebäudeart `heizgrenze.<gebaeudeart>`, `bewoelkung.schwelle` (Pflicht, sobald eine Kategorie nach Bewölkung unterscheidet), wahlfrei `pruefsumme.toleranz` | Abschnitt 2 (Begriffe Wintertag, Heizgrenztemperatur, heiter/bewölkt) |
| `tagesgaenge.csv` (wahlfrei) | `gebaeudeart;typtag;aufloesung_min;index;anteil` | der normierte Tagesgang je Typtag; `aufloesung_min` teilt 1440 und ist Teiler oder Vielfaches von 60, `index` läuft lückenlos ab 0, Summe der Anteile 1. Ohne diese Datei trägt der Tagesgangsatz der Zone die Tagesform | Abschnitt 6.4 und Anhang D (Referenzlastprofile) |

## Formregeln

UTF-8, Kopfzeile mit den Spaltennamen genau wie oben, Trenner Semikolon oder Komma, Zahlen mit
Dezimal**punkt** (auch Exponentialschreibweise), ein leeres Feld heißt „fehlt“. Reihenfolge der Zeilen
und Spalten ist frei; eine unbekannte oder fehlende Spalte, ein Feld ohne Zahl, ein doppelter
Schlüssel, eine Tagessumme ungleich 365 oder eine unvollständige Zone lehnen das Paket als Ganzes ab,
und der Bericht nennt Datei, Zeile und Grund. Die Gebäudeart ist ein frei gewählter Name; er steht
danach in der Auswahl „Gebäudeart“ des Zapfprofils.

## Schritt für Schritt

1. **Den Ordner kopieren** — außerhalb des Repositoriums, etwa nach `…\EPOS-Typtage\VDI4655\`. In
   diesem Ordner hier wird nichts ausgefüllt.
2. **Typtage und Klimazonen** nach dem eigenen Exemplar anlegen (Codes frei wählbar), dazu die
   Gebäudearten, die gebraucht werden.
3. **Je Zone, Gebäudeart und Typtag** eine Zeile in `typtage_je_zone.csv` und in `f_twe_tt.csv`
   anlegen und die Werte eintragen.
4. **Kennwerte eintragen:** `quelle` mit Richtlinie und Ausgabe, die Grenzen des Verfahrens, je
   Gebäudeart eine Heizgrenze.
5. Wahlfrei die **Tagesgänge** ergänzen.
6. **Prüfen:** im Dialog „Typtage nach VDI 4655“ (aus dem Katalog Brauchwasser-Nutzungsarten oder dem
   Zapfprofil, Stufe Experte) **Paket wählen…** und den Ordner, eine Datei darin oder ein ZIP-Archiv
   wählen. Der Prüfbericht nennt die Ablehnung mit Datei, Zeile und Grund oder, was eingespielt würde,
   samt Hinweisen; geschrieben wird dabei nichts.
7. **Einspielen:** Taugt das Paket, schreibt **Einspielen** es; ein vorhandener Satz wird nach einer
   Rückfrage vollständig ersetzt.

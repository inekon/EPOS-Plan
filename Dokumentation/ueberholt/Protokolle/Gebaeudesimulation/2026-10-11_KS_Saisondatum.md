# KS — Datum der Saison und der Kalenderfelder (11.10.2026)

Meldungen des Anwenders vom 11.10.2026: „Saison: Datum lässt sich nicht angeben (nur Monat und Tag)“,
„Format-Hinweis geben“, „Kalender: Saison nur Tag und Monat“ (Datumswähler mit Jahr 2000) und „Saison im
Kalender nicht sichtbar, auch wenn angegeben“.

## Ursachen

1. **Prüfung je Tastendruck.** `EPOS.UI/Bausteine/Gemeinjahrdatum.razor` prüfte im `@oninput`: Schon „1“
   färbte das Feld und zeigte „Ungültiges Datum …“ (Bildschirmfoto: „10“, rot).
2. **Halbe Saison verlor ihren Text.** `KonditionierungMatrix.razor` hielt Beginn ohne Ende in `_saison`, gab
   dem Feld aber `Bearbeitung.Zeiten(...)` (noch leer) als `Wert` zurück; beim nächsten Zeichnen setzte
   `OnParametersSet` den gültigen Text („10.1“) auf leer zurück.
3. **Datumswähler mit Jahr.** `KalenderbedienungAbschnitt.razor` (Karte unter der Matrix) führte Saison,
   Ferien, Zeiträume und Einzeltage als `Datumsfeld` (`<input type="date">`) in einem Darstellungsjahr
   (Preisreihenjahr oder Gemeinjahr ab 2001) — das Jahr erschien in der Anzeige.
4. **Saison im Jahresraster unsichtbar.** Der Kern färbt richtig: `Kalenderbedienung.Jahresraster` markiert
   die Tage außerhalb als `Rastertagart.Saison`, sobald die Saison mit Beginn **und** Ende steht (neuer Test
   `KalenderbedienungTests.Die_Saison_der_Kuehlung_steht_im_Jahresraster_der_Kuehlung`, mit und ohne
   angelegten Kalender: grün ohne Änderung). Die Karte des Anwenders trug nur „von“ (01.05.); `SaisonFeld`
   verwarf die halbe Angabe still — gespeichert war nichts, also nichts zu färben. Rechenweg unberührt.

## Behebung

- **Eine Lese- und Anzeigefunktion im Kern:** `EPOS.Kern/.../Konditionierung/Gemeinjahrdatumtext.cs` —
  „TT.MM.“, „TT.MM“, „T.M.“, „T.M“, Komma statt Punkt, „TT.MM.JJJJ“ (Jahr verworfen, Hinweis „Ohne Jahr …“);
  29.02. und unmögliche Tage abgelehnt; Anzeige einheitlich „TT.MM.“. `Gemeinjahrdatum.Lesen/Anzeigetext`
  rufen sie.
- **`Gemeinjahrdatum`:** `oninput` hält nur den Text (nichts geprüft, verworfen, gemeldet, auch nicht bei
  einem Neuzeichnen des Wirts); `onchange` (Verlassen, Enter) prüft, meldet und zeigt „TT.MM.“. Neue
  Parameter `Hinweis` (sichtbare leise Zeile und `title` am Feld; die Fehlerzeile ersetzt sie) und
  `JahrHinweis`.
- **Matrix:** die halbe Saison geht als `Wert` ans Feld zurück; eine halbe Saison nennt die Zelle
  (`KOND_TXT_SAISON_HALB`).
- **Umgestellt auf `Gemeinjahrdatum`** (Platzhalter „Beginn TT.MM.“/„Ende TT.MM.“, Formathinweis):
  Saison der Matrix (Heizen, Kühlen …), Saison der Kalenderkarte, Ferienliste der Kalenderkarte,
  Zeitraum- und Einzeltag-Editor der Kalenderkarte (Einzeltag ohne Platzhalter), Perioden der
  `Periodenliste` (Von/Bis), Ferien des `BetriebskalenderDialog`. Die Karte nennt eine halbe Saison.
- **Bewusst nicht umgestellt:** jedes `Datumsfeld` außerhalb der Konditionierung mit echtem Kalenderdatum
  samt Jahr (Preisstand, Tarife, Energieträger, Import- und Wirtschaftlichkeitsdaten). In der
  Konditionierung bleibt kein `type="date"`.
- **Gespeichert wird wie bisher** der Jahrestag 1 … 365 (Saison über `SaisonSetzen`, Ferien und Zeilen über
  `Kalendertage.Jahrestag`). Das Darstellungsjahr der Karte bleibt nur für Wochentage und die Rückwandlung.

## Regel des Jahresrasters über mehrere Größen

Das Jahresraster der Kalenderkarte zeigt stets **eine** Größe (die der Karte). „Außerhalb der Saison“
färbt es nach der Saison dieser Größe; die Legende „Zeile für mehrere Größen“ markiert Zuordnungszeilen,
die in mehreren Größen gelten, nicht die Saison. Eine Ansicht „alle Größen“ hat das Raster nicht.

## Bestandswerte mit 29.02.

Der Datumswähler bot in einem Schaltjahr (Darstellungsjahr = Preisreihenjahr) den 29.02. an;
`Kalendertage.Jahrestag(2, 29)` setzte ihn beim Speichern auf Tag 59 (28.02.). Gespeichert war und ist
daher nie ein 29.02., sondern ein Jahrestag 1 … 365; der Leser (`Standardfahrplan.Saisonperiode`) weist
Werte außerhalb 1 … 365 als `SaisonUngueltig` aus. Nichts wird umgedeutet. Das neue Feld lehnt den 29.02.
ab.

## Nachweis

- Rot vor der Behebung: `KonditionierungMatrixTests.Der_Saisonbeginn_laesst_sich_Zeichen_fuer_Zeichen_eingeben`
  (Meldung schon bei „1“).
- Grün danach: UI-Filter (Gemeinjahr, Kalender, Konditionierung, Periodenliste, Betriebskalender, Wache,
  Stilblatt, Saison, Datum, Vorgabe) 516 von 516; Kern-Filter (Kalender, Konditionierung, Vorgabe, Saison, Gemeinjahr, Datum, Wache, Ressource) 1472 von 1472.

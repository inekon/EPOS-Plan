# Protokoll Steife Zone — Abschnittsregel bricht nicht mehr ab (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Statuszeile #743, Commits `c325c1b7d`, `ecac51252`.
**Entscheide:** keine neuen; kein Schemaschritt, keine neue Basis (R38 bleibt).

## 1 Auftrag

Anwenderbefund vom 05.10.2026 (Windows, Debugger): Beim Gebäude „Sportheim_1970_unsaniert“ (10719) wirft `Zonenmodell2K.AbschnittsregelVerletzt` eine `GebaeudeModellException`: „Ein Abschnitt im Betriebsfall HeizenGeregelt bucht die Leistung -46.6743 W mit falschem Vorzeichen“. Die Rechnung soll durchlaufen, ohne die Abschnittsregel aufzuweichen.

## 2 Vorgehen

Ein Opus-Agent fand die Ursache mit einer Zufallsprobe und Nachrechnung und behob sie in zwei Schritten; ein Sonnet-Agent schrieb die Papiere. Gate 743 fährt die Orchestrierung danach.

## 3 Ergebnis

**Ursache.** Der Löser in `Zonenmodell2K.cs` findet bei einer steifen Zone (Eigenwerte −531 und −0,059 1/s, Zeitkonstanten 2 ms und 17 s) die innere Umkehr der geregelten Heizlast nicht. `InnenUmkehr` prüft den Vorzeichenwechsel der Ableitung an Anfang und Ende (am Ende praktisch null); der Goldene Schnitt in `ErsteInnereVerletzung` sieht nur die gleichbleibende Ebene und verfehlt das kurze Minimum am Anfang. Das negative Mittel (−48 992 W im Probefall) ist echt, kein Rundungsfehler. Die Übergabefälle `UebergabeGesaettigt` und `UebergabeRegelbereich` hatten eine ähnliche Lücke: 290 von 400 000 Zufallszonen brachen über 24 h ab.

**Behebung.**
- `c325c1b7d`: In `ErsteInnereVerletzung` gilt die exakte Nullstelle der Ableitung (`NullstelleAbleitung`) als Minimum, wenn der Goldene Schnitt kein verletztes Minimum findet. Der geregelte Abschnitt endet am ersten Nulldurchgang, danach läuft die Zone frei als `Totband`.
- `ecac51252`: Für die Übergabefälle sucht `ErsteInnereVerletzungLeitwert` den ersten Austritt aus dem gültigen Band, nur wenn das Mittel die Abschnittsregel sonst bräche.

Beide Wege greifen nur, wo die Rechnung bisher abbrach.

## 4 Festlegungen

Keine Klemmung, keine Toleranz, kein neuer Fall oder Zähler, keine Schemaänderung. Die Abschnittsregel bleibt für echte Vorzeichenfehler scharf. Kein Wiki-Eintrag (Sonderfall).

## 5 Nachweise

Rechenprobe `EPOS.Kern.Tests/ZonenmodellSteifeZoneTests.cs`, 3 Tests: (1) steife Zone — vorher Abbruch mit −48 991,6 W, jetzt zwei Abschnitte (HeizenGeregelt 7,8e‑5 s und Totband), Heizleistung ≥ 0, Bilanz geschlossen; (2) Gegenfall mit erzwungenem geregelten Abschnitt wirft weiterhin; (3) Übergabefall — jetzt 6 Abschnitte, 56 W. Abnahme des Agenten: Kern-Filter 0 Fehler; 1 673 Tests grün (1 übersprungen); Referenzlauf 21/21 PASS gegen R38 (6 872 111 Werte), byte-gleich außer `protokoll.txt`; Zufallsprobe ohne Abbruch. Gate 743 steht in der Statuszeile #743 der Statusdatei.

## 6 Offenes

- 3 der 290 Übergabefälle enden jetzt mit dem benannten Fehler `AbschnittsdeckelErreicht` (kein Vorzeichenfehler).
- Die Vorprüfung `InnenUmkehr` übersieht in steifen Zonen kleine innere Umkehrungen bei zulässigem Mittel weiterhin. Folgeauftrag: Prüfung über `NullstelleAbleitung`, dann womöglich neue Basis.
- Beim Sportheim prüft der Anwender Zonen mit sehr kleiner Bauteilmasse hinter sehr kleinen Widerständen (winzige Zonen aus dem Zonenbaum, Bauteile ohne Masse, große Glasflächen): Die Rechnung läuft jetzt durch, solche Zonen bleiben physikalisch fragwürdig.
- Sichtabnahme am Sportheim unter Windows, CI-Kennung nach dem Push.

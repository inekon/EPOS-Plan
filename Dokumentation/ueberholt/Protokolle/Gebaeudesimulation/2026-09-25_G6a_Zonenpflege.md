# Protokoll G6a — Pflege mehrerer Zonen je Gebäude (25.09.2026)

**Auftrag.** Stufe G6a der Gebäudesimulation: mehrere Zonen je Gebäude pflegen — anlegen, öffnen,
duplizieren, entfernen, umordnen —, Prüfregeln über die ganze Liste, eine Formel für die
Zonenkennwerte, Wächter für alle Kopierwege, KI-Sicht und Bericht-Zonentabelle. Die Rechnung bleibt
bei genau einer Zone; zwei und mehr lehnt der Lauf benannt ab (gerechnet wird das mit G6b). Kein
Schemaschritt; die Referenzbasis bleibt byte-gleich, weil kein Referenzprojekt Zonen hat.

**Anwenderentscheide vom 25.09.2026 (E46, Konzept N1.50):** A1 — eine zweite Zone ist speicherbar,
mit Rückfrage und Sperrzeile; der Freigabeschalter im Kern steht an und wird für eine Auslieferung
vor G6b ausgeschaltet; Wiki und Logbuch erst mit G6b. A2 — „Aus dem Projekt entfernen" fragt bei
einem Gebäude mit Zonen nach und nennt Zonen und Bauteile. A3 — höchstens 50 Zonen je Gebäude als
vorläufige Konstante (M12 bleibt bis G6c offen).

## 1 Wellen

| Welle | Inhalt | Gate |
|---|---|---|
| W1 | `GebaeudeZonenregeln` (Laufgrenze 1, Pflegegrenze 50, Freigabeschalter); `GebaeudeZonenCtrl.Pruefen` über die Liste (Höchstzahl, Nutzfläche ab zwei Zonen Pflicht, doppelte positive Ids), neu `Hinweise`; `Zonenkennwerte` als einzige Formel (Fläche, Volumen, H_T, H_ve, Bauteile), `Zonensummen.HT` als Hülle darauf; `GebaeudeArbeitsstand` über Ids (`ZoneAnlegen`, `NeueZone`, `ZoneErsetzen`, `ZoneEntfernen`, `ZoneDuplizieren`, `ZoneVerschieben`; neue vorläufige Ids unter allen vergebenen) — behebt die stille Löschung durch das frühere `ZoneSetzen`; `ZoneDaten.VorlageId`; die Hülle prüft vorab; die Bedarfsauskunft nennt den benannten Grund; `SIMENG_G3_MEHRERE_ZONEN` neu gefasst | Referenzlauf 14/14 PASS gegen R16, byte-gleich |
| W2 | Rundlauf über jede Spalte (`SELECT *`) der sieben Tabellen für Duplikat, Variante, Transfer in eine Datenbank ohne Zonen, Löschen des Gebäudes und des Projekts, gewöhnliches Speichern; nach G4b auch der Importweg der Gebäudeliste; Planwächter „zwei Fremdschlüssel → `KINDER`" | Referenzlauf 14/14 PASS gegen R16, byte-gleich |
| W3 | Zonenreiter als Liste mit ▲▼, Öffnen, Duplizieren, Entfernen, Summenfuß und Hinweisen; „+ Neue Zone …" weich gesperrt (Katalogsatz, Tagesbilanz-Weg, Höchstzahl, Freigabeschalter); Rückfragen vor der ersten und der zweiten Zone, Sperrzeile ab zwei Zonen; `ZonenDialog` mit Pflichtfläche; Projektzeile „N Zonen"; Rückfrage „Aus dem Projekt entfernen" (A2); KI-Sicht der Zonenliste | Referenzlauf 14/14 PASS gegen R18 und nach dem Merge von G4b gegen R19, byte-gleich |
| W4 | Tabelle „Zonen" je Gebäude mit Zonen im Gebäudeblock der Projektbeschreibung (Zone, Nutzfläche, Volumen, H_T, H_ve, Bauteile, Summenzeile; Werte nur aus `Zonenkennwerte`; entfällt ohne Zonen), `ProjektDetails` liest die Zonen einmal je Projekt; vier Zonenmerkmale im `AbweichungsErmittler` (Zahl der Zonen, Σ Nutzfläche, Σ H_T, Rechenweg der Hülle) | Referenzlauf 14/14 PASS gegen R19, byte-gleich |

## 2 Befund der Wächter

Der Rundlauf über jede Spalte hat eine Lücke des Projekttransfers aufgedeckt: Beim Einlesen eines
`.wpx` wurde aus NULL in einer Textspalte ein Leertext (`ProjektExportImportCtrl.Passe` wandelte
`DBNull` mit `Convert.ToString`), etwa `Quellkennung`, `Quelle` und `Uebergabe_Art` — gegen die Regel
„NULL bleibt NULL" (Softwarearchitektur 2.6). Behoben in W2; kein Referenzprojekt ist betroffen.
Der Planwächter fand keine Lücke: Jede Tabelle mit zwei oder mehr Fremdschlüsseln auf Plantabellen
steht in `KINDER` oder trägt ein eigenes `ID_Projekt`; die Ausnahmeliste ist leer.

## 3 Abweichungen vom Auftrag

- `GebaeudeZonensatz.EineZone` liest die Laufgrenze aus der Regelklasse (W1 Nr. 1); die Regel selbst
  ist unverändert.
- Der Flächenhinweis gilt erst ab zwei Zonen: Eine einzelne Zone trägt nach der Übernahme mit
  Hochrechnung bewusst die Fläche des wirklichen Gebäudes.
- H_ve der Zone folgt der Regel des Laufs (Fläche der Zone × Raumhöhe des Gebäudes); Raumhöhe und
  Volumen der Zone gehen mit G6b ein.
- Der Haltepunkt H-B entfiel: BV-E2 lag nach dem Merge vor W4 auf origin; die Zonentabelle steht im
  Kapitel „Projektbeschreibung" der Kapitelordnung.
- Die Zonenmerkmale des Variantenvergleichs gelten über alle Gebäude des Projekts, nicht nur über das
  erste wie die übrigen Gebäudemerkmale.
- `ProjektpflegeTests.P7` bleibt unverändert (Projekt 1030 hat keine Gebäude); die Zeilenzählung der
  Zonentabellen tragen die Rundlaufproben.

## 4 Offen

- **Freigabeschalter `GebaeudeZonenregeln.MehrereZonenFreigegeben` vor jeder Auslieferung ohne G6b
  ausschalten** (E46/A1): Dann trägt ein Gebäude höchstens eine Zone, und „+ Neue Zone …" nennt die Sperre.
- **Wiki und Logbuch** mit G6b (A1). Entwurf des Logbuch-Satzes: „Ein Gebäude im Projekt kann
  mehrere Zonen führen; die Simulation rechnet sie ab Version … ."
- **Windows-Sichtabnahme** des Zonenreiters.

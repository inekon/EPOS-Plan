# Protokoll BK-8c — Wiederimport-Vergleich und Normverweise

Auftrag BK-8c, Nachzug zu [BK-8b](2026-10-16_BK-8b_Normgerechte_Teilung.md) nach der Vorschau am echten Katalog, zum
[Entwurf Bauteilkatalog](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md) (Abschnitt 5.3)
und zu den [Rechenschritten 3.1](../../../aktuell/Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md). Arbeitsort Worktree
`bk8c`, Zweig `bk8c`, Basis `b13c7edd7`, Testdatenbank 235. Kein Schemaschritt; der Rechenweg der Simulation bleibt
unverändert.

## Befund aus der Vorschau

Die Vorschau über den Export des Anwenderkatalogs (Stand 233, nur Zählungen):

- 43 bisher abgelehnte Aufbauten (Abschnitte mit verschiedener Schichtzahl) werden geteilt, 174 Teilschichten kommen hinzu;
  7 bleiben abgelehnt („Abschnitt ohne Flächenanteil“), keine neue Ablehnung.
- Hinweis „Wiederimport“: 43 Aufbauten bekämen andere Schichten, davon mit U-Änderung über 1 %: 0, größte Änderung leer
  („keiner verglichen“).

## Ursache

- Die 43 stehen als Importsatz (`Herkunft` IMPORT) im Katalog — aus einem Import vor BK-5. Der damalige Import schrieb die
  Abschnitte ungeprüft, jede Schicht mit ihrem Abschnitt, auch bei verschiedener Schichtzahl je Abschnitt. Ab BK-5 lehnte der
  Import diese Aufbauten ab, der Katalogsatz blieb im alten Stand.
- `WiederimportVergleichen` rechnete den Vergleichswert allein mit `BauteilaufbauCtrl.Kennwerte` am alten Katalogsatz. Für
  einen Satz mit verschiedener Schichtzahl je Abschnitt lehnt die Abschnittsprüfung (`InhomogeneSchichten.Pruefen`, Ebenen
  mit verschiedenen Abschnittsnummern) ab; `Kennwerte` liefert dann kein U, nur einen Grund. Der Vergleich übersprang den
  Aufbau still: gezählt unter „bekämen andere Schichten“, nicht verglichen, nicht als Lücke ausgewiesen. Daher 43 / 0 / leer.

## Lösung

- **Vergleichswert** in dieser Reihenfolge: das U, das der Kern für den bestehenden Katalogsatz heute rechnet (derselbe Weg
  wie Aufbaueditor und Katalogliste, `BauteilaufbauCtrl.Kennwerte`); lässt sich keins rechnen, der gespeicherte U-Wert der
  Quelle am Katalogsatz (`U_Quelle`, aus den Zusatzfeldern des gelesenen Satzes). Begründung: Das gerechnete U ist der Wert,
  den der Anwender heute im Editor sieht und mit dem ein neu übernommenes Bauteil rechnen würde; wo der alte Satz nicht
  rechenbar ist, ist `U_Quelle` der einzige Wert, den der Katalog zu diesem Aufbau führt.
- Ergibt weder der alte Satz noch `U_Quelle` einen Wert, oder ergibt der neue Stand kein U, zählt der Aufbau als **ohne
  Vergleichswert** statt still auszufallen.
- `BauteilkatalogVorschau` führt dazu `WiederimportVerglichen`, `WiederimportGegenQuelle` und
  `WiederimportOhneVergleichswert`. Die Vorschauzeile `BKI_WIEDERIMPORT` (beide Sprachen) nennt: bekämen andere Schichten /
  verglichen / davon über 1 % / größte Änderung / ohne Vergleichswert; ohne Vergleich steht für die größte Änderung ein
  Strich statt „0,0“. Eine Protokollzeile der Vorschau nennt, wie viele gegen den U-Wert der Quelle verglichen wurden.
- Für den Katalog des Anwenders ist zu erwarten, dass die 43 nun gegen `U_Quelle` verglichen werden, soweit der Export einen
  U-Wert führt; die Zahl steht in der Protokollzeile der nächsten Vorschau.

## Normverweise

Maßgebend ist die beim Anwender vorliegende Ausgabe DIN EN ISO 6946:2018-03 (EN ISO 6946:2017) mit Berichtigung 1:2023-04.
Die Anwendungsgrenze „oberer zu unterer Grenzwert höchstens 1,5“ steht in 6.7.2.2, die Fehlerabschätzung (bei 1,5 höchstens
20 %) in 6.7.2.5. Nachgezogen in `InhomogeneSchichten` (Kommentare), den Rechenschritten 3.1 (B0c, Fehlerschranke), dem
Entwurf Bauteilkatalog 5.3, der Wiki-Quelle „Baustoffe und Bauteilaufbauten“ und als Nachtrag in den Protokollen BK-8 und
BK-8b. Die Wiki-Quelle beschreibt dazu die Zahlen der Vorschau beim Wiederimport.

## Tests

`EPOS.Kern.Tests/BauteilkatalogInhomogenTests` (synthetische Probe, runde Werte), zwei neue Fälle:

- Katalogsatz ohne Abschnitte (alter Stand: 100 mm λ 0,04 + 100 mm λ 0,035), Wiederimport mit versetzten Grenzen: verglichen
  gegen das U des Kerns; Handrechnung nach 6.7 R'_T 3,5063, R''_T 3,2627, U neu 0,295466, U alt 0,180925, Änderung 63,31 %;
  Zählung 1 / verglichen 1 / gegen Quelle 0 / über 1 % 1 / ohne Vergleichswert 0.
- Katalogsatz eines frühen Imports (Abschnitte mit 2 und 1 Schicht) ohne `U_Quelle`: zählt ohne Vergleichswert, größte
  Änderung leer, die Zeile nennt „verglichen 0“, „– %“ und „ohne Vergleichswert 1“. Derselbe Satz mit `U_Quelle` 0,3:
  verglichen gegen die Quelle, Änderung 1,51 % (über 1 %), Zeile „1,5 %“.
- Der bestehende Vergleichsfall prüft zusätzlich verglichen 1, ohne Vergleichswert 0.

## Abnahme

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler. Windows-Schale (`EnableWindowsTargeting=true`): 0 Fehler.
- `EPOS.Kern.Tests` mit Filter Bauteil, Inhomogen, Abschnitt, DokumentationLinkWache, WikiProduktdatenWache: 564 grün,
  1 übersprungen. `EPOS.UI.Tests` mit Filter Bauteilkatalog: 9 grün.
- Kein Referenzlauf: Geändert sind allein die Zählung der Vorschau des Katalogimports, ihre Anzeige und Texte; weder der
  Import schreibt anders noch rechnet die Simulation anders.

## Logbuch

Kein Eintrag: Nur die Vorschauzeile des Wiederimports wird genauer.

## Offen

- Die nächste Vorschau am Katalog des Anwenders nennt, wie viele der 43 gegen den U-Wert der Quelle verglichen werden und
  wie viele ohne Vergleichswert bleiben.

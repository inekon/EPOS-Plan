# Berichte & Kosten — Redesign nach Design-PDF V1

Stand: 18.08.2026 · Vorlage: `Bericht_Wirtschafltichkeit-Design-V1.pptx/.pdf` (Repo-Wurzel)

## Auftrag

Der Tab trug nur zwei Knöpfe („Kosten", „Varianten" → modaler Dialog `Form_Variantentest`).
Soll laut PDF: **vertikale Navigation wie in der Detaillierten Simulation** mit vier Bereichen,
Komponenten-Unterschiede je Variante sichtbar, und **je abweichender Komponente die Möglichkeit,
sie aus einer anderen Version (Stamm oder Variante) zu überschreiben**.

## Umsetzung R4a — Tab-Umbau

Neue Dateien unter `Views\BerichteKosten\`: `UcBerichteKosten.cs` (Hub, OwnerDraw-Navigation
208 px, WP-Admin-Palette, GDI+-Icons, Seiten lazy), `UcBkUebersicht.cs`, `UcBkKosten.cs`.
`Form_Wirtschaftlichkeit`/`Form_Bericht` wurden zu `UcWirtschaftlichkeit`/`UcBericht` gehoben
(reiner Verschiebe-Diff, beide waren Code-Formulare ohne Designer); die Formulare bleiben als
dünne Dialog-Wrapper für Altaufrufer. Einstieg über `Form_Start.BaueBerichteKostenSeite()`
(programmatisch, `Form_Start.Designer.cs`/`.resx` unberührt), die zwei Altknöpfe entfallen.

* **Übersicht:** Stamm-Combo + „nur Stammprojekte" + Liste (Art/Bezeichner/Projektname/
  Simulationsstand) + Anlegen/Löschen/Simulieren; darunter Komponentenraster —
  Stammklick = alle belegten Merkmale, Variantenklick = `AbweichungsErmittler.Vergleiche`.
* **Kosten:** Kachelwerte Investition/Betrieb/Energie + Komponenten- und Energieträgerliste,
  Absprung „Kostenverwaltung öffnen…".
* **Wirtschaftlichkeit / Bericht:** eingebettet, voller Funktionsumfang; „Projektvergleich +
  Bericht (alt)" sitzt auf der Berichtsseite und nimmt Stamm + alle angehakten Varianten.

Nebenbefund: `btn_Kosten_Click` war im Bestand **nie verdrahtet**.
Verifikation: 104/104 Headless-Zusicherungen, Geometrie bei 1265×560 und 1000×470 ohne Überstand.

## Umsetzung R4b — Übernahme

* `Controller\MerkmalUebernahmeCtrl.cs` — feldweise Übernahme. Geschrieben wird **genau die
  Zeile, aus der die Anzeige ihren Zielwert las** (`WHERE ID = ? AND ID_Projekt = ?`, eine Spalte).
* `Controller\KomponentenUebernahmeCtrl.cs` — ganze Komponentenkette je Gewerk in **einer
  Transaktion**: Pufferverweise lösen → Anlagen-/Kind-/Gerätezeilen löschen → Gerätezeilen der
  Quelle als Projektkopie (`MAX(ID)+1`, nie Quell-IDs) → Kindtabellen → Pufferverweise
  wiederherstellen → Anlagenzeilen via `WizardCtrl.SQL_ANLAGE_INSERT`. Stromspeicher: Variantenzeile
  nach dem Commit (AutoWert), Aktiv-Invariante über `SetzeAktiv`.
* `Views\BerichteKosten\Form_BkUebernahme.cs` — ein Dialog für beide Fälle, Quellenwahl
  (Stamm oder jede andere Variante der Gruppe) mit live nachgeladenen Werten, Klartext-Vorschau
  (`+ anlegen / ~ ersetzen / − entfernen`). Nach dem Schreiben wird `Tab_Projekt.Aenderungsdatum`
  gesetzt → der vorhandene Status meldet die Ergebnisse als veraltet.

Gewerke: WP (+`Tab_Kenndaten`, `Tab_Kenndaten_Kuehlung`), BHKW, Spitzenkessel, Solarthermie,
Photovoltaik, Pufferspeicher (+`Z_ProjektPufferSp`, `Z_AnlagePufferVerbund`), Stromspeicher
(+`Tab_StromspeicherVariante`).

## Wichtige Befunde

1. **Pairing ist positionell.** `AbweichungsErmittler.ZeileFuer` nimmt durchgängig „erste Zeile
   nach ID". Wo eine Gerätetabelle auf einer Seite mehr als eine Zeile führt, **lehnt die
   Übernahme ab statt zu raten**; `Bezeichner` ist als Schlüsselspalte generell gesperrt.
2. **Verwaiste Gerätekopie in Variante 1027 („Andere WP"):** `Tab_WP` führt dort die Projektkopie
   `1672018 CS6800iAW` ohne Anlagenbezug neben der real verbauten `1672019 WPE-I 59 H 400`.
   `Rows[0]` ist die Waise → die **Diff-Anzeige zeigt für WP scheinbar Gleichstand mit dem Stamm**.
   Die Übernahme ist dagegen abgesichert; die Anzeige bleibt irreführend → Aufräumpaket offen.
3. **Puffer-RI ohne CASCADE** (empirisch): `Tab_Pufferspeicher` lässt sich nicht löschen, solange
   irgendeine Anlagenzeile ihn über `WS_ID_Puffer(2)`/`WQ_ID_Puffer` führt — auch fremde Gewerke.
   Deshalb Sicherung der Verweise als Bezeichner und Wiederherstellung nach dem Neuanlegen.
   Beim Gewerk Pufferspeicher ist der Geräte-FK dieselbe Spalte wie ein Verweis → Reihenfolge:
   Verweise abbilden, Geräte-FK zuletzt. (Fehler im Trockentest gefunden und behoben.)

## Verifikation

337/337 Engine-Tests grün · Prüfbuild Full-MSBuild VS 2022 x86: 0 Fehler, 6 Baseline-Warnungen ·
R4a 104/104 + R4b 128/128 Headless-/DB-Zusicherungen · Produktiv-DB unverändert
(SHA-256 vor = nach) · 114 neue `BK_*`-Schlüssel in de **und** en · Encodings wie vorgefunden.

## Offen

* Aufräumweg für verwaiste Gerätekopien (Befund 2) — Anzeige bleibt sonst irreführend.
* BHKW-Kette, `Tab_Kenndaten_Kuehlung` und die beiden `Z_*Puffer*`-Tabellen sind umgesetzt, aber
  mangels Daten nicht real durchlaufen.
* Auswahl **einer bestimmten** von mehreren gleichartigen Komponenten: kein stabiler
  Komponentenschlüssel im Datenmodell — vom PDF nicht gefordert, wäre ein eigenes Paket.
* Retro-Lokalisierung der gehobenen Bestandsseiten (Wirtschaftlichkeit/Bericht, ~80 Texte).
* Sichtprobe am laufenden Programm steht aus.

# BK-2b — Gruppe, Klasse und Kennzeichen in der Oberfläche

Welle BK-2b nach Entscheid E132 (Entwurf
[Bauteilkatalog](../../../aktuell/Gebaeudesimulation/2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md), Abschnitte 2.2
und 6a; offene Punkte aus [BK-2](2026-10-12_BK-2_Schema221.md)). Ohne Schemaschritt, Rechenweg unberührt, keine neue
SQL-Anweisung außer der erweiterten Spaltenliste der Baustoff-Katalogzeilen. Ausgangsstand `a20bbf1eb`.

## Ergebnis

1. **Kern** (`Bauteilkatalog`): `Gruppen`, `Klassen(gruppe)`, `GruppeText`, `KlasseText` (Anzeige in der Sprache der
   Oberfläche, Ressourcen `BST_GRUPPE_*`, `BKAT_BST_KLASSE_*`, `BKAT_BTA_GRUPPE_*`, `BKAT_BTA_KLASSE_*`) und
   `UngeeignetGrund` — derselbe Regelweg wie `MeistUngeeignet` (erste Schicht ohne λ mit Nummer, Aufbau ohne Schicht,
   Öffnung ohne Schicht und ohne U der Quelle); `MeistUngeeignet` ruft ihn.
2. **Katalogprofile:** Baustoffe mit `KLASSE` (bei Platz) und `HERKUNFT` (breit); Aufbauten mit `EIGNUNG` (bei Platz, vorn),
   `GRUPPE` (breit), `KLASSE` (bei Platz). Die Katalogzeilen der Controller tragen Gruppe und Klasse als Anzeigetext,
   die Herkunft (Vorgabe, eigen, Eingelesen) und beim Aufbau die Marke „meist ungeeignet“ aus dem gespeicherten
   Kennzeichen. Die Filter nach Gruppe, Klasse und Herkunft sind die Trichter dieser Spalten (kein zweiter Filterweg).
3. **Baustoffkatalog:** Klasse als zweite Auswahl unter der Gruppe, ihre Einträge folgen der Gruppe, ein Gruppenwechsel
   nimmt eine fremde Klasse weg; ein eingelesener Satz trägt im Stammblattkopf die Marke, die Quelle steht als Lesewert
   (Feld, Herleitungszeile und Gruppe „Herkunft“).
4. **Bauteilaufbau-Katalog:** Gruppe und Klasse ebenso (ohne Gruppe setzt der Kern beim Speichern die Gliederung der
   Bauteilart); Lesewert „Gebäudesimulation“: „geeignet“ oder „für die Gebäudesimulation meist nicht geeignet – Grund“,
   live aus dem Summenfuß der Hülle (`AufbauKennwerteDaten.UngeeignetGrund`), nie von Hand setzbar; Werkzeugschalter
   „nur geeignete“ blendet die Zeilen mit Marke vor der Liste aus (einmal je Katalogladung gefiltert), Vorgabe an bei
   jeder Öffnung. Bewusst kein Trichter: eine Spalte mit gesetztem Filter weicht nie und schob die Liste bei 400 px um
   34 px quer (katalogprobe BK2b, erste Fassung).
5. **Bauteildialog:** gekennzeichnete Aufbauten (Projekt und Katalog) stehen erst nach dem Schalter „auch ungeeignete
   zeigen“ in der Wahl (Vorgabe aus; die laufende Wahl bleibt immer sichtbar); der Schalter steht nur, wenn eine Wahl
   einen gekennzeichneten Aufbau führt. Die Baustoffwahl an der Schicht und die Aufbauwahl gruppieren heute nicht —
   keine Gliederung nachgezogen.
6. **Befund und Behebung:** Beide Hüllen bauten das Modell beim Speichern ohne die Felder der Quelle — ein Speichern im
   Editor schrieb `Zusatz` (Kopf und Schichten) eines eingelesenen Satzes leer. Jetzt reisen Gruppe, Klasse und die
   Felder der Quelle (Kopf, Schichten gleicher Id) vom gespeicherten Satz mit, die Quelle eines Importsatzes bleibt die
   gespeicherte.
7. **Befund des Anwenders unter Windows (8 427 eingelesene Aufbauten):** Beim Öffnen des Projektgebäudes rechnete der
   Zonenweg (`GebaeudeKatalogHuelle.Wahl`) für jeden Katalogaufbau die Kennwerte; `Bauteilreduktion.SchichtPruefen` warf
   für jede Schicht mit ρ, c_p oder λ außerhalb der Bänder von `GebaeudeFestwerte` eine `GebaeudeModellException`, die
   `Wahl` abfing — langsam, tausende Ausnahmen (im Debugger hielt jede an). Behebung: `Bauteilreduktion.SchichtBefund` und
   `PruefenOhneAusnahme` (dieselben Bänder und Meldungen; `Pruefen` wirft unverändert über sie), `BauteilaufbauCtrl.Kennwerte`
   prüft ohne Ausnahme und nennt den Befund als Grund. Die Kennzeichenregel prüft zusätzlich die Bänder: ein angegebener
   Wert (Dicke, λ, ρ, c_p) außerhalb kennzeichnet den Aufbau, Grund ist die Meldung des Rechenwegs; ein fehlender ρ/c_p
   kennzeichnet nicht (der Summenfuß nennt ihn). Der Import übergibt dafür Dicke, ρ und c_p an die Regel; ein Wiederimport
   (Aktion „aktualisieren“ per Quellkennung) setzt das Kennzeichen neu. Die Bauteilwahl (`GebaeudeKatalogHuelle.Wahlen`)
   rechnet nur ungekennzeichnete Aufbauten; gekennzeichnete (gespeichertes Kennzeichen oder Kernregel) tragen das U der
   Quelle oder leer mit Grund (`AufbauWahl.Grund`, im Bauteildialog als Herleitungszeile) und erscheinen erst mit „auch
   ungeeignete zeigen“. Die SQL-Fassung der Regel im Schemaschritt 221 (`KennzeichenSetzen`) bleibt die engere; ein
   Bestandsaufbau mit Bandverletzung bekommt das Kennzeichen beim nächsten Speichern oder Wiederimport, die Wahl prüft die
   Regel ohnehin selbst.
8. **Texte, KI, Wachen:** 86 Ressourcenschlüssel je Sprache, `Resource.Designer.cs` neu geschrieben; KI-Feldkarte:
   Baustoff `klasse`, Aufbau `gruppe` und `klasse` (Wahl, Sichtklassen nachgezogen); Eingabebilanz Baustoff 8 → 9,
   Aufbau 4 → 7, Bauteildialog 15 → 16 (die beiden Schalter filtern nur, Vermerk).

## Dateien

- Kern: `Allgemein/Katalog/Bauteilkatalog.cs`, `Allgemein/Katalog/Katalogfilterprofil.cs`, `Controller/BaustoffCtrl.cs`,
  `Controller/BauteilaufbauCtrl.cs`, `Allgemein/KI/Dialoge/KiDialoge.cs`, `KiDialogTexte.cs`,
  `MyResource/Resource(.en-US).resx`, `Resource.Designer.cs`
- Hüllen: `EPOS.UI.Daten/Bedarf/BaustoffKatalogHuelle.cs`, `BauteilaufbauHuelle.cs`, `GebaeudeKatalogHuelle.cs`
- Oberfläche: `EPOS.UI/Dialoge/Bedarf/BaustoffKatalogDialog.razor`, `BaustoffKatalogDaten.cs`, `BaustoffKatalogTexte.cs`,
  `BaustoffKatalogKiSicht.cs`, `BauteilaufbauDialog.razor`, `BauteilaufbauDaten.cs`, `BauteilaufbauArbeitsstand.cs`,
  `BauteilaufbauTexte.cs`, `BauteilaufbauKiSicht.cs`, `BauteilDialog.razor`, `GebaeudeZonenDaten.cs`,
  `GebaeudeZonenTexte.cs`
- Rechenweg ohne Ausnahme: `EPOS.Kern/Allgemein/Simulation/Gebaeude/Bauteilreduktion.cs`,
  `Allgemein/Import/Bauteilkatalog/BauteilkatalogImport.cs`
- Tests: `EPOS.Kern.Tests/BauteilkatalogOberflaecheTests.cs` (neu, 9 Fälle, darunter 8 000 Aufbauten durch die Bauteilwahl
  ohne Ausnahme — Zähler über `FirstChanceException` — unter einer Sekunde), `BauteilaufbauVerwaltungTests.cs`
  (Spaltenliste), `EPOS.UI.Tests/Dialoge/BaustoffKatalogDialogTests.cs` (+4), `BauteilaufbauDialogTests.cs` (+4),
  `BauteilDialogTests.cs` (+3), `Hilfe/KiMaskenabdeckungWacheTests.cs` (Bilanz)
- Probe: `Proben/Rasterprobe/Wirt/Seiten/Katalogprobe.razor` (Masken `baustoff`, `bauteilaufbau`),
  `Proben/Rasterprobe/katalogprobe.mjs` (Fälle BK1a/b, BK2a/b mit 10 000 bzw. 8 500 Zeilen)

## Nachweis

ZAHLEN

## Punkte der Windows-Sichtabnahme

1. Administration › Gebäude › Baustoffe: Spalten Gruppe, Klasse (bei Platz), Herkunft (breit); Trichter auf Klasse und
   Herkunft („Eingelesen“) filtern; im schmalen Fenster weichen Klasse und Herkunft vor λ und c_p.
2. Stammblatt eines eigenen Stoffs: Klasse als zweite Klappliste unter der Gruppe; Gruppe „Holz…“ → Klassen Vollholz,
   Holzwerkstoff; Gruppenwechsel leert die Klasse; ohne feste Gruppe ist die Klasse gesperrt.
3. Eingelesener Stoff (nach „Bauteilkatalog einlesen…“): Kopfzeile mit „Eingelesen“, Quelle grau und nicht editierbar,
   Herleitungszeile darunter, Quelle in der Gruppe „Herkunft“.
4. Bauteilaufbauten: Schalter „nur geeignete“ steht an; nach Ausschalten erscheinen die gekennzeichneten Aufbauten mit
   der Marke „meist ungeeignet“ in der ersten Spalte nach dem Namen; „Filter zurücksetzen“ nimmt den Schalter mit.
5. Stammblatt eines Aufbaus: Gruppe und Klasse, Lesewert „Gebäudesimulation“ mit Grund; eine Schicht ohne λ setzen →
   der Lesewert wechselt ohne Speichern auf „… – Schicht n ohne Wärmeleitfähigkeit“.
6. Gebäudeeditor › Bauteil: gekennzeichnete Aufbauten fehlen in beiden Wahlen, bis „auch ungeeignete zeigen“ gesetzt
   ist; ein schon gewählter gekennzeichneter Aufbau bleibt sichtbar.
7. Rollen und Filtern in beiden Listen mit dem eingelesenen Katalog (rund 10 000 / 8 500 Sätze): flüssig, kein Blinken.
8. Dasselbe in Englisch (Gruppen, Klassen, Schalter, Lesewert).

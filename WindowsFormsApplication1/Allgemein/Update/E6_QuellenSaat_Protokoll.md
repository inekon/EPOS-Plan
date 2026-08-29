# Etappe E6 — Quellen-Saat UBA/GEMIS (Migrationsschritt 58): Umsetzungsprotokoll

Stand: 29.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md`](../../../Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md)
**§ 5.2 „Saatvorlage E6"** (Regeln 1–6, Tabelle A/B, Fußnoten ¹–³, Auslassungsliste).
Build x64 Debug: 0 Fehler. Prüfstand: Wegwerf-Kopie der produktiven Datenbank,
`SchemaVersion` 57 → **58**.

## 1. Soll

§ 5.2 verlangt die beiden am 29.08.2026 gelieferten Quellen als **Vorlagen** im
Emissionswerte-Katalog:

- **UBA-Liste „Emissionsfaktoren zur THG-Bilanzierung" v2.1 (2024)**, Blatt
  `01_Stationäre_Verbrennung` — CO₂, CH₄ und N₂O als **Einzelgase**, Feuerung ohne
  Vorkette, heizwertbezogen (Tabelle A, 10 Quellzeilen).
- **GEMIS 5.2 (IINAS)**, Blatt `Wärme-end 2020` bzw. `Strom-lokal DE 2000-2024` —
  SO₂ (Spalte C, **nicht** das SO₂-Äquivalent), NOx, Staub, ausnahmslos inklusive
  Vorkette (Tabelle B, 9 Quellzeilen).

Erwartete Wirkung nach § 5.2 (**Zählung dort am 29.08.2026 von 81 auf 85 berichtigt** —
die alte Zahl unterschlug die Biogas-Fächerung aus Fußnote ²): **85 neue Vorlagenzeilen**
— UBA 40 (8 × CO₂, je 16 × CH₄ und N₂O) und GEMIS 45 (15 Trägerzuordnungen ×
SO₂/NOx/Staub) —, **0 geänderte aktive Werte**, **Zweitlauf 0**.

Jede Zahl dieses Schrittes stammt aus den gerundeten Saatwerten der Tabellen A und B; die
Rundung selbst (Regel 5) ist im Konzept vollzogen und wurde im Code nicht wiederholt.

## 2. Umsetzung

Alles in **`Allgemein\Update\SchemaMigration.cs`** (einzige berührte Quelldatei;
UTF-8 mit BOM, unverändert — kein Kodierungs-Rundweg nötig). Reihenfolge nach dem
Vorfall vom 29.08. 09:25 eingehalten: erst Konstante, Methode und `SCHRITTE`-Eintrag,
**dann** `ZIEL_VERSION`.

Zeilenanker am **Endstand** gemessen (nach dem Guard-Fix aus Abschnitt 3d):

| Baustein | Zeile(n) | Inhalt |
|---|---|---|
| **Schrittkonstante** | `2078` | `SCHRITT_58_QUELLEN_SAAT = 58` samt Doc-Kommentar: Anlass, Teile 58a/58b, Systemgrenzen, CH₄-Zuordnung, Idempotenzschlüssel, erwartete Wirkung |
| **`SCHRITTE`-Eintrag** | `3157` | Beschreibung und Fehlertext im Stil der Schritte 56/57, Methodenzeiger `Schritt_58_QuellenSaat` |
| **Abschnittskopf** | `8353` | `// Schritt 58 - Quellen-Saat UBA/GEMIS (Etappe E6, Konzept § 5.2)` |
| **Trägergruppen** | `8364`–`8373` | `TRAEGER_ERDGAS_OHNE_STADTGAS` (Stadtgas fehlt in beiden Quellen), `TRAEGER_SCHEITHOLZ`, `TRAEGER_KOKS`; alles Übrige aus den vorhandenen Gruppen des Schritts 57 |
| **Zeitbezüge** | `8376`–`8382` | `E6_STAND_UBA` 01.01.2024, `E6_STAND_GEMIS_WAERME` 01.01.2020, `E6_STAND_GEMIS_STROM` 01.01.2024 (Regel 6) |
| **Saattabelle A** | `8416`–`8451` | `UBA_SAAT` — 10 Quellzeilen, je Zeile Zeilennummer und Kennung der Arbeitsmappe als Codekommentar (Prüfweg zurück in die Quelle) |
| **Saattabelle B** | `8481`–`8511` | `GEMIS_SAAT` — 9 Quellzeilen, Spalte A wörtlich als Betreff |
| **Schrittmethode** | `8518`–`8612` | `Schritt_58_QuellenSaat`: Vorbedingungen, Bestandsaufnahme, 58a/58b, Schreiben, Gegenprobe samt Wirkungs-Guard, Protokollzeile |
| **Hilfsmittel** | `8622`–`8858` | `QuellSchluessel`, `VorlagenSchluesselLesen`, `UbaSammeln`, `GemisSammeln`, `UbaZeile`, `GemisZeile`, `QuellenZeilenSchreiben` |
| **`ZIEL_VERSION`** | `97` | 57 → **58**, als **letzte** Code-Änderung; Doc-Kommentar `88`–`95` von „E6 verschoben / 58 reserviert" auf „E6 registriert als Schritt 58, neue Schritte ab 59" gebracht |

**Feldwerte jeder gesäten Zeile** (Regel 1 und 6): `ist_aktiv = falsch`,
`ist_auslieferung = wahr`, `ist_co2e = falsch`, `herkunft_id` NULL, `quelle` =
`UBA_2024` bzw. `GEMIS_52`, `quelle_text` aus den vorhandenen `DbWerte`-Konstanten
`EMISSIONSWERT_TEXT_UBA_2024` / `_GEMIS_52_WAERME` / `_GEMIS_52_STROM`, `gueltig_ab` =
Zeitbezug der Quelle. Die fünf Quell- und Textkonstanten in `DbWerte.cs:2389–2444` wurden
**verwendet, nicht dupliziert**; `DbWerte.cs` blieb unverändert.

**CH₄-Zuordnung** (Regel 4): fossile Träger → `CH4_FOSSIL` (Erdgas, Heizöl, Steinkohle,
Braunkohlebrikett), biogene → `CH4_BIOGEN` (Scheitholz, Pellets, die drei Biogas-Träger,
Biomethan, Deponiegas, Klärgas).

**Trägerauflösung** nach dem Muster der Schritte 56/57: Lesen über `energy_carrier.[name]`,
ein nicht auffindbarer Träger ergibt eine Protokollnotiz und wird gezählt — **kein
Abbruch** (der Katalog einer fremden Datenbank darf ärmer sein als die Solltabelle).

**Idempotenzschlüssel:** (`quelle`, `emissionsart_id`, `carrier_id` bzw. NULL,
`quelle_text`) über die **Vorlagen** — aktive Zeilen bleiben außen vor. Der **Wert** steht
bewusst nicht im Schlüssel (zu einer Quelle gehört je Art und Träger genau eine Vorlage);
der **Quellentext** dagegen schon: Er ist bei den drei trägerlosen UBA-Zeilen (Biomethan,
Deponiegas, Klärgas — alle `carrier_id = NULL`) die einzige Unterscheidung. Ohne ihn
stünden sie übereinander, und die Saat ergäbe 81 statt 85 Zeilen. Eigener Schreibweg
`QuellenZeilenSchreiben` statt `ZeilenSchreiben`: Jener erkennt eine Vorlage am
vollständigen Inhalt **einschließlich des Wertes**; der Bestand des Schritts 57 bleibt
davon unberührt.

**Kein DDL** — Tabellen, Indizes und Beziehung stehen seit Schritt 57. Fehlen
`emissionswert` oder die Pflichtart CO₂, bricht der Schritt hart ab.

## 3. Beweise (Prüfstand auf Arbeitskopie)

Produktiv-DB `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` **nur gelesen** — vor dem Kopieren
geprüft: keine `Kenndaten.laccdb` vorhanden, die Anwendung lief nicht. Datierte
Arbeitskopie (29.08.2026, 151 949 312 Byte) im Scratchpad-Ordner, sämtliche Läufe und
Dumps ausschließlich gegen die Kopie. Wegwerf-Harness: `dev\harness_e6\` (Repo-Wurzel,
gitignored) — Konsolenprojekt mit Projektverweis auf die Anwendung, biegt
`Properties.Settings.DBPath` per Reflection auf die Kopie um (Muster `harness_b4`), prüft
den Pfad gegen „ProgramData" und ruft `SchemaMigration.Ausfuehren`.

### 3a. Erstlauf — 85 neue Zeilen, 0 geänderte aktive Werte

Protokollzeile des Schrittes:

> Quellen-Saat (Schritt 58): **85 Vorlagen neu von 85 geplanten** (UBA-Liste v2.1 40,
> GEMIS 5.2 45), 0 bereits vorhanden, **0 Trägerzuordnung(en) im Katalog nicht
> vorhanden**; Gegenprobe ohne Abweichung, **0 aktive Zeilen dieser Quellen**.

`emissionswert` gesamt **220 → 305** (+85); davon Vorlagen 139 → 224, aktive Zeilen
81 → 81.

Aufschlüsselung je Quelle × Art (SELECT gegen die Kopie):

| Quelle | Art | Zeilen | Soll § 5.2 |
|---|---|---|---|
| `UBA_2024` | `CO2` | 8 | 8 |
| `UBA_2024` | `CH4_FOSSIL` | 8 | zusammen 16 |
| `UBA_2024` | `CH4_BIOGEN` | 8 | zusammen 16 |
| `UBA_2024` | `N2O` | 16 | 16 |
| `GEMIS_52` | `SO2` | 15 | 15 |
| `GEMIS_52` | `NOX` | 15 | 15 |
| `GEMIS_52` | `STAUB` | 15 | 15 |
| | **Summe** | **85** | **85** |

Feldwerte-Kontrolle über alle 85 Zeilen: genau **eine** Kombination —
`ist_aktiv = False`, `ist_auslieferung = True`, `ist_co2e = False`; **0 Zeilen mit
gesetztem `herkunft_id`**. `gueltig_ab`: UBA 40 × 01.01.2024, GEMIS 36 × 01.01.2020
(Wärmeblatt, 12 Träger × 3) und 9 × 01.01.2024 (Stromblatt, 3 Träger × 3).

**Vorher/Nachher-Vergleich (SHA-256 über Volldumps):**

| Dump | vorher | nachher | Befund |
|---|---|---|---|
| **alle aktiven `emissionswert`-Zeilen** (81 Zeilen, alle Spalten) | `EDBA0F09…B9E8` | `EDBA0F09…B9E8` | **IDENTISCH** |
| `energy_carrier` (id, name, **co2, so2, nox**) | `FED610F7…69AC` | `FED610F7…69AC` | **IDENTISCH** |
| `energy_project_settings` (ID, ID_Projekt, **co2, so2, nox**) | `FAD722B3…D83A2` | `FAD722B3…D83A2` | **IDENTISCH** |
| `emissionsart` (alle Spalten) | `B1BDA402…E43B3` | `B1BDA402…E43B3` | **IDENTISCH** |
| alle Vorlagenzeilen | `07961F1F…41FA` (139) | `C22EAAAE…B5F7` (224) | erwartet abweichend |
| `Tab_Applikation.SchemaVersion` | 57 | 58 | erwartet abweichend |

Zeilenweiser Volldump-Diff der Vorlagen: **keine einzige Zeile verschwunden oder
geändert**; die 85 hinzugekommenen Zeilen tragen ausnahmslos `quelle` `UBA_2024` oder
`GEMIS_52`.

### 3b. Zweitlauf — 0 Änderungen

Zwei Belege:

1. **Mit stehendem Marker** (`SchemaVersion` = 58): Schritt 58 meldet „bereits erledigt",
   die Datenbank wird nicht angefasst.
2. **Marker auf der Kopie auf 57 zurückgesetzt**, damit der Schritt tatsächlich erneut
   läuft — der eigentliche Idempotenz-Nachweis:

> Quellen-Saat (Schritt 58): **0 Vorlagen neu von 85 geplanten** (UBA-Liste v2.1 40,
> GEMIS 5.2 45), **85 bereits vorhanden**, 0 Trägerzuordnung(en) im Katalog nicht
> vorhanden; Gegenprobe ohne Abweichung, 0 aktive Zeilen dieser Quellen.

Alle sieben Dumps nach dem zweiten Lauf sind **SHA-256-gleich** mit den Dumps nach dem
ersten — einschließlich `SchemaVersion` (wieder 58).

### 3c. Stichproben, wertgenau gegen § 5.2

| Träger | Art | Einheit | Wert in der Kopie | Soll (Tabelle A/B) | `gueltig_ab` |
|---|---|---|---|---|---|
| Erdgas E | `CO2` | g/kWh | 202,396 | A: Erdgas (Heizwert) 202,396 | 2024-01-01 |
| Steinkohle | `CH4_FOSSIL` | mg/kWh | 482,17 | A: Steinkohle/Kohle 482,17 | 2024-01-01 |
| Biogas Variante | `N2O` | mg/kWh | 5,544 | A: Biogas 5,544 (Fußnote ², dritter Träger) | 2024-01-01 |
| Heizöl S | `SO2` | mg/kWh | 1858,195 | B: `Öl-schwer-Kessel-Industrie-100%` 1858,195 | 2020-01-01 |
| Koks | `NOX` | mg/kWh | 514,369 | B: `StK-Koks-Hzg 100%` 514,369 | 2020-01-01 |
| Strom Variante | `STAUB` | mg/kWh | 25,712 | B: `Stromnetz-lokal 2024` 25,712 | 2024-01-01 |
| Fernwärme | `SO2` | mg/kWh | 106,592 | B: `Fernwärme-mix (KWK: energiealloziert)` 106,592 | 2020-01-01 |

Die drei **trägerlosen** UBA-Zeilen stehen einzeln und sind am Anzeigetext zu
unterscheiden (`carrier_id` NULL):

| Art | Wert | `quelle_text` |
|---|---|---|
| `CH4_BIOGEN` | 978,066 | …Scope 1 ohne Vorkette, Hi — **Biomethan** |
| `CH4_BIOGEN` | 1124,208 | …— **Deponiegas** |
| `CH4_BIOGEN` | 1124,208 | …— **Klärgas** |
| `N2O` | 3,42 | …— **Biomethan** |
| `N2O` | 5,544 | …— **Deponiegas** |
| `N2O` | 5,544 | …— **Klärgas** |

### 3d. Wirkungs-Guard: Anwenderzeilen zählen nicht mit (Review-Befund)

Die Gegenprobe bricht ab, wenn sie eine **aktive** Zeile der Quellen `UBA_2024` /
`GEMIS_52` findet — der Beleg für Regel 1. Der Review zeigte den Randfall: Übernimmt ein
Anwender später eine E6-Vorlage in einen Träger, schreibt
`EmissionskatalogCtrl.Uebernehmen` (`Controller\EmissionskatalogCtrl.cs:392–417`) eine
aktive Zeile mit **derselben Quellkennung** — und zusätzlich der Vorlagen-ID in
`herkunft_id`. Ein späterer Wiederholungslauf mit zurückgesetztem Marker (Support-Fall)
hätte die Migration dann fälschlich abgebrochen.

**Fix:** Der Guard zählt nur noch aktive Zeilen dieser Quellen **ohne `herkunft_id`.**
Was dieser Schritt selbst anlegt, ist herkunftslos (`UbaZeile`/`GemisZeile` setzen
`HerkunftId = null`); eine übernommene Anwenderzeile trägt immer eine Herkunft und ist
bestimmungsgemäßer Gebrauch, kein Befund. Geändert: `VorlagenSchluesselLesen`
(Doc + `SELECT` um `herkunft_id` erweitert, Zählbedingung, Zeilen `8626`–`8666`), der
Kommentar an der Gegenprobe (`8571`–`8577`) und die beiden Meldungstexte.

**Beleg (a) — normaler Wiederholungslauf**, Marker auf der Kopie zurück auf 57:

> Quellen-Saat (Schritt 58): **0 Vorlagen neu von 85 geplanten**, 85 bereits vorhanden,
> … Gegenprobe ohne Abweichung, **0 aktive Zeilen dieser Quellen ohne Herkunft**.
> `ok = True`, Stand 57 → 58 — Guard still.

**Beleg (b) — Randfall simuliert.** Auf der Kopie wurde die UBA-Vorlage `id 222`
(CH₄ fossil an Erdgas E, 10,8 mg/kWh) als *übernommene* aktive Zeile dupliziert
(`ist_aktiv = True`, `herkunft_id = 222`, `ist_auslieferung = False`) — genau die Gestalt
des `INSERT`-Zweigs des Katalog-Dialogs; für dieses Paar (Art, Träger) gab es vorher
nachweislich **0** aktive Zeilen. Zählprobe danach:

| Kreis | Zeilen |
|---|---|
| aktiv **und** Quelle `UBA_2024`/`GEMIS_52` (= alter Guard) | **1** → hätte abgebrochen |
| davon **mit** `herkunft_id` | 1 |
| davon **ohne** `herkunft_id` (= neuer Guard) | **0** |

Lauf mit zurückgesetztem Marker: **kein Abbruch**, `ok = True`, „0 Vorlagen neu von 85
geplanten, 85 bereits vorhanden … 0 aktive Zeilen dieser Quellen ohne Herkunft",
Stand 57 → 58.

Die Simulationszeile wurde anschließend wieder entfernt. Alle sieben Dumps sind danach
**SHA-256-gleich** sowohl mit dem Stand unmittelbar vor der Simulation als auch mit dem
Endstand des Erstlaufs aus 3a — die Kopie steht wieder exakt auf dem gesäten Stand.

*Zur Lesart der Zitate in 3a und 3b:* Sie stammen aus den Läufen **vor** diesem Fix; die
Schlussformel lautet seither „0 aktive Zeilen dieser Quellen **ohne Herkunft**". An den
Zahlen ändert der Fix nichts — Beleg (a) ist die wörtliche Wiederholung des Laufs aus 3b
mit dem berichtigten Code.

### 3e. Endstand

`Tab_Applikation.SchemaVersion` der Arbeitskopie = **58**, Zielstand 58,
`SchemaMigration.Ausfuehren` liefert `true`, `StandVorher = 57`, `StandNachher = 58`.
Kein Schritt vor 58 hat auf der Kopie noch etwas zu tun gehabt (alle „bereits erledigt"),
sämtliche Abschlussprüfungen melden 0 Änderungen.

## 4. Bewusst nicht gesät

Unverändert nach der Auslassungsliste des Konzepts: UBA-Spalte `kg CO2e` (fremde
GWP-Basis), UBA `Erdgas (Brennwert)` (Ho statt Hi), UBA-Blatt `07` (zweite
Systemgrenze), UBA `Altholz/Holzreste` für Holzhackschnitzel, biogenes CO₂, GEMIS
CO₂/CO₂e/CH₄/N₂O, GEMIS `SO2-Äquivalent`, GEMIS Holz-Luftschadstoffe (anderer Nenner),
GEMIS `BrK-Brik-Lau-Hzg 100%` (rheinisch gesetzt), Wasserstoff · Stadtgas · Tierische
Fette (in beiden Quellen nicht vorhanden), Biogas-Luftschadstoffe. Auch die
Einzelraumfeuerungs-Zeile des Scheitholzes bleibt draußen (Fußnote ¹) — gesät ist die
Kessel-Zeile.

## 5. Offene Punkte

**Doku-Nachzug erledigt** (29.08.2026, eigener Vorgang): Beide `CLAUDE.md`-Schemastand-
Absätze sind auf Schritt 58 gebracht, die Konzept-Vermerke stehen auf Rev. 1.7, und beide
380er-Stellen zum Strommix-Vorgabewert sind auf 435 berichtigt — auch die im
Migrationsprotokoll des Schritts 56, die dieser Vorgang bewusst nicht angefasst hat.

| Nr. | Punkt | Stand |
|---|---|---|
| E6-1 | Sichtabnahme der Etappen **E3–E6** am laufenden Programm (Emissions-Tab, Katalog-Dialog mit den neuen Vorlagen, Modusschalter) | offen — nur am Bildschirm zu leisten, nicht auf dem Prüfstand |
| E6-2 | Durchsicht der Mapping-Liste in **§ 5.1** (offener Punkt 5 des Konzepts) | offen, unverändert — betrifft Schritt 57, nicht diese Etappe |
| E6-3 | § 8 Punkt 7: formlose Nutzungsbestätigung bei IINAS für die GEMIS-Ergebnisse | offen, unverändert — für die Auslieferung als Vorlagen laut Konzept ausreichend |
| E6-4 | Die aktiven Luftschadstoffwerte bleiben Feuerungswerte (`STAMM_ALT`, unbelegt); die belegten GEMIS-Zahlen liegen als **Angebot** daneben und liegen um Größenordnungen höher | gewollt (Nutzerentscheid 28.08.2026, § 8 Punkt 2) — wer die LCA-Sicht will, übernimmt sie sichtbar im Katalog-Dialog (E4) |
| E6-5 | Der Prüfstand belegt „0 geänderte aktive Werte" über Volldump-Hashes der drei Wertorte; ein zusätzlicher Referenzlauf-Vergleich der Emissionskennzahlen wurde nicht gefahren | vertretbar: E6 schreibt ausschließlich Vorlagenzeilen, und die Lesekette (`EmissionsFaktorLader`, § 3) liest auf Stufe 2 nur **aktive** Zeilen |
| E6-6 | Wegwerf-Harness `dev\harness_e6\` und die Arbeitskopie im Scratchpad | können nach Review gelöscht werden; `dev\` ist gitignored |

## 6. Grenzen dieses Pakets

Von diesem Vorgang berührt: **`Allgemein\Update\SchemaMigration.cs`**, dieses Protokoll
(neu) und der Wegwerf-Harness `dev\harness_e6\` (gitignored). Am Konzept wurde eine
einzige Stelle ergänzt: § 5.2 **Regel 6** um den Betreff-Zusatz der trägerlosen Zeilen und
seine Rolle im Idempotenzschlüssel.

Frisch gemessene Diff-Statistik der Quelldatei (`git diff --stat`, Endstand):

```
 WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs | 623 ++++++++++++++++-
 1 file changed, 610 insertions(+), 13 deletions(-)
```

Darin steckt eine **fremde** Änderung derselben Datei: Der parallele Vorgang zur
Strommix-Konstanten hat in den Schritt-56-Texten „380 (offene Entscheidung)" durch
„435 (mit E5 entschieden)" ersetzt — zwei Hunks, **+6/−3** (`@@ -1926,2 +1926,4 @@` und
`@@ -7353 +7440,2 @@`). Der Anteil dieser Etappe ist damit **+604/−10** in sechs Hunks:
Zeitform-Berichtigung `86` (1/1), `ZIEL_VERSION`-Doc `88–95` (8/8), `ZIEL_VERSION` `97`
(1/1), Schrittkonstante (+71), `SCHRITTE`-Eintrag (+14), Schritt-58-Block (+509). Die
zehn entfernten Zeilen sind ausschließlich der alte `ZIEL_VERSION`-Absatz samt Konstante
plus die Zeitform-Berichtigung darüber.

**Nicht** angefasst: `DbWerte.cs`, `SchemaKatalog.cs`, beide `CLAUDE.md`, die
Emissions-Controller und -Views. Nichts committet — Review durch die Hauptsession.

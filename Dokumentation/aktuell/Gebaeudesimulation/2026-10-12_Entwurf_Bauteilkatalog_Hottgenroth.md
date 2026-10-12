# Entwurf BK — Baustoff- und Bauteilkatalog vervollständigen, Importweg aus dem Hottgenroth-Katalog

**Stand 12.10.2026 · Befund und Entwurf, Fragen F1–F8 mit E132 entschieden (Abschnitt 6a), nichts gebaut.** Gelesen auf `23ca3798d` (Zweig `ios_migration_september`,
Basis R52). Auftrag BK der Sitzung Gebäudesimulation. Grundlage sind der Code, die Testdatenbank, ein Strukturbericht des
Hottgenroth-Gesamtkatalogs und eine Recherche offener Quellen. Beide Papiere liegen als Protokolle vor:
[Strukturbericht](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-12_BK_Strukturbericht_Hottgenroth.md) und
[Recherche freier Kataloge](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-12_BK_Recherche_freie_Kataloge.md). Der Strukturbericht enthält nur Struktur, Zählungen und Anteile. **Werte, Namen und Herstellerangaben
aus dem Katalog stehen weder hier noch an einer anderen Stelle des Repositoriums.**

**Anwenderwünsche (Wortlaut, 11./12.10.2026):**
- „Der Baustoffkatalog enthält nicht die Daten, die erforderlich sind für einen Baustoff (z. B. Wärmekapazität, …). Ebenso
  ist der Bauteileditor unvollständig. Suche nach einem umfassenden Bauteilkatalog. Nehme dazu die Datenbank
  HsetuAppTimData.mdf auf dem Google Drive und extrahiere die Bauteile und Baustoffe.“
- „Baustoffe [gehören ins] Administration-Menü im Katalog … Auch ein Bauteilkatalog soll im Administrationsmenü sein
  [Rubrik Gebäude]. Die Baustoffauswahl soll in Verbindung mit dem Bauteilaufbau verwendet werden.“ Diesen Wunsch setzt die
  Welle AB3 um: Baustoffe aus dem Import und Baustoffwahl an der Schicht. Beide Kataloge stehen schon unter
  Administration › Gebäude (`EPOS.UI/Bausteine/Menuetabelle.cs:135–137, 263`).

## 0 Das Ergebnis in Punkten

1. **Die Wärmekapazität ist im Baustoffkatalog vorhanden und pflegbar.** Alle 132 Auslieferungsbaustoffe tragen λ, ρ und
   c_p. Die Maske hat ein Feld für c_p, das Stammblatt eine Kennzahl c_p. In der Liste steht c_p aber nur „bei Platz“ und
   fällt im schmalen Fenster weg. Im Bauteildialog erscheint c_p gar nicht, weil keines der 79 Bauteile der Testdatenbank
   einen Aufbau trägt. Der Eindruck „keine Wärmekapazität“ kommt aus der Anzeige, nicht aus dem Datenmodell.
2. **Für den Bauteilweg fehlen im Datenmodell drei Dinge:** inhomogene Schichten (Abschnitte mit Flächenanteil), ein
   Fensteraufbau mit U_w, g und Rahmenanteil im Katalog und eine Herkunft „Import“ für Katalogsätze. Zur Pflege fehlen eine
   feste Stoffgruppe, ein Plausibilitätsband je Gruppe, der Quell-U-Wert zum Abgleich und die Anzeige von R_si/R_se.
3. **Der Hottgenroth-Katalog bringt bauphysikalisch nutzbar** rund 6 300 Baustoffe mit λ, ρ und c sowie 1 547 Aufbauten
   mit Schichten, davon 209 mit inhomogenen Abschnitten. Hinzu kommen Fensterkennwerte (U_g, g, Rahmenanteil) und
   408 Wärmebrücken mit ψ. Feuchte-, Akustik- und Ökobilanzfelder bleiben draußen.
4. **Empfohlener Weg: ein Austauschformat von EPOS mit einem plattformfreien Leser im Kern** (Weg ii). Dazu kommt ein
   Windows-Werkzeug unter `Werkzeuge/`, das die `.mdf` über LocalDB in dieses Format bringt (Weg i). Eine SQL-Server-Naht
   in der Windows-Schale (Weg iii) entfällt. Die Sätze landen nur in der Anwenderdatenbank, mit Herkunft `IMPORT`.
   Die Auslieferungsvorlage entfernt sie. Heute würde sie sie behalten, denn ihre Vorgabe ist „alle Katalogzeilen“.
5. **Ein frei lizenzierter Grundstock ist nicht belegt.** Es bleibt bei 132 Baustoffen und 9 Typaufbauten. Die
   Lizenzfragen an Hottgenroth, Fraunhofer IBP (MASEA) und BBSR (ÖKOBAUDAT) gehen an den Anwender.
6. **Rechenweg und Basis bleiben unberührt.** Kein Referenzprojekt nutzt einen Aufbau (0 von 79 Bauteilen mit
   `ID_Aufbau`), und die Testdatenbank bekommt keinen importierten Satz. Erst die Rechnung inhomogener Schichten nach
   DIN EN ISO 6946 ist eine Rechenwegänderung. Sie wirkt nur auf Projekte mit solchen Aufbauten, und solche gibt es in der
   Basis nicht.
7. **Wellen BK-1 bis BK-7, zusammen rund 11–15 PT.** BK-1 (Editoren ohne Schema) kann sofort beginnen. Der Import (BK-3/4)
   wartet auf F1 bis F4 und auf die Lizenzklärung.

## 1 Bestand

### 1.1 Was der Rechenweg je Schicht, Aufbau und Fenster braucht

| Ebene | Größe | Wo im Kern | Bemerkung |
|---|---|---|---|
| Schicht | Dicke d, λ, ρ, c_p, Luftschicht ja/nein | `Bauteilreduktion.cs:102` (`Schicht`) | c_p in J/(kg·K); eine Luftschicht ohne λ ist ruhend (Tabelle 8 der DIN EN ISO 6946), `:96–108` |
| Schicht | Bänder d 0,5 mm…1 m, λ 0,005…500, ρ 5…8 000, c_p 100…5 000 | `GebaeudeFestwerte.cs:488–509`, Prüfung `Bauteilreduktion.cs:327–368` | ein Wert außerhalb des Bands bricht den Bauteilweg mit `SchichtUngueltig` ab |
| Aufbau | Schichtfolge **raumseitig zuerst** | `Bauteilreduktion.cs:287, 450` | die Reihenfolge ist für die Kettenmatrix nicht vertauschbar |
| Aufbau | R_si, R_se | `Bauteilreduktion.cs:267` (`Uebergangswiderstaende`) | aus Neigung und Randbedingung nach Tabelle 7; **nicht** aus den Daten |
| Aufbau | U = 1/(R_si + Σ d/λ + R_se), C = Σ ρ·c_p·d | `Bauteilreduktion.cs:117–120, 292` | die U-Nachrechnung gibt es; der Katalog zeigt sie im Summenfuß |
| Bauteil | U-Wert oder Aufbau, Fläche, Neigung, Azimut, Rand, ψ·L | `BauteilEingang.cs:69–85`, `Tab_Bauteil` | U NaN heißt „aus den Schichten“; ohne Aufbau gilt der Klassenweg mit der Kapazität aus der Bauweise |
| Fenster | U_w, g, Rahmenanteil, Verschattung | `BauteilEingang.cs:232–244`, `GebaeudeFestwerte.cs:136` | am Bauteil; Rahmenanteil-Vorgabe 0,3; kein Schichtaufbau (`BauteilaufbauCtrl.cs:301–312`) |
| Oberfläche | Emissionsgrad außen, Absorptionsgrad opak | `GebaeudeFestwerte.cs:214, 217` | feste Werte für alle Bauteile, nicht je Baustoff |

### 1.2 Was EPOS heute trägt

- **`Tab_Baustoff(_STAMM)`:** `Bezeichner` (≤ 80), `Gruppe` (freier Text ≤ 40; die Auslieferung nutzt 11 Gruppen),
  `Hersteller`, `Lambda`, `Rho`, `cp`, `Quelle` (≤ 120), `Herkunft`, `Quellkennung` (≤ 64), `ReadOnly` und die Katalogspalten
  (`Katalog_Schluessel`, `_Pruefsumme`, `_Ausgelaufen`). Es gibt 132 Sätze, alle `VORGABE`, alle `ReadOnly`, alle mit
  λ, ρ und c_p (c_p von 380 bis 2 200 J/(kg·K)).
- **`Tab_Bauteilaufbau(_STAMM)`:** `Bezeichner`, `Beschreibung`, `Bauteilart` (neun Werte, auch `FENSTER`), `Typaufbau`
  (neun Typen), dieselben Herkunfts- und Katalogspalten. Es gibt 9 Sätze `VORGABE`: 5 Außenwände, 2 Dächer, 2 Bodenplatten.
- **`Tab_Bauteilschicht(_STAMM)`:** `Reihenfolge`, `ID_Baustoff` (nullbar), `Dicke`, `IstLuftschicht` (0/1) und eine Kopie
  von λ, ρ, c_p. Es gibt 31 Schichten. Eine inhomogene Schicht ist nicht abbildbar.
- **Herkunftswerte:** `CHECK (Herkunft IN ('MANUELL','KATALOG','IFC','GBXML','VORGABE','SQPROJ'))`
  (`EPOS.Kern/Allgemein/Update/ProjektdateiImportSchema.cs:17`, `Importherkunft.cs:49`). Einen Wert `IMPORT` gibt es nicht.
- **Fenster:** U, g, Rahmenanteil und Verschattung stehen nur am Bauteil (`Tab_Bauteil`). Der Aufbaukatalog führt für
  Fenster keine Kennwerte, es gibt also keinen Fensterkatalog.
- **Wärmebrücken:** nur `Tab_Bauteil.Psi_L` als Summe ψ·L in W/K, ohne Katalog.
- **Import:** `Baustoffabgleich` (Namenskette N1…N7), `Waermekapazitaetsrueckfall` (c aus Namensabgleich oder Stofftafel),
  `Ersatzaufbau` (Typaufbauten), `SchichtwerteNaht` (Übersetzung in den Bauteilweg). Der Leser der Projektdatei bildet
  `TcBuildingElementDimension(Layer)` schon ab (`Import/Sqproj/SqprojBauteile.cs:9, 35`): Schichtfolge innen → außen über
  `SortNum`, c in kJ/(kg·K), R_si/R_se als Widerstände. Der Gesamtkatalog trägt dieselben Tabellennamen.
- **Auslieferungsvorlage:** Ihre Vorgabe ist `--kataloge alle`, sie behält also jede Katalogzeile
  (`Werkzeuge/Auslieferungsvorlage/Argumente.cs:91–93`). Eine Regel nach Herkunft gibt es nur für die Tww-Kataloge
  (`TwwKataloge.cs:21, 133`: `FIKTIV`/`IMPORT` fliegen raus). Ein importierter Baustoff in der Datenbank, aus der die
  Vorlage gebaut wird, würde heute ausgeliefert.

### 1.3 Befund (a): Rechenweg, Hottgenroth-Felder und Lücken

1. **Der Rechenweg braucht je Schicht genau d, λ, ρ, c_p und den Luftschichtschalter** (`Bauteilreduktion.cs:102`). EPOS
   trägt alle fünf, am Baustoff und als Kopie an der Schicht. Der Hottgenroth-Katalog führt λ zu 84 %, ρ zu 77 %, c zu 76 %.
   6 308 Baustoffe haben alle drei Werte > 0. c steht gemischt in kJ/(kg·K) (6 192 Zeilen) und J/(kg·K) (193 Zeilen) und
   wird beim Import umgerechnet.
2. **Nützlich und in EPOS nicht vorhanden:**
   - **inhomogene Abschnitte** mit Flächenanteil (209 Aufbauten, 2–8 Abschnitte, Summe 100 %);
   - **Fensterkennwerte am Aufbau**: U_g zu 40 %, g zu 42 % am Flügel, Rahmenanteil in % an 1 280 Aufbauten, U_f nur an
     408. Der Rechenweg braucht nur U_w, g und den Rahmenanteil;
   - **Stoffgruppe als Code**: `MaterialGroupType` mit vermutlich acht Gruppen, zu 37 % belegt, über `TcBasis.GroupType`
     rund 4 800 Sätze nachtragbar;
   - **Bauteilart des Aufbaus** über `TcBasis.GroupType/SubType`;
   - **U-Wert der Quelle** (U nachgerechnet ≈ gepflegt, Median des Verhältnisses 1,00 über 1 101 Aufbauten), als
     Abgleichswert.
3. **Nützlich, aber schon von EPOS bestimmt:** R_si/R_se des Aufbaus. EPOS rechnet sie aus Neigung und Rand
   (`Bauteilreduktion.cs:267`). Der Quellwert dient nur dem U-Abgleich beim Import und wird nicht gespeichert.
4. **Nur bei Bedarf später:** Emissions- und Absorptionsgrad je Oberfläche (`MaterialSurfaceType`). Der Rechenweg
   nimmt dafür feste Werte (`GebaeudeFestwerte.cs:214, 217`); ein Wert je Außenschicht wäre eine Rechenwegänderung.
   Wärmebrücken ψ (408 Sätze) brauchen eine Länge je Bauteil; EPOS führt nur ψ·L. Der Bemessungswert λ ist nur zu 0,4 %
   belegt und bringt nichts.
5. **Draußen bleiben**, weil sie nur für Feuchte, Akustik oder Ökobilanz gelten: μ samt Grenzen, Feuchtegehalt,
   Feuchtezuschlag, Sättigung, Schalter dampfdicht, kapillar und wärmereflektierend, PCM, OI3, EPD und Haustechnik-Werkstoffe
   (`MaterialType` ≥ 13 ohne λ/ρ/c).

### 1.4 Befund (b): Was den Editoren zur Erwartung „vollständig“ fehlt

1. **c_p ist pflegbar, aber schlecht sichtbar.** Feld in der Maske (`EPOS.UI/Dialoge/Bedarf/BaustoffKatalogDialog.razor:519`),
   Kennzahl im Stammblatt (`:565–567`), Bandprüfung (`EPOS.Kern/Controller/BaustoffCtrl.cs:217–219`). In der Liste hat
   c_p den Rang `BeiPlatz` (`EPOS.Kern/Allgemein/Katalog/Katalogfilterprofil.cs:1222`) und verschwindet im schmalen
   Fenster. Der Bauteildialog (`BauteilDialog.razor:55–95`) zeigt nur U, g, Rahmenanteil und ψ·L, keine Masse. Ohne Aufbau
   sieht der Anwender dort nie eine Wärmekapazität.
2. **Baustoff: Die Gruppe ist ein freies Textfeld** (`BaustoffKatalogDialog.razor:510`). Damit gibt es keine feste
   Stoffgruppe, kein Plausibilitätsband je Gruppe (nur die globalen Bänder in `BaustoffCtrl.cs:217`) und keine
   Abbildung fremder Gruppencodes. Je Baustoff gibt es nur eine `Quelle` (`:521`), keine Herkunft je Wert.
3. **Aufbau: Schichtfolge, Baustoffwahl, R, U, C und T_BT sind da** (`BauteilschichtenFelder.razor:1–17, 39, 204–209`;
   Schichten „nach innen/außen“ verschiebbar). Es fehlen die Anzeige von R_si/R_se (sie folgen fest aus der Bauteilart
   an Außenluft, `BauteilaufbauCtrl.cs:179–181`), eine inhomogene Schicht und ein Quell-U zum Vergleich.
4. **Fenster haben im Katalog keinen Inhalt.** Die Bauteilart `FENSTER` ist wählbar, ein Schichtaufbau ist zu Recht
   gesperrt (`BauteilaufbauCtrl.cs:312`). Felder für U_w, g und Rahmenanteil fehlen aber, also auch die Übernahme an das
   Bauteil.
5. **Mengengerüst:** Die Baustoffwahl der Schicht lädt alle Einträge in eine `Suchauswahl`, die höchstens 50 Vorschläge
   zeichnet (`EPOS.UI/Standards/Suchauswahl.razor:142`). Mit 132 Einträgen ist das unkritisch, mit über 6 000 muss es
   gemessen werden. Die Katalogliste ist virtualisiert; das Maß dafür ist die Rasterprobe.

## 2 Zielbild

### 2.1 Datenmodell (nur, was Rechenweg oder Pflege braucht)

| Tabelle | Neu | Zweck |
|---|---|---|
| `Tab_Baustoff(_STAMM)` | `Herkunft` um `IMPORT` erweitert | Importsatz erkennbar, Auslieferungsregel greift |
| `Tab_Baustoff(_STAMM)` | `Gruppe` bleibt Text, bekommt aber eine feste Liste (Auswahl mit den 11 Gruppen der Auslieferung) | Plausibilitätsband je Gruppe, Abbildung der Fremdcodes; kein Schema nötig |
| `Tab_Bauteilaufbau(_STAMM)` | `Herkunft` um `IMPORT` erweitert; `U_Quelle` REAL (nullbar) | Abgleich nachgerechnet gegen Quelle; Warnung ab 5 % |
| `Tab_Bauteilaufbau(_STAMM)` | Fensterfelder `U_Wert` (U_w), `g_Wert`, `Rahmenanteil` (0…1), nullbar; mit `CHECK` nur bei `FENSTER`/`VORHANGFASSADE` belegt | Fensterkatalog; der Bauteildialog übernimmt die drei Werte an das Bauteil |
| `Tab_Bauteilschicht(_STAMM)` | `Abschnitt` INTEGER (nullbar; NULL = homogen) und `Flaechenanteil` REAL (0…1) | inhomogene Schicht: mehrere Zeilen mit gleicher `Reihenfolge`, je eine je Abschnitt; Summe der Anteile 1 |

Nicht aufgenommen: μ, Feuchte, Emissionsgrad je Baustoff, Bemessungswert-Marke, Wärmebrückenkatalog (Fragen F5, F6).
Ein CHECK auf `Herkunft` lässt sich bei STRICT-Tabellen nur durch einen Tabellenneubau erweitern. Der Schritt baut
deshalb vier Tabellen neu und behält IDs und Fremdschlüssel. Er betrifft auch die Projektkopien `Tab_Baustoff` und
`Tab_Bauteilaufbau`.

### 2.2 Editoren (Administration › Gebäude)

**Baustoffkatalog**

1. Die Liste zeigt c_p immer (Rang wie λ); ρ bleibt „bei Platz“.
2. Die Gruppe ist eine Auswahl aus der festen Liste, „Sonstige“ steht für alles übrige. Die Bänder je Gruppe sind
   **Hinweise**, keine Sperren. Die harten Bänder des Kerns bleiben wie sie sind.
3. Herkunft und Quelle stehen im Lesemodus als eine Zeile, Importsätze tragen die Marke „Import“. Ein Filter
   „ohne Importsätze“ arbeitet wie „nur herstellerneutral“ über den Filterstand.

**Bauteilaufbau**

1. Die Schichtfolge läuft innen → außen. Das steht im Rasterkopf, mit „innen“ oben und „außen“ unten.
2. Baustoffwahl je Schicht wie heute; die Welle AB3 baut sie aus.
3. Eine inhomogene Schicht entsteht über „Abschnitt hinzufügen“ an einer Schicht. Das öffnet Unterzeilen mit
   Baustoff, λ, ρ, c_p und Flächenanteil, die Summe muss 100 % ergeben.
4. Der Summenfuß zeigt zusätzlich R_si und R_se der Bauteilart und, falls vorhanden, den Quell-U mit Abweichung in %.
5. Wer die Bauteilart `FENSTER` oder `VORHANGFASSADE` wählt, bekommt statt der Schichten den Fensterteil mit U_w, g und
   Rahmenanteil. U_g und U_f sind optional und nur informativ, weil U_w aus U_g/U_f ohne die Glasrandlänge nicht
   nachrechenbar ist.

**Bauteildialog:** Die Kapazität (C in kJ/(m²K)) erscheint neben dem U-Wert, wenn ein Aufbau gewählt ist; ohne Aufbau
steht „aus der Bauweise“.

### 2.3 Importweg „Bauteilkatalog aus Hottgenroth einlesen“

**Wege zur Wahl:**

| Weg | Beschreibung | Für | Gegen |
|---|---|---|---|
| (i) Windows-Werkzeug mit LocalDB | `Werkzeuge/HottgenrothKatalog` hängt eine **Kopie** der `.mdf` an LocalDB an, liest nur und schreibt das EPOS-Austauschformat | einmalige Extraktion, die Anwendung bleibt frei von SQL Server; ersetzt das lokale Exportskript wiederholbar | allein läuft es nur beim Entwickler bzw. auf Windows mit LocalDB |
| (ii) Austauschformat und Leser im Kern | Ordner mit `Baustoffe.csv`, `Aufbauten.csv`, `Schichten.csv` (UTF-8, `;`, Dezimalpunkt) plus `Kopf.json` (Quelle, Datenstand, Fassung); Leser plattformfrei im Kern, Dialog „Bauteilkatalog einlesen…“ mit Vorschau | Windows und iOS; prüfbar mit synthetischen Proben; dasselbe Format taugt für andere Quellen (WUFI-, baubook- und MASEA-Exporte des Anwenders) | braucht einen Schritt vorher, eben (i) oder das Anwenderskript |
| (iii) SqlClient-Naht in der Schale | `Microsoft.Data.SqlClient` in `WindowsFormsApplication1`, benannte Naht wie `Katalogwege` (`EPOS.UI.Daten/Katalogwege.cs:23`), iOS lehnt benannt ab | ein Klick für den Anwender | LocalDB-Abhängigkeit beim Kunden, neues Paket in der Schale, das Anhängen verändert die Datei, Lizenzlage je Kunde offen |

**Empfehlung: (ii) als Weg der Anwendung, (i) als Werkzeug davor.** Der Kern liest nur das eigene Format, die
SQL-Server-Seite bleibt beim Werkzeug. Weg (iii) kommt erst in Frage, wenn Kunden mit eigener Hottgenroth-Lizenz
selbst importieren sollen und die Lizenz das erlaubt (F1).

**Abbildungsregeln** (nach Strukturbericht 9, umgesetzt im Werkzeug; der Kern prüft noch einmal):

1. Der Platzhalter −987654321,99 und seine Variante −987654,32, Ausreißer > 10⁸ und die Leer-GUID `AAAAAAAA-…` heißen
   „kein Wert“.
2. c: Die Quelle führt c überwiegend in kJ/(kg·K), EPOS rechnet in J/(kg·K). Werte ≤ 10 stehen in kJ und werden mit
   1 000 multipliziert, Werte > 10 stehen schon in J und bleiben (Strukturbericht 9 normiert umgekehrt auf kJ). c = 0
   heißt „kein Wert“, die Schicht bekommt dann den `Waermekapazitaetsrueckfall`.
3. λ = 0 ist ungültig. λ > 10 gilt nur bei Metallen; bei allen anderen Stoffgruppen ist der Satz ungültig und wird mit
   Grund protokolliert.
4. Ein Baustoff wird **vollständig** übernommen, wenn λ, ρ und c im Kernband liegen. Ein unvollständiger Baustoff nur auf
   Wunsch (F3), dann mit der Marke „unvollständig“; der Bauteilweg lehnt ihn an der Schicht benannt ab.
5. Die Stoffgruppe kommt aus `MaterialGroupType`, sonst aus `TcBasis.GroupType`. Die Abbildungstabelle der acht
   Fremdgruppen auf die EPOS-Gruppen liegt im Kern, „0/gemischt“ und leer werden „Sonstige“.
6. Die Bauteilart kommt aus `TcBasis.GroupType/SubType`: Wände → `AUSSENWAND` bzw. `INNENWAND` nach Untertyp; Decken,
   Böden und erdberührte Bauteile → `DECKE` bzw. `BODENPLATTE`; Dächer → `DACH`; Fenster → `FENSTER`; Türen → `TUER`;
   „unklar“ (2 435) → `SONSTIGES`, nur auf Wunsch.
7. Schichten werden nach `SeqNum` je (Aufbau, Abschnitt) geordnet. Die 5 Gruppen mit Lücken werden neu durchnummeriert
   und protokolliert. Die **Richtung innen → außen** prüft das Werkzeug an einer Stichprobe: Liegt die Dämmlage hinter
   der tragenden Schicht und trifft U aus den Schichten mit den R_si/R_se der Quelle den Quell-U? Erst dann wird
   übernommen.
8. Als vollständige Aufbauten gelten nur die 1 547 Aufbauten mit Schichten. Reine U-Wert-Aufbauten ohne Schichten
   kommen nur auf Wunsch (F3), dann als Aufbau ohne Schicht mit `U_Quelle`, ohne Kapazität.
9. Schichten der Quelle sind Kopien ohne Verweis auf den Baustoff. `ID_Baustoff` wird nur gesetzt, wenn der Name einen
   importierten Baustoff derselben Gruppe trifft (Abgleich N1–N3), sonst bleibt er leer und die Werte stehen als Kopie
   an der Schicht. Die 28 % Schichten ohne λ werden über den Namensabgleich gefüllt oder machen den Aufbau
   „unvollständig“.
10. Fenster: Rahmenanteil in % wird zum Anteil 0…1. g und U_g kommen vom Flügel bzw. vom Aufbau, U_w nur, wenn es in der
    Quelle steht.
11. **Herkunft und Quelle:** `Herkunft = 'IMPORT'`, `Quelle = "Hottgenroth-Katalog, Datenstand JJJJ-MM"`, `Quellkennung`
    = GUID des Quellsatzes (36 Zeichen). Mit der Quellkennung erkennt ein zweiter Import einen Satz wieder und
    aktualisiert ihn, statt ihn zu verdoppeln. `ReadOnly = 0`, keine Katalogspalten (`Katalog_Schluessel` leer).
12. **Hersteller:** bleibt leer. Die Quelle nennt nur Katalogherausgeber, keine Produkthersteller (Strukturbericht 6).
    Der Name des Teilkatalogs geht in die `Quelle`. Weder Hersteller- noch Produktnamen kommen ins Wiki.

**Dubletten gegen den EPOS-Katalog:**

- Gleich sind zwei Sätze, wenn der Name nach N1/N2 aufbereitet übereinstimmt, die Gruppe gleich ist und λ ±2 %,
  ρ ±5 % und c ±10 % zutreffen.
- Ein Auslieferungssatz (`VORGABE`) wird nie überschrieben.
- Die Empfehlung ist „nebeneinander mit Marke“: Der Importsatz bleibt, die Vorschau nennt den Treffer, und der Anwender
  kann Dubletten abwählen (F4).
- Doppelte Sätze innerhalb der Quelle (gleiche Werte über mehrere Teilkataloge) werden auf einen zusammengeführt; die
  Quelle nennt dann alle Teilkataloge.

**Mengengerüst:**

| Umfang | Baustoffe | Aufbauten | Schichten |
|---|---|---|---|
| bis zu | 10 719 | 1 547 mit Schichten (8 427 gesamt) | 9 296 plus 538 Abschnitte |
| voraussichtlich gültig | ~6 300 | ~1 500 | — |

- Der Import läuft in einer Transaktion mit vorbereiteten Anweisungen; erwartet werden wenige Sekunden.
- Die Katalogliste ist virtualisiert. Vor Abnahme läuft die Rasterprobe mit einem synthetischen Katalog von 10 000
  Zeilen, ebenso eine Messung der `Suchauswahl` an der Schicht mit 10 000 Einträgen.

**Auslieferung:** Eine neue Regel der Auslieferungsvorlage löscht in `Tab_Baustoff(_STAMM)` und
`Tab_Bauteilaufbau(_STAMM)` jede Zeile mit `Herkunft = 'IMPORT'`, die Schichten gehen per Kaskade mit, und zwar
unabhängig von `--kataloge`, wie bei der Tww-Regel. Der Prüflauf bricht ab, wenn danach noch ein Importsatz steht oder
ein Bauteil der Beispielprojekte auf einen solchen Aufbau zeigt. Die Testdatenbank bekommt nie einen Importsatz; Tests
lesen ein synthetisches Austauschformat.

### 2.4 Ausgelieferter Grundstock

Nach der Recherche gibt es **keine belegt frei lizenzierte Quelle** für λ, ρ und c mit Schichtaufbauten deutscher Bauweisen.
ÖKOBAUDAT, MASEA und TABULA haben eine offene Lizenzlage; baubook und WUFI schließen eine Übernahme aus. Deshalb:

- Es bleibt bei den 132 Baustoffen und 9 Typaufbauten der Auslieferung, ihre Quellen sind belegt.
- Optional kommen **U-Wert-Vorgaben je Baualtersklasse** aus der amtlichen Bekanntmachung zur Datenaufnahme Wohngebäude
  (BAnz AT 04.12.2020 B1, amtliches Werk) bzw. dem Typgebäude-Modell Stein/Loga (CC BY 4.0, Lizenz am Record prüfen)
  dazu. Sie dienen nur als Aufbauten ohne Schichten für den Klassenweg und sind kein Stoffkatalog.
- Die Stoffwerte bleiben Anwenderimport. Lizenzfragen an die Rechteinhaber:
  - **Hottgenroth:** Darf ein Kunde mit Lizenz den Katalog in eine Drittsoftware übernehmen? Darf INEKON ein
    Extraktionswerkzeug ausliefern?
  - **Fraunhofer IBP (MASEA):** Nutzungsbedingungen und Einbettung in kommerzielle Software.
  - **BBSR (ÖKOBAUDAT):** formale Datenlizenz der technischen Zusatzangaben.

## 3 Abstimmung mit den laufenden Wellen

| Welle | Berührt | Folge |
|---|---|---|
| AB3 (Baustoffe aus dem Import, Baustoffwahl an der Schicht) | `BauteilschichtenFelder`, `BauteilaufbauArbeitsstand`, `Baustoffabgleich` | BK-1 und BK-5 fassen dasselbe Raster an, deshalb nach dem Merge von AB3; BK-3 nutzt den Abgleich von AB3 für die Dubletten |
| GD (Gebäudedialog, Mehrzonenübernahme) | `GebaeudeBauteilvorschlag` | keine Überschneidung; ein Importaufbau steht dem Vorschlag wie jeder Katalogaufbau zur Wahl |
| Projektdatei-Import (SQ) | `SqprojBauteile` | gleiche Tabellennamen; die Abbildungsregeln (Richtung, Einheiten, R_si/R_se) werden zwischen Projektdatei und Gesamtkatalog geteilt, nicht doppelt geschrieben |

## 4 Folgen

- **Kern:**
  - Schemaschritt (2.1);
  - Leser des Austauschformats mit Prüfung und Protokoll;
  - Abbildung der Stoffgruppen;
  - Dublettenprüfung (nutzt `Baustoffabgleich`);
  - Controller für den Import in einer Transaktion;
  - `BauteilaufbauCtrl` mit Fensterfeldern und `U_Quelle`;
  - bei BK-5 `Bauteilreduktion` mit R_T nach DIN EN ISO 6946 Abschnitt 6.7 (oberer und unterer Grenzwert).
- **`EPOS.UI.Daten` und Oberfläche:**
  - Hülle und Dialog „Bauteilkatalog einlesen…“ mit Ordnerwahl über die Dateischnittstelle, Vorschau mit Zählungen und
    Gründen, Dublettenwahl;
  - Editoren nach 2.2;
  - Texte in beiden Sprachen.
- **Werkzeuge:**
  - `Werkzeuge/HottgenrothKatalog` (Windows, LocalDB, nur lesen, immer auf einer Kopie);
  - Regel und Prüfung in `Werkzeuge/Auslieferungsvorlage`.
- **Proben:** Rasterprobe mit 10 000 Zeilen.
- **Wiki:** Seiten „Baustoffe“, „Bauteilaufbauten“ und „Bauteilkatalog einlesen“, ohne Hersteller- und Produktnamen;
  dazu ein Logbuch-Entwurf.

## 5 Rechenweg, Basis und Schema

### 5.1 Schema

Gebaut ist **Schemaschritt 221** (`EPOS.Kern/Allgemein/Update/BauteilkatalogSchema.cs`, Kernregeln in
`EPOS.Kern/Allgemein/Katalog/Bauteilkatalog.cs`; Spalten, Abbildung und Nachweis im Protokoll
[BK-2 Schema 221](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-12_BK-2_Schema221.md)):

- **Herkunft** an `Tab_Baustoff(_STAMM)` und `Tab_Bauteilaufbau(_STAMM)` mit `EIGEN` und `IMPORT`, `Quelle` bis 400
  Zeichen (Neubau der vier Tabellen, IDs und Fremdschlüssel bleiben); `Quellkennung` eindeutig je Herkunft `IMPORT`.
  Bestand: ausgeliefert `VORGABE`, eigene Sätze `EIGEN`, eine Importherkunft der Projektdatei bleibt.
- **Gliederung** zweistufig als Code-Spalten mit CHECK: `Gruppe_Code` und `Klasse_Code` an Baustoff und Aufbau
  (Gruppen der Baustoffe = die zwölf festen Stoffgruppen, der Aufbauten = Wände, Decken, Dächer, Öffnungen, Sonstige).
  Abgeleitet aus Stoffgruppe bzw. Bauteilart und Typaufbau; die Abbildungstafel der Fremdcodes liegt im Kern.
- **Kennzeichen** `Simulation_Ungeeignet` am Aufbau (0/1, vom Kern gesetzt): eine Schicht ohne λ oder ein Aufbau ohne
  Schicht (ein Fenster, eine Vorhangfassade oder eine Tür mit `U_Quelle` ausgenommen).
- **Felder der Quelle** (E132 F2), alle nullbar mit Bereichs-CHECK: am Baustoff 17 (Lang-/Infotext, Bemessungswert λ,
  µ-Grenzen, Baustoff- und Oberflächenart, drei Schalter, Feuchtegehalt, Höchsttemperatur, Rauheit, Längenausdehnung,
  OI3); am Aufbau 20 (Infotext, Dicke, `U_Quelle`, R, Rsi, Rse, hinterlüftet, die Fensterfelder Rahmenanteil, g, Ug,
  Uf, Rahmenbreite, Verschattungsfaktor, ψ gesamt/System/Geometrie, OI3 des Rahmens, Gültigkeit); an der Schicht 29
  samt `Quellkennung` und EN-6946-Geometrie.
- **Abschnitte** als eigene Tabelle `Tab_Bauteilabschnitt(_STAMM)` (Nummer, Flächenanteil 0…1, Text, Quellkennung) mit
  `ID_Abschnitt` an der Schicht; die Rechnung nach DIN EN ISO 6946 6.7 bleibt BK-5.
- Das Katalogregister führt Gliederung, Kennzeichen und Felder als Fachspalten; die Prüfsummen der Auslieferungssätze
  stempelt der Schritt neu. Die Auslieferungsvorlage behält Importsätze (F8) und zählt die Sätze je Herkunft.
- Der Bauteilweg liest keine neue Spalte; der Referenzlauf bleibt gleich.

### 5.2 Einfrierregeln

Keine ist berührt:

- Die Referenzprojekte haben kein Bauteil mit Aufbau (0 von 79 `ID_Aufbau`).
- Die gesäten Gebäude- und Zonendaten ändern sich nicht.
- Die Testdatenbank bekommt keinen Importsatz.

Berührt wäre eine Einfrierregel erst, wenn ein Referenzprojekt einen Aufbau nutzt oder ein Auslieferungsbaustoff geändert
wird; dann gilt die Regel „gesäte Zonendaten“ (Bauteile samt Aufbau).

### 5.3 Rechenweg

- BK-1 bis BK-4 und BK-6 sind reine Daten- und Bedienungsänderungen. Die Fensterwerte am Aufbau werden bei der Wahl an
  das Bauteil **kopiert** und laufen dann den bestehenden Fensterzweig.
- BK-5 (inhomogene Schicht) ist eine Rechenwegänderung des Bauteilwegs: R_T als Mittel aus oberem und unterem Grenzwert,
  die Kapazität flächengewichtet. Sie wirkt nur auf Aufbauten mit Abschnitten und lässt die Basis byte-gleich, was der
  Referenzlauf der CI-Auswahl nachweist.
- Ohne BK-5 werden solche Aufbauten beim Import mit dem Vermerk „inhomogen, nicht übernommen“ ausgelassen.

## 6 Fragen an den Anwender

1. **F1 Weg:** Weg (ii) mit dem Werkzeug (i), oder ein Klick in der Anwendung (iii)? **Empfehlung:** (ii) + (i). (iii) nur
   nach Lizenzklärung mit Hottgenroth und wenn Kunden selbst importieren sollen.
2. **F2 Zusätzliche Felder:** Herkunft `IMPORT`, `U_Quelle`, Fensterfelder am Aufbau, Abschnitte an der Schicht, und nicht
   mehr? **Empfehlung:** ja. μ, Feuchte, Emissionsgrad und Bemessungsmarke bleiben draußen.
3. **F3 Umfang:** nur vollständige Baustoffe (λ, ρ, c gültig) und nur Aufbauten mit Schichten, oder auch unvollständige
   Baustoffe und U-Wert-Aufbauten? **Empfehlung:** vollständige zuerst. U-Wert-Aufbauten ohne Schichten als Schalter in
   der Vorschau, Vorgabe aus.
4. **F4 Dubletten:** nebeneinander mit Marke, überspringen, oder mit dem EPOS-Satz zusammenführen? **Empfehlung:**
   nebeneinander mit Marke und Abwahl in der Vorschau; einen Auslieferungssatz nie überschreiben.
5. **F5 Fenster:** jetzt (BK-6) oder später? **Empfehlung:** jetzt, der Aufwand ist klein und der Fensterkatalog heute leer.
6. **F6 Wärmebrücken:** ψ-Katalog jetzt oder später? **Empfehlung:** später. Er braucht Längen je Bauteil, und EPOS führt
   bisher nur ψ·L als Summe.
7. **F7 Inhomogene Schichten:** mit Rechenweg nach DIN EN ISO 6946 (BK-5), oder solche Aufbauten vorerst auslassen?
   **Empfehlung:** BK-5 bauen. 209 Aufbauten, darunter typische Holzbauwände, wären sonst nicht nutzbar.
8. **F8 Lizenz:** Fragt der Anwender bei Hottgenroth, MASEA und ÖKOBAUDAT an (Fragen in 2.4)? **Empfehlung:** ja, vor
   BK-3. Bis zur Antwort nur im eigenen Hause und ausschließlich in der Anwenderdatenbank.

## 6a Entscheide (E132, Anwender 12.10.2026)

Wortlaut: „F3 – Umfang des Imports: Kennzeichne Bauteile ohne Wärmeleitfähigkeit (Auswahl), da meistens nicht geeignet für
Gebäudesimulation. Erstelle Gruppen wie und Klassen für die Bauteile. Baustoffe nicht kennzeichnen nach Wärmeleitfähigkeit,
auch in Gruppen und Klassen einteilen. F4 – Dubletten: Zusammenführen – keine Doubletten. F2 – Zusätzliche Felder im
Katalog: alle Felder aufnehmen. Alle anderen F1, F5–F7: Empfehlung. F8: nehme die Datensätze auf. Lizenzen geklärt.“

| Frage | Entscheid | Folge für Entwurf und Wellen |
|---|---|---|
| F1 Weg | Empfehlung: Austauschformat mit Leser im Kern (ii), Windows-Werkzeug über LocalDB davor (i) | wie 2.3; BK-3 und BK-4 |
| F2 Felder | **alle Felder der Quelle aufnehmen**, nicht nur Herkunft, `U_Quelle`, Fenster und Abschnitte | Schemaschritt von BK-2 trägt zusätzlich die bauphysikalischen Felder der Quelle (u. a. μ, Feuchte, Emissionsgrad, Bemessungsmarke); welche Spalten genau, legt BK-2 aus dem Strukturbericht fest — nur fachliche Felder, keine internen Kennungen außer der Quellkennung; der Rechenweg liest weiterhin nur λ, ρ, c, Dicke, Luftschicht, U und die Fensterwerte |
| F3 Umfang | **alles übernehmen.** Bauteilaufbauten ohne Wärmeleitfähigkeit (reine U-Wert-Aufbauten, Schichten ohne λ) bekommen eine Kennzeichnung „für die Gebäudesimulation meist nicht geeignet“ und lassen sich in der Auswahl danach filtern. Baustoffe werden **nicht** nach λ gekennzeichnet. Bauteile **und** Baustoffe werden in **Gruppen und Klassen** eingeteilt | Regeln 4 und 8 in 2.3 entfallen als Ausschluss; dafür Kennzeichen am Aufbau und Filter in der Katalogliste und an der Bauteilwahl; Gliederung zweistufig (Gruppe, Klasse) für Baustoffe und Aufbauten aus den Gruppen- und Untertypen der Quelle, abgebildet auf feste EPOS-Gruppen mit Klassen darunter; die Abbildungstabellen legt BK-3 im Kern fest und das Protokoll nennt sie |
| F4 Dubletten | **zusammenführen, keine Dubletten** | gleiche Sätze (Regel in 2.3) werden ein Satz: innerhalb der Quelle ohnehin, gegen den EPOS-Katalog ebenso — ein ausgelieferter Satz bleibt mit seinen Werten bestehen, der Import ergänzt nur leere Felder und hängt seine Quelle an; kein zweiter Satz |
| F5 Fenster | Empfehlung: jetzt | BK-6 |
| F6 Wärmebrücken | Empfehlung: später | kein ψ-Katalog in BK |
| F7 Inhomogene Schichten | Empfehlung: bauen | BK-5 |
| F8 Lizenz | **Lizenzen geklärt, die Datensätze werden aufgenommen** | Importierte Sätze dürfen in den Auslieferungskatalog: die Löschregel der Auslieferungsvorlage für `Herkunft = 'IMPORT'` (2.3, BK-2) entfällt; Herkunft und Quelle bleiben am Satz. Die Werte gelangen über das Austauschformat in die Datenbank des Anwenders und von dort über `Werkzeuge/Auslieferungsvorlage` in die Auslieferung — nicht ins Repositorium (die Testdatenbank bleibt ohne Importsätze, Tests lesen synthetische Dateien). Das Wiki bleibt ohne Hersteller- und Produktdaten (Regel der Wurzel-`CLAUDE.md`) |

**Geänderte Wellen:** BK-2 ohne Löschregel, mit allen Feldern und den Spalten für Gruppe, Klasse und Kennzeichen; BK-3 mit
Gliederung, Kennzeichen und Zusammenführung statt Abwahl; BK-1 zeigt Gruppe und Klasse und filtert nach dem Kennzeichen.
Reihenfolge unverändert: BK-1 sofort, BK-2 und BK-6 mit einem Schemaschritt, BK-3 mit BK-4, BK-5, BK-7.

## 7 Wellenplan

| Welle | Inhalt | Aufwand | Abnahme |
|---|---|---|---|
| **BK-1** Editoren ohne Schema | c_p in der Liste immer sichtbar; Gruppe als feste Auswahl; Bänder je Gruppe als Hinweis; R_si/R_se im Summenfuß; Kapazität im Bauteildialog; Richtungsangabe innen → außen im Raster | 1–1,5 PT | Kern-Filter grün; bunit der Dialoge; Rasterprobe, falls `Katalogliste` betroffen |
| **BK-2** Schema und Auslieferungsregel | Schemaschritt (5.1); Herkunft `IMPORT`; `U_Quelle`; Regel und Prüfung in der Auslieferungsvorlage | 1,5–2 PT | Migrationstest gegen die Testdatenbank (IDs und Fremdschlüssel erhalten); SQL-Dialekt-Prüfer; Werkzeugtest „Importsatz fliegt raus“ |
| **BK-3** Austauschformat und Import | Leser im Kern, Prüfung, Abbildung, Dubletten, Transaktion, Hülle und Dialog „Bauteilkatalog einlesen…“ mit Vorschau | 3–4 PT | Kern-Tests mit synthetischen Dateien (Platzhalter, c-Einheit, λ-Regeln, SeqNum-Lücke, Dublette, Wiederimport per Quellkennung); bunit Vorschau; Windows-Schale baut |
| **BK-4** Werkzeug `Werkzeuge/HottgenrothKatalog` | LocalDB, Kopie anhängen, lesen, Austauschformat schreiben, Richtungsstichprobe und U-Abgleich als Bericht | 1,5–2 PT | Lauf beim Anwender auf der Kopie (Zählungen gegen Strukturbericht); Werkzeug-Tests ohne `.mdf` |
| **BK-5** Inhomogene Schicht | Schema `Abschnitt`/`Flaechenanteil`, R_T nach DIN EN ISO 6946 6.7, Editor-Unterzeilen | 2–3 PT | Kern-Tests gegen Handrechnung eines Normbeispiels; Referenzlauf der CI-Auswahl byte-gleich |
| **BK-6** Fenster im Aufbaukatalog | Fensterfelder, Übernahme an das Bauteil, Fensterteil im Editor | 1–1,5 PT | Kern-Test Übernahme; bunit Fensterteil |
| **BK-7** Wiki und Logbuch | Seiten „Baustoffe“, „Bauteilaufbauten“, „Bauteilkatalog einlesen“; Logbuch-Entwurf | 0,5 PT | Gegenlesemuster der Wurzel-`CLAUDE.md`; `WikiProduktdatenWacheTests` grün |

Summe rund 11–15 PT.

**Reihenfolge:**

- BK-1 kann sofort beginnen; Überschneidung mit AB3 nur an `BauteilschichtenFelder`, deshalb nach dem Merge von AB3.
- BK-2 und BK-6 können einen gemeinsamen Schemaschritt nutzen.
- BK-3 kommt nach F1 bis F4 und F8, BK-4 parallel zu BK-3, BK-5 nach F7.

Jede Welle hat einen eigenen Zweig und läuft mit dem Kern-Filter und `--filter` auf die betroffenen Klassen; das Gate fährt
die Orchestrierung nach dem Merge.

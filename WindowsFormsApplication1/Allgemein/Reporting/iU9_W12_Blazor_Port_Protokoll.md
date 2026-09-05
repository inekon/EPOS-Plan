# iU9 — Welle 12: Stromganglinie, Peak-Shaving, Importkonflikte

**Stand 04.09.2026.** Vermessung: `iU9_W12_Vermessung.md` (1 300 Zeilen, Stand
`d1683bd`) mit den Befunden W12‑B1 … W12‑B26. Basis dieser Welle: `73a4338`
(`ios_migration` nach W11b).

Vorbilder für Form und Tiefe: `iU9_W11b_Blazor_Port_Protokoll.md`,
`iU9_W10a_Blazor_Port_Protokoll.md`.

---

## 1. Auftrag und Ergebnis

**Sechs WinForms-Masken — 2 134 Zeilen `.cs`, 1 409 Zeilen Designer, zehn
`MessageBox` und dreizehn indirekte über `Program.ZahlPruefen` — sind sechs
Razor-Komponenten.** Jede WinForms-Fassung ist gelöscht (Regel M1).

| Nr | Komponente (`EPOS.UI/`) | ersetzt | Windows-Hülle |
|---|---|---|---|
| W12.1 | `Dialoge/Strom/GanglinieProtokollDialog` | `Form_GanglinieProtokoll` (148 + 122) | — (Überlagerung in 2 und 6) |
| W12.2 | `Dialoge/Strom/GanglinieImportOptionenDialog` | `Form_GanglinieImportOptionen` (383 + 337) | — (Überlagerung in 2 und 6) |
| W12.3 | `Dialoge/Import/ImportKonflikteDialog` | `Form_ImportKonflikte` (441, **ohne Designer**) | `Views/Import/ImportKonflikteHuelle.cs` (fällt mit W13) |
| W12.4 | `Dialoge/Strom/StromganglinieAdminDialog` | `Form_Stromganglinie_Admin` (276 + 142 + 3 `.resx`) | `Views/Stromverbraucher/StromganglinieAdminHuelle.cs` |
| W12.5 | `Dialoge/Strom/StromganglinieDialog` | `Form_Stromganglinie` (125 + 128 + 3 `.resx`) | `Views/Stromverbraucher/StromganglinieHuelle.cs` |
| W12.6 | `Dialoge/Strom/PeakShavingDialog` | `Form_PeakShaving` (761 + 680 + leere `.resx`) | `Views/Stromspeicher/PeakShavingHuelle.cs` |

**Der rote Faden ist die AP5-Importkette.** Sie stand zweimal wörtlich im
Bestand — `Form_Stromganglinie_Admin.btn_Einlesen_Click` (:93‑261, mit Ablage)
und `Form_PeakShaving.Datei_Click` (:322‑396, ohne). Seit W12.0d ist sie **ein**
Kern-Ablauf mit zwei Ausprägungen und drei Rückrufen
(`EPOS.Kern/Allgemein/Import/GanglinienImportAblauf.cs`).

**Der Nachweis der Welle ist der bitgleiche Import.** Dafür gab es keinen
einzigen Test (Befund W12‑B14). Die zwölf Proben und ihre eingefrorenen
Erwartungswerte sind deshalb der **erste** Schritt der Welle, nicht der letzte.

### Commits (15, auf `73a4338`)

| Commit | Schritt |
|---|---|
| `72dd8ba` | W12.0i — zwölf Ganglinien-Proben und der bitgleiche Nachweis des Imports |
| `d1f65be` | W12.0a — `PeakShavingCtrl` in den Kern, `OleDbException` aufgelöst |
| `cd99e77` | W12.0b — die Konfliktregeln des Imports in den Kern |
| `a85d426` | W12.0c — `GanglinienProtokollText` in den Kern, Farbe wird Stufe |
| `c303751` | W12.0d — die AP5-Importkette als EIN Kern-Ablauf |
| `e71d568` | W12.0e — die acht Auswahllisten des Importdialogs in den Kern |
| `d83b76e` | W12.0f — der Kennzahlenblock der Lastspitzenkappung in den Kern |
| `5c6f163` | W12.0g — das inline-SQL der Stromganglinien in den Kern, Befund B7 behoben |
| `4419051` | W12.1 — das Prüfprotokoll des Lastgangimports |
| `08bb8f0` | W12.2 — Format und Vorschau des Lastgangimports |
| `0ddd8f3` | W12.3 — der gemeinsame Konfliktdialog samt Hülle |
| `1c8c7c3` | W12.4 — die Stromganglinien-Verwaltung |
| `58d02f6` | W12.5 — die Stromganglinien-Zuordnung |
| `5069180` | W12.6 — die Lastspitzenkappung |
| `5a99cab` | W12.6a — die zwei Zwischenmasken der Importkette gelöscht |

W12.7 (Ressourcen), W12.8 (Formularkarte) und W12.9 (Protokoll, Statusblöcke)
stehen im Abschlusscommit dieser Datei.

**W12.0h ist ohne eigenen Commit geblieben** — siehe § 3.4: Der Auftrag sah
einen neuen Renderer vor, der Bestand trug ihn schon.

---

## 2. Die zwei Entscheidungen der Welle

### 2.1 `Form_ImportKonflikte` wird in W12 gebaut — Blatt vor Host, mit Hülle

Der Konfliktdialog hat **fünf** Aufrufer: einer wird mit W12.4 Blazor, die vier
Importmasken der Welle 13 bleiben bis dahin WinForms. Bliebe die alte Fassung
stehen, müsste `StromganglinieAdminDialog` **mitten in einem Rückruf** ein
modales WinForms-Fenster öffnen *und* eine `List<KonfliktEntscheidung>`
zurückbekommen. Die `Sprungbruecke` kann das nicht: Sie löst Schlüssel → `Form`
auf und liefert einen `bool`.

Deshalb: Komponente **und** `ImportKonflikteHuelle` mit der Signatur des
Vorläufers. Die vier W13-Aufrufer
(`Form_Heizkessel_einlesen:240`, `Form_PufferSp_einlesen:227`,
`Form_WP_einlesen:197`, `Form_CECImport:484`) ändern **je eine Zeile**. Die
Hülle wird mit Welle 13 gelöscht; Lebensdauer eine Welle, Kosten rund 80 Zeilen.

### 2.2 Kein neuer Renderer für das Peak-Shaving-Bild (W12.0h)

Der Auftrag ließ die Wahl: `ChartRenderer.ErzeugerStapel` prüfen und, wenn es
trägt, **nutzen** — sonst `ChartRenderer.LastgangKappung` neu bauen.

`ErzeugerStapel` trägt seit iU9‑W11a (Bild B3) eine **Sekundärachse**
(`zweiteAchse`, `y2Titel`), zeichnet Linien ohne Stapel, y2 ab null und ohne
Hauptgitter, misst 1 240 × 560 und rechnet die vier Jahresstundenmarken über die
Reihenlänge um — also auch im Viertelstundenraster, ohne auf 8 760 zu kappen.
Genau das ist die Falle, an der `ChartZeichnen` des Vorläufers zwei Schalter
brauchte (`MaxXVALUE` **und** `MitViertelStunde`, Kommentar :676‑681).

**Also kein neuer Renderer und keine neue ChartProbe.** Neu ist nur
`EPOS.Kern/Allgemein/Bericht/PeakShavingBild.cs` (77 Z.), das die drei Reihen
mit den Farben des Vorläufers zusammenstellt und `ErzeugerStapel` ruft. Die Zahl
der geprüften Bilder bleibt **30**.

---

## 3. Bauweise

### 3.1 Die Importkette — ein Ablauf, drei Rückrufe

`GanglinienImportAblauf.MitAblage(pfad, rasterVorgabe, rueckrufe)` und
`OhneAblage(pfad, rueckrufe)`. Die Schritte sind die des Kommentarblocks
`Form_Stromganglinie_Admin.cs:79‑92`: Erkenne → Optionen → Lies → Prüfe →
Protokoll → Dubletten/Konflikte → Ablage **oder** Rückgabe.

**Der Ablauf zeigt nichts an.** Er legt Entscheidungen als `Func<…, Task<…>>`
vor und liefert seine Meldungen (`IMPORT_MSG_*`) als fertigen Text im Ergebnis;
ob daraus ein Warnbanner, eine `MessageBox` oder eine iOS-Blase wird,
entscheidet der Wirt.

**Der Wirt zeigt sie als Überlagerung.** Jeder Rückruf setzt seinen
Sichtbarkeitsschalter, ruft `InvokeAsync(StateHasChanged)` und wartet auf eine
`TaskCompletionSource`, die der Unterdialog beim Schließen auflöst. Kein zweites
Fenster, keine zweite WebView (Risiko R2). Die Kette selbst läuft in der Hülle
auf `Task.Run`.

**Ein sauberer Lauf legt das Protokoll gar nicht erst vor** — dieselbe Regel wie
`Form_GanglinieProtokoll.Zeigen` :93. Die statische Tür des Vorläufers ist eine
Hilfsfunktion des Wirts geworden (`Noetig(importMoeglich, bestaetigungNoetig)`).

### 3.2 Was in den Kern gezogen ist

| Datei (neu bzw. verschoben) | Herkunft | Grund |
|---|---|---|
| `Controller/PeakShavingCtrl.cs` (331) | `WindowsFormsApplication1/Controller/` | oberflächenfrei, aber für `EPOS.UI` unerreichbar (B23) |
| `Allgemein/Katalog/ImportKonfliktModell.cs` | `Form_ImportKonflikte` :9‑24, :222‑417 | sonst zöge `EPOS.UI` eine WinForms-Datei (B18) |
| `Allgemein/Import/GanglinienProtokollText.cs` | `Views/Stromverbraucher/` | vier Aufrufstellen in zwei Masken, beide werden Razor |
| `Allgemein/Import/GanglinienImportAblauf.cs` | die Kette, zweimal im Bestand | **B1**, der Kern der Welle |
| `Allgemein/Import/GanglinienOptionenModell.cs` | `Form_GanglinieImportOptionen` :36‑63 | Blazor und iOS brauchen dieselben Listen |
| `Controller/PeakShavingKennzahlenBlock.cs` | `Form_PeakShaving` :583‑608 | Muster `SpeicherKennzahlenBlock` (W11a) |
| `Controller/PeakShavingEingaben.cs` | `Form_PeakShaving.ParameterLesen` :419‑480 | vier Regeln und vier Umrechnungen — Fachaussagen |
| `Allgemein/Bericht/PeakShavingBild.cs` | `Form_PeakShaving.ChartZeichnen` :682‑728 | eine Wahrheit über Reihen und Farben |
| `Z_ProjektStromganglinieCtrl.LiesProjekt`, `StromganglinieStammCtrl.FindeStamm` | drei inline-SQL | **B4**, Konkatenation mit Anwendertext |

**Zwei Zählungen der Vermessung sind dabei nachgerechnet worden** (W12.0f): Es
sind **18** Kennzahlen, nicht 17 (5 + 4 + 3 + 6), und **12** Monatszeilen, nicht
13 — „Gesamtreihe" (Monat 0) ist die Sammelposition der Engine für ein Raster
ohne ganzzahliges Tagesmaß, keine dreizehnte Zeile.

### 3.3 Der Rechenlauf läuft nebenher (Befund W12‑B22, behoben)

`BerechnePeakShaving` über 35 040 Werte und `MinimaleSchwelleKw` mit ihrer
Suchschleife über ganze Jahresläufe liefen im Oberflächenfaden. **In einer
WebView ist der Renderfaden derselbe Faden.** Beide gehen deshalb über einen
Delegaten, den `PeakShavingHuelle` auf `Task.Run` legt; ebenso das Lesen der
Ganglinienwerte (`PeakShavingCtrl.LeseWerte`, bis 35 040 Zeilen) und das
Zeichnen des Bildes. Der Wirt zeigt für die Dauer den Baustein `Fortschritt`
mit unbestimmtem Balken und sperrt alle Felder.

**Ohne Abbrechen-Knopf, mit Absicht.** Die Engine kennt keinen Abbruch; ein
Knopf ohne Wirkung wäre schlechter als keiner (Regel des Bausteins,
iU9‑W11a.7).

### 3.4 Das Bild

`PeakShavingBild.Lastgang(ergebnis, mitSoC)` → `ChartRenderer.ErzeugerStapel`
mit leerem Stapel, zwei Linien, ohne Kontur, `Achse.Jahresstunden`,
`sortiert = false` und — nur wenn der Schalter steht — dem Ladezustand als
`zweiteAchse`. Farben wörtlich: `(190, 90, 90)`, `(40, 110, 180)`,
`(120, 130, 140)`.

---

## 4. Feldkarten-Abgleich

Die Feldkarte wurde für jede der fünf Masken mit Designer **neu gezogen**
(`dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`), zuletzt für
`Form_Stromganglinie` (7 Zeilen: 2 ListBox, 5 Button) und `Form_PeakShaving`
(59 Steuerelemente).

| Maske | Kartenzeilen | in der Komponente |
|---|---|---|
| `Form_GanglinieProtokoll` | 6 | Kopftext, Raster (Stufe / Meldung), OK, zweiter Fußknopf |
| `Form_GanglinieImportOptionen` | 8 Listen + Schalter + Vorschau | acht `Auswahlfeld`, `Schalter`, Vorschauraster, drei Fußknöpfe |
| `Form_ImportKonflikte` | (ohne Designer, gegen den Quelltext) | Raster Eintrag / Befund / Aktion, „Alle auslassen", Übernehmen, Abbrechen |
| `Form_Stromganglinie_Admin` | 10 | Katalogliste, Rasterliste (2 Einträge), Dateiwahl, Löschen, OK |
| `Form_Stromganglinie` | **7** | 2 Listen, ◀, ▶, „Bearbeiten…", OK, Abbrechen |
| `Form_PeakShaving` | **59** | Optionsgruppe, Auswahlfeld, Dateiwahl, 14 `Zahlenfeld`, 3 `Schalter`, 2 Knöpfe, 3 `Reiterblatt`, CSV, Schließen |

**Kein Feld ist verlorengegangen.** Was ersatzlos entfällt, steht in § 5.

---

## 5. Abweichungen (A‑Zeilen)

| Nr | Abweichung | Begründung |
|---|---|---|
| **A‑1** | „▶" entfernt die **gewählte Zeile**, nicht den ersten Namensvetter | `btn_Entfernen_Click` :89 verglich `m_szStromganglinie == listBox.Text`; zwei gleich benannte Zuordnungen trafen immer die erste. Dieselbe A‑Zeile wie in W7.8. |
| **A‑2** | `SetControls(szProjekt)` ersatzlos | Der Parameter wurde nicht benutzt, und der Datenbankzugriff :43‑44 las ohne Verwendung (**B2**). |
| **A‑3** | `Z_ProjWaermebedarfModel` als Zwischenablage ersatzlos | Typverwechslung, folgenlos, aber irreführend (**B3**). |
| **A‑4** | Die ReadOnly-Meldung steht als `IMPORT_MSG_SCHREIBGESCHUETZT` im Katalog | Sie stand hartkodiert deutsch im Quelltext (**B12**). |
| **A‑5** | **Rückfrage vor dem Löschen** eines Katalogeintrags | Der Vorläufer löschte ohne jede Sicherheitsabfrage (**B12**, zweite Hälfte). |
| **A‑6** | Der Fehlschlag der Originalkopie steht als Warnung im Protokoll | `catch { }` verschluckte ihn (**B13**). Wer glaubt, sein Original sei gesichert, und es ist nicht so, merkt es sonst erst, wenn er es braucht. |
| **A‑7** | Der Pfad ist Parameter, kein Feld | `filebasename` war ein Feld: Brach der Anwender ab, lief die Kette mit der Datei des **vorigen** Laufs weiter (**B13b**). |
| **A‑8** | Das Protokoll hat einen `InfoKnopf` | Es war als einziges Glied der Kette ohne Hilfeeinstieg (**B17**); Ziel ist die vorhandene Zeile `Form_GanglinieImportOptionen.btn_Help`. |
| **A‑9** | `HilfeKontext` kennt den Konfliktdialog | Er hatte als einzige der sechs Masken keinen Bereich, obwohl `help_mapping.txt:167` seit H1/H2 eine Zeile führt (**B20**). |
| **A‑10** | Die Konfliktaktion ist ein **Wert** | Der Vorläufer las sie aus dem Anzeigetext der Zelle zurück (**B19**); ein Sprachwechsel zur Laufzeit hätte die Zuordnung zerrissen. |
| **A‑11** | Der Fehlsprung `Gewerke.WaermebedarfExtern` ist gestrichen | `StromganglinieKontextMenuCtrl:152` sprang mitten im Stromganglinien-Ablauf in ein fremdes Gewerk (**B7**); die richtige Auffrischung stand unmittelbar danach. |
| **A‑12** | Beide Rechenläufe laufen in `Task.Run` mit `Fortschritt` | **B22**; in einer WebView ist der Renderfaden derselbe Faden. |
| **A‑13** | `catch (OleDbException)` → `catch (Exception)` | Seit der SQLite-Umstellung wirft der Zugriff `SqliteException`; der Rückfall auf die Vorgaben griff gar nicht mehr (**B25**). |
| **A‑14** | Die Zahlenmeldung kommt aus dem Baustein | `Program.ZahlPruefen` zeigte eine hartkodiert deutsche `MessageBox` (**B9**); das `Zahlenfeld` färbt und meldet seinen Namen (`HZKK_MSG_ZAHL`). |
| **A‑15** | Der dritte Kopftext `IMPORT_KOPF_OK` ist erreichbar | Im Vorläufer toter Code, weil `Zeigen` bei sauberem Lauf gar kein Fenster öffnete. |
| **A‑16** | Die beanstandete Konfliktzeile wird **hervorgehoben**, nicht fokussiert | Ein Raster hat keinen Zellfokus. |
| **A‑17** | Die Wahlspalte heißt „Wahl" (`KFAK_SP_WAHL`) | W12.4 trug dafür zunächst `SIM_BTN_OK` („OK") ein — eine Spaltenüberschrift „OK" über runden Wahlknöpfen. Der Hausschlüssel steht seit W1.5 fest. |
| **A‑18** | Die y‑Obergrenze des Bildes ist die geglättete Datenobergrenze, nicht `PAltMax × 1,05` | Der Bestandsrenderer rundet selbst auf einen glatten Wert; eine eigene Obergrenze hätte einen zweiten Renderer gekostet (§ 2.2). Eine x‑Achsenbeschriftung kennt das Bild nicht — die Jahresstundenmarken stehen an der Achse. |
| **A‑19** | `Views/Stromspeicher` bekommt kein interaktives Chart mehr | Der `ChartManager` bleibt (`Form_Klimadaten`), sein Kommentar ist nachgezogen. |

### Wörtlich trotz Befund

| Nr | Verhalten | warum es bleibt |
|---|---|---|
| **B5** | **Keine Dublettenprüfung beim Hinzufügen** — derselbe Katalogeintrag lässt sich beliebig oft zuordnen | Das ist heute so. Ob es so bleiben soll, ist eine **Anwenderfrage** (offener Punkt W12‑O‑1). |
| **B15** | Die Abbildung kennt drei Raster-Plätze, die Auswahlliste hat zwei — `case 2` ist unerreichbar | Die Aussage des Vorläufers; ein dritter Listeneintrag könnte sie morgen brauchen. |
| **B16** | Der OK-Knopf der Importoptionen prüft nichts | Wert- und Zeitspalte dürfen auf demselben Platz stehen; das fällt erst in `GanglinienDatei.Lies` als Protokollfehler auf. |
| **B24** | `MitOk` liefert für Peak-Shaving **immer** `false` | Der einzige Fußknopf trug `DialogResult.Cancel`; niemand wertet den Rückgabewert aus. Die Hülle liefert ihn unverändert. |
| — | Die Vorschau ist **nicht** reaktiv | „Vorschau aktualisieren" ruft `GanglinienDatei.Vorschau`, das nichts rät. Eine Live-Vorschau wäre billiger — und ein anderes Verhalten. |
| — | `chk_Adaptiv` steht beim Öffnen **immer** an | Feste Vorgabe des Vorläufers (:250), nicht aus der Variante. |
| — | Der Engine-Text geht ungefiltert in die Meldung | Wörtlich :533‑538. |
| — | Der Fußknopf der Importoptionen heißt `SIM_BTN_OK`, nicht `IMPORT_BTN_OK` | Begründung des Vorläufers (:143‑148): Der Fachknopf dieses Dialogs ist „Vorschau aktualisieren". |

### Ersatzlos entfallen

`Program.ZahlFaerben`/`ZahlPruefen` (→ `Zahlenfeld`), `FensterEinpassung`,
`m_ID_Projekt`/`m_szProjekt`/`result`/`DateiListe` der Verwaltungsmaske,
`btn_Abbrechen_Click` ohne Knopf (**B11**), der Leseweg von `SetControls`
(**B2**), `btn_Minimal.Enabled = true` ohne Wirkung (:407), die sechs verwaisten
`.resx`-Einträge (**B8**, **B8b**) und die nie gezeigte Maske in
`StromganglinieKontextMenuCtrl:86` (**B6**).

---

## 6. Texte (W12.7)

**Die 18 wirksamen en-Texte der beiden lokalisierten Masken stehen im
Kern-Katalog**; die sechs verwaisten sind entfallen.

| Herkunft | Ziel |
|---|---|
| `Form_Stromganglinie.en-US.resx` (8 wirksam) | `STROMGL_*` (7 neu in W12.4), `SIM_BTN_OK`, `IMPORT_BTN_ABBRECHEN`, `HZK_TIP_*` |
| `Form_Stromganglinie_Admin.en-US.resx` (10 wirksam) | `IMPORT_*` (10 neu in W12.4) |
| verwaist: `Label3`, `btn_Hilfe`, `textBox_Name`, `btn_Oeffnen`, `btn_Datei`, `label6` | entfallen |

**Neue Schlüssel dieser Welle: 19**, beide Sprachen —
17 in W12.4 (`IMPORT_*`, `STROMGL_*`, darunter
`IMPORT_MSG_SCHREIBGESCHUETZT` für A‑4) und **zwei** in W12.6
(`PEAK_MSG_RECHNET`, `PEAK_MSG_SUCHE` für die Fortschrittszeile).
Die vorhandenen Kataloge sind unverändert wiederverwendet:
`IMPORT_*` (54) + `IMPORT_PROT_*` (40), `IMP_KONFLIKT_*` (20), `PEAK_*` (90).

**Beide `.resx` haben denselben Schlüsselsatz** — 3 935 Einträge, 0 nur in einer
Sprache (geprüft mit einem Mengenvergleich über beide Dateien).

---

## 7. Formularkarte (W12.8)

**Der Anker des Erreichbarkeitstests hing an `Form_Stromganglinie`** und musste
umgehängt werden. Er kann seine Form („über die **Startseite**") nicht behalten:
Von den zwölf Masken mit einem Pfad ab `Form_Start` fällt keine erst in W13 oder
W14 — alle W13/W14-Masken hängen am Menü des `MDIMainForm` (**Befund W12‑B26**).

**Nachfolger: `Form_AdminSettings`** über `MDIMainForm → MenuItem_Einstellungen`
— der kürzeste und stabilste Weg im Bestand, und W14c ist die **letzte** der
W13/W14-Wellen. Der Test heißt jetzt
`FormAdminSettingsIstUeberDasHauptfensterZuErreichen`; der zweite Zeuge in
`DieUebersichtZaehltDieErreichbarkeit…` prüft `| Form_AdminSettings | ja |`.

| Zählung | vor W12 | nach W12 |
|---|---|---|
| Designer-Dateien (Repo) | 44 | **39** |
| Masken (Repo) | 43 | **38** |
| davon lokalisiert | 27 | **25** |
| erreichbar „ja" (`WindowsFormsApplication1`) | 42 von 43 | **37 von 38** |
| „nein" / „verwaist" / „unklar" | 0 / 0 / 1 | 0 / 0 / 1 |

Die eine „unklar"-Maske bleibt `Form_PufferSp_Bearbeiten` (Welle 14a).
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md` ist nachgezogen.

**`help_mapping.txt` bleibt unverändert.** Die sechs Zeilen der Welle
(`:167`, `:249‑253`) sind die Adresse eines **Hilfetextes**, nicht einer Klasse;
die Razor-Komponenten führen sie als `HilfeSchluessel` weiter — dieselbe Praxis
wie bei `Form_Solarganglinie.btn_Help` seit W7.8. `HilfeKontext` dagegen bildet
**Klassennamen** ab und ist umbenannt: `PeakShavingDialog`,
`StromganglinieDialog`, `StromganglinieAdminDialog`,
`GanglinieImportOptionenDialog`, `GanglinieProtokollDialog`,
`ImportKonflikteDialog`.

---

## 8. Nachweise

### 8.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64
→ Build succeeded. 0 Fehler, 12 Warnungen
```

Die zwölf sind die bekannten: 6 × WFO1000, 2 × CS0108, 2 × CS0109,
1 × WFO0003, 1 × CA2255. **Keine neue.**

### 8.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ EPOS.Kern.Tests      480 gruen
  EPOS.UI.Tests      1 540 gruen
  SpeicherEngine.Tests 337 gruen
  KiKern.Tests         450 gruen
  = 2 807 gruen, 0 rot

dasselbe mit LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8
→ 2 807 gruen, 0 rot
```

Basis vor der Welle: 2 614, also **193 Fälle mehr**. 189 davon stehen in
fünfzehn neuen Klassen, die übrigen vier in vorhandenen:

| Datei | Fälle | Gegenstand |
|---|---|---|
| `EPOS.Kern.Tests/GanglinienProbenTests.cs` | 18 | der bitgleiche Import, zwölf Proben |
| `EPOS.Kern.Tests/GanglinienImportAblaufTests.cs` | 12 | dieselben Proben durch den neuen Ablauf |
| `EPOS.Kern.Tests/ImportKonfliktModellTests.cs` | 22 | die Regeltabelle des Konzepts, Zeile für Zeile |
| `EPOS.Kern.Tests/GanglinienAnzeigeTests.cs` | 11 | Protokolltexte und die acht Steuerwertlisten |
| `EPOS.Kern.Tests/PeakShavingCtrlTests.cs` | 6 | Vorbelegung ohne Speicherprojekt, Ganglinienliste, Rasterwiederholung |
| `EPOS.Kern.Tests/PeakShavingKennzahlenBlockTests.cs` | 9 | 18 Kennzahlen, 12 Monatszeilen, Negativkennzeichen |
| `EPOS.Kern.Tests/StromganglinieSqlTests.cs` | 4 | `LiesProjekt`, `FindeStamm`, Apostroph |
| `EPOS.Kern.Tests/PeakShavingEingabenTests.cs` | 13 | vier Regeln, vier Umrechnungen, Vorbelegung |
| `EPOS.Kern.Tests/PeakShavingBildTests.cs` | 6 | PNG, 1 240 × 560, Sekundärachse, Determinismus |
| `EPOS.UI.Tests/Dialoge/GanglinieProtokollDialogTests.cs` | 15 | W12.1 |
| `EPOS.UI.Tests/Dialoge/GanglinieImportOptionenDialogTests.cs` | 14 | W12.2 |
| `EPOS.UI.Tests/Dialoge/ImportKonflikteDialogTests.cs` | 16 | W12.3 |
| `EPOS.UI.Tests/Dialoge/StromganglinieAdminDialogTests.cs` | 14 | W12.4 |
| `EPOS.UI.Tests/Dialoge/StromganglinieDialogTests.cs` | 12 | W12.5 |
| `EPOS.UI.Tests/Dialoge/PeakShavingDialogTests.cs` | 17 | W12.6 |

Die Texttests pinnen die Oberflächensprache (Regel seit W8);
`TestDatenbank` wird nur lesend je Klasse geteilt (Regel seit W11a).

### 8.3 Die Import-Proben — der eigentliche Nachweis

```
EPOS.Kern.Tests/Proben/Ganglinien/
  p01_stunden_semikolon_komma_kopf.csv       8 760, ';'  ','  mit Kopf
  p02_stunden_komma_punkt_ohne_kopf.csv      8 760, ','  '.'  ohne Kopf
  p03_stunden_tab_komma_kopf.csv             8 760, Tab  ','  mit Kopf
  p04_stunden_einspaltig_punkt.txt           8 760, einspaltig '.'
  p05_viertelstunden_semikolon_punkt_kopf.csv 35 040, ';' '.'  mit Kopf
  p06_viertelstunden_einspaltig_komma.txt    35 040, einspaltig ','
  p07_schaltjahr_stunden_semikolon_kopf.csv   8 784, Schaltjahr
  p08_sommerzeit_luecke_stunden.csv           8 759, Zeitumstellung Frühjahr
  p09_sommerzeit_dublette_stunden.csv         8 761, Zeitumstellung Herbst
  p10_viertelstunden_kwh_je_intervall.csv    35 040, Einheit kWh je Intervall
  p11_stunden_excel.xlsx                      8 760, ClosedXML
  (p12)                                     525 600 Minutenwerte, im Test erzeugt
```

**Die Erwartungswerte sind aus dem Bestand vom 04.09.2026 eingefroren** — VOR
dem Umbau der Kette, auf die letzte Stelle: Vorschlag der Erkennung, Rohreihe,
Prüfergebnis samt Protokoll, Stichwerten und Summe.
`GanglinienImportAblaufTests` fährt **dieselben** Proben durch den neuen Ablauf
und erwartet **dieselben** Zahlen. Beides grün.

**Befund W12‑B27 (neu, behoben):** Der Excel-Zweig war überhaupt nicht
benutzbar. `ExcelBulkRead` legt sein `object[,]` eins größer an, damit es sich
1‑basiert wie Excel ansprechen lässt; die drei Leseschleifen zählten aber bis
`GetLength()` statt bis `GetLength() - 1`. Jeder `.xlsx`-Import endete in
`IMPORT_PROT_LESEFEHLER` „Index was outside the bounds of the array". Damit ist
der offene Nachweispunkt `Umsetzung_iU0_iU1_Nachweise.md:136` erklärt — und mit
Probe p11 belegt; die Zeile ist abgehakt.

### 8.4 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 231 SQL-Texte geprueft: 0 Fundstellen, 171 dynamisch, 1 060 in Ordnung
```

1 233 → 1 231: Die drei konkatenierten Anweisungen der Masken sind zwei
parametrisierte Kern-Wege geworden (W12.0g).

### 8.5 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 30 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

**Unverändert 30** — die Welle bringt kein neues Renderer-Bild (§ 2.2).

### 8.6 Referenzlauf

```
dotnet run --project EPOS.Referenzlauf -c Release -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w12
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich artifacts/reflauf/ref artifacts/reflauf/w12
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt gegen Referenzlaeufe/2026-08-30_B3-Kaskade
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

**Byte-gleich, nicht nur innerhalb der Toleranz.** Die Welle fasst den
Rechenweg nicht an.

### 8.7 Formularkarte

```
dotnet test Werkzeuge/Formularkarte.Tests -c Release
→ 123 gruen

dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1
→ 38 Masken, 25 lokalisiert, 37 erreichbar, 0 nein, 0 verwaist, 1 unklar
```

### 8.8 Keine Typverwendung ist übrig

`git grep` über `*.cs` und `*.razor` nach den sechs Klassennamen und den zwei
verschobenen Begleitklassen findet **nur noch Kommentare, Maskenschlüssel und
Hilfeadressen** — keinen Aufruf, kein `new`, keine Typreferenz.

---

## 9. Grenzen

- **`ImportKonflikteHuelle` ist Zwischenstand.** Sie lebt eine Welle: Sobald die
  vier Importmasken der Welle 13 selbst Razor sind, wird sie gelöscht.
- **`StromganglinieDialog` schreibt nichts.** Die Hülle gibt die Liste zurück,
  abgelegt wird beim Aufrufer (`Form_Start`, `StromganglinieKontextMenuCtrl`) —
  bis W16 den Assistenten umstellt (Risiko R‑W12‑4).
- **Der Assistentenschnitt ist vorbereitet, nicht benutzt.**
  `StromganglinieDialog` führt `Wizard` und eine geteilte Liste; `Wizard_Stromlastgang`
  (W16) kann sie ohne zweiten Bau übernehmen (**B10**), ist aber noch WinForms.
- **Keine Bildschirmabnahme.** Diese Umgebung hat kein Windows; alle
  Oberflächenaussagen stützen sich auf bunit und die Feldkarte. Die Liste in
  § 10 ist deshalb offen.

---

## 10. Abnahmeliste Windows (iZ5)

- [ ] Menü → **Stromganglinien-Verwaltung**: Liste, Rasterwahl (zwei Einträge),
      „Ganglinie Löschen" mit Rückfrage, ReadOnly-Sperre.
- [ ] Dort **Datei einlesen**, CSV mit `;` und mit `,`, dazu eine `.xlsx` —
      Optionen, Protokoll, Konfliktdialog (Umbenennen, Überschreiben, Auslassen).
- [ ] Startbild → **Strom-Messdaten**: ◀ / ▶, „Bearbeiten…" als Überlagerung,
      Katalog danach frisch; Kachelstatus stimmt auch nach Abbrechen.
- [ ] Menü → **Lastspitzenkappung**: Ganglinie und Datei, „Berechnen" mit
      laufendem Balken, „Minimale haltbare Schwelle" (Wert landet im Feld,
      adaptiv geht aus), drei Reiter, SoC-Schalter, CSV-Export.
- [ ] Dieselbe Maske **ohne geöffnetes Projekt** (Projekt-Id 0).
- [ ] **W13-Importmasken** → Konfliktdialog über die Hülle: Heizkessel,
      Pufferspeicher, Wärmepumpe, CEC — je ein Aufruf.
- [ ] de **und** en, 125 %, Esc je Ebene.

---

## 11. Offene Punkte

| Nr | Punkt |
|---|---|
| **W12‑O‑1** | **Anwenderfrage: Dublettenprüfung beim Hinzufügen (Befund B5).** Derselbe Katalogeintrag lässt sich einem Projekt beliebig oft zuordnen — heute wie früher. Soll die Maske das künftig verhindern, oder ist die Mehrfachzuordnung gewollt (wie bei den Erzeugern der Welle 6, wo sie es ausdrücklich ist)? |
| **W12‑O‑2** | **Befund B28 (neu, wörtlich behalten).** Trägt der Ablageordner `%LOCALAPPDATA%\WP-Plan\Strom` schon eine gleichnamige Datei, wird **diese** gelesen und nicht die soeben gewählte (Bestandsverhalten :132‑133). Eine zweite Datei gleichen Namens mit anderem Inhalt geht damit still verloren. |
| **W12‑O‑3** | Der Wizard-Zwilling `Wizard_Stromlastgang` ist bis **W16** eine zweite Fassung derselben Sache (**B10**). Die Komponente ist dafür geschnitten. |
| **W12‑O‑4** | `ImportKonflikteHuelle` wird mit **W13** gelöscht. |
| **W12‑O‑5** | Die Windows-Abnahme (§ 10) steht aus. |

---

## 12. Geänderte und neue Dateien

**Neu in `EPOS.Kern`:** `Allgemein/Import/GanglinienImportAblauf.cs`,
`Allgemein/Import/GanglinienOptionenModell.cs`,
`Allgemein/Import/GanglinienProtokollText.cs` (verschoben),
`Allgemein/Katalog/ImportKonfliktModell.cs`,
`Allgemein/Bericht/PeakShavingBild.cs`,
`Controller/PeakShavingCtrl.cs` (verschoben),
`Controller/PeakShavingKennzahlenBlock.cs`,
`Controller/PeakShavingEingaben.cs`.

**Neu in `EPOS.UI`:** `Dialoge/Import/ImportKonflikteDialog.razor`,
`Dialoge/Strom/GanglinieProtokollDialog.razor`,
`Dialoge/Strom/GanglinieImportOptionenDialog.razor`,
`Dialoge/Strom/StromganglinieAdminDialog.razor`,
`Dialoge/Strom/StromganglinieDialog.razor`,
`Dialoge/Strom/StromganglinieDaten.cs`,
`Dialoge/Strom/PeakShavingDialog.razor`.

**Neu in `WindowsFormsApplication1`:** `Views/Import/ImportKonflikteHuelle.cs`,
`Views/Stromverbraucher/StromganglinieAdminHuelle.cs`,
`Views/Stromverbraucher/StromganglinieHuelle.cs`,
`Views/Stromspeicher/PeakShavingHuelle.cs`.

**Gelöscht (18 Dateien):** die sechs Masken mit ihren fünf Designern und sechs
`.resx`, dazu `Views/Stromverbraucher/GanglinienProtokollText.cs` und
`Controller/PeakShavingCtrl.cs` an ihren alten Plätzen.

**Geändert:** `EPOS.Kern/Allgemein/Import/GanglinienDatei.cs` (B27),
`Controller/StromganglinieStammCtrl.cs`, `Controller/Z_ProjektStromganglinieCtrl.cs`,
die drei `MyResource`-Dateien, `EPOS.UI/wwwroot/epos-ui.css`,
`Dienste/WinFormsNavigation.cs`, `Controller/StromganglinieKontextMenuCtrl.cs`,
`Views/Hauptformular/Form_Start.cs`, die vier W13-Importmasken (je eine Zeile),
`Allgemein/KI/HilfeKontext.cs`, `Allgemein/GrafikTools/ChartManager.cs`,
`Werkzeuge/Formularkarte.Tests/{StapelTests,ErreichbarkeitTests}.cs`,
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md`,
`Umsetzung_iU0_iU1_Nachweise.md`.


## Anwenderentscheid #76 (05.09.2026) — ein Schema für alle Projekt↔DB-Auswahldialoge

**Der Entscheid.** Nach der Windows-Abnahme (PDF „iOS_Migration_Probleme", S. 2, 6–8)
hat der Anwender festgelegt: *Alle* Dialoge, in denen links „im Projekt ausgewählt" und
rechts „aus der Datenbank/Katalog" mit Pfeilknöpfen dazwischen stehen, folgen dem alten
**BHKW-PLAN-Schema NEBENEINANDER** — Projektliste links, Katalogliste rechts, die zwei
Pfeilknöpfe in einer schmalen Mittelspalte. Auf **schmalem Schirm** (iPad hochkant,
schmales Fenster) bricht das Paar automatisch **untereinander** um; dann gilt das
Schema, das der Gebäudedialog seit Welle 9 hatte (Projektliste oben, Pfeile dazwischen,
Katalog unten). Listen sind in beiden Fällen höhenbegrenzt mit Rollbalken (Befund
W9‑B‑2, `.epos-raster-huelle` / `--epos-listenhoehe`).

**Ein Baustein statt elf Markups.** `EPOS.UI/Bausteine/Zweispaltenauswahl.razor` trägt
drei benannte Bereiche — `Links` (Projekt), `Mitte` (die zwei Knöpfe), `Rechts`
(Katalog) — dazu die Überschriften, die vier Texte der Knöpfe, ihre Sperrzustände und
Rückrufe sowie `NurRechts` für die Verwaltungsbetriebsart. Der Stilblock
„Zweispaltenauswahl" in `EPOS.UI/wwwroot/epos-ui.css` steht direkt hinter dem alten
Block AUSWAHLPAAR; die alte Klasse `.epos-auswahlpfeile` ist entfallen,
`.epos-auswahlpaar`/`.epos-auswahlspalte` bleiben für die fünf Masken **ohne** Pfeile
(`GebaeudetypDialog`, `TypProfilDialog`, `KennlinienEditorDialog`,
`WaermepumpeAnlageDialog`, `WaermepumpeStammDialog`).

**Das Zeichen hängt an der Anordnung, nicht am Text.** Ein Pfeil im Ressourcentext kann
nicht wissen, wie die Listen gerade stehen. Jeder Knopf trägt deshalb **beide** Zeichen
im Markup (`aria-hidden`, damit eine Sprachausgabe den Satz liest und nicht das
Dreieck), und das Stilblatt zeigt je Breite genau eines: nebeneinander **◀/▶** (die
Zeile wandert nach links ins Projekt bzw. nach rechts in den Katalog zurück),
untereinander **▲/▼**. Kein JavaScript.

> **Zur Pfeilrichtung.** Der Entscheidtext nennt in der Klammer „▶ In das Projekt
> übernehmen, ◀ Aus dem Projekt entfernen". Umgesetzt ist es **umgekehrt** — ◀
> übernimmt, ▶ entfernt —, weil derselbe Satz die Projektliste ausdrücklich **links**
> verortet und weil das Vorbild es so hält: `Form_Gebaeude.resx` `btn_Hinzu` = „◀",
> `btn_Entfernen` = „▶"; `Form_Heizkessel.resx` `btn_Kessel_Hinzu` = „◀",
> `btn_Kessel_Entfernen` = „▶". Bei Projektliste links zeigt „übernehmen" nach links.
> Soll es doch andersherum sein, sind es zwei Zeichen in
> `Bausteine/Zweispaltenauswahl.razor` — sonst nichts.

**Der Umbruch ist eine Medienabfrage, kein `flex-wrap`.** Nur so weiß das Stilblatt,
welches Zeichen gerade gilt; bei `flex-wrap` käme die Reihe um, ohne dass eine Regel es
merkt, und die Pfeile zeigten ins Leere. Die Umbruchbreite steht als Token
`--epos-zweispalten-umbruch` (900 px) **und** — weil eine Medienabfrage kein Token
lesen kann — ein zweites Mal in der Abfrage; die Wache
`ZweispaltenauswahlTests.Die_Umbruchbreite_steht_als_Token` hält beide Werte
gegeneinander. Die Breite der Mittelspalte ist `--epos-zweispalten-mitte` (10 rem; im
Bestand 63 px bei `Form_Gebaeude`, 88 px bei `Form_Heizkessel` — hier etwas mehr, weil
die Knöpfe seit Befund W9‑B‑3 ihre Aufgabe im Klartext tragen).

**Texte.** Neu in beiden Sprachkatalogen und im `Resource.Designer.cs`:
`AUSWAHL_BTN_UEBERNEHMEN`, `AUSWAHL_BTN_UEBERNEHMEN_HINWEIS`, `AUSWAHL_BTN_ENTFERNEN`,
`AUSWAHL_BTN_ENTFERNEN_HINWEIS`, `AUSWAHL_GRP_PFEILE` (der Name der Knopfgruppe für die
Sprachausgabe). Aus `GEB_BTN_UEBERNEHMEN` / `GEB_BTN_ENTFERNEN` sind die Zeichen
**▲/▼ entfernt**; die acht nebeneinander stehenden Dialoge nehmen weiter
`HZK_TIP_HINZU` / `HZK_TIP_ENTFERNEN` — jetzt als **Beschriftung** statt nur als
Kurztext.

**Tastaturweg und Sprachausgabe.** Die drei Bereiche stehen in der Reihenfolge links –
Mitte – rechts im Markup; der Tabulator läuft damit von der Projektliste über die zwei
Knöpfe in den Katalog. Jede Spalte ist eine `role="group"` mit ihrer Überschrift als
`aria-label`, die Knopfgruppe ebenso.

### Vorbild → Umsetzung je Dialog

| Dialog | Vorbild (Geometrie im `.resx`) | Umsetzung |
|---|---|---|
| `Strom/StromganglinieDialog` | `Form_Stromganglinie`: links die Ganglinien des Projekts, rechts der Katalog, dazwischen `btn_Hinzufuegen` „◀" und `btn_Entfernen` „▶", „Bearbeiten…" unter der Katalogliste | Stand schon nebeneinander (`epos-auswahlpaar`), jetzt `Zweispaltenauswahl`. Die zwei Parameter `HinzufuegenText`/`EntfernenText` **entfallen**: Ihre Ressourcen `STROMGL_BTN_HINZUFUEGEN` und `STROMGL_BTN_ENTFERNEN` trugen nur „◀" und „▶" — ein Zeichen, das nicht wissen kann, wie die Listen gerade stehen. Die Beschriftung kommt jetzt aus `LabelHinzu`/`LabelEntfernen` (`HZK_TIP_HINZU`/`HZK_TIP_ENTFERNEN`), die bis hierher nur Kurztexte waren |

**Die zwei Ressourcenschlüssel bleiben im Katalog stehen**, ohne Nutzer — ein Eintrag
weniger in einer 12 000-zeiligen `.resx` wiegt den Merge-Aufwand nicht auf. Der
Kopfkommentar von `StromganglinieDialog.razor` nennt sie als erledigt.

**Nicht betroffen ist `StromganglinieAdminDialog`**: eine Liste plus Detailblock, keine
Projekt↔DB-Auswahl.

### Wachen

`EPOS.UI.Tests/Bausteine/ZweispaltenauswahlTests` (17 Fälle) prüft drei Ebenen: den
**Baustein** (Reihenfolge der drei Bereiche = Tastaturweg, `aria`-Beschriftungen, beide
Zeichen je Knopf mit `aria-hidden`, Klartext, Kurztext, Sperrzustände, Rückrufe,
`NurRechts`), die **Regel im Stilblatt** (nebeneinander ist die Vorgabe, kein
`flex-wrap`, Token gegen Medienabfrage, je Anordnung genau ein Zeichen) und den
**Bestand** (alle elf Projekt↔DB-Dialoge nehmen den Baustein; keine Komponente baut die
Pfeilspalte noch selbst). Eine bunit-Probe sieht eine Stilregel nicht — Lehre W6‑B‑1.

### Abnahmepunkte A‑#76

1. **Breit** (Fenster ≥ 900 px): Projektliste **links**, Katalog **rechts**, die zwei
   Knöpfe in einer schmalen Spalte dazwischen; die Zeichen sind ◀ (übernehmen) und ▶
   (entfernen).
2. **Schmal** (Fenster < 900 px, iPad hochkant): Projektliste **oben**, Knöpfe
   darunter nebeneinander, Katalog **unten**; die Zeichen sind ▲ und ▼.
3. **Listen begrenzt**: Beide Listen rollen in ihrem Rahmen, der Spaltenkopf bleibt
   stehen; Filter, Detailblock und Schlussleiste bleiben erreichbar, ohne die ganze
   Seite zu rollen.
4. **Knöpfe**: Beide tragen ihren Satz im Klartext — auf Deutsch **und** auf Englisch —
   und einen Kurztext, der die Herkunft der Zeile nennt. Jeder bleibt gesperrt, solange
   in der jeweils anderen Liste nichts markiert ist.


## Befund W12‑B‑1 (05.09.2026) — die Knopfleiste bemisst sich am Text und bricht um

**Der Befund.** Im Bildschirmfoto der Windows-Abnahme (Dialog „Standard Stromprofil",
Full HD bei 125–150 % Skalierung) meldet der Anwender: *„Beschriftung der Buttons nicht
zur Umrandung passen."* Unter der rechten Liste „Datenbank Strombedarf" stehen **vier**
Knöpfe in einer Reihe — „Stromverbraucher ändern…", „Stromverbraucher neu…",
„Stromverbraucher löschen", „Typ in DB ändern…". Sie waren schmaler als ihre
Beschriftung: „Stromverbraucher" ragte in den Nachbarknopf, „Stromverbrauche neu…" war
abgeschnitten, die Umrandung lag mitten im Wort.

**Die Ursache — zwei Zeilen im Hausblatt, nicht der Baustein.** `.epos-leiste`
(`EPOS.UI/wwwroot/epos-ui.css`, vor der Behebung Zeile 378, jetzt 392) war ein
`display: flex` **ohne** `flex-wrap`, also eine Reihe, die nicht umbricht;
`.epos-knopf` (vorher 396, jetzt 423) hatte `min-width: 88px` und die Vorgabe
`flex-shrink: 1`. In der rechten Spalte der `Zweispaltenauswahl` — die sich die
Dialogbreite mit der Projektliste teilt — forderten die vier Knöpfe zusammen mehr Platz,
als die Zeile hatte. Sie schrumpften deshalb bis auf ihre 88 px, während
„Stromverbraucher" als **unteilbares Wort** breiter blieb als der Innenraum: Der Text lief
über den Rahmen. Der Baustein selbst war unbeteiligt — er reicht `Rechts` nur durch; die
Knopfleiste setzen die Aufrufer.

**Die Behebung — eine Stelle für das ganze Haus.** Kein Dialog wurde angefasst.
`.epos-leiste` bekommt `flex-wrap: wrap` (der Abstand liegt schon auf `gap` und trägt
damit beide Achsen), `.epos-knopf` bekommt `flex: 0 1 auto` (der Knopf bemisst sich an
seiner Beschriftung, `min-width` bleibt ein **Mindest**maß), `white-space: normal` mit
`overflow-wrap: break-word` (reicht der Platz auch nach dem Umbruch nicht, bricht der
Text **im** Knopf um) und `padding: 4px 12px` statt `0 12px` (hält eine zweite Zeile vom
Rahmen frei; bei einzeiliger Beschriftung ändert sich nichts, weil die Höhe weiter aus
`min-height: var(--epos-touchziel)` kommt). **Kein `overflow: hidden`** — Abschneiden
wäre derselbe Fehler in still.

**Reichweite.** `.epos-leiste` ist die eine Knopfleiste des Hauses (110 Fundstellen in
`EPOS.UI`). Direkt betroffen waren die Katalogleisten der elf Projekt↔DB-Dialoge, am
stärksten die mit vier Knöpfen (`BedarfsProfileDialog` — der gemeldete „Standard
Stromprofil" — und `GebaeudeDialog`), danach die mit drei (`BhkwDialog`,
`HeizkesselDialog`, `SolarkollektorenDialog`) und die mit zwei
(`PufferspeicherDialog`, `PhotovoltaikDialog`, `WaermebedarfExternDialog`). Mit
behoben sind ohne eigenes Zutun die Leisten der Katalogverwaltungen am
`Katalograhmen` (`WaermebedarfAdminDialog`, `BedarfAdminDialog`, `KlimadatenDialog`,
`SolarganglinieAdminDialog`, `KatalogBrowserDialog`, `ModulKatalogDialog`) und jede
weitere Leiste des Hauses — dieselbe Klemme hätte dort bei langer Beschriftung oder
schmalem Fenster genauso zugeschlagen.

**Wachen.** Drei neue Fälle in `EPOS.UI.Tests/Bausteine/ZweispaltenauswahlTests`
(14 → 17): `Die_Knopfleiste_unter_der_Katalogliste_traegt_die_Leistenklasse` (Markup —
die vier Knöpfe eines Aufrufers landen in der rechten Spalte des Bausteins und stehen
dort in `.epos-leiste`, ohne Inline-Stil),
`Jeder_Dialog_setzt_seine_Katalogknoepfe_in_eine_epos_leiste` (Bestand — kein Dialog
erfindet eine zweite Knopfleiste, die den Umbruch nicht mitbekäme) und
`Die_Knopfleiste_bricht_um_statt_die_Beschriftung_abzuschneiden` (Stilblatt —
`flex-wrap: wrap`, `flex: 0 1 auto`, `white-space: normal`, `overflow-wrap`, und
ausdrücklich **kein** `white-space: nowrap`, **kein** `overflow: hidden`, **keine**
feste `width`). Eine bunit-Probe sieht eine Stilregel nicht — Lehre W6‑B‑1. Die
Gegenprobe wurde gezogen: Mit zurückgedrehtem Stilblatt fällt der dritte Fall rot aus.

### Abnahmepunkte A‑W12‑B‑1

1. **Der gemeldete Dialog.** „Standard Stromprofil" bei 125 % und 150 % Skalierung
   öffnen: Die vier Knöpfe unter „Datenbank Strombedarf" sind so breit wie ihre
   Beschriftung; kein Text berührt oder überschreitet einen Rahmen, keiner ist
   abgeschnitten.
2. **Schmales Fenster.** Fenster unter 900 CSS‑px ziehen (die Zweispaltenauswahl bricht
   dann untereinander um): Die Knopfleiste bricht in zwei Zeilen um, der Abstand
   zwischen den Zeilen ist derselbe wie zwischen den Knöpfen. Reicht es auch dann
   nicht, bricht der Text **innerhalb** des Knopfes um — der Knopf wird höher, nicht
   der Text kürzer.
3. **Die anderen Dialoge.** Gebäude, Heizkessel, BHKW und Solarkollektoren zeigen
   dasselbe Bild; die Knöpfe stehen nicht mehr auf gleicher Breite, sondern jeder so
   breit, wie sein Text ist.
4. **Nichts sonst hat sich bewegt.** Einzeilige Knöpfe — „OK", „Abbrechen", die
   Startseitenfußleiste — stehen unverändert in ihrer bisherigen Höhe und Breite.

## Windows-Abnahme 05.09.2026 — Stromganglinien: Import, Löschen, Speichern unter (W12‑E‑1)

**Der Wortlaut des Anwenders** (Bildschirmfoto 4, Dialog „Stromganglinien"): „csv-Datei
Stromlastgang importieren (mit Info zum Format) fehlt. Ebenfalls fehlt löschen und Speichern
unter."

**Was das Vorbild hatte — und was nicht.** Die Feldkarte von `Form_Stromganglinie`
(`git show 58d02f6^:…/Form_Stromganglinie.designer.cs`, 678 × 345, 7 Kartenzeilen) führt
`listBox_Auswahl`, `btn_Hinzufuegen` („◀"), `btn_Entfernen` („▶"), `listBox_Extern`,
`btn_OK`, `btn_Abbrechen` und `btn_Bearbeiten` („Bearbeiten…") — **keinen Import, kein
Löschen, kein Speichern unter**. Der Port hat also nichts vergessen: Der Wunsch ist eine
ERWEITERUNG, und zwei ihrer drei Teile gab es im Bestand nur eine Maske weiter.
`Form_Stromganglinie_Admin` (664 × 316, Feldkarte aus `1c8c7c3^`) trug „Datei Einlesen…"
und „Ganglinie Löschen"; **„Speichern unter" gab es im ganzen Bestand nicht** — der
Katalogeintrag „Lastgang_Strom_NestleLB-05-2010-05-2011 - Kopie" der Testdatenbank ist ein
zweiter Import unter anderem Dateinamen, kein Kopierweg. Der einzige Kopierweg der
Stromganglinien war `CopyGanglinieToProjekt` (STAMM → Projekt) und taugt dafür nicht.

**Wo der Import bisher lag.** Ausschließlich hinter „Bearbeiten…" → `StromganglinieAdminDialog`
(dort „Datei Einlesen…" im Block „Ganglinie aus Datei in Datenbank Einlesen", mit der
Rasterliste davor). Der Weg dorthin war zweistufig und unbeschriftet; wer im Dialog
„Stromganglinien" stand, sah keinen Hinweis darauf, dass hinter „Bearbeiten…" der Import liegt.

### Die Umsetzung

**Ein Importweg, zwei Wirte.** Die Kette liegt seit W12.0d im Kern
(`GanglinienImportAblauf.MitAblage`, bitgleich geprüft). Neu ist der Baustein
**`EPOS.UI/Dialoge/Strom/GanglinienImportLauf.razor`** (231 Z.): die OBERFLÄCHENSEITE der
Kette — die drei Überlagerungen (Optionen, Protokoll, Konflikte), je mit ihrer
`TaskCompletionSource`, dazu `Starten(pfad, raster)`, `EtwasOffen` für die Esc-Staffelung und
`StufeZu(ergebnis)` für die Bannerstufe. `StromganglinieAdminDialog` hängt ihn seither ein
statt die drei Überlagerungen selbst zu führen (422 → 303 Z.), `StromganglinieDialog`
ebenso. **Es gibt damit keinen zweiten Importweg** — dieselbe Kette, dieselben
Zwischendialoge, dieselben Delegaten aus `StromganglinieAdminHuelle`.

**Die Knopfleiste unter der Katalogliste** trägt jetzt vier statt einem Knopf, in dieser
Reihenfolge: **„CSV-Datei importieren…" · „Speichern unter…" · „Löschen" · „Bearbeiten…"**.
Sie ist die `epos-leiste` aus W12‑B‑1 und bemisst sich am Text; auf schmalem Schirm bricht
sie um. **Kein Delegat, kein Knopf** — fehlt der jeweilige Rückruf, ist der Knopf gar nicht
da; ohne jede Gabe zeichnet der Dialog weiterhin nur die zwei Listen (Regel W16b‑B‑1).

**Der Formathinweis** steht als leise Zeile unter der Leiste, mit einem `InfoKnopf` daneben,
der denselben Wortlaut als Kurztext trägt und auf die Wikiseite „Strombedarf" führt (neue
Zeile `Form_Stromganglinie.btn_Help_Import` in `help_mapping.txt`). Er nennt genau das, was
die Kette wirklich auswertet — Dateiarten, 8 760 bzw. 35 040 Werte in Zeitfolge ab dem
1. Januar, die vier zugelassenen Feldtrennzeichen und den einspaltigen Fall, die erkannte
Kopfzeile, Komma **oder** Punkt als Dezimaltrennzeichen ohne Tausendertrennung, kW oder kWh je
Intervall, die zulässige aber nicht nötige Zeitstempelspalte und den Bezeichner = Dateiname
ohne Erweiterung (`STROMGL_HINWEIS_FORMAT`, de/en). Neu im Stilblatt ist dafür
`.epos-formathinweis` (Flex, Text nimmt die Breite, Knopf rechts daneben).

**Der Import selbst** holt den Pfad über den erwarteten `DateiWaehlen`-Delegaten und
`await`et ihn (W13‑B‑1: die Hülle ruft `Dienste.Datei.DateiOeffnenAsync`, das Fenster geht
eine geposteten Nachricht später auf) und ruft dann `Starten(pfad, GanglinienRaster.Unbekannt)`
— die Maske gibt **keine** Rastervorgabe: Die Kette erkennt es selbst, und der Optionendialog
lässt es übersteuern. Ein PFADFELD steht hier bewusst nicht: In einer Knopfleiste unter der
Liste hätte es nichts anzuzeigen, der Pfad ist nach dem Lauf ohnehin wieder leer.

**Löschen** prüft ZWEI Sperren, bevor die Rückfrage kommt, und beide MELDEN ihren Grund
(Warnbanner) statt still nichts zu tun: (1) die **Projektzuordnung** — neu im Kern als
`StromganglinieStammCtrl.HatProjektzuordnung` (`SELECT COUNT(*) FROM Z_ProjektStromganglinie
WHERE Bezeichner = ?`, Muster der Solarganglinie W14b); (2) das **Auslieferungskennzeichen**
`ReadOnly`, dessen Grund zusätzlich als `title` am Knopf hängt — er ist synchron bekannt
(Staffelung W16b‑E‑6: der Grund am Bedienelement, das Banner erst nach dem Versuch). Erst
danach steht die `Rueckfrage`; „Ja" löscht, lädt den Katalog neu und meldet.

**Speichern unter** ist die Kopie unter neuem Namen. Der `NamensDialog` schlägt
„&lt;Name&gt; - Kopie" vor — dieselbe Schreibweise, die der Bestand schon führt — und prüft
die Dublette **VOR** dem Einfügen gegen den geladenen Katalog (`Pruefung`, hält den Dialog
offen und sagt, warum). Im Kern legt **`StromganglinieStammCtrl.KopiereStamm(quelle, ziel)`**
Kopf und Werte in **einer** Transaktion an, in Stamm-Reihenfolge (`ORDER BY ID`), immer mit
`ReadOnly = false` — eine Kopie ist Anwenderbestand, auch die eines Auslieferungssatzes. Auch
dort steht die Dublettenprüfung vor dem `INSERT`: Ein vergebener Name ergibt `0` und keine
Zeile, kein SQLite-UNIQUE-Fehler erreicht den Anwender. Der Schreibsatz selbst ist mit
`ImportGanglinie` **geteilt** (neues privates `EinfuegenStamm(v, name, raster, werte)`) — zwei
Fassungen desselben `INSERT` liefen beim ersten Schemawechsel auseinander.

**Zwei Nebenbefunde, mit behoben.** `StromganglinieStammCtrl.ReadAll` warf die Spalte
`ReadOnly` weg, obwohl `SELECT *` sie ohnehin mitbringt: Die Verwaltungshülle fragte sie
deshalb je Katalogzeile einzeln nach (N+1), und die Zuordnungshülle gab schlicht `false`
weiter — der Projektdialog konnte einen Auslieferungssatz gar nicht erkennen. `ReadAll` liest
sie jetzt (`StromganglinieModel.m_bReadOnly`), beide Hüllen nehmen sie von dort, und
`StromganglinieHuelle.KatalogLesen` ruft `StromganglinieAdminHuelle.KatalogLesen` statt die
Schleife ein zweites Mal zu schreiben.

### Wachen

**Kern** — `EPOS.Kern.Tests/StromganglinieKatalogTests` (10 Fälle, neu): `ReadAll` trägt
dasselbe `ReadOnly` wie die Einzelabfrage; `HatProjektzuordnung` trennt zugeordnete von freien
Ganglinien und zählt dieselben Zeilen wie die Tabelle; ein Auslieferungssatz wird nicht
gelöscht; eine freie Ganglinie fällt samt ihren Werten (keine Datenwaisen); die Kopie trägt
denselben Zeitschritt und dieselben Werte unter neuem Namen; die Kopie eines
Auslieferungssatzes ist frei; ein vergebener Name wird abgewiesen statt zu werfen (auch
getrimmt); ohne Quelle oder ohne Namen entsteht keine Kopie; `Exists` prüft den ganzen Namen
und nicht seinen Anfang (der Fehler, der beim Solarkatalog Befund W14‑B70 war). Der
IMPORTweg selbst braucht keine neue Wache — es ist dieselbe Kette, und die steht seit W12 in
`GanglinienImportAblaufTests` und `GanglinienProbenTests`.

**Oberfläche** — `EPOS.UI.Tests/Dialoge/StromganglinieDialogTests` (14 → 30 Fälle): die vier
Knöpfe in ihrer Reihenfolge; „kein Delegat, kein Knopf" (auch der Halbfall Dateiwähler ohne
Kette); der Formathinweis nennt die sechs Angaben und der Infoknopf trägt denselben Wortlaut;
der Dateiwähler DARF warten und die Kette läuft danach mit `Raster.Unbekannt`; ein
abgebrochener Wähler liest nichts; Löschen und Speichern unter sind ohne Auswahl gesperrt;
zugeordnet und schreibgeschützt melden ihren Grund und lassen die Rückfrage gar nicht erst
kommen; Rückfrage mit „Ja"/„Nein"; der Kopiervorschlag „… - Kopie"; ein vergebener Name hält
den Namensdialog offen; ein freier Name legt die Kopie an; eine gescheiterte Kopie meldet sich
als Fehlerbanner; Abbrechen kopiert nichts; Esc meldet nichts, solange Rückfrage oder
Namensdialog stehen. Die Klasse pinnt die Kultur seither vollständig
(`DeutscheOberflaeche()`), weil sie jetzt formatierte Meldungen prüft.

### Nachweise

| Prüfung | Ergebnis |
|---|---|
| `dotnet test EPOS.Kern.Tests -c Release` | 1 084 grün (1 074 + 10 neue), auch unter `LANG=en_US.UTF-8` |
| `dotnet test EPOS.UI.Tests -c Release` | 2 500 grün (2 484 + 16 neue), auch unter `LANG=en_US.UTF-8` |
| `SqlDialektPruefer` | 1 204 SQL-Texte, **0 Fundstellen** |
| Kern-Wächter (`Program.*`, Plattform) | beide leer |
| Referenzlauf | nicht nötig — der Rechenweg ist unberührt |

### Abnahmepunkte A‑W12‑E‑1

1. **Die vier Knöpfe.** Startseite → Kachel „Stromlastgang" (oder Assistentenseite 6):
   Unter „Stromganglinie aus DB" stehen „CSV-Datei importieren…", „Speichern unter…",
   „Löschen" und „Bearbeiten…", jeder so breit wie sein Text. Darunter steht der
   Formathinweis mit dem Fragezeichenknopf rechts daneben; der Knopf öffnet die Wikiseite
   „Strombedarf", sein Tooltip zeigt denselben Hinweistext.
2. **Import mit einer sauberen Datei.** Eine CSV mit 8 760 Zeilen, ein Wert je Zeile, Komma
   als Dezimaltrennzeichen, ohne Kopfzeile, ohne Trennzeichen (einspaltig) — z. B.
   `123,4` / `118,9` / … Der Dateiwähler geht auf (kein Absturz, W13‑B‑1), der Optionendialog
   zeigt „kein Trennzeichen / Dezimaltrenner Komma / keine Kopfzeile / Spalte 1", das
   Protokoll bleibt weg (sauberer Lauf), und die neue Ganglinie steht unter dem Dateinamen
   ohne Erweiterung in der rechten Liste. Grünes Banner mit Name, Wertezahl und Zeitschritt.
3. **Import mit einer Datei, die es schon gibt.** Dieselbe Datei ein zweites Mal wählen: Der
   Konfliktdialog kommt (Auslassen / Überschreiben / Umbenennen) — genau derselbe wie hinter
   „Bearbeiten…"; es gibt keinen zweiten Import.
4. **Import mit Zeitstempel und Kopfzeile.** Eine CSV `Zeit;Leistung` mit Semikolon,
   Kopfzeile und 35 040 Viertelstundenzeilen (`01.01.2024 00:00;123.4`): Der Optionendialog
   erkennt Semikolon, Kopfzeile „ja", Zeitspalte 1, Wertspalte 2 und Punkt als
   Dezimaltrennzeichen; der Zeitschritt steht danach auf 4.
5. **Löschen — zugeordnet.** Eine Ganglinie wählen, die im Projekt steht („In das Projekt
   übernehmen" und speichern, dann Dialog neu öffnen): „Löschen" meldet „Es existiert eine
   Projektzuordnung, Löschen nicht möglich!" — **keine** Rückfrage, nichts gelöscht.
6. **Löschen — Auslieferung.** Ein Katalogeintrag mit `ReadOnly` (Auslieferungsbestand):
   Der Knopf zeigt den Grund schon als Tooltip; der Klick meldet „…schreibgeschützt
   (ReadOnly)…", ohne Rückfrage.
7. **Löschen — frei.** Eine freie, nicht zugeordnete Ganglinie: Rückfrage „Die Stromganglinie
   „X" wird gelöscht. Fortfahren?" → „Ja"; die Zeile verschwindet aus der Liste, grünes
   Banner. „Nein" lässt alles stehen.
8. **Speichern unter.** Eine Ganglinie wählen → „Speichern unter…": Der Name steht mit
   „ - Kopie" hinten vorbelegt. **Erst** einen Namen tippen, den es schon gibt: Der Dialog
   bleibt offen und sagt „…ist bereits in der Datenbank…". Dann den Vorschlag nehmen: Die
   Kopie erscheint in der Liste, grünes Banner mit beiden Namen. Die Kopie danach auswählen
   und „Löschen" — sie ist frei, auch wenn die Quelle Auslieferung war.
9. **Die Werte sind wirklich mitkopiert.** Kopie ins Projekt übernehmen, Simulation rechnen:
   Sie ergibt dasselbe wie mit der Quelle (gleicher Zeitschritt, gleiche Reihe).
10. **Nichts sonst hat sich bewegt.** „Bearbeiten…" zeigt unverändert die Verwaltung als
    Überlagerung; „In das Projekt übernehmen"/„Aus dem Projekt entfernen", OK und Abbrechen
    verhalten sich wie bisher. Esc schließt immer nur die oberste Ebene.

**Beispiel-CSV (Abnahmepunkt 2), die kürzeste Form, die die Kette annimmt:**

```
123,4
118,9
…
(8 760 Zeilen insgesamt, ein Wert je Zeile, in kW, Stunde 1 = 1. Januar 00:00–01:00)
```

Dateiname `Werk-Nord-2024.csv` → Bezeichner der Ganglinie `Werk-Nord-2024`.

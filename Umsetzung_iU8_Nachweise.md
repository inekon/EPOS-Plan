# Nachweisliste iU8 — der erste Blazor-Dialog, Abnahme auf Windows

**Stand 03.09.2026 · Branch `ios_migration` · Strang A `8574911`..`8f5a28e` mit `45a21dc`,
`f5fb05c` (Basis `18f515f`) · Strang B `4369fdb`..`eafbc1f` mit `eff82aa`, `e3d1e5b`
(Basis `c477523`) · Strang C `479fcf9`..`0af7ca7`, `4aa6b15` (Basis `f5fb05c`)**

Paket iU8 des [`Umsetzungskonzept_iOS_EPOS-Plan.md`](Umsetzungskonzept_iOS_EPOS-Plan.md) (§ 4) ist
umgesetzt: `Form_Kosten` öffnet „Energieträger anlegen" als Razor-Komponente aus `EPOS.UI`, die
WinForms-Fassung `Form_Kosten_Auswahl` ist gelöscht. Das ist der **Stichtag iZ5**.

**Alle bisherigen Nachweise wurden auf Linux geführt** — SDK 10.0.400, kein Visual Studio, keine
Datenbank, **keine WebView2**. Auf Linux lässt sich beweisen, dass alles übersetzt, dass die
Komponente sich richtig verhält (bunit) und dass die Veröffentlichung die richtigen Dateien
enthält. Nicht beweisen lässt sich, wie der Dialog **aussieht und sich anfühlt** — genau das steht
hier als abhakbare Liste.

Ergebnis der Linux-Nachweise in einem Satz: **`dotnet build WP-Plan.sln -c Release
-p:Platform=x64` übersetzt mit 0 Fehlern und 34 Warnungen (vorher 36, keine neuen Codes),
`dotnet test WP-Plan.Kern.slnf` meldet 886/886, und der Referenzlauf 1030/1007/1017 ist gegen
`2026-08-30_B3-Kaskade` byte-gleich.**

> **Ohne WebView2-Laufzeit ist nichts davon prüfbar.** Auf Windows 11 ist sie da. Auf einem
> Windows-10- oder LTSC-Rechner zuerst prüfen:
> `reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv`
> — steht dort nichts oder `0.0.0.0`, fehlt sie.

## Vorbedingungen, bevor die Liste beginnt

Zwei Dinge liegen **nicht** im Repo und müssen vom Anwender bereitgestellt werden. Ohne sie
brechen die Punkte weiter unten nicht sinnvoll ab, sondern gar nicht erst an.

- [ ] **`MicrosoftEdgeWebview2Setup.exe` in die Repowurzel legen** — der Online-Bootstrapper von
      <https://go.microsoft.com/fwlink/p/?LinkId=2124703> (~2 MB). `Setup\build-setup.ps1` kopiert
      ihn von dort nach `Setup\Voraussetzungen\`; **fehlt er, bricht der Setup-Bau ab.** Die
      Datei wird **nie eingecheckt**: `.gitignore` schließt `/MicrosoftEdgeWebview2Setup.exe`
      seit `e3d1e5b` aus, damit `GitHub_Sync.bat` sie mit `git add -A` nicht mitnimmt (S9 des
      Setup-Konzepts ist damit erledigt). Offen bleibt allein die Grundsatzfrage, ob der
      Bootstrapper der richtige Weg ist — **iF20** / S10.
- [ ] **Eine Datenbank mit echten Projektdaten** (`Kenndaten.sqlite`) für alles, was der Dialog
      schreibt und liest, und ein `.accdb`-Bestand, falls die Erststart-Migration mitgeprüft
      werden soll.

---

## Nachweise je Commit

### `4369fdb` — iU8-6: Razor-SDK und die wiederverwendbare Blazor-Hülle

**Nachweis hier:** `dotnet build WP-Plan.sln -c Release -p:Platform=x64 --no-incremental` → 0
Fehler, 36 Warnungen, Warnungscodes identisch zur Basis (60 WFO1000, 4 CS0109, 4 CS0108, 2
WFO0003, 2 CA2255); kein CS8632. Die Gegenprobe mit `Microsoft.NET.Sdk` liefert **kein `wwwroot`**
im Veröffentlichungsordner — deshalb der SDK-Wechsel.

**Nachweis Windows:**

- [ ] **Visual Studio 2026 öffnet die Projektmappe** und lädt `WindowsFormsApplication1` unter dem
      Razor-SDK ohne Meldung
- [ ] **Der WinForms-Designer öffnet ein beliebiges Formular** (z. B. `Views/Kosten/Form_Kosten.cs`)
      und zeigt die Entwurfsansicht — das ist das Rest-Risiko G9 des Plans: das offizielle
      WinForms-Blazor-Template geht denselben Weg, geprüft ist es hier noch nicht
- [ ] `dotnet build ..\WP-Plan.sln -c Debug -p:Platform=x64` auf der Kommandozeile, 0 Fehler
- [ ] Nach `dotnet publish`: `wwwroot\index.html`, `wwwroot\EPOS_Plan.styles.css`,
      `wwwroot\_content\EPOS.UI\`, `wwwroot\_framework\blazor.webview.js` liegen im Ausgabeordner

---

### `b12e910` — iU8-7: Hilfe-Brücke `HelpExtender.ZielFuer` und `WindowsHilfeDienst`

**Nachweis hier:** Build 0 Fehler, Warnungscodes unverändert. Die Auflösung selbst ist ohne
laufende Anwendung nicht prüfbar: Sie braucht den einen `HelpExtender`, den
`HilfeAutomatik.Starten` anlegt, und einen geladenen Wiki-Katalog.

**Nachweis Windows:**

- [ ] Der **Infoknopf im Blazor-Dialog** öffnet das angeheftete Hilfefenster mit der Kosten-Seite —
      derselbe Text wie beim Infoknopf von `Form_Kosten`
- [ ] **Ohne Internet / mit leerem Katalog** bleibt der Knopf sichtbar und folgenlos; kein Absturz,
      kein leeres Popup
- [ ] Bei fehlendem Popup öffnet der Rückfall die Wikiseite im Browser
- [ ] Auf **Englisch** (`HKCU\Software\wp-plan`, Wert `Language` = 1) zeigt das Popup die
      englische Seite, sofern der Katalog sie führt

---

### `1e2a44c` — iU8-8b: Ressourcen `KAUSW_*`/`ALLG_BTN_*` und `EnergietraegerVarianteCtrl`

**Nachweis hier:** `EPOS.Kern` allein 0 Fehler, 2 Warnungen (unverändert). Die drei Abfragen sind
zeichengleich aus `Form_Kosten_Auswahl` übernommen; ohne Datenbank ist an ihnen nichts zu prüfen —
deshalb kein Test in `EPOS.Kern.Tests`.

**Nachweis Windows:**

- [ ] Die Auswahlliste zeigt **dieselben Energieträger in derselben Reihenfolge** wie vorher
      (`Tab_Brennstoff_Stamm`, sortiert nach `Bezeichner`)
- [ ] Ein neu angelegter Träger trägt in `energy_carrier` **dieselben Werte** wie ein vor der
      Umstellung angelegter: `group_code`, `pricing_model`, `billing_unit`, `hi_kwh_per_unit`,
      `hs_kwh_per_unit`; in `energy_project_settings` dieselbe `ID_Umrechnung`
- [ ] Visual Studio hat `Resource.Designer.cs` **nicht** neu erzeugt und dabei die von Hand
      eingefügten sieben Eigenschaften verdoppelt (CS0102 — der bekannte Fallstrick)

---

### `92380ea` — iU8-9: Stichtag iZ5, `Form_Kosten` öffnet die Komponente

**Nachweis hier:** Build 0 Fehler / 34 Warnungen; 886 Tests grün; Referenzlauf 1030/1007/1017
**GESAMT PASS** (815 043 Werte), `diff -rq` nur `protokoll.txt`; `git grep Form_Kosten_Auswahl`
findet nur noch Kommentare, die Zeile in `help_mapping.txt`, den Hilfeschlüssel und den
KI-Kontexteintrag; Publish-Probe vollständig.

**Nachweis Windows — das ist die eigentliche Abnahme von iZ5:**

*Grundfunktion*

- [ ] `Form_Kosten` → Reiter Energie → **„Energieträger anlegen"** öffnet den Dialog, mittig über
      dem Elternfenster, feste Größe, ohne Minimier-, Maximier- und Taskleistenknopf
- [ ] **Kein weißes Aufblitzen** beim Öffnen (die Hülle steht auf der Themafläche `#f5f4ef`, bis
      die WebView2 aufgebaut ist)
- [ ] Die Auswahl eines Energieträgers **belegt den Variantennamen vor** — wie vorher
      `cmbBrennstoffArt_SelectedIndexChanged`
- [ ] **OK mit leerem Namen**: der Dialog bleibt offen und zeigt das Warnbanner „Bitte einen
      Variantennamen (Code) eingeben." (vorher eine MessageBox)
- [ ] **OK mit Namen**: Dialog schließt, der Träger wird angelegt, die Meldung „Energieträgervariante
      erfolgreich angelegt." erscheint, und der neue Träger ist in `listBox_Energieträger`
      **markiert**
- [ ] **Zweites Anlegen mit demselben Namen** meldet „… ist diesem Projekt bereits zugeordnet."
- [ ] **Abbrechen** legt nichts an und ändert die Auswahl in der Liste nicht

*Tastatur (M2, Risiko G2 — `AcceptButton`/`CancelButton` sehen keine Tasten aus der WebView2)*

- [ ] **Enter** schließt mit OK, **Esc** bricht ab
- [ ] **Tab** wandert durch Auswahlliste, Textfeld, Abbrechen, OK und bleibt im Dialog
- [ ] Der Erstfokus liegt auf dem Dialog; der erste Tabulatorschritt führt in die Auswahlliste

*Bedienung mit dem Finger (M2)*

- [ ] Auf einem **Touch-Gerät oder Touch-Monitor**: Auswahlliste, Textfeld und beide Knöpfe sind
      mit dem Finger sicher zu treffen (Mindestziel 44 px)
- [ ] Die Auswahlliste öffnet die fingerfreundliche Edge-Liste
- [ ] Die **Bildschirmtastatur** erscheint beim Tippen ins Textfeld und verdeckt es nicht

*Sprache*

- [ ] **Deutsch**: Titel „Energieträger Variante", Beschriftungen „Energieträger:" und
      „Energieträger Varianten Bezeichnung:", Knöpfe „OK"/„Abbrechen" — wortgleich zu vorher
- [ ] **Englisch** (`HKCU\Software\wp-plan\Language` = 1, Neustart): „Energy carrier variant",
      „Energy carrier:", „Energy carrier variant name:", „OK"/„Cancel"
- [ ] Das Kontextmenü der WebView2 (Rechtsklick) ist in derselben Sprache

*Darstellung*

- [ ] **Hochkontrast-Design** (Windows-Einstellung „Kontrastdesigns"): Alle Texte lesbar, das
      Warnbanner bleibt als Warnung erkennbar, kein weißer Text auf weißem Grund
      (`@media (forced-colors: active)` in `epos-ui.css`)
- [ ] **125 %** Skalierung: Der Dialoginhalt ist **scharf**, nicht bitmapskaliert — dann greift die
      DPI-Insel aus `BlazorDialogForm`
- [ ] **150 %** Skalierung: dito; Fenstergröße und Elternfenster passen zusammen
- [ ] **Zweiter Monitor mit anderer Skalierung**: der Dialog folgt beim Verschieben
- [ ] Befund notieren, falls die Insel **nicht** greift (Windows vor 10/1803 — dann bitmapskaliert
      wie der Rest der Anwendung; das ist zulässig, aber es soll dokumentiert sein)

*Ablage*

- [ ] Nach dem ersten Öffnen existiert **`%LOCALAPPDATA%\WP-Plan\WebView2`** — und **kein**
      `EPOS_Plan.exe.WebView2` neben der EXE
- [ ] Der Dialog öffnet auch als **Standardbenutzer** bei maschinenweiter Installation unter
      `C:\Program Files` (das ist der Fall, für den `UserDataFolder` gesetzt ist)
- [ ] Der Dialog öffnet auf einem **zweiten Windows-Konto** desselben Rechners

*Rechenweg*

- [ ] Referenzlauf **1030, 1007, 1017** auf Windows gegen `Referenzlaeufe/2026-08-30_B3-Kaskade`:
      `vergleich` **GESAMT PASS**, `diff -rq` nur `protokoll.txt`

---

### `eafbc1f` — iU8-10: WebView2 als zweite Setup-Voraussetzung

**Nachweis hier:** Kein Inno-Compiler in der Linux-Umgebung; geprüft wurde durch Sichtprüfung gegen
die vorhandenen ACE-Abschnitte — Fortsetzungszeilen, Reihenfolge von `Check`/`Flags`/`AfterInstall`,
beide Sprachen je `CustomMessage`, kein `}` in einem Pascal-Kommentar, kein `^` in der PowerShell.

**Nachweis Windows:**

- [ ] `MicrosoftEdgeWebview2Setup.exe` liegt in der Repowurzel (siehe **Vorbedingungen** oben;
      offener Punkt S8 des Setup-Konzepts)
- [ ] `Setup\build-setup.ps1` läuft durch, kopiert den Bootstrapper nach
      `Setup\Voraussetzungen\` und übersetzt ohne ISCC-Fehler
- [ ] Das erzeugte Setup enthält `wwwroot\_content\EPOS.UI\` (Prüfung z. B. mit `innounp` oder nach
      der Installation im Programmordner)
- [ ] **Windows Sandbox ohne WebView2**: Setup installiert die Laufzeit still, die Anwendung
      startet, der Dialog öffnet
- [ ] **Windows Sandbox ohne Internet**: Der Bootstrapper schlägt fehl, die Meldung
      `WebView2Fehlt` erscheint, **die Installation läuft weiter**, die Anwendung startet, nur der
      Blazor-Dialog bleibt leer
- [ ] Rechner **mit** vorhandener Laufzeit: Der Bootstrapper wird gar nicht erst mitgenommen
      (`Check: not WebView2Vorhanden`)
- [x] ~~`.gitignore` um `/MicrosoftEdgeWebview2Setup.exe` ergänzen, bevor `GitHub_Sync.bat` das
      nächste Mal mit `git add -A` läuft (offener Punkt S9)~~ — **erledigt mit `e3d1e5b`**

---

## Offen aus anderen Strängen

### ~~Der Stapellauf des Formular-Generators liest den gelöschten Dialog~~ — erledigt

`Werkzeuge/Formularkarte.Tests` las die **echten** Designer-Dateien des Bestands.
`Form_Kosten_Auswahl.Designer.cs` ist mit iU8-9 gelöscht, `new Form_Kosten_Auswahl` aus
`Form_Kosten.cs` verschwunden — **22 der 100 Tests scheiterten seitdem**.

**Gelöst mit iU8-12e (`4aa6b15`)**, und zwar durch eine Trennung statt durch eine andere
Probemaske: Der letzte Stand der gelöschten Maske liegt **eingefroren** unter
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/` (Designer, `.cs`, `.resx` und der
Aufrufer-Auszug aus `Form_Kosten.cs`, wortgleich aus `92380ea^`). Das Muster wird **nie
übersetzt** und vom Stapellauf **übergangen** wie `bin` und `obj`; die `StapelTests` prüfen
weiterhin den lebenden Bestand, jetzt an `Form_Kosten_VarAuswahl`. **101 Tests, alle grün.**
Nachgemessen nach iZ5: **122 Designer-Dateien, 119 Masken** im Repo, davon **117 unter `Views/`**.

Das Werkzeug steht weder in `WP-Plan.sln` noch in `WP-Plan.Kern.slnf`; Bau und die 886 Tests
bleiben davon unberührt. **Offen bleibt nur die Aufnahme in die CI** — sie ist Gegenstand des
`kern.yml`-Schritts „Formularkarte-Tests" und läuft nur auf `ubuntu-latest`.

- [ ] `dotnet test Werkzeuge/Formularkarte/Formularkarte.sln -c Release` auf Windows → 101/101

### Verteilung der WebView2-Laufzeit

- [ ] Anwenderentscheidung: Online-Bootstrapper (heute, ~2 MB), Standalone-Installer (~150 MB,
      offline) oder Fixed-Version-Verteilung (Laufzeit im Programmordner, Aktualisierung liegt dann
      bei uns). Offen als **S10** in
      [`Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md`](Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md),
      Abschnitt 5.5.

---

## Wenn der Dialog leer bleibt

| Bild | Wahrscheinliche Ursache | Prüfung |
|---|---|---|
| Fenster öffnet, bleibt **beige** | WebView2-Laufzeit fehlt | Registry-Abfrage oben |
| Fenster öffnet, bleibt **weiß** | `wwwroot\index.html` fehlt im Ausgabeordner | Razor-SDK und `Content Update="wwwroot\**"` in der `.csproj` |
| Inhalt da, aber **ohne Gestaltung** | `_content\EPOS.UI\epos-ui.css` fehlt | Publish-Ordner prüfen; `EPOS_Plan.styles.css` liegt **in** `wwwroot`, nicht neben der EXE |
| Fenster öffnet **gar nicht**, Ausnahme beim Start | Profilordner nicht anlegbar | `UserDataFolder` in `BlazorDialogForm`; Rechte an `%LOCALAPPDATA%\WP-Plan` |
| Inhalt **unscharf** bei 125 % | DPI-Insel greift nicht | Windows-Fassung (10/1803 oder neuer?); der Rest der Anwendung ist absichtlich DpiUnaware |
| Infoknopf **ohne Wirkung** | Zuordnung oder Katalog fehlt | Zeile `Form_Kosten_Auswahl.btn_Help = Kosten` in `help_mapping.txt`; Debug-Ausgabe `[Help] WARNUNG: …` |

# iU9 Welle 15c — Lizenz und Erststart: Portprotokoll

> Vermessung `iU9_W15c_Vermessung.md` (1 905 Z., 04.09.2026, Stand `3ae6847`/`4101740`),
> Arbeitsanweisung `iU9_W15c_Arbeitsanweisung.md`. Form nach
> `iU9_W15b_Blazor_Port_Protokoll.md`.

## 0 — Was die Welle getan hat

**Drei Masken sind gefallen** (1 588 Zeilen `.cs`, 246 Zeilen Designer, 119 Zeilen
`.resx`, 16 `MessageBox` — dazu drei Meldungen im Startweg von `Program.cs`):

| Maske | Zeilen | Nachfolger |
|---|---|---|
| `Views/Admin/Form_LizenzVerwaltung.{cs,Designer.cs,resx}` | 302 + 246 + 119 | `LizenzVerwaltungDialog.razor` + `Views/Admin/LizenzVerwaltungHuelle.cs` |
| `Views/Admin/Form_Erststart.cs` | 273 (kein Designer) | `ErststartDialog.razor` + `Views/Admin/ErststartHuelle.cs` |
| `Views/Help/Form_Lizenz.cs` | 1 013 (kein Designer) | `LizenzDialog.razor` + `Views/Help/LizenzHuelle.cs` |

**Kein neuer Baustein** (Befund B12) — die Welle lebt vollständig vom Vorhandenen:
`Reiter`/`Reiterblatt`, `Textfeld`, `Warnbanner`, `Rueckfrage`, `Fortschritt`,
`InfoKnopf`, `Ueberlagerung`.

**Der Befund der Welle ist eine Null.** Der Lizenzkern liegt seit iU5‑U1
plattformfrei in `EPOS.Kern/Allgemein/Lizenz/` — 659 Zeilen mit sechs Zuständen,
zwei Fristen, einem Kulanzfenster, einer Karenzzeit, einem Uhr-Manipulationsschutz
und einer Ed25519-Signaturprüfung. **Geprüft hat davon bis heute nichts** (Befund
B1). Der Wellennachweis „Lizenzzustände NichtAktiviert…Lesemodus durchspielen" ist
deshalb keine Nachführung, sondern eine **Erstanlage** — und sie entsteht **vor der
ersten Maske**.

**Zwei Startschritte laufen jetzt über eine Blazor-Hülle** (Erststart und
Lizenzzustimmung), und beide enden bei `false`, wenn ihr Fenster leer bleibt. Ohne
WebView2-Laufzeit wäre EPOS-Plan damit **unstartbar** (Befund B10). Deshalb prüft
`Program.Main` die Laufzeit seit W15c.6a selbst.

## 1 — Commits

| # | Commit | Was |
|---|---|---|
| 1 | `bb805d3` | **W15c.1** `LizenzManager.Bewerten` herausgezogen, 14 Zustandsfälle (der Wellennachweis) |
| 2 | `903fb39` | **W15c.2** `LizenzTokenTests` — die Signaturprüfung, Testschlüsselpaar im Test |
| 3 | `a3e4e6a` | **W15c.3** `StatusText`/`TypText` auf `MyResource`, neun Schlüssel zweisprachig |
| 4 | `0fbebba` | **W15c.4** `LizenzCtrl` — die Datenseite der Lizenzverwaltung im Kern |
| 5 | `6a16d0e` | **W15c.5** `LizenzVerwaltungDialog` + Hülle, `Form_LizenzVerwaltung` gelöscht |
| 6 | `93581a2` | **W15c.6** die vier Zusätze an `BlazorDialogForm<T>` |
| 7 | `ece2ac2` | **W15c.6a** WebView2-Riegel in `Program.Main` (E‑8, Weg 2) |
| 8 | `a0722ce` | **W15c.7** `ErststartDialog` + besitzerlose Hülle, `Form_Erststart` gelöscht |
| 9 | `b86d18c` | **W15c.8/9** `LizenzTextCtrl` und `ZustimmungCtrl` im Kern |
| 10 | `a9787a8` | **W15c.10** die 63 Texte — 27 Rechtstexte **maschinell** umgezogen |
| 11 | `d06371e` | **W15c.11** `LizenzDialog` + zwei Hüllenwege, `Form_Lizenz` gelöscht |
| 12 | `e0ed876` | **W15c.12** Zeugen, Schwellen und die drei `CLAUDE.md` |
| 13 | (dieser) | Protokoll |
| 14 | `2434322` | **Nachtrag 04.09.2026, nach W16** — `LizenzTexte`, das Bündel aus W15c‑O‑2 (§ 12) |

Die Reihenfolge folgt § 14 der Vermessung ohne Abweichung: Kern zuerst, kleinste
Maske zuerst, die Hüllenzusätze vor dem Erststart, der Rechtstext zuletzt.

## 2 — Feldkartenabgleich

**Eine** der drei gefallenen Masken hatte einen Designer; für sie ist die Feldkarte
vor dem Port gezogen worden. Die beiden anderen sind **K4** (Oberfläche im Code
aufgebaut, kein Designer, keine `.resx`, Befund B2) und stehen hier als Abnahmeliste
von Hand.

### 2.1 `Form_LizenzVerwaltung` (Feldkarte: 12 Kartenzeilen, 3 Abschnitte, 0 ohne Beschriftung, 9 MessageBox, 2 Aufrufer, „Öffner erreichbar: ja")

| # | Steuerelement | Typ | Text de | Nachfolger | ☑ |
|---|---|---|---|---|---|
| 1 | `_statusBox` | GroupBox | Lizenzstatus auf diesem Arbeitsplatz | `<section>` + `h2.epos-lizverw-kopf` | ☑ |
| 2 | `_statusWert` | Label, Semibold, **drei Farben** | aus `StatusText()` | `p.epos-lizverw-status--gut/warn/schlecht`, `role="status"` | ☑ |
| 3 | `_detailWert` | Label `#5A6066` | `LIZ_DETAIL` / `LIZ_DETAIL_KEINE` | `p.epos-lizverw-detail` | ☑ |
| 4 | `_portal` | LinkLabel | Lizenzportal öffnen … | `<a target="_blank" rel="noopener noreferrer">` | ☑ |
| 5 | `_aktivBox` | GroupBox | Aktivieren | `<section>` | ☑ |
| 6 | `_schluesselLabel` + `_schluessel` | Label + TextBox (`CharacterCasing = Upper`) | Lizenzschlüssel: | `<input>` mit `text-transform: uppercase` **und** `ToUpperInvariant()` | ☑ |
| 7 | `_licLaden` | Button | Lizenzdatei (.lic)… | `button.epos-lizverw-lic` → Delegat `LicLesen` | ☑ |
| 8 | `_emailLabel` + `_email` | Label + TextBox | E-Mail (Benutzer): | `<input type="email">` | ☑ |
| 9 | `_aktivieren` | Button | Jetzt aktivieren | `button.epos-knopf--primaer` | ☑ |
| 10 | `_aktivHinweis` | Label 8 pt `#787E84` | zweizeiliger Datenschutzhinweis | `p.epos-lizverw-hinweis` | ☑ |
| 11 | `_aktionenBox` | GroupBox | Weitere Aktionen | `<section>` | ☑ |
| 12 | `_trial` | Button | Testversion anfordern… | gesperrt, sobald ein Token da ist | ☑ |
| 13 | `_freigeben` | Button | Gerät von der Lizenz lösen | gesperrt, solange keines da ist; `Rueckfrage` mit `VorgabeNein` | ☑ |
| 14 | `_hinweis` | Label (bewusst leer) | Statuszeile laufender Vorgänge | `<Warnbanner>`, leer = weg | ☑ |
| 15 | `_schliessen` | Button (CancelButton) | Schließen | `button.epos-knopf--primaer` | ☑ |
| — | `InfoKnopf.Anbringen(this)` | — | — | `<InfoKnopf Schluessel="Form_LizenzVerwaltung.btn_Help">` | ☑ |
| — | `FensterEinpassung.Einhaengen` | — | — | entfällt — Sache der Hülle | ☑ |
| — | 9 × `MessageBox` | — | — | **acht** werden `Warnbanner`, **eine** bleibt Rückfrage (Gerät lösen) | ☑ |

### 2.2 `Form_Erststart` (K4, von Hand — 9 Steuerelemente)

| Glied | Nachfolger | ☑ |
|---|---|---|
| `kopf` Label Dock Top 158 px, neunzeiliger Ablauftext | `p.epos-erststart-kopf` (`white-space: pre-line`), Text aus `ERST_KOPF` mit den drei Dateinamen | ☑ |
| `_status` Label 24 px, AutoEllipsis, „Bereit." | `p.epos-erststart-status`, `role="status"` | ☑ |
| `_balken` ProgressBar **Marquee**, `Visible = false` | `<Fortschritt Anteil="null">` — unbestimmt, und nur während des Laufs | ☑ |
| `_protokoll` TextBox Fill, Multiline, ReadOnly, GenericMonospace | `<Textfeld Mehrzeilig NurLesen Festbreite Zeilen="14">` | ☑ |
| `_starten` Button „Jetzt umstellen" (AcceptButton) | `button.epos-knopf--primaer`, während des Laufs gesperrt | ☑ |
| `_beenden` Button „Beenden" (CancelButton) | `button.epos-knopf`, während des Laufs gesperrt | ☑ |
| `FormClosing`-Riegel `e.Cancel = true` + `ControlBox = false` | `BlazorDialogForm.SchliessenGesperrt`, geschaltet über den Rückkanal `LaufAktiv` | ☑ |
| `ShowInTaskbar = true`, `CenterScreen`, `MinimumSize 600×400` | `ImTaskbar`, `AufBildschirmMittig`, `Mindestmass` (die Zusätze aus W15c.6) | ☑ |
| eigener `Thread` + `Progress<string>` auf dem Oberflächenstrang | `Task.Run` in der Hülle, `Progress<string>` unverändert dort erzeugt | ☑ |
| kein `InfoKnopf`, kein `FensterEinpassung` | bleibt so — als einzige Maske der Welle | ☑ |

### 2.3 `Form_Lizenz` (K4, von Hand — 25 Steuerelemente, 10 Button-Deklarationen)

| Glied | Nachfolger | ☑ |
|---|---|---|
| `kopf` Panel 58 px weiß + `titel` + `untertitel` | `header.epos-lizenz-kopf` mit `h1`/`p` | ☑ |
| `_register` TabControl mit **drei** `TabPage` (B3: nicht vier) | `<Reiter>` mit drei `<Reiterblatt>` | ☑ |
| `_text` RichTextBox (Vertrag) | `<Textfeld Mehrzeilig NurLesen Zeilen="20">` — die RTF-Anzeige entfällt (E‑1) | ☑ |
| `_hinweise`, `_komponenten` RichTextBox | `h3`/`p` aus 15 bzw. 12 `RechtsAbschnitt` | ☑ |
| `_suche` + `btnSuchen` + `btnGroesser` + `btnKleiner` | **entfallen** (E‑12) — die WebView bringt Zoom mit | ☑ |
| `btnWaehlen` „Datei wählen..." | `button` → Delegat `DateiWaehlen` über `Dienste.Datei` | ☑ |
| `_lblQuelle` zweizeilig (Lizenzstand + Quelle/Stand) | `div.epos-lizenz-herkunft` mit zwei `span` | ☑ |
| `btnDrucken` | bleibt — `window.print()` statt `PrintDocument` (E‑2) | ☑ |
| `btnSpeichern` | bleibt — `Dienste.Datei.DateiSpeichern`, Text statt RTF (E‑1) | ☑ |
| `btnAktivieren` | bleibt — öffnet die Verwaltung als **Überlagerung** (E‑11) | ☑ |
| `btnSchliessen` / `btnZustimmen` + `btnAblehnen` | zwei Gesichter über `Zustimmungsmodus`; Reihenfolge bitgleich (E‑14) | ☑ |
| `LinkClicked` → `Process.Start` | `<a target="_blank" rel="noopener noreferrer">`, nur bei `https://` | ☑ |
| Registry `LizenzDatei` / `LizenzZugestimmt` | `Dienste.Einstellungen` über `LizenzTextCtrl` / `ZustimmungCtrl` — **derselbe Zweig** (B17) | ☑ |
| `%APPDATA%`-Zwischenspeicher | `Dienste.Pfade.Anwendungsdaten` in `LizenzTextCtrl` | ☑ |
| `Program.ApplicationPath_*` in `DateiSuchen` | `Dienste.Pfade.Gemeinsam` / `.BenutzerLokal` | ☑ |
| 7 × `MessageBox` | vier bleiben (als `Warnbanner` bzw. `Dienste.Dialog`), drei entfallen mit Suche und Druckweg | ☑ |
| `InfoKnopf.Anbringen(this)` | `<InfoKnopf Schluessel="Form_Lizenz.btn_Help">` — vor dem Hauptfenster folgenlos wie bisher (B25) | ☑ |

## 3 — Der Wellennachweis: 79 neue Kern-Fälle und 67 bunit-Fälle

| Zeuge | Ort | Fälle | Was |
|---|---|---|---|
| **`LizenzZustandTests`** | `EPOS.Kern.Tests` | **19** | Die **14 Fälle** aus § 11.7 a: kein Token, fremdes Gerät, gültig, Kulanzränder +14/+15, ohne Kulanztage, Karenzränder +14/+15, Uhrtoleranz von genau einem Tag, „Laufzeit sticht Leine", Schreibrecht je Zustand (6 Theorie-Fälle). **Kein Fall fasst die Ablage an** (R‑W15c‑3) |
| **`LizenzTokenTests`** | dito | **10** | Die **4 Fälle** aus § 11.7 b: fremd signiertes Token abgelehnt (Paar im Test erzeugt), unbekanntes Format abgelehnt, verbogene Nutzdaten brechen die Signatur, unlesbares JSON meldet statt zu werfen, `TypText` für `demo`/`person`/`firma` und zwei Randfälle, dazu der öffentliche Schlüssel als Zeuge |
| **`LizenzTexteTests`** | dito | **12** | Zu jedem Zustand genau ein Schlüssel; alle neun in **beiden** Sprachen und nicht auf den deutschen Wert zurückfallend; die zwei Formatschlüssel tragen ihre Platzhalter; der Lizenztyp wird übersetzt |
| **`LizenzCtrlTests`** | dito | **21** | Sechs Zustandsnamen, zehn E-Mail-Fälle (darunter `name@firma` OHNE Punkt und ein Anzeigename), vier `.lic`-Fälle, die Portaladresse |
| **`LizenzTextCtrlTests`** | dito | **17** | `HtmlZuText` an einem Schnipsel (Skript/Stil weg, Entitäten aufgelöst, Leerraum eingeebnet), `StandFormatieren`, die Antwortauswertung ohne Netz, Mindestlänge, Onlinequelle, gemerkter Pfad, Suchreihenfolge — dazu **W15c.9**: Zustimmung merken, das eingefrorene Vermerkformat, und **der Fehlerpfad `catch → true`** an einer werfenden Ablage |
| **`LizenzVerwaltungDialogTests`** | `EPOS.UI.Tests` | **28** | § 11.7 c vollständig: Feldbestand, sechs Zustände auf drei Stufen, Sperrlogik Trial/Freigeben, `LIZ_MSG_EINGABE_FEHLT`, `LIZ_MSG_EMAIL_UNGUELTIG` **mit** Adresse und **ohne** Schlüssel, Feld leer nach Erfolg (S4), Servermeldung vs. eigener Text, drei `.lic`-Fälle, Testversion, Rückfrage mit „Ja" und **„Nein" tut nichts**, Netzfehler, Schließen, Vorbelegung, ohne Delegat passiert nichts |
| **`ErststartDialogTests`** | dito | **13** | § 11.7 e: Kopftext nennt alle drei Dateinamen, vor dem Start ist alles bedienbar, Protokoll nur lesbar in fester Schrittweite, **kein Abbruch während des Laufs**, `Anteil = null`, `LaufAktiv` in der richtigen Reihenfolge, Protokollzeilen und Statuszeile, Erfolg und Fehlschlag, zweiter Klick |
| **`LizenzDialogTests`** | dito | **26** | § 11.7 d und mehr: drei Reiter in ihrer Reihenfolge, Reiterwechsel, Zustimmungsmodus mit genau zwei zusätzlichen Knöpfen, „Zustimmen" meldet **beides**, Knopfreihenfolge (E‑14), Fußzeile mit und ohne Stand, Dateiwahl, Speichern der **aktiven** Karte, Online-Nachladen **genau einmal**, die Verwaltung als Überlagerung, der Sprachhinweis (E‑7), **aus Text wird nie Auszeichnung** |

**Kein Netz in einem einzigen Fall.** Der Lizenzserver und die Online-Fassung sind
Delegaten; die Antwortauswertung ist vom Abruf getrennt (`LizenzTextCtrl.AntwortLesen`).
**Kein Fall fasst eine echte Ablage an** — weder `Dienste.Lizenzablage` noch die
Registry.

## 4 — Die Angleichungen (A‑1 … A‑9)

| Nr | Was | Warum |
|---|---|---|
| **A‑1** | **Die Suchleiste und die Knöpfe A+/A− entfallen** (E‑12). | Die WebView bringt Zoom mit (Strg+Mausrad); eine eigene Textsuche wäre neuer Programmtext für einen Nebenweg, und zwei Bedienwege für dieselbe Sache sind eine Falle. |
| **A‑2** | **Die RTF-Anzeige entfällt** (E‑1); `.rtf` und `.docx` zeigen **denselben** Hinweistext. | `RichTextBox.LoadFile` hat in HTML kein Gegenstück. Der Normalfall ist ohnehin die Online-Fassung: Die `.rtf` liegt nur im Entwicklungsbaum. |
| **A‑3** | **„Drucken…" ruft `window.print()`** statt `PrintDocument` mit eigenem Seitenumbruch (E‑2); Kopf- und Fußzeile kommen aus `@media print`. | Der einzige `PrintDocument`-Nutzer des ganzen Bestands (B15) fällt damit. Das Druckbild sieht anders aus — dieselbe Aussage, andere Form. |
| **A‑4** | **Die Lizenzverwaltung erscheint als Überlagerung**, nicht als zweites Fenster (E‑11). | Regel R2 aus iU8: zwei WebViews übereinander gibt es nicht. Der Anwender sieht eine Abdunkelung; Esc und „Schließen" führen zurück. |
| **A‑5** | **Verweise werden nicht geraten.** Nur `https://`-Läufe im **erzeugten** Rechtstext werden Verweise; der Vertragstext der ersten Karte steht als reiner Text. | Der Vorläufer ließ die `RichTextBox` Adressen ERKENNEN (`DetectUrls`). Dieselbe Linie wie W15b‑A‑4: Ein HTML-Baustein darf nicht raten. Gebaut werden die Verweise als **Elemente**, nie als `MarkupString` — aus Text darf nie Auszeichnung werden. |
| **A‑6** | **„Speichern unter…" schreibt Text statt RTF**, und der Dateifilter nennt nur noch `.txt`. | Folge von A‑2: Es gibt keine `RichTextBox`, die eine RTF-Datei schreiben könnte. Was gespeichert wird, ist, was auf dem Bildschirm steht. |
| **A‑7** | **Die Online-Fassung wird ABGEWARTET** statt aus einem `async void` in die Anzeige geschrieben (B28). | Der Vorläufer prüfte nur `IsDisposed`. In Razor wartet die Komponente den Delegaten ab und zeichnet über den Verteiler; ein Zugriff auf ein entsorgtes Steuerelement ist damit ausgeschlossen. |
| **A‑8** | **`Mindestmass` statt `new MinimumSize`** an der Hülle. | Die Vermessung schlug ein `public new Size MinimumSize` vor. Eine geerbte Eigenschaft zu verdecken ist eine Falle: Wer die Hülle je über eine `Form`-Variable hielte, setzte die andere. Der neue Name sagt dasselbe und schreibt dieselbe Eigenschaft. |
| **A‑9** | **Der Zusatz „Binding version in German." steht EINMAL** (`LIZR_HINWEIS_SPRACHE`, deutsch leer) statt 27-mal angehängt. | E‑7 verlangt den Hinweis im englischen Zweig. Als eigener Schlüssel steht er über den Abschnitten, ist an einer Stelle zu ändern, und die 27 Rechtstexte bleiben in beiden Zweigen **zeichengleich** — genau das prüft der Vergleichslauf. |

## 5 — Anwenderfragen

| Nr | Frage | Was jetzt im Code steht |
|---|---|---|
| **E‑8** | **Die wichtigste der Welle.** Ohne WebView2-Laufzeit wird die Anwendung nach W15c unstartbar (B10). Weg 1 (WinForms-Rückfallmasken), Weg 2 (Prüfung + Meldung + Ende) oder Weg 3 (die beiden Startmasken bleiben WinForms)? | **Weg 2 umgesetzt.** `Program.Main` prüft nach der Sprachwahl und vor dem ersten besitzerlosen Dialog `CoreWebView2Environment.GetAvailableBrowserVersionString()` in `try/catch`; fehlt sie, erscheint eine native `MessageBox` mit der Bezugsquelle (`START_WEBVIEW2_FEHLT`, Wortlaut nach `Setup/EPOS-Plan.iss:204/205`, zweisprachig) und das Programm endet. **Keine Rückfallmasken** (Regel M1). Weg 1 hätte zwei Fassungen derselben Maske hinterlassen, Weg 3 zwei WinForms-Masken, die auf iOS nie gebraucht werden und in W16 erneut anzufassen wären. **Der Anwender bestätigt oder ändert das.** |
| **E‑7** | Werden die 27 Rechtstexte übersetzt — und durch wen? | **Nicht übersetzt.** Sie stehen **deutsch in beiden Sprachzweigen**; im Englischen erscheint darüber „Binding version in German." (A‑9). Begründung: Die Maske sagt selbst, verbindlich sei allein die deutsche Lizenzvereinbarung, und eine maschinelle Übersetzung von Haftungs- und Gewährleistungsabsätzen wäre ein Risiko, keine Erleichterung. **Eine andere Entscheidung kostet den Austausch von 27 Werten im englischen Zweig — mehr nicht.** |
| **E‑9** | Wird die Lesemodus-Durchsetzung Teil dieser Welle? | **Nein.** W15c macht den Zustand **sichtbar** (sechs Zustände, drei Stufen, Detailzeile) und **prüfbar** (19 Kern-Fälle), setzt ihn aber nicht durch. Der Lesemodus hat bis heute **genau einen Leser** — `KiAusfuehrer.Schreibrecht` (B7); die Durchsetzung an Simulation, Projektanlage und allen Speicherwegen ist ein eigenes Paket. **Neuer Registerpunkt: iF30 — Lesemodus-Durchsetzung, nach W16**, wenn alle Speicherwege Razor sind und ihre Zahl feststeht. |
| **E‑12** | Sollen Suchleiste und A+/A− erhalten bleiben? | **Entfallen** (A‑1). Wenn die Suche zurück soll, ist sie neu zu bauen (Markierung im HTML) — das ist eine eigene kleine Arbeit, kein Nachzug. |
| **E‑17** | Soll der Vertragsendpunkt `epos/v1/vertrag` die AGB-Seite als Quelle ablösen? | **Heute bitgleich die AGB-Seite.** `LizenzTextCtrl.ONLINE_QUELLE` ist **eine Zeile**, und der Zeuge hält sie fest. Der Server bietet seit `epos-lizenz` 1.4.0 je Tarif Stand, SHA‑256 und URL des Dokuments, **das der Kunde im Checkout akzeptiert hat** (B27) — die fachlich richtigere Quelle. Die Umstellung kostet genau diese Zeile. |
| **E‑2** | Soll der Druckknopf bleiben? | **Bleibt**, mit Browserdruck (A‑3). Fällt die Entscheidung anders, entfällt ein Knopf und ein CSS-Block. |
| **E‑4** | Soll iOS einen Einstieg in die Lizenzverwaltung bekommen? | **Vorerst nicht.** `AppWurzel` bleibt bei fünf Seitenschlüsseln; die Verwaltung ist ein Dialog, keine Fachseite (B11). Die Frage gehört zu iU11, wenn `AppWurzel` ein Menü bekommt. |

## 6 — Befunde B1 … B28

| Nr | Eingetreten? | Entscheid |
|---|---|---|
| **B1** | ja | **Der Kern der Welle.** 79 neue Kern-Fälle, angelegt VOR der ersten Maske. |
| **B2** | ja | Zwei K4-Masken; ihre Feldlisten stehen in § 2.2/2.3 von Hand. Der Stapellauf nimmt deshalb nur EINEN Designer mit. |
| **B3** | ja | Drei Registerkarten (nicht vier), eine `TextBox` + drei `RichTextBox`, Aufruferzeilen `MDIMainForm.cs:418/679`. Im Zeugen festgehalten. |
| **B4** | ja | Keine `Masken.*`, kein `Sprungziel` — die Welle fasst die Sprungtabelle nicht an. |
| **B5** | ja | **Auflage O‑1 umgesetzt**: „Microsoft .NET 8" → .NET 10, die ACE-Engine steht jetzt bei ihrem tatsächlichen Zweck (einmalige Umstellung eines Access-Altbestands), SQLite und die WebView2-Laufzeit sind ergänzt. **Der einzige der 27 Texte, der sich ändert.** |
| **B6** | ja | Der Lokalisierungszähler bleibt bei **7** — `Form_LizenzVerwaltung` zählte nie mit (sie setzt ihre Texte im Code aus `MyResource`). Bestätigt: 7 vor und nach der Welle. |
| **B7** | ja | → **iF30** (E‑9). |
| **B8** | ja | Die vier Zusätze an `BlazorDialogForm<T>` (W15c.6), alle mit heutigem Vorgabewert. |
| **B9** | ja | `ErststartCtrl` bleibt in der Windows-App; auf iOS erscheint der Assistent nie (E‑5). |
| **B10** | ja | → **E‑8, Weg 2** (W15c.6a). |
| **B11** | ja | Kein Lizenzweg auf iOS; `AppWurzel` unverändert, der Prüfmodus fragt die Lizenz weiterhin nicht. |
| **B12** | ja | Kein neuer Baustein. |
| **B13** | ja | **Regel S‑2 durchgehalten**: Keine der drei Komponenten nennt `LizenzManager`, `LizenzServerClient`, `GeraeteId` oder eine Signaturprüfung. Alles kommt als Gabe herein. |
| **B14** | ja | **Auflage O‑3 umgesetzt**: `[".rtf"] = "public.rtf"`. Für `.lic` bleibt `public.data` richtig. |
| **B15** | ja | Der einzige `PrintDocument`-Nutzer fällt (A‑3). |
| **B16/B17** | ja | Der Umstieg auf `Dienste.Einstellungen` ist bitgleich — derselbe Registry-Zweig `HKCU\Software\wp-plan`, dieselben zwei Namen (`LizenzDatei`, `LizenzZugestimmt`). |
| **B18** | ja | **`catch → true` wortgleich übernommen**, mit demselben Kommentar, in `ZustimmungCtrl.IstZugestimmt` — und mit einem eigenen Zeugen (werfende Ablage). |
| **B19** | ja | Überlagerung statt zweitem Fenster (A‑4). |
| **B20** | ja | **E‑16 umgesetzt**: Der Trial-Name kommt aus der HÜLLE (`Environment.UserName` unter Windows), nicht aus der Komponente. |
| **B21** | ja | `LicLesen` prüft die Signatur weiterhin **nicht** — im Kommentar und im Zeugen festgehalten (Regel S3). |
| **B22** | ja | Beim Umhängen des Menüeintrags ist die stille Nachprüfung (`_ = LizenzManager.NachpruefungImHintergrund()`) **nicht** mitgegangen; sie steht unverändert in `InitLizenzMenue`. |
| **B23** | ja | → E‑10/W15c.1: `Bewerten` ist eine reine Funktion, kein Test fasst den Zeitanker an. |
| **B24** | ja | Die jüngste Maske war die am leichtesten umzustellende — 88 ausführbare Zeilen Komponente. |
| **B25** | ja | Der Infoknopf bleibt vor dem Hauptfenster folgenlos, wie im Bestand. |
| **B26** | **nein — vorweggenommen** | Der Typzeuge musste **nicht** umgebaut werden: W14a/W14c haben `DieHaeufigstenTypenSindAbgedeckt` schon auf „Bestand **ODER** Prüfmuster" gestellt, und `GroupBox` steht im eingefrorenen Muster (`Pruefmuster/Pufferspeicher`, `Pruefmuster/Wizard`). Der Test läuft unverändert grün. |
| **B27** | ja | → **E‑17**; die Quelle ist eine Zeile und im Zeugen festgehalten. |
| **B28** | ja | Der Onlineabruf wird abgewartet (A‑7). |

**Zwei neue Befunde:**

| Nr | Befund | Entscheid |
|---|---|---|
| **B29** *(neu)* | **Entscheid E‑6 ist gegenstandslos.** Die Vermessung nahm an, der „ja"-Zeuge liege nach W14c auf einer Maske, die W15c löscht, und müsse deshalb auf `Form_StromTest` wandern. Gemessen liegt er seit W14c.9 auf **`MDIMainForm`** — der Wurzel selbst, Pfadlänge 1 —, und der Test heißt dort `DasHauptfensterIstDieWurzelUndDamitErreichbar`. Er kann nicht unerreichbar werden und fällt erst mit Welle 16. | **Nicht angefasst.** Ein Umzug auf `Form_StromTest` wäre ein Rückschritt: kürzerer Pfad, aber eine Maske, die früher fällt. |
| **B30** *(neu)* | **Der Kopftext des Erststarts überschreibt seinen eigenen Zustandstext.** `Fertig()` setzt „Umstellung abgeschlossen." und hängt DANACH die Schlussmeldung an — und `ZeileAnhaengen` zieht jede Zeile in die Statuszeile nach. Am Ende steht dort also die SCHLUSSMELDUNG, nicht der Zustandstext. Beim ersten Zeugenentwurf fiel das als Fehlschlag auf. | **Bitgleich übernommen** (`Form_Erststart.cs:265-267`), samt einem Zeugen für beide Fälle: mit Schlussmeldung zieht sie nach, ohne bleibt der Zustandstext stehen. |

## 7 — Sicherheit (S1 … S4)

| Regel | Wo eingelöst | Nachweis |
|---|---|---|
| **S1** Der Geltungsbereich der Ablage ist eingefroren | `LizenzCtrl` reicht **keine** Bereichsangabe durch; `ILizenzAblage`, `DpapiLizenzAblage` und `IosLizenzAblage` sind nicht angefasst | `git diff f71853b` über die drei Dateien ist **leer** |
| **S2** Die gehashte Geräte-Id-Zeichenkette ist eingefroren | `GeraeteId.cs` und `IosGeraeteId.cs` sind nicht angefasst | `git diff` ebenfalls **leer** |
| **S3** Eine Signaturprüfstelle | `LizenzToken.SignaturPruefen` bleibt der einzige Weg, auf dem aus einer Zeichenkette ein Token wird. `LizenzToken.FuerPruefstand` ist **kein zweiter Prüfweg**, sondern eine Bauhilfe ohne `RohJson` (ein so gebautes Token lässt sich nicht ablegen) und `internal`. `LicDateiLesen`/`LizenzCtrl.LicLesen` prüfen weiterhin **nicht** | `git grep` auf `Ed25519`, `OEFFENTLICHER_SCHLUESSEL`, `LizenzManager` in `EPOS.UI` = **0** (nur Kommentare) |
| **S4** Der Schlüssel verlässt die Komponente nur Richtung `Aktivieren` | Das Schlüsselfeld wird nie vorbelegt, nach Erfolg **geleert**, in keiner Meldung wiederholt, und nichts landet in `localStorage` | drei Zeugen in `LizenzVerwaltungDialogTests` |

## 8 — Texte

**Neu: 89 Schlüssel, alle zweisprachig** (4 414 → 4 503 Einträge je Datei):

| Gruppe | Anzahl | Was |
|---|---|---|
| `LIZ_ST_*`, `LIZ_TYP_*` | **9** | Die sechs Zustandssätze und drei Lizenztypen des Kerns — bis W15c deutsche Literale in `LizenzManager`/`LizenzToken` |
| `LIZR_*` (Bedienung) | **36** | Titel beider Betriebsarten, Kopfzeile, drei Reiter, sieben Knöpfe, Zustimmungshinweis, Fußzeile, drei Ersatztexte, sechs Dateidialogtexte, drei Dateinamen, vier Meldungen, der Sprachhinweis |
| `LIZR_RH_*`, `LIZR_KO_*` (Rechtstext) | **27** | 7 + 8 „Rechtliche Hinweise", 6 + 6 „Komponenten" |
| `ERST_*` | **9** | Der Erststart-Assistent |
| `START_*` | **8** | Die drei Startmeldungen samt Titeln (die offene Zusage aus `Program.cs:157-159`) und die zwei WebView2-Texte |

**Der maschinelle Umzug und der Vergleich Zeichen für Zeichen** (Risiko R‑W15c‑9):
Die 27 Rechtstexte sind mit einem Skript aus
`git show 3ae6847:WindowsFormsApplication1/Views/Help/Form_Lizenz.cs` gezogen
(Erkennung der `SchreibeUeberschrift`/`SchreibeAbsatz`-Aufrufe samt ihrer über mehrere
Zeilen verketteten Literale, `Environment.NewLine` → `\n`, die zwei
`SemantikModell`-Konstanten und die zwei Laufzeitwerte → Platzhalter) und danach aus
beiden `.resx` **zurückgelesen und verglichen**:

```
zeichengleich : 26 von 27
berichtigt    :  1 (O-1, "Laufzeit und Bibliotheken")
Abweichungen  :  0
```

Von Hand abgetippt wäre man an einem Bindestrich, einem Anführungszeichen oder einem
Gedankenstrich gescheitert — der Text trägt beides.

`Resource.Designer.cs` ist maschinell und **alphabetisch** ergänzt, damit Visual
Studio beim nächsten Regenerieren keine Duplikate baut (CS0102).

## 9 — Die vier Hüllenzusätze

`BlazorDialogForm<T>` bekommt vier Schalter, **alle mit dem heutigen Vorgabewert** —
für die vorhandenen Blazor-Aufrufer ändert sich nichts:

| Zusatz | Vorgabe | Wofür |
|---|---|---|
| `ImTaskbar` | `false` | Ein minutenlanger Lauf ohne Elternfenster und ohne Taskleisteneintrag ist nicht wiederzufinden |
| `AufBildschirmMittig` | `false` (`CenterParent`) | Ohne Besitzer ausdrücklich der Bildschirm statt „irgendein Hauptschirm" |
| `SchliessenGesperrt` | `false` | `ControlBox` aus **und** ein Riegel in `OnFormClosing`, wörtlich aus `Form_Erststart:196-200`; nur `CloseReason.UserClosing` wird gefangen. Der Setter marshallt, weil der Rückkanal aus dem Blazor-Verteiler kommt |
| `Mindestmass` | 520 × 360 | Das Kleinstmaß; siehe A‑8 |

**Der Rückkanal `LaufAktiv`** ist der erste Weg Komponente → Hülle außer `Schliessen`:
Die Komponente weiß, wann ein Lauf beginnt und endet, und meldet es als
`EventCallback<bool>`; die Hülle legt darauf `SchliessenGesperrt`. **Erst die Sperre
lösen, dann schließen** — sonst finge der Riegel den eigenen Schließbefehl; im Zeugen
als Reihenfolge geprüft.

Die vier Eigenschaften tragen `DesignerSerializationVisibility.Hidden`; ohne sie
stünde **WFO1000** wieder bei vier statt bei null (Stand seit iU9‑W14c).

## 10 — Gate

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 6 Warnungen | **0 / 6** (Vollneubau) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 685 + neue | **3 831** (KiKern 450, SpeicherEngine 337, EPOS.Kern.Tests **955**, EPOS.UI.Tests **2 089**) — **+146** |
| dieselben Tests unter `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8` | gleich | **grün** |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 124 | **124**, auch unter `en_US` |
| Stapellauf `--alle . --erreichbarkeit` | **11 Masken / 14 Designer**, 11 / 0 / 0 / 0, lokalisiert 7 unverändert | **11 / 14, 7 lokalisiert, 11 ja / 0 nein / 0 verwaist / 0 unklar** |
| `SqlDialektPruefer` | 0 Fundstellen, 1 235 Texte unverändert | **0 von 1 235** (die Welle hat kein SQL) |
| `ChartProben` | 32 unverändert | **32 Bilder, 0 Verstöße** |
| Referenzlauf 1030 / 1007 / 1017 gegen `2026-08-30_B3-Kaskade` | byte-gleich | **PASS, 815 043 Werte; `diff -rq` byte-gleich in allen drei** |
| Wächter `Program.*` im Kern und in den Kernkandidaten | leer | **leer** |
| Wächter `System.Windows.Forms`/`System.Drawing`/`MessageBox.`/`Registry.`/`ProtectedData`/`OleDb` im Kern | leer | **leer** |
| `git diff` auf `ILizenzAblage`, `DpapiLizenzAblage`, `IosLizenzAblage`, `GeraeteId`, `IosGeraeteId` | leer (S1/S2) | **leer** |
| `git grep` auf die drei gefallenen Klassen | nur Kommentare, Protokolle, `help_mapping.txt` und `HilfeKontext` | erfüllt |

**Nach dem Merge von `origin/ios_migration`** (`5a73fd6` — der W15b-Statusblock,
die Anwenderentscheide W15b‑O‑1/O‑2 und der neunzehnte iOS-Lauf) ist das ganze Gate
ein zweites Mal gelaufen: Build **0 / 6**, **3 833** grün (EPOS.UI.Tests 2 091 — die
zwei Fälle kommen aus dem Merge, nicht aus dieser Welle) und ebenso unter `en_US`,
Formularkarte **124**, Stapellauf **11 / 14** mit 11 / 0 / 0 / 0, SQL **0 von 1 235**,
ChartProben **32**, Referenzlauf **byte-gleich in allen drei Projekten**, beide
Wächter leer. **Der Merge lief ohne Konflikt** — die andere Seite hat die beiden
`Resource.resx` nicht angefasst.

**Zur 400-Zeilen-Grenze** (R‑W15b‑1, sinngemäß fortgeführt): `LizenzDialog.razor`
steht bei 450 Zeilen, `LizenzVerwaltungDialog.razor` bei 452. Davon sind **84 bzw. 77
Zeilen XML-Dokumentation** an den Parametern und **14 bzw. 19 Zeilen
Kopfkommentar** — der ausführbare Teil ist **268 bzw. 258 Zeilen**.
`ErststartDialog.razor` hat 168 (88 ausführbar). Beide großen Dateien tragen ihre
Texte als Einzelparameter statt als Bündel, weil die Hülle sie einzeln aus
`MyResource` setzt; ein `LizenzTexte`-Bündel wie `KiChatTexte` wäre der nächste
Schritt, wenn eine weitere Welle daran arbeitet.

## 11 — Windows-Abnahme

Die folgenden Punkte sind **auf dem Entwicklungsrechner nicht prüfbar** (kein
Windows, keine WebView2, kein `.accdb`-Bestand, kein Lizenzserver) und gehören in die
Handprobe. **Die Reihenfolge ist die der Arbeitsanweisung; Punkt 1 ist der
Abnahmepunkt der Welle.**

| # | Was | Erwartung |
|---|---|---|
| **1a** | **Erststart auf einem echten `.accdb`-Bestand.** `Kenndaten.sqlite` löschen bzw. umbenennen, `Kenndaten.accdb` liegen lassen, starten | Der Assistent geht auf — **besitzerlos, mit Taskleisteneintrag, in Bildschirmmitte**. Der Kopftext nennt Ordner und die drei Dateinamen. „Jetzt umstellen" sperrt beide Knöpfe, der Balken läuft unbestimmt, das Protokoll füllt sich, **das Kreuz ist weg und Alt+F4 wirkungslos**. Am Ende schließt sich das Fenster von selbst, und das Programm startet normal |
| **1b** | Derselbe Lauf, aber **Fehlschlag erzwingen** (z. B. `Kenndaten.vor-sqlite.accdb` vorher anlegen) | Das Fenster schließt sich ebenfalls; danach kommt die Meldung „Die Datenbank wurde nicht umgestellt…" mit `LetzteMeldung` und ggf. „Bericht: …", und das Programm endet |
| **1c** | **Windows Sandbox OHNE WebView2** (Nachweis aus `Umsetzung_iU8_Nachweise.md:230` wiederholen) | **Eine `MessageBox` „Microsoft Edge WebView2 Runtime fehlt"** mit der Bezugsquelle, dann Programmende — **kein leeres beiges Fenster** |
| **2** | **Erstzustimmung**: Registry-Wert `HKCU\Software\wp-plan\LizenzZugestimmt` löschen, starten | Der Lizenzdialog geht **besitzerlos** auf, Titel „EPOS-Plan - Lizenzvereinbarung", in der Fußzeile der Bestätigungshinweis, rechts [Zustimmen] [Ablehnen] |
| **2a** | „Ablehnen" | Das Programm endet, der Registry-Wert bleibt leer |
| **2b** | „Zustimmen", danach neu starten | Der Dialog kommt nicht wieder; der Wert steht als `<Fassung> \| yyyy-MM-dd HH:mm` |
| **2c** | Registry-Zweig unlesbar machen (Rechte entziehen) | Das Programm startet **trotzdem** — „im Zweifel den Start nicht blockieren" (E‑15) |
| **3** | **Menü Administration → Lizenz…**: Aktivieren mit echtem Schlüssel und E-Mail | Statuszeile „Aktivierung läuft…", danach „Die Lizenz wurde erfolgreich aktiviert.", **das Schlüsselfeld ist leer**, Status grün, Detailzeile mit Lizenz-Id, Firma, Benutzer und Gerätename |
| **3a** | `.lic`-Datei laden | Beide Felder gefüllt, Hinweis „Lizenzdatei geladen — bitte mit ‚Jetzt aktivieren' abschließen." |
| **3b** | Testversion anfordern (ohne Token) | Knopf bedienbar; mit gültiger Adresse „Der Test-Lizenzschlüssel wurde per E-Mail versandt." |
| **3c** | Gerät lösen (mit Token) | **Rückfrage** mit Vorgabe „Nein"; „Nein" tut nichts, „Ja" löst und der Status springt auf rot |
| **3d** | Netz abschalten, Gerät lösen | „Der Lizenzserver ist zurzeit nicht erreichbar — bitte später erneut versuchen."; das Token bleibt liegen |
| **3e** | Ungültige Adresse (`name@firma`) | Meldung **mit** der Adresse, **ohne** den Schlüssel; kein Serverlauf |
| **4** | **Menü Hilfe → Lizenz** | Drei Reiter, Kopfzeile, in der Fußzeile Lizenzstand und Quelle/Stand |
| **4a** | „Datei wählen…", eine `.rtf` wählen | Der Hinweistext nennt den Pfad; die Quelle in der Fußzeile wechselt; der Pfad ist beim nächsten Öffnen gemerkt |
| **4b** | Ohne Vertragsdatei, **mit Netz** öffnen | Zuerst der Ladehinweis, dann **von selbst** der geholte Vertragstext samt „Stand …" |
| **4c** | Ohne Vertragsdatei, **ohne Netz**, nach 4b | Der Zwischenspeicher steht sofort da |
| **4d** | „Speichern unter…" auf jedem der drei Reiter | Je eine `.txt` mit dem Inhalt der aktiven Karte; danach die Meldung „Gespeichert: …" **im Dialog** |
| **4e** | „Drucken…" | Der Druckdialog der WebView; im Druckbild **ohne** Reiterleiste und **ohne** Knopfleiste |
| **4f** | „Lizenz aktivieren…" | Die Verwaltung erscheint **als Abdunkelung im selben Fenster**, nicht als zweites Fenster; Esc führt zurück |
| **4g** | Ein Verweis im Reiter „Rechtliche Hinweise" (Impressum, Datenschutz) | Öffnet im Systembrowser |
| **5** | Sprache auf Englisch, alles noch einmal | Alle Bedienungstexte englisch; **die Rechtstexte bleiben deutsch**, darüber steht „Binding version in German." |
| **5a** | 125 % Skalierung | Scharf (die DPI-Insel greift im modalen Lauf) |
| **5b** | Esc je Ebene | Verwaltung → Lizenzdialog → Fenster; der Erststart lässt sich **während des Laufs** nicht schließen |

## 12 — Offene Punkte

| Nr | Punkt |
|---|---|
| **iF30** *(neu)* | **Lesemodus-Durchsetzung.** `LizenzManager.DarfSchreiben()` hat bis heute genau einen Leser (`KiAusfuehrer.Schreibrecht`, B7); weder Simulation noch Projektanlage noch ein Speicherweg fragt. Das Konzept verlangt es (§ 6), der Umsetzungsstand führt es seit 01.08.2026 als offen. **Nach W16**, weil die Zahl der anzufassenden Stellen bis dahin sinkt und ihre Form (Razor statt WinForms) einheitlich wird. Ebenfalls dort: die Warnstufen 30/14/7 Tage vor Ablauf. |
| **W15c‑O‑1** | **Der Vertragsendpunkt** (E‑17, B27). `LizenzTextCtrl.ONLINE_QUELLE` ist eine Zeile; die Umstellung auf `epos/v1/vertrag` wartet auf Server 1.4.0. **Entschieden 04.09.2026 (Empfehlung angenommen):** umstellen, sobald der Lizenzserver 1.4.0 im Betrieb ist — die eine Zeile und ihr Zeuge; bis dahin bleibt die AGB-Seite. |
| **W15c‑O‑2** | **Ein `LizenzTexte`-Bündel.** Die zwei großen Komponenten trugen ihre Anzeigetexte als **18 bzw. 29** Einzelparameter (die Schätzung des offenen Punkts lautete 25 bzw. 20). Ein Bündel wie `KiChatTexte` (W15b) würde beide unter 350 Zeilen bringen; das ist eine Aufräumarbeit, keine Fachänderung. **Entschieden 04.09.2026 (Empfehlung angenommen):** umsetzen **nach W16** als eigener Commit, mit den Dialogfällen als Netz — solange der Rahmen der Welle 16 an den Hüllen arbeitet, nicht parallel. **Umgesetzt 04.09.2026** (Commit `2434322`): `LizenzTexte` — EIN Bündel für **beide** Masken (die `LIZ_*`-Texte der Verwaltung unter `.Verwaltung`, damit `LIZR_BTN_AKTIVIEREN` und `LIZ_BTN_AKTIVIEREN` nicht denselben Namen brauchen). Anders als `KiChatTexte` füllt es sich **ohne Angabe selbst** aus `MyResource` in der Oberflächensprache (Linie `Menuepunkt.TextFuer`); die Hüllen überschreiben nur den Fenstertitel, weil er den Produktnamen mitführt. `LizenzDialog` **349 Z.** (vorher 449), `LizenzVerwaltungDialog` **347 Z.** (vorher 451). Kein Dialogfall gestrichen, einer neu (`LizenzTexteTests` — die Selbstfüllung über **alle** Eigenschaften beider Sätze in de und en); Gate 4 003 grün, auch unter `en_US`. |
| **W15c‑O‑3** | **Die Textsuche im Vertragstext** (E‑12). Entfallen; wenn sie zurück soll, ist sie als Markierung im HTML neu zu bauen. |
| **W15c‑O‑4** | **Der Lizenzeinstieg auf iOS** (E‑4, B11). `AppWurzel` kennt keinen; die Komponenten sind gebaut und würden dort unverändert laufen. Gehört zu iU11. |
| **W15c‑O‑5** | **`Form_HelpPopup` ist damit die letzte WinForms-Maske**, die weder mit W15c noch mit W16 fällt (Entscheid W15b‑E‑2); sie geht mit `HelpCatalog`/`HelpExtender` in iU11. |

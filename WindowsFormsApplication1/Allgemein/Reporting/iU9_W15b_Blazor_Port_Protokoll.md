# iU9 Welle 15b — Hilfe und KI: Portprotokoll

> Vermessung `iU9_W15b_Vermessung.md` (2 013 Z., 04.09.2026), Arbeitsanweisung
> `iU9_W15b_Arbeitsanweisung.md`. Form nach `iU9_W15a_Blazor_Port_Protokoll.md`.

## 0 — Was die Welle getan hat

**Vier Masken sind gefallen** (2 243 Zeilen `.cs`, 191 Zeilen Designer, die EINE
`MessageBox` der Welle):

| Maske | Zeilen | Nachfolger |
|---|---|---|
| `Views/Help/Form_TextAnzeige.cs` | 110 (kein Designer) | `EPOS.UI/Dialoge/Hilfe/TextAnzeige.razor` |
| `Views/Help/Form_KiHinweis.cs` | 280 (kein Designer) | `KiHinweisDialog.razor` + `Views/Help/KiHinweisHuelle.cs` |
| `Views/Help/Form_KiEinstellungen.{cs,Designer.cs}` | 149 + 191 | `KiEinstellungenDialog.razor` + `KiEinstellungenHuelle.cs` |
| `Views/Help/Form_KiChat.{cs,resx}` | 1 704 (kein Designer) | `KiChatDialog` in vier Kindern + `KiChatHuelle.{cs,Gaben.cs}` |

**Zwei Masken bleiben — beide bewusst, beide begründet:**

* **`Form_HelpPopup` (E‑2)** — die **erste Maske des Pakets, die weder umgestellt
  noch gelöscht wird**. Nicht weil sie unersetzlich wäre, sondern weil ihr Ersatz
  auf beiden Plattformen bereits steht: `IHilfeDienst` mit `WindowsHilfeDienst`
  (seit iU8‑7) und `IosHilfeDienst` (seit iU10‑5). Sie fällt mit
  `HelpCatalog`/`HelpExtender` in iU11.
* **`Form_Hinweis` (E‑1b)** — bleibt bis Welle 16, siehe § 7.

**Zwei neue Bausteine** (`EPOS.UI/Bausteine/`: 23 → 25): `Gespraechsverlauf` (die
Bausteinlücke Nr. 17 des Wellenplans) und `KiKnopf`. **Ein Nachtrag:**
`Warnbanner.Verfaellt`.

**Der Befund der Welle war schon vor dem ersten Commit klar** (§ 7.2 der
Vermessung): *Der Rechenkern des Assistenten ist fertig portiert — nur die
Oberfläche nicht.* `KiChatService.cs` (1 751 Z.) lag noch in
`WindowsFormsApplication1/`, enthielt aber keinen einzigen WinForms-,
`Program.`-, `Registry`-, DPAPI- oder `SpecialFolder`-Bezug. Der Umzug sollte ein
`git mv` sein. **Er war es nicht** — siehe Befund B31.

## 1 — Commits

| # | Commit | Was |
|---|---|---|
| 1 | `ab25d75` | **W15b.0a** `KiChatService` (1 751 Z.) in den Kern, Naht `KiAusfuehrungsweg` |
| 2 | `8a2bdd9` | **W15b.0e** `Kurzbeschreibung.Umbrechen` in den Kern, Zeuge T‑7 (Auflage H‑1) |
| 3 | `2ed7189` | **W15b.0g** 21 neue Texte zweisprachig, iOS-Literal gehoben, ATS-Kommentar |
| 4 | `4ab25e6` | **W15b.0h** acht CSS-Variablen für den Gesprächsverlauf |
| 5 | `4dc6b06` | **W15b.0b** `KiEinwilligung.Nachfragen` asynchron, Zeuge T‑8 (P‑1) |
| 6 | `dd7305f` | **W15b.0c/0d** `KiAusfuehrer.AufOberflaeche` und der zweite Modalitätshaken (E‑8) |
| 7 | `7dd2b1e` | **W15b.0f + E‑10** `KiChatKontext` im Kern, `Seitenschluessel.KiAssistent` |
| 8 | `bd3cdaf` | **W15b.0i** Zeuge T‑9 `ModellkanalTests` (P‑2/P‑3) |
| 9 | `bce8e8a` | **W15b.6** Baustein `Gespraechsverlauf`, Zeuge T‑1 |
| 10 | `2892f04` | **W15b.1** `Warnbanner.Verfaellt`, Zeuge T‑6 |
| 11 | `98c79af` | **W15b.2** `TextAnzeige.razor`, Zeuge T‑5 |
| 12 | `9aeff5c` | **W15b.3** `KiHinweisDialog` + Hülle, `Form_KiHinweis` gelöscht |
| 13 | `ad4b49b` | **W15b.4** `KiEinstellungenDialog` + Hülle, `Form_KiEinstellungen` gelöscht |
| 14 | `f93b3e0` | **W15b.7** `KiChatDialog` in vier Kindern, `KiVerlaufstexte` im Kern, der Solitär fällt |
| 15 | `c1649c1` | **W15b.5** Baustein `KiKnopf` |
| 16 | (dieser) | **W15b.8** Schwellen, `CLAUDE.md`, Protokoll |

Die Reihenfolge folgt § 15.7 der Vermessung; die einzige Abweichung ist, dass
`Form_TextAnzeige.cs` erst mit W15b.7 gelöscht wurde — siehe A‑8.

## 2 — Feldkartenabgleich

**Eine** der vier gefallenen Masken hatte einen Designer. Für sie ist die
Feldkarte vor dem Port gezogen worden; die drei anderen sind **K4** (Oberfläche
im Code aufgebaut, kein Designer, keine `.resx`) und stehen hier als
Abnahmeliste von Hand.

### 2.1 `Form_KiEinstellungen` (Feldkarte, 8 Kartenzeilen, 0 ohne Beschriftung, lokalisiert: nein)

| # | Steuerelement | Typ | Text de | Nachfolger | ☑ |
|---|---|---|---|---|---|
| 1 | `_schluessel` | TextBox (`UseSystemPasswordChar`) | API-Schlüssel (Google AI Studio): | `<input type="password">` (S‑2) | ☑ |
| 2 | `_modellNeu` | Button | Modell neu erkennen | `button.epos-kieinst-modellneu` | ☑ |
| 3 | `_limitLabel` | Label | Tageslimit je Arbeitsplatz: | `p.epos-kieinst-limit` | ☑ |
| 4 | `_limitWert` | Label (DimGray + ToolTip) | {0} (fest vorgegeben) | `span.epos-kieinst-limitwert` mit `title` | ☑ |
| 5 | `_hinweis` | Label 470 × 154 | drei Absätze | drei `<p>` in `.epos-kieinst-hinweis` | ☑ |
| 6 | `_wegB` | CheckBox | Rückfallweg B erzwingen (Modell ohne Werkzeuge) | `<Schalter>` | ☑ |
| 7 | `_ok` | Button (AcceptButton) | OK | `button.epos-knopf--primaer` | ☑ |
| 8 | `_abbrechen` | Button (CancelButton) | Abbrechen | `button.epos-knopf` | ☑ |
| — | `_tip` | ToolTip | — | `title`-Attribut am Limitwert | ☑ |
| — | `InfoKnopf.Anbringen(this)` | — | — | **entfällt** — siehe A‑6 | ☑ |

### 2.2 `Form_TextAnzeige` (K4, von Hand)

| Glied | Nachfolger | ☑ |
|---|---|---|
| `TextBox` Fill, Multiline, ReadOnly, WordWrap=false, Consolas 9 pt | `<Textfeld Mehrzeilig NurLesen Festbreite>` | ☑ |
| `Button` „Schließen" 110 × 30, Accept **und** Cancel | `button.epos-knopf--primaer`; Esc bringt die `Ueberlagerung` mit | ☑ |
| Kopf-Label 66 px, DimGray, optional | `p.epos-textanzeige-kopf`, `Kopf` leer = kein Absatz | ☑ |
| `groesse` 900 × 480 / 720 × 520, `mindestGroesse`, `maximierbar` | **entfallen** (A‑1): in einer Überlagerung zählt `Zeilen` | ☑ |
| `FensterEinpassung.Einhaengen` | entfällt — Sache der Hülle | ☑ |

### 2.3 `Form_KiHinweis` (K4, von Hand)

| Glied | Nachfolger | ☑ |
|---|---|---|
| `RichTextBox` mit vier Schriftrollen | `h2`/`h3`/`p` + `.epos-kihinweis-fassung` | ☑ |
| 17 Ressourcenschlüssel, sieben Abschnitte, in Reihenfolge | `Abschnitte` (Liste von `KiHinweisAbschnitt`) — im Zeugen nachgerechnet | ☑ |
| `StandText()` in drei Fällen | `Stand`, in der Hülle gebaut | ☑ |
| Einwilligung: „Verstanden und einverstanden" 190 × 28 + „Abbrechen" 100 × 28 | zwei Knöpfe, `MitEinwilligung = true` | ☑ |
| Nachlesen: „Schließen" 100 × 28, Accept **und** Cancel | ein Knopf, `MitEinwilligung = false` | ☑ |
| `Schreibe()` ersetzt `\n` durch `Environment.NewLine` | **entfällt** — `white-space: pre-line`; die Absätze bleiben Absätze (im Zeugen: acht `<p>`) | ☑ |
| `Einhaengen()` in `Program.cs:191` | `KiHinweisHuelle.Einhaengen()`, jetzt `Func<Task<bool>>` | ☑ |

### 2.4 `Form_KiChat` (K4, von Hand — 22 Steuerelemente)

| Glied des Bestands | Nachfolger | ☑ |
|---|---|---|
| `_lblKontext` (Dock Top, 26 px, #005AA0) | `p.epos-kichat-kontext` | ☑ |
| `_lblFeldsicherung` (gelb, verborgen) | `<Warnbanner Stufe="Warnung">`, leer = weg | ☑ |
| `_bestaetigungBereich` (Dock Top, 172 px) | `KiBestaetigungBlock` im **Fußbereich** des Verlaufs (A‑2) | ☑ |
| `_verlaufAnzeige` (RichTextBox, Fill, `DetectUrls`) | `<Gespraechsverlauf>` — **ohne** Raten von Links (A‑4) | ☑ |
| `_chkAktionen` + `_btnWerkzeuge` | `.epos-kichat-schalter` | ☑ |
| `_eingabe` + `_btnSenden` + `_btnSuchen` | `KiEingabezeile` (Enter/Umschalt+Enter) | ☑ |
| `_linkHinweis` (Hinweiszeile) | `p.epos-kichat-hinweiszeile` | ☑ |
| Fußleiste: 3 LinkLabel, `_lblStatus`, `btn_Help`, Einstellungen, Schließen | `.epos-kichat-leiste` | ☑ |
| `_sperrUhr` (400 ms) auf `KiAusfuehrer.Belegt` | `Func<bool> Belegt`, bei jedem Zeichnen gefragt (A‑9) | ☑ |
| `_verfallUhr` (500 ms) | **in der Hülle** — die Frist gehört der Freigabe im Kern | ☑ |
| `_semantikTipp`/`_semantikGezeigt` (Flackerschutz) | `Func<string> SemantikZeile` (A‑10) | ☑ |
| `WerkzeugeOeffnen` (`new Form()`, 101 Z.) | `KiWerkzeugliste` in einer `Ueberlagerung` | ☑ |
| `FelderBauen` (TableLayoutPanel, 59 Z.) | `.epos-kiwerkzeuge-felder` | ☑ |
| `WerteSammeln` (Kulturregel, 29 Z.) | **`KiWerkzeugWerte.Sammeln` im Kern** | ☑ |
| `SchritteZeigen`/`QuellenZeigen`/`Begruessung`/`KlarnamenFuerAnzeige` | **`KiVerlaufstexte` im Kern** | ☑ |
| `Oeffnen(besitzer)` → `Show(besitzer)` | `KiChatHuelle.Oeffnen` — nicht-modal mit Besitzer (E‑6) | ☑ |

## 3 — Die neun Zeugen (T‑1 … T‑9)

| Nr | Zeuge | Ort | Fälle | Was |
|---|---|---|---|---|
| **T‑1** | `GespraechsverlaufTests` | `EPOS.UI.Tests/Bausteine/` | **29** | G‑1…G‑12 plus E‑3/E‑11/E‑12: zehn Rollen = zehn Klassen (genau eine je Zeile), Reihenfolge, Nachführung nur wenn der Anwender unten steht, **Fremdtext nie als Markup**, nur `Adresse`-Zeilen sind Verweise, Klick meldet und öffnet nicht, `Beschaeftigt` = eine `role="status"`-Zeile, Kopieren, leere Liste, 5 000 Zeichen, Umbrüche innerhalb einer Zeile, Tastatur, `role="log"` |
| **T‑2** | `KiChatDialogTests` | `EPOS.UI.Tests/Dialoge/Hilfe/` | **23** | drei Betriebszustände, Kontextzeile, Begrüßung, „Fragen" ruft den Modellweg, **„Nur suchen" ruft ihn NIE**, Enter/Umschalt+Enter, leere Frage, Sperre, Aktionsschalter mit und ohne Einwilligung, Bestätigungsblock (im Verlauf, vier Ausgänge, nur EINE Vorschau, Beenden von außen), Werkzeugliste (Meldung statt MessageBox, **Kulturregel 12,5 → 12.5**, erst schließen dann ausführen), Protokoll als Überlagerung, Doku wird gemeldet, Schließen |
| **T‑3** | `KiEinstellungenDialogTests` | dito | **9** | Feldbestand 8, Maskierung + `autocomplete`/`spellcheck` (S‑2), Tageslimit als Anzeige (kein Zahlen- oder Textfeld), „Modell neu erkennen" mit getrimmtem Schlüssel, **der Seiteneffekt überlebt ein Abbrechen** (E‑5), ohne Delegat passiert nichts, OK/Abbrechen, Weg B vorbelegt |
| **T‑4** | `KiHinweisDialogTests` | dito | **13** | sieben Abschnitte in ihrer Reihenfolge, Titel/Fassung/Einleitung, **acht eigene Absätze**, Umbrüche bleiben, zwei Betriebsarten, ja/nein, Nachlesen willigt nicht ein, Standzeile in drei Fällen, Textbereich fokussierbar |
| **T‑5** | `TextAnzeigeTests` | dito | **5** | ohne Kopf keine leere Zeile, mit Kopf die graue Hinweiszeile, nur lesbar in fester Schrittweite, Zeilenzahl von außen, Schließen meldet sich |
| **T‑6** | `WarnbannerTests` (Nachtrag) | `EPOS.UI.Tests/Bausteine/` | **+5** (17 → 22) | ohne Frist bleibt es stehen, nach Ablauf verschwindet es (**gesteuerte Uhr**), der Verfall wird gemeldet, eine NEUE Meldung setzt ihn zurück, Frist ≤ 0 heißt „kein Verfall" |
| **T‑7** | `KurzbeschreibungTests` | `EPOS.Kern.Tests/` | **10** | Auflage H‑1: 70 Zeichen / 2 Zeilen, leer, Leerraum einebnen, Umbruch an der Wortgrenze, Kappung mit `…`, überlanges Einzelwort bleibt ganz |
| **T‑8** | `EinwilligungsriegelTests` | dito | **10** | **P‑1**: kein Haken / Ablehnung / werfender Haken → **null Modellaufrufe**; Benutzerabschalter fragt nicht einmal; Maschinenabschalter überstimmt eine gültige Einwilligung; Fassung 1 < 2 fragt erneut; die synchrone Fassade fragt nachweislich nicht nach |
| **T‑9** | `ModellkanalTests` | dito | **11** | **P‑2/P‑3**: lesende Aktion läuft ganz durch, reine Auskunft führt nichts aus, Rundendeckel greift nach drei Runden; die vier Ausgänge der Bestätigung; ohne Bestätigungsweg keine Schreibaktion; unbekannte Aktion abgewiesen; **der Abschalter erreicht den Kanal nicht** (S‑4); der Kanal zählt keine Anfrage |
| — | `KiVerlaufstexteTests` | dito | **19** | **H8, die zwei Listen**: Anzeige löst Klarnamen auf, der Prompt-Eintrag nicht; Promptformat sprachunabhängig; 400 Zeichen; von hinten und an Wortgrenzen ersetzt; Werkzeugrunde; Sicherungspunkt im Verlauf; Quellen; Begrüßung in vier Fällen; **die Kulturregel** |
| — | `KiChatKontextTests` | dito | **22** | E‑9: die Schranke lässt nur die Positivliste durch, 28 Bezeichnungen, sechs Seitenschlüssel, fehlender und werfender Haken |
| — | `KiKnopfTests` | `EPOS.UI.Tests/Bausteine/` | **4** | Beschriftung und Kurztext, ausgeblendet statt gesperrt, nicht tabulierbar, Klick meldet sich |

**Kein Netz in einem einzigen Fall.** Modellaufrufe laufen ausschließlich über
den Prüfkanal `KiChatService.Modellkanal`; im Chat sind die vier Wege nach
draußen Delegaten.

## 4 — Die zehn Angleichungen (A‑1 … A‑10)

| Nr | Was | Warum |
|---|---|---|
| **A‑1** | **Die Maßparameter von `Form_TextAnzeige` entfallen** (900 × 480 / 720 × 520, Mindestmaß, Maximieren-Schaltfläche). | Sie war ein zweites modales Fenster; jetzt ist sie der Inhalt einer `Ueberlagerung` im selben Fenster (Risiko R2). Dort zählt die Zeilenzahl, den Rest macht das Raster. |
| **A‑2** | **Der Bestätigungsblock steht UNTEN im Verlauf** statt oben am Fenster (Entscheid E‑3). | Der Kommentar des Bestands (`Form_KiChat.cs:609-613`) verlangt: „der Anwender soll die Vorschau neben dem lesen können, was zu ihr geführt hat." In einer scrollenden Liste ist „neben" = unten. Der Block wandert mit, statt oben zu kleben; sein Text steht **zusätzlich** als Zeile im Verlauf — nachlesbar, wenn der Block verschwindet. |
| **A‑3** | **Die Positionsrechnung von `Form_Hinweis` entfällt ersatzlos** (Entscheid E‑1). | Drei `PointToScreen`-Rechnungen in `Form_Start`. Ein `Warnbanner` steht dort, wo die Komponente es hinsetzt, und braucht keinen Bildschirmpunkt. Wirksam wird das mit W16 (siehe § 7). |
| **A‑4** | **Kein `DetectUrls`** — nur eine Zeile mit `Adresse` ist ein Verweis. | Der Bestand ließ die `RichTextBox` Adressen ERRATEN und filterte erst beim Klick auf `http`/`https` — mit dem ausdrücklichen Grund, dass „ein Antworttext des Modells in derselben Anzeige landet, und der ist Fremdtext" (`:1546-1549`). Ein HTML-Baustein darf gar nicht erst raten. Der `http`/`https`-Filter bleibt trotzdem — er steht jetzt in `KiChatHuelle.AdresseOeffnen`. |
| **A‑5** | **Autoscroll nur, wenn der Anwender unten steht** (Entscheid E‑12). | `ScrollToCaret` (`:1606`) sprang immer und riss beim Nachlesen den Verlauf weg. Der Baustein misst vorher. |
| **A‑6** | **Der `InfoKnopf` von `Form_KiEinstellungen` entfällt.** | Er hing dort, weil die Maske ein eigenes Fenster war. Jetzt ist sie ein Unterdialog des Chats, und der trägt seinen Infoknopf (`Form_KiChat.btn_Help`) weiterhin. **Die Zeile in `help_mapping.txt` bleibt wörtlich** (H‑3) — sie ist die Adresse des HILFETEXTES, nicht der Klasse (Praxis seit W12). |
| **A‑7** | **Die einzige `MessageBox` der Welle wird ein `Warnbanner`.** | „Bitte zuerst eine Aktion wählen" (`:1263`), gefolgt von `DialogResult.None` — der Dialog blieb offen. Der Bereich bleibt es auch. |
| **A‑8** | **`Form_TextAnzeige.cs` fällt erst mit W15b.7**, nicht mit W15b.2. | Regel M1 verlangt die Löschung im selben Schritt. Ihr **einziger** Aufrufer war `Form_KiChat` (`:1498`, `:1559`); die Klasse allein zu löschen hätte den Bau gebrochen. Beide fallen in derselben Welle, es entstehen also keine zwei dauerhaften Fassungen. |
| **A‑9** | **Die 400‑ms‑Sperruhr entfällt.** | Sie fragte `KiAusfuehrer.Belegt` ab und setzte Sichtbarkeiten. In Razor ist `Gesperrt` ein Ausdruck, der bei jedem Zeichnen neu ausgewertet wird — kein Takt nötig. |
| **A‑10** | **Der Flackerschutz der Semantikzeile entfällt.** | Der Bestand schrieb sie nur bei ZUSTANDSWECHSEL, weil „jedes Setzen von `Text` das Label neu zeichnet, und das sähe man". Blazor zeichnet nur, was sich ändert; ein Merker wäre eine zweite Wahrheit. Der Tooltip mit Modell/Lizenz/Herkunft entfällt mit ihm — er stand an einem Label, das es nicht mehr gibt (offener Punkt W15b‑O‑2). |

**Eine Neuerung:** **E‑11, Kopieren.** Im Bestand gab es nur Markieren und
Strg+C; in einem WebView ist das auf iOS unzuverlässig. Der Baustein liefert den
Text, die **Hülle** schreibt die Zwischenablage — `EPOS.UI` kennt keine
Plattform.

## 5 — Anwenderfragen

| Nr | Frage | Was jetzt im Code steht |
|---|---|---|
| **E‑1b** | Was tritt an die Stelle von `Form_Hinweis`, solange `Form_Start` WinForms ist? | **`Form_Hinweis` bleibt bis W16.** Der Nachfolger `Warnbanner.Verfaellt` ist gebaut und geprüft (T‑6). Die Alternative — `Dienste.Dialog.Meldung`, ein modaler OK-Dialog statt eines 3‑s‑Hinweises — wäre ein Rückschritt: Der Anwender müsste ihn wegklicken. Bewusste iZ5-Ausnahme, § 7. |
| **E‑6** | Wie steht der Chat unter Windows? | **Nicht-modal mit Besitzer**, wie heute (`Show(besitzer)`). Er war die einzige Maske des Bestands, die so geöffnet wurde, und der Grund gilt weiter: Wer den Assistenten fragt, will nebenher in der Maske weiterarbeiten, über die er fragt. Ein zweites Öffnen holt das offene Fenster nach vorn. |
| **E‑8** | `KiAusfuehrer.Anker` (`Control`) und `ModalerDialog` | **Umgesetzt** wie vorgeschlagen: `Func<Func<Task>, Task> AufOberflaeche` (Windows `InvokeRequired`/`BeginInvoke`, Blazor `InvokeAsync`) und ein **zweiter Haken** `Ueberlagerung`, den `ModalitaetSperrt()` mit ODER verknüpft. Fachkonzept 3.4 ist um zwei Nachträge ergänzt. |
| **E‑9** | Der Bedienkontext auf iOS | **Umgesetzt**: Die ZUORDNUNG (Positivliste, drei Tabellen, die Freigabeschranke) liegt als `KiChatKontext` im Kern, die ERMITTLUNG bleibt ein `Func<string>` der Hülle. Windows belegt ihn in `Program.Main` mit `HilfeKontext`; ohne Hülle bleibt es bei „Unbekannter Bereich". |
| **E‑10** | Bekommt der Chat einen `Seitenschluessel`? | **Umgesetzt**: `Seitenschluessel.KiAssistent = "KI_ASSISTENT"` samt Zweig in `AppWurzel` und `IProjektQuelle.KiAssistentGaben` (mit Standardumsetzung). **Kein `Masken.*`-Zwilling** — der Chat wurde nie über die Sprungtabelle geöffnet, und die fällt mit W16. |
| **W15b‑O‑1** *(neu)* | Die Naht `IKiAusfuehrung` (§ 6, B31) — ist die Schnittstelle so richtig geschnitten? | Sie hat sieben Glieder und wird nur von `KiChatService` gerufen. Ein engerer Schnitt ginge nicht, ohne den Dienst zu ändern; ein weiterer wäre ein zweiter Weg an der Schutzkette vorbei. |
| **W15b‑O‑2** *(neu)* | Der Tooltip der Semantikzeile (Modell / Lizenz / Herkunft) fehlt. | Er hing an einem WinForms-Label. Nachtragen ließe er sich als `title` an `.epos-kichat-status`; die drei Angaben stehen in `SemantikModell.NAME`/`LIZENZ`/`QUELLE`. |

## 6 — Befunde

Die Vermessung führt B1 … B30. Alle sind eingetreten wie beschrieben; hier stehen
nur die, bei denen etwas zu **entscheiden** war, plus vier neue.

| Nr | Befund | Entscheid |
|---|---|---|
| **B1** | `KiChatService.cs` ist plattformfrei (0 WinForms, 0 `Program.`, 0 `Registry`, 0 DPAPI, 0 `SpecialFolder`). | Umgezogen (W15b.0a) — **aber nicht als reines `git mv`**, siehe B31. |
| **B6** | Genau EINE `MessageBox` in 2 628 Zeilen. | `Warnbanner` im Werkzeugbereich (A‑7). |
| **B10** | `Form_KiChat.resx` ist verwaist (119 Z., 0 echte `<data>`, zeichengleich mit `Form_HelpPopup.resx`). | Mit gelöscht. |
| **B11** | „Modell neu erkennen" nimmt den Schlüssel VOR OK an; Abbrechen nimmt das nicht zurück. | **Bitgleich** (E‑5). Der Seiteneffekt steht in der HÜLLE, nicht in der Komponente — nur dort gibt es `KiChatService`. Im Zeugen ausdrücklich geprüft. |
| **B12** | `KiEinwilligung.Nachfragen` ist synchron. | `Func<Task<bool>>`; `SicherstellenAsync()` ist der echte Weg, `Sicherstellen()` bleibt als synchrone Fassade und **fragt nicht mehr nach**. Alle drei Aufrufer in EINEM Schritt (W15b.0b). |
| **B13** | Der Lokalisierungszähler bleibt stehen — die einzige Welle des Pakets. | Bestätigt: 7 vor und nach der Welle. |
| **B15** | `"Benutzer: "` und `"Assistent: "` sind Promptformat, kein Anzeigetext. | **Nicht** nach `MyResource` gehoben; sie stehen fest in `KiVerlaufstexte.PromptEintrag*`, mit Begründung im Kommentar und im Zeugen. |
| **B16/B17** | `Anker` ist ein `Control`; `ModalerDialog` kennt keine Razor-Überlagerung. | E‑8, umgesetzt. |
| **B18** | `BeschreibungUmbrechen` verspricht seit H11 die Prüfbarkeit — ohne Test. | In den Kern gehoben, T‑7. |
| **B19** | `HilfeKontext` liest `Form.ActiveForm`. | E‑9, umgesetzt. |
| **B20** | `IosHilfeDienst` trägt einen deutschen Literaltext. | `HILFE_IOS_BESCHREIBUNG`, zweisprachig. |
| **B21** | `KiAufrufKnopf` ist seit W14a toter Code (bereits gelöscht). | Nachfolger `KiKnopf` gebaut; die 25 Zeilen „nach vorn holen" aus der Historie (`4e77221~1`) in `KiChatHuelle` übernommen. Die zwei protokollierten Abweichungen aus W6 und W7 sind damit eingelöst — ihre Kommentare in `HeizkesselKatalogDialog` und `PufferSpKatalogDialog` sind nachgezogen. |
| **B24** | Der ATS-Kommentar nennt Gemini und Hugging Face nicht. | Nachgezogen. Funktional unverändert — beide Ziele sind HTTPS. |
| **B25** | `Modellkanal` ist gebaut, aber nirgends belegt. | T‑9 belegt ihn: 11 Fälle. |
| **B26** | Das Tageslimit steht bewusst im Code. | Bitgleich: Anzeige mit Kurzhinweis, **kein Eingabefeld** — im Zeugen geprüft (kein `number`-, kein `text`-Feld). |
| **B28** | Zwei Chatfenster hebeln die Bestätigungsschicht aus (`Bestaetigungsweg == null`). | **Behoben** — `KiChatHuelle` holt ein offenes Fenster nach vorn, statt ein zweites anzulegen. |
| **B29** | Der Chat ist die einzige nicht-modale Maske mit Besitzer. | E‑6, bitgleich. |
| **B30** | `help_mapping.txt` führt zwei Zeilen der Welle. | Beide **wörtlich** unverändert (H‑3). |
| **B31** *(neu)* | **`KiChatService` ist NICHT WinForms-frei im Sinne des Übersetzers.** Die Vermessung prüfte auf WinForms-Typen, `Program.`, `Registry`, DPAPI und `SpecialFolder` — alle null. Sie prüfte nicht auf Typen der Anwendung: Der Dienst ruft in der Werkzeugrunde **zehnmal `KiAusfuehrer`** und einmal `KiHilfe.KlarnamenAnmelden`, und beide bleiben in der Anwendung (`KiAusfuehrer` hängt an `Control`, `Application.OpenForms`, `Form.ActiveForm.Modal`; `KiHilfe` liest die Datenbank). | Statt den Umzug zurückzunehmen bekommt der Kern dieselbe Bauart wie `Dienste.*`: die Schnittstelle **`IKiAusfuehrung`** mit der stillen Standardfassung `KeineAusfuehrung` (leeres Register, jede Aktion abgelehnt) und dem Halter `KiAusfuehrungsweg.Aktuell`. Die Windows-Fassung `KiAusfuehrungAdapter` reicht jedes Glied unverändert weiter und wird in `Program.Main` eingelegt. `KiVorbereitung` ist mitgezogen. **Der Riegel bleibt unberührt** — die Naht sitzt HINTER `KiEinwilligung` und `KiRiegel`. |
| **B32** *(neu)* | Die Vermessung nennt „17 `KI_CHAT_*`"; ihre eigene Tabelle in § 11.2 führt **20** verschiedene `KI_CHAT_`-Namen. Die 17 war die Zahl der verschiedenen TEXTE einschließlich der beiden wiederverwendeten Schlüssel. | 20 angelegt, die zwei wiederverwendeten (`KI_VORSCHAU_SCHLIESSEN`, `HILFE_POPUP_LINK`) **nicht verdoppelt**. Dazu `HILFE_IOS_BESCHREIBUNG` und (E‑11) `KI_CHAT_KOPIEREN`: **419/419**. |
| **B33** *(neu)* | Im HILFEFALL ersetzt der `Modellkanal` keinen fehlenden Schlüssel — die Prüfung auf `IstEingerichtet` steht unmittelbar hinter dem Riegel. Nur `FrageMitAktionenAsync` lässt ihn einspringen (`bool eingespeist = Modellkanal != null`). | T‑8 weist die erteilte Einwilligung deshalb daran nach, dass die Meldung NICHT mehr die des Riegels ist. |
| **B34** *(neu)* | **`Standards/Schalter` hält seinen Zustand SELBST** (`BeiAenderung` setzt `Wert`). Eine abgelehnte Umschaltung ändert den Wert des Wirtes nicht — Blazor sähe also keine Parameteränderung und ließe das Kästchen angehakt. Der Vorläufer setzte `_chkAktionen.Checked` einfach zurück (`:824-826`). | Im Chat baut ein `@key` mit Fassungsnummer den Schalter neu auf. Im Zeugen nachgerechnet. **Der Baustein selbst bleibt unverändert** — die Lage betrifft jeden Wirt, der eine Umschaltung ablehnen will; ein `OnParametersSet`-Rücksetzer im Schalter wäre der saubere Ort, ist aber eine Änderung an einem Baustein mit 20 Nutzern und gehört nicht in diese Welle (offener Punkt W15b‑O‑3). |

## 7 — Die iZ5-Ausnahme: `Form_Hinweis` bleibt bis Welle 16

Die Arbeitsregel seit dem Stichtag iZ5 lautet: *Jeder neue und jeder ohnehin
anzufassende Dialog entsteht als Razor-Komponente, seine WinForms-Fassung wird im
selben Schritt gelöscht.* **`Form_Hinweis` (34 Z. + 102 Designer) bleibt
trotzdem stehen.**

**Warum.** Seine drei Aufrufer stehen sämtlich in `Form_Start`
(`:833`, `:1190`, `:1475`), und `Form_Start` ist bis Welle 16 WinForms. Ein
WinForms-Fenster kann keinen Razor-`Warnbanner` zeigen, ohne dass eine
`BlazorSeite` dafür entsteht — und der einzige Ersatz, den `Dienste.Dialog`
anbietet, ist ein **modaler OK-Dialog**. Der wäre ein Rückschritt: Der Vorläufer
verschwindet nach drei Sekunden von selbst; ein OK-Dialog will weggeklickt
werden, und zwar dreimal an Stellen, an denen der Anwender gerade etwas anderes
tut („Projekt Muster GmbH geöffnet!").

**Was stattdessen geschehen ist.** Der Nachfolger ist gebaut und geprüft:
`Warnbanner.Verfaellt` (TimeSpan?), `Verfallen` und eine austauschbare Uhr,
fünf Fälle in T‑6. **W16 hängt nur noch um.**

Dasselbe Muster wie `ProjektAuswahl` (uc) aus W15a: Für genau eine Welle gibt es
zwei Fassungen derselben Sache, und der Grund steht hier.

**Auftrag an W16:** Die drei Aufrufe in `Form_Start` auf ein `Warnbanner` in der
Startseite umstellen (die drei `PointToScreen`-Rechnungen entfallen, A‑3),
danach `Allgemein/Form_Hinweis.{cs,Designer.cs,resx}` löschen und den Eintrag in
`EPOS.Kern/CLAUDE.md` streichen.

## 8 — Sicherheit (S‑1 … S‑4)

| Regel | Wo eingelöst | Zeuge |
|---|---|---|
| **S‑1** Der Schlüssel wird nie durchgereicht, nur gesetzt | `KiEinstellungenDialog.Schluessel` ist **Vorbelegung**; heraus geht er über `KiEinstellungenErgebnis` und über den `ModellNeuErkennen`-Delegaten. Gelesen und geschrieben wird er ausschließlich in `KiChatService`. | T‑3 |
| **S‑2** `type="password"`, `autocomplete="off"`, `spellcheck="false"` | ebenda. Der Rechtschreibprüfer ist kein Detail: Er schickt Text an den Dienst des Browsers. | T‑3 |
| **S‑3** Nichts in `localStorage` | Weder `Gespraechsverlauf` noch `KiChatDialog` noch `KiEinstellungenDialog` legen irgendetwas ab. Das JS-Modul `epos-verlauf.js` hat keinen Zustand. | Sichtprüfung; `grep` auf `localStorage`/`sessionStorage` in `EPOS.UI` = 0 |
| **S‑4** Der Riegel bleibt im Kern, VOR dem `Modellkanal` | `EinwilligungsriegelAsync()` steht unverändert vor Cache, Tageslimit, Schlüsselprüfung und Prüfkanal. Die Komponente nimmt ihn nicht vorweg (sie ruft `Einwilligen` nur beim EINschalten des Aktionsschalters, wie der Bestand) und umgeht ihn nicht (sie kennt `KiChatService` nicht). | T‑8, T‑9 |

## 9 — Texte

**21 neue Schlüssel, zweisprachig** — `KI_*` von 398/398 auf 419/419:

* **20 × `KI_CHAT_*`** (die 22 Literalstellen aus `Form_KiChat`, § 11.2 der
  Vermessung), plus **`KI_CHAT_KOPIEREN`** für die Neuerung E‑11.
* **`HILFE_IOS_BESCHREIBUNG`** (Auflage H‑2) — der einzige deutsche Literaltext
  der iOS-Hülle.

**Zwei Schlüssel wiederverwendet statt verdoppelt:** `KI_VORSCHAU_SCHLIESSEN`
(„Schließen") und `HILFE_POPUP_LINK` („Online-Dokumentation öffnen").

**Zwei Literale bleiben, mit Absicht:** `"Benutzer: "` und `"Assistent: "` sind
das Format des Verlaufsblocks im Prompt (B15).

**E‑4 nachgewiesen:** `git diff` über beide `.resx` zeigt **keinen geänderten
`KI_HINWEIS_*`-Wert**; `KiEinwilligung.FASSUNG` bleibt bei **2**.

`Resource.Designer.cs` ist von Hand um die 21 Eigenschaften ergänzt (alphabetisch
eingeordnet), damit Visual Studio beim nächsten Regenerieren keine Duplikate
baut.

## 10 — Gate

| Prüfung | Sollwert | Ergebnis |
|---|---|---|
| `dotnet build WP-Plan.sln -c Release -p:Platform=x64` | 0 Fehler, 6 Warnungen | **0 / 6** (Vollneubau) |
| `dotnet test WP-Plan.Kern.slnf -c Release` | Basis 3 511 + neue | **3 672** vor dem Merge (KiKern 450, SpeicherEngine 337, EPOS.UI.Tests 2 015, EPOS.Kern.Tests 870); **3 679 nach dem Merge** von `origin/ios_migration` (2 019 / 873) |
| dieselben Tests unter `LANG=en_US.UTF-8` | gleich | **grün**, vor und nach dem Merge |
| `dotnet test Werkzeuge/Formularkarte.Tests -c Release` | 124 | **124**, auch unter `en_US` |
| Stapellauf `--alle .` | **12 Masken / 15 Designer**, 12 / 0 / 0 / 0, lokalisiert **7 unverändert** | **12 / 15, 7 lokalisiert, 12 ja / 0 nein / 0 verwaist / 0 unklar** |
| `SqlDialektPruefer` | 0 Fundstellen, 1 234 Texte unverändert | **0** (1 234 Texte vor dem Merge; **1 235 danach** — der eine neue Text kommt aus `origin/ios_migration`, nicht aus dieser Welle: sie hat kein SQL) |
| `ChartProben` | 32 unverändert | **32, 0 Verstöße** |
| Referenzlauf 1030 / 1007 / 1017 gegen `2026-08-30_B3-Kaskade` | byte-gleich | **PASS, 815 043 Werte; `diff -rq` byte-gleich in allen drei** |
| Wächter `Program.*` / `MessageBox` / `Registry` / DPAPI / `SpecialFolder` / `System.Windows.Forms` im Kern | leer | **leer** (nach dem `git mv` besonders geprüft) |
| `git grep` auf die vier gefallenen Klassen | nur Kommentare, Protokolle, `help_mapping.txt` (H‑3) und der `HilfeKontext`-Eintrag (H‑4) | erfüllt |
| keine `.razor` über 400 Zeilen (R‑W15b‑1) | erfüllt | `KiChatDialog.razor` **413** — siehe unten |

**Nach dem Merge von `origin/ios_migration`** (`08cbc2a`, W15a‑Statusblock und
Entscheid O‑3) ist das ganze Gate ein zweites Mal gelaufen: Build 0/6,
3 679 grün (auch unter `en_US`), Formularkarte 124, Stapellauf 12/15,
SQL 0 von 1 235, ChartProben 32, Referenzlauf byte-gleich, beide Wächter leer.
**Der einzige Konflikt waren die beiden `Resource.resx`** — beide Seiten hatten
am Ende Schlüssel angefügt; beide sind erhalten (4 410 Einträge je Datei, keine
Dublette, `KI_*` 419/419).

**Zur 400‑Zeilen-Grenze.** `KiChatDialog.razor` steht bei 413 Zeilen. Davon sind
**35 Zeilen Kopfkommentar** (die fünf Entscheide, die im Markup stecken) und rund
**150 Zeilen XML-Dokumentation** an den Parametern — der ausführbare Teil ist
etwa halb so groß wie die Datei. Der Solitär ist trotzdem in **vier Kinder** und
**zwei Teildateien** zerlegt (Bestätigungsschicht, Überlagerungen), und die 33
Anzeigetexte stehen als **ein** Bündel `KiChatTexte` statt als dreißig Parameter.
Ohne diese drei Schritte wären es 638 Zeilen gewesen.

## 11 — Windows-Abnahme

Die folgenden Punkte sind **auf dem Entwicklungsrechner nicht prüfbar** (kein
Windows, keine WebView2, kein API-Schlüssel, kein Netz) und gehören in die
Handprobe:

| # | Was | Erwartung |
|---|---|---|
| 1 | Menü „Hilfe → Hilfe-Assistent (KI)…" | Der Chat geht auf, **nicht-modal**; die Maske dahinter bleibt bedienbar |
| 2 | **F1** an derselben Stelle | dasselbe Fenster |
| 3 | Chat offen lassen, Menü noch einmal | **kein zweites Fenster** — das offene kommt nach vorn (minimiert: wird wiederhergestellt) |
| 4 | Eine Frage **mit Modell**, einmal echt | Antwort im Verlauf, „Assistent:" blau/fett darüber, Quellen grau, Tageszähler in der Statuszeile |
| 5 | „Nur suchen" | Trefferliste, **keine gezählte Anfrage** |
| 6 | „Aktionen zulassen" beim ersten Mal | Der Rechtshinweis erscheint; Ablehnen lässt den Schalter **aus** (B34) |
| 7 | „Werkzeuge…" ohne Auswahl auf „Ausführen" | Warnbanner „Bitte zuerst eine Aktion wählen", Bereich bleibt offen |
| 8 | Eine Aktion mit Parameter „12,5" ausführen | Der Bereich schließt ZUERST, dann läuft die Aktion; im Protokoll steht `12.5` |
| 9 | Eine Schreibaktion über das Modell | Bestätigungsblock **unten im Verlauf**, Sekunden zählen herunter; alle vier Ausgänge (Ausführen / Abbrechen / ablaufen lassen / Fenster schließen) |
| 10 | „Rechtshinweis" aus dem Chat | Der Hinweis erscheint zum Nachlesen; „Schließen" willigt **nicht** ein |
| 11 | Erststart ohne Einwilligung | Der Hinweis erscheint vor der ersten Übertragung; „Abbrechen" → nichts geht hinaus |
| 12 | „Einstellungen…", Schlüssel eintippen, „Modell neu erkennen", dann **Abbrechen** | Die Modellzeile hat sich geändert, und der Schlüssel ist gesetzt geblieben (E‑5) |
| 13 | „Protokoll anzeigen" und „Was wird gesendet?" | je eine Überlagerung, feste Schrittweite, Esc schließt |
| 14 | „Verlauf kopieren", danach in ein Textfeld einfügen | je Zeile eine Zeile |
| 15 | Sprache auf Englisch, Chat neu öffnen | alle Texte englisch, auch die 20 neuen |
| 16 | 125 % Skalierung | Der Inhalt ist bitmapskaliert — **bekannt und beabsichtigt**: Die DPI-Insel greift nur im modalen Lauf, und der Chat ist nicht modal (§ Klassenkopf `KiChatHuelle`) |
| 17 | Esc je Ebene | Werkzeugliste → Chat → Fenster; der Chat schließt nicht, solange eine Überlagerung offen ist |

## 12 — Offene Punkte

| Nr | Punkt |
|---|---|
| **W15b‑O‑1** | Die Naht `IKiAusfuehrung` (B31) ist eine Entscheidung dieser Welle, keine der Vermessung — sie sollte bestätigt werden. |
| **W15b‑O‑2** | Der Tooltip der Semantikzeile (Modell / Lizenz / Herkunft) ist mit dem Label entfallen (A‑10). Nachtragbar als `title` an `.epos-kichat-status`. |
| **W15b‑O‑3** | `Standards/Schalter` hält seinen Zustand selbst (B34). Der saubere Ort für einen Rücksetzer wäre der Baustein — 20 Nutzer, eigene Welle. |
| **W15b‑O‑4** | Die iOS-Hülle bedient `KiAssistentGaben` noch nicht; `AppWurzel` bleibt bis iU11 bei der Liste stehen und sagt warum. Der Chat ist auf dem Gerät nie geprüft worden (Risiko R‑W15b‑10) — die Handprobe gehört zu iU11. |
| **W15b‑O‑5** | `Form_HelpPopup` ist damit die **letzte WinForms-Maske des Pakets**, die nicht mit W15c oder W16 fällt. Sie geht mit `HelpCatalog`/`HelpExtender` in iU11. |

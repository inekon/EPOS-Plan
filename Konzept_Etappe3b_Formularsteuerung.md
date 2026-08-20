# Umsetzungskonzept: Etappe 3b — Formularsteuerung (EPOS-Plan)

Stand: 2026-08-20, Rev. 1 — **zur Abnahme** ·
Auftraggeber: Philipp (INEKON) ·
Grundlage: Fachkonzept `Konzept_KI-Assistent_Aufgabensteuerung.md` **Rev. 2, Kapitel 11** (Formularsteuerung,
Feldsicherung, Aufrufknopf; Grenzen 11.7 und alle Vorschläge am 20.08.2026 abgenommen) und Prüfung des
Arbeitsbaums `Documents\WP-Plan` am 20.08.2026 (nach Etappe 3: Riegel, Bestätigungsschicht und drei
Schreibaktionen liegen im Code). Auftragslage: Umsetzung beauftragt, auf Wunsch des Auftraggebers **zurückgestellt,
bis dieses Umsetzungskonzept abgenommen ist**. Ergänzung vom 20.08.2026 (Fachkonzept **11.9**): Hilfe-Betrieb —
KI-Oberfläche per Konfiguration ausblendbar, Aufrufknopf wechselt auf „Hilfe" → Arbeitspaket **F5**.

> **Belegregel.** Wie im Fachkonzept: Jede Bestandsaussage trägt `Datei:Zeile`; Pfade ohne Präfix liegen unter
> `WindowsFormsApplication1\`. Nicht Verifizierbares ist als **(Annahme)** markiert. Für dieses Konzept wurde
> ausschließlich gelesen.

---

## 1. Ziel und Umfang

Am Ende der Etappe kann der Anwender in einer geöffneten Startmaske (z. B. Heizkessel bearbeiten) den
Assistenten über einen dezenten Knopf aufrufen und sagen: *„Trag bei den Wartungskosten 1200 ein und speichere."*
Der Assistent zeigt „Wartungskosten · 850 → 1200 · Knopf ‚Speichern' wird ausgelöst", der Anwender bestätigt mit
einem Klick, die Maske übernimmt den Wert **durch ihre eigene Knopfprüfung** — und der Anwender kann vorher
fragen: *„Welche Felder gibt es hier, und was bedeutet Betriebsbereitschaftsverlust?"*

Umfang: die vier Arbeitspakete F1–F4 (Abschnitt 3) für die **Startmasken** `Form_Heizkessel_Bearbeiten`,
`Form_PV`, `Form_PufferSp_Bearbeiten`, `Form_WP`. Nicht in dieser Etappe: weitere Masken (kommen maskenweise),
Rechenaktionen (Etappe 4), globales F1 (Etappe 5).

## 2. Bestandsanker — worauf die Umsetzung aufsetzt

| # | Fakt | Beleg | Bedeutung für 3b |
|---|---|---|---|
| B1 | Bestätigungsschicht ist im Code: Vorschaublock mit Ausführen/Abbrechen/Verfall im Chat; Freigabeweg `KiFreigabe`; Ausführung nur über `KiAusfuehrer.AusfuehrenAsync(aufruf, freigabe, …)` | `Views\Help\Form_KiChat.cs:373`, `:453`; `Allgemein\KI\KiAusfuehrer.cs:376`, `:464` | 2F-Aktionen brauchen **keine neue UI** — die Vorschau der Aktion füllt den vorhandenen Block |
| B2 | Riegel mit zwei Grenzen; Bestätigungspflicht hängt an der Stufe, nicht an Namenslisten | `KiKern\KiRiegel.cs:33`, `:57`, `:109` | 2F wird als `Schutzstufe.Schreiben` deklariert — Riegel bleibt unangetastet |
| B3 | Schreibrechtsprüfung zentral: `KiAusfuehrer.Schreibrecht = LizenzManager.DarfSchreiben` | `KiAusfuehrer.cs:201`, `:482` | gilt automatisch auch für 2F |
| B4 | **Modalitätssperre:** Aktionen werden abgewiesen, solange ein modaler Dialog offen ist (`Form.ActiveForm.Modal`) | `KiAusfuehrer.cs:214`, `:650-669` | **Muss für Dialogaktionen umgedreht werden** — sie *verlangen* die offene Zielmaske (3.3/F3) |
| B5 | Eingabeprüfung sitzt am Aktionsknopf: `Program.ZahlPruefen`/`GanzzahlPruefen` (`:272`, `:291`); Beispiel Heizkessel: `EingabenPruefen` prüft 15 Felder beim Speichern | `Program.cs:272`, `:291`; `Views\Heizkessel\Form_Heizkessel_Bearbeiten.cs:526-540` | Der Assistent setzt nur Text — geprüft wird beim Knopf, exakt wie bei Handeingabe; „12.5" und „12,5" kommen identisch an (Commit `fff27c3`) |
| B6 | UI-Thread-Marshalling und Einläufigkeit vorhanden: `AufUiThread`, `Anker`, `Interlocked`-Lauf | `KiAusfuehrer.cs:614`, `:233`, `:138` | Feldsetzen nutzt denselben Weg |
| B7 | Chat-Öffnung mit Besitzer vorhanden: `Form_KiChat.Oeffnen(IWin32Window besitzer = null)` | `Views\Help\Form_KiChat.cs:1354` | Der Aufrufknopf übergibt die Maske als Besitzer — Chat bleibt neben modalem Dialog bedienbar |
| B8 | Hilfetexte je Control-Slug: `WordPressHelpCatalog.Get(slug)` (Tooltip + Artikel-URL, Offline-Cache); Zuordnung `HelpExtender`/`help_mapping.txt` (Datei nicht im Repo) | `Allgemein\Hilfe\HelpCatalog.cs:194`, `:209`, `:250-304` | Quelle fürs **Erklären**; Katalog referenziert Slugs nur optional |
| B9 | Startmasken führen teils **nicht-ASCII-Controlnamen** (`tb_Wirkungsgrad_Öl`) und sind **BOM-loses cp1252** | `Form_Heizkessel_Bearbeiten.cs:528`; Encoding-Befund 18.08.2026 | Katalog-Controlpfade müssen exakt stimmen; Editieren nur byte-erhaltend (Abschnitt 6) |
| B10 | Aktionsdeklaration erzwingt Vorschau ab Stufe 2 schon beim Registrieren | `KiKern\KiAktion.cs:63-71` | 2F-Aktionen liefern die Feldliste „alt → neu" als Vorschau — Pflicht, nicht Kür |
| B11 | **KI-Abschalter existiert:** `KiEinwilligung.Abgeschaltet` (HKCU `KiDeaktiviert`, HKLM übersteuert und ist aus der App nicht lösbar); unterbindet jede Übertragung; blendet heute den Menüeintrag komplett aus (Auswertung beim Aufklappen) | `Allgemein\KI\KiEinwilligung.cs:80-93`, `:145-147`; `MDIMainForm.cs:259-264` | Träger des **Hilfe-Betriebs** (F5) — wiederverwenden, kein neuer Schalter |

## 3. Arbeitspakete

### F1 — KiKern: Dialogdeklaration und Feldsicherung (S–M)

**Neu in `KiKern\` (bleibt referenzfrei — keine WinForms-, keine DB-Referenz):**

* `KiDialog` (Maskenname = Typname der Form, Anzeigename, Felder, Knöpfe, optionale `Knopfposition` für den
  Aufrufknopf), `KiDialogFeld` (logischer Name, Controlpfad, Anzeigename, Typ — Wiederverwendung
  `KiParameterTyp` —, Einheit, `leerErlaubt`, Erläuterung, optionaler Hilfe-Slug), `KiDialogKnopf`
  (logischer Name, Controlpfad, Anzeigename). Konstruktorprüfungen nach dem Muster von `KiAktion`
  (`KiKern\KiAktion.cs:56-71`): keine Duplikate, Pflichttexte, gültige Namen.
* `KiDialogKatalog`: Nachschlag nach Maskenname, Aufzählung für `dialog_lesen`; **Löschknöpfe sind per Bauart
  nicht deklarierbar** (Namensprüfung weist `*loesch*`/`*delete*`-Controlpfade ab — zweite Linie zur
  Positivliste, Fachkonzept 11.3).
* `KiFeldsicherung`: `Aktiv` (Standard **an**), einmalige `Abschalten(grund)`-Methode (kein Wiedereinschalten
  zur Laufzeit — der Schalter ist ein Startzustand, kein Betriebsmodus), Klartexte für Chat-Hinweis und
  Protokollvermerk in `KiTexte`.
* `KiAktion` erhält das optionale Flag **`formularaktion`** (Standard `false`). Stufe bleibt
  `Schutzstufe.Schreiben`; `KiRiegel` wird **nicht** angefasst (B2).
* Blocktext-Bauer (reine Funktion): aus Maskenname + Liste (Feld-Anzeigename, alter Text, neuer Text) bzw.
  Knopf-Anzeigename entsteht der Bestätigungstext „Feld · alt → neu" — nie aus Modelltext.

**Tests (`KiKern.Tests`):** Deklarationsprüfungen (Duplikate, Löschknopf-Abweisung), Feldsicherungszustand,
Blocktexte, `formularaktion`-Verhalten im Zusammenspiel mit `KiRiegel.BrauchtBestaetigung`. Bestehende Tests
(298+) bleiben grün; `dotnet test` mit eigenem `ArtifactsPath` (x86/x64-Kollision).

### F2 — Aufrufknopf (S)

* Neu `Allgemein\KI\KiAufrufKnopf.cs`: `Anbringen(Form)` nach Fachkonzept 11.8 — ≈ 24 px, schlichte
  Beschriftung **„KI"** (Festlegung 20.08.2026, cp1252-sicher, einheitlich auf allen Systemen),
  `FlatStyle.Flat`, kein `TabStop`, `Anchor Top|Right`, gedämpft/Hover-betont, Tooltip „KI-Assistent" aus
  `MyResource` (de **und** en, ans Dateiende, Designer nachziehen). Klick → `Form_KiChat.Oeffnen(form)` (B7);
  ist der Chat schon offen, holt der Klick ihn nach vorn.
* **Zweigestaltig (Fachkonzept 11.9):** Der Helfer liest beim Anbringen `KiEinwilligung.Abgeschaltet` (B11) —
  gesetzt heißt Beschriftung **„Hilfe"**, Tooltip „Hilfe", gleiche Gestaltung, gleicher Platz, gleiches Ziel
  (das Fenster entscheidet selbst über seinen Betrieb, F5). Knopfbreite passt sich der Beschriftung an.
* Verdrahtung: **ein** Aufruf je Startmaske im Konstruktor nach `InitializeComponent()` — Designer-Dateien
  bleiben unberührt (Hausregel). Die vier Dateien sind cp1252 → byte-erhaltend editieren (Abschnitt 6).
* Kollisionsprüfung oben rechts je Startmaske; weicht eine Maske ab, hält ihr Katalogeintrag (F3) die
  Position im Feld `Knopfposition` fest.

### F3 — Dialogkatalog, Aktionen, Feldsetzweg (M)

* **Katalogeinträge** unter `Allgemein\KI\Dialoge\` für die vier Startmasken. Feldumfang v1 = die von der
  Knopfprüfung erfassten Eingabefelder (Heizkessel z. B. die 15 aus `EingabenPruefen`,
  `Form_Heizkessel_Bearbeiten.cs:526-540`) plus die Knopf-Positivliste (Heizkessel: `btn_Speichern`,
  `btn_Speichern_Unter`, `btn_Ueberschreiben`, `btn_Abbrechen` — `:43-50`, `:325`, `:352`; **kein**
  Löschknopf).
* **Fünf Aktionen** in `Allgemein\KI\Aktionen\KiAktionenDialog.cs` (Fachkonzept 11.4): `dialog_lesen`,
  `dialog_parameter_erklaeren` (Stufe 1) sowie `feld_setzen`, `formular_ausfuellen`,
  `dialog_aktion_ausfuehren` (Stufe `Schreiben` + `formularaktion`). Erklären zieht Katalog-Erläuterung und,
  wo ein Slug deklariert ist, den Hilfetext über `WordPressHelpCatalog.Get` (B8).
* **Feldsetzweg im `KiAusfuehrer`:** Zielmaske über `Application.OpenForms` per Typname finden (genau eine
  Instanz, sonst Klartext-Ablehnung); Controlpfad rekursiv auflösen (Muster `FindControlRecursive`,
  `Allgemein\Hilfe\HelpCatalog.cs:306`); auf dem UI-Thread (B6) setzen — v1 unterstützt `TextBox.Text`,
  `CheckBox.Checked`, `ComboBox` (Auswahl per Anzeigetext); alles andere sowie `ReadOnly`/`Enabled=false`
  wird mit Klartext abgelehnt. Kein `SendKeys`, keine Fensternachrichten.
* **Modalitätsweiche (B4):** Aufrufe mit `formularaktion` sind von der Modalitätssperre ausgenommen; sie
  verlangen stattdessen, dass die Zielmaske offen **und das aktive Fenster** ist. Für alle übrigen Aktionen
  bleibt die Sperre unverändert.
* `maske_oeffnen` für genau die vier Startmasken freischalten (Fachkonzept 5.1, Positivliste).

### F4 — Feldsicherungsschalter und Abschluss (S)

* `Program.cs`: Befehlszeilenschalter **`/ki-feldsicherung-aus`** → `KiFeldsicherung.Abschalten(...)` beim
  Start — die einzige Stelle, die abschalten kann (Abnahme 20.08.2026).
* Werkzeugrunde/Chat: Bei abgeschalteter Sicherung laufen `formularaktion`-Aufrufe ohne Feldbestätigung;
  das Chatfenster zeigt dauerhaft „Feldsicherung AUS", jede Protokollzeile trägt den Vermerk. Die
  Stufe-2-Bestätigung der DB-Schreibaktionen bleibt nachweislich unberührt.
* **Prüfbuild und Testlauf:** Full-MSBuild VS 2022 (`C:\Program Files\Microsoft Visual Studio\2022\Community\
  MSBuild\Current\Bin\MSBuild.exe`), **`/p:Platform=x64`** (ACE-OLEDB ist auf diesem Rechner nur 64-bittig
  registriert), 0 Fehler, Baseline-Warnungen unverändert; `dotnet test` für `KiKern.Tests` und
  `SpeicherEngine.Tests` (337/337) grün.

### F5 — Hilfe-Betrieb: KI-Oberfläche per Konfiguration ausgeblendet (S–M)

Umsetzung von Fachkonzept 11.9 auf dem vorhandenen Schalter `KiEinwilligung.Abgeschaltet` (B11) — es wird
**kein neuer Schalter** eingeführt:

* **Menüeintrag** (`MDIMainForm.cs:226`, heute „Hilfe-Assistent (KI)..."): Bei gesetztem Schalter heißt er
  „Hilfe-Assistent…" ohne KI-Zusatz und **bleibt sichtbar** — die heutige Komplettausblendung
  (`MDIMainForm.cs:264`, `Available = false`) entfällt zugunsten der Umbenennung; die dynamische Auswertung
  beim Aufklappen bleibt. Beide Texte wandern nach `MyResource` (de und en).
* **Chatfenster im Hilfe-Betrieb:** keine KI-Beschriftungen, keine Werkzeugliste, kein „Was wird gesendet?",
  keine Aufgabensteuerung — nur Hilfesuche und Hilfeartikel (der Fensterbestand kann das: ohne Dienst
  arbeitet `Form_KiChat` als lokale Hilfesuche, Fachkonzept 1.3). Auswertung bei jedem Öffnen, nicht nur
  beim Start.
* **Aufrufknopf:** zweigestaltig aus F2; keine weitere Arbeit hier außer dem gemeinsamen Textbestand.
* **Sicherheitswirkung unverändert:** Dass nichts hinausgeht und keine Aktionen laufen, trägt weiterhin
  `KiEinwilligung.Sicherstellen` (`KiEinwilligung.cs:145-147`) und der Riegel — die Ausblendung ist eine
  reine Darstellungsfrage und ersetzt keinen Schutz.

### Abgrenzung F5

F5 ändert bewusst **kein** Verhalten bei nicht gesetztem Schalter; die Einwilligungslogik (`FASSUNG`,
`Nachfragen`, `Erteilen`) bleibt unangetastet.

## 4. Ablauf einer Feldsetzung (Soll-Verhalten, konsolidiert)

1. Modell schlägt `formular_ausfuellen(maske, werte)` vor → Registerprüfung wie gehabt (`KiPruefung`).
2. Vorbedingung: Maske im Katalog, offen, aktiv; Felder deklariert, änderbar; Schreibrecht (B3).
3. Vorschau: Blocktext „Feld · alt → neu" je Feld (alter Wert wird auf dem UI-Thread gelesen).
4. **Feldsicherung an:** Block erscheint im Bestätigungsblock (B1), **ein** Klick bestätigt alles; Verfall
   wie in 3.5 des Fachkonzepts. **Feldsicherung aus:** Schritt entfällt, Hinweis + Protokollvermerk.
5. Ausführung auf dem UI-Thread; `TextChanged` färbt wie bei Handeingabe (B5).
6. `dialog_aktion_ausfuehren("Speichern")` — eigener Aufruf, eigener Block: die **Knopfprüfung des Bestands**
   entscheidet; ihre Meldung (z. B. „ungültige Zahl") ist das Ergebnis, das der Chat zeigt.
7. Genau eine Protokollzeile je Ausführung, mit Sicherungszustand.

## 5. Reihenfolge und Aufwand

F1 → F2/F3/F5 (F2 und F5 unabhängig von F1, F3 braucht F1) → F4 (Abschluss mit Prüfbuild). Aufwand: F1
**S–M**, F2 **S**, F3 **M**, F5 **S–M**, F4 **S**; gesamt **M–L** (Erfahrungswerte, **Annahme**). Jedes Paket
endet baubar mit grünen Tests; Umsetzung durch Opus-Arbeitsaufträge je Paket, Abnahme durch den Auftraggeber
nach F4 anhand Abschnitt 7.

## 6. Risiken und Fallen (verbindliche Arbeitsregeln)

* **cp1252-Falle:** Die Startmasken-Dateien sind BOM-loses Windows-1252 mit nicht-ASCII-Controlnamen (B9) —
  nie mit dem Edit-Werkzeug bearbeiten, sondern byte-erhaltend (`io.open(..., encoding='cp1252',
  newline='')`), danach Umlaut-Kontrolle.
* **Sync-Automatik:** kein `git commit`/`push` aus der Umsetzung; nach Parallelsitzungen repoweit auf
  `<<<<<<<` prüfen; Ressourcendateien zusätzlich per XML-Parser.
* **Build:** nur VS-2022-Community-MSBuild, x64; `vswhere` liefert die falsche Instanz (MSB4236).
  `KiKern.Tests` mit eigenem `ArtifactsPath`.
* **Nicht-ASCII-Controlpfade** im Katalog (z. B. `tb_Wirkungsgrad_Öl`): Katalogtest muss die Auflösung
  nachweisen, sonst schlägt die Maske zur Laufzeit fehl.
* **Produktiv-DB** `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` nur lesen bzw. Arbeitskopie.

## 7. Abnahmeprüfliste (aus Fachkonzept 11.6/11.8)

1. Aufrufknopf auf allen vier Startmasken sichtbar, ohne Fokusklau/Tabstopp; Klick öffnet den Chat mit der
   Maske als Besitzer; aus dem modalen Dialog heraus bleibt der Chat bedienbar.
2. `dialog_lesen` nennt Felder, Werte und Knöpfe genau der offenen Maske; `dialog_parameter_erklaeren`
   liefert Anzeigename, Typ, Einheit, Erläuterung (und Hilfetext, wo Slug deklariert).
3. Kein Feld wird bei aktiver Feldsicherung ohne Klick gesetzt; der Block zeigt alt → neu.
4. „abc" im Zahlenfeld: Setzen gelingt, `dialog_aktion_ausfuehren("Speichern")` scheitert mit der Meldung
   der **Bestandsprüfung** — der Assistent ersetzt sie nicht.
5. `ReadOnly`-/deaktivierte Felder, fremde Masken, nicht deklarierte Knöpfe: Klartext-Ablehnung.
6. `/ki-feldsicherung-aus`: Feldbestätigung entfällt, Hinweis „Feldsicherung AUS" steht im Chat, Protokoll
   trägt den Vermerk; die Stufe-2-Bestätigung einer DB-Schreibaktion (z. B. `kostenposition_setzen`)
   erscheint **weiterhin**.
7. Prüfbuild x64 0 Fehler, Baseline-Warnungen unverändert; `KiKern.Tests` und `SpeicherEngine.Tests` grün;
   Encoding-Nachweis je berührter cp1252-Datei.
8. **Hilfe-Betrieb** (`KiDeaktiviert=1`): Aufrufknopf zeigt „Hilfe" und öffnet die Hilfesuche; Menüeintrag
   ohne KI-Zusatz, aber sichtbar; im Chatfenster keine KI-Beschriftung, keine Werkzeugliste, keine
   Aufgabensteuerung; nachweislich keine Anfrage an den Dienst; der maschinenweite Schalter (HKLM)
   überstimmt die Benutzereinstellung. Bei nicht gesetztem Schalter ist alles unverändert.

## 8. Detailfestlegungen (Auftraggeber, 20.08.2026)

1. **Symbol des Aufrufknopfs:** schlichtes **„KI"** in gedämpfter Schrift — kein Emoji, keine Symbolschrift
   (cp1252-sichere Darstellung, einheitlich auf allen Systemen).
2. **ComboBox-Setzen:** **nur per Anzeigetext**; bei Mehrdeutigkeit (zwei gleiche Einträge) wird mit Klartext
   abgelehnt. Kein Setzen per Index.

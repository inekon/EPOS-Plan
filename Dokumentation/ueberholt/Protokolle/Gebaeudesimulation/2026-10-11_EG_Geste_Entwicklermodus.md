# EG — Geste des Entwicklermodus im Dialog „Über EPOS-Plan“

Datum: 11.10.2026 · Zweig: Arbeitszweig der Sitzung Gebäudesimulation (Fehlerbehebung)

## Befund

Anwenderbefund 11.10.2026 (Windows, deutsche Tastatur): Strg+Umschalt+Alt+E im Dialog
„Über EPOS-Plan“ schaltet den Entwicklermodus nicht.

## Ursache (aus dem Code, ohne Windows nicht abschließend nachweisbar)

- Die Schale fing die Geste allein in `BlazorDialogForm.ProcessCmdKey` ab
  (`UeberFenster`, `Befehlstaste`). Liegt der Fokus in der WebView2, gehen die Tastenmeldungen an
  das Browserfenster des WebView2-Prozesses; das Fenster sieht sie nur, wenn die WebView2 sie über
  `AcceleratorKeyPressed` als Beschleuniger an die WinForms-Steuerung zurückgibt.
- Auf der deutschen Tastatur ist Strg+Alt gleich AltGr. Die WebView2 (Chromium) behandelt
  AltGr-Kombinationen als Zeicheneingabe (AltGr+E = „€“), nicht als Beschleuniger — der Druck
  erreicht `ProcessCmdKey` dann nicht. Der Razor-Dialog wertete in `BeiTaste` nur Escape aus und
  bekam beim Öffnen keinen Fokus; damit gab es keinen zweiten Weg.

## Behebung

- `EPOS.UI/Dialoge/Hilfe/UeberDialog.razor`: erkennt die Geste selbst an der physischen Taste
  (`CtrlKey && ShiftKey && AltKey && Code == "KeyE"`, gleich welches `Key`) und ruft den neuen
  Rückruf `GesteAusgeloest`; ohne Rückruf folgenlos, eine Ausnahme meldet der Banner. Beim
  Öffnen bekommt der Dialog den Fokus (`FocusAsync`, Hausbrauch der übrigen Dialoge).
- `EPOS.UI.Daten/Hilfe/UeberHuelle.cs`: `Gaben` reicht den Rückruf durch (optional).
- `WindowsFormsApplication1/Views/Help/UeberFenster.cs`: beide Wege (ProcessCmdKey und Dialog)
  laufen über dieselbe Handlung — eine Nachricht später `EntwicklungHuelle.GesteUmschalten`,
  Hauptfenster neu versorgen, Anstoß. Die `Gestensperre` lässt während des Schaltens und 500 ms
  danach kein zweites zu, falls ein Tastendruck beide Wege erreicht.
- Entwicklerhandbuch Abschnitt 1, Zeile „Tastenfolge“: Fokus in der Oberfläche, AltGr,
  Ausweichweg `--entwickler`.

## Nachweis

bunit `UeberDialogTests`: Geste mit `Code = "KeyE"` ruft den Rückruf genau einmal (`Key` „E“,
„€“, leer, „Unidentified“); ohne einen Modifikator oder mit anderem `Code` kein Aufruf; ohne
Rückruf folgenlos; Ausnahme im Banner; Escape schließt weiter; Fokus beim Öffnen.

## Offen

Sichtabnahme unter Windows mit deutscher Tastatur: Geste bei Fokus im Dialog, Rückfrage genau
einmal, Diagnoseblock und Hauptfenster danach im neuen Modus.

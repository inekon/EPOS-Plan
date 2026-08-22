# Nachtrag · Projektkopf: Stamm/Varianten-Dropdown ersetzt den Projekttext

> UI-Nachtrag zum Kasten rechts oben auf der Startseite (`Form_Start`), beauftragt am
> 22.08.2026 mit Screenshots. Ausgangsstand `483b605`.
> Geändert wurde **eine** Datei: `Views\Hauptformular\Form_Start.cs` — Layout
> programmatisch (Hausregel), Designer und `.resx` unangetastet.

Kurzfassung: Das Auswahlfeld für Stamm und Varianten steht jetzt in der ersten Zeile an
der Stelle des blauen Projektnamens und trägt ihn selbst — gleiche Schrift, gleiche Farbe,
flach. Die zweite Zeile „Stamm / Variante:" ist weg, der Kasten um 30 px flacher. Der
Mouseover-Hinweis ist ersatzlos entfallen.

---

## 1 Fundstellen

Die Box ist **nicht** im MDI-Rahmen, sondern in der eingebetteten Startseite: Parent-Kette
`MDIMainForm` → `Form_Start` → `panelVariante` → fünf Steuerelemente. Nachgemessen an den
`Parent`-Einträgen der `.resx`, nicht angenommen.

| Element | Was es war | Fundstelle (Ausgangsstand) |
|---|---|---|
| `panelVariante` | Kasten (873, 110), 419 × **79**, gerundeter Rahmen | `Form_Start.Designer.cs:1348-1357`, `Form_Start.resx:4544-4562` |
| `label_ProjektStatus` | ✔/⚠, 18 pt, (13, 10) | `Form_Start.resx:4226-4262` |
| `label11` | „Projekt:", Segoe UI 12 pt fett, (63, 13) | `Form_Start.resx:4052-4085` |
| `textBox_ProjektOpen` | **der blaue Projektname**, Segoe UI 12 pt fett, `ForeColor 0,0,192`, randlos, (149, 13) | `Form_Start.Designer.cs:1226-1233`, `resx:4022-4049` |
| `label4` | **„Stamm / Variante:"**, MS Sans Serif 10 pt, (26, 48) | `Form_Start.resx:4514-4541`, Text bei `:4530` |
| `comboBox_Varianten` | Auswahlfeld, Segoe UI 10 pt, (148, **44**) — zweite Zeile | `Form_Start.Designer.cs:1336-1341`, `resx:4490-4511` |

Der Anzeigetext des Auswahlfeldes entsteht in `FuelleVariantenCombo`
(`Form_Start.cs:2110 ff.`), der Umschaltweg in `comboBox_Varianten_SelectedIndexChanged`
(`:2314 ff.`).

---

## 2 Umbau

Alles in `ProjektkopfAufbauen()` (`Form_Start.cs:2214`), gerufen aus `Form_Start_Load`
(`:92`). Der Lade- und Umschaltmechanismus ist unberührt: `FuelleVariantenCombo` und
`SelectedIndexChanged` arbeiten wie bisher, das Feld ist nur verlegt und eingefärbt.

| Schritt | Wirkung |
|---|---|
| `DropDownStyle = DropDownList`, `FlatStyle = Flat`, `BackColor = White` | flache Anmutung, im Projektkopf wird gewählt statt getippt (vorher war das Feld editierbar) |
| `ForeColor`/`Font` **aus `textBox_ProjektOpen`** | exakt dieselbe Optik wie der bisherige Text — kein zweiter Farb-/Schriftwert im Code |
| `Left = textBox_ProjektOpen.Left - 3`, `Width` bis 14 px vor den Kastenrand, senkrecht auf `label11` zentriert | rückt in Zeile 1 neben „Projekt:" |
| `textBox_ProjektOpen.Visible = false` | Das Feld **bleibt** — an fünf Stellen wird sein Text gelesen bzw. gegen den Platzhalter `Text_Select` geprüft (`:1093`, `:1102`, `:1209`, `:1235`, `:1519`). Es führt den Namen weiter, es zeigt ihn nur nicht mehr. |
| `label4` aus `panelVariante.Controls` entfernt und `Dispose()` | die Zeile „Stamm / Variante:" ist weg |
| `panelVariante.Height = max(Unterkante Statuszeichen, Unterkante Auswahlfeld) + 10` | 79 → ca. 49 px, kein leerer Streifen; der gerundete Rahmen zeichnet sich über `ClientRectangle` selbst nach |

Die verwaisten `label4`-Schlüssel in den drei `.resx`-Dateien bleiben stehen (auftragsgemäß
additiv/unangetastet); `resources.ApplyResources(label4, …)` in `InitializeComponent` läuft
weiterhin folgenlos, bevor das Label entfernt wird.

**Anzeige folgt dem Projekt.** Damit der Kasten auf allen Wegen denselben Namen zeigt wie
vorher das Textfeld, hängen drei kleine Helfer an denselben Stellen wie die bisherigen
Zuweisungen an `textBox_ProjektOpen.Text`:

| Helfer | Gerufen aus | Zweck |
|---|---|---|
| `KopfNameZeigen` (`:2258`) | `SetTextProjekt` (`:136`) | wählt den passenden Eintrag; fehlt er, wird die Gruppe zum offenen Projekt neu aufgebaut (die Menüwege in `MenueCtrl.cs:134/181` laufen **nicht** über `ProjektKontextUebernehmen`) |
| `KopfNameWaehlen` (`:2277`) | intern | wählt per Text — **mit abgehängtem `SelectedIndexChanged`**, löst also keinen Projektwechsel aus |
| `KopfEinzeltextZeigen` (`:2303`) | Konstruktor (`:48`), `pBox_Delete_Click` (`:1244`) | Platzhalter „bitte auswählen!", solange kein Projekt offen ist. Der Eintrag ist bewusst **kein** `VariantenComboItem` — die Wächterzeile in `SelectedIndexChanged` lässt ihn dadurch wirkungslos. |

Dazu eine Zeile in `FuelleVariantenCombo` (`:2158`): `cb.SelectedIndex = -1` → `= sel`.
Der Index `sel` (das geöffnete Projekt) wurde bisher berechnet und dann **verworfen** — das
Feld stand nach jedem Füllen leer da. Reine Anzeige, das Ereignis ist an dieser Stelle
abgehängt.

Bewusst in Kauf genommen: Beim Projektwechsel über `ProjektKontextUebernehmen` wird die
Gruppe zweimal geladen — einmal aus `SetTextProjekt`, einmal aus dem schon vorhandenen
`VariantenAnzeigeAktualisieren()` am Ende derselben Methode. Das ist eine zusätzliche
Leseabfrage pro Wechsel; der Preis dafür, dass auch die Menüwege ohne Sonderbehandlung
denselben Namen im Kopf zeigen.

---

## 3 Tooltip-Quelle

`comboBox_Varianten_SelectedIndexChanged` setzte am Ende zwei Kurzinfos aus einem eigenen
`ToolTip`-Feld:

```
_tip.SetToolTip(textBox_ProjektOpen, textBox_ProjektOpen.Text);
_tip.SetToolTip(comboBox_Varianten, comboBox_Varianten.Text);
```

Beide Zeilen und das Feld `private readonly ToolTip _tip` sind entfallen — das Feld
mit, sonst hätte es eine neue Warnung „zugewiesen, nie verwendet" gegeben. Kein anderer
Mechanismus (Hilfesystem/`HelpExtender`) beschriftet diese Steuerelemente; projektweit
gibt es keine weitere `SetToolTip`-Zuweisung auf `Form_Start`.

---

## 4 Anzeigeformat — Entscheid

Die Einträge wichen vom bisherigen Textformat ab: Der Stamm stand als
`"Stamm: " + Projektname` in der Liste, während das blaue Textfeld den reinen
Projektnamen aus `Tab_Projekt` zeigte.

**Entschieden: Vorsatz „Stamm: " entfällt** (`:2136`). Damit ist die Liste im selben
Format wie die Auswahl und wie der frühere Text — Stamm „Wöhler", Variante
„Wöhler - Test2" (Varianten führen in `Tab_Projekt` ohnehin den Namen
`<Stamm> - <Bezeichner>`, `VariantenCtrl.cs:119`). Die dahinterliegende Auswahllogik
hängt unverändert an `VariantenComboItem.IdProjekt`, nicht am Anzeigetext; das Flag
`IstStamm` bleibt erhalten.

Preis dieses Entscheids: In der aufgeklappten Liste ist der Stamm nicht mehr am Wort
„Stamm" erkennbar, sondern nur noch daran, dass er als einziger keinen Zusatz trägt.
Das ist der Preis für die geforderte Anzeige-Kontinuität im Kopf.

---

## 5 Build

`WindowsFormsApplication1.csproj`, Debug/x86, inkrementell, VS-2022-MSBuild — **erfolgreich**.
Keine Diagnose aus der geänderten Datei. Es blieben ausschließlich Altwarnungen aus
fremden Dateien: `KlimaregionStammCtrl` (2 ×), `StromverbraucherStammCtrl`,
`WErzeugerModel`, `MDIMainForm` (CS1998, Zeile 429 — die Zeilennummer hat sich gegenüber
der Altliste durch zwischenzeitliche Commits verschoben; die zweite MDIMainForm-Warnung
CS4014 trat nicht mehr auf).

---

## 6 Sichtprüfung (Philipp)

1. Der Kasten rechts oben zeigt in der ersten Zeile neben „Projekt:" das **Auswahlfeld**
   in derselben blauen, fetten Schrift wie vorher der Text.
2. Aufklappen und Umschalten zwischen Stamm und Variante wirkt wie bisher (Klimaregion,
   Statuszeichen, Wizard-Symbole, Reiter „Berichte & Kosten" ziehen nach).
3. **Kein** Mouseover-Hinweis mehr über dem Feld.
4. Die Zeile „Stamm / Variante:" ist weg, der Kasten schließt ohne leeren Streifen ab.
5. Ohne offenes Projekt steht „bitte auswählen!" im Feld; ein Klick darauf tut nichts.
6. Nach „Projekt neu"/„Projekt bearbeiten" über das **Menü** steht der neue Name im Feld.

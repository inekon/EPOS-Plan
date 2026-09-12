# Paket B3 — Warnkriterium und Dialoganzeige „Quelle unbegrenzt trotz Pufferwahl"

**Nutzerauftrag 28.08.2026, Punkt 2 des Folgeauftrags:** „Falls Puffer als Quelle
gewählt, altes Unbegrenzt-Häkchen übersteuert kommentarlos — als Kandidat für ein
Warnkriterium bzw. einen Dialogfix vornehmen." Umgesetzt am 28.08.2026 abends,
Codestand-Basis `311e0ed`.

## Der Befund

An der Booster-WP 14818 des Anwenderprojekts 1042 stand nach der Umverschaltung
`WQ_Typ = Pufferspeicher` und `WQ_ID_Puffer = 1054198` — aber aus dem Zwischenstand vom
Mittag zusätzlich noch **`WQ_Unbegrenzt = True` mit `WQ_Temp = 45`**.
`WaermequelleClass.Quellspeicher` (Bestandssemantik, dort seit jeher kommentiert mit
„unbegrenzt verfügbar → nur die Temperatur wirkt, keine Bilanz") liefert in diesem Fall
**keinen** Quellspeicher: Die gesamte Speicherkopplung aus B1/B2 samt Lesepunkt blieb
still abgeschaltet, die WP rechnete mit konstant 45 °C — sichtbar nur indirekt am
Kennlinien-Hinweis „8760 Stunden über der oberen Stützstelle". Weder Dialog noch Karte
noch Laufstart nannten den Konflikt.

**Abgrenzung:** Der HEIZKESSEL liest das Flag nicht
(`SimulationControl.QuellbezuegeAufbauen` fragt nur `WQ_Typ` und `WQ_ID_Puffer`) — die
Falle ist eine reine Wärmepumpen-Falle, und genau so ist das Kriterium zugeschnitten.

## Umsetzung

**1. Warnkriterium `QUELLE_UNBEGRENZT`** (`Warnkriterien.cs`, Muster
`SoleOhneQuellePruefen`): eigene stille Abfrage in `PruefeProjekt` — WP-Anlagen des
Projekts mit `WQ_Unbegrenzt = TRUE`, im Code gefiltert auf
`WQ_Typ = WaermequelleClass.TYP_PUFFER` (Persistenzwert nicht als SQL-Literal,
Drei-Schichten-Regel) und auf einen wirklich benannten Puffer (Fremdschlüssel, sonst
Bezeichner — dieselbe Rückfallkette wie die Engine). **Weich:** Der Lauf rechnet den
dokumentierten Bestandsweg weiter; eine stille Ergebnisänderung für Bestandsprojekte
gäbe es nur um den Preis, eine gespeicherte Anwenderangabe zu verwerfen. Der Befund
läuft über die bestehende Katalogmechanik an Karte und Laufstart; die Engine meldet
nicht zusätzlich (keine Doppelmeldung, B2-Lektion).

**2. Dialoganzeige** (`Form_QuellePufferspeicher.UnbegrenztKonfliktAnzeigen`): Ist das
Häkchen gesetzt UND ein Puffer gewählt, färbt sich die Checkbox-Beschriftung warnrot
(Firebrick) und wechselt auf `SIMQ_PUFFER_CB_UNBEGRENZT_KONFLIKT` — „… Speicherkopplung
AUS, konstant {0} °C!" mit der Temperatur aus dem Eingabefeld. Live nachgeführt über
`CheckedChanged`, `SelectedIndexChanged`, `TextChanged` und ausdrücklich am Ende von
`SetControls` (leere Liste, unveränderter Checked-Wert). **Bewusst keine stille
Korrektur beim Speichern:** Das Häkchen ist eine Anwenderangabe mit legitimem Altfall
(Puffer benannt, bewusst als unerschöpflich gerechnet); der Dialog macht den Konflikt
unübersehbar, entscheiden muss der Anwender. Beim Heizkessel ist die Rubrik ausgeblendet
(D5b) — die Methode läuft dort ins Leere.

**Breitenmaß** (TextRenderer, 96 DPI): breitester Fall deutsch/Segoe UI 9 pt = 383 px
Text + 20 px Kästchen, Ende bei x = 419 — **171 px vor der Rubrikkante 590**; englisch
400. Keine Abschneidung möglich (die AutoSize-Falle der Design-Politur 21.08. ist
vermessen statt behauptet).

**3. Ressourcen:** `SIMWARN_QUELLE_UNBEGRENZT` und `SIMQ_PUFFER_CB_UNBEGRENZT_KONFLIKT`,
DE + EN + Designer je **2635** (deckungsgleich). Die parallel geöffnete VS-Instanz hat
den Designer bei der resx-Änderung sofort selbst regeneriert — die beiden
Hand-Einfügungen waren wie dokumentiert (CLAUDE.md-Fallstrick) als CS0102-Duplikate zu
entfernen, die generierten blieben.

## Verifikation

Feste Quellkopie `C:\Waermeplan\_b3basis\DB` (produktive DB 28.08.2026 **23:34:55**, nur
gelesen, per `migration`-Modus auf Schemastand 55; bekannter Schema-Nachweis-Befund der
zwei 1027-Altlastzeilen, außerhalb der Referenzmenge).

- **A/B auf identischer Kopie:** Lauf A = Codestand `4be1862` (reguläres `bin\`), Lauf B
  = B3 (Bin-Kopie des Messwerkzeugs mit getauschter App-DLL, Muster
  Worktree-Verifikation; `bin\` war durch die wieder gestartete Anwendung gesperrt).
  Alle 26 Projektläufe Exit 0. **332 von 332 CSV byte-/MD5-gleich** — B3 ändert keinen
  einzigen Rechenwert.
- **Wirkprobe:** Im B3-Lauf erscheint die Warnung genau **einmal** — Projekt 1042,
  „Anlage ‚CS7800iLW 16': Der Pufferspeicher ‚Puffer 3000Ltr (2)' ist als Wärmequelle
  gewählt, aber ‚Quelle unbegrenzt verfügbar' ist gesetzt …" — und in keinem anderen
  Projekt. Im A-Lauf (B2-Code) null Fundstellen.
- **Nebenbefund Datenstand:** Lauf A (Datenstand 23:34) ist gegen die eingefrorene Basis
  `2026-08-28_B2` (Datenstand 17:19) **332/332 byte-gleich** — die Abendsitzung des
  Anwenders hat keine rechenrelevanten Projektdaten verändert; die Basis bleibt exakt
  gültig, ein Refresh wegen der Abendsitzung ist nicht nötig.
- Produktive DB in diesem Paket ausschließlich gelesen.

## Offene Punkte

- **B3-O1:** Der Basis-Refresh mit scharfer Booster-Kopplung steht weiter aus — er
  braucht die Anwenderaktion (Häkchen an 14818 entfernen; der Dialog zeigt den Konflikt
  jetzt rot) und eine anschließend geschlossene App. Erwartung danach: nur 1042 ändert
  sich, Kopplung + Lesepunkt-Zeile („DAVOR") erscheinen im Protokoll.
- **B3-O2:** Die Karten-Chips des Konfigurationsdialogs zeigen Katalogbefunde projektweit
  an; eine zusätzliche Chip-Kurzform speziell für QUELLE_UNBEGRENZT (analog „Quelle
  wählen!") wäre denkbar, ist aber bewusst nicht gebaut — Dialogrot + Karte + Laufstart
  sind drei Sichtbarkeiten für denselben Befund.

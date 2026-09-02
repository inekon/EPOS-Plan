# S1-Restbericht — Nachschlagefelder

**01.09.2026** · letzter offener Punkt aus Arbeitspaket S1
([Implementierungskonzept](../Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md),
Abschnitt 1.4: „Offen aus S1 bleibt allein die Sichtung der Nachschlagefelder … erledigt der
Schema-Generator nebenbei").

Erhoben beim S2-Lauf von [`sql/tools/Erzeuge-Schema.ps1`](tools/Erzeuge-Schema.ps1) über die
DAO-Feldeigenschaften `DisplayControl`, `RowSource`, `RowSourceType` und `BoundColumn`
(strikt lesend, DAO 120, `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb`, Schemastand 61, nach S0).
Rohwerte auch in [`schema/inventar.json`](schema/inventar.json) unter `Nachschlagefelder` und
`DisplayControlVerteilung`.

## Befund

**Es gibt kein einziges Nachschlagefeld.** Über alle 114 Tabellen und 2.479 Spalten hinweg
trägt keine Spalte `DisplayControl` = 110 (Listenfeld) oder 111 (Kombinationsfeld):

| `DisplayControl` | Bedeutung | Spalten | Verteilung nach DAO-Typ |
|---|---|---:|---|
| 106 | Kontrollkästchen | 46 | 46 × Boolean |
| 109 | Textfeld | 1.766 | 1.421 Double · 175 Long · 169 Text · 1 Boolean |
| *(Eigenschaft fehlt)* | kein Lookup gesetzt | 667 | 309 Double · 196 Long · 84 Text · 50 Boolean · 20 Datum · 8 Memo |
| **110 / 111** | **Listen-/Kombinationsfeld** | **0** | — |

Gegenprobe über die drei begleitenden Eigenschaften: `RowSource` ist in **0** Spalten gesetzt,
`RowSourceType` in **0**, `BoundColumn` in **0**. Ohne RowSource kann per Definition kein
Nachschlagefeld existieren — der Befund ist damit doppelt belegt.

## Bewertung

Damit entfällt die Tabelle „Tabelle.Spalte + RowSource" mangels Einträgen. Auch wenn es
Nachschlagefelder gäbe, wären sie folgenlos: `DisplayControl`/`RowSource` sind reine
**Anzeigeeigenschaften des Access-Designers**. Sie steuern, wie das Access-Frontend ein Feld im
Datenblatt zeichnet, und haben weder auf die gespeicherten Werte noch auf Constraints,
Indizes oder Abfrageergebnisse Einfluss. EPOS-Plan öffnet die Datenbank ohnehin nie im
Access-Frontend, sondern ausschließlich über den Provider. SQLite kennt keine Entsprechung und
braucht keine — die Auswahl-Listen der Anwendung entstehen im C#-Code, nicht im Schema.

**Ergebnis: kein Migrationsgegenstand, keine Nacharbeit, kein Risiko.** Der Punkt ist
abgeschlossen.

## Übrige S1-Punkte

Alle anderen S1-Fragen sind bereits entschieden und dort belegt —
siehe [Implementierungskonzept, Abschnitt 1.4](../Implementierungskonzept_DB-Migration_SQLite_EPOS-Plan.md):
positionelle `?`-Bindung (Umschreibung `?`→`@pN` ist Pflicht), **D3** Boolean
`NOT NULL DEFAULT 0` (Variante a), **D5** Kollation `BINARY` mit Case-Drift-Messlauf,
die 5 unbekannten `Abfrage_*`-Namen (kein Migrationsgegenstand) und die NULL-Schreibpfade auf
Boolean (keine).

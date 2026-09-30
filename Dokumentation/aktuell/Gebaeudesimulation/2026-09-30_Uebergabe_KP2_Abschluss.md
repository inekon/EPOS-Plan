# Übergabe: Sitzung „Gebäudesimulation“ — Stand 30.09.2026, KP2 bis auf U5 umgesetzt

**Anlass:** Die Sitzung hat bei 87 % Wochennutzung angehalten (Anwendervorgabe: Halt bei 90 %); die Arbeit geht auf
einem anderen Claude-Konto weiter. Diese Übergabe löst die [Übergabe vom 27.09.2026](2026-09-27_Uebergabe_Gebaeudesimulation_Cloud.md)
ab; beide wandern mit dem Abschluss von KP2 (Welle U5) nach `ueberholt/`.

## 1 Stand

- **Zweige:** `ios_migration_september` trägt KP2 bis #634 samt dieser Übergabe; der Arbeitszweig der Cloud-Sitzung
  `claude/inspiring-bell-b8wq90` ist gleich weit (alles committet und gepusht, kein Worktree, kein Agent läuft).
- **Basis** `Referenzlaeufe/2026-09-30_R29_Kesseltakten` (16 Projekte, aus der Nachbarsitzung Kessel);
  **Schemastand 157** (156 Kessel-Kennlinie, 157 Saat der 14 Konditionierungsvorlagen). ChartProben-Messlatte
  `Proben/ChartProben/Messlatte_2026-09-30.sha256` (194 Zeilen).
- **KP2 nach dem [Entwurf](2026-09-29_Entwurf_KP2.md) (E56):** umgesetzt und veröffentlicht die Wellen
  K1, U0a, U0b (#618), K3 (#619), K2, K4 (#621), U1 (#623), U2 (#626), U4, U3 (#634) — jede mit Gate, alle
  Referenzläufe byte-gleich gegen die jeweils gültige Basis, CI grün (zuletzt Kern-Lauf 36708236840 auf `d8289c1d`).
  Einzelheiten, Befunde und Nachweise: [Protokoll KP2](../../ueberholt/Protokolle/Gebaeudesimulation/2026-09-30_KP2_Konditionierung_Oberflaeche.md);
  Stand je Welle in [`Status_iOS_Migration.md`](../Status_iOS_Migration.md) (Zeilen und Blöcke „Nach #618“ bis „Nach #634“)
  und [`Status_Gebaeudesimulation_VDI6007.md`](../Status_Gebaeudesimulation_VDI6007.md) (Zeile KP2, Entscheide E55–E57).
- **Entscheid E57** (30.09.2026): Die Abkürzung „gleichnamige Vorlage in allen Größen übernehmen…“ wird gebaut — ein
  Eintrag in der Zeile „Vorlage“, eine Rückfrage für alle Größen, kein Satzbegriff (P11 bleibt). Noch nicht umgesetzt.

## 2 Was aussteht

### 2.1 Welle U5 (letzte Welle von KP2, auf Auftrag)

1. **Abkürzung nach E57** in der Kalenderkarte bzw. Zeile „Vorlage“: für jede Größe, in der eine gleichnamige Vorlage
   existiert, übernehmen; **eine** Rückfrage aus den Befunden aller Größen vor dem Schreiben (P12 je angelegtem
   Kalender, „aufteilen“ bei Gesamtangabe der Lüftung); Zählfall KN6 neu zählen (heute 11–16 Handgriffe); Assistent und
   Aktionswissen nachziehen; Konditionierungsprobe um den Fall erweitern.
2. **Abschluss der Stufe:** Festlegungen der Umsetzung als **N1.66** im
   [Leitkonzept](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (Entwurf Abschnitt 5 samt den Festlegungen der
   Wellen im Protokoll); Verweise und Stufenstand im [Teilkonzept](../Konzept_Konditionierungsprofile_EPOS-Plan.md)
   (Kapitel 7, 8); Protokoll KP2 abschließen; Entwurf KP2 und beide Übergaben per `git mv` nach `ueberholt/` samt
   Indexzeilen; Statuszeile.
3. **Logbuch-Sätze** entwerfen (K1 „Speichern unter“, U1 Reiter, U2 Vorlagen, U3/U4 Karte, Zonen, Verwaltung) —
   **Versionsnummer beim Anwender erfragen**; Wiki-Quellen der Bedienung folgen mit KP4.

### 2.2 Offen beim Anwender

- **Sichtabnahme SA1 unter Windows** (Entwurf Abschnitt 7, 100 % und 125 %); da alle Oberflächenwellen stehen, deckt
  sie SA2 weitgehend mit ab. Zusätzlich: Hilfepille hausweit 44 px, Zonendialog als Blatt, Verwaltungsblatt,
  Kalenderkarte samt Teppichbild, „In den Kalender übernehmen“ an der Wärmeübergabe.
- Versionsnummer der Logbuch-Sätze; aus Übergabe 27.09. weiter offen: Windows-Sichtabnahmen früherer Stufen,
  Freigabeschalter des gbXML-Exports, Register M11.

### 2.3 Kleine offene Punkte (in den Blöcken „Nach #6xx“ der Statusdatei)

Rückknopf des Vorlagenverwaltungsblatts trägt den Dialogtitel statt „Gebäude in DB ändern…“; „eigene Woche“ einer
Periode nur angezeigt; Zonenkarten ohne Einzelheiten; Beschriftung des Teppichbilds bei 390 px sehr klein;
`Proben/Rasterprobe/LIESMICH.md` trägt eine BOM; Windows-Messliste der ChartProben um neun Zeilen nachziehen.

### 2.4 Weitere Stufen (je auf Auftrag)

KP3 (Bericht, CSV, KI-Sicht und Variantenvergleich der Nachtauskühl- und Sommerlüftungsstunden, neues Referenzprojekt,
Einfrierregel „gesäte Konditionierungsdaten“, neue Basis), KP4 (Wiki, Rechenschritte, Logbuch), KU3
(Kühlkalender je Zone).

## 3 Regeln der Sitzung (gelten weiter, ergänzt um die Lehren aus KP2)

- **Ablauf je Welle:** Auftrag als Datei, Agent im eigenen Worktree (`model: opus`, ausdrücklich), Abnahme mit vollem
  Gate im Worktree; danach **Merge → Gate → Statuszeile und Protokoll → Push → CI-Nachweis**. Nach grünem Gate pushen
  ohne Rückfrage; **iOS-, macOS- und Setup-Läufe nur nach Rückfrage, jedes Mal.**
- **Zwei Wellen, die getrennt grün waren, sind zusammen noch nicht geprüft:** nach jedem Merge ein Gate über den
  gemeinsamen Stand (Lehre K1 × U0b: das Schloss blieb nach „Speichern unter“ stehen).
- **origin bewegt sich oft** (Nachbarsitzungen Kessel, Wirtschaftlichkeit, Diagramme; Basis R26 → R29 an einem Tag):
  Statusnummern und Schemanummern unmittelbar vor dem Commit gegen `origin` messen; vor dem Veröffentlichen `origin`
  mergen. Ändert der Merge den Rechenweg oder die Basis, volles Gate neu; sonst gezielte Nachprüfung (UI-Tests,
  betroffene Kern-Tests, Referenzlauf — dauert eine Minute —, Windows-Schale) und das im Nachweis so benennen.
- **CI-Nachweis:** Ein Lauf auf `ios_migration_september` wird durch einen fremden Push abgebrochen; dann gilt der
  grüne Lauf desselben Commits auf dem Arbeitszweig.
- **`.resx`-Merge:** Endblöcke beider Seiten übernehmen, an der Naht `</data>` ergänzen, XML- und Doppelprüfung,
  `designer_neu.py` (der Designer führt im Repository LF; „ABWEICHEND“ nur wegen CRLF ist kein Befund).
- **Werkzeugfallen:** `pgrep -f`/`pkill -f` mit einem Muster aus der eigenen Befehlszeile trifft die eigene Shell;
  Gates mit `run_in_background` starten, damit das Ende gemeldet wird.
- **Papiere:** Statuszeile nach der letzten Zeile einfügen, Block „Nach #n“ vor dem jüngsten eigenen Block, Wachen
  `DokumentationLinkWache`, `RepositoryOrdnungWache`, `WikiProduktdatenWache` vor jedem Papiercommit.
- **Kontingent:** Der Anwender nennt die Wochennutzung auf Nachfrage; Halt bei 90 %. Eine Welle kostete in KP2 rund
  3–5 Punkte samt Orchestrierung; ein `/compact` bei großem Kontext spart.

## 4 Einstieg auf dem neuen Konto

Erste Anweisung: „Lies `CLAUDE.md`, diese Übergabe, die Statusdatei der Gebäudesimulation und in
`Dokumentation/aktuell/Status_iOS_Migration.md` die Blöcke ‚Nach #618‘ bis ‚Nach #634‘; dann `git fetch`, `origin`
mergen, Nummern messen und die Welle U5 (Abschnitt 2.1) als Auftrag an einen Agenten im eigenen Worktree geben.“

# ZU34 — Gemeinsamer Katalogeinstieg auf dem iPad (26.09.2026)

Protokoll des Postens **#540**. Auftrag: „führe aus: Gemeinsamer Katalogeinstieg auf iOS“ — die
Kataloge, die auf iOS aufgehen (Baustoffe, Bauteilaufbauten, Brauchwasser-Nutzungsarten), sollen
dort sichtbar erreichbar sein und nicht nur über den Hilfe-Assistenten (Anwenderentscheid ZU34,
N23 (b)); die übrigen Katalogverwaltungen bleiben geschlossen (KI-D-Q10), weitere kommen später
als Datenzeile hinzu. Zweig `zios`, Worktree `.claude/worktrees/zios`, Opus 5.5. Kein
Schemaschritt, Testdatenbank unberührt, kein CI-Lauf. Die Festlegungen stehen als **Nachtrag N29**
im [Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Zapfprofilgenerator_EPOS-Plan.md); dieses
Protokoll hält Commits, Prüfung, Abweichungen und Gates fest.

## 1. Commits

| Commit | Inhalt |
|---|---|
| `a0f885dd` | Einstieg: `Menuepunkt.Katalog`, zwölf gekennzeichnete Punkte und `Menuetabelle.Kataloge`, `AppWurzel.FuehrtZiel` (aus `OeffneMaske` herausgezogen), Knopf und Liste in `Projektliste.razor`, Stil in `epos-ui.css`, `KATEIN_KNOPF`/`KATEIN_LISTE` in beiden Sprachen samt Designer, `KatalogeinstiegTests` |
| `a2ae68d1` | Wiki-Quellen: Baustoffe und Bauteilaufbauten (Einstieg auf dem iPad), Brauchwasser-Zapfprofil (Abschnitt Katalog) |
| Merge | `origin/ios_migration_september` (#535) ohne Konflikt |
| Papiere | N29, Kapitel 9 Zeile ZU34, Statuszeile #540, iU11-Vermerk, Nachtrag im Umsetzungskonzept iOS, Wiki-Update (Seiten, Logbuch-Satz), Index, dieses Protokoll |

## 2. Prüfung gegen den Auftrag

- **Sichtbarer Einstieg auf iOS:** Knopf „Kataloge…“ im Seitenkopf der Projektliste — erfüllt
  (Ort siehe Abweichung 1).
- **Datenquelle Menütabelle, Positivliste als einzige Wahrheit:** `Menuetabelle.Kataloge(AppWurzel.FuehrtZiel)`;
  `OeffneMaske` fragt dieselbe Methode — erfüllt. Name je Eintrag aus dem Textschlüssel der
  Menütabelle (beide Sprachen), Wächter `Jeder_gekennzeichnete_Punkt_hat_eine_Beschriftung_in_beiden_Sprachen`.
- **Nur die drei freigegebenen Kataloge:** Test `Die_Wurzel_gibt_genau_die_drei_Kataloge_frei` und
  `Ohne_Kopfleiste_listet_der_Einstieg_genau_die_freigegebenen_Kataloge` — erfüllt.
- **Weitere Kataloge als Datenzeile:** Die neun Geräte- und Verbraucherkataloge tragen das Kennzeichen
  schon; sie erscheinen, sobald die Wurzel ihren Schlüssel führt — erfüllt.
- **Kein Untermenü mit einem Punkt:** bei genau einem Katalog öffnet der Knopf unmittelbar (Test) — erfüllt.
- **Öffnen über `OeffneMaske`, benannte Ablehnung bleibt:** Tests für alle drei Kataloge und für den
  fehlenden Parametersatz — erfüllt.
- **KI-Sicht/Feldkarte:** Knopf und Liste tragen kein Eingabefeld; die `KiMaskenabdeckungWacheTests`
  bleibt grün, eine Feldkarte entfällt.
- **iOS-Schale und Prüfmodus:** nichts beizusteuern, alles läuft in der `AppWurzel`; die Katalogprobe
  aus #524 bleibt an ihrem Schalter (eine Kopplung an den Einstieg wäre nicht trivial: die Probe liest
  ohne Oberfläche).
- **Wiki:** zwei Absätze, Gegenlese-Muster ohne Treffer.

## 3. Abweichungen

1. **Projektliste statt Startseite.** Der Auftrag nennt eine Kachel auf der Startseite. Auf iOS geht
   die Startseite vor iU11 nicht auf (`IProjektQuelle.StartseiteGaben` ohne iOS-Fassung); die
   Startansicht dort ist die Projektliste. Eine Kachel auf der Startseite wäre auf dem iPad
   unsichtbar geblieben. Der Einstieg steht deshalb als Knopf im Seitenkopf der Projektliste.
2. **Nur ohne Menüband.** Entschieden nach der Frage im Auftrag: Unter Windows führt das Menü alle
   zwölf Kataloge, die Wurzel dort aber nur drei (die übrigen öffnet die Schale); ein Knopf mit dreien
   wäre eine zweite, unvollständige Liste. Merkmal ist die fehlende Kopfleiste, wie bei der
   Gattungszeile der Startseite.
3. **Scratchpad:** Beim Aufräumen eigener Hilfsdateien ist im gemeinsamen Scratchpad eine fremde
   Zwischendatei `annexIII.txt` (aus #537, abgeschlossen; aus `annexIII_raw.html` wiederherstellbar)
   mitgelöscht worden. Kein Einfluss auf das Repository.

## 4. Gates

vor dem Merge gefilterte Tests (Startseite, AppWurzel, Katalog, KiMasken, Menue, Projektliste,
Dokumentations-, Wiki- und Ordnungswache) 2 509 erfolgreich; nach dem Merge von `origin` (Stand
`ebf01a90`): Kern-Filter 0 Fehler; voller Lauf 0 Fehler (15 614 erfolgreich, 2 übersprungen);
Windows-Schale 0 Fehler; Designer wiederholbar (+2 Schlüssel, zweiter Lauf +0); Wiki-Tabuwörter 0;
kein Referenzlauf (kein Rechenweg). Der Schluss-Merge und seine Wiederholung stehen in der Statuszeile.

## 5. Folgen

- Sichtprüfung auf dem iPad mit dem nächsten `ios.yml`-Lauf (Rückfrage beim Anwender).
- Weitere Kataloge auf iOS (KI-D-Q10, iU11): je Katalog ein Zweig der Wurzel und sein Schlüssel in
  `FuehrtZiel`; der Einstieg zieht ohne Änderung nach.
- Logbuch-Satz (Sammel-Upload 1.2.0.4): „Auf dem iPad öffnet der Knopf „Kataloge…“ der Projektliste
  die freigegebenen Katalogverwaltungen.“

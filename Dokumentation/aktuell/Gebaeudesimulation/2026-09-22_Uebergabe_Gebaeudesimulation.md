# Übergabe: Konzeptfamilie Gebäudesimulation VDI 6007 — Stand 22.09.2026

**Zweck.** Der Auftrag „Gebäudesimulation EPOS-Plan" wechselt auf ein anderes Konto. Dieses
Papier sagt dem Nachfolger, was fertig ist, was aussteht und welche Regeln gelten. Es ersetzt
kein Konzept; die Sachlage steht in den Papieren selbst.

## 1 Was fertig ist

- **Entscheide E1 bis E26** sind eingearbeitet. Der jüngste, **E26** (17.09.2026), steht als
  Nachtrag N1.31 im [Leitkonzept](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md): Der Altweg
  (Tagesbilanz) ist ein Übergang, der VDI-Weg löst ihn später komplett ab und arbeitet
  eigenständig; die Stufe **GA — Altweg ablösen** steht wieder als letzte Stufe im Plan, ihr
  Zeitpunkt ist offen (Q24), ihr Umfang ist Q25.
- **Prüfung vom 17.09.2026** (sechs Blickwinkel, je ein Gegenprüfer): 128 Befunde und 29
  Ergänzungen, Ergebnis und 31 Festlegungen F-Ü1 bis F-D1 im
  [Prüfprotokoll](2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md), Festlegungen verbindlich im
  [Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md), Kapitel 8.4 (Widerspruch bis
  zur Beauftragung von G1 möglich).
- **Nachgezogen** (17.09. begonnen, 22.09. vervollständigt): Leitkonzept Rev. 3,
  Umsetzungskonzept Rev. 4, Rechenschritte Rev. 2, Softwarearchitektur Rev. 4, Systementwurf
  Rev. 4, Kühlkonzept Rev. 4, Anlagenkopplung Rev. 2, Mehrzonenmodell Rev. 3, Datenaustausch
  Rev. 2, ADR-002/003/005/006, Befund X, Katalogfilter, Register (66 offene Punkte),
  [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md), Index. Alle Papiere UTF-8 ohne BOM mit
  CRLF; Linkprobe über `Dokumentation/` ohne Fehler unter `aktuell/`.
- **Nicht committet.** Der Stand liegt im Arbeitsbaum; `GitHub_Sync.bat` nimmt ihn mit, sobald
  die fremde Sperrdatei `AGENT_LAEUFT` (Sitzung „Energiekosten", 22.09.2026) verschwunden ist.

## 2 Was aussteht

### 2.1 Restabgleich (ein Auftrag, rund 1–2 Stunden Agentenarbeit)

Am 22.09.2026 gestartet und vor der ersten Änderung abgebrochen; nichts davon ist halb erledigt.

1. **Klimaspalten sind umgesetzt** (Schemaschritt 95, Anwenderentscheid 19.09.2026, Aufträge
   KL-3/KL-4): `Tab_Solar` und `Tab_Solar_STAMM` führen `Gegenstrahlung`, `Luftfeuchte`,
   `Bedeckungsgrad`; `Tab_Klimaregion(_STAMM)` führen `Quelle`, `Importdatum`; Schritt 97 bringt
   Szenario und Bezugsjahr (`EPOS.Kern/Allgemein/Update/SchemaKatalog.cs`, `SchemaStand.cs`:
   `Zielversion = 100`, nächste freie 101). Der Klimaspalten-Schritt der Gebäudesimulation
   (Papiername M4, Stufe G2) ist damit **vorweggenommen** — keine Spalte `Windgeschwindigkeit`,
   dafür `Bedeckungsgrad` und `Luftfeuchte`. Nachzuziehen mit Verweis auf
   [Konzept Klimadatenquellen](../Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md): Softwarearchitektur
   (Kapitel 0 Punkt 3, Bild 1.1, 2.2, 2.4 Zeile M4, 2.8 Einfrierregel „gesäte Klimareihen"),
   Rechenschritte 1.2/1.3 und die NULL-Regel zur Gegenstrahlung (bei NULL weiter Δθ_lw = 0 und
   α_str,A = 5,0; eine Schätzung aus dem Bedeckungsgrad wäre möglich, wird aber nicht gerechnet),
   Leitkonzept Kapitel 11/12 (M4 als umgesetzt), Systementwurf (zwei Stellen), Umsetzungskonzept
   1.7 gegen Kapitel 4; Schemastand-Momentaufnahmen „Stand 22.09.2026: 100, nächste freie 101".
2. **Aufwand der Anlagenkopplung:** das Papier führt seit Rev. 2 AK1 mit **10–15 PT** und AK0–AK3
   mit **45–70 PT**. Zu übernehmen in Register Kapitel 1 (Q26) und 8.4 F-S7, Statusdatei (Zeilen
   AK1 und Anlagenkopplung), Leitkonzept Kapitel 13 (Q26) und 15, Prüfprotokoll Kapitel 3 F-S7
   und 5.5.
3. **Umsetzungskonzept 1.4, Ergebnisreihen des Eingangsbauers** (`PhiSolarAW`, `PhiSolarIW`,
   `PhiSolarLuft`, `PhiIntern`) an die Ausgabe von Schritt E der Rechenschritte angleichen
   (Festlegung F-P2: Aufteilung im Eingangsbauer; Reihen `ThetaOut`, `ThetaEq`, `PhiRadAW`,
   `PhiRadIW`, `PhiConv`, `ThetaSoll`, `ThetaMax`/`ThetaKuehl`).
4. **Zusicherung Heizen/Kühlen je Stunde:** Rechenschritte 7.1 akkumulieren Heiz- und Kühlanteil je
   Abschnitt getrennt (F-P3); eine Stunde mit Fallwechsel kann beides tragen. Kühlkonzept 3.3 und
   10.2 sichern „in keiner Stunde beide größer null" zu. Festlegung (Ergänzung zu F-K3): je
   **Abschnitt** nie beides; je **Stunde** möglich bei Fallwechsel; die Probe prüft die
   Abschnittsregel scharf und zählt die Stunden mit beidem als Hinweis, nicht als Fehler.
   Nachziehen in Kühlkonzept 3.3/10.2 (und 3.5), Rechenschritte 7.1, Register 8.4 F-K3,
   Prüfprotokoll Kapitel 3 F-K3.
5. **Prüfprotokoll Kapitel 4 und 5** auf die Wirklichkeit bringen: Nachziehen am 17.09. begonnen,
   am 22.09. vervollständigt; Word-Kurzfassung nicht neu gebaut (Abschnitt 2.4); Register zählt
   66 Punkte (K20 durch Umsetzung erledigt); Klimaspalten durch Schritt 95 vorweggenommen;
   Schemastand 100/101. Vermerk unter dem Titel: „Stand 22.09.2026 (Nachziehen vervollständigt)".
6. Zum Schluss über alle Papiere: `grep` nach `Windgeschwindigkeit`, `44–68`, `9–13 PT`,
   `Zielversion = 8`, `Tab_DBTagV_Daten`, `dauerhaft`, `Schemaschritt 77` — jede verbleibende
   Stelle erklären (gewollt oder Physikbegriff).

### 2.2 Abschlussprüfung

- Zeilenenden je Datei: `wc -l` gleich `tr -cd '\r' | wc -c`, kein BOM.
- Linkprobe (Perl, unten) und danach die Wache
  `dotnet test EPOS.Kern.Tests/EPOS.Kern.Tests.csproj -c Release --filter "FullyQualifiedName~DokumentationLinkWacheTests|FullyQualifiedName~RepositoryOrdnungWacheTests"`
  — erst, wenn keine fremde `AGENT_LAEUFT` liegt. Die 17 Linkfehler unter `ueberholt/` sind
  Altlasten, die die Wache ausnimmt.
- Mermaid-Prüfung: das frühere Werkzeug ist verloren (Abschnitt 2.4); die Blöcke der Papiere sind
  zuletzt am 17./22.09. von den Nachzieh-Agenten syntaktisch geprüft worden.

### 2.3 Zusammenfassung der offenen Punkte für den Anwender

Der Anwender hat sie mit dem Prüfauftrag verlangt. Quellen: Register Kapitel 0 (36 Punkte, die vor
G1 fällig sind), Prüfprotokoll Kapitel 5 (Q24, Q25, U17, D17, K24; Aufgaben für G0; Q26; H6
vertagt). Kurz und übersichtlich, ein Satz je Punkt.

### 2.4 Word-Kurzfassung — Entscheid des Anwenders

Die Word-Datei
[`Gebaeudesimulation_VDI6007_Architektur_Design_Rechenweg_2026-09-16.docx`](Gebaeudesimulation_VDI6007_Architektur_Design_Rechenweg_2026-09-16.docx)
steht auf **E1–E25 (16.09.2026)**. Ihre Markdown-Quelle (rund 2 300 Zeilen, 16 Mermaid-Bilder,
345 Formeln) und die beiden Konverter (Markdown → docx mit Word-Gleichungen; Mermaid → PNG über
Playwright und Edge) lagen außerhalb des Repositoriums im Temp-Ordner und sind mit ihm gelöscht
worden. Auf dem Rechner fehlen pandoc und node; dotnet und Python 3.12 sind da.

Wege, zwischen denen der Anwender wählt:

1. **Neuaufbau als Werkzeug im Repositorium** (`Werkzeuge/MdZuDocx`, C#, DocumentFormat.OpenXml,
   OMML-Formeln aus `$…$`; Mermaid über Playwright mit dem installierten Edge) und eine neue
   Markdown-Quelle aus den geltenden Papieren — rund 2–3 Tage Agentenarbeit; das Werkzeug bleibt
   dann erhalten und kann jede weitere Fassung bauen.
2. **Word aus dem Prüfprotokoll und den Papieren mit Bordmitteln** (Python-docx, ohne gesetzte
   Formeln, Bilder als Text) — schnell, aber deutlich unter der bisherigen Qualität.
3. **Word ruhen lassen**, bis die Konzeptfamilie steht (Beauftragung von G1); bis dahin gilt das
   Prüfprotokoll als Kurzfassung.

## 3 Regeln, die für diese Papiere gelten

- Markdown UTF-8 **ohne** BOM, **CRLF**; große Papiere gezielt lesen (`grep -n '^#'`).
- Entscheide nur als Nachtrag N1.x im Leitkonzept plus Statuszeile; Nachträge nie umschreiben,
  nur Vermerke nach dem Muster von N1.1 anfügen. Hauptteil und Papiere im Fließtext umschreiben,
  nicht relativieren; Kapitelnummern stabil.
- Wortwahl: „Bestandsweg" für den Altweg, „bis zur Ablösung (Stufe GA, Zeitpunkt offen)" statt
  „dauerhaft"; Schemaschritte mit Papiernamen (M3, M4, KU-S1, AK-S1 …), die Zielnummer nur als
  datierte Momentaufnahme aus `SchemaStand.Zielversion`.
- Keine Normzahlen der VDI 6007/6020/2078 in den Papieren; keine Hersteller- oder Produktdaten;
  Testdatenbank nur lesend; Laufwerk Z: nur lesen.
- Zahlen aus dem Code (Zeilennummern) am Arbeitsbaum nachmessen, bevor sie geschrieben werden.
- Agentenarbeit im Hauptbaum nur mit eigener `AGENT_LAEUFT`; eine fremde nie anfassen; solange
  eine fremde liegt, weder bauen noch `dotnet test` im Hauptbaum.
- Kein Commit und kein Push ohne Auftrag; der Anwender synchronisiert mit `GitHub_Sync.bat`.
- Modellwahl (Anwenderregel 22.09.2026, `CLAUDE.md` Abschnitt „Modellwahl und Agenten"): Opus
  orchestriert und arbeitet; Agenten mit `model: opus` (Papiere, Nachzüge, Konfliktauflösung),
  `model: sonnet` (Suchen, kleine Textpflege), `model: haiku` (Zählungen, Encoding-Prüfungen);
  Fable nur, wenn Opus die Aufgabe nachweislich nicht leisten kann — auch der Restabgleich aus
  2.1 läuft mit Opus.

**Linkprobe (Perl, ohne Build):**

```perl
use strict; use warnings; use File::Find; use File::Basename; use File::Spec;
my $root = '.';
my @files; find(sub { push @files, $File::Find::name if /\.md$/i }, "$root/Dokumentation");
push @files, "$root/CLAUDE.md", "$root/README.md";
my ($nf, $nl, $ne) = (0, 0, 0);
for my $f (sort @files) {
  open my $h, '<:raw', $f or die; local $/; my $c = <$h>; close $h; $nf++;
  my $dir = dirname($f);
  while ($c =~ /\]\(([^)\s]+?)(?:\s+"[^"]*")?\)/g) {
    my $z = $1; next if $z =~ /^(https?:|mailto:|#|<)/i; $nl++;
    $z =~ s/#.*$//; next if $z eq ''; $z =~ s/%20/ /g;
    my $p = File::Spec->rel2abs($z, $dir);
    unless (-e $p) { $ne++; print "FEHLT: $f -> $z\n"; }
  }
}
print "Dateien: $nf, Verweise: $nl, Fehler: $ne\n";
```

## 4 Fundstellen

| Was | Wo |
|---|---|
| Entscheide E1–E26 mit Wortlaut | [Statusdatei](../Status_Gebaeudesimulation_VDI6007.md) Abschnitt 1; Leitkonzept Nachtrag 1 (N1.1–N1.31) |
| Stufenplan G0, GB, G1–G7, GA, KU0–KU3, AK0–AK3 | Statusdatei Abschnitt 2; Umsetzungskonzept Kapitel 4; Löschliste der Stufe GA in Kapitel 6 |
| Offene Anwenderentscheide (66) und Festlegungen | [Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md), Kapitel 0 und 8.4 |
| Befunde der Prüfung je Blickwinkel | [Prüfprotokoll](2026-09-17_Pruefung_Konsistenz_Umsetzbarkeit.md), Kapitel 2 |
| Kühlung mit Wärmepumpen (Konzept gegen Code) | [Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md), Kapitel 4 und 5; Befund W |
| Trennung Altweg / VDI-Weg | [ADR-006](../ADR-006_Trennung_Altweg_VDI6007.md); Befund X Kapitel 4 (Grundlage der Stufe GA) |

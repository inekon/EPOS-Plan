# E31 Fachvorgabe: Der Wirtschaftlichkeitsbericht folgt dem gewählten Szenario (Nach #582)

Stand 29.09.2026, Sitzung „EPOS Plan Wirtschaftlichkeit" (lokal). Bauplatz nach Empfehlung der Wirtschaftlichkeit:
Cloud-Sitzung „EPOS-Plan Berichterstellung", die am 29.09.2026 mit dem Bau begonnen hat (ungecommittet im Worktree
geparkt). Fachliche Führung, Konzeptabsatz E31 und Abnahme bleiben bei der Wirtschaftlichkeit. Der Vermerk in #588
(„gehört der Wirtschaftlichkeits-Sitzung") meint diese Führung, nicht den Bauplatz.

## 1. Anlass und Entscheide

- **#582** (Dialoge und Korrekturen, 26.09.2026): „Zum Bericht ›" reicht Baustein, angehakte Versionen und Szenario als
  `BerichtVorbelegung` (`EPOS.UI/Seiten/Berichte/BerichtDaten.cs`) an die Berichtsseite. Dort wird das Szenario nur in der
  leisen Zeile genannt (`BerichtSeite.razor`, `VorbelegungUebernehmen`). Der Baustein Wirtschaftlichkeit rechnet weiter fest
  mit `WirtschaftlichkeitSzenario.ERWARTET` (neun Stellen in `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs`,
  eine in `ExcelBerichtGenerator.cs`) und zeigt die Bandbreite aller drei Szenarien.
- **Anwenderentscheid 27.09.2026** (Nach #582, eingetragen mit #588): ja, der Bericht folgt dem gewählten Szenario.
- **29.09.2026:** Die Cloud-Sitzung Berichterstellung fragt nach der Zuständigkeit; ihr Agent hat begonnen. Empfehlung der
  Wirtschaftlichkeit an den Anwender: dort weiterbauen, nach dieser Vorgabe. Die Bestätigung des Anwenders hält die
  Statuszeile der Umsetzung fest.

## 2. Regeln

1. **Wahl und Ablage.** Das Szenario des Wirtschaftlichkeitsberichts ist ein Feld der `BerichtsKonfiguration`
   (`EPOS.Kern/Allgemein/Bericht/BerichtsKonfiguration.cs`, JSON je Stammprojekt über `BerichtCtrl.Lade`), also **kein
   Schemaschritt**. Gespeichert wird der Schlüssel aus `WirtschaftlichkeitSzenario` (ERWARTET, BEST, WORST; Konstanten in
   `WirtschaftlichkeitDaten.cs`), Vorgabe ERWARTET; ein fehlendes oder unbekanntes Feld liest sich als ERWARTET (Altbestand
   und alte Vorlagenpakete bleiben gültig). Die Berichtsseite bietet die Wahl am Baustein Wirtschaftlichkeit (Klappliste mit
   den drei Anzeigetexten `WIRT_SZEN_*`, nur sichtbar, wenn der Baustein angehakt ist). Die `BerichtVorbelegung` belegt die
   Klappliste vor (SzenarioId der Ergebnisseite auf den Schlüssel abbilden; die Reihenfolge steht in
   `WirtschaftlichkeitSeiteGaben.cs`, Zeilen 141 bis 143); die leise Zeile bleibt. `BerichtAuftrag` reicht den Schlüssel an
   die Hülle, `SchreibeWord(k, daten, konfig)` und `Erzeuge(daten, konfig, ziel)` lesen ihn aus `konfig`: keine
   Signaturänderung.
2. **Was dem gewählten Szenario folgt (Wortbericht).** Alle Stellen, die heute fest ERWARTET wählen: die Kennzahltafel
   (Überschrift „Kennzahlen im Szenario ‚<Anzeigetext>'"), die Mehrjahresübersicht, die Brücke zur Kapitalwertdifferenz,
   das Bild „Kumulierte Barwerte je Version", die Bezugsergebnisse je Version (Aktualitätsprüfung, Referenzkessel,
   Erzeuger, Emissionen), die KWK-Modultafel und die Positionstafeln. Die Überschriften nennen den Anzeigetext des
   Szenarios; für Erwartet sind sie byte-gleich mit heute.
3. **Was unverändert bleibt.** Die Szenarienübersicht (Ungünstig, Erwartet, Günstig) mit Tafel, Spannenbild, Annahmenzeilen
   je Szenario, Szenarioabdeckung und Vorschlagstext ist der Normbeleg der Szenarioanalyse und bleibt vollständig, auch der
   Erwartungsfall als Punkt im Spannenbild. Das Dreierbild des Verlaufs bleibt (alle drei Szenarien, Strichart = Szenario).
   Punkt 9 der Anhang-E-Checkliste bleibt bei „Günstig und Ungünstig gerechnet" (`AnhangECheckliste.StandPunkt9`). Die
   Sensitivitätsanalyse bleibt auf Erwartet bezogen, solange der Sammler sie nur für Erwartet liefert; ihre Überschrift sagt
   das weiterhin ausdrücklich. Ein Rechenweg wird nicht berührt, der Referenzlauf gegen R23 bleibt unverändert.
4. **Excel.** Das Blatt „Wirtschaftlichkeit" behält seine drei Spaltengruppen in der Reihenfolge Erwartet, Günstig, Ungünstig
   samt Formeln (E8b, E14). Nur die Aktualitätsprüfung (`ExcelBerichtGenerator.cs`, heute Zeile 593) prüft das gewählte
   Szenario. Ist das gewählte Szenario nicht Erwartet, nennt eine Kopfzeile des Blatts es („Szenario des Wortberichts: …");
   bei Erwartet entfällt die Zeile, damit die Messlatte unverändert bleibt.
5. **Rückfall.** Fehlt für eine angehakte Version das Ergebnis des gewählten Szenarios, fällt der **ganze** Baustein auf
   Erwartet zurück und sagt es in einer Hinweiszeile (neue Ressource de/en), damit keine Tafel Zahlen zweier Szenarien
   mischt. Fehlt auch Erwartet, gilt der bestehende Weg (Veraltet- und Fehlgrund-Hinweise).
6. **Texte.** Neue Texte als Ressourcen de/en (Klappliste, Hinweiszeile, Excel-Kopfzeile); die Überschriften des Bausteins
   folgen dem heutigen Muster der Datei.

## 3. Prüfungen

- **Messlatten byte-gleich:** `EPOS.Kern.Tests/Messlatten/Bericht_Word_1030.txt`, `Bericht_Word_1030_Vorlage.txt`,
  `Bericht_Excel_1030.txt`, `Bericht_Word_Gruppe.txt`, `Bericht_Word_Gruppe_Vorlage.txt`, `Bericht_Excel_Gruppe.txt`
  (Vorgabe Erwartet). Eine Abweichung ist ein Fehler der Umsetzung, nicht ein Anlass, die Messlatte zu erneuern.
- **Neue Proben:** (a) `BerichtsKonfigurationJsonTests`: fehlendes Feld, unbekannter Wert und gültiger Schlüssel;
  (b) Bericht 1030 im Szenario Ungünstig: Überschrift nennt „Ungünstig", die Kennzahltafel trägt die WORST-Ergebnisse der
  Testdatenbank (`WirtschaftlichkeitErgebnis` je Version), die Szenarienübersicht ist gleich der Erwartet-Ausgabe;
  (c) Rückfall: Szenario ohne Ergebnis liefert Hinweiszeile und Erwartet-Zahlen; (d) Excel Ungünstig: Kopfzeile vorhanden,
  drei Spaltengruppen unverändert; (e) bUnit: Vorbelegung setzt die Klappliste, „Erstellen" reicht den Schlüssel im
  `BerichtAuftrag`, die gespeicherte Konfiguration zeigt die Wahl nach Neuaufbau. Deutsche Texte nur mit gepinnter
  Kulturvorrichtung (Standardkultur der Testprojekte ist en-US).
- **Gate:** Linux-Gate `Werkzeuge/Gate/gate_linux.sh` komplett; die Wachen der Dokumentation und der Lokalisierung
  (`LokalisierungWirtschaftlichkeitWacheTests`, `HuellenTextschluesselWacheTests`) gehören dazu.

## 4. Papiere und Zuständigkeiten

- **Berichterstellung Cloud:** Statuszeile mit der nächsten freien Nummer nach `git fetch` (am 29.09. 10:10: #590; #589 liegt
  auf `claude/wiki-help-assistant-docs-jllq1r`), Verweis auf diese Vorgabe; Nach-#582-Block auf „erledigt mit #59x";
  Wiki-Seiten „Wirtschaftlichkeit" (Anker `bericht-erzeugen`, `szenariofuss`) und „Berichtsvorlagen" (Anker `erstellen`)
  nachziehen; Logbuchsatz unter 1.2.0.5 in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`. Push nur auf
  `ios_migration_september`.
- **Nicht anfassen:** `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`
  und das Entscheidungsregister. Den Konzeptabsatz E31 (E6, E9a, W5-B-11: „im Erwartungsfall" wird „im gewählten Szenario,
  Vorgabe Erwartet") trägt die Wirtschaftlichkeit nach dem Push nach.
- **Abnahme (Wirtschaftlichkeit):** Bericht 1030 in Word im Szenario Ungünstig, Sichtprüfung der Kennzahltafel gegen die
  Einzelheiten der Ergebnisseite im selben Szenario; Szenarienübersicht unverändert; Messlatten byte-gleich; Gate grün.

## 5. Nicht Teil dieses Auftrags

Sensitivitätsanalyse je Szenario, Strichartwechsel im Dreierbild, ein wandernder Punkt im Spannenbild, ein Umbau des
Excel-Blatts, die Gruppenzahl im Kostenkapitel (Nach #555 b, getrennt committet). Bei Bedarf eigene Etappe mit
Anwenderentscheid.

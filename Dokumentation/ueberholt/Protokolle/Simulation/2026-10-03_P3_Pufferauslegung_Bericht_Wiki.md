# Protokoll P3 — Pufferspeicher-Auslegung: Bericht, Wiki, Hilfe (03.10.2026)

Abschluss der Stufenfolge aus [`2026-10-03_P1_Pufferauslegung.md`](2026-10-03_P1_Pufferauslegung.md) und
[`2026-10-03_P2_Pufferauslegung_Oberflaeche.md`](2026-10-03_P2_Pufferauslegung_Oberflaeche.md) nach dem Auftrag
„starte P3“ (Anwender, 03.10.2026). Grundlage: Konzept
[`Konzept_Pufferspeicher_Auslegung_EPOS-Plan.md`](../../Konzept_Pufferspeicher_Auslegung_EPOS-Plan.md) Abschnitt 7
(Stufe P3) und 10 (Wiki, Logbuch); mit diesem Protokoll ist das Konzept umgesetzt und nach `ueberholt/` gewandert. Bau
durch einen Opus-Agenten im Worktree, Zweig `claude/p3-pufferauslegung` auf `88d351bc`. Statuszeile **#688**.

## 1 Was gebaut ist

| Welle | Commit | Inhalt |
|---|---|---|
| W8 | `98390413` | Bericht: `PufferAuslegungCtrl.Gespeichert(idProjekt, nachrechnen)` liest `Tab_PufferAuslegung` samt Pufferbezeichner und rechnet jede Zeile mit gespeicherter Eingabe und aktuellem Projektstand nach (lesend); `VariantenDaten.Pufferauslegungen`, Sammler füllt nur den Stamm; Baustein `BausteineProjekt.PufferauslegungSchreiben` am Ende der Projektbeschreibung (Überschrift, je Puffer Eigenschaftstafel mit Klasse, Vorlage, Nutzungsprofil, Zonen, bemessendem Kriterium mit Herkunft, Empfehlung und gewähltem Volumen, Starts/Tag, Verlust kWh/d und W/K, Datum; Hinweise als Liste; Zeile bei abweichender oder fehlgeschlagener Nachrechnung; ohne Zeile kein Abschnitt); kein neuer Platzhalter, Vorlagen unberührt; 9 Ressourcenschlüssel `BER_PAUS_*` de/en; `PufferAuslegungBerichtTests` (5 Fälle, darunter Projektkopie von 1045) |
| W9 | `c8611483` | Wiki-Quellen: neu `Projekte/Wiki/Programm Dokumentation - Pufferspeicher auslegen.wiki` (174 Zeilen: Einstiege, Ablauf, Herkunftsmarken, Tiefe, vier Schritte, Kriterientabelle mit Normbezug, Bemessung, Beispiel mit runden Werten, Kennzahlen, Hinweise, Übernehmen, Speichern, Bericht, Grenzen, iPad); `Grundlagen - Pufferspeicher.wiki` Abschnitt „Auslegung“ (+32 Zeilen); Übersicht und Hilfetabelle in `Programm Dokumentation.wiki`; Nachbarseiten berichtigt (`Pufferspeicher`: Verweise und Taktaussage; `Brauchwasser-Zapfprofil`: Knopf „An Speicherauslegung übergeben…“ ist aktiv, iPad-Sperre; `Simulation`: dritter Knopf); `help_mapping.txt` `Form_PufferAuslegung.btn_Help = Pufferspeicher auslegen` |
| Merge | `c01e73e3` | Zusammenführung mit origin (#686 KP3 R5 Schema 174, #687 Projektkopien der Brennstoffe und Pufferauslegungs-Vorgaben Schema 175); keine Konflikte |

## 2 Festlegungen beim Bau

- Kennzahlen und Hinweise im Bericht stammen aus der Nachrechnung, weil `Tab_PufferAuslegung` sie nicht speichert;
  Sitzungseingaben ohne Spalte (Kriterienschalter, Expertenweg, Heizlast, Wohneinheiten) fehlen darin, eine abweichende
  Empfehlung wird genannt. Der Sammler rechnet die Bedarfsreihen einmal je Projekt.
- Herkunfts- und Rechenwegtexte bleiben deutscher Klartext aus dem Kern, auch im englischen Bericht.
- Kein Platzhalter `tabelle.pufferauslegung` für frei gestaltete Vorlagen (Folgeauftrag über Vorlagenfeldkatalog).
- Eine Repo-Quelle des Update-Logbuchs gibt es nicht; der Logbuch-Satz steht in der Statusdatei unter Version 1.2.0.7 (Anwenderentscheid 03.10.2026).
- Die Hilfe-Wache brauchte keinen Nachzug (Zeile ohne Anker, wie Zapfprofil und Betriebskalender).

## 3 Nachweise

Gate des Agenten (`c8611483` auf `88d351bc`): Kern-Filter 0 Fehler; Tests Kern 10 378 (1 übersprungen), UI 7 357,
KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen); Windows-Schale 0 Fehler; ResourceDesigner
unverändert; SQL-Prüfer 2 235 Texte, 0 Fundstellen; Referenzlauf CI-Sieben 7/7 PASS gegen R33; Wiki-Regex auf der
neuen und fünf geänderten Seiten 0 Treffer.

Gate auf dem Merge-Stand (`c01e73e3`, Schemastand 175): Kern-Filter 0 Fehler; Tests Kern 10 448 (1 übersprungen), UI 7 367 (davon `KalenderkarteInhaltTests.Das_Teppichbild_rechnet_…entprellt_einmal` im Gesamtlauf unter Last der parallelen Läufe rot, einzeln zweimal grün — KP2-Bestand, nicht P3), KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen); Windows-Schale auf Linux 0 Fehler; Referenzlauf 16/16 PASS gegen R33, 487/487 CSV byte-gleich; SQL-Prüfer 2 270 Texte, 0 Fundstellen; ResourceDesigner unverändert.

## 4 Offen

Wiki-Upload gebündelt (neue Seite und fünf geänderte Seiten) mit Logbuch-Satz unter 1.2.0.7;
Sichtabnahme unter Windows; iOS-Lauf nach Rückfrage (Adapter aus P2); Ressourcenschlüssel für Herkunft und Rechenweg;
Nachbarstufen für die Nutzen-Aufwand-Zeile (V47); Vorbelegung aus den Teillastfeldern der Welle M4; Platzhalter für
frei gestaltete Berichtsvorlagen.

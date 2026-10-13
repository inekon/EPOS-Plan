# Zapfprofil: Zonenferien als Liste, Schemaschritt 224 (W3a, 13.10.2026)

Protokoll der Welle **W3a** der Sitzung „Zapfprofilgenerator Cloud“ zu dem Vorschlag **ZB5** (Datenteil) und
dem Entscheid **ZB‑Q3 B** des
[Konzepts Zapfprofil — Benutzerführung](../../../aktuell/Konzept_Zapfprofil_Benutzerfuehrung_EPOS-Plan.md)
(Befund B6).

**Rahmen.** Worktree `agent-a07fde7d002b76739`, Zweig `worktree-agent-a07fde7d002b76739` von `f5cb9c391`
(Merge der Welle W1), Opus 5.5. Vier Commits `a5c1c098a`, `c63c051aa`, `58363146a`, `13f4d211d`. Ein
Schemaschritt, keine Änderung des Rechenwegs, die Maske bleibt bis W3b, wie sie ist.

---

## 1 Schemaschritt 224 `TwwZonenferienSchema`

Entscheid ZB‑Q3 B: eine Zone trägt **beliebig viele** eigene Ferienzeiträume, wie der Gebäudedialog. Statt
vier fester Spaltenpaare gibt es die Tabelle `Tab_TwwZonenferien` (`STRICT`):

- `ID` Primärschlüssel, `ID_Zone` mit Fremdschlüssel auf `Tab_TwwZone` und Löschweitergabe (die Beziehung über
  die ID);
- `Reihenfolge` ≥ 1, `UNIQUE (ID_Zone, Reihenfolge)`;
- `Name` wahlfrei;
- `Beginn` und `Ende` als Tag des Jahres 1 bis 365 (Gemeinjahr), `Beginn` nullbar (vom 1. Januar an); ein
  Zeitraum über den Jahreswechsel beginnt im Dezember, die Deutung bleibt beim Kern.

**Übernahme.** Je Zone werden die Paare `Ferienbeginn_n`/`Ferienende_n` der Reihe nach in Zeilen übertragen;
ein Paar ohne gültiges Ende entfällt, ein ungültiger Beginn wird leer. Eine Zone, die schon Zeilen trägt, wird
nicht ein zweites Mal übertragen. **Wegfall.** Die vier Spaltenpaare verschwinden aus `Tab_TwwZone` über den
Neubau nach dem Rezept des Hauses (die IDs bleiben).

**Kette.** Der Schritt hängt an 223 `KaeltebedarfSplitAbrechnungSchema`; die Nummern 223 und 224 sind am
13.10.2026 mit der Sitzung Anlagendialog getauscht worden (die Zonenferien liefen zunächst vorläufig an 221
beziehungsweise als 222).

## 2 Lesen, Speichern, Kopieren

- Kern: `ZapfprofilCtrl` (Lesen, Eingang, Speichern) und `ZapfprofilStand` führen die Ferien als Liste; der
  Zapfkalender (`Zapfkalender`) liest sie aus der Liste.
- Projektkopie (`ProjektDuplizierenCtrl`) und Projekttransfer nehmen die Tabelle als Kindtabelle der Zone mit.
- Hülle `ZapfprofilHuelle` und DTO `ZapfprofilDaten`: die Liste 1:1 mit Name, ohne Auffüllen auf feste Zahl.
- Die Ergebnisse bleiben gleich, solange eine Zone nicht mehr als vier Zeiträume trägt.

## 3 Anmeldungen

`SchemaStand` (Schemastand-Nachtrag), `Paketanhebung` (Umformung der Pakete alter Fassung),
`SchemaMigration` (neuer Schritt in der Kette), Schemawerkzeug `Werkzeuge/Testdatenbankschema`,
Testvorrichtungen, `Werkzeuge/Auslieferungsvorlage` (Tabelle leer in der Auslieferung),
`Werkzeuge/ZapfprofilValidierung` (Objektlauf), Ressourcen in beiden Sprachen mit neu erzeugtem Designer,
Umsetzungskonzept und `Referenzlaeufe/LIESMICH.md` (Schemastand).

## 4 Tests und Abnahme

Neu `EPOS.Kern.Tests/TwwZonenferienSchemaTests` (Schritt, Übernahme, Neubau, Wiederholbarkeit);
`TwwKopierstellenTests` um die Kopierstelle ergänzt, `ZapfprofilFerienKalenderTests`,
`ZapfprofilSpeichernTests`, `ZapfprofilCtrlTests` und die Hüllentests auf die Liste umgestellt; Tests der
Auslieferungsvorlage zählen die Tabelle (leer, `STRICT`). Ergebnisse im Worktree, soweit die Commit-Botschaften
sie nennen; maßgeblich ist das Gate auf dem Merge-Stand (Statuszeile).

## 5 Offen

Die Maske (Ferienliste, Jahresraster) folgt mit W3b. Die Testdatenbank wird auf dem Integrationsstand über das
Schemawerkzeug auf Schritt 224 gehoben; die Basis ändert sich dadurch nicht.

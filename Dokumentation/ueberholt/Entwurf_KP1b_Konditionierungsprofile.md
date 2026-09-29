# Entwurf KP1b — zweite Hälfte der Stufe KP1 (Konditionierungsprofile)

> **Umgesetzt am 29.09.2026** (Statuszeile #596): [Protokoll](Protokolle/Gebaeudesimulation/2026-09-29_KP1b_Konditionierung_zweite_Haelfte.md),
> Festlegungen der Umsetzung im Leitkonzept N1.63. Dieses Papier ist Geschichte, nicht Regelquelle.

**Stand 27.09.2026 · vorgelegt, Umsetzung auf Auftrag.** Grundlage: [Teilkonzept](../aktuell/Konzept_Konditionierungsprofile_EPOS-Plan.md)
Rev. 3 mit E54, Leitkonzept [N1.61 und N1.62](../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Protokoll
[KP1a](Protokolle/Gebaeudesimulation/2026-09-27_KP1_Konditionierungskalender.md) Abschnitt 6,
[Übergabe](../aktuell/Gebaeudesimulation/2026-09-27_Uebergabe_Gebaeudesimulation_Cloud.md) Abschnitt 2.1.

**Verfahren** (Regel „Entwurf vor Bau"): zwei Leser (Code-Bestand zu Schema und Kopierwegen, zu Rechenweg und Hinweisen),
zwei unabhängige Entwürfe — A „Datenmodell und Kopierwege zuerst", B „Rechenweg und Risiko zuerst" —, eine
Gegenprüfung, die jeden neuen Code-Befund beider Entwürfe nachgeprüft und ihre Widersprüche aufgelöst hat, dann diese
Synthese. Die tragenden Befunde sind zusätzlich von der Orchestrierung im Code nachgesehen. Alles nur lesend; die
Rohberichte lagen im Arbeitsordner der Cloud-Sitzung, nicht im Repositorium.

## 1. Befunde aus KP1a, die vor KP2 zu schließen sind

Keiner wirkt heute: Ohne Oberfläche entsteht kein angelegter Kalender und keine Vorgabezeile, kein Referenzprojekt trägt
eine. Sobald KP2 Kalender erzeugbar macht, wirken alle.

| # | Befund | Stelle | Folge |
|---|---|---|---|
| G1 | Ein Heizsollwert „aus" (NaN) in der ersten Vorlaufstunde (8 040) wird als Startzustand gesetzt | `Vdi6007Rechenweg.cs:299`, `Zonenlauf.cs:217`, `Zonenschleife.cs:398` (dort auch in die Jacobi-Schritte der Nachbarn, :413–416) → `Zonenmodell2K.Zuruecksetzen` (:252) | **Ausnahme, der Lauf bricht ab**; die Regel aus 3.6 (Startwert der unbeheizten Zone) ist nicht gebaut, eine Probe „aus in Stunde 8 040" (R3) fehlt |
| — | Die Nachtzeile der Lüftung wirkt **unbedingt**; `Bedingt_K` wird gelesen und durchgereicht, aber nirgends ausgewertet | `Standardfahrplan.cs:231`, `GebaeudeModellEingang.KonditionierungAufloesen` | widerspricht P9 (b) |
| G2 | Die Schwelle der Sommerlüftung bleibt mit Kühlkalender die Konstante `Kuehl_Sollwert − 3 K` | `Vdi6007Rechenweg.cs:289-292`, `Zonenlauf.cs:54-57` | 3.6 verlangt θ_K(h) − 3 K, bei „aus" 23 °C |
| G3 | Die Zulufttemperatur gekoppelter Zonen trägt nur den Zusatzleitwert der Sommerlüftung, nicht den des Lüftungskalenders | `ZonenEingang.cs:150-156` | der Kalenderüberschuss wird mit Mischluft statt Außenluft gerechnet; ohne Kalender ist die Sommerlüftung richtig |
| G6 | Die konstante Kühlprüfung läuft mit Kühlkalender zusätzlich zur stündlichen | `GebaeudeModellEingang.KuehlungAufloesen` (:1726-1733) gegen `KuehlpruefungStuendlich` | ein gültiger Kühlkalender kann abgelehnt werden (F17 unvollständig) |
| NB9 | Ein Wert in der Vorgabezeile schlägt die Bestandsspalte; `KonditionierungCtrl.Vorgabe` schreibt Zahlen in jede Zeile ohne Weiche | `Vorgabematrix.cs:344`, `KonditionierungCtrl.cs:423-490` | „ein Ort je Zelle" (5.6) ist nur Konvention; der alte Dialog zeigte wirkungslose Werte |
| NB5 | `KonditionierungCtrl` prüft `ReadOnly` bei keinem Schreibweg | `KonditionierungCtrl.cs` | gesperrte Katalogbauten und Vorlagen sind beschreibbar |
| NB3 | Die Vorschau des Arbeitsstands (Id 0) liest keine Konditionierung | `GebaeudeBedarfCtrl.cs:639-655` | Vorschau und Lauf weichen ab, sobald Katalogbauten Kalender tragen |
| R14 | Der Hinweis „Kühl-Nachtwert wirksam" fehlt; er gehört an den Übergang, an dem ein abgeleiteter Kühlkalender den Nachtwert der Bestandsspalte erstmals wirksam macht (`Konditionierungdatenweg.Satz` liefert ohne Zeile `null`) | `Konditionierungdatenweg.cs:66-69` | R14 ohne Prüfweg |

Offen aus der Liste von N1.61, im Code bestätigt: F16 (die Kennzahlen lesen nur `Nachtzeit.Nutzungszeit`), die
Auslegungswerte aus 3.6 (Übergabe `:1305` noch `SollTag`, Kälte `:1444` und `Auslegungskuehllast` `:1533/1541` noch
`Kuehl_Sollwert`, Auslegungsheizlast mit dem Jahresminimum des Luftwechsels `:1353-1355`), F21 (`:1979`, noch gegen
`SollTag`, geprüft in `Daten()` vor dem Heizkalender), der Hinweis auf Untertemperatur. **Konzept 5.5 beschreibt
Zielbilder, nicht den Ist-Stand:** `CopyFromStamm`, „Speichern unter" (heute Logik in
`EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor`), das Duplizieren eines Katalogbaus und der Prüfbericht der
Auslieferungsvorlage führen keine Konditionierung; `KonditionierungsvorlageCtrl` fehlt. `KINDER`, Duplikat, Variante und
`.wpx` tragen die drei Tabellen dagegen schon (KP1a).

## 2. Wellenplan

Nach W1 zwei Spuren in eigenen Worktrees — **R** (Rechenweg) und **D** (Daten) —, jede Welle mit grünem Gate committet.

| Welle | Inhalt | PT |
|---|---|---|
| **W1 Schema 152** | Abschnitt 3; Werkzeugprobe R4 (erste Teilindizes im Schema) | 1–1,5 |
| **R1 Korrekturen** | G1 auf allen drei Startwegen samt Hinweis, G2, G3, G6, Hinweis R14; Probe Heizperiode mit AK1 im vollen Lauf | 1–1,5 |
| **R2 Nachtauskühlung** | bedingt nach P9 (b), je Zone (Festlegung 3); Kennzahl bis in die Datenbank (mit Spaltenwächter, iOS migriert nicht); Test `KonditionierungReihenTests` „Der_Ueberschuss_der_Nachtlueftung…" als Semantikwechsel umschreiben; Laufzeitmessung R7, Zwischenspeicher nur bei Befund | 2–2,5 |
| **R3 Nutzung und Auslegung** | F16 nur in den Kennzahlen; Auslegungswerte Übergabe, Kälte, Heizlast; F21 als Verzweigung; Hinweis auf Untertemperatur | 1,5–2 |
| **D1 Kopierwege** | ein Kopierer; `CopyFromStamm`, erneute Übernahme, „Speichern unter" (Kernmethode und Hülle, Razor unverändert), Katalogbau duplizieren; Zellenort-Weiche auch in `Vorgabe`; Schloss; Arbeitsstand; Proben für Duplikat, Variante, `.wpx` und Paketanhebung 151 → 152 | 2–2,5 |
| **D2 Auslieferungsvorlage** | Prüfbericht „Konditionierung" je Eigentümerart; Katalogbereinigung räumt eigene Vorlagen über die Kaskade | 0,5–0,75 |
| **D3 Vorlagen** *(Schnittlinie)* | `KonditionierungsvorlageCtrl` (listen je Größe, als Vorlage speichern nach E54, übernehmen nach P12, umbenennen, löschen, duplizieren, Namensregel), Werkzeuge Zeitfenster und Feiertage; kann folgenlos an den Anfang von KP2 wandern | 1,5–2 |
| Papiere | Nachtrag der Festlegungen im Leitkonzept, Protokoll, Statuszeile | 0,5 |

**Summe 10,5–13 PT**, kritischer Pfad mit zwei Spuren rund 6–8 PT. Kollisionen der Spuren: beide `.resx` und
`Resource.Designer.cs` (getrennte Präfixe `SIMENG_KOND_*` und `KOND_MSG_*`, nach jedem Merge
`designer_neu.py schreiben`), `Konditionierungdatenweg.cs` und `Konditionierungssatz.cs` (die Änderung der Spur D zuerst
mergen). **Byte-Gleichheit:** Jede Änderung hängt an einem Kalender, einer Vorgabezeile oder einem „aus", das kein
Referenzprojekt trägt; jeder Nullzweig steht wörtlich. Die neuen Ergebnisspalten ändern den Referenzlauf nicht, weil
`Referenzlauf/Ergebnisexport.cs` `Tab_ErgebnisGebaeude` und `Tab_ErgebnisZone` nicht liest. Gerechnet wird jede Welle
gegen R23, fünfzehn Projekte byte-gleich. Kein iOS-Lauf: Der Seed der Hülle ist die Testdatenbank.

## 3. Schemaschritt 152

Eine Klasse neben `KonditionierungSchema` mit `SCHRITT = KonditionierungSchema.SCHRITT + 1`, `Lesbar`, `Vollstaendig`,
`Ausfuehren`; wiederholbar, ein `VorgangOhneFremdschluessel`. Nummer unmittelbar vor dem Schemacommit gegen `origin`.

1. `Tab_Konditionierungsvorlage_STAMM` nach 5.7 (`STRICT`; `Groesse`, `Bezeichner` 1–80 Zeichen, `Beschreibung`,
   `Nutzung`, `ReadOnly` mit `CHECK (ReadOnly IN (0,1))`), Namensregel als benannter Index
   `(Groesse, Bezeichner COLLATE NOCASE)`.
2. **Fremdschlüssel `ID_Vorlage`** in `Tab_Konditionierungskalender` und `Tab_Konditionierungsvorgabe` per Tabellenneubau
   nach Schritt 96 (`ProjektFremdschluessel`): Zieltext aus `sqlite_master` mit genau einer eingesetzten Klausel
   `REFERENCES "Tab_Konditionierungsvorlage_STAMM" ("ID") ON DELETE CASCADE`, `RENAME` unter `legacy_alter_table = ON`
   (sonst schreibt SQLite 3.53 den Verweis der Periodentabelle auf `…_alt` um), `INSERT … SELECT`, `DROP`, Indizes neu,
   `foreign_key_check` leer vor dem Commit.
3. **Acht Teilindizes** der Eindeutigkeit (Kalender und Vorgabe je Eigentümerart Gebäude ohne Zone, Zone, Katalogbau,
   Vorlage); Dubletten brechen benannt ab, Waisen an `ID_Vorlage` werden gezählt (erwartet 0).
4. `Nachtauskuehlstunden_H INTEGER CHECK (… BETWEEN 0 AND 8760)` an `Tab_ErgebnisGebaeude` und `Tab_ErgebnisZone`,
   nullbar.

Einhängen: `SchemaMigration` der Windows-Schale, `Werkzeuge/Testdatenbankschema`, `EPOS.Kern.Tests/TestDatenbank`,
`SchemaStand.Zielversion`, `SchemaKatalog`, `DbWerte` (Nutzungskennwörter), `Paketanhebung.STUFEN`
(`Stufe(152, Art.Ddl, …)`), Kettenprobe der Zielversion, STRICT-Zählung der Auslieferungsvorlage 152 → 153,
Schemastand in `Referenzlaeufe/LIESMICH.md`, Testdatenbank mit aktivem LFS-Filter. Kein DML an Bestandsdaten.

## 4. Vorgeschlagene Festlegungen der Umsetzung

Benannt, nicht entschieden — sie gehen mit der Umsetzung als Nachtrag ins Leitkonzept; Widerspruch ist bis zur
Beauftragung billig.

1. **Fremdschlüssel per Neubau** statt Bindung im Controller: hebt N1.61 Nr. 1 auf und stellt 5.1 her. Nur mit ihm räumt
   `--kataloge readonly` eigene Vorlagen samt Kalendern, Perioden und Vorgaben; ohne ihn blieben Waisen, die kein
   `foreign_key_check` meldet.
2. **Teilindizes jetzt**, keine Trigger. Die Namensregel prüft der Controller mit `OrdinalIgnoreCase` und getrimmt;
   `NOCASE` faltet nur ASCII und ist die Rückfallsperre.
3. **Nachtauskühlung:** geteilt wird die Nutzerreihe (die Infiltration ist nie bedingt). Fenster und Tagwert n_T kommen
   aus der Matrix des Eigentümers, dessen Kalender gilt — auch bei angelegtem, von Hand geändertem Kalender. Bedingt ist
   max(0, n(h) − n_T) in den Stunden des Fensters, das Jahresminimum wird über den unbedingten Teil gebildet; ein
   Überschuss außerhalb des Fensters bleibt unbedingt, ohne n_T gibt es keinen bedingten Anteil (Hinweis). Die Regel ist
   `Sommerlueftungsregel` mit einem Feld für ΔT (`Bedingt_K`, NULL = 2 K), ein Exemplar je Zone; Schwelle je Stunde
   θ_K(h) − 3 K, wo θ_K endlich ist, sonst 23 °C; der Zustand läuft durch. Eine Stunde zählt, wenn die Regel an ist und
   der bedingte Anteil > 0 — gleich, welcher Luftwechsel gewinnt; das Gebäude zählt sie, wenn eine beheizte Zone sie hat.
   Der Zweig ohne Lüftungskalender bleibt wörtlich (N1.61 Nr. 11); θ_Lue nimmt denselben Zusatzleitwert wie der Rand.
4. **Vorlaufstart bei „aus"** nach 3.6 auf allen drei Startwegen, mit Hinweis. Die konstante Kühlprüfung gilt nur ohne
   Kühlkalender.
5. **F16** wirkt nur in den Kennzahlen; `GebaeudeModellEingang.Nutzungszeit` steuert den Sollwertfahrplan und bleibt
   unberührt. Ohne Anwesenheitsstunde gilt die Nachtzeit (Hinweis, sonst Teilung durch null in
   `MittlereRaumtemperaturHeizzeit`).
6. **Auslegung und F21** als Verzweigung mit wörtlichem Bestandsausdruck im Else-Zweig, keine Umformung; ohne endliche
   Nutzungsstunde gilt der Bestand, F21 entfällt.
7. **Untertemperatur** nur als Protokollhinweis mit Zahl und tiefster Unterschreitung; ein Tag liegt außerhalb der
   Heizperiode, wenn seine Quelle (`Konditionierungskalender.Quelle(tag)`) die Saisonperiode ist.
8. **Ein SQL-Kopierer** (`Konditionierungskopie`, Spalten aus `pragma_table_info`, Alt→Neu-Zuordnung im selben
   Vorgang) für Katalog → Projekt, Projekt → Katalog, Katalog → Katalog, Vorlage ↔ Ziel; `Katalogkopie` bleibt
   unverändert. Duplikat, Variante und `.wpx` bleiben beim `KINDER`-Plan.
9. **Zellenort-Weiche** auf allen Schreibwegen, auch in `KonditionierungCtrl.Vorgabe`; das **Schloss** sperrt jeden
   Schreibweg.
10. **„Speichern unter"** nimmt nur die Gebäudeebene mit (Kernmethode, Hülle bindet den Eigentümer); im Katalogmodus
    wirkt es wie Duplizieren.
11. **Vorlagen:** Herkunft nur als Text in `Bemerkung`, nie als Id am Projekt; eine Vorlage trägt keine Perioden der
    Arten FERIEN und BETRIEBSPAUSE und nach E54 weder Nennwert noch Saison.
12. **Rangbänder** (größerer Rang gewinnt, N1.61 Nr. 5): Feiertage 100–108 — unter den Ferien, sonst heizte ein Feiertag
    in den Ferien „wie Sonntag" —, Ferien 200 + k, eigene und übernommene Perioden 310–899, Saison 900; eine Kollision
    wird benannt abgelehnt.
13. **Stufenzuordnung:** gbXML `SollHeizenC`, Ferien des Zapfprofils und „Zeitstruktur übernehmen" in die erste
    Kernwelle von KP2; Bericht, CSV, KI-Sicht und Variantenvergleich der Lüftungsstunden in KP3 (E54).

## 5. Entscheid E54 (27.09.2026)

Zwei Fragen der Synthese hat der Anwender entschieden (Leitkonzept N1.62, Teilkonzept 9.4):

- **„Als Vorlage speichern" nimmt weder Nennwert noch Saison mit** — abweichend von der Empfehlung (Saison ja, Nennwert
  nein) und von 3.5 alter Fassung. Eine eigene Vorlage trägt Zeitstruktur und die Werte der Nutzungszeilen (Tag, Nacht
  mit Zeiten und `Bedingt_K`, Wochenende, Ferienwert); Nennwert und Heiz- bzw. Kühlperiode bleiben beim Ziel.
- **Beide Lüftungskennzahlen in Bericht und Export** — nach Empfehlung: KP3 bringt `Nachtauskuehlstunden_H` und
  `Sommerlueftungsstunden_H` in Bericht, CSV-Export, KI-Sicht und Variantenvergleich (rund 0,25 PT zusätzlich).

## 6. Nicht in KP1b

Oberfläche samt Rückfragetexten und Ergebniszeile (KP2); Saat der 14 Vorlagen (KP-S1b) und
`KonditionierungsvorlagenWacheTests` (KP2); gbXML, Zapfprofil, „Zeitstruktur übernehmen" (erste Kernwelle KP2);
KI-Aktionswissen (KP2); Bericht und Export der Lüftungsstunden, Rampenstunden in F16, neues Referenzprojekt, Einfrierregel
und Basis (KP3); Rechenschritte und Wiki (KP4); Kühlkalender je Zone (KU3); `Sommerlueftungsstunden_H` an
`Tab_ErgebnisZone` (nicht beauftragt).

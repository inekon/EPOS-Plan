# H3 — Pflichtpositionen im Betrieb: Löschsperre, Auto-Anlage, Anzeigetexte (Protokoll)

Etappe H3 des Pflichtpositionen-Vorhabens — die offenen Punkte **H1-2, H1-3 und H1-4**
aus [`H1_Pflichtpositionen_Protokoll.md`](H1_Pflichtpositionen_Protokoll.md), umgesetzt
am **29.08.2026** auf Branch `Pufferspeicher` (Auftrag „fahre fort mit h1-2/3/4");
Commit ohne Push (Etappenregel § 8 des BHKW-Konzepts).

---

## 1 Umgesetzt

### 1.1 Verschlusslücke der H1-Saat: das Merkmal wandert jetzt mit

**Befund beim Ansetzen:** `IstPflicht` wurde von keinem Laufzeitpfad gelesen — Schritt 59
markiert Vorlagen und Bestand, aber eine **neue** Übernahme erzeugte Projektzeilen ohne
Merkmal; Löschsperre und Auto-Anlage liefen ins Leere. Geschlossen durch:

- `KostenVorlagenPosition.IstPflicht` + Laden in `KostenVorlagenCtrl.Positionen`
  (spaltentolerant über eine Probe nach dem Muster `WirtschaftlichkeitCtrl.SpalteVorhanden`
  — eine nie migrierte Datenbank liest wie bisher).
- `KostenVorlagenUebernahmeCtrl.AusVorlage`: **jede** Übernahme reicht das Merkmal per
  `KostenProjektPositionenCtrl.PflichtSetzen(id, true)` in die neue Projektzeile durch.

### 1.2 H1-2 — Löschsperre, zweischichtig (Muster ReadOnly-Schutz)

| Schicht | Verhalten |
|---|---|
| **Controller** `KostenProjektPositionenCtrl.Loeschen` | verweigert bei `IstPflicht(id)` — greift unabhängig vom Dialog |
| **Dialog** `Form_KostenKomponente.Zeile_LoeschenAngefordert` | im Projektmodus eigene Meldung **mit Ausweg**: „… kann nicht gelöscht werden. Zum Deaktivieren den Satz bzw. Betrag auf 0 setzen." (`KDLG_MSG_PFLICHT_LOESCHEN`, de + en, `Text_`-Rückfallmuster) |

Der Adminkontext (Vorlagenpflege) bleibt frei — dort **definiert** der Anwender die
Pflicht; die Standardvorlage selbst ist weiterhin nur gegen Löschen geschützt (Ä8).
Fehlende Spalte oder Lesefehler bedeuten **keine Sperre auf Verdacht**.

### 1.3 H1-3 — Auto-Anlage nach dem Anlagen-Speicherweg

`KostenVorlagenUebernahmeCtrl.PflichtpositionenSicherstellen(projektId)`: je Anlagenzeile
des Projekts die Pflichtpositionen der **Standard**-Betriebskostenvorlage ihrer
Komponente, über die vorhandene `AusVorlage`-Mechanik mit neuem Filter `nurPflicht`
(NurAnlegen-Dublettencheck je Anlage, Ä20/Ä25).

- **Einbau:** `WizardCtrl.Add_WP_Waermeerzeuger`, bewusst NACH
  `ZuordnungReparieren`/`AnkerNachziehen` — erst dort hängen die Bestandspositionen
  wieder an den neuen Anlagen-Ids des Del+Add-Speicherwegs, und der Dublettencheck
  erkennt sie (sonst Doppel bei jedem Speichern). BEST EFFORT wie die Nachbarblöcke.
  Damit deckt der eine Einbau **alle** Anlege-Wege ab: Wizard, Karten, Kontextmenüs
  (sie alle speichern über diese Methode).
- **Typ-Landkarte:** `ID_Type` (WizardItemClass) → `Tab_KostenKomponente.ID` (feste
  Nummern 1…7, Begründung `Form_Kosten.GetKomponentenID`). **Referenztypen 5–9 bekommen
  bewusst keine Positionen**; die Projektkopie (`KomponentenUebernahmeCtrl`) und der
  Migrationspfad (`ProjektPuffer`) bleiben unberührt — Kopien bringen ihre Positionen
  selbst mit.
- **Ergebnisneutral:** Vorlagen tragen keine Sätze (KL-Regel „Struktur, nicht Preise") —
  jede neue Zeile steht auf 0 €/a. Beim ersten Speichern eines Bestandsprojekts entsteht
  damit einmalig die Pflicht-Struktur (der Geist der Nachzieh-Entscheidung P4/M-3).

### 1.4 H1-4 — Anzeigetexte

`BM_P_ENDENERGIEKOSTEN` („% der Endenergiekosten" / “% of final energy costs") und
`BM_P_ENDENERGIEBEDARF` („% des Endenergiebedarfs" / “% of final energy demand") in
`MyResource` (de + en) — der `BemessungKatalog` liest sie über seinen `ResourceKey`-Weg.
Dazu `KDLG_MSG_PFLICHT_LOESCHEN` (1.2).

> **CS0102-Falle bestätigt:** Die parallel geöffnete VS-Instanz regenerierte
> `Resource.Designer.cs` unmittelbar nach dem resx-Edit — die Hand-Einfügungen wurden
> nach Hausregel entfernt, die generierten Fassungen behalten (Fallstrick-Eintrag der
> `CLAUDE.md` traf wörtlich ein).

---

## 2 Nachweise

**Build:** VS-MSBuild x64 Debug, OutDir umgeleitet — **grün**; Warnungsprofil exakt der
Altbestand (2× CS0108, 2× CS0109, 1× CS1998).

**Harness `..\dev\h3pflicht\`** (gitignored): Phase 1 **nur lesend** gegen die
Produktivdatenbank, Phase 2 **schreibend ausschließlich gegen eine Kopie**
(151.949.312 Bytes; `Settings.DBPath` per Reflection umgebogen und rückverifiziert).

| Probe | Ergebnis | Soll |
|---|---|---|
| [1] Pflicht in den 7 Standardvorlagen (über `Positionen`, also den NEUEN Leseweg) | 3+3+2+3+3+3+3 = **20** | 20 (H1-Migration; PV = 2, Hilfsenergie dort keine Pflicht) |
| [2] Bestandszeilen `IstPflicht = TRUE` | **3**; `IstPflicht(101600554)` = True, `IstPflicht(101600309)` = False | 3 / True / False |
| [3] `Loeschen(Pflichtzeile)` | **GESPERRT**, 183 → 183 | gesperrt, unverändert |
| [3] Gegenprobe `Loeschen(freie Zeile)` | gelöscht, 183 → **182** | −1 |
| [4] Projekt 1042 (7 Anlagen) | Erstlauf **21** angelegt (7 × 3), 0 → 21 Betriebszeilen, **alle 21 mit Pflichtmerkmal**; Zweitlauf **0** | 21 / idempotent |
| [4] Projekt 1018 (4 Anlagen, 3 Pflicht im Bestand) | Erstlauf **9** = 4 × 3 − 3 vorhandene (Dublettencheck traf exakt), 13 → 22, Pflicht 12 = 9 + 3; Zweitlauf **0** | 9 / idempotent |

Die Beispielzeilen sind anlagenscharf („Anlage 14818: Hilfsenergiekosten (Pumpen) /
Instandhaltung Wärmepumpe / Wartung Wärmepumpe" — die Booster-WP von 1042).

**ACE-Befund am Rande** (erster Harness-Lauf): Eine VOR fremden Schreibungen geöffnete
zweite ACE-Verbindung sieht diese verzögert — Kontrollzählungen zeigten 183 → 183 trotz
echtem Delete und „0 → 0" trotz 21 Anlagen. Behoben, indem jede Kontrollzählung ihre
eigene frische Verbindung öffnet. Wer je zwei Verbindungen mischt, sollte das wissen.

---

## 3 Offen (Fortschreibung)

| Nr. | Punkt |
|---|---|
| ~~H1-2~~ | ~~Löschsperre~~ — **erledigt** |
| ~~H1-3~~ | ~~Auto-Anlage~~ — **erledigt** (Live-Probe des Wizard-Durchgangs beim nächsten Anwender-Speichern; die Kernmechanik ist gegen die Kopie bewiesen) |
| ~~H1-4~~ | ~~Anzeigetexte~~ — **erledigt** |
| H1-1b | Bezugsgrößen der übrigen KD1-Arten (Gerätekatalog-Leseketten) |
| H1-6 | Nachzieh-Migration Bestandsprojekte — durch 1.3 faktisch entschärft: das erste Speichern zieht nach; ein eigener Migrationsschritt bleibt Option |
| H2-1 | Mengen-Ausweis beim Dialog-Speichern („Stand des Laufs vom …") |

## 4 Geänderte Dateien

```
Controller/KostenVorlagenCtrl.cs            IstPflicht-Feld + tolerantes Laden (§ 1.1)
Controller/KostenProjektPositionenCtrl.cs   PflichtSpalteVorhanden/IstPflicht/
                                            PflichtSetzen; Loeschen gesperrt (§ 1.2)
Controller/KostenVorlagenUebernahmeCtrl.cs  AusVorlage(nurPflicht), Durchreichung,
                                            PflichtpositionenSicherstellen, Typ-Landkarte (§ 1.3)
Controller/WizardCtrl.cs                    Einbau nach ZuordnungReparieren (§ 1.3)
Views/Kosten/Form_KostenKomponente.cs       Dialogsperre mit Ausweg (§ 1.2)
MyResource/Resource.resx / .en-US.resx /    BM_P_ENDENERGIE* + KDLG_MSG_PFLICHT_LOESCHEN
  Resource.Designer.cs                      (Designer: VS-generierte Fassung)
```

Harness `..\dev\h3pflicht\` und die DB-Kopie im Scratchpad gehören nicht zum
Lieferumfang.

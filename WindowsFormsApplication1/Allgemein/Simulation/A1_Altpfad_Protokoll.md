# Paket A1 — Altpfad-Rückbau: Umsetzungsprotokoll

Stand: 27.08.2026 · Branch `Pufferspeicher` · Bezug:
[`Konzept_Brauchwasser_Heizung_Pufferspeicher.md`](Konzept_Brauchwasser_Heizung_Pufferspeicher.md)
Leitentscheidung **L1** („Altpfad entfällt ersatzlos"), Kapitel 9 (Schritt 51), Paketzeile A1.
Build x64 Debug: 0 Fehler. **Meilenstein Z1: ein Rechenweg** — jeder Lauf rechnet über die
dreikanalige Speicherstufe mit herausgelöster Ladephase.

## 1. Umfang

Der einkanalige Altpfad ist **ersatzlos entfallen**: die Rechenweg-Weiche samt Projekt-Flag
`Kaskade_Zweikanalig`, die einkanaligen Modul-Altwege (WP-Altschleife ~960 Zeilen, Kessel-,
Solar-Altweg), der Registry-Block 1 der Alt-Zuordnung `Z_ProjektPufferSp` (K2-O7), die
`WS_`-Spiegelung des Senkendialogs (S1-O5) und die Alt-Zuordnungs-Brücke der Oberfläche
(`WpSenkeSpiegeln`). Migrationsschritt 51 übernimmt vorher die Betriebstemperaturen aus der
Alt-Zuordnung in die Pufferzeilen. Dazu als Sofortmaßnahme (Nutzerbefund 27.08., Booster-Kette):
ehrliche Quellen-Anzeige und Warnkriterium für Sole-/Wasser-Wasser-WPs ohne Quelle.

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Schritt 51** | 51a Temperaturübernahme: Puffer ohne vollständiges Paar (`ΔT ≤ 0`/leer, exakt die `Init`-Regel) erhalten Vorlauf/Rücklauf aus der `Z_ProjektPufferSp`-Zeile nach exakt der bisherigen Engine-Vorrangkette (Projektgrenze, ID-**oder**-Bezeichner-Probe je Zeile in Prioritätsreihenfolge); jeder Puffer im Migrationslog benannt (übernommen bzw. „bleibt auf Rückfall-ΔT"). 51b `Kaskade_Zweikanalig` im Bestand auf WAHR (zielgenaues UPDATE). `ZIEL_VERSION` 50 → **51**; nichts wird gelöscht — Tabelle und Spalte bleiben stillgelegte Altlast | `SchemaMigration.cs`, `SchemaKatalog.cs` (Stilllegungs-Doku), `Z_ProjektPufferSpCtrl.cs` (Kopf) |
| **Weiche raus** | `Do_Simulation_Intern` ruft nur noch die Speicherstufe; Flag-/BHKW-/Verbund-Auswertung, alle drei Rechenweg-Protokollhinweise und die Modulschleife des Altpfads entfallen. `SimulationControl.KaskadeZweikanalig` bleibt als konstantes Altlast-Feld (Leser `Form_Simulation_Detail`, fällt mit L) | `SimulationControl.cs` (−831 Zeilen) |
| **Modul-Altwege** | WP: `Berechnung()`/`Berechnung_Stundenschleife()` (~440 Z.), `_speicherLaden`, `SenkeAbziehen(string,double,ref,ref)`-Kette bis in die Kaskadenschleife; Kessel: `Berechnung(int)`/`Heizkessel_Simulation`; Solar: `Berechnung(int)`; Runner-Zweige — alle Löschungen mit Aufrufer-Beleg | `SimulationWaermepumpe.cs` (−521), `SimulationSPK.cs`, `SimulationSolarthermie.cs`, `SimulationRunner.cs`, `Kaskadenschleife.cs` |
| **Registry-Block 1 (K2-O7)** | WP-Senkenspeicher kommen ausschließlich aus der Senkenliste; erzwungene Rolle, Z-Schwellen, `ZuordnungsTemperaturen` und die Block-1-Klassen-Set-Ausnahme entfallen; Temperatur-Vorrangkette ersatzlos (Schritt 51 hat übernommen, Rückfall-ΔT 10/20 K bleibt); `WaermequelleClass.Quelltemperatur` ohne die zwei Z-Rückfallstufen | `SimulationControl.cs`, `WaermequelleClass.cs`, `SimulationPufferspeicher.cs` |
| **S2-B1 geschlossen** | `IstEigenerSenkenPuffer` fragt die im Lauf geladene Senkenliste statt `WS_ID_Puffer/2` — der Engine-Kurzschlussguard greift auch ab Rang 3 | `SimulationControl.cs` |
| **WS_-Spiegelung raus (S1-O5)** | `Form_Waermesenke` schreibt ausschließlich `Z_AnlageSenke` (Wirkprobe: zehn `WS_*`-Felder nach Dialog-Speichern byte-gleich, Senkenliste 1 → 3 Zeilen); `WaermesenkeClass`: WS_-Schreibweg, `Lesen`, Anzeige-Doppel und der `AusAltspalten`-Rückfall entfernt (Migrationspflicht macht „Tabelle fehlt" unerreichbar); `Pruefen` rangübergreifend; Übersichts-Abfrage ohne WS_-Spalten, Karten/Schema lesen die Kette (Speicherkarte sah Lader ab Rang 3 vorher nicht) | `Form_Waermesenke.cs`, `WaermesenkeClass.cs`, `Form_Simulation_Config.Uebersicht.cs`/`Karten.cs`, `SchemaModell.cs` |
| **Alt-Zuordnungs-Brücke raus** | `WpSenkeSpiegeln`, `ZuordnungBrueckeAnwenden`/`ZuordnungenLaden`/`_zuordnungen`, Zelleditor der unsichtbaren Zuordnungsliste, Delete/Insert-Zyklus, `PufferSpCtrl.SetTemperaturen`; die Temperaturpflege liegt bei `Form_PufferSp_Projekt` (Tab_Pufferspeicher ist die Wahrheit) | `Form_Simulation_Config.cs`, `PufferSpCtrl.cs` |
| **Dialog-Abrisse** | `Form_KonfigPufferspeicher` gelöscht (4 Dateien; Aufrufer als No-op-Rümpfe, Designer-Regel); Checkbox „Zweikanalige Kaskade" samt Automatik/Handler entfernt (Extrapolations-Häkchen rückt nach links) | `Views/Simulation/` |
| **S2-O6 erledigt** | `SenkenlistenLadenStill` (ohne Protokollkanal); `Ladeordnung.Ladereihenfolge` liest die Senkenliste — Schema-Kreisziffern stimmen ab Rang 3 (Wirkprobe: Rang 1 „1 von 2", Rang 3 „1 von 1") | `WaermesenkeClass.cs`, `Ladeordnung.cs` |
| **Ressourcen** | 35 entfernt (S2-O4-Trio + 32 durch A1 verwaist, je Fundstellen-Beweis), 3 neu: `SIM_PUFFER_PROZESS_KURZ`, `SIMQ_QUELLE_FEHLT`, `SIMWARN_QUELLE_FEHLT`; Bestand 2536, DE/EN deckungsgleich; Nachtrag in `Lokalisierung_Katalog.md` | `MyResource/` |
| **Sofortmaßnahme Quelle** | Nutzerbefund: „Sole/Wasser-WP hat Quelle Außenluft — nicht möglich". Chip zeigt bei Sole-/Wasser-Wasser-WP mit leerem `WQ_Typ` jetzt **„Quelle wählen!"** statt „Außenluft"; neues weiches Kriterium **`QUELLE_FEHLT`** im Warnkatalog (Karte + Laufprotokoll: „rechnet ersatzweise mit der Außenluft, was für diese Bauart fachlich nicht passt"). Die echte Quellkopplung (Stundentemperatur aus dem Puffer) kommt mit Paket B1 | `WaermequelleClass.QuelleAnzeige`, `Warnkriterien.cs` |

## 3. Verifikation — A/B auf identischer Quellkopie

Feste Quellkopie der produktiven DB (27.08. 18:33, kopiert 20:13, nur gelesen), **Vorher** =
S2-Binärkopie auf Stand-50-Kopie, **Nachher** = A1-Build (umgeleiteter Ausgabepfad — die
Anwendung des Anwenders lief) auf der 51-migrierten Schwesterkopie; je 13 Projekte im
`projekt`-Modus, 0 Fehlläufe.

| Ergebnis | Projekte | Zuordnung |
|---|---|---|
| **byte-/MD5-gleich, alle Dateien** | 1017, 1018, 1021, 1024, 1030, 1039, 1040, 1041, 1042 | rechneten bereits Speicherstufe — Abriss nachweislich verhaltensneutral (inkl. Pendelspeicher 1018, Quellspeicher 1021, Parallelverbund 1040/1041) |
| toleranz-PASS (Rundungen, 13 Dateien) | 1007 | Altpfad → Speicherstufe ohne verbaute Speicher |
| **gewollt geändert** | 1008 (55 802), 1011 (1 617), 1023 (52 768 Werte) | 1008: Projekt-Puffer erstmals real bewirtschaftet; 1011: **Prozess-Herauslösung real** (`wp_warmwasserbedarf` ohne Prozessanteil, Konzept 11.2); 1023: Deckungsumverteilung mit Heizstab im WW-Kanal |

**Invarianten:** `Waermebedarf_Gesamt`/`Strombedarf_Gesamt` in allen 13 Projekten exakt
unverändert; **Energieprobe 0 Meldungen**. **Selbstvergleich:** 104/104 CSV der vier
geänderten Projekte im Zweitlauf byte-gleich (die neun anderen sind durch die zwei
unabhängigen Läufe doppelt belegt). Migrationswirkprobe (zweifach, inkl. hartem
Marker-Rücksetzen): idempotent; 1 Übernahme (1007007 ← 50/30), 33 dokumentierte
Rückfall-Fälle (invertierte Paare bzw. Bezeichner-Varianten „Liter"/„Ltr" — exakt das
bisherige Engine-Verhalten, kein Ergebnisverlust).

**Neue Basis: [`Referenzlaeufe/2026-08-27_A1`](../../../Referenzlaeufe/2026-08-27_A1/lauf_protokoll.md)**
— dreizehn Projekte, erstmals mit den vier Konzept-11.1-Projekten; `2026-08-27_K1` rückt zu
den früheren Ständen.

## 4. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| A1-O1 | Produktiv-Migration lief vorzeitig: Während der Agentenarbeit startete die Anwendung (20:40) auf einem Zwischen-Build und migrierte die produktive DB regulär auf Stand 51 (idempotent, verlustfrei; Übernahme identisch zur Wirkprobe). Der Anwender arbeitete anschließend auf dem Zwischenstand — Neustart auf dem finalen Build erforderlich | Anwender |
| A1-O2 | `KomponentenUebernahmeCtrl` kopiert `Z_AnlageSenke` nicht — übernommene Fremdkomponenten starten mit der Rang-1-Vorbelegung statt der Quell-Senkenliste (Engine rechnete sie schon vor A1 so) | Einzelfix |
| A1-O3 | Aufruferfrei geworden, bewusst stehen gelassen (Schnitt zusammen mit Spalte/Tabelle): `KonfigurationCtrl.KaskadeZweikanalig*`/`KaskadeNotwendig`, `AnlagePufferVerbundCtrl.ProjektHatVerbund`, `Z_ProjektPufferSpCtrl` (ganze Klasse), `SimulationControl.KaskadeZweikanalig`-Leser in `Form_Simulation_Detail`, `DbWerte.ERZEUGER_GESAMTSYSTEM` | Paket L |
| A1-O4 | `SenkenPufferDerAnlagen` liest zusätzlich zur Senkenliste weiter `WS_ID_Puffer/2` — Altdaten-Reste (WS-Wert bei Ziel `Heizkreis`) stünden sonst nicht mehr in Registry/Ergebnis; fällt mit dem Spalten-Abriss | Paket L |
| A1-O5 | `Waermekanaele` (SimulationKanaele) bleibt: kein Altpfad-Bestandteil, ihr Debug-`Selbsttest` Punkt 8 ist die einzige Invariantenprüfung von `Senkenzuordnung`/`Senkenliste`/`BedientKanal` | dokumentiert, bleibt |
| A1-O6 | Verbund-Kandidatenliste weiter verwendungsgefiltert (`KonfliktPruefen`, zwei Plätze) — S2-O5 unverändert | P1/P2 |
| A1-O7 | 1042 (Booster) ist mit **unkonfigurierter** Quelle eingefroren (`QUELLE_FEHLT` meldet); nach Anwender-Konfiguration und Paket B1 (Quellkopplung) wird die Basis erneuert | B1 |

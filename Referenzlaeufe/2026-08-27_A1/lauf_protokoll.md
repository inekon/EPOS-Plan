# Basis 2026-08-27_A1 — Laufprotokoll

Eingefroren am 27.08.2026 nach Paket **A1** (Altpfad-Rückbau, Schritt 51). **Dreizehn
Projekte, 329 CSV**: die neun Bestandsprojekte (1007, 1008, 1011, 1017, 1018, 1021, 1023,
1024, 1030) plus erstmals die **vier Anwenderprojekte aus Konzept 11.1** — 1039 (Mehrgebäude),
1040 (zwei Puffer je Kanal, Parallelverbund), 1041 (Prozesswärme mit eigenem Puffer),
1042 (Booster-Kette mit Kombi-Speicher; Wärmequelle der Booster-WP zum Zeitpunkt des
Einfrierens noch unkonfiguriert — Chip „Quelle wählen!" und Warnkriterium `QUELLE_FEHLT`
melden das).

**Codestand:** Paket A1 auf `Pufferspeicher` (Commit siehe A1-Protokoll), gebaut mit
umgeleitetem Ausgabepfad (die Anwendung des Anwenders lief während der Verifikation).
**Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **27.08.2026 18:33** (nach dem
Sync-Commit dae01e0), kopiert um 20:13 als feste Quellkopie, **nur gelesen**; Migration
**50 → 51** (Temperaturübernahme: 1 Puffer — 1007007 ← 50/30 aus Zuordnung 10060; 33 bleiben
dokumentiert auf Rückfall-ΔT; 16 Einstellungssätze auf WAHR) lief ausschließlich auf der
Arbeitskopie. Gerechnet im Modus `projekt` je Projekt (Muster der Paket-7-Basis).

**A/B gegen den unmittelbaren Vorgängerstand (S2, identische Quellkopie Stand 50):**

| Projekte | Ergebnis | Zuordnung |
|---|---|---|
| 1017, 1018, 1021, 1024, 1030, 1039, 1040, 1041, 1042 | **byte-/MD5-gleich (alle Dateien)** | rechneten bereits über die Speicherstufe — der Abriss von Weiche, Registry-Block 1 und Alt-Zuordnung ist dort nachweislich verhaltensneutral, einschließlich 1018 (BHKW-Pendelspeicher) und 1021 (Quellspeicher) |
| 1007 | toleranz-PASS, 13 Dateien mit Kleinstabweichungen | Altpfad → Speicherstufe ohne verbaute Speicher: reine Rundungs-/Reihenfolgeeffekte innerhalb 1e-4 |
| 1008 | **gewollt geändert** (55 802 Werte) | Altpfad → Speicherstufe: die Projekt-Puffer werden erstmals real bewirtschaftet (`puffer_entladung` statt 0), Deckung umverteilt |
| 1011 | **gewollt geändert** (1 617 Werte) | **Prozess-Herauslösung wird real** (Konzept 11.2): der Prozessanteil verlässt den Warmwasser-Topf der WP (`wp_warmwasserbedarf` sinkt je Stunde um den Prozessanteil) |
| 1023 | **gewollt geändert** (52 768 Werte) | Altpfad → Speicherstufe: Deckungsumverteilung, Heizstab springt in WW-Knappheitsstunden an |

**Invarianten:** `Waermebedarf_Gesamt` und `Strombedarf_Gesamt` in **allen 13 Projekten exakt
unverändert** (die Bedarfsseite ist von A1 unberührt); **Energieprobe: 0 Meldungen** über
13 × 8760 Stunden. **Selbstvergleich:** die neun byte-gleichen Projekte sind durch die zwei
unabhängigen Läufe (Vorher-/Nachher-Prozesse) doppelt belegt; die vier geänderten wurden auf
derselben Kopie erneut gerechnet — **104/104 CSV byte-/MD5-gleich**.

**Was diese Basis erstmals absichert:** zwei Puffer je Kanal mit arbeitendem Parallelverbund
(1040/1041), Prozesswärme mit eigenem Puffer über die Senkenliste (1041, 1011), die
Booster-Konstellation als Datenlage (1042 — die Quellkopplung selbst kommt mit Paket B1 und
wird die Basis dort erneuern). **Weiterhin nicht abgesichert:** Wirtschaftlichkeit (kein
`WirtschaftlichkeitCtrl`-Aufruf), Kessel an Quellpuffer mit Wert ≠ 0.

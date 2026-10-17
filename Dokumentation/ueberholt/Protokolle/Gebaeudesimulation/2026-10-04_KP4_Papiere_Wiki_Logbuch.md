# Protokoll KP4 — Papiere, Wiki-Quellen und Logbuch-Entwurf der Konditionierung (04.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle KP4, Zweige `kp4-papiere` und `kp4-wiki` (Worktrees), Merges `31c7c0b` und `fe87e44`, Nachzug `f06ae69`. Statuszeile #707.
**Entscheid:** keiner neu. Kein Code, keine Testdatenbank, Basis R34 unverändert.

## 1 Auftrag

Die Papiere und die Wiki-Quellen auf den gebauten Stand der Konditionierung bringen (Kalender, Aufheizoptimierung, Nachtauskühlung, Erdreich nach DIN EN ISO 13370 mit Vorgabe) und den Logbuch-Entwurf schreiben.

## 2 Vorgehen

Zwei Agenten parallel in getrennten Dateien: Opus (Papiere, 78 Aufrufe) und Sonnet (Wiki, 62 Aufrufe). Danach zwei konfliktfreie Merges und ein Nachzug der Teilkonzept-Vermerke auf den Wiki-Stand.

## 3 Ergebnis Papiere (`c504f08`, `b98de28`, `dd81b2a`, `dad75ff`, `c90da09`)

- **Rechenschritte:** neu 7.5 Schritt K (Kalenderreihen, Aufheizrampe mit Bemessung (a)/(b), Stufenzahl, Rampe, Zähler W1–W5) und 7.6 Schritt L (bedingte Nachtauskühlung); Kapitel 0 Knoten SK; 1.1 Gebäudezeilen und Tabelle `Aufheiz_*`; 1.3 λ_Erd, w, R_se; E6, E8; 8.2 Kennzahltabelle; Kapitel 11 Zeilen 2, 22, 23.
- **Leitkonzept:** 4.4, 4.5, Kapitel 15 und neue N1.69 (58 Zeilen: Festlegungen 1–43 des Entwurfs KP3 wie gebaut, 29/30/40 nicht gebaut, Nr. 44–52 Wellen R1–R5/D1/D2/O1, 53–54 RP1/RP2a, 55–58 EV1).
- **Softwarearchitektur:** 1.3 Bausteintabelle (zehn Bausteine), 2.2, Kapitel 5.
- **Teilkonzept:** Kopf, 4.4 (Reserve 1–100 %, leer 20 %), 4.6, 5.4 (Schritte 174/180), Abschnitt 8; Entwurf KP3 mit Kopfvermerk.

## 4 Ergebnis Wiki (acht Commits `065327e` bis `ee7fc6c`)

Sieben Seiten unter `Projekte/Wiki/`:

- Gebäude: Reiter „Konditionierung“ mit Ankern `konditionierung`, `matrix`, `saison`, `heizperiode`, `kalender`, `vorlagen`, `zonenmatrix`; alte Anker bleiben.
- Mehrzonenmodell: Zonen erben Matrixzellen und Kalender.
- Gebäudemodell VDI 6007: Heizperiode, Nachtauskühlung, Aufheizoptimierung, Bodenplatte nach ISO 13370 mit Vorgabe.
- Kühlung: Kühlspalte, Kühlperiode, Kühlkalender.
- Simulation: Punkt `aufheizoptimierung` (Bemessung, ΔT_K, Reserve, Art, Herleitungszeile).
- Simulationsergebnisse: Abschnitt `gebaeudekennzahlen`, Hinweise W1–W5.
- Gebäudeimport: Matrixfelder, Kalender anlegen oder Vorlage.

Teilkonzept 10.4: alle sieben Zeilen „nachgezogen“. 10.5 Logbuch-Entwurf mit Sätzen zu #691, #692, #693 (schon unter 1.2.0.6) und #705; #690, #695–#697 ohne Satz (intern). Version offen.

## 5 Abweichungen Code gegen Papier (nach dem Code gefasst)

- Φ_RH = ρ·Φ_stat nur bei Bemessung (a) (N1.69 Nr. 41, Rechenschritte 8.2).
- Kusuda bleibt Randtemperatur; ISO 13370 liefert R_g in Reihe.
- Startwertregel in `Zonenschleife.StartwerteRechnen` (Nr. 13).

## 6 Abnahme

DokumentationLinkWache, WikiProduktdatenWache, RepositoryOrdnungWache 35/35 grün; Konfliktmarker keine; BOM in Markdown keine; Kodierung aller 13 Dateien unverändert. Gegenlese-Muster: 0 neue Treffer (Bestandstreffer „Befund“ als Spaltenname in `Gebäudeimport.wiki`, Zeile 135).

## 7 Offen

- Noch nicht beschrieben (erst mit O1b/O2/O3): Aufschlag h/% und „Aufheizzeit manuell“ im Dialog, Gruppe „Aufheizung“ im Bedarfsdialog, Berichtstafeln.
- Beim Anwender: Logbuch-Version für 10.5, Prüfung der Entwurfssätze #691/#692, Wiki-Upload gebündelt (sieben Seiten).
- Festwert-Verortung in der Wurzel-`CLAUDE.md`: Die Einfrierregel Erdreichdaten nennt `GebaeudeFestwerte` für die Abschnittsobergrenze 8, im Code steht sie als `Zonenmodell2K.INNENPRUEFUNG_ABSCHNITTE`.
- Nächste Welle: G7b (E66).

## 8 Aufwand

Opus 78 Aufrufe, Sonnet 62 Aufrufe.

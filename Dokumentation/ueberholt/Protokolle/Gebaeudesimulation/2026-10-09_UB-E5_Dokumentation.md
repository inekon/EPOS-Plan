# Protokoll UB-E5 — Wiki-Quellen, Logbuch-Entwurf, Konzepte „wie gebaut“ (09.10.2026)

**Sitzung:** Gebäudesimulation, Statuszeile **#854**. Commits E5-b `7ef71ecfa`, `61d52c924`; E5-a `7d3d96936`; Merges `fea975d77` (ub-e5b) und
`c641a2682` (ub-e5a) in den Hauptbaum; fachliche Korrektur der Wiki-Quelle `6125d1711`. **Entscheid:** E109.
Konzepte: [Fachkonzept Übergabegrenze/Bivalenz](../../Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md) und
[Umsetzungskonzept](../../Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md), beide jetzt unter `ueberholt/`.

## 1 Auftrag und Entscheidlage

- **E109** (Anwender, 08.10.2026): Reihenfolge UB-E1 → E2 → E3 → E4 → E5; E5 schließt die Welle mit Wiki-Quellen, Logbuch-Entwurf und der Ablage der
  Konzepte als „wie gebaut“. Kein Code, kein Schemaschritt, keine Rechenwirkung: Die Referenzbasis R46 bleibt.

## 2 Wellen

| Welle | Commits | Ergebnis |
|---|---|---|
| E5-a | `7d3d96936` | Wiki-Quellen unter `Projekte/Wiki/`: Wärmepumpe (+14 Zeilen: Abschnitt „Gerätegrenzen“ mit sechs Feldern, Einbindung, Vorwärmbetrieb, Betriebsbereiche), BHKW (+1: höchster Rücklauf), Gerätekataloge (+2), Simulationsergebnisse (+3/−1: Kachelzeile Betriebsbereiche, CSV-Kopfzeilen, Absatz Bivalenz), Berichtsvorlagen (+2). `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`: ausstehende Uploads mit #851/#852/#853, Logbuch-Abschnitt „Version offen (Vorschlag 1.2.1) — nicht veröffentlicht“ mit fünf Sätzen (Einbindung und Vorwärmbetrieb; Gerätegrenzen im Katalog; Reiter Betriebsbereiche; Bivalenzdiagramm und -tafel im Bericht; Kennzahlen `wp.bivalenz.*` in den Vorlagen) |
| E5-b | `7ef71ecfa`, `61d52c924` | Fachkonzept Abschnitt 10a und Umsetzungskonzept Abschnitt 11 „Umsetzung — wie gebaut“: Etappentafel und Abweichungsliste (17 Zeilen Konzept ↔ gebaut ↔ Grund). Glättungen im Fachkonzept: Abschaltpunkt als Deckel, maßgebend der wärmere Wert aus θ_biv,2 und eingegebenem Abschaltpunkt (UB‑Q4 a); θ_biv,2 = −3,5 °C (gerechnet −3,549 °C, auf 0,1 K gerundet) statt −3,6 °C. Beide Konzepte per `git mv` nach `Dokumentation/ueberholt/`, Indexzeilen in `Dokumentation/LIESMICH.md`, 36 Verweise in 11 Dateien umgestellt |
| Merge | `fea975d77`, `c641a2682` | ohne Konflikt |
| Prüfung | `6125d1711` | fachliche Prüfung der Wiki-Sätze am Code, fünf Sätze berichtigt (Abschnitt 4) |

## 3 Dateien

- Wiki-Quellen: `Projekte/Wiki/Programm Dokumentation - {Wärmepumpe,BHKW,Gerätekataloge,Simulationsergebnisse,Berichtsvorlagen}.wiki`.
- Logbuch und Uploads: `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`.
- Konzepte: `Dokumentation/ueberholt/Konzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md`, `Dokumentation/ueberholt/Umsetzungskonzept_Uebergabegrenze_Bivalenz_EPOS-Plan.md`; Index und Verweise (u. a. Statusdateien, Übergabepapier).

## 4 Fachliche Prüfung der Wiki-Sätze (Wärmepumpe)

| Prüfpunkt | Code | Befund |
|---|---|---|
| Einleitung „Gerätegrenzen“ | `Geraetegrenzen.Bilden`, `Bivalenzvorgaben` | berichtigt: leere Felder nehmen die Vorgabe der Kältemittelklasse bzw. die allgemeine Vorgabe — die Grenze entfällt nicht |
| (a) Bezugsrücklauf, Abwertung je Kelvin | `Ruecklaufgrenze.Faktor`, `Geraetegrenzen.Bilden` | leer = Vorgabe des Kältemittels (nur R744: 30 °C, 2,5 %/K), 0 = aus: bestätigt. Berichtigt: der Faktor trifft Wärmeleistung **und** Leistungszahl (Strom unverändert) und wirkt vom Bezugsrücklauf bis zum größten Rücklauf |
| (b) Vorwärmbetrieb | `Bivalenzmodul.Wirksam`, `SimulationWaermepumpe.BivalenzAufbauen`, `Betriebsbereich`, `Bivalenzpruefung` | Einbindung nötig, Alternativbetrieb mit Hinweiszeile (`WPA_HINWEIS_VORWAERMBETRIEB_ALTERNATIV`): bestätigt. Berichtigt: zweiter Erzeuger ist ein Kessel **hinter** der Wärmepumpe in der Kaskade **oder** ein Heizstab |
| (c) Einbindung | `Hydraulikgrenze`, `Uebergabegrenze`, `SIMENG_WP_PUFFER_ENTLADESEITE` | Werte direkt (ohne Puffer), Puffer, Weiche bestätigt. Berichtigt: die Einbindung wirkt nur mit aktiver Anlagenkopplung; beim Puffer rechnet die Übergabe die Ladeseite mit dem Rücklauf aus der untersten Pufferzone, die Entladeseite wird als Näherung nicht gerechnet |

Gegenlesen der geänderten Quellen auf Änderungs- und Entscheidvermerke: 0 Treffer in neuen Zeilen.

## 5 Offen

- Wiki-Upload der fünf Seiten und Veröffentlichung der Logbuch-Sätze; Versionsnummer beim Anwender (Vorschlag 1.2.1).
- Der Oberflächentext `WPA_HINWEIS_EINBINDUNG_PUFFER` („rechnet mit der Entladeseite als Näherung“) widerspricht dem Code wie zuvor der Wiki-Satz; nicht Teil von E5.

## 6 Wachen

Bau EPOS.Kern.Tests 0 Fehler; Wachen DokumentationLinkWache, RepositoryOrdnungWache, WikiProduktdaten, Bivalenz 78/78 grün; keine BOM, keine Konfliktmarker; volles Gate nach dem Merge durch die Orchestrierung.

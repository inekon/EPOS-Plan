# Konzept Wirtschaftlichkeit — Protokoll der Entscheidwege (Schnitt A13, 22.09.2026)

Statuszeile #435 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Entscheid A13 des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 4). Schwesterpapiere: das Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
— der gültige Stand — und das
[`Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
— die Entscheide. Zweig `a13` von `f96b59db`, Opus 5.5 im Worktree `.claude/worktrees/a13`.

> **Was dieses Protokoll ist.** Die Geschichte des Wirtschaftlichkeitskonzepts, so wie sie bis zum
> Schnitt im Konzept selbst stand: Entstehung und Quellen, die ausführlichen Zeilen jeder Etappe, die
> erledigten offenen Punkte mit ihren Gründen, die Messungen und Befunde vor der Umsetzung und die
> Wege, auf denen die Entscheide gefallen sind. Es ist **nie Regelquelle** — was gilt, steht im
> Konzept, der Entscheid im Register. Jeder eingerückte oder abgesetzte Block unten ist **Wortlaut des
> Konzepts vor dem Schnitt** (Stand `f96b59db`, 2 818 Zeilen) mit seiner alten Fundstelle; geändert
> sind allein relative Verweise, die vom Ort dieses Protokolls aus auflösen müssen (§ 0.6). Die
> Etappen stehen in der Reihenfolge ihrer Statusnummern, je mit ihrem Wellenprotokoll. **§ 8** schreibt
> das Protokoll nach dem Schnitt fort: Was ab der Statuszeile #436 aus dem gültigen Stand weicht, steht
> dort mit dem Wortlaut des Konzepts **vor der jeweiligen Statuszeile** (#436, #437, #439, #440, #446, #452, #454, #455, #460, #461, #462, #463, #474, #477, #478, #479, #484, #492, #498), jede
> Berichtigung mit einer Zeile.

---

## 0 Der Schnitt A13

### 0.1 Auftrag, Entscheid und Grundsatz

Der Entscheid, im Wortlaut des Analysepapiers (§ 4; entschieden 20.09.2026 nach Empfehlung):

| # | Frage | Empfehlung |
|---|---|---|
| **A13\*** | Konzept in drei Papiere schneiden (gültiger Stand, Entscheidungsregister, Protokoll der Entscheidwege)? | **Ja**, vor der ersten Codeetappe; Papierpflege ohne Entscheid zuerst (§ 5 E0) |

Den Zeitpunkt hat der Anwender am 22.09.2026 nach Empfehlung bestimmt: **nach E5, vor E6**, wenn E4
und E5 die § 2.6 und § 2.13 umgebaut haben. E5 ist mit #434 gebaut; der Schnitt ist die
Statuszeile #435. Der Auftrag an die Welle: das konsolidierte Konzept in drei Papiere trennen — gültiger Stand,
Entscheidungsregister, Protokoll der Entscheidwege —, ohne eine fachliche Aussage zu ändern; nur
Papiere, kein Code.

**Der Grundsatz: verschoben, nicht umgeschrieben.** Jede fachliche Aussage, jede Zahl und jeder
Entscheid bleibt im Wortlaut erhalten; nur der Ort ändert sich. Wo ein Abschnitt im Konzept bleibt,
aber Entscheid- oder Verlaufsprosa trug, steht dort jetzt die geltende Regel als Aussage, und die
Herkunft — wer, wann, warum, was vorher war — steht im Register oder hier, mit einem Verweis in beide
Richtungen. Nummern und Kürzel bleiben, wo sie waren: die §-Nummern, Nr. 1–32 des § 6.3, A1–A20,
Q1–Q25, V‑1…V‑4, V‑G1…V‑G12, K1–K11, U1–U45, E0–E12 und die Fragen der Etappen. An einer alten
Stelle steht, wo nötig, ein Einzeiler „→ Register R‑…" oder „→ Protokoll § …".

### 0.2 Was wohin gewandert ist

| Quelle (Konzept vor dem Schnitt) | Ziel |
|---|---|
| Titel „— konsolidiert" | Konzept: „— gültiger Stand (konsolidiert)"; alter Titel in § 0.3 |
| Geltung, Z. 22–29 (Konsolidierungsprosa) | § 2.2; im Konzept ein neuer Geltungsblock mit den beiden Schwesterpapieren |
| Herkunft, Z. 42–45 | § 2.1 |
| Quelltabelle der drei Ursprungsdokumente, Z. 47–54 | § 2.3 |
| Begleitendes Artifact, Z. 62–67 (die fünf abgelösten) | § 2.4; der gültige Artifact-Link bleibt im Konzept |
| § 2.2, Z. 218–219 (Vermerk „Entscheid Q2 offen") | § 5.1; Konzept: Verweis auf Register R‑Q |
| § 2.5, Z. 353–355 (Ist-Zustand vor der Umsetzung) | § 3.3 |
| § 2.6, Z. 422–423 (Anlass der Nullzeile) | § 3.3 |
| § 2.7, Z. 522–523 (Vermerk „Entscheid Q9 offen") | § 5.1; Konzept: Verweis auf Register R‑Q |
| § 2.8, Z. 527–530 (Herkunft Entwurf B) | § 2.6; Register R‑EZ (EZ‑3) |
| § 2.9, Z. 603–619 und 642–644 (Anforderung, Ist-Zustand, Begründung, Einordnung) | § 4.1; Register R‑D (D‑3) |
| § 2.11.4, Z. 764–769 (Entscheid A5) | § 5.5; Register R‑A |
| § 2.11.4, Z. 771–776 (Entscheidungsfragen V‑1…V‑4) | Register R‑V; die Tafel im Wortlaut in § 7.4 |
| § 2.11.6, Z. 822–841 (Entscheid zu V‑G10, Messung am Generator) | § 3.11; Register R‑V |
| § 2.11.7, Z. 885–890 (Messung, die dem Hinweistext zugrunde liegt) | § 3.11 (Z. 883–884 bleiben im Konzept) |
| § 2.13, Z. 1003–1004 und 1144–1149 (Durchsicht, weitere Festlegungen) | § 3.7 |
| § 2.13, Z. 1044–1048 (erledigt mit #357) | § 3.10 |
| § 2.13, Z. 1069–1077 (Entscheid A1) | § 5.5; Register R‑A |
| § 2.15, Z. 1191–1210, 1287–1312, 1326–1328 | § 4.1 (der Mockup-Verweis bleibt im Konzept) |
| § 2.15, Z. 1314–1324 (VG‑Q1…VG‑Q7) | Register R‑VG; die Tafel im Wortlaut in § 7.4 |
| § 2.16, Z. 1348–1401, 1454–1486, 1500–1503 | § 4.2 (die Mockup-Verweise bleiben im Konzept) |
| § 2.16, Z. 1488–1498 (VV‑Q1…VV‑Q7) | Register R‑VV; die Tafel im Wortlaut in § 7.4 |
| § 3.6, Z. 1973–1975 (Rechtskette nachgetragen) | § 2.5 |
| § 5, Z. 2271–2303 und 2323–2328 (Entscheidtafeln K1–K11, D‑1…D‑3, E‑1, ET‑D‑1…3, UR‑1, E1, U‑1) | Register R‑K und R‑D; die Tafeln im Wortlaut in § 7.4; im Konzept ein Verweisabsatz, die Regeln und rechtlichen Unsicherheiten bleiben |
| § 6.1, Z. 2367–2401 (ausführliche Etappenzeilen) | § 1 bis § 6, je Etappe; Konzept: Kurztafel mit A13 (#435) |
| § 6.3, erledigte Punkte 1–9, 9a–9g, 9i, 9k, 9l, 12, 14, 17, R4, 25–28, 31, 20, 21 | je Etappe (§ 0.5); Konzept: Einzeiler |
| § 6.3 Nr. 29, 30, 32 (Entscheidvermerke) und Z. 2609–2610 | Register R‑NR; § 5.1 und § 5.4 im Wortlaut; Konzept: die Regel |
| § 6.4, Z. 2679–2682 (gestrichene Fallstricke) | § 7.2 |
| § 6.5, Z. 2686–2706 (Auflösung der Doppelung des KWK-Zuschlags) | § 3.9; Konzept: der Stand in vier Zeilen |
| § 7, Z. 2726–2741 (Reihenfolge, Einordnung) und 2743–2745 (Wiederaufnahme) | § 7.3 und § 6.1; Konzept: Verweis auf Analysepapier § 5, B8 und B9 |
| § 7, Z. 2749–2764 (Entscheide vor der nächsten Codeetappe) | § 5.5; Register R‑A |
| Überschriften mit Datums- oder Standvermerk | § 0.3 |
| Herkunftsvermerke im Fließtext | § 0.4; Register |

### 0.3 Überschriften vor dem Schnitt

Die Nummern sind geblieben; weggefallen sind Datum, Auftragsart und „— umgesetzt".

```text
# Konzept: Wirtschaftlichkeit EPOS-Plan — konsolidiert
### Emissionsanzeige der Energieträgertabelle (Auftrag 30.08.2026) — umgesetzt
## 2.6 Eigene Rubrik „Erlöse und Vorteile" (Auftrag 30.08.2026) — umgesetzt
## 2.8 Betriebskosten-Raster der Kostenverwaltung — Entwurf B (übernommen 31.08.2026)
## 2.9 Wählbares Vergleichsprojekt — die Referenz der Differenzrechnung (Anforderung 31.08.2026) — umgesetzt
## 2.10 Integrationsort der ValERI-Darstellung (Anwendervorgabe 31.08.2026)
## 2.11 ValERI (DIN EN 17463) — Integration und Darstellung (Auftrag 31.08.2026)
### 2.11.5 Vollständige Szenarioabdeckung (Entscheidung 31.08.2026)
### 2.11.6 Formelbericht — Stufenplan und Grenze (Entscheid 18.09.2026 zu V-G10)
### 2.11.7 Hinweistext bis zur vollständigen Szenarioabdeckung (Entscheid 18.09.2026 zu V-4)
## 2.12 Kategorien-Mockups mit Rechenweg (Auftrag 02.09.2026; Mockup vom Anwender abgenommen 22.09.2026)
## 2.13 Ergebnisansicht (Anwenderdurchsicht 18.09.2026)
## 2.14 Erfassungsgruppen auf der Kostenseite (Anwenderentscheid K-WZ-1)
## 2.15 Vergleichssicht der Ergebnisansicht — alle Varianten gegen die Referenz oder zwei Stände (Anforderung 18.09.2026) — umgesetzt
## 2.16 Vergütung je Variante — eigene Werte oder vom Stammprojekt übernommen (Anforderung 18.09.2026) — umgesetzt
# 5 Offene Entscheidungen K1–K11
```

### 0.4 Herkunftsvermerke, die aus dem Fließtext gewandert sind

Je Zeile oben der Wortlaut vor dem Schnitt, darunter die Fassung im Konzept. Der Entscheid steht im
Register in der genannten Familie.

**§ 2.2, Gruppe 2 (Z. 205):**

```text
**Der Vorschlag steht am Feld, nicht als Sammelknopf** (Anwenderwunsch 17.09.2026): Unter jedem der
**Der Vorschlag steht am Feld, nicht als Sammelknopf** (→ Register R‑BK, BK-E-1): Unter jedem der
```

**§ 2.2, Gruppe 4 (Z. 239):**

```text
(K3 erledigt mit B6, Statuszeile #328). Beide Sprünge speichern nur, wenn der Arbeitsstand vom
(K3, → Register R‑K). Beide Sprünge speichern nur, wenn der Arbeitsstand vom
```

**§ 2.5, Emissionsanzeige (Z. 378):**

```text
**Der Tooltip trägt die Herleitung — Entscheidung E-1 (30.08.2026).** Im Modus `CO2E` kann derselbe
**Der Tooltip trägt die Herleitung** (E-1, → Register R‑D). Im Modus `CO2E` kann derselbe
```

**§ 2.7, Fußleiste der Wirtschaftlichkeitsseite (Z. 517):**

```text
**Entschieden 18.09.2026 (K8, nach Empfehlung):** kein weiterer Knopf, sondern ein Umschalter
**Entscheid K8 (→ Register R‑K):** kein weiterer Knopf, sondern ein Umschalter
```

**§ 2.11.5 (Z. 780):**

```text
**Anwenderentscheid:** Alle Parameter — Investitionskosten, Energiekosten, Betriebskosten,
**Umfang (V-G5, → Register R‑V):** Alle Parameter — Investitionskosten, Energiekosten, Betriebskosten,
```

**§ 2.11.7 (Z. 908):**

```text
*Entscheid A14 (Analyse vom 19.09.2026, entschieden 20.09.2026 nach Empfehlung):* Die Ressource
*Entscheid A14 (→ Register R‑A):* Die Ressource
```

**§ 2.12 (Z. 923 und 929):**

```text
— **die Repo-Datei führt**; das Artifact ist am 22.09.2026 aus ihr neu veröffentlicht (Q22, Systemschrift).
— **die Repo-Datei führt** (Q22, → Register R‑Q).
**Die Dialogform der Komponentenkosten ist abgenommen** (Anwender, 02.09.2026): Kopfband,
**Die Dialogform der Komponentenkosten ist abgenommen** (→ Register R‑EZ, EZ‑4): Kopfband,
```

**§ 3.4 (Z. 1690 und 1728–1729):**

```text
der **erfasste Betrag** (Anwenderentscheid I-2, 30.08.2026). Eine ermittelte Menge 0 rechnet weiter
der **erfasste Betrag** (Entscheid I-2, → Register R‑EZ, EZ‑1). Eine ermittelte Menge 0 rechnet weiter
**Der Strompreis einer Anlage ist der ihres eigenen Trägers** (Anwenderentscheid
19.09.2026). Trägt eine Anlage, die selbst Strom bezieht — Wärmepumpe, Heizstab,
**Der Strompreis einer Anlage ist der ihres eigenen Trägers** (→ Register R‑EZ, EZ‑6).
Trägt eine Anlage, die selbst Strom bezieht — Wärmepumpe, Heizstab,
```

**§ 3.6, Befund K‑1 (Z. 1997):**

```text
> **Entschieden 18.09.2026, nach Empfehlung: Kennzeichen und Stromkennzahl je Anlage aufnehmen,
> **Entscheid K-1 (→ Register R‑EZ, EZ‑5): Kennzeichen und Stromkennzahl je Anlage aufnehmen,
```

**§ 4, Investitionsseite, I‑2 (Z. 2229):**

```text
| ✔ **I-2** | Abgeleitete Bemessung ohne Satz ⇒ 0 €, nicht der erfasste Betrag. **Entschieden 30.08.2026 (Anwender):** Ist die Ableitung nicht rechenbar, gilt der erfasste Betrag; eine ermittelte Menge 0 rechnet zu 0. |
| ✔ **I-2** | Abgeleitete Bemessung ohne Satz ⇒ 0 €, nicht der erfasste Betrag. **Entscheid (→ Register R‑EZ, EZ‑1):** Ist die Ableitung nicht rechenbar, gilt der erfasste Betrag; eine ermittelte Menge 0 rechnet zu 0. |
```

**Anhang, Etappenreihe E0–E12 (Z. 2802 und 2808)** — nachgezogen, weil der Schnitt mit dieser
Statuszeile ausgeführt ist:

```text
| **E0** Papierpflege | Kopf, § 6.1/§ 6.3/§ 6.4/§ 6.5/§ 7 und die Nebenkonzepte; **A13-Schnitt nicht ausgeführt** | **#379**; Nachpflege auf den Stand vom 22.09.2026 mit **E0c** |
| **E0** Papierpflege | Kopf, § 6.1/§ 6.3/§ 6.4/§ 6.5/§ 7 und die Nebenkonzepte; der **A13-Schnitt** folgte mit **#435** | **#379**; Nachpflege auf den Stand vom 22.09.2026 mit **E0c** |
| **E6** … **E12** | Verlauf · rechenwirksame Lücken (B8, dazu Nr. 32) · V-C/V-D · V-E · ND-S3 · Wiki (E11 entfällt) | offen — **nächste Etappe: A13-Schnitt, dann E6** |
| **E6** … **E12** | Verlauf · rechenwirksame Lücken (B8, dazu Nr. 32) · V-C/V-D · V-E · ND-S3 · Wiki (E11 entfällt) | offen — **nächste Etappe: E6** |
```

**§ 6.3, Einleitung (Z. 2438)** — ergänzt um den Satz zu den Einzeilern:

```text
nicht neu nummeriert, damit Verweise aus Protokollen und Statuszeilen weiter treffen.*
nicht neu nummeriert, damit Verweise aus Protokollen und Statuszeilen weiter treffen. Erledigte Punkte stehen hier als Einzeiler; ihr Wortlaut mit dem Erledigt-Grund steht im Protokoll der Entscheidwege (Fundstellen dort in § 0.5).*
```

Die übrigen Stellen stehen hier als ganze Blöcke im Wortlaut: § 6.3 Nr. 29 (Z. 2596–2605) in § 5.4,
Nr. 30 und 32 (Z. 2612–2619, 2630–2643) in § 5.1, die Zeilen B8 und B9 der Tafel des § 7
(Z. 2731–2732) in § 7.3, die Nullzeile des § 2.6 (Z. 422–423) in § 3.3, der Satz zur Durchsicht des
§ 2.13 (Z. 1003–1004) in § 3.7, die Messung des § 2.11.7 (Z. 885–890) in § 3.11 und die Rechtskette
des § 3.6 (Z. 1973–1975) in § 2.5.

### 0.5 Wo die erledigten Punkte des § 6.3 stehen

| Nr. | Stand | hier |
|---|---|---|
| 1, 2, 3 | erledigt mit B5 bzw. U31 (#347) und #364 | § 3.1 |
| 4–9 | erledigt mit B6 bzw. schon vorher | § 3.2 |
| 9a–9d | erledigt mit E4, B7P, U17 (#346) | § 3.3 |
| 9e–9g, 9l, 9m | erledigt mit BK1a, U17 (#346), BK1b; 9m ist die abgenommene Ausnahme und steht weiter im Konzept | § 3.4 |
| 9h–9k | 9i und 9k erledigt; 9h erledigt mit E10 (#463), der Wortlaut vor #463 und der Grund in § 8.23, der Halbsatz aus A8 zur Speichervariante mit E13 (#474), § 8.25 — vorher zum Teil (#357, #434, dazu die Entkopplung von Ersatz und Restwert mit E7c2 (#446), der Wortlaut vor #446 und der Grund in § 8.9; mit E7c3 (#452) gemessen, der Wortlaut vor #452 in § 8.11); 9j erledigt mit E6 (#436), der Grund in § 8.1 | § 3.7, § 8.1, § 8.9, § 8.11, § 8.23 |
| 21 | erledigt mit #333 und E1 (#380); offen allein die Betriebskosten von 1030 | § 3.6 |
| 30–32 | 31 erledigt mit E5 (#434); 32 erledigt und 30 zum Teil erledigt mit E7a (#437), der Wortlaut vor #437 und der Grund in § 8.3; 30 ganz erledigt mit E7c1 (#440), der Wortlaut vor #440 und der Grund in § 8.7 | § 5.1, § 8.3, § 8.7 |
| R4 | erledigt mit E1 (#380) | § 5.2 |
| 25–29 | 25–28 erledigt mit E2 (#405); 29 erledigt mit E7a (#437), der Wortlaut vor #437 und der Grund in § 8.3 | § 5.4, § 8.3 |
| 20 | entfällt (22.09.2026) | § 6.4 |
| 12, 14, 17 | erledigt bzw. überholt; 14 ganz erledigt mit E18 (#492) — die Wache Konstante gegen Katalog —, der Wortlaut vor #492 und der Grund in § 8.35 | § 7.1, § 8.35 |
| 16 | erledigt mit E18 (#492), der Wortlaut vor #492 und der Grund in § 8.35; Nr. 18 mit E18 nachgemessen und wieder offen, ebenda | § 8.35 |
| 15, 33 | 15 überholt durch die Schalentrennung, mit Wache (E19, #498); 33 erledigt mit E19 (#498) — der Wortlaut vor #498 und der Grund in § 8.37 | § 8.37 |
| 11, 13 | geschlossen nach den Anwenderentscheiden vom 25.09.2026 (11 nicht nachziehen, 13 nur Volumen), der Wortlaut vor #498 und der Grund in § 8.39; dort auch die Entscheide zu Nr. 10, 18 und 19, die offen bzw. dokumentiert bleiben | § 8.39 |
| Q11 (ohne Nummer) | erledigt mit E7b (#439); stand vorher nicht in § 6.3, der Grund in § 8.5 | § 8.5 |

### 0.6 Umgebogene Verweise

Die relativen Verweise der verschobenen Blöcke zeigen vom Ordner `aktuell/Wirtschaftlichkeit_Kosten/`
aus; hier liegen sie unter `ueberholt/Protokolle/Reporting/`. Umgeschrieben sind nur die Pfade, nie
der Text: `../../ueberholt/Protokolle/Reporting/X` → `X`, `../../ueberholt/X` → `../../X`,
`../X` → `../../../aktuell/X`, `Rechenweg/X` und `2026-09-19_Analyse_…` →
`../../../aktuell/Wirtschaftlichkeit_Kosten/…`, Mockup-Spannen `../Mockups/X` →
`../../../aktuell/Mockups/X`.

---

## 1 Die Etappen bis zur Konsolidierung

Die Etappen, deren Protokolle die Konsolidierung vom 02.09.2026 als Quellen übernommen hat
(Quelltafel in § 2.3) — § 6.1 im Wortlaut:

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **W4 E1–E8, L12/L13** | Gesetzeskatalog, Vbh elektrisch, VDI-Bemessungsarten, Steuergutschriften, Tarif-Rollenmodell, KWKG je Modul, Bericht; Methodenwechsel 2027, Biomassekonvention | abgenommen 19.08.2026 |
| **K1–K6** | Alttabellen, kWh-Konsistenz, Einheiten-Seeds, Kostenprofil, Komponenten und Zuschuss, KWKG/Steuern einheitenrichtig | abgeschlossen 20.08.2026 |
| **KD1–KD6** | Vorlagentabellen, Komponenten-Kostendialog, Übernahme, Energieträgerverwaltung, Ertrag/Bonus | abgeschlossen 26.08.2026 |
| **P1–P6** | EEG-Satzrechner, Monatsmarktwerte, § 51/§ 51a, PV-Dialog, Kennzahlen | abgeschlossen 26.08.2026 |
| **H1–H4b, H21** | Pflichtpositionen und Endenergie-Bemessung, Bezugsgrößen-Auflöser, Investitionsraster als Drei-Runden-Kaskade, Mengen-Ausweis „frisch vor Konserve" | durchgängig ergebnisneutral |
| **B1** | Zahlenprobe: Doppelzählung § 9 Nr. 3 bestätigt (1.510,84 €/a doppelt) | reine Messung |
| **B2** | Schema 60, Preisbestandteile Brennstoff, Kohärenzprüfung (4 Fälle) | keine — 332/332 CSV byte-gleich |
| **B3a** | Schema 61, Steuerwahl je Anlage, § 54 auf Kesselbrennstoff | keine — Anker exakt |
| **B3b** | `HilfsstromRechner`, Netting nur in der KWKG-Reihe, Eigenstrom-Tatbestand je Anlage | keine bei Anteil NULL; **ja, sobald gepflegt** |
| **B4** | Stromsteuer-Schnellwahl katalogbasiert, Unternehmensart hebt hervor | keine — wertgleich |
| **BK1/BK2** | Trägerzuordnung über `code`, Wizard-Automatik, Emissionsspalten | **ja, gewollt** — CO₂-Bilanz ändert sich |
| **HB1** | Anzeigesortierung, Hydraulikbild liest `Z_AnlageSenke` | keine — 90 Dateien SHA256-gleich |

| Etappe | Wellenprotokolle |
|---|---|
| W4 E1–E8, L12/L13 | [`W4_E1_Gesetzesparameter_Protokoll.md`](W4_E1_Gesetzesparameter_Protokoll.md), [`W4_E2_Vollbenutzungsstunden_Protokoll.md`](W4_E2_Vollbenutzungsstunden_Protokoll.md), [`W4_E3_Kostenarten_Betriebskosten_Protokoll.md`](W4_E3_Kostenarten_Betriebskosten_Protokoll.md), [`W4_E4_Steuergutschriften_Protokoll.md`](W4_E4_Steuergutschriften_Protokoll.md), [`W4_E5_Tarife_Strombezug_Protokoll.md`](W4_E5_Tarife_Strombezug_Protokoll.md), [`W4_E6_Zuschlag_je_Modul_Protokoll.md`](W4_E6_Zuschlag_je_Modul_Protokoll.md), [`W4_E7_Bericht_Mehrjahrestabelle_Protokoll.md`](W4_E7_Bericht_Mehrjahrestabelle_Protokoll.md), [`W4_E8_Abnahme_Protokoll.md`](W4_E8_Abnahme_Protokoll.md), [`W4_L12_L13_Methodenwechsel_Protokoll.md`](W4_L12_L13_Methodenwechsel_Protokoll.md) |
| K1–K6 | [`K1_Aufraeumung_Protokoll.md`](K1_Aufraeumung_Protokoll.md), [`K2_Einheitenpruefung_Protokoll.md`](K2_Einheitenpruefung_Protokoll.md), [`K3_Seeds_Dialog_Protokoll.md`](K3_Seeds_Dialog_Protokoll.md), [`K4_FormKosten_Protokoll.md`](K4_FormKosten_Protokoll.md), [`K5_Komponenten_Zuschuss_Protokoll.md`](K5_Komponenten_Zuschuss_Protokoll.md), [`K6_KWKG_Steuern_Drops_Protokoll.md`](K6_KWKG_Steuern_Drops_Protokoll.md) |
| KD1–KD6 | [`KD1_Kostenvorlagen_Protokoll.md`](KD1_Kostenvorlagen_Protokoll.md), [`KD2_Kostendialog_Protokoll.md`](KD2_Kostendialog_Protokoll.md), [`KD3_Uebernahme_Protokoll.md`](KD3_Uebernahme_Protokoll.md), [`KD4_Energietraeger_Protokoll.md`](KD4_Energietraeger_Protokoll.md), [`KD5_ErtragBonus_Protokoll.md`](KD5_ErtragBonus_Protokoll.md), [`KD6_Protokoll.md`](KD6_Protokoll.md) |
| P1–P6 | [`PV_P1-P5_Protokoll.md`](PV_P1-P5_Protokoll.md), [`PV_P6_Protokoll.md`](PV_P6_Protokoll.md) |
| H1–H4b, H21 | [`H1_Pflichtpositionen_Protokoll.md`](H1_Pflichtpositionen_Protokoll.md), [`H2_Endenergie_Protokoll.md`](H2_Endenergie_Protokoll.md), [`H3_PflichtIntegration_Protokoll.md`](H3_PflichtIntegration_Protokoll.md), [`H4a_Bezugsgroessen_Protokoll.md`](H4a_Bezugsgroessen_Protokoll.md), [`H4b_Investitionsraster_Protokoll.md`](H4b_Investitionsraster_Protokoll.md), [`H21_MengenAusweis_Protokoll.md`](H21_MengenAusweis_Protokoll.md) |
| B1 · B2 · B3a · B3b · B4 | [`B1_Zahlenprobe_Protokoll.md`](B1_Zahlenprobe_Protokoll.md), [`B2_Preisbestandteile_Protokoll.md`](B2_Preisbestandteile_Protokoll.md), [`B3a_SteuerwahlJeAnlage_Protokoll.md`](B3a_SteuerwahlJeAnlage_Protokoll.md), [`B3b_Hilfsstrom_Protokoll.md`](B3b_Hilfsstrom_Protokoll.md), [`B4_Energieintensitaet_Protokoll.md`](B4_Energieintensitaet_Protokoll.md) |
| BK1/BK2 · HB1 | [`BK1_Traegerzuordnung_Protokoll.md`](BK1_Traegerzuordnung_Protokoll.md), [`HB1_Hydraulikbild_Sortierung_Protokoll.md`](HB1_Hydraulikbild_Sortierung_Protokoll.md) |

---

## 2 Entstehung und Konsolidierung (30.08. bis 02.09.2026)

### 2.1 Herkunft und Arbeitsregel

*Konzept, Kopf, Z. 42–45:*

**Herkunft.** Das Papier ist am 30.08.2026 unter der Arbeitsregel des Anwenders „erst das Konzept,
keine Umsetzung" begonnen worden; damals war von den hier beschriebenen Vorhaben nichts
implementiert. Seither sind die in § 6.1 geführten Etappen gelaufen — die Regel beschreibt die
Entstehung des Papiers, nicht seinen heutigen Geltungsumfang.

### 2.2 Der Geltungsblock vor dem Schnitt

*Konzept, Geltung und Abgrenzung, Z. 22–29 (der Rest des Blocks, Z. 31–40, steht weiter im
Konzept):*

> **Dieses Dokument ändert nichts am Code.** Seit der Konsolidierung vom **02.09.2026** ist es die
> **führende Fassung** des Wirtschaftlichkeitskonzepts: Es führt
> `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` (Etappenkonzept, letzter Stand 30.08.2026 mit B4),
> `KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` und `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`
> zusammen. **Bei Widerspruch gilt dieses Dokument.** Was sie an Stoff enthielten, der hier bislang
> fehlte, ist mit der Konsolidierung nachgezogen: § 2.12 (Kategorien-Mockups), § 3.4 (Vorrangregel),
> § 3.11 (Emissionsfaktoren und CO₂-Preispfad), § 5 (rechtliche Unsicherheiten), § 6.5 (doppelte
> Wahrheiten).

### 2.3 Die Quellen der Konsolidierung

*Konzept, Kopf, Z. 47–54:*

| Quelle | Was daraus einfließt |
|---|---|
| Formelkarte `rechenwege_formelkarte.md` (30.08.2026, gegen `2cfb871d`) | § 3 vollständig, § 4 — **nicht erhalten**: lag im Sitzungs-Scratchpad; Belege heute an der Codestelle |
| Feldkarte `b5_feldkarte.md` | § 2 vollständig, § 5 — **nicht erhalten**: lag im Sitzungs-Scratchpad; Belege heute an der Codestelle |
| [`ueberholt/Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`](../../Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md) | Leitentscheidungen BW1–BW10 |
| [`ueberholt/KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md`](../../KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md) | Datenwelten, Festlegungen L/KL/E/FK |
| [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../aktuell/Grundlagen_KWKG_Energiesteuer_Stromsteuer.md) | Rechtsstand, Sätze, Fristen |
| Protokolle B1–B4, BK1, H1–H4b, H21, HB1, W4 E1–E8, K1–K6, KD1–KD6, P1–P6 | § 6 |

### 2.4 Die Artifacts

*Konzept, Begleitendes Artifact, Z. 62–67 (das gültige Artifact „Dialog, Formel, Zahlenprobe" steht
weiter im Konzept):*

Fünf weitere Artifacts sind durch Repo-Dateien abgelöst und stehen nur noch im Protokoll: das
B5-Dialogmockup durch `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html` Kat. 5 und den gebauten
`BhkwWirtschaftlichkeitDialog`, die Rechenwege durch [`Rechenweg/01…08`](../../../aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/01_Investitionskosten_BHKW.md),
die Erlösrubrik BHKW durch [`Rechenweg/07_Erloesrubrik.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/07_Erloesrubrik.md), die
Pflichtpositionen durch § 2.8 und die ValERI-Bewertung Höfingen durch
[`Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md).

### 2.5 Nachgetragen: die Rechtskette des Nettings

*Konzept § 3.6, Z. 1973–1975 (die Normtafel und der Schluss der Kette stehen weiter im Konzept):*

**Rechtskette — nachgetragen 30.08.2026.** Bis dahin stand die Regel „der Zuschlag bemisst sich auf
die Nettostromerzeugung" **ohne Fundstelle** im Konzept (§ 4.3) und war so implementiert. Der
Nachweis:

### 2.6 Entwurf B der Pflichtpositionen (31.08.2026)

*Konzept § 2.8, Z. 527–530:*

*Aus dem Artifact [Pflichtpositionen je Komponente](https://claude.ai/code/artifact/236c8a8a-a2e0-47f4-a099-aa1456de883a),
Entwurf B, auf Anwenderentscheid in dieses Konzept übernommen. Es ist die Spezifikation des offenen
Punkts „Live-Frisch-Anzeige der Bezugsgröße samt Herleitungszeile im Kostendialog"
(§ 6.3 Nr. 2, H21-Grenze 1).*

Die Entscheide dieses Zeitraums — I‑2, D‑1, E‑1, D‑2 und U‑1 (30.08.2026), D‑3, V‑G5, der
Integrationsort der ValERI-Darstellung und die Übernahme von Entwurf B (31.08.2026), die Abnahme der
Dialogform der Komponentenkosten (02.09.2026) — stehen im Register (R‑EZ, R‑D, R‑V).

---

## 3 Die Wellen vom 17. und 18.09.2026

### 3.1 B5 — der BHKW-Wirtschaftlichkeitsdialog (#286 ff.)

Protokolle [`B5_BhkwWirtschaftlichkeit_Dialog_Protokoll.md`](B5_BhkwWirtschaftlichkeit_Dialog_Protokoll.md)
und [`B5b_Blazor_Port_Protokoll.md`](B5b_Blazor_Port_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B5** (#286) | Der BHKW-Wirtschaftlichkeitsdialog als Razor-Komponente (`BhkwWirtschaftlichkeitDialog.razor`, acht Gruppen, § 2.2): Auszug aus dem Parameterdialog, Schreibweg der Anlagenspalten (K7), Brennstoff-Leser (K4), Live-Herleitung; mit #286 auf die einheitliche Speichern-/Abbrechen-Leiste umgebaut, danach mit #330, #342, #352 weiter gepflegt | keine — solange niemand die neuen Felder pflegt |

*§ 6.3, B5-Kernaufgaben:*

**B5-Kernaufgaben — alle drei erledigt**

1. ~~Schreibweg der drei B3a-Anlagenspalten — `KwkgAnlagenCtrl.Speichere` von 8 auf 11 Spalten (K7)~~
   — **erledigt**: `Speichere(g, mitSteuerangaben)` schreibt 8 + 4 Spalten (`KwkgAnlagenCtrl.cs:283–299`)
2. ~~Live-Frisch-Anzeige der Bezugsgröße mit Herleitungszeile im Kostendialog~~ — **erledigt mit
   U31 (#347) und #364**; spezifiziert bleibt sie in § 2.8 (Entwurf B, übernommen 31.08.2026)
3. ~~Erste Kostenposition mit Anlagenbezug entsteht erst hier~~ — **erledigt mit B5 (#286 ff.)**

### 3.2 B6 — § 9 Abs. 1 Nr. 3 als Ausweis (#328, anderer Rechner)

Protokoll [`B6_Stromsteuer_Ausweis_Protokoll.md`](B6_Stromsteuer_Ausweis_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B6** (#328, anderer Rechner) | § 9 Abs. 1 Nr. 3 StromStG als **Ausweis** statt als Erlös (Befund B-1, Entscheidung K3, § 3.8), Kohärenz-Nachträge, Lokalisierung (K11). **Schemaschritt 88** legt `Tab_ProjektWirtschaftlichkeit.Stromst_Befreiung_Modus` an (TEXT, `AUSWEIS`/`ERLOES`, kein DML, NULL = AUSWEIS); der Modus wandert mit ins Ergebnis | **ja, gewollt** — Projekt 1030: Ausweisbetrag 8.862,15 €/a in beiden Modi, Kapitalwert von −21.763.530,86 € (Erlös) auf −21.895.377,28 € (Ausweis) |

*§ 6.3, nach B6:*

**Nach B6 — alle sechs erledigt**

4. ~~§ 9 Nr. 3 als Ausweis (`Stromst_Befreiung_Modus`, Vorgabe AUSWEIS) — K3~~ — Schemaschritt 88, § 3.8
5. ~~Doppelmeldung § 9b in `RechneAufschlaege` streichen~~ — `RechneAufschlaege` ist mit dem Umbau der Aufschläge entfallen; die § 9b-Hinweise der Kohärenzprüfung schließen einander aus (Fall 2 kehrt zurück, bevor Fall 3 geprüft wird)
6. ~~Hinweiszeile für Träger mit 1.000-kg-Satz bei Literabrechnung (`density` leer)~~ — Fall 4a in § 3.9
7. ~~Positive Nennung im Kohärenzfall 1~~ — Bestätigungszeile in § 3.9
8. ~~Lokalisierung `Views\Wirtschaftlichkeit` (63 Literale) und der Auflöser-Texte — K11~~ — nach dem Razor-Port blieben 7; alle überführt, bewacht von `LokalisierungWirtschaftlichkeitWacheTests`
9. ~~Altkatalog-Bemessung `PROZENT_BRENNSTOFFKOSTEN` nachziehen — K10~~ — war schon erledigt: Der Seed führt sie nur noch zur Anzeige von Bestandsdaten (`FuerBetrieb = false`), abgelöst von `PROZENT_ENDENERGIEKOSTEN`; die Eskalation zieht gleich

### 3.3 B7 — die Erlösrubrik (#329)

Protokoll [`B7_Erloesrubrik_Protokoll.md`](B7_Erloesrubrik_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B7** | Erlösrubrik in Reiter, Word, Excel und BHKW-Vorschau; Energiekosten je Anlage; eine Emissionsspalte nach Modus; eine Sichtbarkeitsregel für alle drei Ausgaben | keine auf den Kapitalwert — Referenzlauf der fünf CI-Projekte PASS |

*§ 2.5, Emissionsanzeige, Z. 353–355 — der Ist-Zustand vor der Umsetzung:*

**Ist-Zustand vor der Umsetzung.** Die Tabelle „Energieträger des Projekts" auf der Kostenseite führt **drei feste
Emissionsspalten** — CO₂ [g/kWh], SO₂ [mg/kWh], NOx [mg/kWh] (`EPOS.UI/Seiten/Berichte/KostenSeite.razor` mit `KostenSeiteGaben.cs:658`, aus BK1).
Sie stehen unabhängig davon da, was das Projekt rechnet.

*§ 2.6, Z. 422–426 — die Nullzeile mit Grund und ihr Anlass (Ausschnitt aus dem Stand-Block; ab
Z. 423 „Eine A-Zeile erscheint …" steht der Absatz weiter im Konzept):*

> **Die Nullzeile mit Grund** (Anwenderbefund 17.09.2026 „Vergütungen und Reduktionen sind in den
> Ergebnissen nicht dargestellt"): Eine A-Zeile erscheint, sobald das Projekt eine Anlage führt,
> für die die Position gilt — auch bei Betrag 0, dann mit dem Klartext der fehlenden Grundlage
> („0 — kein KWK-Zuschlagssatz gepflegt oder Kontingent erschöpft"). Excel bekommt weiterhin die
> blanke Zahl, damit Filter und Diagramme des Blattes numerisch bleiben.

*§ 6.3, nach B7 — was die Etappe offen ließ, und womit es erledigt wurde:*

**Nach B7 — was die Etappe offen lässt**

9a. ~~**B7-1: A4 und A5 stehen in einer Zeile.**~~ — **erledigt mit E4/1 (#432, U7):**
    `SteuerErgebnis` führt `Energiesteuer53Eur`, `Energiesteuer54Eur` und `Energiesteuer54SockelEur`
    getrennt, `EnergiesteuerEur` ist nur noch ihre Summe und kann sich von ihnen nicht lösen; je
    gerechneter Position ein `EnergiesteuerNachweis` (Paragraf, Menge, Satz, Betrag). Die Rubrik
    weist § 53/§ 53a beim Blockheizkraftwerk und § 54 beim Kessel als zwei Zeilen mit Herleitung aus
    (`ERL_A_ENERGIESTEUER`, `ERL_A_ENERGIESTEUER_54`); Nachweisumschlag Fassung 4. Ein vor U7
    gebuchter Stand kennt seine Aufteilung nicht und fällt auf die eine Gesamtzeile zurück — die
    Summe des Blocks A bleibt in jedem Fall zahlengleich (keine Rechenwirkung). Vorher kam der
    Betrag als eine Summe zurück und war an einem Projekt mit Blockheizkraftwerk und Kessel keiner
    Anlage zuzuordnen.
9b. ~~**B7-2: `KwkgModulNachweis` und die Energiekosten je Anlage werden nicht persistiert.**~~ —
    erledigt mit B7P (Anwenderentscheid B7-E-1): Modulnachweis, Energiekosten je Anlage,
    Betriebskostenpositionen (E3) und Kohärenzzeilen reisen als JSON-Umschlag in der Spalte
    `Nachweis_Json` von `Tab_ErgebnisWirtschaftlichkeit` mit, zusammen mit den vier Skalaren
    `VermiedenMengeMWh`, `VermiedenEntlastung9bJahr`, `ProduzierendesGewerbe` und
    `BezugsspitzeKW`. Ohne Schemaschritt: Die Ergebnistabelle wächst wie ihre zwanzig
    Vorgängerspalten über `SpalteSicher`, die Zielversion bleibt 89. Eine Nachweistabelle je
    Ergebnis wäre vier Schemata, vier Schreib- und vier Lesewege für Daten, aus denen nichts
    gerechnet wird. Ein fehlender oder unlesbarer Umschlag kostet nur die Unterzeilen und setzt
    genau einen Kohärenzhinweis — nie eine Ergebniszeile.
9c. ~~**B7-3: Die KWKG-Pauschale (§ 9 KWKG, A3) hat keine Rubrikzeile.**~~ — **erledigt mit U17
    (#346)**: Die Zeile ist gebaut (`WirtschaftlichkeitZeilen.cs:405–412`), die Spalte ebenso
    (`:1209`), bewacht von `KwkgPauschaleZeileTests`; sie steht als Jahr-0-Ausweis, nicht in einer
    €/a-Spalte. Siehe § 2.6 (A3).
9d. ~~**B7-4: Der Grund einer Nullzeile ist aus den Ergebnisdaten abgeleitet, nicht vom Rechner
    durchgereicht.**~~ — **erledigt mit E4/2 (#432):** `SteuerPosition` (`ENERGIEST_53`,
    `ENERGIEST_54`, `STROMST_BEFREIUNG`, `STROMST_ENTLASTUNG`) und `SteuerErgebnis.PositionsGruende`
    ordnen jede Begründung dort zu, wo sie entsteht — der erste Grund je Position gilt, er ist der,
    an dem die Rechnung ausgestiegen ist; die flache Liste bleibt wortgleich für das Hinweisfeld. Je
    Geldzeile der Rubrik eine Herleitungszeile (Text, Einzug 1, ohne Summen- und Excelwirkung):
    Herleitung, wo es eine gibt, sonst der Grund des Laufs; ohne Feststellung entfällt sie. KWKG-
    und Einspeisegrund kommen aus dem Modulnachweis, erfunden wird nichts; Nachweisumschlag
    Fassung 5. Vorher stand die Diagnose nur als mit „ | " verbundener Text im Hinweisfeld, und die
    Rubrik nannte allein die Bedingung der Position.

### 3.4 BK1 — der KWK-Zuschlag gehört der Anlage (#330)

Protokoll [`BKW1_Kwk_Anlagenwahrheit_Protokoll.md`](BKW1_Kwk_Anlagenwahrheit_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **BK1** (Entscheid `BK-E-1` (a)) | KWK-Zuschlag gehört der Anlage: Schemaschritt 89 (`KWKG_Kostenanteil` + Datenschritt), Rückfall Anlage → Projekt entfällt, Kontingent je Anlage nach § 8, Vorschlagsknöpfe am Feld, Gruppe 2 auf die vier projektweiten Angaben eingedampft, **ein** Aktivierungsschalter statt sechs Kopien | keine — Datenschritt ergebnisneutral, gemessen an Projekt 1030 (Zuschlag Jahr 1 7.315,96 €, Kapitalwert −21.895.377,28 € vorher wie nachher); Referenzlauf der fünf CI-Projekte PASS |

*§ 6.3, nach BK1 — was die Etappe offen ließ:*

**Nach BK1 — was die Etappe offen lässt**

9e. ~~**BK1-1: Sechs Projektspalten bleiben ungelesen stehen.**~~ **Erledigt mit BK1a**
    (Anwenderentscheid `BK1-1` „Empfehlung" vom 18.09.2026): **Schemaschritt 90** entfernt
    `KWKG_Bonus`, `KWKG_Bonus_Einspeisung`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel`,
    `KWKG_Tatbestand` und `KWKG_Anlagenart` aus `Tab_ProjektWirtschaftlichkeit`. Der Datenschritt
    bleibt nachvollziehbar — er steht als Quelle in `KwkAnlagenwahrheit`, und Schritt 89 läuft vor
    Schritt 90.
9f. ~~**BK1-2: Der projektweite Ersatzweg rechnet weiter mit den Projektsätzen.**~~ **Erledigt mit
    BK1a:** Der Ersatzweg bildet aus den Anlagen eine **leistungsgewichtete virtuelle
    Gesamtanlage** (§ 3.6) und liest damit dieselbe Quelle wie der Regelweg. Der benannte
    Widerspruch zum Aktivierungsschalter ist damit fort: Ein Projekt mit Anlagensätzen bekommt
    auch auf diesem Weg seinen Zuschlag.
9g. ~~**BK1-3: Ein Jahr-0-Ausweis der KWKG-Pauschale in der Erlösrubrik**~~ — **erledigt mit U17
    (#346)**, dieselbe Sache wie 9c.
9l. ~~**BK1-4: `KWKG_Kostenanteil` (Projekt) hat keinen Rechenleser mehr, das Feld bleibt.**~~
    **Erledigt mit BK1b (Schemaschritt 91):** Anwenderentscheid 18.09.2026 „BK1-4: (a)
    Entfernen“. Eine gepflegte Angabe ohne Wirkung neben derselben Größe mit Wirkung war der
    Rest der doppelten Wahrheit; § 8 Abs. 2/3 KWKG leitet das Kontingent aus dem Kostenanteil
    **der Anlage** ab. Die Projektspalte `Tab_ProjektWirtschaftlichkeit.KWKG_Kostenanteil` und
    ihr Dialogfeld in Gruppe 2 sind entfallen; das Anlagenfeld in Gruppe 1b samt
    Vorschlagsknopf bleibt.
9m. **BK1-Q2 (a) — abgenommen, hier als Ausnahme festgehalten:** Ein Projekt, dessen
    Vbh-Kontingent an Projekt UND Anlagen leer ist, rechnete auf dem Ersatzweg still mit dem
    Feldvorgabewert 30 000 h. Seit BK1a leitet `KontingentDerAnlage` daraus 0 h mit Begründung ab
    — dieselbe Antwort, die der Regelweg seit BK1 gibt. Wissentlich abgenommen.

### 3.5 B7P — die Nachweise eines Laufs überleben ihn (#331, anderer Rechner)

Protokoll [`B7P_Nachweispersistenz_Protokoll.md`](B7P_Nachweispersistenz_Protokoll.md). Die Punkte
9b, 9k und 17 des § 6.3, die B7P erledigt hat, stehen in § 3.3, § 3.7 und § 7.1.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B7P** | Nachweise eines Wirtschaftlichkeitslaufs werden persistiert: `ErgebnisNachweisUmschlag` legt vier Listen und vier Skalare als JSON mit Präfix `nw1:` in `Tab_ErgebnisWirtschaftlichkeit.Nachweis_Json` (über `SpalteSicher`, ohne Schemaschritt), Längenwächter 4 MiB, toleranter Leseweg | keine — die davon-Zeilen stehen auch nach dem Neuladen |

### 3.6 Befund B‑1 — Kesselbrennstoff (#331, dieser Rechner) und die neue Basis (#333)

Protokoll [`B-1_Kesselbrennstoff_Modulzeile_Protokoll.md`](B-1_Kesselbrennstoff_Modulzeile_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B-1 / Kesselbrennstoff** (#331, dieser Rechner) | Die Kessel-Modulspalte `Verbrauch` trägt den Brennstoffeinsatz des Laufs; Endenergie-Positionen am Kessel fallen nicht mehr auf `null` (§ 4, Befunde B-1/N1) | **ja, gewollt** — neue Referenzbasis `2026-09-18_R9_Kesselbrennstoff` mit **#333** eingefroren (§ 6.2) |

*§ 6.3 Nr. 21:*

21. ~~Basiswechsel der Referenzläufe entscheiden~~ — **erledigt mit #333** (neue Basis
    `2026-09-18_R9_Kesselbrennstoff`, § 6.2; heute `2026-09-19_R10_BhkwWirkungsgrad`).
    ~~**Offen bleibt**: Kapitalwert und Betriebskosten von 1030 verankern.~~ — **Entscheid A11
    (20.09.2026 nach Empfehlung): Ankertests zuerst**, die Erweiterung des Referenzlaufs ist eine
    Frage für die nächste Basis. Mit **E1 (#380)** gebaut: Der Kapitalwert 1030 ist verankert
    (−21.895.377,28 €); **offen bleiben allein die Betriebskosten von 1030**

### 3.7 Die Durchsicht der Ergebnisansicht (#332)

*§ 2.13, Einleitung, Z. 998–1004 (bis Z. 1002 steht der Absatz weiter im Konzept):*

*Die Ergebnisansicht steht in Kategorie 8 des einen Mockups
`../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`: Kopf mit Umschalter, dann die
vier Fragen „Lohnt es sich · Wie sicher ist das · Woraus entsteht die Zahl · Was ist angenommen",
die Kapitalwertformel, die Gegenprobe und die Bericht-Ausgaben. Woraus die einzelnen Beträge
entstehen, sagen die Kategorien 1 bis 7; was noch nicht gebaut ist, steht im Anhang
„Umsetzungsstand". Der Anwender hat die Ansicht am 18.09.2026 durchgesehen; die fünf Punkte, dazu (6) als Verweis, und ihre
Messung am Bestand:*

*§ 2.13, Z. 1144–1149 — weitere Festlegungen des Mockups:*

**Weitere Festlegungen des Mockups:** Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf der Seite
(K8/V-1); Hinweistext zu den Szenarien unter der Annahmentafel (§ 2.11.7) — beide **umgesetzt #434**; der Kopfabschnitt „Was
sich gegenüber der heutigen Seite ändert", der alle fünf Punkte führt, steht bislang nur im
abzulösenden Mockup `../../../aktuell/Mockups/Ergebnis_Bandbreite_Herkunft.html`. Die drei Entscheide, die den
Zuschnitt änderten: K-3 ist mit B6 erledigt (Statuszeile #328, anderer Rechner), B-1 (Kessel) behebt
Auftrag #331, die Erlösrubrik steht seit B7.

*§ 6.3, aus der Durchsicht der Ergebnisansicht:*

**Aus der Anwenderdurchsicht der Ergebnisansicht (§ 2.13)**

9h. **Nutzungsdauer, Ersatz, Restwert — drei fehlende Stücke (Mockup-Anhang U39):** Entkopplung
    von Ersatz und Restwert, die ungelesenen geräteeigenen Nutzungsdauer-Spalten, der Anschluss
    der Speicherflotte; dazu der Hinweis „T über Vorgabe, Position ohne Dauer" auf Seite und
    Bericht und die plattformfreie Hülle der Zeitraumzeile. **Erledigt mit #357:** die Nachpflege
    des Bestands (Knopf „Nutzungsdauern vorbelegen…") und der Pflegeort der Positionsart
    (Klappliste im Zeileneditor) — s. § 2.13 (3). **Erledigt mit E5 (#434):** der Hinweis „T über
    Vorgabe, Position ohne Dauer" auf der Seite und in Wort- und Tabellenbericht und die Zeitraumzeile,
    beide aus dem Kern (`NutzungsdauerHinweisCtrl`) über die plattformfreie Hülle. Offen bleiben die
    drei Stücke (E7/E10).
9i. ~~**Erlösrubrik nach Komponente innen:** Anlagenbezug je Katalogzeile, Block „projektweit",
    Eigenverbrauchsmengen je Anlage für die vermiedenen Kosten.~~ — **erledigt mit E4/3 (#432,
    U6; Q15, A12):** `WirtZeile.Komponente` mit den Kennungen `KOMPONENTE_BHKW`, `_PV`, `_KESSEL`,
    `_PROJEKTWEIT`; A und B bleiben die äußere Ordnung, innen je Komponente Kopf, Zeilen und
    Zwischensumme (`WIRT_ERL_KOMPONENTE`, `WIRT_ERL_TEILSUMME`), zuletzt „projektweit"
    (`WIRT_ERL_PROJEKTWEIT`); Block A summiert unverändert dieselben Summanden, Block B endet je
    Komponente mit „vermiedene Kosten wirksam". `VermiedenAnlageNachweis.Verteile()` ist der eine
    Verteilschlüssel (Näherung V‑4 nach den Eigenverbrauchsmengen, ausgewiesen; die letzte Zeile
    trägt den Rest, damit die Summe bitgenau trifft), der Leistungsanteil bleibt projektweit und
    nennt ohne Bezugsspitze den Grund. Seite, Wort- und Tabellenbericht und BHKW-Vorschau lesen den
    einen Katalog; Nachweisumschlag Fassung 6, kein Schemaschritt. Offen bleibt Nr. 32 (U6‑Q1:
    die vermiedene Menge führt heute keinen PV-Eigenverbrauch).
9j. **Verlauf mit drei Szenarien:** Dreierreihe statt eines Szenarios je Lauf, Reihenbildung
    Variante × Szenario, Spaltengruppen je Szenario im Tabellenbericht, plattformfreie Rechen-
    und Zeichenlogik. `Gestrichelt` liest das Verlaufsbild.
9k. ~~**Persistenz der Nachweise je Anlage** (Energiekosten-Unterzeilen nach dem Neuladen) — Teil
    von B7-2.~~ — erledigt mit B7P zusammen mit 9b: Der Nachweisumschlag der Ergebniszeile
    trägt die Unterzeilen mit.

### 3.8 BK1a — der Ersatzweg auf Anlagenbasis, Schemaschritt 90 (#335)

Protokoll [`BK1a_Projektspalten_Drop_Protokoll.md`](BK1a_Projektspalten_Drop_Protokoll.md). Die
Punkte 9e und 9f des § 6.3 stehen in § 3.4.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **BK1a** | Aufräumen nach der Anlagenwahrheit: Schemaschritt 90 entfernt die sechs KWKG-Projektspalten; Ersatzweg über eine leistungsgewichtete virtuelle Gesamtanlage (Gewicht `g_i = P_el,i`), Gruppe 2 auf die projektweiten Angaben eingedampft | keine — Datenschritt ergebnisneutral |

### 3.9 BK1b — der Kostenanteil fällt, Schemaschritt 91 (#336)

Protokoll [`BK1b_Kostenanteil_Drop_Protokoll.md`](BK1b_Kostenanteil_Drop_Protokoll.md). Punkt 9l
des § 6.3 steht in § 3.4.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **BK1b** | Die Projektspalte `KWKG_Kostenanteil` fällt (Schemaschritt 91); der Anteil der Neuherstellungskosten steht nur noch an der Anlage | keine — einziger Pflegeort wandert, kein Rechenleser betroffen |

*§ 6.5, Z. 2686–2706 — die aufgelöste Doppelung des KWK-Zuschlags:*

> **Aufgelöst mit BK1 (Entscheid `BK-E-1` (a), Schemaschritt 89): der KWK-Zuschlag.** Er stand
> zweimal da — je Anlage (`Tab_Energieanlagen.KWKG_*`, Schritt 22) und je Projekt
> (`Tab_ProjektWirtschaftlichkeit.KWKG_*`, Schritt 28) —, und dazwischen lag eine Rückfallkette.
> Der Anwender pflegte damit Felder, deren Wirkung davon abhing, ob ein zweites Feld anderswo leer
> war; eine Kaskade aus zwei verschieden alten Modulen war gar nicht abbildbar. Die Wahrheit ist
> jetzt die Anlage.
>
> **Vollständig aufgelöst mit BK1a (Schemaschritt 90).** Die sechs Projektspalten `KWKG_Bonus`,
> `KWKG_Bonus_Einspeisung`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel`, `KWKG_Tatbestand` und
> `KWKG_Anlagenart` sind entfernt; der letzte Leser — der projektweite Ersatzweg — rechnet seither
> mit einer leistungsgewichteten virtuellen Gesamtanlage aus den Anlagen (§ 3.6). **Ein Vorbehalt
> bleibt und ist abgenommen** (`BK1-Q2` a): Wo weder Projekt noch Anlage ein Vbh-Kontingent
> führen, galten bisher still 30 000 h; jetzt gilt 0 h mit Begründung. **Ein Feld bleibt ohne
> Leser stehen** (`BK1-Q1` c, offener Punkt `BK1-4`): `KWKG_Kostenanteil` des Projekts samt seinem
> Dialogfeld.
>
> **Damit zu Ende gebracht mit BK1b (Schemaschritt 91).** Auch dieses letzte Feld ist fort:
> Projektspalte und Dialogfeld in Gruppe 2 entfallen, der Kostenanteil steht nur noch an der
> Anlage (Gruppe 1b, mit Vorschlagsknopf am Feld). `Tab_ProjektWirtschaftlichkeit` führt von den
> ursprünglich elf KWKG-Projektspalten noch vier: `KWKG_Abschlag_Negativ`, `KWKG_Pauschalmodus`,
> `KWKG_Stichtag` und `KWKG_Inbetriebnahme` — alle vier gelten wirklich projektweit.

### 3.10 Nutzungsdauern S2 (#357)

Statuszeile #357; die Regeln stehen im
[Nutzungsdauer-Konzept](../../Konzept_Nutzungsdauer_AfA_EPOS-Plan.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **Nutzungsdauern S2** (#357) | Knopf „Nutzungsdauern vorbelegen…" als vierter der Rasterleiste (U8) und die Tafel „Ersatz und Restwert" im Kostendialog (U30); Positionsart als Klappliste im Zeileneditor (§ 2.13 (3)) | keine, solange keine Dauer gepflegt wird; danach **ja** — Ersatz und Restwert entstehen |

*§ 2.13 (3), Z. 1044–1048:*

**Erledigt mit #357** (Nutzungsdauern Stufe S2, U8 und U30): die **Nachpflege des Bestands** — der
Knopf „Nutzungsdauern vorbelegen…" steht als vierter der Rasterleiste
(`KostenKomponenteDialog.razor:310`); offen bleibt allein sein gesperrter Zwilling auf der
Betriebsseite. Ebenso der **Pflegeort für die Positionsart** — `NutzungsdauerID` ist seither eine
Klappliste im Zeileneditor (`KostenProjektPositionenCtrl.NutzungsdauerArtZuordnen`).

### 3.11 Die Entscheide vom 18.09.2026 zu V‑G10 und V‑4 und die Messungen dazu

Die Entscheide selbst — K8/V‑1, V‑2/V‑G10, V‑4, K‑1, BK‑E‑1, BK1‑1…BK1‑4, BK1‑Q1/Q2, K‑WZ‑1,
B7‑E‑1/2, VG‑Q1…Q7, VV‑Q1…Q7, ET‑D‑1…3 und E1 — stehen mit Wortlaut im Register. Hier stehen die
Messungen, die das Konzept bis zum Schnitt neben den beiden ValERI-Entscheiden führte.

*§ 2.11.6, Z. 822–841 — Entscheid zu V‑G10 und die Grenze am Generator:*

**Der Anwender hat abweichend von der Empfehlung entschieden: der ganze Bericht formelbasiert,
nicht nur das ValERI-Blatt.** Das kippt V-2. Damit das Konzept nichts Unmögliches verspricht, ist
die Grenze am Generator gemessen (`ExcelBerichtGenerator`, eine Klasse, 1 412 Zeilen (gemessen 19.09.2026), vier
Blattarten; adversarisch gegengelesen):

- Der Generator schreibt heute **keine einzige Formel** — `.Value` mit Zahl oder Text, Zahlenformat,
  Füllfarbe, Autofilter, Freeze; keine benannten Bereiche, keine Excel-Tabellen, keine Diagramme,
  keine Bilder. Er greift **nur fünfmal** auf den Parametersatz zu (Nachweis, `SatzFuer`,
  `NichtMonetaer`, Betrachtungszeitraum als Rechenargument, `IdKraftwerkspark`) — **keine Zahl des
  Parametersatzes erreicht eine Zelle**; die Annahmen stehen als ein zusammengesetzter Text in einer
  einzigen grauen Zelle.
- Vier Annahmen der ersten Messung hat die Gegenlesung widerlegt, und sie begrenzen den Plan: Die
  Spalte *Betrieb* ist **keine** einfache Fortschreibung (zwei verschieden eskalierte Töpfe mit p_B
  und p_E, deren Basen bei Positionen mit späterem Startjahr springen); die Sensitivitätstafel hat
  **fünf** Zeilen und ist **nicht** als Mehrfachoperation darstellbar (jede Zeile ist die Differenz
  zweier neu gerechneter Zahlungsbilder, die fünfte streicht die KWKG-Reihe ganz, der
  Investitionsausschlag koppelt in die abgeleiteten Betriebskosten); die Kosten-Kennzahlen sind
  **nicht** Menge × Preis (Grundpreise je Träger und ein Leistungspreisanteil aus der
  **Viertelstunden**-Bezugsspitze — feiner als das Stundenraster); die Spalte *Herleitung* bleibt
  nötig (fester Betrag, szenariogepflegter Wert, fehlende Menge oder fehlender Satz).

*§ 2.11.7, Z. 883–890 — die Messung hinter dem Hinweistext (Z. 883–884 stehen weiter im Konzept):*

Die vollständigen Parametersätze je Szenario (§ 2.11.5) kommen **nach** der Darstellungsetappe. Bis
dahin sagt ein Hinweis unter der Annahmentafel der Wirtschaftlichkeitsseite, was ein Szenario heute
variiert und was nicht. **Gemessen** an `WirtschaftlichkeitParameter.FuerSzenario` und der Eingabe
(`LiesInvestitionen`, Einspeiseerlös, PV-Reihe): Best und Worst ersetzen Zins, p_E, p_B und p_I und
wirken in der Eingabe auf Investition, Erträge und Nutzungsdauer ungepflegter Positionen; der
Betrachtungszeitraum, die Trägerpreise, die Erlössätze, die Mengen und die gesetzlichen Sätze
bleiben in allen drei Szenarien gleich. (Die Spalte „EPOS heute" der Zeile V-G5 beschreibt den
Stand vor der Etappe W5‑B‑9; seitdem variieren Zins und Preisraten sehr wohl.)

---

## 4 Die Wellen vom 19.09.2026

### 4.1 VG — wählbares Vergleichsprojekt und Vergleichssicht (#358)

Statuszeile #358; Schemaschritt 92.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **VG** (§ 2.9, § 2.15) | Wählbares Vergleichsprojekt je Gruppe (Schemaschritt 92, `ID_Referenzprojekt`, NULL = Stamm) und die Vergleichssicht der Ergebnisansicht: alle Varianten gegen die Referenz oder zwei Stände A und B mit A als Referenz dieser Sicht. Die Referenz ist Parameter von `Berechne` und `BerechneVerlauf`, ihre Auflösung samt Randfällen steht einmal in `Referenzwahl`; Sicht, A und B liegen in `Vergleichsauswahl`, der Paarlauf persistiert nicht | keine in der Vorgabe — Referenz = Stamm rechnet bitgleich, Referenzlauf der fünf CI-Projekte PASS |

*§ 2.9, Z. 603–604 — die Anforderung:*

**Anforderung des Anwenders:** Die Differenzrechnung soll nicht fest gegen das Stammprojekt laufen —
**die Referenz (das Vergleichsprojekt) soll wählbar sein.**

*§ 2.9, Z. 606–610 — der Ist-Zustand vor der Umsetzung:*

**Ist-Zustand vor der Umsetzung — die Referenz war hart verdrahtet.** Das Stammprojekt war überall die
Unterlassensalternative: `Kapitalwertdifferenz = KW(Variante) − KW(Stamm)`; Annuität, dynamische
Amortisation und IZF rechneten ausschließlich auf dieser Differenz; der Stamm war als Referenz
**nicht abwählbar**, und die Nachweiszeile sagte fest „Referenz: Stammprojekt". Eine Auswahl
existierte nirgends.

*§ 2.9, Z. 612–619 — warum die Anforderung fachlich richtig ist:*

**Warum die Anforderung fachlich richtig ist:** Die Altanwendung (Höfingen-Mappe,
`Tab_kurz_KWKG2020`) rechnet durchgehend gegen eine ausdrücklich benannte **Vergleichsheizung** —
Investition 9.624 €, Betriebskosten 518 €/a, Brennstoff 11.498 €/a sind dort eigene Größen der
Referenz, und jede Erlöszeile („Einnahmen Wärme = Betriebskosten des Vergleichssystems") ist eine
Differenz dagegen. Auch DIN EN 17463 verlangt den Vergleich gegen die **Unterlassensalternative** —
und welche Alternative das ist, ist eine fachliche Entscheidung je Bewertung, keine Strukturvorgabe
der Software. Wer zwei Ausbauvarianten gegeneinander stellen will (statt jede gegen den Stamm),
braucht die freie Wahl.

*§ 2.9, Z. 642–644 — die Einordnung:*

**Einordnung:** Umgesetzt als eigene, kleine Etappe — ergebnisneutral in der Vorgabe, erste
Rechenwirkung erst bei ausdrücklicher Wahl einer anderen Referenz. Sie steht **vor** der
ValERI-Berichtsetappe, weil deren Bewertungsbericht die Unterlassensalternative benennen muss.

*§ 2.15, Z. 1191–1195 — die Anforderung im Wortlaut:*

**Anforderung des Anwenders, im Wortlaut:** „Es soll die Optionen geben, entweder Stamm mit allen
Varianten (wie bisher) oder zwischen zwei Varianten (oder Stamm mit einer Variante)." Gemeint ist die
Ergebnisansicht der Wirtschaftlichkeit (Berichte & Kosten → Wirtschaftlichkeit) mit ihren
Vergleichstafeln — Kennzahltafel, Empfehlung, Bandbreite, Verlauf, Gliederung des Kapitalwerts und
Brücke.

*§ 2.15, Z. 1197–1200 — warum ein eigener Abschnitt:*

**Warum ein eigener Abschnitt und kein sechster Punkt in § 2.13:** Die fünf Punkte der
Anwenderdurchsicht sind Darstellungsbefunde je Größe. Die Vergleichssicht greift dagegen in die
Differenzrechnung ein und ist das Schwesterstück von § 2.9 — sie braucht dieselbe Gliederung
(Ist, Soll-Tafel, Randfälle, Abnahme, Einordnung als Etappe). § 2.13 verweist unter (6) hierher.

*§ 2.15, Z. 1202–1210 — der Ist-Zustand vor der Umsetzung:*

**Ist-Zustand vor der Umsetzung:** Die Ergebnisansicht stellte alle angehakten Stände der
Vergleichsgruppe nebeneinander — eine Spalte je Stand — und rechnete die Differenzkennzahlen jeder
Variante (Kapitalwertdifferenz, Annuität, dynamische Amortisation, interner Zinsfuß) gegen den
Stamm. `KapitalwertRechner.AmortisationDifferenz` und `InternerZinsfuss` nehmen zwei
Zahlungsbilder; `WirtschaftlichkeitZeilen.Kennzahlen` zeichnete die Stammspalte mit dem
Platzhalter „Referenz". Welche Stände in der Ansicht stehen, entscheiden die Häkchen der
Vergleichsgruppen-Liste (`Vergleichsauswahl` — eine Sitzungswahl für Übersicht, Kosten und
Wirtschaftlichkeit). Eine Wahl zweier Stände gegeneinander gab es nicht. Das Mockup zeigt diesen
Aufbau in Kategorie 8 als Sicht 1.

*§ 2.15, Z. 1287–1297 — die Abnahme:*

**Abnahme:**

- Sicht 1 ist byte-gleich zum Bestand: Referenzlauf der fünf Projekte, Word- und Excel-Bericht
  unverändert.
- Sicht 2 mit A = Gruppenreferenz: alle Kennzahlen von B stimmen mit der Spalte B der Sicht 1
  überein (Test).
- Sicht 2 mit A ≠ Gruppenreferenz: Kapitalwertdifferenz(B − A) = Kapitalwertdifferenz₁(B) −
  Kapitalwertdifferenz₁(A) auf 0,01 €; die Gliederung geht je Bestandteil auf; ein Tausch von A
  und B dreht das Vorzeichen von Kapitalwertdifferenz und Annuität (Tests).
- bunit: beide Zustände der Optionsgruppe, Listen ohne den jeweils anderen Stand, Sperre bei nur
  einem Stand, Erklärzeile mit beiden Referenzen.

*§ 2.15, Z. 1299–1312 — was die Umsetzung brauchte:*

**Was die Umsetzung braucht:**

1. die Referenz als Parameter der Differenzrechnung in `WirtschaftlichkeitCtrl.Berechne` — dort
   ist der Stamm fest verdrahtet (`if (v.IstStamm) { … stammBild = bild; … }`); das ist zugleich
   der Kern der Etappe § 2.9;
2. Sicht, A und B in `Vergleichsauswahl` neben den Häkchen — plattformfrei, mit Vorbelegung und
   Rückfallregel;
3. `WirtschaftlichkeitZeilen.Kennzahlen` mit Menge und Referenz statt `IstStamm` als
   Referenzkennzeichen; der Platzhalter `StammAnzeige` wird zum Referenzplatzhalter;
4. die Optionsgruppe mit zwei Klapplisten und Erklärzeile in `WirtschaftlichkeitSeite.razor`,
   Texte in `MyResource.Resource.*` (beide Sprachen);
5. Bericht: Menge und Referenz aus der Sitzungswahl, dazu die Deklarationszeile;
6. Verlauf und Brücke lesen die Referenz aus derselben Wahl — `BerechneVerlauf` rechnet die
   Differenz zum Stamm und braucht denselben Parameter.

*§ 2.15, Z. 1326–1328 — die Einordnung:*

**Einordnung:** Eigene kleine Etappe **nach § 2.9**, deren Referenzparameter sie voraussetzt;
ergebnisneutral in der Vorgabe (Sicht 1), erste Wirkung erst mit der Wahl der Sicht 2. Mockup:
Kategorie 8 in `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html#sicht2`, Umsetzungsstand U37.

### 4.2 VV — Vergütung je Variante (#359)

Statuszeile #359; Schemaschritt 93.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **VV** (§ 2.16, #359) | Vergütung je Variante: eigene Werte oder vom gewählten Projekt übernommen. **Schemaschritt 93** an `Tab_ProjektPhotovoltaik`; der Kopierlauf fror die Vergütung bisher nur zufällig ein | keine in der Vorgabe — Übernahme rechnet wie bisher |

*§ 2.16, Z. 1348–1352 — die Anforderung im Wortlaut:*

**Anforderung des Anwenders, im Wortlaut:** „Die Vergütung kann in der Variante unterschiedlich vom
Stamm sein. Die Option der Übernahme soll es geben, aber eigene Vergütung in den Varianten muss
möglich sein." Gemeint ist die PV-Vergütung, die der Reiter „Ertrag/Bonus" des Kostendialogs
(`Rechenweg/03_Kosten_Photovoltaik.md`) und der Knopf „Photovoltaik…" der Wirtschaftlichkeitsseite
im Vergütungsdialog (§ 2.3, `Rechenweg/06_Verguetungen_PV.md`) öffnen.

*§ 2.16, Z. 1354–1358 — warum ein eigener Abschnitt:*

**Warum ein eigener Abschnitt:** Wie § 2.9 (Referenz) und § 2.15 (Vergleichssicht) greift die
Anforderung in die Frage ein, was je Gruppe und was je Stand gilt (Regel R‑1: Rahmenparameter je
Stammprojekt). § 2.9 wählt die Referenz je Gruppe, § 2.15 die Sicht je Sitzung; dieser Abschnitt löst
eine Größe, die als gruppenweit galt, auf die Stände auf. § 2.13 bekommt keinen neuen Punkt: Die
Vergütung ist keine Darstellungsfrage der Ergebnisansicht.

*§ 2.16, Z. 1360–1401 — der Ist-Zustand am 18.09.2026:*

**Ist (18.09.2026) — zwei Ablageorte, ein Leseweg je Stand:**

- *Der flache Einspeisesatz* `Einspeiseverguetung` [€/kWh] (und `Einspeiseverguetung_KWK`) steht in
  der Rahmenzeile `Tab_ProjektWirtschaftlichkeit` — eine Zeile je Stammprojekt (`ID_Projekt` =
  Stamm; `WirtschaftlichkeitParameter.IdStamm`, `WirtschaftlichkeitCtrl.LadeParameter(idStamm)`;
  Schemaschritt 84 hat ihn aus der Trägerkarte hierher gezogen, `Allgemein/Update/VerguetungUmzug.cs`).
  `Berechne(daten, p)` rechnet jede Variante mit demselben `p`: `Erlös = PV-Überschuss × 1000 ×
  p.Einspeiseverguetung` (Flat-Pfad). Er gilt je Gruppe — wie Zins und Zeitraum (R‑1).
- *Die Dialogangaben* — Vermarktungsform, anzulegender Wert (Override), Einspeiseart,
  Inbetriebnahme, Degradation, DV-Entgelt, PPA, § 51/§ 51a, 60-%-Begrenzung, Marktwerte — stehen in
  `Tab_ProjektPhotovoltaik` (Schemaschritt 41; eindeutiger Index auf `ID_Projekt`, keine
  Löschweitergabe), im Katalog als „eine Zeile je Stammprojekt" beschrieben. Geschrieben wird sie über
  `ProjektPhotovoltaikCtrl.Speichern` mit der Id, die die Hülle hereingibt:
  `PhotovoltaikVerguetungHuelle.Gaben(idStamm)` von der Wirtschaftlichkeitsseite
  (`WirtschaftlichkeitSeiteGaben`, Stamm der Gruppe) und vom Reiter Ertrag/Bonus mit dem Eintrag der
  Klappliste „Stammprojekt:" — die alle Projekte aus `Tab_Projekt` führt, Varianten eingeschlossen
  (`KostenVorlagenUebernahmeCtrl.Projekte`), ohne Vorwahl des geöffneten Projekts
  (`ErtragBonusGaben.Bauen(komponente)` kennt keine Projekt-Id).
- *Der Rechenweg liest je Stand:* `WirtschaftlichkeitCtrl.RechnePvVerguetung(v, p, e)` holt
  `ProjektPhotovoltaikCtrl.Lies(v.IdProjekt)` — die Zeile des **jeweiligen** Projekts, Stamm wie
  Variante —, gibt sie an `PvErloesRechner.Rechne` und ersetzt den PV-Anteil des flachen Erlöses durch
  die Reihe `PV_VERGUETUNG`; fehlt die Zeile oder ist sie inaktiv, bleibt der Flat-Pfad.
  `SkaliereErtraege` skaliert danach je Stand (Best/Worst).
- *Folge:* Die „eine Vergütungswahrheit" ist eine Wahrheit **je Projekt**, nicht je Gruppe. Eine
  Variante bekommt die Dialogangaben nur, wenn sie eine eigene Zeile hat — und die hat sie genau
  dann, wenn sie **nach** der Pflege des Stamms angelegt wurde: `VariantenCtrl.AnlegenAusStamm`
  kopiert über `ProjektDuplizierenCtrl` jede Tabelle mit `ID_Projekt` (ausgenommen allein
  `Berichtskonfiguration`), also auch `Tab_ProjektPhotovoltaik` und `Tab_ProjektWirtschaftlichkeit`.
  Diese Kopie ist ein eingefrorener Stand des Anlegetags; spätere Änderungen am Stamm erreichen sie
  nicht. Eine Variante, die vor der Pflege angelegt wurde, hat keine Zeile und rechnet mit dem
  flachen Satz. Beides ist von außen nicht zu erkennen: Reiter, Dialog und Bericht sagen
  „stammprojektbezogen". Die Testdatenbank führt keine Zeile in `Tab_ProjektPhotovoltaik`; in
  `Tab_ProjektWirtschaftlichkeit` tragen die Varianten 1023 und 1024 (Stamm 1019) Kopien, die kein
  Rechenweg liest.
- *BHKW-Vergütung, zum Vergleich:* Die KWKG-Größen (Satz Eigen/Einspeisung, Vbh-Kontingent,
  Jahresdeckel, Anlagenart, Eigenstromfall, Stichtag, Inbetriebnahme, Kostenanteil) stehen an der
  Anlage (`Tab_Energieanlagen.KWKG_*`); Anlagen gehören dem Projekt (`BhkwAnlagen(v.IdProjekt)`), die
  Variante hat ihre eigenen — beim Anlegen kopiert, seither eigenständig. Der BHKW-Dialog zeigt die
  Anlagen der ganzen Gruppe (`KwkgAnlagenCtrl.LadeGruppe(idStamm)`). Nur der flache KWK-Einspeisesatz
  und die projektweiten KWKG-Angaben (Stichtag, Abschlag bei negativen Preisen, Pauschale) liegen in
  der Rahmenzeile je Gruppe. Das BHKW erfüllt die Anforderung für den Zuschlag also von selbst, weil
  er anlagenscharf ist; die Regel „Vergütung je Stand, Rahmensatz je Gruppe" ist dort schon Praxis.

*§ 2.16, Z. 1454–1458 — das Abnahmekriterium:*

**Abnahmekriterium:** Alle Bestandsvarianten auf „übernehmen" (Testdatenbank: keine Zeile in
`Tab_ProjektPhotovoltaik`, weder beim Stamm noch bei einer Variante) → Referenzlauf **byte-gleich**;
die Wahl „eigene Vergütung" mit einer Zeile, die der Stammzeile wertgleich ist → dieselben Zahlen wie
„übernehmen" (Tests auf `WirtschaftlichkeitErgebnis.PvAnzulegenderWert` und `EinspeiseerloesPvJahr`);
Stammänderung → die übernehmende Variante folgt, die eigene nicht (Test).

*§ 2.16, Z. 1460–1486 — was die Umsetzung brauchte:*

**Was die Umsetzung braucht:**

1. Schemaschritt **93**: Spalte `Uebernahme_Stamm` an beiden DDL-Orten, Ableitung der Wahl aus
   dem Bestand (Randfälle), Löschweitergabe `Tab_Projekt → Tab_ProjektPhotovoltaik` — sie steht
   als Vorarbeit in `ProjektCtrl.Delete`, siehe Einordnung;
2. `ProjektPhotovoltaikCtrl.LiesAufgeloest` und `Speichern` mit der Spalte;
   `ProjektDuplizierenCtrl`: `Tab_ProjektPhotovoltaik` in die feste Ausnahmeliste; `ProjektCtrl.Delete`
   und `VariantenCtrl.LoescheVariante`: Übernahme in eigene Zeilen vor dem Lösen;
3. `WirtschaftlichkeitCtrl.RechnePvVerguetung` auf die Auflösung; Herkunft im Nachweisumschlag;
   Zeile in `WirtschaftlichkeitZeilen` (Word, Excel); `KohaerenzPruefung`;
4. Dialoge: Optionsgruppe und Erklärzeile in `ErtragBonus.razor`, Projekt-Id in
   `ErtragBonusGaben.Bauen` (die Vorwahl ist das geöffnete Projekt, nicht das erste der Liste),
   Hinweiszeile und Knopf „eigene Werte" in `PhotovoltaikVerguetungDialog.razor`; die Hülle
   (`PhotovoltaikVerguetungHuelle`) öffnet für das gewählte Projekt; auf iOS war der Dialog **aus
   zwei Gründen** nicht erreichbar. **Stand: umgesetzt #431** — beide sind gefallen:
   `PhotovoltaikVerguetungHuelle` liegt jetzt plattformfrei in `EPOS.UI.Daten/Wirtschaftlichkeit/`,
   und `IosProjektQuelle.BerichteKostenGaben` liefert nicht mehr `null` (erreichbar als Überlagerung
   der Wirtschaftlichkeitsseite über `BERICHTE_KOSTEN`); ein bestätigender `ios.yml`-Lauf steht noch
   aus;
5. Ressourcen (beide Sprachen): Optionsgruppe, Erklärzeilen, Hinweiszeile, Knopf, Nachweiszeile,
   Kohärenztext;
6. Tests: Auflösung (Stamm; Variante eigene; Variante übernommen; Stamm ohne Zeile), Kopierlauf ohne
   PV-Zeile, Löschweg, Ableitung aus dem Bestand, bunit für beide Zustände der Optionsgruppe,
   Referenzlauf;
7. Wiki: `Programm Dokumentation - Wirtschaftlichkeit.wiki` (Vergütungsdialog, Herkunftszeile) und
   `Programm Dokumentation - Varianten.wiki` (was eine Variante vom Stamm übernimmt); Logbuch-Eintrag
   mit der Veröffentlichung.

*§ 2.16, Z. 1500–1503 — die Einordnung:*

**Einordnung:** Eigene kleine Etappe, unabhängig von § 2.9 und § 2.15 — sie ändert weder Referenz
noch Sicht, nur den Leseweg einer Größe je Stand; ergebnisneutral in der Vorgabe. Mockup:
Kategorie 3 (Reiter Ertrag/Bonus) unter `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html#pvkosten` und
Kategorie 6 (Kopfzeile) unter `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html#pv`; Umsetzungsstand U38.

### 4.3 Übernahme aus der Kostenverwaltung (#363)

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **Übernahme aus der Kostenverwaltung** (#363) | „Aus Vorlage übernehmen…" öffnet den **Katalogblock** der Administration (Komponente · Kategorie · Variante · Positionsvorschau mit Spalte „Ziel") statt der bisherigen Klappliste | keine — reine Auswahlseite |

### 4.4 Bezugsgrößen (#364)

Punkt 2 des § 6.3 (mit U31, #347) steht in § 3.1.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **Bezugsgrößen** (#364) | Betriebskosten: Bezugsgrößen aus dem gespeicherten Lauf, jeder Fehlgrund benannt; Grundlage der Live-Frisch-Anzeige (§ 2.8 Punkt 3, § 6.3 B5-Kernaufgabe 2) | **ja** — zuvor ungerechnete Hilfsenergiekosten entstehen |

### 4.5 Hilfsstrom am Endenergiebedarf (#365/#366)

Schemaschritt 94; K2 des Konzepts § 5 (Register R‑K).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **Hilfsstrom am Endenergiebedarf** (#365/#366) | **Schemaschritt 94** (reines DML): Die drei Hilfsstrom-Positionen der Standardvorlagen BHKW, Heizkessel und Wärmepumpe wechseln von „% der Endenergiekosten" auf „% des **Endenergiebedarfs**"; Weg B bewertet den Endenergiebedarf einer Anlage mit dem **Preis ihres eigenen Stromträgers** (§ 2.2 Gruppe 1, § 5 K2) | **ja, gewollt** — die Hilfsenergiekosten werden überhaupt erst berechnet |

---

## 5 Die Wellen vom 20.09.2026

### 5.1 E0 — Papierpflege (#379)

Protokolle [`E0a_Papierpflege_Konzept_Wirtschaftlichkeit_Protokoll.md`](E0a_Papierpflege_Konzept_Wirtschaftlichkeit_Protokoll.md),
[`E0b_Papierpflege_Mockups_Index_Protokoll.md`](E0b_Papierpflege_Mockups_Index_Protokoll.md) und die
Nachpflege [`E0c_Papierpflege_Stand_428_Protokoll.md`](E0c_Papierpflege_Stand_428_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E0 Papierpflege** (#379) | Papierpflege ohne Entscheid: Kopfzeile, Geltungsblock, Quelltabelle und Artifacts dieses Papiers; § 2.2/§ 2.3/§ 2.4/§ 2.7/§ 2.8/§ 2.12 auf den Razor-Stand; § 3.1 Formelkarte mit Endenergie-Topf und p_I; § 6.1 um acht Etappenzeilen, § 6.3 um neun Erledigte bereinigt, § 6.4/§ 6.5/§ 7 berichtigt; neuer Anhang „Kürzel und Etappen"; dazu Szenarienkonzept, Nutzungsdauer-Konzept, Rechenwege 04/05/08, Mockups und Index (E0b). **Nicht ausgeführt:** der Schnitt in drei Papiere (A13) | **keine** — kein Code berührt |

*§ 2.2, Z. 218–219, und § 2.7, Z. 522–523 — zwei Vermerke, die E0a gesetzt hat; beide Fragen sind
entschieden (Register R‑Q, Q2 und Q9):*

*Entscheid Q2 offen (Analyse vom 19.09.2026):* ob der Vorschlag am Feld bleibt oder als
Sammelknopf „Sätze und Herkunft…" mit der Überlagerung U22 zusammengeführt wird.

*Entscheid Q9 offen (Analyse vom 19.09.2026):* Kopfband und Fehlerfarbe des Hausstils sowie die
Frage, ob die Bauformregeln als eigener Abschnitt „Hausstil Dialoge" geführt werden.

*§ 6.3, aus der Papierpflege E0 — Nr. 30 bis 32 im Wortlaut vor dem Schnitt (30 und 32 stehen als
Regel weiter im Konzept, 31 ist erledigt):*

**Aus der Papierpflege E0 (#379) — Sachpunkte der Datenaufnahme**

*Beide Punkte sind neue Anwenderfragen ohne Empfehlung; der Entscheid „nach Empfehlung" vom
20.09.2026 deckt sie **nicht**.*

30. **Sieben Energieanlagen tragen `KWKG_Anlagenart = ''`** (leere Zeichenkette statt NULL oder
    eines Steuerwerts). Die Anlagenart entscheidet über Kontingent und Satzstaffel; eine leere
    Zeichenkette ist weder „nicht gepflegt" noch eine Wahl. **Entschieden 22.09.2026 (Anwender,
    nach Empfehlung): ein DML-Schritt setzt die leere Zeichenkette auf NULL; NULL heißt „nicht
    gepflegt" — der Kern bucht dann keinen KWKG-Zuschlag und meldet es als Kohärenzzeile
    „Anlagenart fehlt", der Dialog zeigt „bitte wählen". Ein geratener Wert würde Kontingent und
    Satzstaffel setzen, die niemand eingegeben hat. Umsetzung mit E7** (Schemaschritt, Nummer bei
    der Umsetzung; Projekte 1032 und 1043 der Testdatenbank, Live-Datenbank vorher prüfen).
31. ~~**`Nachweis_Json` ist in 0 von 78 Ergebniszeilen belegt.**~~ — **erledigt mit E5 (#434):** Die
    Kennzeichnung ist gebaut — `WirtschaftlichkeitErgebnis.OhneNachweis` erkennt eine Ergebniszeile
    ohne Umschlag, und sie trägt „Nachweis liegt mit der nächsten Rechnung vor"
    (`WIRT_NACHWEIS_NAECHSTE_RECHNUNG`) unter den Annahmen der Seite, in Block 5 der Darstellung
    „ValERI-Bewertung" und in Wort- und Tabellenbericht; ein frisch gebuchter Lauf trägt den Umschlag
    und keine Kennzeichnung, auch nach dem Neuladen. Die Persistenz der Nachweise (B7P, Punkt 9b) war
    gebaut, aber kein Bestandsergebnis trug den Umschlag: Er entsteht erst beim nächsten Rechenlauf.
    **Entschieden 22.09.2026 (Anwender, nach Empfehlung): kein Nachziehlauf** — er würde 78
    Bestandsergebnisse mit den heutigen Rechenwegen neu rechnen und Zahlen ändern, die der Anwender
    bereits gesehen hat.
32. **Die vermiedene Bezugsmenge führt keinen PV-Eigenverbrauch** (Befund aus E4/3, Frage U6‑Q1).
    `StromMatrix.Baue` bildet „Bedarf ohne Anlage" als Strombedarf abzüglich PV-Eigennutzung;
    `VermiedenMengeMWh` ist damit allein der KWK-Eigenverbrauch, und der Verteilschlüssel der
    Erlösrubrik (V‑4) bringt nur das Blockheizkraftwerk ein — der vermiedene Bezug der Photovoltaik
    bleibt seine eigene Ausweiszeile im Block Photovoltaik. Das Mockup-Beispiel rechnet dagegen
    „ohne jede Eigenerzeugung" (1.179,7 = 1.094,2 + 85,5 MWh). **Entschieden 22.09.2026 (Anwender, nach Empfehlung): ohne jede Eigenerzeugung.**
    Bezugsgröße des Ausweises ist der Strombedarf des Projekts ohne jede Eigenerzeugung; die
    vermiedene Menge führt KWK- und PV-Eigenverbrauch, die § 9b-Korrektur greift auf beide, der
    Verteilschlüssel der Erlösrubrik bringt beide Anlagen ein. Der KWK-Eigenanteil (min-Regel auf
    den Bedarf nach Abzug der Photovoltaik) bleibt unverändert, ebenso der Kapitalwert (er rechnet
    mit dem tatsächlichen Restbezug). Betroffen sind die gespeicherten Ausweisspalten
    `VermiedenArbeit`, `VermiedenLeistung`, `VermiedenGesamt` und `VermiedenEntlastung9b` sowie der
    projektweite Leistungsanteil (Lastbild ohne Photovoltaik). Umsetzung mit E7: A/B-Nachweis über
    die dreizehn Basisprojekte, Anker 316.159,6 €/a am Beispielprojekt (293.245,6 + 22.914,0).

### 5.2 E1 — Nachweisfundament (#380)

Protokoll [`E1_Nachweisfundament_Wirtschaftlichkeit_Protokoll.md`](E1_Nachweisfundament_Wirtschaftlichkeit_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E1 Nachweisfundament** (#380) | Das Nachweisfundament dieses Feldes: `WirtschaftlichkeitAnkerTests` (9 Anker, § 6.2), `SteuerGutschriftRechnerTests` (39), `EegSatzRechnerTests` (49), `PvErloesRechnerEegTests` (23, Befund V‑2 gepinnt), `BerichtBlattstrukturWacheTests` (5), `WirtZeileFormatWacheTests` (4); **Kaskadenrunde 2** (`InvestKaskade`) in zwei Phasen wie Runde 3, damit reihenfolgeunabhängig | **keine** — A/B über 25 Projekte × 3 Szenarien zeilenweise identisch; Referenzlauf unverändert |

*§ 6.3, aus Etappe E1:*

**Aus Etappe E1 (#380) — geschlossen**

*Ohne eigene Nummer, weil der Punkt erst im Umsetzungsplan des Analysepapiers entstand:* **R4
Kaskadenrunde 2.** Runde 2 der Drei-Runden-Kaskade (§ 3.2) hing an der Reihenfolge der Zeilen.
`InvestKaskade` fährt sie seit #380 in zwei Phasen wie Runde 3 und ist damit
reihenfolgeunabhängig; der A/B-Nachweis über 25 Projekte × 3 Szenarien ist zeilenweise identisch
(`InvestKaskadeTests`, 25 Fälle).

### 5.3 DL‑2e — Knopfleisten der beiden Kostendialoge (#390)

Statuszeile #390; das Konzept dazu:
[`Konzept_Knopfleisten_Administration_EPOS-Plan.md`](../../../aktuell/Konzept_Knopfleisten_Administration_EPOS-Plan.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **DL‑2e Knopfleisten** (#390) | Die Fußleisten der beiden Kostendialoge dieses Papiers auf den Hausstil: `KostenKomponenteDialog` und `EnergietraegerDialog` tragen die `SpeichernLeiste` mit Status · Speichern · Abbrechen · OK; die vier Rasterknöpfe des Reiters „Kosten" (§ 2.8) bleiben Blattleiste; „Bezeichnung speichern" der Energieträgerkarte entfällt zugunsten **eines** Schreibwegs | keine — reine Bedienung |

### 5.4 E2 — kleine Kernkorrekturen (#405)

Protokoll [`E2_Kleine_Kernkorrekturen_Wirtschaftlichkeit_Protokoll.md`](E2_Kleine_Kernkorrekturen_Wirtschaftlichkeit_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E2 Kleine Kernkorrekturen** (#405, 20.09.2026) | Ausweis und Bedienung ohne Rechenwirkung: CO₂-Doppelansatz und Strommix-Rückfall als Kohärenzzeilen (§ 3.9), Kohärenzzeilen im **einen** Zeilenkatalog und damit in Rubrik, Wort- und Excelbericht; Erlaubnisschwelle StromStG mit Leser; PV-Reihe als eigene Spalte der Mehrjahrestabelle; Bezugsmenge aus dem `BemessungKatalog`; zeichengenauer Steuerwertvergleich; Kapitalwertdifferenz über dem Nettobarwert; Bandbreite mit Spalte „Spanne" und Referenzzeile in Wort- und Excelbericht, Empfehlungssatz und Δ-Fußzeile mit der gewählten Referenz, Zeitraumhinweis auch im Excel-Blatt; dazu die Dialogkorrekturen der Mockup-Prüfung (Löschrückfragen mit Vorgabe „Nein", Gesetzesparameter im PV-Zweig, Sprungknopf auf den OK-Weg, OK-Weg nur bei Änderung, Hausschlüssel der Standardknöpfe, `help_mapping`-Anker) | **keine** — die vier Ankertests unverändert, Referenzlauf der fünf CI-Projekte PASS |

*§ 6.3, aus Etappe E2 — Nr. 25 bis 29 (29 steht als Regel weiter im Konzept):*

**Aus Etappe E2 (#405, 20.09.2026) — geschlossen**

25. ~~**CO₂ doppelt gebucht** (R5): aktiver CO₂-Bestandteil im Arbeitspreis und gebuchte
    BEHG-Reihe nebeneinander blieben unbemerkt.~~ — **erledigt**: neuer Fall der Kohärenzprüfung,
    WARNUNG mit dem doppelt gebuchten Jahresbetrag (§ 3.9). Der Rechenweg bleibt, wie er ist.
26. ~~**Kohärenzzeilen erreichen nur die Seite** (R6); der Strommix-Rückfall war ein Laufhinweis
    ohne seinen Wert.~~ — **erledigt**: Die Zeilen stehen im **einen** Zeilenkatalog
    (`WirtschaftlichkeitZeilen`) und damit in Rubrik, Wort- und Excelbericht; der Rückfall nennt
    seine 435 g CO₂/kWh.
27. ~~**B-7** (`MengenEinheit` beschriftet jede Bezugsmenge mit „€") · **I-5** (uneinheitliche
    Vergleichsstrenge) · **S-3** („(0 kW)" am Kessel) · **S-5** (Erlaubnisschwelle ohne Leser) ·
    **V-3** (PV-Reihe ohne Spalte).~~ — **alle fünf erledigt**, je ohne Rechenwirkung; Einzelheiten
    in der Befundtafel des § 4.
28. ~~**G7** (Zeitraumhinweis fehlt im Excel-Blatt) · **G8** (Bandbreite ohne Spalte „Spanne" und
    ohne Referenzzeile) · **G9** (`WIRT_EMPF_KEINE` nennt „Stammprojekt" statt der gewählten
    Referenz).~~ — **alle drei erledigt**; Word und Excel führen dieselbe Bandbreitentafel, und
    Empfehlungssatz wie Δ-Fußzeile nennen die Referenz beim Namen.
29. **Hi/Ho am CO₂-Grenzwert** (R11) — **entschieden 22.09.2026 (Anwender): „es gilt immer der
    Brennwert."** Der Katalog führt zum Erdgas einen heizwert- und einen brennwertbezogenen
    EBeV-Faktor (200,9 bzw. 181,4 g/kWh) samt Umrechnung; gelesen wird heute der Schlüssel der
    Anlage, die beiden Ho-Zeilen haben keinen Leser. Der Grenzwert 270 g/kWh des § 2 StromStG wird
    **brennwertbezogen** geprüft: Der Zähler nimmt den Ho-Faktor (bei heizwertbezogenem Katalogwert
    die Umrechnung Hi → Ho), sonst fiele er rund 10 % zu hoch aus und die Befreiung entfiele in
    Grenzfällen zu Unrecht. Umsetzung mit **E7** (Rechenwirkung: der gebuchte Befreiungsbetrag und
    damit der Kapitalwert ändern sich in Grenzfällen; A/B-Nachweis, die Pinnung in
    `KleinkorrekturenE2Tests` wird auf den Brennwert umgestellt). Die Frage entstand erst mit E2
    und war vom Entscheid „nach Empfehlung" des 20.09.2026 nicht gedeckt.

### 5.5 Die Entscheide vom 20.09.2026: A1–A20 und Q1–Q25 nach Empfehlung

Mit dem Auftrag vom 20.09.2026 („Entscheidung nach Empfehlung", Statuszeile #405) sind alle zwanzig
Entscheide des Analysepapiers und alle fünfundzwanzig Fragen der Mockup-Prüfung nach Empfehlung
entschieden; ihr Wortlaut steht im Register (R‑A, R‑Q). Hier die Stellen, an denen das Konzept bis
zum Schnitt davon erzählte.

*§ 2.11.4, Z. 764–769 — der Entscheidweg zu A5:*

**Entscheid A5 (20.09.2026, nach Empfehlung): V-E rechnet die Degradation nicht ein.** V-E nannte die
**Degradation (V-G2)** als Teil der Etappe; das Szenarienkonzept führt dieselbe Sache als `G3` mit dem
Entscheid vom 09.09.2026 „nicht umsetzen". Der Entscheid vom 20.09.2026 löst den Widerspruch zugunsten
des Szenarienkonzepts auf: **V-E wird ohne Degradation geplant**, die Vereinfachung bleibt offengelegt
(Szenarienkonzept § 9.4). Die Nummerierung `V-G2` ↔ `G3` bleibt, wie sie ist — die Übersetzungstafel
steht am Anfang von § 2.11.2.

*§ 2.13, Z. 1069–1077 — der Entscheidweg zu A1:*

**Entscheid A1 (20.09.2026, nach Empfehlung): der Umzug kommt vor der Ergebnisansicht.** Rechenaufruf
und Datenseite werden als eigene Welle **E3 Plattform** aus der Windows-Schale geholt, nicht erst mit
der Ergebnisansicht — sonst entsteht jedes Stück dieser Ansicht ein zweites Mal nur für Windows.
**Das Muster liegt seit #428 (KI‑F8) vor:** Vier Hüllen (`KlimadatenHuelle`, `ProjektKopieHuelle`,
`PeakShavingHuelle`, `StromganglinieAdminHuelle`) sind plattformfrei nach `EPOS.UI.Daten` gewandert,
während Windows je Hülle einen **Fenster-Adapter** behielt (`KlimadatenFenster`, `ProjektKopieFenster`,
`PeakShavingFenster`, `StromganglinieAdminFenster`); die Wurzel öffnet dieselben Masken auf iOS über
Nähte in `IProjektQuelle`. Nach diesem Muster sind die Hüllen dieses Papiers umgezogen — allen voran
`KostenKomponenteHuelle` (Q14, E3 Schritt 5). **Stand: umgesetzt #431** (Merge `2cfee66b`).

*§ 7, Z. 2749–2764 — die Tafel „Entscheide vor der nächsten Codeetappe":*

Kennungen nach dem Analysepapier vom 19.09.2026, § 4. **Anwenderentscheid 20.09.2026: alle Entscheide
A1–A20 gelten nach der Empfehlung der Papiere.** Damit blockiert keiner dieser Entscheide noch eine
Etappe; ausgeführt sind sie damit nicht.

| # | Entscheid (20.09.2026, nach Empfehlung) | Stand |
|---|---|---|
| **A1** | Rechenaufruf und Datenseite kommen **vor** der Ergebnisansicht aus der Windows-Schale — als eigene Welle **E3 Plattform**, in der gemessenen Reihenfolge; sonst entsteht jedes Stück der Ergebnisansicht ein zweites Mal nur für Windows (betrifft § 2.13 (5), die plattformfreie Hülle) | entschieden **und gebaut** — E3 ist umgesetzt (**#431**) |
| **A2** | Befund **K-1**: Vor der Umsetzung wird gemessen, ob die modulscharfe Nutzwärme vorliegt; sonst Aufteilung nach P_el mit Herleitungszeile (§ 3.6, § 4) | entschieden, nicht gebaut — E7 |
| **A5** | **Degradation: V-E rechnet sie nicht ein.** Der Entscheid „G3 nicht umsetzen" des Szenarienkonzepts (§ 2.11.2, dort V‑G2) gilt; V-E (§ 2.11.4) wird **ohne Degradation** geplant | entschieden — der Widerspruch zwischen beiden Papieren ist aufgelöst |
| **A11** | Nachweis der Wirtschaftlichkeitsgrößen: **Ankertests zuerst**; die Erweiterung des Referenzlaufs ist eine Frage für die nächste Basis (§ 6.2, § 6.3 Nr. 21) | entschieden **und gebaut** mit E1 (#380) |
| **A13** | Schnitt dieses Papiers in drei Papiere (gültiger Stand · Entscheidungsregister · Protokoll der Entscheidwege) — **ja**, vor der ersten Codeetappe; Papierpflege ohne Entscheid zuerst | entschieden, **nicht ausgeführt** — E0 (#379) hat nur die Pflege gemacht; Anwender 22.09.2026 nach Empfehlung: **Ausführung nach E5, vor E6**, wenn E4 und E5 die § 2.6 und § 2.13 umgebaut haben — mit E5 (#434) ist das geschehen, der Schnitt ist die nächste Etappe |

Die übrigen Entscheide A3, A4, A6–A10, A12, A14–A20 stehen mit ihrer Empfehlung und ihrer Etappe im
Analysepapier § 4 und § 5; sie gelten seit dem 20.09.2026 ebenso nach Empfehlung. **Nicht** vom
Entscheid gedeckt sind die Punkte 29, 30 und 31 des § 6.3 — sie entstanden erst mit E0 und E2 und
tragen keine Empfehlung.

---

## 6 Die Wellen vom 22.09.2026

### 6.1 Wiederaufnahme und E3 — Plattform (#431)

*§ 7, Z. 2743–2745:*

**Wiederaufnahme 22.09.2026.** Die Umsetzung war am 20.09.2026 zurückgestellt (Statusdatei, Block
„Nach #405" (f)); der Anwender hat sie am 22.09.2026 mit dem Auftrag wieder aufgenommen, das Mockup
`../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html` umzusetzen.

Protokoll [`E3_Plattform_Wirtschaftlichkeit_Protokoll.md`](E3_Plattform_Wirtschaftlichkeit_Protokoll.md).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E3 Plattform** (#431, Merge `2cfee66b`) | Acht Schritte: vier nahtlose Hüllen, PV-Dateiwahl über `Dienste.Datei`, `KostenSeiteGaben`/`WirtschaftlichkeitSeiteGaben` und der Rechenaufruf (`BerichtsDatenSammler`) nach `EPOS.Kern`/`EPOS.UI.Daten` verschoben, `KostenKomponenteHuelle` und `GesetzeskatalogHuelle` mit Fenster-Adapter, Tarif-Sprünge als Überlagerung statt Zweitfenster, `IosProjektQuelle.BerichteKostenGaben` beliefert alle vier Seiten, Whitelist 18 → 21 Schlüssel | **keine** — Referenzlauf 13/13 Projekte, 3 882 737 Werte innerhalb der Toleranz (Teil a und Teil b) |

### 6.2 E4 — Erlösrubrik und Steuerzeilen (#432)

Protokoll [`E4_Erloesrubrik_Steuerzeilen_Protokoll.md`](E4_Erloesrubrik_Steuerzeilen_Protokoll.md);
die Punkte 9a, 9d und 9i des § 6.3 stehen in § 3.3 und § 3.7, die Anwenderfragen U6‑Q1…Q3 und
U7‑Q1/Q2 im Register (R‑E4).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E4 Erlösrubrik und Steuerzeilen** (#432, Merge `1190622c`) | U7: `SteuerErgebnis` mit § 53/§ 53a und § 54 als getrennten Beträgen samt Sockel und `EnergiesteuerNachweis`, zwei Rubrikzeilen mit Herleitung, Nachweisumschlag Fassung 4; 9d: `SteuerPosition` und `PositionsGruende`, je Geldzeile eine Herleitungszeile mit Herleitung oder Grund des Laufs, Fassung 5; U6: `WirtZeile.Komponente`, Komponentenköpfe und Zwischensummen in A und B, Block „projektweit", `VermiedenAnlageNachweis.Verteile()` (Näherung V‑4, ausgewiesen; Leistungsanteil projektweit), Fassung 6; 22 Ressourcenschlüssel de/en, kein Schemaschritt; Blattstruktur-Wache nachgezogen | **keine** — Referenzlauf 13/13 Projekte, 3 882 737 Werte, 357 CSV byte-gleich; die Rubrik steht nicht im Referenzexport (A11), Nachweis sind die Ankertests (Blocksumme A 14.575 €/a unverändert, Zahlenprobe 316.159,6 €/a) |

### 6.3 E5 — Ergebnisansicht und V‑A (#434)

Protokoll [`E5_Ergebnisansicht_VA_Protokoll.md`](E5_Ergebnisansicht_VA_Protokoll.md); Punkt 31 des
§ 6.3 steht in § 5.1, die Fragen E5‑Q1…Q7 und die vier Fragen aus Teil b im Register (R‑E5).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E5 Ergebnisansicht und V‑A** (#434, Merge `deba5e57`) | Teil a: V‑A im Kern — „nachrichtlich" an Amortisation und Zinsfuß (`WirtschaftlichkeitZeilen.IstNachrichtlich`, E5‑Q3), Vorzeichenwechsel der Differenzreihe (`KapitalwertRechner.Vorzeichenwechsel`, Nachweisumschlag Fassung 7; mehr als einer ⇒ Warnung, keiner ⇒ „kein Zinsfuß bestimmbar" statt Abbruch), Deklarationen (`ValeriAusweis.Deklarationen()`), Steigungsspalte der Sensitivität; Einstufung je Version (`WirtschaftlichkeitBandbreite`), `WirtschaftlichkeitCtrl.BerechneBandbreite` (drei Szenarioläufe ohne Speichern), `WirtschaftlichkeitBewertung` als ein Modell für Seite und Berichte, Nutzungsdauer-Hinweis im Kern (`NutzungsdauerHinweisCtrl`, Teil von U39), Nr. 31 (`OhneNachweis`), Q16 („— ‹Grund›", Excel leer), Kennzahlen in der Reihenfolge des Mockups; Teil b: Umschalter „Kennzahlen / ValERI-Bewertung" als Sitzungswahl (K8/V‑1, U2), vier Abschnitte, Empfehlungskarten je Version (U5), Bandbreite nebeneinander (U4), Sensitivitätstafel mit Steigung, Hinweistext `WIRT_SZEN_HINWEIS` (U10, A14), Darstellung „ValERI-Bewertung" mit den Blöcken 1, 3, 4 und 5, „Bericht erzeugen" über den bestehenden Berichtsweg (U44); Wort- und Tabellenbericht lesen `BerichtsDaten.Bewertung`, Sicht 2 rechnet im Bericht gegen A (E5‑Q6); 52 Ressourcenschlüssel de/en neu, drei geändert, einer gestrichen; kein Schemaschritt | **keine** — Referenzlauf 13/13 Projekte, 3 882 737 Werte, 357 CSV byte-gleich; der gebuchte Lauf rechnet unverändert gegen die Referenz der Gruppe, Bandbreite und Sicht 2 rechnen ohne Speichern; die Wirtschaftlichkeit steht nicht im Referenzexport (A11), Nachweis sind die Kern- und bunit-Tests (Gate: 10 624 Tests grün) |

### 6.4 Die Entscheide vom 22.09.2026

Am 22.09.2026 hat der Anwender die Reste der Mockup-Prüfung entschieden (Q9, Q11, Q15, Q18, Q20, Q22,
Q23), die drei Punkte, die der Entscheid vom 20.09.2026 nicht deckte (Nr. 29, 30, 31), die Frage
U6‑Q1 (Nr. 32), die Fragen aus E4 und E5, die Abnahme des Mockups und die Wiederaufnahme — alle im
Register (R‑Q, R‑NR, R‑E4, R‑E5, R‑EZ). Dazu kam der Entscheid, der die Zahlenprobe gegen die
Altanwendung entfallen ließ:

*§ 6.3 Nr. 20:*

20. ~~Zahlenprobe gegen die Altanwendung (A8, ≡ B9)~~ — **entfällt, Anwenderentscheid 22.09.2026:
    „BHKW-Plan-Mappen: nicht relevant."** Die Inventarisierung der Mappen bleibt als Geschichte in
    [`ueberholt/Protokolle/Reporting/Analyse_Altanwendung_BHKW-Plan.md`](Analyse_Altanwendung_BHKW-Plan.md);
    Entscheid A17 (Referenzmappe, `_kap`) ist damit gegenstandslos, Etappe E11 des Analysepapiers
    entfällt. Der Nachweis der Wirtschaftlichkeitsgrößen läuft über die Anker aus E1 (§ 6.2) und
    die A/B-Nachweise der rechenwirksamen Etappen (E7, E9, E10)

---

## 7 Weitere Stücke des Konzepts vor dem Schnitt

### 7.1 § 6.3 — erledigt aus anderen Reihen

*§ 6.3, fachlich und technisch — Nr. 12 (W5‑B‑8 des Szenarienkonzepts), Nr. 14 und Nr. 17 (B7P):*

12. ~~`InvestSummeFuer` auf die abgeleitete Kaskadensumme umbauen (B-5)~~ — **erledigt mit
    W5‑B‑8**: Basis ist `InvestKaskade.Summen` (§ 3.4)

14. ~~Reduzierter Stromsteuersatz bleibt Konstante bis zur Katalog-Nachpflege~~ — **überholt**:
    Der Katalog führt `STROMST_REDUZIERT_SATZ` (`GesetzKatalog.cs:1156`); die Konstante in
    `StrompreisZerlegungModel` ist nur noch wertgleiche **Rückfallebene** (`:82–89`). Offen bleibt
    allein, dass **keine Wache Konstante gegen Katalog** hält (§ 6.5)

17. ~~Kohärenzzeilen nicht persistiert~~ — erledigt mit B7P, sie reisen im Nachweisumschlag mit ·
    Fall 4 ohne Katalogsatz bleibt still

### 7.2 § 6.4 — die gestrichenen Fallstricke

**Überholt und deshalb gestrichen:** „Keine `.cs` unterhalb von `WindowsFormsApplication1\`
(CS0017); Harnesse nach `dev\`" — den Ordner `dev\` gibt es nicht. „Build nur über das MSBuild von
Visual Studio, x64 — `dotnet build` scheitert an COM" — `CLAUDE.md` nennt
`dotnet build WP-Plan.sln -c Debug -p:Platform=x64` ausdrücklich als den Weg (SDK 10.0.400).

### 7.3 § 7 — die Reihenfolge vor dem Schnitt

*§ 7, Z. 2726–2741 (die Wiederaufnahme, Z. 2743–2745, steht in § 6.1):*

**B5, B6 und B7 sind gelaufen und stehen mit ihrer Ergebniswirkung in § 6.1.** Offen sind nur noch
die beiden Etappen dieser Tafel:

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B8** | Die verbliebenen Befunde: **S-2** (kein projektweites Doppelentlastungsverbot) und **B-6** (geschluckte Fehler, `catch {}` ⇒ still 0). Der **PV-Teil von V-3** und **I-5** sind mit **E2 (#405)** erledigt. S‑2 ist mit **A3** entschieden (20.09.2026 nach Empfehlung): **Sperre mit Begründungszeile**, nicht Warnung. Beide Punkte laufen in **E7** des Etappenplans mit | **ja** bei S-2 — jeder Punkt einzeln mit A/B-Nachweis; B-6 ist Robustheit |
| **B9** ≡ A8 | Zahlenprobe gegen die Altanwendung — **entfällt (Anwenderentscheid 22.09.2026: „BHKW-Plan-Mappen: nicht relevant")**. Die Inventur der Mappen ([`Analyse_Altanwendung_BHKW-Plan.md`](Analyse_Altanwendung_BHKW-Plan.md)) und die neun Abweichungen der Altanwendung (§ 5 der [Grundlagen](../../../aktuell/Grundlagen_KWKG_Energiesteuer_Stromsteuer.md)) bleiben als Geschichte stehen; der Nachweis der Wirtschaftlichkeitsgrößen läuft über die Anker aus E1 (§ 6.2) und die A/B-Nachweise der rechenwirksamen Etappen | entfällt |

Zur Einordnung: **I-1, I-2, I-3, B-1/N1, N3, B-5 und S-6 sind erledigt** (§ 4); die frühere
Reihenfolgebegründung („B5 bleibt ergebnisneutral, die erste gewollte Ergebnisänderung kommt mit
B6") ist mit B5, B6, B7, BK1, BK1a, BK1b, VG, VV und der Hilfsstrom-Umstellung überholt. Die
Reihenfolge der **heute** offenen Etappen — V-A…V-E (§ 2.11.4), U39 (§ 2.13 (3)),
Erlösrubrik-Ausbau (§ 6.3 9a/9d/9i), ND-S3, B8, B9 — steht im Etappenplan E0–E12 des
Analysepapiers [`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md) § 5.
Davon sind **E0 (#379), E1 (#380), E2 (#405), E3 (#431), E4 (#432) und E5 (#434)** gebaut; als
Nächstes kommt der **A13-Schnitt** dieses Papiers, dann **E6 Verlauf mit drei Szenarien**.

### 7.4 Die Entscheidtafeln vor dem Schnitt

Die geltende Fassung dieser Entscheide steht im Register (R‑V, R‑VG, R‑VV, R‑K, R‑D); hier stehen die
Tafeln in der Form, die sie im Konzept hatten.

*§ 2.11.4, Z. 771–776:*

| Nr. | Entscheidungsfrage | Empfehlung |
|---|---|---|
| **V-1** | Fünf Blöcke als Aufklappabschnitte unter der Vergleichstabelle oder als zweite Ansicht mit Umschalter „Kennzahlen / ValERI-Bewertung"? | Umschalter — die Seite ist schon voll. **Entschieden 18.09.2026 nach Empfehlung** (zusammen mit K8, § 2.7): Umschalter im Kopf der Seite — **gebaut #434** (Sitzungswahl; die Blöcke 1, 3, 4, 5 in der Darstellung „ValERI-Bewertung") |
| **V-2** | XLSX-Formelexport: nur das ValERI-Blatt oder den ganzen Bericht formelbasiert? | Empfehlung war: nur das ValERI-Blatt (drei Szenariotabellen + Parameterblock), der übrige Bericht bleibt Werte. **Gekippt 18.09.2026 durch den Entscheid zu V-G10: der ganze Bericht, soweit ableitbar** — was ableitbar ist und was Wert bleibt, steht in § 2.11.6 |
| **V-3** | IZF/Amortisation von den Kacheln nehmen oder mit „nachrichtlich"-Label behalten? | behalten mit Label — Anwender kennen die Größen, die Norm verlangt nur die richtige Einordnung. **Gebaut #434:** Kacheln mit Label über den Empfehlungskarten |
| **V-4** | Szenario-Parametersätze (V-G5) sofort oder nach V-A–V-D? | **Umfang entschieden** (§ 2.11.5). **Zeitpunkt entschieden 18.09.2026 nach Empfehlung: danach**, einzige Etappe mit Rechenwirkung, eigener A/B-Nachweis — **mit Hinweistext** bis dahin (§ 2.11.7); der Hinweistext ist **gebaut #434** |

*§ 2.15, Z. 1314–1324:*

**Fragen mit Empfehlung — entschieden (Anwenderentscheid 18.09.2026: „VG‑Q1 bis VG‑Q7: Empfehlung"):**

| Frage | Empfehlung |
|---|---|
| **VG‑Q1** Setzt Sicht 2 A als Referenz des Rechenlaufs, oder ist die Paarwahl eine Anzeige über der Referenzrechnung? | **A als Referenz dieser Sicht**, ohne die Gruppenreferenz zu schreiben — Amortisation und Zinsfuß sind nicht linear, und es gibt nur einen Rechenweg für Differenzkennzahlen |
| **VG‑Q2** Beschriftung der Sicht 1: „gegen den Stamm" oder „gegen die Referenz"? | **„gegen die Referenz"**; die Erklärzeile nennt den Namen. „Stamm" wäre falsch, sobald § 2.9 eine Variante wählt |
| **VG‑Q3** Persistenz der Paarwahl? | **Sitzung**, in `Vergleichsauswahl` neben den Häkchen; keine Spalte |
| **VG‑Q4** Folgt der Bericht der Sicht, oder druckt er immer alle Stände? | **Er folgt der Sicht**, mit Deklarationszeile — so wie er den Häkchen folgt; wer alle Stände will, wählt Sicht 1 vor dem Druck |
| **VG‑Q5** Verlauf in Sicht 2: eine Differenzkurve B − A oder die zwei Kurven A und B gegen die Gruppenreferenz? | **eine Differenzkurve B − A** — ihr Nulldurchgang ist die Amortisation des Paars; mit A = Gruppenreferenz wäre die A-Kurve die Nulllinie |
| **VG‑Q6** ValERI-Bewertung in Sicht 2 erlaubt? | **ja**, mit der Deklaration „Vergleich zweier Maßnahmen · Unterlassensalternative der Gruppe: ‹Referenz›"; der Kapitalwert von B gegenüber A ist die Differenz zweier Kapitalwerte gegen dieselbe Unterlassensalternative |
| **VG‑Q7** Tauschknopf ⇄ zwischen den Listen? | **ja, klein** — er spart zwei Listenwahlen und macht die Vorzeichenregel sichtbar; kein Muss |

*§ 2.16, Z. 1488–1498:*

**Fragen mit Empfehlung — entschieden am 18.09.2026 („VV‑Q1 bis VV‑Q7: Empfehlung"):**

| Frage | Empfehlung |
|---|---|
| **VV‑Q1** Gilt dieselbe Regel für die BHKW-Vergütung? | **Ja, sinngemäß — und dort ist sie schon erfüllt:** der KWKG-Zuschlag ist anlagenscharf, also je Stand; der flache KWK-Einspeisesatz bleibt wie der flache PV-Satz in der Rahmenzeile je Gruppe. Keine Änderung am BHKW in dieser Etappe. Einheitliche Regel: *Vergütung folgt der Anlage bzw. dem Stand, Rahmensätze folgen der Gruppe* |
| **VV‑Q2** Einzelwerte übernehmbar oder nur der ganze Block? | **nur der Block** — die Felder bedingen einander (die Vermarktungsform bestimmt, welche Felder gelten; § 51 hängt an Inbetriebnahme und Leistung); ein Mischsatz ist keine gepflegte Vergütung |
| **VV‑Q3** Anzeige der Wahl im Kostendialog (Reiter Ertrag/Bonus) oder nur im Vergütungsdialog? | **an beiden Orten, gewählt an einem:** die Optionsgruppe sitzt im Reiter (dort ist die Variante im Blick, dort fragt der Anwender); der Vergütungsdialog zeigt die Herkunft als Hinweiszeile und bietet nur den Weg „eigene Werte". Der Reiter bleibt Anzeige plus Wahl, kein zweiter Rechenweg |
| **VV‑Q4** Bestandsvarianten ohne eigene Zeile bei aktiver Stammzeile: ergebnisneutral (eigene, inaktive Zeile) oder übernehmen (Rechenwirkung)? | **ergebnisneutral**, mit Kohärenzhinweis; der Anwender schaltet je Variante mit einem Klick auf „übernehmen". Ein Schemaschritt, der Zahlen ändert, ohne dass jemand gewählt hat, verletzte die Regel „Vorgabe ergebnisneutral" |
| **VV‑Q5** Auch der flache Satz `Einspeiseverguetung` der Rahmenzeile je Variante? | **nein** — er ist ein Rahmenparameter wie Zins und Zeitraum (R‑1) und der Rückfall, wenn kein Dialog aktiv ist; wer je Variante vergüten will, tut es im Dialog |
| **VV‑Q6** Was lädt „eigene Werte" vor: die Stammwerte oder die Vorbelegung des Controllers? | **die Stammwerte** — der Anwender will eine Abweichung von einer bekannten Basis, keinen leeren Satz; die Zeile trägt `GeaendertAm` |
| **VV‑Q7** Klappliste „Stammprojekt:" im Reiter: umbenennen? | **„Projekt:"** — sie führt alle Projekte; im Projektmodus des Kostendialogs ist das geöffnete Projekt vorgewählt, und die Liste entfällt |

*§ 5, Z. 2271–2303:*

| # | Frage | Stand |
|---|---|---|
| K1 | Feld „Deckung je Modul" | **entschieden: kein Feld** — die Befreiung ist bilanziell |
| **K2** | Hilfsenergie-Basis je Anlage: **Weg B — „% des Endenergiebedarfs" der Anlage**, bewertet mit dem **eigenen Trägerpreis der Anlage**; Wege A und C nur in der Kostenposition | **erledigt mit #365/#366** (Schemaschritt 94): Vorlage „Standard" und Dialog benennen die Basis, Feldbeschriftung „Hilfsenergieanteil [% des Endenergiebedarfs]" (§ 2.2) |
| **K3** | Modusfeld § 9 Nr. 3 — Spalte kommt erst mit B6 | **erledigt mit B6** (Statuszeile #328, anderer Rechner): Schemaschritt 88, Feld offen, Vorgabe AUSWEIS — der Entscheid vom 18.09.2026 („Ausweis") ist damit umgesetzt, keine zweite Baustelle |
| K4 | Tabellenspalte „Brennstoff" ohne Leseweg | kleiner Leser `CarrierId` → Name in B5 |
| K5 | Jahresnutzungsgrad bleibt Projektgröße | als Projektfeld zeigen |
| K6 | WP-Hilfsenergie: Spalte gilt formal für alle, Leser nur BHKW und Kessel | B5 zeigt das Feld nur bei BHKW |
| **K7** | Schreibweg der drei B3-Spalten fehlt (`KwkgAnlagenCtrl.Speichere` = 8 Spalten) | **erledigt**: `Speichere(g, mitSteuerangaben)` schreibt 8 + 4 Spalten (`KwkgAnlagenCtrl.cs:283–299`) |
| **K8** | Fußleiste voll — ein achter Knopf läge bei x = −50. **Die Frage ist gegenstandslos:** die Razor-Fußleiste führt fünf Knöpfe (§ 2.7) | **entschieden 18.09.2026** (nach Empfehlung, = V-1): Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf der Seite statt eines weiteren Knopfes; der Knopf „Verlauf…" entfällt mit der Ergebnisansicht (§ 2.7, § 2.13) |
| K10 | Hilfsenergie-Bemessung doppelt: Seed gegen Altkatalog | **erledigt**: Altarten nur noch zur Anzeige, abgelöst von `PROZENT_ENDENERGIEKOSTEN` |
| K11 | `Views\Wirtschaftlichkeit` unlokalisiert (63 Literale) | **erledigt mit B6**: 0 nackte Anzeigetexte, eigene Wache |

Dazu die Entscheidungen zur Darstellung (30.08.2026):

| # | Frage | Entscheidung |
|---|---|---|
| **D-1** | Emissionsspalte der Energieträgertabelle | **eine** Spalte, Kopf und Inhalt nach `Emission_Berechnungsmodus`; SO₂/NOx entfallen aus dieser Übersicht (§ 2.5) |
| **E-1** | Modus `CO2E`, wenn außer CO₂ nichts gepflegt ist bzw. der Wert schon ein Äquivalent ist | **Wert zeigen, Umstand im Tooltip benennen** — drei Herleitungsfälle, kein stiller Rückfall auf „CO₂" (§ 2.5) |
| **D-2** | Erlösdarstellung | eigene Rubrik in zwei Blöcken, getrennte Summen; Block B (Ausweis) wird nicht addiert (§ 2.6) |
| **D-3** | Referenz der Differenzrechnung | **wählbares Vergleichsprojekt** je Gruppe (Stamm oder Variante), Vorgabe Stamm = ergebnisneutral; `ID_Referenzprojekt` an der Rahmenzeile; die Referenz ist die Unterlassensalternative der DIN EN 17463 (§ 2.9, Anforderung 31.08.2026) |

Zum Energieträger-Dialog kommen die Entscheide vom 18.09.2026 („Der Dialog im Mockup ist sehr
übersichtlich und besser als der vorhandene Dialog Energieträgerverwaltung … und verbessert
werden wie im Mockup"):

| # | Frage | Entscheidung |
|---|---|---|
| **ET-D-1** | In welcher Einheit stehen die Preisbestandteile? | **(a) in der Abrechnungseinheit** (€/m³, €/l, €/t) an der Anzeigekante; gerechnet, gespeichert und geprüft wird weiter in ct/kWh. Ohne Heizwert bleibt ct/kWh mit Hinweis (§ 3.5) |
| **ET-D-2** | Was zeigt der Emissionsblock der Trägerkarte? | **(a) die Arten DIESES Trägers** samt Bilanzierungsmethode als Klappliste, Summenzeile und Fußnote — **kein Primärenergiefaktor, keine Trägerübersicht**. Der Modus ist Projektsache und im Katalogkontext nur lesbar (Entscheide D-1/E-1 bleiben) |
| **ET-D-3** | Was bietet die Preisbasis an? | **(a) genau zwei Einträge** — Abrechnungseinheit und kWh, Faktor = Heizwert — **umgesetzt**. Die Umrechnungsregeln werden zum zugeklappten **Prüfblock** „Einheiten und Umrechnung". **Offener Rest (U32):** Der Kartenzustand fällt weiterhin auf `ID_Umrechnung = -1` zurück |
| **UR-1** | Die Preisbasis rechnete mit dem `factor` einer Umrechnungsregel statt mit dem Heizwert (Anwenderfoto: 0,07 €/kWh eingegeben, 0,04 €/Nm³ gespeichert, Formelzeile 0,0033 €/kWh) | **behoben mit ET-D**: `EnergietraegerPreisCtrl.Preisbasen` liefert den Heizwert als Faktor; `Umrechnungen` liest nur noch aktive Regeln. **Bestandsprojekte werden nicht stillschweigend umgerechnet** — erkennbar an der Formelzeile der Trägerkarte, die den Preis je kWh nennt; wer einen falsch gespeicherten Arbeitspreis hat, gibt ihn neu ein |
| **E1** | Welchen Energieträger bekommt ein **Elektroheizkessel** („Für Elektroheizkessel muss Strom als Energieträger auswählbar und zuzuordnen sein")? | **Er gehört zur elektrischen Welt wie Wärmepumpe und Heizstab.** Ein Heizkessel, dessen Gerät `Tab_Heizkessel.Brennstoff` = 13 führt, lässt nur die Stromfamilie zu, erscheint in der Komponentenliste der Energieträgerverwaltung mit dem **projektweiten Stromträger** als Vorgabe und ist dort wie eine Wärmepumpe zuzuordnen. Nicht über den Brennstoffweg: Der Katalog führt mehrere Träger auf Brennstoff 13, und die Auswahl unter ihnen könnte einen anderen treffen als die Wärmepumpe desselben Projekts. **Keinen eigenen Stromtarif je Verbraucher** (18.09.2026): Es gibt **einen Stromträger je Projekt**. Welcher es ist, wählt die Zuordnung an den Anlagen in der Rangfolge Wärmepumpe → Heizstab → Elektrokessel → Speicher → PV (`ProjektEnergietraegerCtrl.StromTraegerDerAnlagen`); ohne Wahl gilt die Vorgabe des Projekts. Bepreist wird der Netzbezug einmal — ein Heizstromtarif je Anlage entsteht daraus nicht |

*§ 5, Z. 2323–2328:*

Aus dem Energieträger-Umfeld kommt eine weitere Entscheidung desselben Tages hinzu. Sie betrifft
die Wirtschaftlichkeitsrechnung nicht, wohl aber den gemeinsamen Schema-Nummernraum:

| # | Frage | Entscheidung |
|---|---|---|
| **U-1** | Einheitenbruch `Tab_Brennstoff_Stamm.Einheit` ↔ `energy_conversion` (BK3 § 6 Nr. 4): die Identitätsregel-Ableitung liefert für 9 von 25 Brennstoffen `-1` | **entschieden 30.08.2026 — Weg (a)**: Der Stammtext der fünf Gase (Brennstoffe 1, 2, 3, 14, 25) wird „m³" → „Nm³" gezogen (Muster Schritt 26a; Leitentscheidung L4 auf die Stammseite fortgeschrieben). Die Wege (b) Identitätsregel-Saat und (c) `billing_unit`-Ableitung sind **nicht beauftragt** |

---

## 8 Nach dem Schnitt — Fortschreibung ab E6 (#436)

*Was mit den Statuszeilen ab #436 aus dem gültigen Stand des Konzepts hierher gewandert ist und welche
Sätze dort berichtigt wurden. Die abgesetzten Blöcke dieses Abschnitts sind Wortlaut des Konzepts vor der
jeweiligen Statuszeile — in § 8.1 und § 8.2 **vor #436** (Stand `fa80786a`), in § 8.3 und § 8.4 **vor
#437** (Stand `befec9dc`), in § 8.5 und § 8.6 **vor #439** (Stand `954d4dcc`), in § 8.7 und § 8.8 **vor
#440** (Stand `ea8e2a12`), in § 8.9 und § 8.10 **vor #446** (Stand `41764ab0`), in § 8.11 und § 8.12 **vor #452**
(Stand `9c7a0023`), in § 8.13 und § 8.14 **vor #454** (Stand `485052c6`), in § 8.15 und § 8.16 **vor #455** (Stand
`09037a32`, der Merge `704356a4` samt den Papieren zu #454), in § 8.17 und § 8.18 **vor #460** (Stand `9ab55946`, der
Merge #460 über den Entscheid-Papieren zu E8b, `46023235`), in § 8.19 und § 8.20 **vor #461** (Stand `62613292`, der
Merge #461 über `8793591b` = Zapfprofil Z3 samt den Papieren zu #460), in § 8.21 und § 8.22 **vor #462** (Stand
`75d45630`, der Merge #462 über `f06c8c9e` = Kühlung KU2 Welle 3 über Dialog Design #458 Stufe 3b samt den
Papieren zu #461), in § 8.23 und § 8.24 **vor #463** (Stand `94521f2e`, der Merge #463 über `502fea3c` = Dialog
Design #466 samt den Papieren zu #462), in § 8.25 und § 8.26 **vor #474** (Stand `4b50b77b`, der erste Merge #474 über
`3ff9840b` = Anlagenkopplung AK1 samt den Papieren zu #463 und #470 und den Entscheid-Papieren vom 24.09.2026), in § 8.27
und § 8.28 **vor #477** (Stand `61efa054`, der Anwender-Merge der Anlagenkopplung AK1, Welle 2, samt den Papieren zu
#474 und #476), in § 8.29 und § 8.30 **vor #478** (Stand `6d022f6d` = #477 samt seinen Papieren; der erste Merge #478,
`d176b378`, lässt die Papiere unberührt), in § 8.31 und § 8.32 **vor #479** (Stand `c99c4c7a` = #478 samt seinen Papieren
und #482; der erste Merge #479, `0462f92e`, lässt die Papiere unberührt), in § 8.33 und § 8.34 **vor #484** (Stand
`c778ab12` = #479 samt seinen Papieren, #488, die Anlagenkopplung AK1 Welle 3 und die Entscheid-Papiere vom
24.09.2026; der Merge #484, `ae7b0ed0`, lässt die Papiere unberührt), in § 8.35 und § 8.36 **vor #492** (Stand `247e2091` = #493 mit
Schemaschritt 130 samt den Papieren zu #484, #489, #490, #491 und #493; der Merge #492, `e79bffb1`, lässt die Papiere
unberührt), in § 8.37 bis § 8.39 **vor #498** (Stand `f55a4cd5` = #496 mit Schemaschritt 141 samt den Papieren zu
#492, #493, #494, #495 und #496; der Merge #498, `31a0b085`, lässt die Papiere unberührt), in § 8.40 **vor #506**
(Stand `cbed6dba` = #498 samt seinen Papieren, zusammengeführt mit #499 (Zapfprofilgenerator, ohne Berührung der
Wirtschaftlichkeit); der Merge #506, `e7c2f8f7`, lässt die Papiere unberührt), in § 8.42 und § 8.43 **vor #502**
(Stand `365143e1` = #506 samt seinen Papieren, zusammengeführt mit `origin` = `f52d38ec` (#501 und #505 mit
Schemaschritt 142); der Merge #502, `49ea20e0`, lässt die Papiere unberührt), in § 8.44 und § 8.45 **vor #503**
(Stand `f83ce27d` = #502 samt seinen Papieren, zusammengeführt mit `origin` = `bcd61fe2` (AK1 Welle 5 mit der Basis
R15 und #507); der Merge #503, `76f8661d`, zieht den Basisnamen an drei Stellen des Konzepts nach — Kopf, Tafel der
Regressionsanker, § 6.3 Nr. 21), in § 8.46 und § 8.47 **vor #510** (Stand `c48de9e0` = `origin` nach #509, darin #503
samt seinen Papieren; der Merge #510, `f7823b8e`, lässt die Papiere unberührt) —, nicht vor dem Schnitt.*

### 8.1 E6 — Verlauf mit drei Szenarien (#436)

Protokoll [`E6_Verlauf_Szenarien_Protokoll.md`](E6_Verlauf_Szenarien_Protokoll.md); die Fragen E6‑Q1 und
E6‑Q2 im Register (R‑E6), der Stand von E5b‑2, E5b‑3 und E5b‑4 in R‑E5.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E6 Verlauf mit drei Szenarien** (#436, Merge `57b15a7c`) | Aufzählung `ChartRenderer.Strichart` mit der dritten Strichart (Vorgabe byte-gleich); `WirtschaftlichkeitCtrl.BerechneVerlaufSzenarien` — drei vollständige Läufe je Version ohne Speichern; `ChartRenderer.KapitalwertSzenarien` mit Farbe = Variante, Strichart = Szenario, zweigeteilter Legende und Marken der Nulldurchgänge; der Verlauf als Abschnitt in „Wie sicher ist das?" (`KapitalwertVerlaufAbschnitt`, Hülle `KapitalwertVerlaufHuelle`), Knopf „Verlauf…" und `KapitalwertVerlaufDialog` entfallen; „Verlauf nach Excel…" und Blatt „Verlauf" (`VerlaufExcel`); Wortbericht mit dem Dreierbild, Tabellenbericht mit einer Spaltengruppe je Szenario; Nachträge E5b: `WIRT_EMPF_SATZ_STAMM`, „Bericht erzeugen" ohne Merken, Spannenbild `ChartRenderer.KapitalwertSpanne`; 34 Ressourcenschlüssel neu, drei geändert, zehn gestrichen; kein Schemaschritt | **keine** — Referenzlauf gegen `2026-09-22_R11_Bestandsbefunde` 13/13 Projekte, 3 882 737 Werte, 357 CSV byte-gleich; Verlauf und Spannenbild rechnen ohne Speichern; Nachweis sind die ChartProben (17 neue Bilder, die 91 bisherigen byte-gleich) und die Kern- und bunit-Tests (10 758 grün) |

*§ 2.13 (5), Z. 1022–1061 — die Messung vor der Umsetzung und die Liste dessen, was die Umsetzung
brauchte; im Konzept ersetzt durch den Stand-Vermerk „umgesetzt #436" mit den drei Kernaussagen
Dreierreihe, Farbe/Strichart und Legende:*

**(5) Der Verlauf mit allen drei Szenarien.** Gemessen: Der Knopf „Verlauf…" öffnet
`KapitalwertVerlaufDialog`, der **ein** Szenario je Lauf rechnet, immer bei Erwartet beginnt (die
Seite reicht ihre Szenariowahl nicht durch) und zwei Bilder zeigt (Differenz zur Referenz; kumulierte
Barwerte je Projekt absolut). Der Renderer (`ChartRenderer.KapitalwertVerlauf`) kennt keine
Szenarien, zeichnet aber beliebig viele Reihen auf eine Jahresachse und führt eine Legende mit
Name, Farbe und **Strichart je Reihe** (`Reihe.Gestrichelt` wird gelesen, `Segment.Gestrichelt`
zeichnet das Legendenfeld gestrichelt statt gefüllt); er kann **nicht**: ein Flächenband, mehr als
etwa zwei Legendenzeilen im festen Maß 1240 × 620, mehr als acht unterscheidbare Farben. **Entscheid des Entwurfs:** eigener Abschnitt bei „Wie sicher
ist das?" — die Bandbreite zeigt die Spanne am Ende, der Verlauf über die Zeit; der Knopf entfällt;
**Farbe = Variante, Strichart = Szenario** (ein Band ist bei mehreren Varianten unlesbar und vom
Renderer nicht zeichenbar); die Legende zweigeteilt (Varianten + 3 Einträge statt Varianten × 3);
der Nulldurchgang je Szenario markiert. **Das zweite Bild** (Versionen absolut) bleibt nicht auf der
Seite: Alle Versionen liegen tief im Negativen und nahezu parallel, entschieden wird über den
Abstand; der absolute Vergleich steht in der Kennzahltafel (Nettobarwert absolut) und in der
Mehrjahresübersicht des Berichts. **Als Bild steht er an genau einem Ort: im Wortbericht**, unter
dem Titel „Kumulierte Barwerte je Version", mit **Legende je Version** (Name und Farbe) und
gestrichelter Stammlinie — sie ist die Bezugsgröße und keine Version und muss auch im
Schwarz-Weiß-Ausdruck davon zu trennen sein. Auf der Seite bleibt allein das Differenzbild.
Nachweis: `Proben/ChartProben` (Bild `kapitalwert_absolut_legende`, Gegenproben
`kapitalwert_verlauf_gestrichelt_wirkt` und `kapitalwert_verlauf_legende_nennt_die_version`).
**Was die Umsetzung braucht:**

- einen Rechenaufruf für die Dreierreihe — `BerechneVerlauf` nimmt einen Szenario-String und
  `WirtschaftlichkeitVerlauf` trägt genau einen: entweder drei Läufe der bestehenden Methode (die
  Berichtsdaten werden ohnehin nur einmal gesammelt, der Mehraufwand ist die Zahlungsbildrechnung)
  oder ein Sammelmodell mit drei Szenarien;
- eine Reihenbildung, die Variante und Szenario zugleich unterscheidet — heute vergibt
  `VerlaufsReihen` Farben nach laufendem Index, und die Reihennamen tragen nur den Projektnamen
  (dasselbe Projekt in drei Szenarien bekäme drei beliebige Farben und dreimal denselben Namen).
  Die Palette hat **acht** Farben (`ChartRenderer.cs:553–562`); mit „Farbe = Variante" reicht sie
  bis acht Varianten;
- Platz für die zweigeteilte Legende und ein passendes Bildmaß (das Lesen von `Gestrichelt` in
  `KapitalwertVerlauf` steht). **`Reihe.Gestrichelt` ist ein `bool`** (`ChartRenderer.cs:106`) und
  trägt damit **zwei** Stricharten — drei Szenarien brauchen eine dritte;
- im Tabellenbericht je Szenario eine Spaltengruppe (die heutige Tabelle „Jahr, je Projekt eine
  Spalte, dann die Δ-Spalten" ist dafür nicht vorbereitet), im Wortbericht ein zusätzliches Bild;
- **plattformfrei**: Rechen- und Zeichenlogik der Ansicht gehören nach `EPOS.UI.Daten` — **Stand:
  umgesetzt #431**, der Ordner `Wirtschaftlichkeit` besteht, `KapitalwertVerlaufHuelle` liegt darin
  und sammelt, rechnet und zeichnet bereits plattformfrei über den Renderer des Kerns; offen bleibt
  allein die Dreiszenarien-Erweiterung dieses Punkts (E6).

*§ 6.3 Nr. 9j (Z. 2224–2226, Wortlaut in § 3.7):* **erledigt mit E6 (#436)** — die Dreierreihe steht
in `WirtschaftlichkeitCtrl.BerechneVerlaufSzenarien`, die Reihenbildung Variante × Szenario in
`ChartRenderer.VerlaufsReihenSzenarien` mit der Aufzählung `ChartRenderer.Strichart` (das Lesen von
`Gestrichelt` ist damit abgelöst), die Spaltengruppen je Szenario im Tabellenbericht samt Blatt
„Verlauf", die Rechen- und Zeichenlogik plattformfrei in `KapitalwertVerlaufHuelle`. Im Konzept steht
der Punkt als Einzeiler.

### 8.2 Berichtigungen im gültigen Stand (#436)

Nach #435 (a) nannte Sätze des Konzepts, die nach dem Grundsatz des Schnitts („verschoben, nicht
umgeschrieben") stehen geblieben waren, obwohl sie nicht mehr stimmten; mit den E6-Papieren sind sie
berichtigt. Dazu kommen die Stellen, die mit E6 selbst veraltet sind. Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Stand 22.09.2026, Codestand `deba5e57` | Stand 23.09.2026, Codestand des Merges #436 |
| § 2.2, Gruppe 2 | nur die Verweiszeile „Vorschlag am Feld und Überlagerung „Sätze und Herkunft…" (U22): → Register R‑Q (Q2)" — der von Q2 verlangte Satz fehlte | der Satz nach Q2: Die Grundlagenzeile mit dem Knopf bleibt am Feld, die Überlagerung ergänzt je Größe Wahl und Herleitung |
| § 2.6, Abweichungen | „Drei Abweichungen von der Tabelle unten"; „A4 und A5 stehen in einer Zeile …: `SteuerErgebnis.EnergiesteuerEur` ist eine Summe, ihre Trennung wäre … ein Umbau" | „Abweichungen und Klarstellungen"; A4 und A5 stehen wie in der Tabelle in zwei Zeilen (umgesetzt #432, U7), `EnergiesteuerEur` ist nur ihre Summe |
| § 2.7, Hausstil | „Fehlerzeile Firebrick `#B22222`" | „Fehlerzeile `#B00020` (Token `--epos-stufe-fehler`)" — Q9 |
| § 2.7, Fußleiste | „führt fünf Knöpfe — Photovoltaik, BHKW, Strombezug, Verlauf…, Berechnen —, nach dem Wegfall von „Verlauf…" vier"; „links die heutige Kennzahltafel, rechts die Abschnitte"; „Der Knopf „Verlauf…" entfällt mit der Ergebnisansicht" | höchstens vier Knöpfe; die Darstellung „Kennzahlen" mit den vier Abschnitten, „ValERI-Bewertung" mit den Blöcken der Norm (umgesetzt #434); einen Knopf „Verlauf…" gibt es nicht (umgesetzt #436) |
| § 2.10, Andockvorschlag | „… **oder** als zweite Ansicht der Seite … — Entscheidung am Mockup"; „der vorhandene Verlauf-Dialog bleibt als Vollbild-Absprung" | V‑1 entschieden, umgesetzt #434; der Verlauf als Abschnitt in „Wie sicher ist das?" (umgesetzt #436), kein Verlaufsdialog; Block 4: Frage E6‑Q1 |
| § 2.11.4 | V‑C „offen Block 2 … und das Cashflow-Bild"; „gebaut sind daraus E0 … und E5 (#434)" | das Cashflow-Bild als Verlauf gebaut #436 (Block 4: E6‑Q1); Etappenvermerk bis E6 (#436) |
| § 2.13 (5) | Messung, Entwurfsentscheid und „Was die Umsetzung braucht" (§ 8.1 oben) | Stand-Vermerk „umgesetzt #436" mit Dreierreihe, Farbe/Strichart und Legende; Regel zum zweiten Bild unverändert |
| § 3.6, K‑1 | „nächster freier Schemaschritt ist **97**" | „**101**" — 97 bis 100 außerhalb dieses Feldes (Kopf) |
| § 3.9, Tafel | CO₂-Zeile „Warnung mit Betrag — Soll, nicht gebaut (Etappe E2)"; Strommix „Laufhinweis ohne Wertangabe, kein `KohaerenzHinweis`" | beide umgesetzt #405: Warnung mit dem gebuchten Jahresbetrag (`Co2DoppelansatzBehg`); Hinweis mit dem Strommix-Vorgabewert (`KOH_CO2_STROMMIX_RUECKFALL`) |
| § 5, Einheitenbruch | „Die Umsetzung ist nicht freigegeben und gehört auf den Pufferspeicher-Strang." | freigegeben mit A9 (vor dem nächsten Vorlagenbau), DML-Schritt G mit E7 |
| § 6.1 | Kurztafel bis A13 (#435) | Zeile E6 (#436) |
| § 6.2, Tafel | Referenzbasis `Referenzlaeufe\2026-09-19_R10_BhkwWirkungsgrad` | `Referenzlaeufe/2026-09-22_R11_Bestandsbefunde` mit Verweis auf `Referenzlaeufe/LIESMICH.md` |
| § 6.3 Nr. 9j | offener Punkt (Wortlaut in § 3.7) | Einzeiler „erledigt mit E6 (#436)" |
| § 6.5, Tafel | „Kennzahlenliste dreifach — aufgelöst mit E7" | „aufgelöst mit W4 E7 (vor #300; nicht E7 des Etappenplans)" — gemeint war die Etappe E7 der Ausbaustufe W4, Protokoll [`W4_E7_Bericht_Mehrjahrestabelle_Protokoll.md`](W4_E7_Bericht_Mehrjahrestabelle_Protokoll.md) |
| § 7 und Anhang | „… und E5 (#434) … Als Nächstes kommt E6"; Kürzeltafel bis #434, „Mockup-Anhang U1…U45"; Etappenreihe „E6 … E12 — offen, nächste Etappe: E6" | bis E6 (#436), nächste Etappe E7; Kürzeltafel mit #436, U1…U49; Etappenzeile E6 = #436, „E7 … E12 — nächste Etappe: E7" |

### 8.3 E7a — rechenwirksame Lücken, Teil a (#437)

Protokoll [`E7a_Rechenwirksame_Luecken_Protokoll.md`](E7a_Rechenwirksame_Luecken_Protokoll.md); der Stand von
Nr. 29, 30 und 32 im Register (R‑NR), die drei offenen Fragen der Etappe unter R‑E7.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E7 Teil a** (#437, Merge `befec9dc`) | `SteuerGutschriftRechner.Co2JeEnergieertrag` prüft den CO₂-Grenzwert brennwertbezogen (Erdgas mit dem Katalogwert 181,4 g/kWh, die übrigen Träger über H_i/H_s, ohne Brennwert der Hi-Faktor mit Begründung; Herleitung je Anlage); Schemaschritt 101 setzt die leere `KWKG_Anlagenart` auf NULL (sieben Anlagen, kein BHKW), der BHKW-Dialog zeigt „(bitte wählen)"; `StromMatrix` bildet Bedarf und Lastbild vor Abzug der PV-Eigennutzung, beide Verteilschlüssel kommen brutto aus der Matrix, im Rollentarif ersetzt der PV-Anteil die Zeile „PV: vermiedener Bezug"; fünf Ressourcenschlüssel neu, vier geändert; Testdatenbank auf Schemastand 101 | **gewollt, im Bestand ohne Wirkung auf die Wirtschaftlichkeit** — die dreizehn Basisprojekte unverändert (kein Projekt erreicht die CO₂-Prüfung, kein Rollentarif im Bestand); der gespeicherte Strommatrix-Bedarf der Projekte mit Photovoltaik steigt um die PV-Eigennutzung (1007/1046, 1040, 1045); Referenzlauf gegen R11 13/13, 357 CSV byte-gleich; Anker 316.159,6 €/a über den Kernweg; 10 778 Tests grün |

*§ 6.3 Nr. 29 (Z. 2267–2275):*

29. **Hi/Ho am CO₂-Grenzwert** (R11) — **Es gilt immer der Brennwert** (→ Register R‑NR).
    Der Katalog führt zum Erdgas einen heizwert- und einen brennwertbezogenen
    EBeV-Faktor (200,9 bzw. 181,4 g/kWh) samt Umrechnung; gelesen wird heute der Schlüssel der
    Anlage, die beiden Ho-Zeilen haben keinen Leser. Der Grenzwert 270 g/kWh des § 2 StromStG wird
    **brennwertbezogen** geprüft: Der Zähler nimmt den Ho-Faktor (bei heizwertbezogenem Katalogwert
    die Umrechnung Hi → Ho), sonst fiele er rund 10 % zu hoch aus und die Befreiung entfiele in
    Grenzfällen zu Unrecht. Umsetzung mit **E7** (Rechenwirkung: der gebuchte Befreiungsbetrag und
    damit der Kapitalwert ändern sich in Grenzfällen; A/B-Nachweis, die Pinnung in
    `KleinkorrekturenE2Tests` wird auf den Brennwert umgestellt).

**Erledigt mit E7a (#437):** Der Zähler nimmt den Ho-Faktor — zu Erdgas den Katalogwert
`EF_BILANZ_EBEV_ERDGAS_HO` (181,4 g/kWh), zu den übrigen Trägern den heizwertbezogenen Wert × H_i/H_s des
Trägers, ohne gepflegten Brennwert den Hi-Faktor mit Begründung; die Herleitung nennt den Wert je Anlage.
Die Pinnung in `KleinkorrekturenE2Tests` steht auf dem Brennwert (Grenzfall 0,00 → 8.200,00 €/a). Kein
Basisprojekt erreicht die Prüfung (Hocheffizienz und räumlicher Zusammenhang überall 0); an einer Probe von
1024 mit beiden Angaben 278,6 → 262,2 g/kWh, Befreiung 0 → 1.680,07 €/a im Ausweis. Im Konzept steht der
Punkt als Einzeiler, die Regel in § 3.8.

*§ 6.3 Nr. 30 (Z. 2279–2286):*

30. **Sieben Energieanlagen tragen `KWKG_Anlagenart = ''`** (leere Zeichenkette statt NULL oder
    eines Steuerwerts). Die Anlagenart entscheidet über Kontingent und Satzstaffel; eine leere
    Zeichenkette ist weder „nicht gepflegt" noch eine Wahl.
    **Ein DML-Schritt setzt die leere Zeichenkette auf NULL; NULL heißt „nicht
    gepflegt" — der Kern bucht dann keinen KWKG-Zuschlag und meldet es als Kohärenzzeile
    „Anlagenart fehlt", der Dialog zeigt „bitte wählen". Ein geratener Wert würde Kontingent und
    Satzstaffel setzen, die niemand eingegeben hat. Umsetzung mit E7** (Schemaschritt, Nummer bei
    der Umsetzung; Projekte 1032 und 1043 der Testdatenbank, Live-Datenbank vorher prüfen). Entscheid: → Register R‑NR.

**Zum Teil erledigt mit E7a (#437):** Schemaschritt 101 (`SCHRITT_101_KWKG_ANLAGENART_LEER`) setzt die sieben
leeren Zeichenketten auf NULL (Anlage 12310 in 1032; 14819, 14842, 14843, 14844, 14851, 14852 in 1043 — kein
BHKW), der Dialog zeigt „(bitte wählen)" statt „(nicht erfasst — gilt als Neuanlage)". Kern-Regel und
Kohärenzzeile sind nicht gebaut: Wörtlich träfe die Regel jedes BHKW ohne Anlagenart, auch 1030 mit gepflegtem
Kontingent (KWKG-Erlös Jahr 1 7.315,96 € → 0, Kapitalwert −59.438,48 €) — Frage E7‑Q1. Im Konzept steht der
Punkt gekürzt mit dem offenen Teil.

*§ 6.3 Nr. 32 (Z. 2288–2301):*

32. **Die vermiedene Bezugsmenge führt keinen PV-Eigenverbrauch** (Befund aus E4/3, Frage U6‑Q1).
    `StromMatrix.Baue` bildet „Bedarf ohne Anlage" als Strombedarf abzüglich PV-Eigennutzung;
    `VermiedenMengeMWh` ist damit allein der KWK-Eigenverbrauch, und der Verteilschlüssel der
    Erlösrubrik (V‑4) bringt nur das Blockheizkraftwerk ein — der vermiedene Bezug der Photovoltaik
    bleibt seine eigene Ausweiszeile im Block Photovoltaik. Das Mockup-Beispiel rechnet dagegen
    „ohne jede Eigenerzeugung" (1.179,7 = 1.094,2 + 85,5 MWh). **Es gilt: ohne jede Eigenerzeugung** (U6‑Q1, → Register R‑NR).
    Bezugsgröße des Ausweises ist der Strombedarf des Projekts ohne jede Eigenerzeugung; die
    vermiedene Menge führt KWK- und PV-Eigenverbrauch, die § 9b-Korrektur greift auf beide, der
    Verteilschlüssel der Erlösrubrik bringt beide Anlagen ein. Der KWK-Eigenanteil (min-Regel auf
    den Bedarf nach Abzug der Photovoltaik) bleibt unverändert, ebenso der Kapitalwert (er rechnet
    mit dem tatsächlichen Restbezug). Betroffen sind die gespeicherten Ausweisspalten
    `VermiedenArbeit`, `VermiedenLeistung`, `VermiedenGesamt` und `VermiedenEntlastung9b` sowie der
    projektweite Leistungsanteil (Lastbild ohne Photovoltaik). Umsetzung mit E7: A/B-Nachweis über
    die dreizehn Basisprojekte, Anker 316.159,6 €/a am Beispielprojekt (293.245,6 + 22.914,0).

**Erledigt mit E7a (#437):** `StromMatrix` bildet Bedarf und Lastbild vor Abzug der PV-Eigennutzung
(`PvEigenGesamtMWh`), der KWK-Eigenanteil bleibt `min(BHKW, Bedarf nach PV)`; beide Verteilschlüssel kommen
brutto aus der Strommatrix (Orchestrator 23.09.2026 nach dem Mockup, Kategorie 7), im Rollentarif ersetzt der
PV-Anteil die Zeile „PV: vermiedener Bezug". Anker über den Kernweg 293.245,6 + 22.914,0 = 316.159,6 €/a; die
dreizehn Basisprojekte sind wirtschaftlich unverändert (kein Rollentarif im Bestand), der Strommatrix-Bedarf
steigt in 1007/1046 (19,10 → 24,00 MWh), 1040 (5,40 → 8,00) und 1045 (5,94 → 8,00). Im Konzept steht der
Punkt als Einzeiler, die Regel in § 3.6.

### 8.4 Berichtigungen im gültigen Stand (#437)

Die Stellen, die mit E7a veraltet sind; „vorher" ist der Wortlaut vor #437 (Stand `befec9dc`). Je Stelle eine
Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `57b15a7c`, `SchemaStand.Zielversion` = 100, „Schemaschritte 90–100 vergeben, neue ab **101**"; „Die vier zuletzt vergebenen Schritte gehören nicht diesem Feld: **97** … **100**" | Codestand `befec9dc`, `Zielversion` = 101, 90–101 vergeben, 102 an den Zapfprofilgenerator, neue ab 103; 101 = leere Anlagenart (§ 6.3 Nr. 30), 102 = Zapfprofilgenerator (Zweig `z0`) |
| § 2.2, Gruppe 1, Anlagenart | „(nicht erfasst = Neuanlage)" | „(bitte wählen) = nicht gepflegt, NULL"; ohne Anlagenart kein abgeleitetes Kontingent, die Kern-Regel ist Frage E7‑Q1 |
| § 2.6, Tafel A7 | „CO₂ < 270 g/kWh" | „CO₂ < 270 g/kWh brennwertbezogen (§ 3.8)" |
| § 2.6, Tafel B2 | „dito bzw. Mengenausweis" | dazu: im Rollentarif trägt der PV-Anteil an B1 den vermiedenen Bezug, die Zeile zum Flat-Preis steht nur ohne PV-Anteil |
| § 2.6, Klarstellung (1) | „`StromErloesErgebnis.VermiedenMengeMWh` = Bedarf ohne Anlage − Restbezug" | „= Bedarf ohne jede Eigenerzeugung − Restbezug"; KWK- und PV-Eigenverbrauch, die Korrektur greift auf beide |
| § 2.13 (4) | „der Kern verteilt heute nach dem Netto-Stromanteil"; „die Bezugsgröße der vermiedenen Menge wird mit E7 auf den Bedarf ohne jede Eigenerzeugung gestellt" | nach dem Eigenverbrauch je Anlage; mit #437 der Bedarf ohne jede Eigenerzeugung, der Schlüssel brutto aus der Strommatrix |
| § 3.5, Emissionsfaktor-Kette | ohne Satz zur Bezugsgröße | Bilanz und BEHG-Reihe heizwertbezogen, die Grenzwertprüfung des § 9 Abs. 1 Nr. 3 StromStG brennwertbezogen |
| § 3.6, KWKG-Prüfkette | „Realisierungsfrist 4 Jahre" ohne Vermerk | die Konstante `KWKG_REALISIERUNG_JAHRE = 4`; das Förderende 2030 als Frage E7‑Q3 |
| § 3.6, K‑1 | „nächster freier Schemaschritt ist **101** (… 97–100 außerhalb dieses Feldes …)"; „ob modulscharf im Ergebnismodell, ist vor der Umsetzung zu prüfen" | „**103** (… 101 die leere Anlagenart, 102 der Zapfprofilgenerator …)"; gemessen mit E7a: der Wärmeüberschuss nur als Projektsumme, Aufteilung nach P_el, Teilfragen E7‑Q2 |
| § 3.6, vermiedene Stromkosten | „Bezug = Rollenkosten(Bezugstarif, Bedarf OHNE Anlage)" | „Bedarf OHNE JEDE EIGENERZEUGUNG", dazu Menge und Schlüssel und der Absatz „Ohne jede Eigenerzeugung" |
| § 3.7 | — | ein Satz: dieselbe Umrechnung H_i/H_s stellt den brennwertbezogenen CO₂-Faktor (§ 3.8) |
| § 3.8 | „CO₂ < 270 g/kWh Energieertrag = Faktor_EBeV × Brennstoff/(Strom+Wärme)"; „Beleg: Heizöl 303,1 g/kWh → keine Befreiung ; Erdgas 228,6 → Befreiung" (heizwertbezogen) | `Faktor_Ho` mit den drei Wegen; Beleg brennwertbezogen: Beispielprojekt 218,6 g/kWh, Grenzfall 279,0 → 251,9 g/kWh (0,00 → 8.200,00 €/a), Heizöl EL auch brennwertbezogen über 270; Absatz „Der CO₂-Grenzwert ist brennwertbezogen" |
| § 3.11, Hi/Ho-Falle | ohne Vermerk zum Leser | die Grenzwertprüfung liest den brennwertbezogenen Faktor; `EF_BILANZ_EBEV_UMRECHNUNG_HO` ohne Leser |
| § 6.1 | Kurztafel bis E6 (#436) | Zeile E7a (#437) |
| § 6.2, Tafel | — | Anker „Vermiedene Kosten des Beispielprojekts über den Kernweg" 316.159,6 €/a |
| § 6.3 Nr. 29 und 32 | offene Punkte (Wortlaut in § 8.3) | Einzeiler „erledigt mit E7a (#437)" |
| § 6.3 Nr. 30 | offener Punkt (Wortlaut in § 8.3) | „zum Teil erledigt mit E7a (#437)", offen die Kern-Regel (E7‑Q1) |
| § 7 und Anhang | „… und E6 (#436) … Als Nächstes kommt **E7**"; B8 „in E7"; Etappenzeile „E7 … E12 — nächste Etappe: E7" | bis E7 Teil a (#437), nächste Etappen E7b und E7c; B8 in E7c; Kürzeltafel mit § 6.3 Nr. 29, 30, 32 = #437; Etappenzeilen „E7 Teil a" = #437 und „E7b, E7c … E12" |

### 8.5 E7b — Zeitzonentarif und Leistungspreis-Staffel (#439)

Protokoll [`E7b_Zeitzonentarif_Staffel_Protokoll.md`](E7b_Zeitzonentarif_Staffel_Protokoll.md); der Stand von Q11 im
Register (R‑Q), die vier Fragen der Etappe unter R‑E7b.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E7 Teil b** (#439, Merge `954d4dcc`) | `StromMatrix` ohne Tarifzonen (Jahressummen und Lastbilder, eine Jahreszeile je Projekt in `Tab_ErgebnisStromMatrix`); ein aktiver Zonensatz rechnet nicht mehr (Hinweis `WIRT_HINWEIS_ZEITZONENTARIF`); `LeistungspreisStaffel` am Stromträger im `KostenEmissionRechner` (Viertelstundenspitze, Vorrang vor Saisonreihe und Satz), gepflegt auf der Trägerkarte (`EnergietraegerPreisCtrl`, Hülle, KI-Felder); der Tarifdialog nur noch im Rollenmodell, Knopf und Sprung „Strombezug…" entfallen; Schemaschritt 104 (drei Staffelspalten an `energy_project_settings`, die Staffel der Zonensätze an den Stromträger, Zonensätze gelöscht und ihre gespeicherten Läufe verworfen, die Zonenzeilen der Matrix zu einer Jahreszeile); 21 Ressourcenschlüssel neu, 5 geändert, 38 gestrichen; Testdatenbank auf Schemastand 104 | **gewollt, im Bestand ohne Wirkung** — die dreizehn Basisprojekte unverändert (kein Tarifsatz im Bestand), im frischen Lauf 1030 um 1·10⁻⁸ € (Matrixsumme in einem Durchlauf); die gespeicherte Matrix eine Jahreszeile statt vier Zonenzeilen (±0,001 MWh Rundung); Probe 1030 mit Zonensatz: Energiekosten 1.832.155,35 → 1.760.606,20 €/a, Kapitalwert −34.819.801,17 → −33.551.896,03 €; Referenzlauf gegen R11 13/13, 3 882 737 Werte byte-gleich; Gate 10 996 Tests grün |

*Konzept § 6.3, Q11 (ohne Nummer, neu als Einzeiler):* Q11 stand vor #439 nicht als offener Punkt in § 6.3; sein
Stand stand im Register (R‑Q, „offen — E7") und im Etappenplan (§ 7, Anhang).

**Erledigt mit E7b (#439):** Den Zeitzonentarif gibt es nicht mehr — die Strommatrix führt keine Tarifzonen,
Schemaschritt 104 übernimmt die Staffel an den Stromträger, löscht die Zonensätze und verwirft ihre gespeicherten
Läufe (E7b‑Q4), ein Satz vor dem Schritt bekommt einen Hinweis am Ergebnis; die zweistufige Staffel steht beim
Stromträger der Kostenverwaltung, bemessen an der Viertelstundenspitze (E7b‑Q2), mit Vorrang vor Leistungspreis
und Saisonreihe (E7b‑Q3); der Tarifdialog ist auf das Rollenmodell reduziert (E7b‑Q1 = b), der Einstieg
„Strombezug…" entfällt, einen Menüpunkt gab es nicht. Im Konzept steht der Punkt als Einzeiler, die Regel in
§ 3.5 und § 2.5.

### 8.6 Berichtigungen im gültigen Stand (#439)

Die Stellen, die mit E7b veraltet sind, und die Schrittnummern nach der Umnummerierung vom 23.09.2026 (#438);
„vorher" ist der Wortlaut vor #439 (Stand `954d4dcc`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `befec9dc`, `SchemaStand.Zielversion` = 101, „Schemaschritte 90–101 vergeben, **102** an den Zapfprofilgenerator, neue ab **103**"; „Die Schritte 97 bis 100 gehören nicht diesem Feld …; **101** setzt die leere `KWKG_Anlagenart` auf NULL (`SCHRITT_101_KWKG_ANLAGENART_LEER` …); **102** führt die Sitzung des Zapfprofilgenerators …" | Codestand `954d4dcc`, `Zielversion` = 104, 90–104 vergeben, 105 an K‑1, neue ab 106; nicht diesem Feld 97–101 und 103 (101 Gebäudespalten, 103 Zapfprofilgenerator), diesem Feld 102 (leere Anlagenart, `SCHRITT_102_KWKG_ANLAGENART_LEER`) und 104 (Zeitzonentarif abgelöst, Staffel am Stromträger) |
| § 2.2, Gruppe 1, Anlagenart | „„(bitte wählen)" und Schritt 101 #437" | Schritt 102 |
| § 2.2, Gruppe 4 | „**zwei Sprungknöpfe „Strombezug…" und „BHKW-Tarif…"**"; „Beide Sprünge speichern nur …" | ein Sprungknopf „BHKW-Tarif…" (Tarifstruktur im Rollenmodell); einen Sprung „Strombezug…" gibt es nicht, die Staffel steht beim Stromträger (§ 2.5) |
| § 2.5, Trägerkarte | — | Absatz „Die Leistungspreis-Staffel steht beim Stromträger": Gruppe im Block „Preis und Heizwert", drei Spalten an `energy_project_settings` (Schritt 104), NULL = nicht gepflegt, kein Katalog |
| § 2.7, Fußleiste | „führt **höchstens vier Knöpfe** — Photovoltaik, BHKW und Strombezug je nach Ausstattung der Gruppe, dazu Berechnen" | höchstens drei Knöpfe — Photovoltaik und BHKW, dazu Berechnen; kein „Strombezug…", die Tarifstruktur öffnet sich aus BHKW- und PV-Dialog |
| § 2.11.6, was dauerhaft Werte bleibt | „die **Strommengen-Matrix** nach Tarifzonen (Zonenzuordnung je Stunde, stundenweises Minimum für den KWK-Eigenstrom)" | als Jahreszeile ohne Tarifzonen (Jahressummen, stundenweises Minimum, höchste Stundenlast) |
| § 2.13 (4) | „die Strommatrix trennt nur nach **Tarifzone** (`StromMatrix.Zone`), nicht nach Anlage" | sie führt je Projekt Jahressummen (eine Jahreszeile, keine Tarifzonen), nicht Mengen je Anlage |
| § 3.5, Netzbezug Strom | „das Stundenmittel (StromMatrix.MaxBezugKW) glättet die Spitze und bleibt der Tarifstruktur vorbehalten"; Satz und Saisonreihe, keine Staffel; „Im Tarifmodus ersetzt der Zonen- oder Rollenbetrag den **ganzen** Flat-Anteil samt Leistungsanteil." | das Stundenmittel bemisst allein die Leistungspreismodelle des Rollentarifs; Staffel vor Saisonreihe vor Satz, mit der Formel der Staffel; die Absätze „Die zweistufige Leistungspreis-Staffel" (Vorrang, Viertelstundenspitze, Probe 1030 mit 135.990 €/a) und „Kein Zeitzonentarif" (Jahreszeile, der Rollentarif ersetzt den Flat-Anteil samt Staffel, Schritt 104, Hinweis vor dem Schritt) |
| § 3.6, K‑1 | „nächster freier Schemaschritt ist **104** (… 97–100 außerhalb dieses Feldes, 101 die leere Anlagenart (§ 6.3 Nr. 30), 102 der Zapfprofilgenerator, 103 die Leistungspreis-Staffel (E7b) …)" | „**105** (… 97–101 und 103 außerhalb dieses Feldes, 102 die leere Anlagenart (§ 6.3 Nr. 30), 104 die Leistungspreis-Staffel (§ 3.5) …)" |
| § 3.6, Einspeiseerlös | „→ Zonentarif → Rollentarif → PV-Dialog …" | „→ Rollentarif (ein Einspeisepreis für beide Mengen) → PV-Dialog …" |
| § 5, Einheitenbruch | „(nächster freier Schritt am 22.09.2026: **101** — 90–100 sind vergeben)" | heute **106** — 90–104 vergeben, 105 gehört K‑1 |
| § 6.1 | Kurztafel bis E7a (#437), dort „Schemaschritt 101" | E7a mit Schemaschritt 102; Zeile E7b (#439) |
| § 6.3 | Nr. 30 „Schemaschritt 101 setzt die leere Zeichenkette auf NULL"; Q11 ohne Zeile | Schemaschritt 102; die Gruppe „Aus der Mockup-Prüfung (Q11) — geschlossen" mit dem Einzeiler Q11 (Grund in § 8.5) |
| § 7 und Anhang | „… und E7 Teil a (#437) … Als Nächstes kommen **E7b** (…) und **E7c** (…)"; Kürzeltafel „Schemaschritt 101"; Etappenzeilen „E7 Teil a" (Schemaschritt 101) und „E7b, E7c … E12 — nächste Etappen: E7b und E7c" | bis E7 Teil b (#439), als Nächstes E7c mit K‑1 als Schritt 105; Kürzeltafel mit Schritt 102 und der Zeile Q11 = #439; Etappenzeilen „E7 Teil a" (Schritt 102), „E7 Teil b — Q11" = #439 und „E7c … E12 — nächste Etappe: E7c" |

### 8.7 E7c1 — K‑1 Fall 2, Förderende 2030, Anlagenart-Kohärenz (#440)

Protokoll [`E7c1_KWKG_Fall2_Foerderende_Protokoll.md`](E7c1_KWKG_Fall2_Foerderende_Protokoll.md); der Stand von A2\*,
A20, Nr. 30, EZ‑5 und E7‑Q1 bis E7‑Q3 im Register (R‑A, R‑NR, R‑EZ, R‑E7), die acht offenen Fragen der Etappe
unter R‑E7c1.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E7 Teil c1** (#440, Merge `ea8e2a12`) | Schemaschritt 105 (`KWKG_Abwaermeabfuhr` 0/1 und `KWKG_Stromkennzahl` an `Tab_Energieanlagen`); `KwkStromRechner` — der zweite Fall des § 2 Nr. 16 KWKG, `min(Netto, Nutzwärme × σ)`, σ gepflegt oder P_el ÷ P_th, sonst kein Zuschlag der Anlage mit Kohärenzzeile, nur der Wärmeüberschuss nach P_el verteilt, Kürzung zuerst an der Einspeisung, Ersatzweg nach P_el, Herleitung je Anlage, sieben nullbare Nachweisfelder (Fassung 7); das Ende der Frist zur Inbetriebnahme als Katalogdatum `KWKG_INBETRIEBNAHME_FRISTENDE` 31.12.2030 (Generation 8) an Stelle von `KWKG_REALISIERUNG_JAHRE = 4`, auch ohne Stichtag, ohne Katalogwert „ungeprüft"; die Kohärenzzeilen „Stromkennzahl fehlt" und „Anlagenart fehlt"; die Überlagerung „Sätze und Herkunft" mit den zwei Feldern; 43 Ressourcenschlüssel neu, einer geändert; Testdatenbank auf Schemastand 105 mit der Anlagenart NEUANLAGE der 1030-BHKW | **gewollt, im Bestand ohne Wirkung** — die dreizehn Basisprojekte unverändert (kein Kennzeichen im Bestand; 1030 mit Inbetriebnahme 2027 vor dem Fristende und gepflegtem Kontingent); Proben an 1030 im Protokoll E7c1 (σ 0,5: KWKG Jahr 1 7.315,96 → 6.137,94 €); Referenzlauf gegen R11 13/13, 3 882 737 Werte byte-gleich; Gate 11 058 Tests grün |

*§ 3.6, Befund K‑1 (Z. 1800–1832):*

> ⚠ **Befund K-1 (neu): Der zweite Fall des § 2 Nr. 16 fehlt.** Verfügt eine Anlage **über eine
> Vorrichtung zur Abwärmeabfuhr** — beim Notkühler größerer BHKW der Regelfall —, ist KWK-Strom
> **nicht** die Nettostromerzeugung, sondern `Nutzwärme × Stromkennzahl`. Das ist eine völlig
> andere Größe: Sie hängt an der genutzten Wärme, nicht an der Stromerzeugung, und kann bei
> Wärmeüberschuss deutlich darunter liegen. EPOS-Plan führt **weder** ein Kennzeichen
> „Abwärmeabfuhr vorhanden" **noch** eine Stromkennzahl und rechnet immer den ersten Fall.
> Für Anlagen mit Notkühler fällt der Zuschlag damit **zu hoch** aus. Zu entscheiden, nicht
> stillschweigend zu lassen.
>
> **Entscheid K-1 (→ Register R‑EZ, EZ‑5): Kennzeichen und Stromkennzahl je Anlage aufnehmen,
> Fall 2 rechnen.** Neuer Boden seit BK1: Der Zuschlag gehört der Anlage (Schemaschritt 89,
> § 6.5) — die zwei Felder sind zwei weitere Anlagenspalten neben den neun `KWKG_*`-Spalten von
> `Tab_Energieanlagen`, kein Umbau; nächster freier Schemaschritt ist **105** (90 BK1a,
> 91 BK1b, 92 Vergleichsprojekt, 93 Vergütung je Variante, 94 Hilfsstrom-Bemessung, 95 KL-3 Klimaspalten, 96 FK-2 Projekt-Fremdschlüssel,
> 97–101 und 103 außerhalb dieses Feldes, 102 die leere Anlagenart (§ 6.3 Nr. 30), 104 die Leistungspreis-Staffel (§ 3.5), siehe Kopf;
> die Nummer wird bei der Umsetzung vergeben). Das Kennzeichen
> `KWKG_Abwaermeabfuhr` (0/1, `CHECK`), die Stromkennzahl als nullbare Zahl mit **Vorschlag am
> Feld** aus P_el / P_th der Gerätezeile (`Tab_BHKW`, wo σ heute nur für die Katalogliste gerechnet
> wird) — dasselbe Muster wie die Vorschlagszeilen aus BK1. Ohne gepflegten Wert (`KWKG_Stromkennzahl`) oder P_th gibt es keinen Ersatzwert, sondern die Kohärenzzeile „Stromkennzahl fehlt" (Entscheid E7‑Q2 (2), 23.09.2026: keine willkürliche Vorgabe). **Wo die Fallunterscheidung sitzt:**
> `WirtschaftlichkeitCtrl.ReiheJeAnlage` bildet je Anlage `stromNettoJeAnlage[i] = max(0,
> StromVon(Modul[i]) − Hilfsstrom[i])` mit `StromVon` = Klemmenerzeugung (`Stromproduktion`);
> bei gesetztem Kennzeichen tritt dort `min(Nettostromerzeugung, Nutzwärme × σ)` — die Nutzwärme je
> Modul aus Wärmeproduktion abzüglich Wärmeüberschuss. **Gemessen mit E7a (#437, A2):** Die
> Wärmeproduktion liegt je Modul vor, der Wärmeüberschuss nur als Projektsumme (in allen
> BHKW-Basisprojekten 0) — es greift die Aufteilung nach P_el, und zwar nur für den Überschuss
> (E7‑Q2, entschieden 23.09.2026, → Register R‑E7). Der Torwächter `BaueKwkgReihe` (`v.Ergebnis.BHKW.Stromproduktion`
> als Summe) bleibt. **Referenzprojekte, gemessen an der Testdatenbank:** BHKW führen 1017, 1018,
> 1024 und 1030 (und das Nichtbasisprojekt 1031); KWKG-Sätze trägt allein 1030 (8,0 / 4,0 an beiden
> Anlagen), `Betriebsart` ist überall leer, `Wärmeüberschuss` in der Basis überall 0. Das
> Kennzeichen wäre nach dem Schemaschritt nirgends gesetzt, und die Referenzbasis vergleicht nur
> Simulationsgrößen — **kein Basisprojekt ist betroffen, keine neue Basis**; nötig würde sie nur,
> wenn die Umsetzung an der Simulation oder an einer in `aggregate.csv` landenden Spalte der
> Testdatenbank etwas änderte.

**Erledigt mit E7c1 (#440):** Kennzeichen und Stromkennzahl stehen je Anlage an `Tab_Energieanlagen`
(Schemaschritt 105); `KwkStromRechner` rechnet Fall 2 nach den fünf Teilantworten zu E7‑Q2 — nur der
Überschuss nach P_el; σ gepflegt oder P_el ÷ P_th, sonst kein Ersatzwert, sondern kein Zuschlag der Anlage und
die Kohärenzzeile „Stromkennzahl fehlt"; die Kürzung zuerst an der Einspeisung; der Ersatzweg nach P_el; die
Pflege in der Überlagerung „Sätze und Herkunft". Die dreizehn Basisprojekte sind unverändert (kein Kennzeichen
im Bestand); an 1030 gemessen: σ gepflegt 0,5 → KWKG Jahr 1 7.315,96 → 6.137,94 €, σ berechnet 50 ÷ 81 →
7.315,92 €, ohne σ → 1.116,03 €. Im Konzept steht an der Stelle des Befunds die Regel „Der zweite Fall des § 2
Nr. 16 — Anlagen mit Vorrichtung zur Abwärmeabfuhr" (§ 3.6), in § 4 der Befund als erledigt.

*§ 3.6, Prüfkette (Z. 1737–1742):*

> Deckelstaffel 5.000 (2021) … 3.300 (2026) … 2.500 (ab 2030). Vorgeschaltete Prüfkette: Stichtag
> ≤ 31.12.2026 · Realisierungsfrist 4 Jahre · Ausschreibung > 500 kW · Heizöl-Neuanlage ab 2025. Die
> Realisierungsfrist ist eine Konstante (`KWKG_REALISIERUNG_JAHRE = 4`); das Förderende 2030 (A20,
> R‑U5) ersetzt sie als Katalogdatum für das Ende der Frist zur Inbetriebnahme — der Zuschlag läuft
> danach bis zum Ende des Kontingents weiter, keine Höchstdauer in Kalenderjahren (entschieden
> E7‑Q3, Lesart b, 23.09.2026, → Register R‑E7 — Bau E7c).

**Erledigt mit E7c1 (#440):** Die Konstante ist gestrichen; `KWKG_INBETRIEBNAHME_FRISTENDE` = 2030 (der
31.12.2030, Katalog-Generation 8) wird mit dem Inbetriebnahmejahr nachgeschlagen, auch ohne Stichtag geprüft
und ohne Katalogwert mit der Zeile „ungeprüft" übergangen; die Reihe läuft bis zum Kontingentende. Proben an
1030: Stichtag 2025 und Inbetriebnahme 06/2030 → KWKG Jahr 1 0 → 5.899,97 €; Inbetriebnahme 03/2031 ohne
Stichtag 5.899,97 → 0 €. Im Konzept steht die Regel in § 3.6 (Absatz „Das Ende der Frist zur Inbetriebnahme
ist ein Katalogdatum").

*§ 6.3 Nr. 30 (Z. 2362–2371):*

30. **Sieben Energieanlagen trugen `KWKG_Anlagenart = ''`** — **zum Teil erledigt mit E7a (#437),
    siehe Protokoll:** Schemaschritt 102 setzt die leere Zeichenkette auf NULL (NULL heißt „nicht
    gepflegt"; die sieben Anlagen der Projekte 1032 und 1043 sind kein BHKW), der Dialog zeigt
    „(bitte wählen)". Ein geratener Wert würde Kontingent und Satzstaffel setzen, die niemand
    eingegeben hat. **Entschieden** (E7‑Q1, Lesart b, 23.09.2026, → Register R‑E7): Die Kern-Regel
    „NULL ⇒ kein KWKG-Zuschlag" und die Kohärenzzeile „Anlagenart fehlt" greifen nur dort, wo das
    Kontingent nach § 8 abzuleiten ist — nicht wörtlich bei jedem BHKW ohne Anlagenart; das BHKW von
    1030 behält mit gepflegtem Kontingent (30.000 h) seinen Zuschlag, seine Anlagenart wird in der
    Testdatenbank gepflegt. Die Live-Datenbank ist vor dem Ausrollen zu prüfen (der Schritt trifft
    jede leere Zeichenkette). Entscheid: → Register R‑NR. Bau in E7c.

**Erledigt mit E7c1 (#440):** Die Kern-Regel greift nur, wo das Kontingent aus der Anlagenart abzuleiten ist
(Lesart b) — so rechnete `KwkgKontingentRechner` schon (0 h mit Grund); neu sind die Herleitung
`WIRT_KWKG_KONTINGENT_ANLAGE_OHNE_ART` und die Kohärenzzeile „Anlagenart fehlt" (`KOH_KWKG_ANLAGENART_FEHLT`,
Hinweis, ohne Betrag). Die Testdatenbank führt die Anlagenart der 1030-BHKW (14920, 14921) als NEUANLAGE; das
Kontingent bleibt gepflegt, kein Anker bewegt sich. Im Konzept steht der Punkt als Einzeiler, die Regel in
§ 3.6 und § 3.9.

### 8.8 Berichtigungen im gültigen Stand (#440)

Die Stellen, die mit E7c1 veraltet sind; „vorher" ist der Wortlaut vor #440 (Stand `ea8e2a12`). Je Stelle eine
Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `954d4dcc`, `SchemaStand.Zielversion` = 104, „Schemaschritte 90–104 vergeben, **105** an K‑1 (E7c), neue ab **106**"; „Diesem Feld gehören **102** … und **104** …; **105** bekommt K‑1 (§ 3.6, E7c)" | Codestand `ea8e2a12`, `Zielversion` = 105, 90–105 vergeben, neue ab 106; diesem Feld gehören 102, 104 und 105 (Kennzeichen und Stromkennzahl je Anlage, `SCHRITT_105_KWKG_ABWAERMEABFUHR`, #440) |
| § 2.2, Gruppe 1, Anlagenart | „(entschieden E7‑Q1, Lesart b, 23.09.2026, § 6.3 Nr. 30 — Bau E7c)"; Stand „„(bitte wählen)" und Schritt 102 #437" | „(E7‑Q1, Lesart b, § 6.3 Nr. 30, § 3.9)"; Stand dazu „Kohärenzzeile #440" |
| § 2.2, Gruppe 1, Tafel der Anlagenfelder | — | zwei Zeilen „KWK-Strom (§ 2 Nr. 16 KWKG)" (`KWKG_Abwaermeabfuhr`) und „Stromkennzahl σ" (`KWKG_Stromkennzahl`), neu #440 |
| § 2.2, Gruppe 2 | — | Absatz „Die Überlagerung „Sätze und Herkunft" trägt die zwei Felder des zweiten Falls des § 2 Nr. 16" (gebaut #440; der Rest von U22 mit Frage E7c1‑Q7) |
| § 2.2, Gruppe 5 | — | ein Satz: bei Fall 2 zeigt die Mengenkette die Herleitung des Laufs |
| § 3.6, Vbh-Kontingent | „…; darunter 0 mit Fehlgrund." | dazu: ohne Anlagenart 0 h mit Grund und die Kohärenzzeile „Anlagenart fehlt" — nur dann; ein gepflegtes Kontingent bleibt wirksam |
| § 3.6, Jahresreihe | — | Zeile „Fall 2(A): Eigen/Einsp(A) um Netto(A) − KWK-Strom(A) gekürzt, zuerst Einsp" |
| § 3.6, Prüfkette | Wortlaut in § 8.7 | „Inbetriebnahme bis zum Ende der Frist zur Inbetriebnahme"; Absatz „Das Ende der Frist zur Inbetriebnahme ist ein Katalogdatum" (auch ohne Stichtag, ohne Katalogwert „ungeprüft", keine Höchstdauer in Kalenderjahren) |
| § 3.6, Befund K‑1 | Wortlaut in § 8.7 | Regel „Der zweite Fall des § 2 Nr. 16 — Anlagen mit Vorrichtung zur Abwärmeabfuhr" mit Formelblock, den fünf Teilantworten zu E7‑Q2, Herleitung, Nachweisfeldern und den Fragen E7c1‑Q1, Q2 und Q6 |
| § 3.9 | Tafel bis „Strommix-Rückfall" | zwei Zeilen „Stromkennzahl fehlt" und „Anlagenart fehlt" (Hinweis, ohne Betrag; Schwere: Frage E7c1‑Q5) |
| § 3.10, Schritt 9 | „Satz (marginal) → Hilfsstrom-Netting → Anteile → Bonus_voll" | „Prüfkette (Fristende der Inbetriebnahme aus dem Katalog) → Satz (marginal) → Hilfsstrom-Netting → Anteile → Fall 2: Kürzung auf den KWK-Strom, zuerst an der Einspeisung → Bonus_voll" |
| § 4, K‑1 | „⚠ **K-1** … Weder Kennzeichen noch Stromkennzahl sind im Datenmodell vorhanden; der Zuschlag fällt für solche Anlagen zu hoch aus." | „✔ **K-1** … **Erledigt mit E7c1 (#440)**" |
| § 5, Einheitenbruch | „90–104 sind vergeben, 105 gehört K‑1" | „90–105 sind vergeben, 105 trägt K‑1" |
| § 5, R-U5 | „Förderzeitraum als Datumsparameter im Katalog, nicht als Konstante" | dazu „umgesetzt #440" mit `KWKG_INBETRIEBNAHME_FRISTENDE` |
| § 6.1 | Kurztafel bis E7b (#439) | Zeile E7c1 (#440) |
| § 6.3 Nr. 30 | „zum Teil erledigt mit E7a (#437)", offen die Kern-Regel, „Bau in E7c" (Wortlaut in § 8.7); Gruppentitel „Sachpunkte der Datenaufnahme" | Einzeiler „erledigt mit E7a (#437) und E7c1 (#440)" mit dem Prüfhinweis zur Live-Datenbank; Gruppentitel „…, alle drei erledigt" |
| § 7 und Anhang | „… und E7 Teil b (#439) … Als Nächstes kommt **E7c** (…, K‑1 mit Schemaschritt 105)"; B8 „in E7c"; Kürzeltafel ohne #440, „U1 = Befund K-1"; Etappenzeile „E7c … E12 — nächste Etappe: E7c" | bis E7 Teil c1 (#440), als Nächstes E7c2; B8 in E7c2; Kürzeltafel mit „#437 (Nr. 30 zum Teil, der Rest #440)", der Zeile K‑1 · A20 · Nr. 30 = #440 und „U1 = Befund K-1 (erledigt #440)"; Etappenzeilen „E7 Teil c1" = #440 und „E7c2 … E12 — nächste Etappe: E7c2" |

### 8.9 E7c2 — Schritte E, F, G, S‑2, V‑1/V‑2, B‑4 und die Reste aus E7c1 (#446)

Protokoll [`E7c2_Schritte_EFG_S2_V_B4_Protokoll.md`](E7c2_Schritte_EFG_S2_V_B4_Protokoll.md); der Stand von A3, A4,
A6, A9, ET‑D‑3, U‑1, Q2 und E7c1‑Q1, Q2, Q7 im Register (R‑A, R‑D, R‑Q, R‑E7c1), die acht Fragen der Etappe —
alle am 23.09.2026 entschieden — unter R‑E7c2.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E7 Teil c2** (#446, Merge `41764ab0`) | Schemaschritt 111 (`ErsatzFuehren`, `RestwertAnsetzen` je Position, nullbar) mit Zeileneditor, Tafel „Ersatz und Restwert" und Kapitalwert; Schemaschritt 112 (`Preisbasis` an `energy_project_settings`, einmalig aus `ID_Umrechnung`); Schemaschritt 113 (die fünf Gase auf Nm³, der Brennstoff 24 auf kWh, eine Preiszeile); S‑2: die Mischlage gesperrt, § 54 = 0 mit Begründung, Kohärenzzeile als Warnung; V‑1/V‑2: EV-Mix ungerundet, Erlös auf Cent, § 51a bei fester Vergütung mit der Einspeisevergütung; B‑4: die zwei Prozentarten frisch aus dem Lauf (Arbeitskosten); E7c1‑Q2 b: Vollbenutzungsstunden in Fall 2 aus dem KWK-Strom; E7c1‑Q1: der Rundungsgrund; E7c1‑Q7: der Rest der Überlagerung (`KwkgJahresbetrag`), der KI-Feldkatalog und die Modultafel mit fünf Spalten mehr; 72 Ressourcenschlüssel neu, einer gestrichen; Testdatenbank auf Schemastand 113 | **gewollt, im Bestand ohne Wirkung** — die dreizehn Basisprojekte 9.195 von 9.195 Werten gleich; Proben im Protokoll E7c2 (S‑2 an 1030: § 54 Jahr 1 7.987,41 → 0 €; B‑4 an 1030: 300,00 → 15.483,29 €/a; E7c1‑Q2 b an 1030 mit σ 0,5: KWKG Jahr 1 6.137,94 → 7.316,03 €); Referenzlauf gegen R12 13/13, 4 250 839 Werte, 399/399 byte-gleich; Gate #446 grün |

*§ 2.2, Gruppe 2, die Überlagerung (Z. 219–230):*

> **Die Überlagerung „Sätze und Herkunft" trägt die zwei Felder des zweiten Falls des § 2 Nr. 16**
> (K‑1, § 3.6; → Register R‑E7, E7‑Q2 (5); umgesetzt #440). Die Gruppe „Angaben der gewählten Anlage"
> zeigt die Zeile „KWK-Strom (§ 2 Nr. 16 KWKG): Fall 1 — Nettostromerzeugung." bzw. „Fall 2 — Vorrichtung
> zur Abwärmeabfuhr, Stromkennzahl σ … (Herkunft)" und den Knopf „Sätze und Herkunft…". Die Überlagerung
> „Sätze und Herkunft — ‹Anlage›" führt die Gruppe „KWK-Zuschlag — diese Anlage" mit der Wahl Fall 1 /
> Fall 2 samt Wirkung je Fall und die Tafel Größe · Vorschlag · Herkunft · eigener Wert (leer = Vorschlag)
> · gilt mit der Zeile „Stromkennzahl σ" (Vorschlag P_el ÷ P_th der Gerätezeile, „Vorschlag übernehmen"
> leert das Feld, ohne P_th weich gesperrt), dazu Abbrechen und Übernehmen. Sie hält einen eigenen
> Zwischenstand; „Übernehmen" legt ihn auf den Arbeitsstand der Anlage, geschrieben wird im OK-Weg des
> Dialogs (`KwkgAnlagenCtrl.Speichere`). Die übrigen Größen der Überlagerung — Anlagenart, Tatbestand,
> Satztafel, Energie- und Stromsteuer, „Wirkung Jahr 1", der Knopf „Wahl und Herkunft…" — stehen weiter
> im Formular (Mockup-Anhang U22; entschieden E7c1‑Q7, nach Empfehlung, Bau E7c2 → Register R‑E7c1).

**Erledigt mit E7c2 (#446):** Die Überlagerung trägt alle Größen von U22 — Anlagenart und Tatbestand mit der
Wirkung je Wahl, Fall 1 / Fall 2, die Satztafel für Einspeisung, Eigenstrom, Kontingent und Deckel, „Wirkung
Jahr 1", die Gruppen Energiesteuer und Stromsteuer, der zweite Knopf „Wahl und Herkunft…". „Leer = Vorschlag"
lässt bei Kontingent und Deckel das Feld leer, ein leeres Satzfeld erscheint als eigener Wert 0. Die Klapplisten
und die Knöpfe „Vorschlag übernehmen" des Formulars bleiben daneben (Q2). Im Konzept steht der Absatz „Die
Überlagerung „Sätze und Herkunft" trägt alle Wahlen und Sätze" mit den drei Gruppen und der Regel „Leer heißt
Vorschlag".

*§ 2.13 (3), die fehlenden Stücke (Z. 1024–1032):*

> **Was der späteren Umsetzung fehlt** (drei Stücke, im Mockup-Anhang als **U39** geführt):
>
> 1. die **Entkopplung** von Ersatz und Restwert — ein Kennzeichen je Position oder je Technik
>    („Ersatz führen", „Restwert ansetzen"), weil ein Anwender oft das eine ohne das andere will;
> 2. die **geräteeigenen Nutzungsdauer-Spalten** (`Tab_BHKW`, `Tab_Heizkessel`,
>    `Tab_StromspeicherVariante`) — eine zweite Wahrheit, die kein Wirtschaftlichkeitsrechner liest;
> 3. der **Anschluss der Speicherflotte**, die ihren Ersatz über die gleichnamigen **Felder des
>    Flottenstands** (`ErsatzintervallJahre`, `RestwertEuro` als JSON in `Tab_SpeicherAuslegung`) führt
>    — nicht über Spalten; der Anschluss berührt deshalb die **Einfrierregel** des Projekts 1046.

**Stück 1 erledigt mit E7c2 (#446):** Die Kennzeichen stehen je Position (A6), nullbar, als Schemaschritt 111 an
`Tab_ProjektWerte` und `Tab_KostenVorlagePosition`; „nein" streicht die Ersatzbeschaffung bzw. den Restwert, leer
und „ja" rechnen wie vorher; gepflegt im Zeileneditor „Position bearbeiten". Probe 1024 mit Nutzungsdauern:
−2.897.442,20 € ohne Kennzeichen, Wärmepumpe „Ersatz nein" −2.895.805,46 €, Kessel „Restwert nein"
−2.897.995,88 €. Im Konzept steht der Absatz „Ersatz und Restwert je Position — entkoppelt", die Liste führt die
zwei übrigen Stücke.

*§ 5, der Rest zu ET-D-3 und der Einheitenbruch (Z. 2173–2174 und 2197–2203):*

> Einheitenbruchs und die rechtlichen Unsicherheiten. Offen aus diesen Entscheiden ist ein Rest zu
> ET-D-3 (U32): Der Kartenzustand fällt weiterhin auf `ID_Umrechnung = -1` zurück.
>
> …
>
> **Der Schemaschritt ist noch nicht vergeben.** Der einst genannte **Schritt 62 ist anderweitig
> belegt** (`Schritt_62_KlimaWaisen`); U-1 steht aus und bekommt seine Nummer **bei der Umsetzung**
> (nächster freier Schritt heute **106** — 90–105 sind vergeben, 105 trägt K‑1; siehe Kopf). Gemessen am
> 19.09.2026: Die fünf Gase führen in `Tab_Brennstoff_Stamm.Einheit` unverändert `m³`;
> `energy_carrier.billing_unit` steht dagegen seit Schritt 26a auf `Nm³`. **Die Umsetzung ist
> freigegeben** (A9, → Register R‑A: vor dem nächsten Vorlagenbau) und läuft als DML-Schritt G mit E7
> des Etappenplans.

**Erledigt mit E7c2 (#446):** U32 — die Preisbasis steht als eigener Kartenzustand in
`energy_project_settings.Preisbasis` (Schemaschritt 112; Testdatenbank: 5 Zeilen „kWh", 23 mit der
Abrechnungseinheit), der Rückfall auf −1 entfällt für die Karte. U‑1 — Schemaschritt 113 zieht Einheit und
Preiseinheit der fünf Gase auf Nm³ und €/Nm³ (dazu eine Preiszeile, Projekt 1039), nach E7c2‑Q4 auch den
Brennstoff 24 auf kWh; der Vorlagenbau läuft danach ohne Auffälligkeit. Im Konzept stehen in § 5 der Satz „Der
Rest zu ET-D-3 (U32) ist erledigt" und der Absatz „Der Einheitenbruch ist behoben", die Regel der Preisbasis in
§ 2.5 und § 3.5.

*§ 6.3 Nr. 9h (Z. 2354–2358):*

> 9h. **Nutzungsdauer, Ersatz, Restwert — drei fehlende Stücke (Mockup-Anhang U39):** Entkopplung
>     von Ersatz und Restwert, die ungelesenen geräteeigenen Nutzungsdauer-Spalten, der Anschluss
>     der Speicherflotte — offen (E7/E10). Erledigt sind die Nachpflege des Bestands und der Pflegeort
>     der Positionsart (#357) sowie der Hinweis „T über Vorgabe, Position ohne Dauer" und die
>     Zeitraumzeile auf Seite und Bericht (E5, #434) — siehe Protokoll.

**Zum Teil erledigt mit E7c2 (#446):** die Entkopplung von Ersatz und Restwert (Schemaschritt 111, wie oben). Im
Konzept führt Nr. 9h die zwei übrigen Stücke — die geräteeigenen Dauerspalten und die Speicherflotte, vorgesehen
mit E7c3, wobei A7 und A8 beides an ND‑S3 binden — und die Entkopplung unter den erledigten.

### 8.10 Berichtigungen im gültigen Stand (#446)

Die Stellen, die mit E7c2 veraltet sind; „vorher" ist der Wortlaut vor #446 (Stand `41764ab0`). Je Stelle eine
Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `ea8e2a12`, `SchemaStand.Zielversion` = 105, „Schemaschritte 90–105 vergeben, neue ab **106**"; „Die Schritte 97 bis 101 und 103 gehören nicht diesem Feld … Diesem Feld gehören **102** …, **104** … und **105** …" | Codestand `41764ab0`, `Zielversion` = 113, 90–113 vergeben, neue ab 114; nicht diesem Feld auch 107 (Gebäudesimulation, E30) und 108 bis 110 (Kühlung KU1); 106 an Tabellen dieses Feldes, aus der Welle #444; diesem Feld dazu 111, 112 und 113 (#446) |
| § 2.2, Gruppe 2 | Wortlaut in § 8.9 | Absatz „Die Überlagerung „Sätze und Herkunft" trägt alle Wahlen und Sätze" mit drei Gruppen und „Leer heißt Vorschlag"; die Energiesteuer-Vorschau je Wahl mit E7c3 (E7c2‑Q8) |
| § 2.2, Gruppe 3 | „… Kohärenzzeile in Firebrick, wenn der erfasste Brennstoffpreis die Energiesteuer nicht ausweist." | dazu der Knopf „Wahl und Herkunft…" und die gesperrte Mischlage |
| § 2.5, Trägerkarte | „… und die gewählte Basis geht als `ID_Umrechnung` mit. Beim Öffnen mit gespeicherter Preisbasis wird die Anzeige umgerechnet." | „Die gewählte Basis ist ein eigener Kartenzustand" (`energy_project_settings.Preisbasis`, Schemaschritt 112; leer = Abrechnungseinheit, E7c2‑Q3; ohne Spalte der Grund) |
| § 2.13 (3) | Wortlaut in § 8.9; „Offen bleiben die drei Stücke oben (U39)." | Absatz „Ersatz und Restwert je Position — entkoppelt"; die Liste mit zwei Stücken; „Offen bleiben die zwei Stücke oben (U39); die Entkopplung … ist umgesetzt #446." |
| § 3.1, Nutzungsdauer, Ersatz, Restwert | Formelblock ohne Kennzeichen | je eine Zeile „nur wenn ErsatzFuehren ≠ nein" und „nur wenn RestwertAnsetzen ≠ nein", dazu ein Satz zur Entkopplung |
| § 3.4, Vorrangordnung Nr. 5 | „**Nur Konserve** bleiben `PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN` (Rest von Befund B-4)" | „**Projektweite Arten** … frisch aus dem jüngsten Lauf …", Bezugsgröße die Arbeitskosten (E7c2‑Q2), die Konserve nur, wo frisch nichts ermittelbar ist, mit Grund |
| § 3.5, Anzeigekante | „(€/m³, €/l, €/t): Wer einen Gaspreis pflegt, pflegt ihn je Kubikmeter." | „(€/Nm³, €/l, €/t) … je Normkubikmeter" |
| § 3.5, Preisbasis | „… stellen die `ID_Umrechnung` der Projektzeile; gerechnet wird mit H_i und H_s." | dazu der Satz zum Kartenzustand `energy_project_settings.Preisbasis` — eine Eingabehilfe ohne Rechenwirkung |
| § 3.6, Jahresreihe | — | Zeile „Vbh(A) = Bruttostrom(A) ÷ P_el(A); Fall 2(A): KWK-Strom(A) ÷ P_el(A)" |
| § 3.6, Hilfsstrom | „… und die Vollbenutzungsstunden bleiben **brutto**." | dazu „ausgenommen die Vollbenutzungsstunden einer Anlage im zweiten Fall des § 2 Nr. 16" |
| § 3.6, der zweite Fall | „Vollbenutzungsstunden und Kontingentverbrauch zählen bis E7c2 weiter nach dem Bruttostrom des Moduls … (Bau E7c2) … (… mit Hinweis — Bau E7c2)"; „— Fall 2 wirkt dann erst mit Wärmeüberschuss oder einem gepflegten kleineren σ"; „Proben an 1030 im Protokoll E7c1" | Absatz „Die Vollbenutzungsstunden in Fall 2 zählen aus dem KWK-Strom" samt E7c2‑Q7 und dem Rundungsgrund; „eine Kürzung entsteht dann erst …", dazu die Vollbenutzungsstunden nach dem Hilfsstromabzug (Beispielprojekt, von Hand: 32.022,2 → 33.800,2 €); Proben in den Protokollen E7c1 und E7c2 |
| § 3.6, Photovoltaik / EEG | „EV_mix = max(0, AW_mix − 0,40) nur ≤ 100 kW"; „§ 51a: im letzten Vergütungsjahr Ausfallarbeit_J1 × 0,5 × AW/100" | EV_mix ungerundet, Erlös auf Cent; § 51a mit dem Satz EV_mix bei fester Vergütung, sonst AW; Absatz „V‑1 und V‑2" |
| § 3.7 | Formelblock bis zur Kessel-Bemessung | Zeile „GESPERRT neben § 53/§ 53a"; Absatz „Die Mischlage ist gesperrt" |
| § 3.9 | Tafel ohne den Fall 5 | Zeile „5 Mischlage § 53/§ 53a neben § 54 — gesperrt" (Warnung) |
| § 3.10 | Schritte 7, 9, 10 und 11 ohne die Punkte aus E7c2 | Schritt 7 mit den Prozentarten, 9 mit den Vollbenutzungsstunden aus dem KWK-Strom, 10 mit der Sperre, 11 mit Ersatz und Restwert je Kennzeichen |
| § 4, B-4 · S-2 · V-1/V-2 | „B-4 \| Zwei Arten nie frisch: …"; „⚠ **S-2** \| Kein projektweites Doppelentlastungsverbot …"; „V-1 · V-2 · V-4 \| EV-Rundung (EvMix unrundet, Erlös gerundet) · § 51a bewertet mit AW statt EV · …" | „✔ **B-4** … **Erledigt mit E7c2 (#446)**"; „✔ **S-2** … **Erledigt mit E7c2 (#446)**"; „✔ **V-1** · ✔ **V-2** · V-4 … V‑1 und V‑2 erledigt mit E7c2 (#446) … V‑4 bleibt die benannte Näherung" |
| § 5 | Wortlaut in § 8.9; Randfragen mit „Brennstoff 24 „Sonstige"" | „Der Rest zu ET-D-3 (U32) ist erledigt …"; Absatz „Der Einheitenbruch ist behoben"; die Randfrage Brennstoff 24 mit der Einheit entschieden, offen H_i = H_s = 1,0 (E7c3) |
| § 6.1 | Kurztafel bis E7c1 (#440) | Zeile E7c2 (#446) |
| § 6.2 | „… und gehört zu **E7**. Bis dahin gilt der **gemessene** Wert als Anker." | „… gehört zu **E7c3**"; dazu: E7c1 (#440) und E7c2 (#446) haben keinen Anker bewegt |
| § 6.3 Nr. 9h | Wortlaut in § 8.9 | „zwei fehlende Stücke …, vorgesehen mit E7c3; A7 und A8 binden beides an ND‑S3"; die Entkopplung unter den erledigten Stücken |
| § 7 und Anhang | „… und E7 Teil c1 (#440) … Als Nächstes kommt **E7c2** (…)"; B8 „es läuft in E7c2 mit", „Beide Punkte laufen in **E7c2**"; Kürzeltafel B8 „offen (in **E7c2**)", 9h „#357, #434"; Etappenzeile „E7c2 … E12 — nächste Etappe: E7c2" | bis E7 Teil c2 (#446), als Nächstes E7c3, danach E8; B8: S‑2 erledigt #446, B‑6 in E7c3; Kürzeltafel mit „S-2 **#446**, B-6 offen (in **E7c3**)", 9h mit „#446 (Entkopplung)", der Zeile S‑2 · V‑1 · V‑2 · B‑4 · Schritte E, F, G = #446 und „U22 und U32 erledigt #446, U39 teilweise"; Etappenzeilen „E7 Teil c2" = #446 und „E7c3 … E12 — nächste Etappe: E7c3" |

### 8.11 E7c3 — Vollbenutzungsstunden nach Definition, B‑6, Kapitalwert 1024 und die Energiesteuer-Vorschau (#452)

Protokoll [`E7c3_Reste_B6_Kapitalwert_Protokoll.md`](E7c3_Reste_B6_Kapitalwert_Protokoll.md); der Stand von E7c1‑Q2
(präzisiert), E7c1‑Q6, E7c1‑Q8, E7c2‑Q4, Q5, Q7 und Q8, A7, A8 und Q2 im Register (R‑E7c1, R‑E7c2, R‑A, R‑Q), die acht
Fragen der Etappe — offen — unter R‑E7c3. Mit diesem Teil ist E7 abgeschlossen.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E7 Teil c3** (#452, Merge `9c7a0023`, Nachtrags-Merge `387c2d9f`) | Vbh = W_a ÷ P_Nenn (Definition des Anwenders vom 23.09.2026) in Fall 1 und Fall 2 — E7c2/7 zurückgebaut, E7c1‑Q2 b präzisiert, E7c2‑Q7 erledigt; `VpvCtKwh` ungerundet (E7c2‑Q5 b); Katalog-Generation 9 als Nachpflege — Brennstoff 24 H_i = H_s = 1,0, `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB` mit dem vierten Status ABGEKUENDIGT (E7c1‑Q8); Kapitalwert 1024 nachgerechnet (Datenstand, der Anker bleibt); B‑6 in den fünf Prioritätsdateien (100 leere Fänge benannt, strenger Leseweg an 26 Stellen, Warnzeilen, Gründe in Eigenschaften); die Energiesteuer-Vorschau je Wahl im Kern (E7c2‑Q8 b, Nachweisfassung 8); die Wahlen der Überlagerung als Anzeigezeilen (U22); § 6.3 Nr. 9h gemessen; 43 Ressourcenschlüssel neu, drei geändert; Testdatenbank auf Katalog-Generation 9 | **gewollt, im Bestand ohne Wirkung** — die dreizehn Basisprojekte 9.519 von 9.519 Werten gleich, die vier Anker unverändert; Proben im Protokoll E7c3 (1030 mit σ 0,5: Vbh 6.055,2 → 7.475,69 h/a, KWKG Jahr 1 7.316,03 → 6.137,94 € — die Werte vor E7c2/7); Referenzlauf gegen R12 13/13, 4 250 839 Werte, 399/399 byte-gleich; Gate #452 grün |

*§ 3.6, der zweite Fall des § 2 Nr. 16, die Vollbenutzungsstunden (Z. 1925–1944):*

> **Die Vollbenutzungsstunden in Fall 2 zählen aus dem KWK-Strom** (entschieden E7c1‑Q2, Lesart b, → Register
> R‑E7c1; umgesetzt #446): Vbh(A) = KWK-Strom(A) ÷ P_el(A), auf dem Ersatzweg der KWK-Strom der Gesamtanlage ÷ Σ P_el;
> Kontingentverbrauch und Jahresdeckel laufen über diese Stunden, je eine Hinweiszeile nennt sie
> (`WIRT_KWKG_FALL2_VBH`, `WIRT_KWKG_FALL2_VBH_ERSATZ`), und der Nachweis führt sie in `VbhElektrisch`. Ohne
> Kennzeichen (Fall 1) bleiben die Vollbenutzungsstunden brutto. Bindet der Jahresdeckel, entfällt die Kürzung bis
> auf den Mischsatz: Bezahlt werden Deckel × P_el zum Satz der gekürzten Mengen, bei gleichen Sätzen ist das der
> Zuschlag ohne Kürzung (Deckel × P_el × Satz); die Reihe bleibt bei zwölf Jahren (entschieden E7c2‑Q7, nach
> Empfehlung, → Register R‑E7c2). Bindet der Deckel nicht, reicht das Kontingent länger, und die Reihe wird länger.
> …
> Die Vollbenutzungsstunden zählen dagegen schon ohne Kürzung aus dem KWK-Strom, also nach dem Hilfsstromabzug; bei
> bindendem Deckel steigt damit der Jahresbetrag (Beispielprojekt, von Hand gerechnet: Jahr 1 32.022,2 € in Fall 1,
> 33.800,2 € in Fall 2 ohne Kürzung — Rechenweg 05). …

**Berichtigt mit E7c3 (#452):** Der Anwender hat am 23.09.2026 (17:10, mit Anlage) die Vollbenutzungsstunden
definiert — „Vollbenutzungsstunden (siehe Anlage) — gehe nach dieser Definition": Vbh = W_a ÷ P_Nenn, die erzeugte
Arbeit brutto durch die Nennleistung, in Fall 1 und Fall 2 gleich. E7c2/7 ist zurückgebaut (E7c3/5): Kontingent und
Deckel zählen wieder die Bruttostunden, der KWK-Strom bestimmt allein die bezahlte Menge; E7c2‑Q7 hat sich erledigt,
und die Folge „bei bindendem Deckel steigt der Jahresbetrag" (33.800,2 statt 32.022,2 €) gibt es nicht mehr. Im
Konzept steht der Absatz „Die Vollbenutzungsstunden zählen in beiden Fällen brutto: Vbh = W_a ÷ P_Nenn".

*§ 6.2, der Kapitalwert 1024 (Z. 2407–2413):*

> **Zwei Abweichungen zum bisherigen Konzepttext, beide als Befund festgehalten (#380):** Die
> Kaskadenprobe 1042 ergibt ±0,00 € statt +20.927,61 € — die drei Prozentzeilen des Projekts tragen im
> heutigen Datenstand keinen Einheitpreis. Der Kapitalwert 1024 liegt mit −2.896.359,13 € um
> **−676.036,81 €** unter dem Konzeptwert; die Abweichung ist eingegrenzt, aber nicht nachgerechnet
> (Kandidaten: Kesselbrennstoff B‑1/#331, Hilfsstrom #365/#366, Schemaschritte 93–96) und gehört zu
> **E7c3**. Bis dahin gilt der **gemessene** Wert als Anker; die Etappen E7c1 (#440) und E7c2 (#446) haben keinen
> Anker bewegt.

**Erledigt mit E7c3 (#452):** nachgerechnet — −676.495,37 € aus Schemaschritt 83 (#313; 11,746 ct/kWh
Strompreisanteile des Stromträgers 60 in den Arbeitspreis gefaltet, 35,000 → 46,746 ct/kWh), +458,56 € aus der
Übernahme Access → SQLite am 02.09.2026; die drei Kandidaten tragen 0,00 € bei, mit 35,000 ct/kWh rechnet der Kern
bitgleich −2.219.863,76 €. Der Anker bleibt (E7c3‑Q1 offen, gebaut ist Lesart a). Im Konzept stehen der Befund im
Absatz und in der Ankertafel, dazu die Zeile „mit dem Strompreis vor Schritt 83".

*§ 6.3 Nr. 9h (Z. 2462–2467):*

> 9h. **Nutzungsdauer, Ersatz, Restwert — zwei fehlende Stücke (Mockup-Anhang U39):** die ungelesenen
>     geräteeigenen Nutzungsdauer-Spalten und der Anschluss der Speicherflotte — offen (vorgesehen mit
>     E7c3; A7 und A8 binden beides an ND‑S3, E10). Erledigt sind die Nachpflege des Bestands und der
>     Pflegeort der Positionsart (#357), der Hinweis „T über Vorgabe, Position ohne Dauer" und die
>     Zeitraumzeile auf Seite und Bericht (E5, #434) sowie die Entkopplung von Ersatz und Restwert
>     (E7c2, #446, Schemaschritt 111, § 2.13 (3)) — siehe Protokoll.

**Gemessen mit E7c3 (#452), nicht gebaut:** Nr. 9h bleibt offen mit ND‑S3 (E10). Im Konzept stehen die Messung
(`Tab_BHKW` 3 von 6, Stamm 44 von 79; `Tab_Heizkessel` 1 von 22, Stamm 0 von 63 — beide ohne Leser;
`Tab_StromspeicherVariante` 13 von 13; die Nutzungsdauer-Tabelle mit BHKW-Modul 15 a und Batterie 10 a; die Flotte
von 1046 mit Ersatzintervall 10 a und Restwert 500 bzw. 300 €) und der Vorschlag für ND‑S3.

### 8.12 Berichtigungen im gültigen Stand (#452)

Die Stellen, die mit E7c3 veraltet sind; „vorher" ist der Wortlaut vor #452 (Stand `9c7a0023`). Je Stelle eine
Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `41764ab0` | Codestand `387c2d9f`; dazu „Gesetzeskatalog Generation 9 (Nachpflege ohne Schemaschritt, § 3.6)" |
| § 2.2, Gruppe 2 | „(… umgesetzt #440 und #446)"; die Energiesteuer „je mit ihrer Wirkung"; „Satz und Betrag der Energiesteuer zeigt die Überlagerung für die gebuchte Wahl, die übrigen Wahlen als Text; eine Vorschau je Wahl aus dem Kern ist entschieden (E7c2‑Q8, Lesart b, → Register R‑E7c2) und folgt mit E7c3." | „umgesetzt #440, #446 und #452"; jede Wahl als eine Zeile mit ihrer Wirkung (U22); Satz und Betrag im ersten Jahr für jede Wahl aus der Vorschau des Laufs (§ 3.7), ohne Vorschau der Text der Vorschrift; die Klapplisten des Formulars bleiben (E7c3‑Q7, offen) |
| § 3.6, Jahresreihe | „Vbh(A) = Bruttostrom(A) ÷ P_el(A) ;  Fall 2(A): KWK-Strom(A) ÷ P_el(A) (E7c1‑Q2 b, unten)" | „Vbh(A) = W_a ÷ P_Nenn = erzeugte Arbeit brutto(A) ÷ P_el,Nenn(A) in Fall 1 und Fall 2 (unten)" |
| § 3.6, Prüfkette | — | Absatz „Die zwei Katalogzeilen der alten Frist sind abgekündigt" mit der Katalog-Generation 9 (E7c3‑Q2, E7c3‑Q3 offen) |
| § 3.6, Hilfsstrom | „… bleiben **brutto** — ausgenommen die Vollbenutzungsstunden einer Anlage im zweiten Fall des § 2 Nr. 16: Sie zählen aus ihrem KWK-Strom (unten)." | „… bleiben **brutto** — auch bei einer Anlage im zweiten Fall des § 2 Nr. 16 (Vbh = W_a ÷ P_Nenn, unten)." |
| § 3.6, der zweite Fall | „… sieben nullbare Felder, die Fassung des Nachweisumschlags bleibt 7 (entschieden E7c1‑Q6, nach Empfehlung)."; Wortlaut in § 8.11 | „… sieben nullbare Felder ohne eigene Fassung …; die Fassung des Nachweisumschlags ist 8, seit er die Energiesteuer-Vorschau trägt (§ 3.7)"; Absatz „Die Vollbenutzungsstunden zählen in beiden Fällen brutto: Vbh = W_a ÷ P_Nenn" |
| § 3.6, Photovoltaik / EEG | „Der Satz der Speicherbewertung (`VpvCtKwh`) nimmt noch den gerundeten Mix; ungerundet folgt er mit E7c3 (E7c2‑Q5, Lesart b, → Register R‑E7c2)." | denselben ungerundeten Satz wie die Erlösreihe (umgesetzt #452), mit den Proben 100 kWp und 750 kWp |
| § 3.7 | Formelblock und Absatz „Die Mischlage ist gesperrt" | dazu Absatz „Die Vorschau je Wahl" mit der Handprobe des Rechenwegs 05 |
| § 3.9 | Tafel bis „Anlagenart fehlt" | Zeilen „Prüfung nicht ausführbar" und „Rechenstufe nicht ausführbar" (Warnung), Absatz „Der Grund statt der stillen Null" |
| § 3.10 | Schritt 9 „… Jahresreihe mit Vbh (Fall 2: aus dem KWK-Strom)/Deckel/Restkontingent"; Schritte 10 und 12 ohne Vorschau und Warnzeile | Schritt 9 „… Vbh (brutto, W_a ÷ P_Nenn) …", Schritt 10 mit der Vorschau je Anlage und Wahl, Schritt 12 mit den Warnzeilen gescheiterter Stufen (B‑6) |
| § 4, B-6 | „B-6 \| Fehler werden geschluckt (`catch {}` ⇒ still 0)." | „**B-6** … Für die fünf Prioritätsdateien erledigt mit E7c3 (#452) … Offen: 29 leere `catch` in 13 weiteren Dateien und fünf stille Lesestellen im Engine-Modus (E7c3‑Q5 …)" |
| § 5 | „… sein Stamm trägt H_i = H_s = 0, eine Preisumrechnung gibt es deshalb nicht …"; Randfrage „offen bleibt H_i = H_s = 1,0 wie bei Strom und Fernwärme, E7c3"; R‑U5 ohne die abgekündigten Zeilen | H_i = H_s = 1,0 mit der Katalog-Generation 9 (#452); R‑U5 mit „die zwei Katalogzeilen der alten Frist tragen den Status ABGEKUENDIGT" |
| § 6.1 | Kurztafel bis E7c2 (#446) | Zeile E7c3 (#452); in der Zeile E7c2 „(mit E7c3 zurückgebaut)" |
| § 6.2 | „`WirtschaftlichkeitAnkerTests` (9 Fälle)"; Zeile Kapitalwert 1024 „gemessen (#380) — das Konzept führte **−2.220.322,32 €**"; Wortlaut in § 8.11 | 10 Fälle; die Zeile mit dem Befund, dazu die Zeile „mit dem Strompreis vor Schritt 83 −2.219.863,76 €"; der Absatz mit dem nachgerechneten Befund |
| § 6.3 Nr. 9h | Wortlaut in § 8.11 | „offen mit ND‑S3"; die Messung und der Vorschlag für ND‑S3 |
| § 7 und Anhang | „… und E7 Teil c2 (#446) … Als Nächstes kommt **E7c3** (…) … Danach **E8**. … B‑6 (es läuft in E7c3 mit)"; B8 „B‑6 läuft in **E7c3** des Etappenplans mit"; Kürzeltafel B8 „B-6 offen (in **E7c3**)", 9h bis „#446 (Entkopplung, Schritt 111)", „U22 und U32 erledigt #446, U39 teilweise"; Etappenzeile „E7c3 … E12 — nächste Etappe: E7c3" | bis E7 Teil c3 (#452), E7 abgeschlossen, als Nächstes E8 mit seinem Inhalt, danach der B‑6-Rest und Q6 nach Entscheid; B8 „B‑6 … erledigt mit E7c3 (#452) …, der Rest ist offen (E7c3‑Q5)"; Kürzeltafel „B-6 **#452** (fünf Dateien …)", 9h mit „#452 (gemessen)", neue Zeile = #452, „U22 erledigt #446 und #452 (Anzeigezeilen)"; in den Zeilen zu #446 „Vbh aus dem KWK-Strom (zurückgebaut #452)"; Etappenzeilen „E7 Teil c3 — Reste" = #452 und „E8 … E12 — nächste Etappe: E8" |

### 8.13 E8a — die ValERI-Ansicht vollständig (#454)

Protokoll [`E8a_ValERI_Bloecke_Protokoll.md`](E8a_ValERI_Bloecke_Protokoll.md); der Stand von E6‑Q1 (R‑E6), E5b‑4
(R‑E5), V‑1 (R‑V) und EZ‑2 im Register, die vier Fragen der Etappe — entschieden 23.09.2026 nach Empfehlung — unter
R‑E8a. Teil b der Etappe (V‑D: Formelbericht, Anhang-E-Checkliste, Anhang-D-Gegenprobe) folgt mit #455.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E8 Teil a** (#454, Merge `485052c6` auf dem Hilfszweig `pm5`) | V‑C vollständig: Block 2 „Zahlungsreihen" je Stand und Szenario aus der neuen Kern-Klasse `Zahlungsgliederung` (sechs Bestandteile, Netto, Barwert, Summe nominal), darunter das Zahlungsstrombild (E8a‑Q1 a); Block 4 mit Spannenbild und Verlauf (E6‑Q1); die Gliederung „Woraus entsteht die Zahl?" mit Nominalsumme und Differenzspalte (U46); das Brückenbild auf der Seite und im Wortbericht (U41); die Tafel „Was daraus im Lauf wird" (U47); die Fußzeile zur Herkunft der Annahmen (U48); 43 Ressourcenschlüssel neu, einer geändert; 14 ChartProben-Bilder neu | **keine** — alles ist Ausgabe; die Ankertests unverändert, Referenzlauf gegen R13 13/13, 4 145 687 Werte, 387/387 byte-gleich; Gate auf dem Gesamtstand mit #455 (`704356a4`) grün |

*§ 2.10, Andockvorschlag, die zwei Zeilen zur ValERI-Darstellung (Z. 711–712):*

> | Die fünf ValERI-Blöcke (Investition · Betrieb · Erlöse · Energie · Wirtschaftlichkeit über Nutzungsdauer) | als
> zweite Ansicht der Seite hinter dem Umschalter „Kennzahlen / ValERI-Bewertung" — **V‑1 entschieden** (→ Register
> R‑V), umgesetzt #434 mit den Blöcken 1, 3, 4 und 5 |
> | Kumulierter diskontierter Cashflow | als Abschnitt „Verlauf" in „Wie sicher ist das?" der Darstellung
> „Kennzahlen", mit allen drei Szenarien (§ 2.13 (5), umgesetzt #436); einen eigenen Verlaufsdialog gibt es nicht. Ob
> Block 4 der Darstellung „ValERI-Bewertung" ihn ebenfalls zeigt, ist Frage E6‑Q1 (→ Register R‑E6) |

**Umgesetzt mit E8a (#454):** die fünf Blöcke vollständig, Block 4 mit Spannenbild und Verlauf. Im Konzept stehen
„vollständig #454 mit Block 2 „Zahlungsreihen" samt Zahlungsstrombild und Block 4 mit Spannenbild und Verlauf" und
„Block 4 … zeigt denselben Abschnitt samt Spannenbild (E6‑Q1 …; umgesetzt #454)".

*§ 2.11.4, die Zeile V‑C (Z. 808):*

> | **V-C** | ValERI-Ansicht (fünf Blöcke + Cashflow-Chart) in der Wirtschaftlichkeitsseite | teilweise vorgezogen mit
> **#434**: die Darstellung „ValERI-Bewertung" hinter dem Umschalter mit den Blöcken 1, 3, 4 und 5; das Cashflow-Bild
> steht mit **#436** als Verlauf mit drei Szenarien unter „Wie sicher ist das?" der Darstellung „Kennzahlen" (ob auch
> in Block 4: Frage E6‑Q1, → Register R‑E6); offen Block 2 (Zahlungsreihen, an seiner Stelle eine Hinweiszeile) |
> Ausweis | **E8** |

**Umgesetzt mit E8a (#454):** Im Konzept steht die Zeile mit dem gebauten Inhalt — Block 2 samt Zahlungsstrombild,
Leitversion, Block 4, Gliederung mit Nominalsumme und Differenzspalte, Brückenbild, „Was daraus im Lauf wird",
Fußzeile, die Regel „alles ist Ausgabe" und die Jahresreihen nach einem Lauf in der Sitzung (E8a‑Q3); Stand „**E8**
Teil a — gebaut".

*§ 2.13 (5), der letzte Satz (Z. 1156–1157):*

> Offen ist Frage E6‑Q1 — Verlauf und Spannenbild auch in Block 4 der Darstellung „ValERI-Bewertung" (→ Register
> R‑E6).

**Umgesetzt mit E8a (#454):** Im Konzept steht der Absatz „Block 4 der Darstellung „ValERI-Bewertung" zeigt
Spannenbild und Verlauf mit denselben Bausteinen … je Darstellung steht genau ein Verlauf".

### 8.14 Berichtigungen im gültigen Stand (#454)

Die Stellen, die mit E8a veraltet sind; „vorher" ist der Wortlaut vor #454 (Stand `485052c6`). Je Stelle eine
Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `387c2d9f` | Codestand `485052c6`; dazu „Referenzbasis `2026-09-23_R13_Kuehlung`" |
| § 2.10 | Wortlaut in § 8.13 | die fünf Blöcke vollständig #454; Block 4 mit demselben Verlauf (E6‑Q1, umgesetzt #454) |
| § 2.11.3 | Tafel der fünf Darstellungsblöcke nach Kostenarten, ohne Brücke zur Nummerierung 1–5 der Seite | dazu die Fußnote „Zur Nummerierung" — zwei Ordnungen desselben Inhalts, gebaut ist 1 bis 5 |
| § 2.11.4 | V‑C: Wortlaut in § 8.13; V‑D „**E8**"; Fußnote „gebaut sind daraus E0 … E5 (#434) und E6 (#436)" | V‑C mit dem gebauten Inhalt, „**E8** Teil a — gebaut"; V‑D „**E8** Teil b"; Fußnote bis „E7 (#437, #439, #440, #446, #452) und E8 Teil a (#454)" |
| § 2.13 (5) | Wortlaut in § 8.13 | Absatz „Block 4 der Darstellung „ValERI-Bewertung" zeigt Spannenbild und Verlauf …" |
| § 6.1 | Kurztafel bis E7c3 (#452) | Zeile E8a (#454) |
| § 7 und Anhang | „… und E7 Teil c3 (#452) — E7 ist damit abgeschlossen … Als Nächstes kommt **E8**: die fünf ValERI-Blöcke vollständig (…), der Formelbericht …, die Anhang-E-Checkliste (U43), die Anhang-D-Gegenprobe, Nominalsummen, Differenzspalte und Brückenbild …, „Was daraus im Lauf wird" und die Fußzeile (U41, U46 bis U48) und E6‑Q1 …"; Kürzeltafel V‑A…V‑E „V-A = **#434**, sonst keine …", „V-C/V-D = **E8** (die Blöcke 1, 3, 4, 5 … mit #434 vorgezogen)", § 2.13 bis „**#436**", Mockup-Anhang bis „U39 teilweise (…)"; Etappenzeile „E8 … E12 — nächste Etappe: E8" | bis E8 Teil a (#454) mit seinem Inhalt, als Nächstes E8 Teil b (V‑D) mit der Fußzeile in der Knopfreihe (E8a‑Q4), danach E9; Kürzeltafel „V-C = **#454**", „V-C = **E8** Teil a (gebaut #454 …), V-D = **E8** Teil b", § 2.13 mit „**#454** … beide auch in Block 4 (E6‑Q1)", neue Zeile = #454, „U41, U42 und U46 bis U49 erledigt #454"; Etappenzeilen „E8 Teil a — V‑C" = #454 und „E8 Teil b … E12 — nächste Etappe: E8 Teil b" |

### 8.15 E8b — Formelmappe, Anhang-E-Checkliste und Anhang-D-Gegenprobe (#455)

Protokoll [`E8b_Formelmappe_AnhangE_D_Protokoll.md`](E8b_Formelmappe_AnhangE_D_Protokoll.md); der Stand von V‑G10 und
V‑2 (R‑V) und von Q18 (R‑Q) im Register, die sechs Fragen der Etappe — offen, gebaut jeweils Lesart a, Q5 und Q6
erledigt — unter R‑E8b. Mit #455 ist E8 abgeschlossen.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E8 Teil b** (#455, Merge `704356a4` auf dem Hilfszweig `pm5`) | V‑D: die Blattstruktur-Wache über Excel- und Wortbericht und der ClosedXML-Befund als Wache; die Formelmappe in den Stufen 0 bis 3 (Parameterblock aus echten Zellen mit Namen, Mehrjahrestabellen und Kennzahlen des Erwartungsfalls in Formeln, bemessene Betriebskosten als Menge × Satz, der Δ%-Block als Zellbezug) — EPOS trägt die Werte ein, Excel rechnet beim Öffnen neu; die Anhang-E-Checkliste (U43) als Abschlussseite beider Berichte und hinter einem Knopf der Ergebnisseite; die Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne`; die Fußzeile in der Knopfreihe (E8a‑Q4); 103 Ressourcenschlüssel neu | **keine** — die Formeln geben die Werte der Wertfassung wieder (13 Prüfgruppen), die Ankertests unverändert, Referenzlauf gegen R13 13/13, 4 145 687 Werte, 387/387 byte-gleich; Gate auf `704356a4` grün |

*§ 2.11.2, die Zeilen V-G10 und V-G12 der Gap-Tafel (Z. 775 und 777):*

> | V-G10 | **Bericht** mit Pflichtinhalten a)–d) + **editierbarer XLSX mit Formeln** nach Anhang-A-Raster (9) | Excel-Export
> existiert (ClosedXML), aber als **Werte** — der Generator schreibt keine einzige Formel, und keine Zahl des
> Parametersatzes erreicht eine Zelle (gemessen 18.09.2026) | **größte Einzellücke mit hartem Muss**. **Entschieden
> 18.09.2026, abweichend von der Empfehlung: der ganze Bericht formelbasiert**, soweit ableitbar — Stufenplan und die
> Liste dessen, was dauerhaft Wert bleibt, in § 2.11.6; das ValERI-Blatt (Parameterblock mit absoluten Bezügen,
> Periodenspalten, Gesamt-/Barwert-/NPV-Zeile je Szenario) ist darin Stufe 0 und 1 |
> | V-G12 | **Anhang-E-Checkliste** (15 Punkte, Note 1–5) | fehlt | als Abschlussseite des Berichts; zugleich interne
> Abnahmecheckliste der Etappe |

**Umgesetzt mit E8b (#455):** Beide Zeilen tragen die Marke „**Stand: gebaut #455**" mit dem gebauten Inhalt; die
Spalte „EPOS heute" bleibt als Messung vom 18.09.2026 stehen, wie bei V‑G6, V‑G8 und V‑G9.

*§ 2.11.2, der Absatz zu Anhang D (Z. 779–782):*

> **Anhang D der Norm ist eine BHKW-Fallstudie** (90 kW_th, 18 Jahre, NPV 64.480 €, Worst −202.802 €,
> Best +598.320 €) — sie dient der Etappe als **externe Gegenprobe**: EPOS muss mit denselben
> Eingaben dieselben Zahlen treffen. *(Vorsicht: Zwei Zeilen der Sensitivitätstabelle D.6 tragen im
> Normtext versehentlich Werte des Pumpenbeispiels — als Prüfreferenz ungeeignet, dokumentiert.)*

**Umgesetzt mit E8b (#455):** Der Absatz bleibt und führt dahinter die Gegenprobe als Kern-Fall — die Zahlen des
Rechenkerns (64.479,51 €, −202.801,57 €, 598.319,65 €), die Tafeln D.5 und D.6, die Umrechnung Basis × (1 + p) und
die Ausnahmen („Gasverbrauch BHKW", der Tippfehler in D.7).

*§ 2.11.4, die Zeile V-D (Z. 818):*

> | **V-D** | XLSX-Formelbericht nach Anhang-A-Raster + Berichtsinhalte a)–d) + Anhang-E-Checkliste; **Gegenprobe an der
> Anhang-D-Fallstudie** | deckt sich mit **V-G10** (Entscheid 18.09.2026, § 2.11.6) | Ausgabe | **E8** Teil b |

**Umgesetzt mit E8b (#455):** Im Konzept steht die Zeile mit dem gebauten Inhalt — Formelmappe samt beiden Wachen,
Anhang-E-Checkliste, Anhang-D-Gegenprobe, die Regel „alles ist Ausgabe" und die Werte von Günstig und Ungünstig
(E8b‑Q1); Stand „**E8** Teil b — gebaut".

*§ 2.11.6, der letzte Absatz (Z. 910–913):*

> Vor Stufe 0 zu klären: ob die eingesetzte ClosedXML-Fassung Formeln mit zwischengespeichertem
> Ergebnis ablegt oder Excel beim Öffnen rechnen muss, und ob eine Formelmappe in anderen
> Tabellenkalkulationen dieselben Werte zeigt. Im Bestand deckt kein Test den Excel- und den Word-Generator ab —
> die Stufen brauchen zuerst eine Wache über beide Blattstrukturen.

**Beantwortet mit E8b (#455):** Die Fragen sind gemessen (Protokoll E8b, „Der ClosedXML-Befund und der Entscheid");
im Konzept steht an dieser Stelle die Regel „EPOS trägt die Werte ein, Excel rechnet neu" mit den zwei Wachen, dazu
über dem Absatz „Dauerhaft Werte bleiben" der Abschnitt „Was die Mappe trägt". Der Satz „Im Bestand deckt kein Test
…" traf seit E1 (#380) nur noch zur Hälfte: Die Blattstruktur-Wache rief beide Generatoren, hielt aber die Stellen
der Stufen nicht fest.

### 8.16 Berichtigungen im gültigen Stand (#455)

Die Stellen, die mit E8b veraltet sind; „vorher" ist der Wortlaut vor #455 (Stand `09037a32`). Je Stelle eine
Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `485052c6` | Codestand `704356a4` |
| § 2.10 | Zeile „ValERI-Bewertungsbericht (Anhang E der Norm)" nur als Baustein der Bericht-Seite | dazu die Anhang-E-Checkliste als Abschlussseite, letztes Blatt der Mappe und hinter dem Knopf „Anhang-E-Checkliste…" (V‑G12, U43; umgesetzt #455) |
| § 2.11.2 | V‑G10 und V‑G12 ohne Stand; Absatz zu Anhang D: Wortlaut in § 8.15 | beide Zeilen mit „Stand: gebaut #455"; der Absatz mit der Gegenprobe, ihren Zahlen und Ausnahmen |
| § 2.11.4 | V‑C „… die **Fußzeile** … (U48)"; V‑D: Wortlaut in § 8.15; Fußnote „… und E8 Teil a (#454)" | V‑C „(U48; mit #455 links in der Reihe der Knöpfe „Anhang-E-Checkliste…" und „Bericht erzeugen", E8a‑Q4)"; V‑D mit dem gebauten Inhalt, „**E8** Teil b — gebaut"; Fußnote „… und E8 (#454, #455)" |
| § 2.11.6 | Einleitung ohne Stand; letzter Absatz: Wortlaut in § 8.15 | „Gebaut sind alle vier Stufen mit #455"; der neue Abschnitt „Was die Mappe trägt" (Stufen 0 bis 3, keine Formel ohne Gegenrechnung); der Absatz „EPOS trägt die Werte ein, Excel rechnet neu"; „Dauerhaft Werte bleiben" und die drei Sätze unverändert |
| § 6.1 | Kurztafel bis E8a (#454) | Zeile E8b (#455) |
| § 6.2 | „… die Blattwache `BerichtBlattstrukturWacheTests` (5) und die Formatwache `WirtZeileFormatWacheTests` (4)." | die Blattwache (13), die Formatwache (4), die Befundwache `FormelmappeClosedXmlBefundTests` (2) und die Gegenprobe `AnhangDFallstudieTests` (10); dazu die Ankerzeile „Fallstudie DIN EN 17463, Anhang D" |
| § 7 und Anhang | „… und E8 Teil a (#454) — E7 ist damit abgeschlossen, von E8 die ValERI-Ansicht (V‑C) … Als Nächstes kommt **E8 Teil b** (V‑D): … Danach **E9**. Die acht Fragen aus E7c3 sind offen …"; Kürzeltafel „V-A = **#434**, V-C = **#454**" und „V-D = **E8** Teil b", Mockup-Anhang bis „U41, U42 und U46 bis U49 erledigt #454"; Etappenzeile „E8 Teil b … E12 — nächste Etappe: E8 Teil b" | bis E8 Teil b (#455), E7 und E8 abgeschlossen, als Nächstes E9 mit den Schritten B bis D; offen die Fragen aus E7c3 und E8b, dazu die zwei kleinen Aufträge zu E8b‑Q2 und E8b‑Q3; Kürzeltafel „V-D = **#455**" und „V-D = **E8** Teil b (gebaut #455)", neue Zeile = #455, „U12 und U43 erledigt #455"; Etappenzeilen „E8 Teil b — V‑D" = #455 und „E9 … E12 — nächste Etappe: E9" |

### 8.17 E8c — Bemessungstexte und Gliederungsprobe der Betriebskosten (#460)

Protokoll [`E8c_Bemessungstexte_Startjahr_Protokoll.md`](E8c_Bemessungstexte_Startjahr_Protokoll.md); E8b‑Q2 und
E8b‑Q3 stehen im Register unter R‑E8b als erledigt, die zwei Fragen der Welle — offen, gebaut jeweils Lesart a — unter
R‑E8c. Die Mini-Welle baut die zwei kleinen Aufträge, die der Anwender am 23.09.2026 aus E8b beschlossen hat („Fragen
aus E8b: Empfehlung").

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E8c** (#460, Merge `9ab55946` auf dem Hilfszweig `pm7`) | E8b‑Q2: die Spalte „Bemessung" der Betriebskostentabelle aus dem Bemessungskatalog (alle 18 Steuerwerte, gewerkeigene Beschriftung), `BetriebskostenCtrl.Bemessungsfaktor` als gemeinsame Frage von Herleitung und Formelmappe (Stufe 3); E8b‑Q3 (Lesart b): die Probe der Gliederung nur mit den Positionen des ersten Jahres, „ab Jahr X" in der Herleitungsspalte, Nachweisumschlag Fassung 9; der Kommentar zu U42; fünf Ressourcenschlüssel gestrichen, einer neu, zwei neu gefasst | **keine** — Zellvergleich über 15 Prüfgruppen: geändert nur die Spalte „Bemessung" (54 Zellen), die Warnzeilen und der Hinweistext; die Ankertests unverändert, Referenzlauf gegen R13 13/13, 4.145.687 Werte in der Toleranz; Gate auf `9ab55946` grün |

*§ 2.11.6, „Was die Mappe trägt", die Stufe 3 (Z. 921–923):*

> - **Stufe 3** — Menge und Satz einer bemessenen Position rechts des Betrags, der Betrag ihr Produkt (bei
>   Prozentbemessung ÷ 100, ein Erlös negativ; welche Rechnung gilt, sagt die Bemessung selbst,
>   `BetriebskostenCtrl.Betrag`); die Summe als Spaltensumme; der Δ%-Block als (Wert − Stamm) / |Stamm| · 100.

**Umgesetzt mit E8c (#460):** Die Stufe fragt `BetriebskostenCtrl.Bemessungsfaktor` — dieselbe Frage wie die
Herleitungsspalte —, für jede der 16 bemessenen Arten; die Spalte „Bemessung" nennt jede Art mit dem Text des
Bemessungskatalogs. Das Verhalten der Mappe ist gleich: Menge × Satz hatte sie schon für alle bemessenen Arten.

*§ 3.6, der Satz zur Fassung des Nachweisumschlags (Z. 1999–2000):*

> … die Fassung des Nachweisumschlags ist 8, seit er
> die Energiesteuer-Vorschau trägt (§ 3.7).

**Umgesetzt mit E8c (#460):** Fassung 9 — 8 mit der Energiesteuer-Vorschau, 9 mit dem Startjahr je
Betriebskostenposition (§ 3.4).

*§ 7, die Sätze zur nächsten Etappe (Z. 2706–2713):*

> Als Nächstes kommt **E9** (V‑E, die
> Szenarioabdeckung nach § 2.11.5): die Schemaschritte B (Betrachtungszeitraum und Mengenfaktor je Szenario), C
> (Trägerpreise best/worst) und D (Erlössätze best/worst) mit ihren Nummern bei der Umsetzung, der ±-Knopf an den neuen
> Orten, der Kern liest die Paare, der Hinweistext (§ 2.11.7) entfällt; ohne Degradation (A5), rechenwirksam je Pflege.
> Offen sind die acht Fragen aus E7c3 (→ Register R‑E7c3); die sechs aus E8b sind entschieden (23.09.2026, nach
> Empfehlung, → Register R‑E8b). Nach dem Entscheid aus E7c3 kommen der Rest von B‑6 (E7c3‑Q5) und die Anzeige der drei
> Kerneigenschaften `Ladefehler`, `Speicherfehler`, `Vorsorgewarnung` (E7c3‑Q6) dazu; aus E8b kommt **E8c** mit den
> zwei kleinen Aufträgen zu E8b‑Q2 und E8b‑Q3.

**Umgesetzt mit E8c (#460):** E8c ist gebaut; E9 läuft in zwei Wellen — E9a mit den Schemaschritten 116, 117 und 118
(Vergabe 24.09.2026), danach E9b; offen sind außerdem die zwei Fragen aus E8c.

### 8.18 Berichtigungen im gültigen Stand (#460)

Die Stellen, die mit E8c und der Schemaschritt-Vergabe vom 24.09.2026 veraltet sind; „vorher" ist der Wortlaut vor
#460 (Stand `9ab55946`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Stand 23.09.2026, Codestand `704356a4`, `SchemaStand.Zielversion` = 113, „Schemaschritte 90–113 vergeben, neue ab **114**"; fremd „die Schritte 97 bis 101, 103 und 107 bis 110" | Stand 24.09.2026, Codestand `9ab55946`, Zielversion 114, „90–114 vergeben, 115 zugesagt, 116–118 an E9a vergeben"; fremd dazu 114 (Kühlung KU2, KU-S3) und der zugesagte 115 (Zapfprofil T2); ein Satz zur Vergabe von 116 bis 118 an die Schritte B, C und D |
| § 2.11.4 | Fußnote „… und E8 (#454, #455)" | „… und E8 (#454, #455; die Nachbesserung E8c #460)" |
| § 2.11.6 | Stufe 3: Wortlaut in § 8.17; „in allen 13 Prüfgruppen gleicht die Formelfassung der Wertfassung" | Stufe 3 mit `Bemessungsfaktor` und den Katalogtexten; dazu „mit #460 über 15 Prüfgruppen nachgemessen …" |
| § 3.4 | kein Absatz zur Betriebskostentabelle der Berichte | neuer Absatz „Die Betriebskostentabelle der Berichte" — Bemessung, Herleitung, Startjahr, Probe der Gliederung, Nachweis |
| § 3.6 | Wortlaut in § 8.17 | „… die Fassung des Nachweisumschlags ist 9 — 8 mit der Energiesteuer-Vorschau (§ 3.7), 9 mit dem Startjahr je Betriebskostenposition (§ 3.4, #460)." |
| § 6.1 | Kurztafel bis E8b (#455) | Zeile E8c (#460) |
| § 6.2 | „… die Befundwache `FormelmappeClosedXmlBefundTests` (2) und die Gegenprobe an der Norm `AnhangDFallstudieTests` (10 …)." | dazu die zwei Klassen der Betriebskostentabelle `BemessungstexteAlleArtenTests` (21) und `BetriebskostenStartjahrGliederungTests` (12) |
| § 7 und Anhang | § 7: Wortlaut in § 8.17; Kürzeltafel bis #455; Etappenzeilen „E8c … offen — Entscheid 23.09.2026, nach Empfehlung" und „E9 … E12 — nächste Etappe: E9 (offen die acht Fragen aus E7c3 …; E8b entschieden …)" | § 7 mit E8c (#460) gebaut, E9 in zwei Wellen (E9a mit 116 bis 118, voraussichtlich #461; E9b, voraussichtlich #462), offen dazu die zwei Fragen aus E8c; neue Zeile der Kürzeltafel = #460; Etappenzeilen „E8c — E8b‑Q2/Q3" = #460 (Merge `9ab55946`) und „E9 … E12 — E9 läuft …" |

### 8.19 E9a — Vollständige Szenarioabdeckung, Teil a: Schemaschritte 116 bis 118, der Kern liest die Paare (#461)

Protokoll [`E9a_Szenarioabdeckung_Kern_Protokoll.md`](E9a_Szenarioabdeckung_Kern_Protokoll.md); der Stand von V‑4,
V‑G2 und V‑G5 (R‑V) und von A5 (R‑A) im Register, dazu der Vermerk zu V‑G7 und V‑G3 (R‑V); die sieben Fragen der Welle
— offen, gebaut jeweils Lesart a — unter R‑E9a. Die Welle ist der erste Teil der Etappe E9 (V‑E), die einzige neben E7
mit Rechenwirkung; die Dialoge folgen mit E9b.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E9 Teil a** (#461, Merge `62613292` auf dem Hilfszweig `pm8`) | V‑E im Kern: die Schemaschritte 116 (Szenariorahmen: Betrachtungszeitraum und Mengenänderung je Szenario), 117 (Trägerpreise best/worst) und 118 (Erlössätze best/worst), reines DDL; der Kern liest Zeitraum, Mengenfaktor, Trägerpreise, Einspeisevergütungen, DV-Entgelt und PPA-Preis je Szenario an je einer Stelle; Nachweiszeile, Parameterblock der Formelmappe (15 Zeilen) und Verlauf je Szenario; 17 Ressourcenschlüssel; Testdatenbank 118 | **ja, je Pflege** — ohne Pflege bitgleich: A/B über neun Größen mit Erwartet bitgleich, die Ankertests unverändert, Referenzlauf gegen R13 13/13, 387/387 byte-gleich (keine Pflege in den Referenzprojekten); Gate auf `62613292` grün |

*Kopf, die Schemaschritte (Z. 3, 5, 14 und 24–26):*

> **Stand 24.09.2026** · Codestand `9ab55946` · `SchemaStand.Zielversion` = 114 · Schemaschritte 90–114 vergeben,
> 115 zugesagt, 116–118 an E9a vergeben · …
>
> Die Schritte 97 bis 101, 103, 107 bis 110 und 114 gehören nicht diesem Feld, ebenso der zugesagte 115: … und
> **115**, der Zapfprofil-Stufe Z3 zugesagt (T2). … Für die Schritte B, C und D der Etappe E9 (§ 2.11.5) sind am
> 24.09.2026 **116**, **117** und **118** vergeben (Teil a, E9a).

**Umgesetzt mit E9a (#461):** Codestand `62613292`, Zielversion 118, „90–118 vergeben (116–118 die Schritte B, C und D
der Etappe E9a)"; 115 als Schritt der Zapfprofil-Stufe Z3 unter den fremden Schritten (`SCHRITT_115_ZAPFKATEGORIEN`,
#453); 116 bis 118 mit ihren Konstanten unter den Schritten dieses Feldes.

*§ 2.11.2, die Zeile V-G5 der Gap-Tafel (Z. 773), Spalte „Lücke / Behandlung":*

> **entschieden 31.08.2026: vollständige Abdeckung** — alle Parameter (Investition, Energiekosten, Betriebskosten,
> Erlöse, Rahmen, Mengen) erhalten Best/Worst-Werte; Modell in § 2.11.5

**Umgesetzt mit E9a (#461):** dahinter die Marke „**Stand: teilweise gebaut #461** (V‑E Teil a, Kern)" mit dem
Verweis auf die Regeln des Kerns; die Spalte „EPOS heute" bleibt als Messung vom 18.09.2026 stehen.

*§ 2.11.4, die Zeile V-E (Z. 833):*

> | **V-E** | Vollständige Szenarioabdeckung nach § 2.11.5 (V-G5, Umfang entschieden 31.08.2026), Risiko (V-G7),
> n-jährliche Zeitpunkte (V-G3) — **ohne Degradation (V-G2), A5** | Szenarioabdeckung und Freitext teils geliefert
> durch **W5‑B‑9** und **W5‑B‑12** (Migrationsschritte 71, 72) | **ja** — je Pflege, mit A/B-Nachweis; NULL = wie
> Erwartet hält die Etappe bis zur ersten Pflege ergebnisneutral | **E9** |

**Umgesetzt mit E9a (#461):** Die Spalte „entspricht / bereits geliefert durch" nennt Teil a mit den drei
Schemaschritten, der einen Lesestelle je Größe, Tests und A/B; Risiko (V‑G7) und n-jährliche Zeitpunkte (V‑G3) baut E9
nicht (E9a‑Q6); Stand „**E9** — Teil a gebaut #461 (Kern); Teil b … mit E9b".

*§ 2.11.5, die Spalte „Stand" der Parametertafel (Z. 860–863) und die Regeln „Pflege" und „Ausweis im Bericht"
(Z. 878–883):*

> | **Energiepreise** je Träger | … | **neu** |
> | **Erlössätze** (Marktgrößen) | … | **neu** |
> | **Rahmen** | … | **6 von 8 vorhanden** seit Schritt 71 (`Szen_Best/Worst_Zins`, `_Preis_E`, `_Preis_B`); neu sind
> allein Best/Worst des **Betrachtungszeitraums**. Namensvorsicht: … |
> | **Mengen** (Simulationsergebnis) | … | **neu** |
>
> - **Pflege**: der vorhandene ±-Knopf (`CaseEingabeDialog`) als einheitliches Muster auch an
>   Trägerpreisen, Erlösfeldern und der Rahmen-Gruppe; die ValERI-Ansicht zeigt je Szenario, welche
>   Parameter gepflegte Abweichungen tragen („12 von 31 Parametern szenariert").
> - **Ausweis im Bericht** (Norm 9c): Die Kalkulationstabelle je Szenario nennt die
>   Parametereinstellungen vollständig — die Szenariospalten der Rahmenzeile erscheinen im
>   Parameterblock des XLSX-Blatts.

**Umgesetzt mit E9a (#461):** je Zeile „**Kern gebaut #461**" mit den Spalten der Schritte 116 bis 118, die
Rahmenzeile „**8 von 8**"; die Regel „Pflege" mit dem Satz, dass die neuen Größen bis E9b keine Eingabestelle haben;
„Ausweis im Bericht" mit dem Vermerk „Umgesetzt #455 und #461" (Nachweiszeile, Parameterblock, Annahmentafel); neu
der Block „**Regeln des Kerns**" — keine Vorgaben, Zeitraum je Szenario, Mengenfaktor, Trägerpreis-Ersatz,
Erlössätze, Rollenmodell, Nachweis —, je Regel mit ihrer Lesestelle und der Frage aus E9a.

*§ 7, die Sätze zur laufenden Etappe (Z. 2742–2748):*

> **E9** (V‑E, die Szenarioabdeckung nach § 2.11.5) läuft in zwei Wellen: **E9a** mit den Schemaschritten 116 (B,
> Betrachtungszeitraum und Mengenfaktor je Szenario), 117 (C, Trägerpreise best/worst) und 118 (D, Erlössätze
> best/worst) und dem Kern, der die Paare liest (voraussichtlich #461), danach **E9b** mit dem ±-Knopf an den neuen
> Orten, mit ihr entfällt der Hinweistext (§ 2.11.7; voraussichtlich #462); ohne Degradation (A5), rechenwirksam je
> Pflege. Offen
> sind die acht Fragen aus E7c3 (→ Register R‑E7c3) und die zwei aus E8c (→ Register R‑E8c); die sechs aus E8b sind
> entschieden (23.09.2026, nach Empfehlung, → Register R‑E8b).

**Umgesetzt mit E9a (#461):** E9 Teil a ist gebaut; als Nächstes kommt E9b (voraussichtlich #462) mit dem ±-Knopf an
den drei neuen Orten und den Zeilen 8 und 9 der Szenariotafel, mit ihr entfällt der Hinweistext zugunsten des
Ausweises „n von m Parametern szenariert"; offen sind dazu die sieben Fragen aus E9a.

### 8.20 Berichtigungen im gültigen Stand (#461)

Die Stellen, die mit E9a veraltet sind; „vorher" ist der Wortlaut vor #461 (Stand `62613292`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Wortlaut in § 8.19 | Codestand `62613292`, Zielversion 118, „90–118 vergeben (116–118 die Schritte B, C und D der Etappe E9a)"; 115 unter den fremden Schritten (Zapfprofil Z3, #453); 116 bis 118 unter den Schritten dieses Feldes |
| § 2.11.2 | V‑G5 ohne Stand; Wortlaut in § 8.19 | „Stand: teilweise gebaut #461 (V‑E Teil a, Kern)" |
| § 2.11.4 | V‑E: Wortlaut in § 8.19; Fußnote „… und E8 (#454, #455; die Nachbesserung E8c #460)" | V‑E mit Teil a gebaut #461; Fußnote „…, E8 (#454, #455; die Nachbesserung E8c #460) und E9 Teil a (#461)" |
| § 2.11.5 | Spalte „Stand" und die Regeln „Pflege" und „Ausweis im Bericht": Wortlaut in § 8.19 | „Kern gebaut #461" mit den Spalten je Zeile, „8 von 8"; „Pflege" mit dem Satz zu E9b; „Ausweis im Bericht" mit dem Umgesetzt-Vermerk; neuer Block „Regeln des Kerns" |
| § 2.11.7 | endete mit dem Absatz zu A14 („… und in Wort- und Tabellenbericht.") | dazu der Absatz „Bis E9b (Stand #461)": der Hinweistext bleibt bis E9b und stimmt nur noch ohne Pflege der neuen Größen; an seiner Stelle kommt der Ausweis „n von m" |
| § 6.1 | Kurztafel bis E8c (#460) | Zeile „E9a Szenarioabdeckung, Teil a" (#461) mit den Schemaschritten 116 bis 118, ihren Tabellen und Spalten |
| § 6.2 | endete mit „… und `BetriebskostenStartjahrGliederungTests` (12, die Probe mit den Positionen des ersten Jahres)." | dazu `SzenarioParameterTests` (39) und der Satz „E9a bewegt keinen Anker" |
| § 7 und Anhang | § 7: Wortlaut in § 8.19, dazu die Aufzählung „… E8 Teil a (#454) und E8 Teil b (#455)"; Kürzeltafel „V-D = **#455**" und „V-E = **E9**", Mockup-Anhang bis „U12 und U43 erledigt #455"; Etappenzeile „E9 … E12 — E9 läuft: E9a mit den Schritten 116 bis 118 (voraussichtlich #461), dann E9b (voraussichtlich #462) …" | § 7 mit E9 Teil a (#461) gebaut, als Nächstes E9b, offen dazu die sieben Fragen aus E9a; Kürzeltafel „V-E Teil a = **#461**", neue Zeile = #461, „U15 teilweise #461 (Kern; Dialog mit E9b)"; Etappenzeilen „E9 Teil a — V‑E im Kern" = #461 (Merge `62613292`) und „E9 Teil b … E12 — nächste Etappe: E9b" |

### 8.21 E9b — Vollständige Szenarioabdeckung, Teil b: Pflege in den Dialogen, Ausweis statt Hinweistext (#462)

Protokoll [`E9b_Szenarioabdeckung_Dialoge_Protokoll.md`](E9b_Szenarioabdeckung_Dialoge_Protokoll.md); der Stand von V‑4,
V‑G2 und V‑G5 (R‑V), von A5 und A14 (R‑A) und von Q18 (R‑Q) im Register; die fünf Fragen der Welle — offen, gebaut
jeweils Lesart a, Empfehlung a, a, a, a, b — unter R‑E9b. Die Welle ist der zweite und letzte Teil der Etappe E9 (V‑E);
mit ihr ist E9 abgeschlossen.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E9 Teil b** (#462, Merge `75d45630`) | V‑E in den Dialogen: die Zeilen 8 (Betrachtungszeitraum) und 9 (Mengenänderung) der Szenariotafel, „Vorgaben" leert 18 Felder; der ±-Knopf — der `CaseEingabeDialog` als allgemeiner Baustein — an Arbeits-, Grund- und Leistungspreis der Trägerkarte, an der Einspeisevergütung PV und KWK, an DV-Entgelt und PPA-Preis; der Hinweistext entfällt, an seiner Stelle der Ausweis „n von m Parametern szenariert"; 38 Ressourcenschlüssel neu, 1 entfallen, 2 geändert; kein Schemaschritt | **keine ohne Pflege** — die Rechenwege stehen seit E9a; Anker unverändert, Referenzlauf gegen R13 13/13, 387/387 byte-gleich; erstes Gate auf `9fbac8c6` grün, zweites Gate Build 0 Fehler, ChartProben 146/146 gleich der Windows-Messlatte, voller Lauf 12.532 bestanden / 0 Fehler / 1 übersprungen (EPOS.Kern 5.700, EPOS.UI 5.877, KiKern 542, SpeicherEngine 386, SpeicherPlanung 27+1), Dokumentationswachen 26/26 (Log GATE462b) |

*Kopf (Z. 3):*

> **Stand 24.09.2026** · Codestand `62613292` · `SchemaStand.Zielversion` = 118 · Schemaschritte 90–118 vergeben
> (116–118 die Schritte B, C und D der Etappe E9a) · …

**Umgesetzt mit E9b (#462):** Codestand `75d45630`, Zielversion 119 und „90–119 vergeben" (119 die Kühlung
KU2, unter den fremden Schritten), dazu „E9b ohne Schritt".

*§ 2.11.2, die Zeile V-G5 der Gap-Tafel (Z. 776), Spalte „Lücke / Behandlung":*

> … Modell in § 2.11.5. **Stand: teilweise gebaut #461** (V‑E Teil a, Kern) — die Schemaschritte 116 bis 118 und die
> Regeln des Kerns in § 2.11.5: Betrachtungszeitraum, Mengenänderung, Trägerpreise und Erlössätze je Szenario, NULL/0 =
> wie Erwartet; die Pflege in den Dialogen mit E9b

**Umgesetzt mit E9b (#462):** „**Stand: gebaut #461/#462**" mit Szenariotafel, ±-Knopf und Ausweis.

*§ 2.11.4, die Zeile V-E (Z. 836) und die Fußnote (Z. 841):*

> … `SzenarioParameterTests`, A/B-Nachweis über neun Größen mit Erwartet bitgleich. Risiko (V-G7) und n-jährliche
> Zeitpunkte (V-G3) baut E9 nicht (E9a‑Q6, → Register R‑E9a) | … | **E9** — Teil a gebaut #461 (Kern); Teil b (die
> Pflege in den Dialogen, der Hinweistext entfällt, der Ausweis „n von m") mit E9b |
>
> *… E8 (#454, #455; die Nachbesserung E8c #460) und E9 Teil a (#461).*

**Umgesetzt mit E9b (#462):** Die Spalte „entspricht / bereits geliefert durch" nennt „Teil b gebaut #462" (Zeilen 8
und 9, ±-Knopf, Ausweis, `SzenarioAbdeckungTests`); Stand „**E9** — gebaut #461 (Kern) und #462 (Dialoge, Ausweis);
E9 abgeschlossen"; die Fußnote „… und E9 (#461, #462)".

*§ 2.11.5, die Spalte „Stand" der Parametertafel (Z. 863–866) und die Regel „Pflege" (Z. 881–885):*

> | **Energiepreise** je Träger | … | **Kern gebaut #461**, Spalten aus Schritt 117: …; die Pflege im Dialog mit E9b |
> | **Erlössätze** (Marktgrößen) | … | **Kern gebaut #461**, Spalten aus Schritt 118: …; die Pflege im Dialog mit E9b |
> | **Rahmen** | … | **8 von 8:** 6 seit Schritt 71 (…); der **Betrachtungszeitraum Kern gebaut #461**, Spalten aus
> Schritt 116: … |
> | **Mengen** (Simulationsergebnis) | … | **Kern gebaut #461**, Spalten aus Schritt 116: `Szen_Best/Worst_Menge` (%) |
>
> - **Pflege**: der vorhandene ±-Knopf (`CaseEingabeDialog`) als einheitliches Muster auch an
>   Trägerpreisen, Erlösfeldern und der Rahmen-Gruppe; die ValERI-Ansicht zeigt je Szenario, welche
>   Parameter gepflegte Abweichungen tragen („12 von 31 Parametern szenariert"). **Mit E9b** — die
>   neuen Größen haben bis dahin keine Eingabestelle; ein Projekt trägt sie nur, wenn sie in seiner
>   Datenbank stehen.

**Umgesetzt mit E9b (#462):** je Zeile „**gebaut #461/#462**" mit dem Ort der Pflege (Trägerkarte, Parameterdialog,
Dialog „BHKW-Wirtschaftlichkeit", PV-Vergütungsdialog, die Zeilen 1 bis 4, 8 und 9 der Szenariotafel); die Regel
„Pflege" beschreibt den gebauten Baustein, die drei Orte, Kennzeichen und Warnzeichen und die Szenariotafel für
Rahmen-Gruppe und Mengenänderung — die Rahmen-Gruppe ohne eigenen ±-Knopf (Abweichung 6 des Phase‑1-Berichts); neu
die Regel „**Ausweis „n von m Parametern szenariert"**" mit Ort, Zählregel und Lesart — statt des Beispiels „12 von
31" ein Satz aus den Tests.

*§ 2.11.7 (Z. 1024–1055):*

> Die vollständigen Parametersätze je Szenario (§ 2.11.5) kommen **nach** der Darstellungsetappe. Bis
> dahin sagt ein Hinweis unter der Annahmentafel der Wirtschaftlichkeitsseite, was ein Szenario heute
> variiert und was nicht (V-4, → Register R‑V; die Messung dazu: → Protokoll § 3.11).
> …
> **Bis E9b (Stand #461):** Mit E9 Teil a rechnet der Kern Betrachtungszeitraum, Mengenänderung,
> Trägerpreise und Erlössätze je Szenario (§ 2.11.5). Der Hinweistext bleibt stehen, bis die Pflege in den
> Dialogen kommt; er stimmt nur noch für Projekte ohne Pflege dieser Größen — eine Pflege zeigen dann die
> Annahmentafel, die Nachweiszeile je Szenario und die Kohärenzzeilen. Er entfällt mit E9b; an seiner Stelle
> steht der Ausweis „n von m Parametern szenariert" (§ 2.11.5, Pflege).

**Umgesetzt mit E9b (#462):** vorn der Absatz „**Entfallen mit E9b (#462)**"; der Rest ist Rückschau in der
Vergangenheitsform — der Wortlaut des Hinweistexts bleibt als Zitat stehen, dazu „entfallen #462"; der Absatz „Bis
E9b" heißt jetzt „Zwischen E9a und E9b (#461)" und endet mit dem Wegfall.

*§ 2.13 (5), die Folge in Block 4 (Z. 1281–1283):*

> … in der Folge Bandbreite, Spannenbild, Vorschlag, Hinweistext, Verlauf, Sensitivität; …

**Umgesetzt mit E9b (#462):** „Ausweis der Szenarioabdeckung (an der Stelle des Hinweistexts, #462)" statt
„Hinweistext".

*§ 7, die Sätze zur laufenden Etappe (Z. 2806–2813):*

> **E9** (V‑E, die Szenarioabdeckung nach § 2.11.5) läuft in zwei Wellen: **E9 Teil a (#461) ist gebaut** — …; **als
> Nächstes kommt E9b** (voraussichtlich #462) mit dem ±-Knopf an den drei neuen Orten und den Zeilen 8 und 9 der
> Szenariotafel, mit ihr entfällt der Hinweistext (§ 2.11.7) zugunsten des Ausweises „n von m Parametern
> szenariert". Offen sind die acht Fragen aus E7c3 (→ Register R‑E7c3), die zwei aus E8c (→ Register R‑E8c) und die
> sieben aus E9a (→ Register R‑E9a); …

**Umgesetzt mit E9b (#462):** E9 ist in zwei Wellen gebaut; als Nächstes kommt E10 (Nutzungsdauer S3 und
Speicherflotte, voraussichtlich #463) nach dem eigenen Entscheid zu ND‑S3; offen dazu die fünf Fragen aus E9b.

### 8.22 Berichtigungen im gültigen Stand (#462)

Die Stellen, die mit E9b veraltet sind; „vorher" ist der Wortlaut vor #462 (Stand `75d45630`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `62613292`, Zielversion 118, „90–118 vergeben" | Codestand `75d45630`, Zielversion 119, „90–119 vergeben"; 119 (Kühlung KU2) unter den fremden Schritten; „E9b ohne Schritt" |
| § 2.11.2 | V‑G5 „Stand: teilweise gebaut #461"; Wortlaut in § 8.21 | „Stand: gebaut #461/#462" |
| § 2.11.4 | V‑E: Wortlaut in § 8.21; Fußnote „… und E9 Teil a (#461)" | V‑E mit Teil b gebaut #462, Stand „E9 abgeschlossen"; Fußnote „… und E9 (#461, #462)" |
| § 2.11.5 | Spalte „Stand" und die Regel „Pflege": Wortlaut in § 8.21 | „gebaut #461/#462" mit dem Ort der Pflege je Zeile; „Pflege" mit Baustein, Orten und Szenariotafel; neue Regel „Ausweis „n von m Parametern szenariert"" mit Zählregel und Lesart |
| § 2.11.7 | begann mit „Die vollständigen Parametersätze … kommen nach der Darstellungsetappe"; endete mit „Bis E9b (Stand #461)" | vorn „Entfallen mit E9b (#462)", der Rest als Rückschau; „Zwischen E9a und E9b (#461)" |
| § 2.13 (5) | „Vorschlag, Hinweistext, Verlauf, Sensitivität" | „Vorschlag, Ausweis der Szenarioabdeckung (an der Stelle des Hinweistexts, #462), Verlauf, Sensitivität" |
| § 6.1 | Kurztafel bis E9a (#461) | Zeile „E9b Szenarioabdeckung, Teil b" (#462) |
| § 6.2 | endete mit „… E9a bewegt keinen Anker." | dazu `SzenarioAbdeckungTests` (15), `EnergietraegerSzenarioHuelleTests` (6) und der Satz „E9b bewegt keinen Anker" |
| § 7 und Anhang | § 7: Wortlaut in § 8.21, dazu die Aufzählung „… E8 Teil b (#455) und E9 Teil a (#461)" und „E7 und E8 sind damit abgeschlossen"; Kürzeltafel „V-E Teil a = **#461**" und „Teil b mit E9b", Mockup-Anhang „U15 teilweise #461 (Kern; Dialog mit E9b)"; Etappenzeile „E9 Teil b … E12 — nächste Etappe: E9b (voraussichtlich #462)" | § 7 mit E9 Teil b (#462), „E7, E8 und E9 sind damit abgeschlossen", als Nächstes E10, offen dazu die fünf Fragen aus E9b; Kürzeltafel „V-E = **#461**/**#462**", neue Zeile V‑E Teil b = #462, „U15 erledigt #461/#462, U10 entfallen #462"; Etappenzeilen „E9 Teil b — V‑E in den Dialogen" = #462 (Merge `75d45630`) und „E10 … E12 — nächste Etappe: E10 (voraussichtlich #463)" |

### 8.23 E10 — Nutzungsdauer S3 und Speicherflotte: Sätze je Technik, Speicherflotte an der Nutzungsdauertabelle, Kennzeichnung A8 (#463)

Protokoll [`E10_Nutzungsdauer_S3_Speicherflotte_Protokoll.md`](E10_Nutzungsdauer_S3_Speicherflotte_Protokoll.md); der
Stand von A7 und A8 (R‑A), von ND‑Q4, ND‑Q6 und ND‑Q7 (R‑ND) und von Nr. 20 (R‑NR) im Register; die sieben Fragen der
Welle — gebaut jeweils Lesart a, Empfehlung jeweils a, E10‑Q5 erledigt, sechs offen — unter R‑E10. Die Welle ist die
Etappe E10 des Analysepapiers; mit ihr ist § 6.3 Nr. 9h erledigt und der Mockup-Anhang U39 geschlossen.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E10** (#463, Merge `94521f2e`) | Schemaschritt 120 sät die Instandsetzungssätze der fünf Standardzeilen der Nutzungsdauertabelle (die Mitte der Vorlagenbereiche), der Dialog „Nutzungsdauern (AfA)" zeigt Instandsetzung und Wartung; die Sätze wirken nur über die ausdrückliche Vorbelegung — Übernahme einer Kostenvorlage oder „Sätze vorbelegen…" der Betriebsseite —, die Herkunft steht am Satz, in Herleitung und Formelmappe (Nachweisfassung 10); ein neuer Kesseleintrag in %/a übernimmt den Wartungssatz; die Flottenstudie rechnet den Restwert je Einheit linear aus der Nutzungsdauer, ohne eigenes Intervall aus der Batteriezeile, der feste Restwert der Einheit ist Altfeld; die Nutzungsdauer von Kessel und BHKW als Gerätedaten gekennzeichnet (A8); 24 Ressourcenschlüssel neu, 6 geändert; Testdatenbank auf Schemastand 120 | **keine ohne Zutun** — der zuerst gebaute Rückfall auf den Tabellensatz zur Rechenzeit ist zurückgebaut (E10/9, ND‑Q4); A/B mit dem Knopf auf einer Arbeitskopie (1030 −630.612,02 €), Flottenstudie 1046 +3.432,79 €; Anker unverändert, Referenzlauf gegen R13 13/13, 387/387 byte-gleich, keine R14; Gate auf `94521f2e`: Kern-Filter 0 Fehler, ChartProben 146/146 gleich der Windows-Messlatte, voller Lauf 12.580 bestanden / 0 Fehler / 1 übersprungen, Dokumentationswachen 26/26 |

*Kopf (Z. 3) und die Schritte dieses Feldes (Z. 30–31):*

> **Stand 24.09.2026** · Codestand `75d45630` · `SchemaStand.Zielversion` = 119 · Schemaschritte 90–119 vergeben
> (116–118 die Schritte B, C und D der Etappe E9a; E9b ohne Schritt; 119 die Kühlung KU2) · …
>
> **118** — die Erlössätze best/worst (`SCHRITT_118_ERLOESSATZ_SZENARIO`). Wer hier einen Schritt plant, nimmt die
> nächste freie Nummer **bei der Umsetzung** — nicht im Papier.

**Umgesetzt mit E10 (#463):** Codestand `94521f2e`, Zielversion 120, „90–120 vergeben (…; 120 die Sätze der
Nutzungsdauertabelle, Etappe E10)"; unter den Schritten dieses Feldes steht **120** hinter 118
(`SCHRITT_120_NUTZUNGSDAUER_SAETZE`, reines DML, § 3.4).

*§ 2.1, die Zeilen BHKW und Heizkessel der Anlagentafel (Z. 140, 142), Spalte „Investitionsfelder":*

> … · `Wartungskosten_kwhel` · Nutzungsdauer
>
> `Investitionskosten` · `Wartungskosten` mit Einheit (€/a \| €/kWh \| %/a) · Nutzungsdauer

**Umgesetzt mit E10 (#463):** je „Nutzungsdauer (Gerätedaten, nicht rechenwirksam — A8, #463)"; beim Kessel dazu
„ein neuer Eintrag in %/a übernimmt den Wartungssatz der Nutzungsdauertabelle".

*§ 2.8, Rahmen der Seite, die Rasterknöpfe (Z. 659–660):*

> - **Vier Rasterknöpfe:** „+ Position hinzufügen" · „Aus Vorlage übernehmen…" · „Positionskatalog…" ·
>   „Nutzungsdauern vorbelegen…"; darunter die Dialogleiste „Abbrechen · Speichern · OK".

**Umgesetzt mit E10 (#463):** dazu der vierte Knopf der Betriebsseite, „Sätze vorbelegen…" — leere Sätze füllen,
belegte nach Rückfrage, geschrieben mit „Speichern"/„OK" —, und die Herkunft unter dem Satzfeld.

*§ 2.13 (3), die zwei Stücke aus U39 (Z. 1230–1236) und der Schluss des Absatzes zur Hinweiszeile (Z. 1245–1246):*

> **Was der späteren Umsetzung fehlt** (zwei Stücke, im Mockup-Anhang als **U39** geführt):
>
> 1. die **geräteeigenen Nutzungsdauer-Spalten** (`Tab_BHKW`, `Tab_Heizkessel`,
>    `Tab_StromspeicherVariante`) — eine zweite Wahrheit, die kein Wirtschaftlichkeitsrechner liest;
> 2. der **Anschluss der Speicherflotte**, die ihren Ersatz über die gleichnamigen **Felder des
>    Flottenstands** (`ErsatzintervallJahre`, `RestwertEuro` als JSON in `Tab_SpeicherAuslegung`) führt
>    — nicht über Spalten; der Anschluss berührt deshalb die **Einfrierregel** des Projekts 1046.
>
> … die Hülle sammelt nicht mehr selbst. Offen bleiben die zwei Stücke oben (U39); die Entkopplung von Ersatz und
> Restwert ist umgesetzt #446.

**Umgesetzt mit E10 (#463):** „Die zwei Stücke aus U39 — gebaut mit E10 (#463)": (1) die Spalten von BHKW und Kessel
als „Nutzungsdauer (Gerätedaten)" gekennzeichnet, keine entfernt, `Tab_StromspeicherVariante` rechnet weiter selbst,
der Halbsatz aus A8 zu den Positionsarten 20/21 offen; (2) die Speicherflotte mit linearem Restwert je Einheit auf der
Ersatzkette der Flotte, der Intervall-Vorgabe aus der Standardzeile „Stromspeicher · Batterie" und dem Altfeld
`RestwertEuro`, ohne neue Basis. Der Schluss sagt „U39 ist damit erledigt; offen bleibt allein der Halbsatz aus A8 zur
Speichervariante".

*§ 3.4:* neu, hinter „Basis „% der Investition" auf der Betriebsseite" (Z. 1778–1780), der Absatz „**Sätze der
Nutzungsdauertabelle**" — die Saat aus Schritt 120, „die Tabelle rechnet nicht selbst" (ND‑Q4), die ausdrückliche
Vorbelegung über Kostenvorlage und „Sätze vorbelegen…", die Zuordnung über den Positionsschlüssel und die Herkunft am
Satz (Nachweisfassung 10).

*§ 6.3 Nr. 9h (Z. 2712–2725):*

> 9h. **Nutzungsdauer, Ersatz, Restwert — zwei fehlende Stücke (Mockup-Anhang U39):** die ungelesenen
>     geräteeigenen Nutzungsdauer-Spalten und der Anschluss der Speicherflotte — offen mit ND‑S3 (E10; A7
>     und A8 binden beides daran). **Gemessen mit E7c3 (#452):** `Tab_BHKW` führt eine Nutzungsdauer in 3 von
>     6 Zeilen der Testdatenbank (10 a), im Stamm in 44 von 79; `Tab_Heizkessel` in 1 von 22 (20 a), im Stamm
>     in 0 von 63 — beide ohne Leser in der Wirtschaftlichkeit; `Tab_StromspeicherVariante` in 13 von 13 (20 a),
>     sie rechnet in der Speicherwirtschaftlichkeit und im Peak-Shaving. Die Nutzungsdauer-Tabelle führt
>     abweichend BHKW-Modul 15 a und Batterie 10 a; die Flotte von 1046 zwei Einheiten mit Ersatzintervall 10 a
>     und Restwert 500 bzw. 300 €. **Vorschlag für ND‑S3:** die Gerätespalten nur als „Gerätedaten"
>     kennzeichnen (A8); für Speichervariante und Flottenintervall die Tabelle nur als Vorgabe neuer Einträge
>     nehmen — das bewegt nichts; ein linearer Restwert aus der Nutzungsdauer erst mit ND‑S3 und einer neu
>     eingefrorenen Basis für 1046. Erledigt sind die Nachpflege des Bestands und der
>     Pflegeort der Positionsart (#357), der Hinweis „T über Vorgabe, Position ohne Dauer" und die
>     Zeitraumzeile auf Seite und Bericht (E5, #434) sowie die Entkopplung von Ersatz und Restwert
>     (E7c2, #446, Schemaschritt 111, § 2.13 (3)) — siehe Protokoll.

**Erledigt mit E10 (#463):** beide Stücke — die Gerätespalten von BHKW und Kessel sind als Gerätedaten gekennzeichnet
(A8), die Speicherflotte ist angeschlossen (A7). Abweichend vom Vorschlag der Messung gilt die Intervall-Vorgabe der
Flotte zur Rechenzeit der Studie, nicht nur für neue Einträge (Auftrag E10, Punkt 6; mitzuentscheiden mit E10‑Q3); der
lineare Restwert kam ohne neue Basis, weil der Referenzlauf keine Flottenwirtschaftlichkeit führt. Die Speichervariante
liest weiter ihre eigene Spalte (der Halbsatz aus A8 ist offen). Im Konzept steht der Punkt als Einzeiler.

*§ 6.3 Nr. 19 (Z. 2745):*

> 19. Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung Kessel"

**Umgesetzt mit E10 (#463):** „dokumentiert mit E10 (#463)" (E10‑Q6, Lesart a) — der Kessel führt seine Wartung je
Katalogeintrag in €/a, €/kWh oder %/a, ein neuer Eintrag in %/a übernimmt den Wartungssatz der Tabelle; das BHKW führt
sie fest in €/kWh el und bekommt keine Vorbelegung. Der Punkt bleibt offen; behoben ist nichts.

*§ 6.1, § 6.2 und § 6.5:* neu die Zeile „E10 Nutzungsdauer S3 und Speicherflotte" (#463) der Kurztafel; der Satz zu den
Testklassen `NutzungsdauerS3Tests` (16), `SpeicherFlottenNutzungsdauerTests` (9) und `NutzungsdauerKennzeichnungTests`
(4) mit „E10 bewegt keinen Anker"; die Doppelung „Nutzungsdauer an zwei Orten" als benannt, nicht gekoppelt.

*§ 7, die Sätze zur nächsten Etappe (Z. 2841–2844):*

> Ausweis „n von m Parametern szenariert" an der Stelle des Hinweistexts (§ 2.11.7). **Als Nächstes kommt E10**
> (Nutzungsdauer S3 und Speicherflotte, voraussichtlich #463) nach dem eigenen Entscheid zu ND‑S3. Offen
> sind die acht Fragen aus E7c3 (→ Register R‑E7c3), die zwei aus E8c (→ Register R‑E8c), die sieben aus E9a
> (→ Register R‑E9a) und die fünf aus E9b (→ Register R‑E9b); …

**Umgesetzt mit E10 (#463):** E10 ist gebaut, den eigenen Entscheid zu ND‑S3 vertreten die Fragen aus E10; als
Nächstes kommt E12 (Wiki-Runden), E11 entfällt — der Etappenplan ist bis auf E12 abgearbeitet; offen dazu sechs der
sieben Fragen aus E10. In der Aufzählung davor „… E9 Teil b (#462) und E10 (#463)" und „E7, E8, E9 und E10 sind damit
abgeschlossen".

*Anhang:* in der Kürzeltafel „§ 6.3 Nr. 9h" mit #463 und „erledigt mit ND‑S3 (E10)", eine neue Zeile der Welle (A7 · A8
· § 3.4 · § 6.3 Nr. 9h und Nr. 19 · U39 · ND‑Q6 · ND‑Q7 · E10‑Q1…Q7 = #463), „S3 = #463" statt „S3 offen (E10)" und im
Mockup-Anhang „U39 erledigt #446 und #463" statt „U39 teilweise (Nr. 9h gemessen #452, der Rest mit ND‑S3)"; in der
Etappentafel die Zeile „E10 — Nutzungsdauer S3 und Speicherflotte" = #463 (Merge `94521f2e`) und „E11 … E12 — nächste
Etappe: E12" statt „E10 … E12 — nächste Etappe: E10 (voraussichtlich #463)".

### 8.24 Berichtigungen im gültigen Stand (#463)

Die Stellen, die mit E10 veraltet sind; „vorher" ist der Wortlaut vor #463 (Stand `94521f2e`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `75d45630`, Zielversion 119, „90–119 vergeben"; die Schritte dieses Feldes enden mit 118 | Codestand `94521f2e`, Zielversion 120, „90–120 vergeben"; 120 (die Sätze der Nutzungsdauertabelle, E10) unter den Schritten dieses Feldes |
| § 2.1 | BHKW und Heizkessel „· Nutzungsdauer" | „Nutzungsdauer (Gerätedaten, nicht rechenwirksam — A8, #463)"; beim Kessel der Wartungssatz für neue Einträge in %/a |
| § 2.8 | „Vier Rasterknöpfe: … „Nutzungsdauern vorbelegen…"; darunter die Dialogleiste" | dazu „Sätze vorbelegen…" auf der Betriebsseite und die Herkunft unter dem Satzfeld |
| § 2.13 (3) | „Was der späteren Umsetzung fehlt" (die zwei Stücke U39); „Offen bleiben die zwei Stücke oben (U39)" | „Die zwei Stücke aus U39 — gebaut mit E10 (#463)"; „U39 ist damit erledigt; offen bleibt allein der Halbsatz aus A8 zur Speichervariante" |
| § 3.4 | — | neuer Absatz „Sätze der Nutzungsdauertabelle" |
| § 6.1 | Kurztafel bis E9b (#462) | Zeile „E10 Nutzungsdauer S3 und Speicherflotte" (#463) |
| § 6.2 | endete mit „… E9b bewegt keinen Anker." | dazu `NutzungsdauerS3Tests` (16), `SpeicherFlottenNutzungsdauerTests` (9), `NutzungsdauerKennzeichnungTests` (4) und „E10 bewegt keinen Anker" |
| § 6.3 Nr. 9h | Wortlaut in § 8.23 | Einzeiler „erledigt mit E10 (#463)"; offen der Halbsatz zur Speichervariante |
| § 6.3 Nr. 19 | „Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung Kessel"" | „dokumentiert mit E10 (#463)" mit der Beschreibung; der Punkt bleibt offen |
| § 6.5 | — | neue Zeile „Nutzungsdauer an zwei Orten … — benannt, nicht gekoppelt (A8, #463)" |
| § 7 und Anhang | § 7: Wortlaut in § 8.23, dazu „… E9 Teil a (#461) und E9 Teil b (#462)" und „E7, E8 und E9 sind damit abgeschlossen"; Kürzeltafel „S3 offen (E10)", „U39 teilweise (Nr. 9h gemessen #452, der Rest mit ND‑S3)"; Etappenzeile „E10 … E12 — nächste Etappe: E10 (voraussichtlich #463)" | § 7 mit E10 (#463), „E7, E8, E9 und E10 sind damit abgeschlossen", als Nächstes E12, offen dazu sechs Fragen aus E10; Kürzeltafel „S3 = #463", neue Zeile der Welle, „U39 erledigt #446 und #463"; Etappenzeilen „E10 — Nutzungsdauer S3 und Speicherflotte" = #463 (Merge `94521f2e`) und „E11 … E12 — nächste Etappe: E12" |

### 8.25 E13 — Checkliste Punkt 9, Fehlergründe in der Oberfläche, A8-Halbsatz, zwei Hilfe-Anker (#474)

Protokoll [`E13_Checkliste_Fehlergruende_Protokoll.md`](E13_Checkliste_Fehlergruende_Protokoll.md); der Stand von E9b‑Q5
(R‑E9b), E7c3‑Q6 (R‑E7c3) und A8 (R‑A) im Register. Die Welle ist die kleine Bauwelle nach den Anwenderentscheiden vom
24.09.2026 („Freigabe für: kleine Bauwelle für E9b‑Q5 (b) und E7c3‑Q6 (a)"), erweitert um den Halbsatz aus A8 und zwei
der vier Anker-Kandidaten aus E12; keine Etappe des Plans E0–E12, keine offene Frage.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E13** (#474, erster Merge `4b50b77b`, End-Merge `c71addf5`) | Punkt 9 der Anhang-E-Checkliste „erfüllt", sobald Günstig und Ungünstig gerechnet sind, „teilweise" bei nur Erwartet oder einem Szenario, „offen" ohne Lauf (E9b‑Q5 b); `Fehlergrund.Anzeigezeilen` und die Gründe `Ladefehler`, `Speicherfehler`, `Vorsorgewarnung` in der Statuszeile der Ergebnisseite und im Dialog BHKW-Wirtschaftlichkeit, je Grund einmal; einen Datenbankfehler beim Schreiben meldet die Anwendung selbst, `Speicherfehler` trägt nur Fehler außerhalb der Datenbankanweisung, nach einem gescheiterten UPDATE kein INSERT (E7c3‑Q6 a, E13/6); eine neue Speichervariante nimmt die Nutzungsdauer der Zeile „Stromspeicher · Batterie" (A8); `Form_VorlagenPosition` und `Form_LeistungspreisReihe` auf ihren Abschnitt der Seite Kosten; 5 Ressourcenschlüssel neu, 2 neu gefasst | **keine** — Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich (394/394 CSV, 4.207.049 Werte), kein Schemaschritt; Gate auf `4b50b77b`: Kern-Filter 0 Fehler, ChartProben 146/146 gleich der Windows-Messlatte, voller Lauf 12.714 bestanden / 0 Fehler / 1 übersprungen, Dokumentationswachen 26/26 |

*§ 3.9, der Schluss des Absatzes zu B‑6 (Z. 2391–2392):*

> `Katalogfehler` (Kraftwerksparks), `Ladefehler`, `Speicherfehler` und `Vorsorgewarnung` (Wirtschaftlichkeit); die
> letzten drei zeigt die Oberfläche nicht (E7c3‑Q6, offen). Ein nicht lesbarer Tarif gilt als nicht aktiv.

**Umgesetzt mit E13 (#474):** Die letzten drei zeigt die Oberfläche (E7c3‑Q6 a): je Grund eine Zeile, Statuszeile der
Ergebnisseite und Dialog BHKW-Wirtschaftlichkeit, der Datenbankfehler einmal als Meldung der Anwendung,
`StelleTabellenSicher` setzt die `Vorsorgewarnung` zurück.

*§ 7, die Sätze zu den offenen Fragen (Z. 2879–2885):*

> **E12** (Wiki-Runden) ist mit **#470** vorbereitet — der Sammel-Upload selbst steht nach Freigabe
> des Anwenders aus; E11 entfällt — damit ist der Etappenplan E0–E12 bis auf den Upload
> abgearbeitet. Offen
> sind die acht Fragen aus E7c3 (→ Register R‑E7c3), die zwei aus E8c (→ Register R‑E8c), die sieben aus E9a
> (→ Register R‑E9a), die fünf aus E9b (→ Register R‑E9b) und sechs der sieben aus E10 (→ Register R‑E10; E10‑Q5 ist
> erledigt); … Nach dem Entscheid aus E7c3 kommen der Rest von B‑6 (E7c3‑Q5) und die Anzeige der drei
> Kerneigenschaften `Ladefehler`, `Speicherfehler`, `Vorsorgewarnung` (E7c3‑Q6) dazu.

**Umgesetzt mit E13 (#474):** „Alle Fragen sind entschieden"; die zwei offenen Bauten und der Halbsatz aus A8 sind mit
E13 gebaut; aus E7c3 bleibt der Rest von B‑6 (E7c3‑Q5 a, eine eigene kleine Etappe); der Sammel-Upload ist für den
26.09.2026 freigegeben.

### 8.26 Berichtigungen im gültigen Stand (#474)

Die Stellen, die mit E13 veraltet sind; „vorher" ist der Wortlaut vor #474 (Stand `4b50b77b`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `94521f2e`, Zielversion 120, „90–120 vergeben"; fremde Schritte bis 119 | Codestand `c71addf5`, Zielversion 124, „90–124 vergeben (…; 121–124 anderen Feldern; E13 ohne Schritt)"; fremd dazu 121 (Katalogverweis, #468), 122/123 (Anlagenkopplung AK1), 124 (Zapfprofil Z4, #464) |
| § 2.11.2, Zeile V‑G12 (Z. 792) | „Stand: gebaut #455 … Knopf „Anhang-E-Checkliste…" auf der Ergebnisseite" | dazu „Punkt 9 (Szenarioanalyse), gebaut #474": erfüllt / teilweise / offen, der Ausweis als Beleg |
| § 2.11.5, Ausweis (Z. 914) | endete mit „(beides zum Mitentscheiden, E9b‑Q2)." | dazu „In Punkt 9 der Anhang-E-Checkliste ist der Ausweis Beleg, nicht Bedingung" (E9b‑Q5 b) |
| § 2.13 (3) (Z. 1244–1245, 1264) | „der Halbsatz aus A8, sie solle die Positionsarten 20/21 lesen, ist offen"; „offen bleibt allein der Halbsatz aus A8 zur Speichervariante" | „erledigt mit #474 als Vorgabe neuer Einträge" mit den zwei Wegen; „der Halbsatz aus A8 zur Speichervariante ist mit #474 gebaut" |
| § 3.9 (Z. 2391–2392) | Wortlaut in § 8.25 | die Anzeige der drei Gründe (E7c3‑Q6 a, #474) |
| § 6.1 | Kurztafel bis E10 (#463) | Zeile „E13 Checkliste Punkt 9, Fehlergründe, A8-Halbsatz" (#474) |
| § 6.2 | endete mit „… E10 bewegt keinen Anker." | dazu die Ergänzungen an `AnhangEChecklisteTests`, `RobustheitB6Tests`, `SpeichervarianteSicherstellenTests` und „E13 bewegt keinen Anker" |
| § 6.3 Nr. 9h (Z. 2754–2755) | „offen bleibt allein der Halbsatz aus A8 zur Speichervariante (Positionsarten 20/21)" | „der Halbsatz … ist erledigt mit E13 (#474)" |
| § 6.5, Nutzungsdauer an zwei Orten (Z. 2853) | „(der Halbsatz aus A8 ist offen)" | „eine neue Variante bekommt sie aus der Zeile „Stromspeicher · Batterie" (Halbsatz aus A8, #474)" |
| § 7 und Anhang | § 7: Wortlaut in § 8.25; Mockup-Zeile „U12 und U43 erledigt #455"; Etappenzeile „E11 … E12 — nächste Etappe: E12 …; Bau offen: E9b‑Q5 b, E7c3‑Q6 a" | § 7 „Alle Fragen sind entschieden", E13 (#474); Kürzeltafel mit der Zeile der Welle (#474) und „U43 … (Punkt 9 „erfüllt" #474)"; Etappenzeilen „E11 … E12 — E12 vorbereitet #470, Sammel-Upload 26.09.2026 …; gebaut mit E13 (#474) …; alle Fragen entschieden" und „E13 — kleine Bauwelle" = #474 |

### 8.27 E14 — Formelmappe je Szenario: Stufen 1 und 2 für Günstig und Ungünstig (#477)

Protokoll [`E14_Formelmappe_je_Szenario_Protokoll.md`](E14_Formelmappe_je_Szenario_Protokoll.md); der Stand von E8b‑Q1
(R‑E8b, abgelöst), der Befund 1 aus E9a (R‑E9a, erledigt) und die neue Familie R‑E14 im Register. Die Welle folgt dem
Befund 1 aus E9a („Formelmappe Stufe 1 und 2 rechnen nur Erwartet") auf den Auftrag des Anwenders vom 24.09.2026, 18:35
(„Formelmappe und Befund aus E9a: Auftrag"); sie ergänzt V‑D (E8 Teil b) und ist keine eigene Etappe des Plans E0–E12.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E14** (#477, erster Merge `f3f071d2` über `61efa054`, der Baum gleich `f6727fdd`; End-Merge `b9c660b9`) | Stufe 1 je Szenario: unter den Tabellen von Erwartet je Stand eine Tabelle Günstig und Ungünstig aus dem Lauf des Szenarios, Formeln auf seine Spalte im Parameterblock, bis zum längsten Zeitraum, jenseits von T_s leer über eine Schutzformel, der Restwert am Ende von T_s (E14‑Q1 a); Stufe 2 je Szenario: Nettobarwert, Annuität, Differenzreihe, Amortisation, zwei Hilfsspalten für Vorzeichen und Wechsel, der Zinsfuß „nicht eindeutig" bei mehreren Wechseln (E14‑Q3 a, auch bei Erwartet), die Bandbreite als Zellbezug mit `MAX-MIN` (E14‑Q2 a löst E8b‑Q1 a ab); Punkt 11 der Checkliste „alle drei Szenarien formelbasiert"; 5 Ressourcenschlüssel neu, 3 neu gefasst | **keine** — Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich (394/394 CSV, 4.207.049 Werte), kein Schemaschritt; Wertfassung = Formelfassung in 16 Prüfgruppen (Excel 16 und ClosedXML abweichend 0, OpenXML 0 Fehler); voller Lauf auf `f6727fdd` 12.967 bestanden / 0 Fehler / 1 übersprungen; Gate auf `f3f071d2`: Kern-Filter 0 Fehler, ChartProben 151/151 gleich der Windows-Messlatte, voller Lauf 12.967 / 0 / 1, Dokumentationswachen 29/29 |

*§ 2.11.6, Stufe 0, der Schluss (Z. 994–996):*

> drei Sätze der Grenze. Die Formeln rechnen mit der Spalte Erwartet: Wer dort einen Satz ändert, sieht
> Mehrjahrestabellen und Kennzahlen des Erwartungsfalls mitziehen; die Jahreszeilen stehen fest — ein anderer
> Betrachtungszeitraum verlangt einen neuen Bericht.

**Umgesetzt mit E14 (#477):** Die Formeln rechnen je Szenario mit seiner Spalte; die Jahreszeilen reichen bis zum
längsten Zeitraum der drei Szenarien, Jahre nach T_s eines Szenarios bleiben leer — ein längerer Zeitraum verlangt
einen neuen Bericht.

*§ 2.11.6, Stufe 2, der Schluss (Z. 1006–1008):*

> Ein mehrdeutiger Zinsfuß und die
> Kennzahlen der Blöcke Günstig und Ungünstig bleiben Werte — für sie gibt es keine Mehrjahrestabelle (E8b‑Q1,
> → Register R‑E8b).

**Umgesetzt mit E14 (#477):** Die Blöcke Günstig und Ungünstig rechnen auf ihre eigenen Tabellen in Formeln
(E14‑Q2 a); ein mehrdeutiger Zinsfuß steht in allen drei Szenarien als Text „nicht eindeutig" (E14‑Q3 a).

### 8.28 Berichtigungen im gültigen Stand (#477)

Die Stellen, die mit E14 veraltet sind; „vorher" ist der Wortlaut vor #477 (Stand `61efa054`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf | Codestand `c71addf5`, „E13 ohne Schritt" | Codestand `b9c660b9`, „E13 und E14 ohne Schritt" |
| § 2.11.2, Zeile V‑G10 (Z. 792) | „Stand: gebaut #455 … die Kennzahlen des Szenarios Erwartet in Formeln …" | dazu „Ergänzt #477 (E14): Stufe 1 und 2 rechnen alle drei Szenarien formelbasiert" |
| § 2.11.4, Zeile V‑D (Z. 846) und Fußnote (Z. 852) | „… die Kennzahlen von Günstig und Ungünstig bleiben Werte (E8b‑Q1, → Register R‑E8b)"; Stand „E8 Teil b — gebaut"; Fußnote bis E9 (#461, #462) | „Ergänzt #477 (E14, nach dem Befund 1 aus E9a)" mit Tabellen, Kennzahlen, Zinsfuß und Bandbreite je Szenario, 16 Prüfgruppen, E14‑Q2 a löst E8b‑Q1 a ab; Stand „… gebaut; ergänzt #477 (E14)"; Fußnote „außerhalb des Plans E13 (#474) und E14 (#477)" |
| § 2.11.6, Einleitung (Z. 974–975) | „Gebaut sind alle vier Stufen mit #455 …" | dazu „mit #477 (E14) rechnen die Stufen 1 und 2 alle drei Szenarien formelbasiert" |
| § 2.11.6, „Was die Mappe trägt", Stufen 0, 1, 2, 3 (Z. 994–1015) | Stufe 0 und 2: Wortlaut in § 8.27; Stufe 1 ohne Szenarien; Stufe 3 ohne Szenarioangabe | Stufe 0 und 2 wie in § 8.27; Stufe 1 mit dem Absatz „Je Szenario (#477, E14‑Q1 a)" — Eingangswerte, Energiespalte ohne Zerlegung Menge × Preis, Schutzformel, Restwert, Nachweisblock nur bei Erwartet; Stufe 3 „gilt für das Szenario Erwartet" |
| § 2.11.6, Gegenrechnung (Z. 1017–1018) | „in allen 13 Prüfgruppen … (mit #460 über 15 Prüfgruppen nachgemessen …)" | dazu „mit #477 über 16 Prüfgruppen für alle drei Szenarien — Excel 16 und die ClosedXML-Nachrechnung abweichend 0" |
| § 2.11.6, „Dauerhaft Werte bleiben" und „EPOS trägt die Werte ein" (Z. 1041–1043) | Liste ohne Satz zu Günstig/Ungünstig; „EPOS trägt die Werte ein, Excel rechnet neu." | dazu „Die Kennzahlen der Szenarien Günstig und Ungünstig gehören nicht dazu …"; „… Excel rechnet neu — in allen drei Szenarien." |
| § 6.1 | Kurztafel bis E13 (#474) | Zeile „E14 Formelmappe je Szenario" (#477) |
| § 6.2 | endete mit „… E13 bewegt keinen Anker." | dazu die zwei Wachfälle in `BerichtBlattstrukturWacheTests`, `Punkt_11_nennt_alle_drei_Szenarien_formelbasiert` und „E14 bewegt keinen Anker" |
| § 7 und Anhang | § 7: „Alle Fragen sind entschieden: …" (Z. 2899), der Absatz endete mit „… nicht gebaut). Aus der" (Z. 2904); Kürzeltafel bis #474; Mockup-Zeile „U12 und U43 erledigt #455 (Punkt 9 „erfüllt" #474)"; Etappenzeilen bis „E13 — kleine Bauwelle" | § 7 „Alle Fragen der Etappen bis E10 sind entschieden", dazu E14 (#477) mit drei offenen Fragen und die freigegebenen Aufträge E15, E16, E17; Kürzeltafel mit der Zeile der Welle (#477); „… Günstig und Ungünstig in Formeln, Punkt 11 #477"; Etappenzeile „E14 — Formelmappe je Szenario" = #477, die Zeile „E11 … E12" mit „E8b‑Q1 abgelöst … alle Fragen bis E10 entschieden" |

### 8.29 E15 — Risikomodul nach DIN EN 17463: Zinszuschlag oder Zahlungsstromabzug, Schemaschritt 125 (#478)

Protokoll [`E15_Risikomodul_Protokoll.md`](E15_Risikomodul_Protokoll.md); im Register die Zeile V‑G7 in R‑V, der
Vermerk zu E9a‑Q6 (R‑E9a) und die neue Familie R‑E15. Die Welle folgt dem Auftrag des Anwenders vom 24.09.2026 („V‑G7
Risiko: eigener kleiner Auftrag ausführen"); sie baut die Lücke V‑G7, die die Zeile V‑E mitnennt und E9 nicht gebaut
hat (E9a‑Q6 a), und ist keine eigene Etappe des Plans E0–E12.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E15** (#478, erster Merge `d176b378` über `6d022f6d`, der Baum gleich `5eaed19c`; End-Merge `dedfc760`) | Schemaschritt 125 mit `Risiko_Art`, `Risiko_Zinszuschlag`, `Risiko_Verlust` und `Risiko_Wahrscheinlichkeit` an `Tab_ProjektWirtschaftlichkeit` (reines DDL, Testdatenbank 125); `RisikoModul` als eine Stelle der Regeln: Zinszuschlag in allen drei Szenarien über `FuerSzenario` (E15‑Q1 a) oder Zahlungsstromabzug R_loss × p_loss / 100 je Periode ab Jahr 1, nicht Jahr 0 und nicht der Restwert, als Bestandteil RISIKO (E15‑Q2 a) für jeden Stand außer der Referenz (E15‑Q4 a); Gruppe „Risiko (DIN EN 17463, 6.5)" im Parameterdialog mit Infoknopf auf `Wirtschaftlichkeit#risiko`, KI-Feldkarte; Ausweis nur bei Pflege (E15‑Q3 a) — Nachweiszeile, Annahmentafel, Deklaration 6.5, Punkt 6 der Checkliste, Gliederung, Mehrjahrestabelle, Risikozeilen der Formelmappe | ja, je Pflege; Vorgabe aus — ohne Pflege bitgleich (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich); A/B: Zuschlag 1 %-Punkt an 1030 wie ein Lauf mit i + 1 (−31.141.243 → −28.306.379 €), Abzug 1.000 €/a an 1030 −14.877 € im Erwartungsfall, 1024 gegen den Stamm −1.807.372 → −1.822.250 € |

*§ 2.11.2, Gap-Tabelle, Zeile V‑G7 (Z. 789):*

> | V-G7 | **Risiko**: Zinszuschlag **oder** Abzug `R_loss × p_loss` auf die Periodennettosumme, nur t > 0 (6.5, Anhang F) | fehlt | optionales Risikomodul; Anhang F bevorzugt den Zahlungsstromabzug; Vorgabe aus |

**Umgesetzt mit E15 (#478):** „gebaut #478 (E15, Schemaschritt 125)" mit den vier Spalten, den zwei Wegen, dem Ausweis
und der Lesart der Norm — Anhang F, Tabelle F.2 rechnet R_loss als Prozent des Nettorückflusses, gebaut ist ein
Betrag in € je Periode (die Prozentlesart als E15‑Q4 c).

*§ 2.11.4, Zeile V‑E, der Schluss der Spalte „geliefert durch" (Z. 847):*

> Risiko (V-G7) und n-jährliche Zeitpunkte (V-G3) baut E9 nicht (E9a‑Q6, → Register R‑E9a)

**Umgesetzt mit E15 (#478):** das Risiko gebaut mit dem eigenen Auftrag E15; V‑G3 offen, Auftrag E16.

### 8.30 Berichtigungen im gültigen Stand (#478)

Die Stellen, die mit E15 veraltet sind; „vorher" ist der Wortlaut vor #478 (Stand `6d022f6d`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3) und Schrittabsatz (Z. 34) | Codestand `b9c660b9`, Zielversion 124, „Schemaschritte 90–124 vergeben"; der Absatz endete mit dem Schritt 120 der Etappe E10 | Codestand `dedfc760`, Zielversion 125, „90–125 vergeben … 125 das Risikomodul, Etappe E15"; dazu der Schritt **125** (`SCHRITT_125_RISIKOMODUL`) mit seinen vier Spalten |
| § 2.11.2, Zeile V‑G7 (Z. 789) | „fehlt" | Wortlaut in § 8.29; „gebaut #478" samt der Lesart R_loss in € je Periode |
| § 2.11.4, Zeile V‑E (Z. 847) und Fußnote (Z. 853) | Wortlaut in § 8.29; Stand „E9 — gebaut … E9 abgeschlossen"; Fußnote „… E13 (#474) und die Formelmappe je Szenario E14 (#477, ergänzt V‑D)" | „Risiko (V‑G7) gebaut #478 … V‑G3 offen — der Auftrag E16"; Stand dazu „Risiko gebaut #478 (E15), V‑G3 offen (E16)"; Fußnote mit „das Risikomodul E15 (#478, V‑G7 aus V‑E, Schemaschritt 125)" |
| § 2.11.5, „Ausweis im Bericht" (Z. 930) | endete mit „… Einspeisevergütungen nur bei Pflege." | dazu der Absatz „Risiko (umgesetzt #478, V‑G7, E15‑Q3 a)" — Nachweiszeile, Annahmentafel, Deklaration, Punkt 6, Parameterblock; kein Szenariowert, zählt nicht im Ausweis „n von m" |
| § 2.11.6, „Was die Mappe trägt", Stufe 0 (Z. 999) und Stufe 1 (Z. 1012) | ohne Risiko | Stufe 0 mit den Risikozeilen (`Zins_Basis`, `Risiko_Zuschlag`, `Zins_i` als Formel bzw. `Risiko_Verlust`, `Risiko_p`, `Risiko_Abzug`); Stufe 1 mit der Spalte „Risikoabzug" als `=-Risiko_Abzug` je Szenario und dem Bestandteil RISIKO der Gliederung |
| § 6.1 | Kurztafel bis E14 (#477) | Zeile „E15 Risikomodul" (#478) mit Schemaschritt 125 |
| § 6.2 (Z. 2722) | endete mit „… E14 bewegt keinen Anker." | dazu `RisikoModulTests` (25) und fünf Dialogproben; „E15 bewegt keinen Anker" |
| § 7 (Z. 2930–2932) | „Freigegeben und im Bau sind die Lücken V‑G7 (Risikomodul, E15) und V‑G11 (…, E17); V‑G3 (…, E16) folgt nach E15" | „Die Lücke V‑G7 ist mit E15 (#478) gebaut … vier Fragen offen (→ Register R‑E15). Freigegeben und im Bau ist V‑G11 (E17); V‑G3 (E16) folgt nach E15" |
| Anhang (Z. 2976, 2993, 3019) | Zeile V‑A…V‑E ohne Risiko; Mockup-Zeile „… Punkt 11 #477"; Etappenzeilen bis „E14 — Formelmappe je Szenario" | „aus V-E das Risiko V‑G7 = E15 (gebaut #478), V‑G3 = E16 (offen)"; „… Punkt 6 mit dem Risiko und die Risikozeilen der Mappe #478"; Kürzelzeile der Welle (#478) und Etappenzeile „E15 — Risikomodul (V‑G7)" = #478 |

### 8.31 E17 — Nicht monetarisierbare Wirkungen: Kategorie und Beurteilung, Schemaschritt 127 (#479)

Protokoll [`E17_Nicht_monetaere_Wirkungen_Protokoll.md`](E17_Nicht_monetaere_Wirkungen_Protokoll.md); im Register die
Zeile V‑G11 in R‑V und die neue Familie R‑E17. Die Welle folgt dem Auftrag des Anwenders vom 24.09.2026 („V‑G11 …
kleiner Dialog-und-Bericht-Auftrag ohne Rechenwirkung"); sie ergänzt den Freitext aus W5‑B‑12 um Kategorie und
Beurteilung und ist keine eigene Etappe des Plans E0–E12. Der Schritt heißt 127; 126 ist die Reparatur der
Gebäude-Katalogsätze (Dialog Design, #485).

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E17** (#479, erster Merge `0462f92e` über `c99c4c7a`, der Baum gleich `079de7d7`; End-Merge `52614c33` nach dem Nachzug auf #485) | Schemaschritt 127 mit der Tabelle `Tab_ProjektWirkung` (STRICT, Fremdschlüssel auf `Tab_Projekt` mit Weitergabe; Kategorie, Beschreibung, Dauer 1–3, drei Wirkungsgrade 0–3) und der Übernahme eines gepflegten Freitexts als Wirkung SONSTIG ohne Beurteilung (E17‑Q1 a, E17‑Q3 a; Testdatenbank 127); die Beurteilung nach 8.2 als Dauer × stärkste Wirkung, 0 bis 9, an einer Stelle (`NichtMonetaereWirkungen`, E17‑Q2 a); der Baustein `WirkungenListe` im Bewertungsblock statt des Freitexts (Altfeld nur lesbar), Infoknopf auf `Wirtschaftlichkeit#nicht-monetaer`, KI-Sicht `wirkung_*`; die Tabelle in Wort- und Tabellenbericht, die Punkte 2b und 3b der Anhang-E-Checkliste „erfüllt"/„teilweise"/„offen" | keine — kein Rechenweg liest die Tabelle (Anker „keine Rechenwirkung" bitgleich, Referenzlauf 13/13 gegen R14 byte-gleich) |

*§ 2.11.2, Gap-Tabelle, Zeile V‑G11 (Z. 795):*

> | V-G11 | **Nicht monetarisierbare Wirkungen**: erfassen, kategorisieren (Energiefluss / finanziell / sonstig), beurteilen nach Dauer × Wirkung auf Organisation/Mitarbeiter/Umwelt (6.1, 8.2) | **Freitext umgesetzt** (W5‑B‑12/G6 des Szenarienkonzepts) | es fehlen **Kategorie und Beurteilung** nach Dauer × Wirkung; fließt nie in den NPV, immer in den Bericht |

**Umgesetzt mit E17 (#479):** „gebaut #479 (E17, Schemaschritt 127)" mit der Tabelle, den Skalen, der Regel der
Beurteilung, dem Altfeld und dem Ausweis in Bericht und Checkliste.

### 8.32 Berichtigungen im gültigen Stand (#479)

Die Stellen, die mit E17 veraltet sind; „vorher" ist der Wortlaut vor #479 (Stand `c99c4c7a` = #478 samt seinen Papieren
und #482). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3) und Schrittabsatz (Z. 36) | Codestand `dedfc760`, Zielversion 125, „Schemaschritte 90–125 vergeben"; der Absatz endete mit dem Schritt 125 der Etappe E15 | Codestand `52614c33`, Zielversion 127, „90–127 vergeben … 126 die Reparatur der Gebäude-Katalogsätze (#485); 127 die nicht monetarisierbaren Wirkungen, Etappe E17"; dazu der Schritt **127** (`SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN`) mit Tabelle und Freitext-Übernahme |
| § 2.11.2, Zuordnungstafel V‑G ↔ G (Z. 781) | „… der Freitext ist mit W5‑B‑12/G6 gebaut" | dazu „die Liste mit Kategorie und Beurteilung mit E17 (#479)" |
| § 2.11.2, Zeile V‑G11 (Z. 795) | Wortlaut in § 8.31 | „gebaut #479 (E17, Schemaschritt 127)" samt Stand |
| § 2.11.2, Zeile V‑G12 (Z. 796) | endete mit Punkt 9 | dazu „Punkte 2b und 3b …, gebaut #479" — erfüllt, teilweise, offen; der Freitext zählt nicht mehr |
| § 2.11.4, Zeile V‑E (Z. 849) und Fußnote (Z. 855) | endete mit „V‑G3 offen — der Auftrag E16"; Fußnote „… und das Risikomodul E15 (#478, …)" | dazu „Den Freitext aus W5‑B‑12 löst E17 (#479) ab … nur noch V‑G3 offen (→ E16)"; Fußnote mit „die nicht monetarisierbaren Wirkungen E17 (#479, V‑G11, Schemaschritt 127)" |
| § 2.11.6, „Dauerhaft Werte bleiben" (Z. 1069) | „… Empfehlungssatz, nicht monetäre Wirkungen)" | dazu „mit #479 die Tafel der Wirkungen, die Beurteilung als Zahl, keine Formel" |
| § 6.1 | Kurztafel bis E15 (#478) | Zeile „E17 Nicht monetarisierbare Wirkungen" (#479) mit Schemaschritt 127 |
| § 6.2 (Z. 2747) | endete mit „… E15 bewegt keinen Anker." | dazu `NichtMonetaereWirkungenTests`, `WirkungenListeTests` und zwei Fälle der Blattstruktur-Wache; „E17 bewegt keinen Anker" |
| § 7 (Z. 2957–2958) | „Freigegeben und im Bau ist V‑G11 (nicht monetarisierbare Wirkungen, E17); V‑G3 (…, E16) folgt nach E15" | „Die Lücke V‑G11 ist mit E17 (#479) gebaut … vier Fragen offen (→ Register R‑E17). Aus der Gap-Tafel … ist nur noch V‑G3 (…, E16, #484) offen" |
| Anhang (Z. 3002, 3018, 3020, 3047) | Zeile V‑A…V‑E ohne V‑G11; Kürzelzeilen bis #478; Mockup-Zeile „… Risikozeilen der Mappe #478"; Etappenzeilen bis „E15 — Risikomodul (V‑G7)" | „der Freitext aus W5‑B‑12 abgelöst durch V‑G11 = E17 (gebaut #479)"; Kürzelzeile der Welle (#479); „… Punkte 2b und 3b mit der Wirkungsliste #479"; Etappenzeile „E17 — Nicht monetarisierbare Wirkungen (V‑G11)" = #479 |

### 8.33 E16 — Wiederholperiode je Kostenposition: „alle n Jahre", Schemaschritt 129 (#484)

Protokoll [`E16_Wiederholperiode_Protokoll.md`](E16_Wiederholperiode_Protokoll.md); im Register die Zeile V‑G3 in R‑V
und die neue Familie R‑E16. Die Welle folgt dem Auftrag des Anwenders vom 24.09.2026 („V‑G3 n‑jährliche Zeitpunkte:
Ausbau der Bemessung an den Kostenpositionen (eine Wiederholperiode je Position plus Rechenweg und Ausweis), ebenfalls
mit Schemaspalte"); sie baut aus V‑E die n-jährlichen Zeitpunkte, die E9 nicht gebaut hatte (E9a‑Q6 a), und ist keine
eigene Etappe des Plans E0–E12. Der Schritt heißt 129; 128 ist der Heizkreis je Gebäude der Anlagenkopplung AK1,
Welle 3, der zuerst gepusht wurde.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E16** (#484, Merge `ae7b0ed0` über `c778ab12`, der Baum gleich `7c8f4fc4`) | Schemaschritt 129 mit der Spalte `Wiederholperiode_a` an `Tab_ProjektWerte` und `Tab_KostenVorlagePosition` (leer, 0, 1 = jährlich; Testdatenbank 129); eine Betriebsposition mit n ≥ 2 zahlt in s, s + n, … ≤ T (`KapitalwertRechner.ZahltImJahr`, E16‑Q1 a), nur Betriebspositionen (E16‑Q2 a), Betriebskosten p. a. = Zahl des ersten Jahres (E16‑Q3 a); das Ganzzahlfeld „Zahlung alle: [n] Jahre" im Zeileneditor der Betriebsseite und in den Kostenvorlagen, die Vorlagenübernahme, KI-Feld `wiederholperiode`; „alle n Jahre ab Jahr X" in der Betriebskostentabelle beider Berichte, Hilfsspalte je Topf in der Formelmappe, Nachweisumschlag Fassung 11 | **ja**, je Pflege — ohne Pflege bitgleich (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich); A/B an 1030 (n = 2) gleich der Handrechnung in allen drei Szenarien |

*§ 2.11.2, Gap-Tabelle, Zeile V‑G3 (Z. 791):*

> | V-G3 | **Zeitpunktattribut** je Cashflow: Periode 0 · jährlich · alle n Jahre · einmalig in k (6.3.1) | teilweise (StartJahr, Ersatz über Nutzungsdauer) | „alle n Jahre" fehlt (z. B. Dichtheitsprüfung alle 2 a) — kleiner Ausbau der Bemessung |

**Umgesetzt mit E16 (#484):** „gebaut #484 (E16, Schemaschritt 129)" mit allen vier Zeitpunktarten, der Spalte, der
Regel der Zahlungsjahre, dem Pflegeort und dem Ausweis.

### 8.34 Berichtigungen im gültigen Stand (#484)

Die Stellen, die mit E16 veraltet sind; „vorher" ist der Wortlaut vor #484 (Stand `c778ab12`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3), Liste der fremden Schritte (Z. 5, Z. 18) und Schrittabsatz (Z. 39–40) | Codestand `52614c33`, Zielversion 127, „Schemaschritte 90–127 vergeben"; „… 119 und 121 bis 124 gehören nicht diesem Feld"; der Absatz endete mit dem Schritt 127 der Etappe E17 und „126 … gehört nicht diesem Feld" | Codestand `ae7b0ed0`, Zielversion 129, „90–129 vergeben … 128 der Heizkreis je Gebäude im Ergebnis (Anlagenkopplung AK1, Welle 3); 129 die Wiederholperiode je Kostenposition, Etappe E16"; 128 in der Liste der fremden Schritte; dazu der Schritt **129** (`Wiederholperiode_a`, `WiederholperiodeSchema.SCHRITT`) und „126 … und 128 … gehören nicht diesem Feld" |
| § 2.11.2, Zeile V‑G3 (Z. 791) | Wortlaut in § 8.33 | „gebaut #484 (E16, Schemaschritt 129)" samt Stand |
| § 2.11.4, Zeile V‑E (Z. 853) und Fußnote (Z. 855–860) | „**V‑G3 offen** — der Auftrag E16 (…)"; „Aus der Gap-Tafel § 2.11.2 ist damit nur noch V‑G3 offen (→ E16)"; Stand „… Risiko gebaut #478 (E15), V‑G3 offen (E16)"; Fußnote endete mit „… die nicht monetarisierbaren Wirkungen E17 (#479, V‑G11, Schemaschritt 127)" | „n-jährliche Zeitpunkte (V‑G3) gebaut #484 mit dem eigenen Auftrag E16 …"; „Mit V‑G3 (E16, #484) ist die Gap-Tafel § 2.11.2 geschlossen — keine Lücke offen"; Wirkung „… und die Wiederholperiode (leer = jährlich)"; Stand „V‑G3 gebaut #484 (E16)"; Fußnote mit „die Wiederholperiode je Kostenposition E16 (#484, V‑G3 aus V‑E, Schemaschritt 129)" |
| § 2.11.6, Stufe 1 (Z. 1019–1022) | die Hilfsspalten „Basis Betrieb mit p_B/p_E" samt den Stufen späterer Startjahre | dazu die Hilfsspalte „Positionen alle n Jahre mit p_B/p_E" mit `IF(AND(Jahr>=s,MOD(Jahr-s,n)=0),Betrag,0)` und die Betriebszelle `(Basis+Wiederholt)*(1+p)^(Jahr-1)` |
| § 2.13 (3), nach dem Absatz „Ersatz und Restwert je Position — entkoppelt" (Z. 1282) | — | neuer Absatz „Betriebspositionen „alle n Jahre"": Spalte, Regel, Startjahr = Beginn der Folge, Nutzungsdauer/Ersatz/Restwert unberührt, Zeileneditor, Kostenvorlagen, Übernahme, Assistent |
| § 3.1 (Z. 1630–1632 und Z. 1678) | A_t ohne n-jährliche Positionen; der Block „Nutzungsdauer, Ersatz, Restwert, Startjahr" endete mit `RestwertAnsetzen` | A_t mit „+ Σ_w Betrag_w × (1 + p_w)^(t−1) · [t zahlt]"; dazu der Block „Wiederholperiode einer Betriebsposition" mit den Zahlungsjahren s, s + n, … ≤ T |
| § 3.4, Betriebskostentabelle (Z. 1880) | Punkte „Startjahr" und „Probe der Gliederung" ohne Periode | neuer Punkt „Alle n Jahre": „alle n Jahre ab Jahr X", Betriebskosten p. a., Nachweisumschlag Fassung 11 |
| § 6.1 (Z. 2714) | Kurztafel bis E17 (#479) | Zeile „E16 Wiederholperiode je Kostenposition" (#484) mit Schemaschritt 129 |
| § 6.2 (Z. 2759) | endete mit „… E17 bewegt keinen Anker." | dazu `WiederholperiodeTests` und fünf Dialogproben; „E16 bewegt keinen Anker" |
| § 7 (Z. 2970–2973) | „Aus der Gap-Tafel des § 2.11.2 ist nur noch V‑G3 (Wiederholperiode je Kostenposition, E16, #484) offen" | „Die Lücke V‑G3 ist mit E16 (#484) gebaut … vier Fragen offen (→ Register R‑E16). Damit ist die Gap-Tafel des § 2.11.2 geschlossen" |
| Anhang (Z. 3017, 3034, 3064) | Zeile V‑A…V‑E mit „V‑G3 = E16 (offen)"; Kürzelzeilen bis #479; Etappenzeilen bis „E17 — Nicht monetarisierbare Wirkungen (V‑G11)" | „V‑G3 = E16 (gebaut #484)"; Kürzelzeile der Welle (#484); Etappenzeile „E16 — Wiederholperiode je Kostenposition (V‑G3)" = #484 |

### 8.35 E18 — Restpunkte der Stromsteuer: Wache der Rückfallebene, erfasster Stromsteueranteil, Nr. 18 nachgemessen (#492)

Protokoll [`E18_Restpunkte_Stromsteuer_Protokoll.md`](E18_Restpunkte_Stromsteuer_Protokoll.md); im Register die neue
Familie R‑E18. Die Welle folgt dem „fahre fort" des Anwenders vom 24.09.2026 auf die Empfehlung, die Wache Konstante
gegen Katalog zusammen mit Nr. 18 und Nr. 16 als nächste kleine Welle zu bauen; sie ist keine Etappe des Plans E0–E12
und kommt ohne Schemaschritt aus (130 ist #493, die Anschlusslängen im Gebäudekatalog). Die Fragen E18‑Q1…Q6 hat der
Orchestrator am 24.09.2026 mit der Baufreigabe nach Empfehlung a entschieden; E18‑Q7 ist der neue Restpunkt Nr. 33.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E18** (#492, Merge `e79bffb1` über `247e2091`, Zweig `e18` = `29fad34d` von `fbc4e536`) | Wache der Rückfallebene `StrompreisZerlegungModel` gegen die älteste Zeile je Schlüssel in Saat und Testdatenbank (Stromsteuersätze und drei Umlagen), zwei tote Ressourcen gestrichen (E18‑Q1 a, Q2 a); `StrompreisZerlegungCtrl.StromsteuerErfasst`, der rohe Leseweg aus `KohaerenzPruefung` verschoben (Q6 a); der erfasste Stromsteueranteil unter der Unternehmensart im Dialog „BHKW-Wirtschaftlichkeit", Gruppe 4 und Überlagerung, mit Satzabgleich und Kohärenzzeile ohne Sperre (Q4 a, Q5 a); Nr. 18 nachgemessen, offen, HB1-O1 an den fünf Rechenweg-Sortierungen vermerkt (Q3 a) | **nein** — Anker unberührt, Referenzlauf 13/13 gegen R14 byte-gleich |

*§ 6.3 Nr. 14 (Z. 2884):*

> 14. ~~Reduzierter Stromsteuersatz bleibt Konstante bis zur Katalog-Nachpflege~~ — überholt, siehe Protokoll; offen bleibt allein, dass **keine Wache Konstante gegen Katalog** hält (§ 6.5)

**Erledigt mit E18 (#492):** Die Wache `StrompreisZerlegungTests.Die_Rueckfallebene_steht_wertgleich_im_Katalog_der_Testdatenbank`
hält die Konstanten gegen die älteste Zeile des Katalogs der Testdatenbank, der Saattest
`Die_Katalogwerte_und_die_Rueckfallebene_sind_wertgleich` gegen die älteste Saatzeile (statt `JahrVon == 2026`); die
Meldung nennt die drei nachzuziehenden Orte; die Ressourcen `PREIS_STROMSTEUER_REGELFALL/_REDUZIERT` sind gestrichen.
Gegenprobe: `STROMST_REGELSATZ` 2026 = 21 EUR/MWh in einer Kopie der Testdatenbank → rot.

*§ 6.3 Nr. 16 (Z. 2886):*

> 16. Rückweg „Parameterdialog zeigt den erfassten Preisanteil" fehlt

**Erledigt mit E18 (#492):** Die Pflegestelle der Unternehmensart ist seit dem Auszug der Steuerfelder der Dialog
„BHKW-Wirtschaftlichkeit", nicht der Parameterdialog; dort zeigen Gruppe 4 und die Überlagerung „Sätze und Herkunft"
den erfassten Stromsteueranteil mit Satzabgleich und Kohärenzzeile, gepflegt wird er nur in „Strompreis Details"
(E18‑Q4 a, Q5 a). Herkunft des Punkts: B4 § 4 Grenze 3.

*§ 6.3 Nr. 18 (Z. 2888–2890):*

> 18. ~~Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1)~~ — **vermutlich überholt**:
>     `SimulationControl.cs:1512, 3372, 4117` sortieren `ORDER BY Prioritaet, ID`; ob dies die
>     gemeinte Stelle ist, ist nicht gegengeprüft — nachmessen, dann streichen

**Nachgemessen mit E18 (#492), offen:** Die Stellen waren die gemeinten, aber es sind fünf — `SimulationControl` dreimal
und `WaermesenkeClass` zweimal —, und sie sortieren weiterhin ungepflegt vor gepflegt; die 99er-Regel änderte die
Reihenfolge in 5 von 13 Referenzprojekten. Der Umbau ist eine eigene Etappe mit neuem Referenzlauf (E18‑Q3 a); die
Durchstreichung entfällt, der Punkt steht wieder offen, die Stellen tragen den Vermerk „HB1-O1, offen".

*§ 6.5, Zeile Stromsteuersatz (Z. 2962):*

> | Stromsteuersatz an zwei Orten — Katalog `STROMST_REGELSATZ` und `STROMST_REDUZIERT_SATZ` gegen die `const double` in `StrompreisZerlegungModel` | wertgleich, **gekoppelt ist nichts**: Die vorhandene Wache (`StrompreisZerlegungTests`) prüft Modell gegen Konstante, **nicht** Konstante gegen Katalog — eine gepflegte Novelle erreicht die Modellkonstante nicht, und eine Wache dafür fehlt. Der reduzierte Satz ist gesät; die Konstante ist ausdrücklich nur noch Rückfallebene (§ 6.3 Nr. 14) |

**Berichtigt mit E18 (#492):** Die Aussage war unzutreffend — die Wache hielt die Konstante schon gegen die Saat, aber
nur für `JahrVon == 2026`; es fehlten die Testdatenbank und die Robustheit gegen eine spätere Jahreszeile. Neu:
gekoppelt durch zwei Wachen (Saat und Testdatenbank, älteste Zeile) — eine Novelle ist eine spätere Jahreszeile und
lässt die Rückfallebene stehen.

### 8.36 Berichtigungen im gültigen Stand (#492)

Die Stellen, die mit E18 veraltet sind; „vorher" ist der Wortlaut vor #492 (Stand `247e2091`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3), Liste der fremden Schritte (Z. 5, Z. 18–19) und Schrittabsatz (Z. 39–40) | Codestand `ae7b0ed0`, Zielversion 129, „Schemaschritte 90–129 vergeben"; „… 121 bis 124 und 128 gehören nicht diesem Feld"; der Absatz endete mit „126 … und 128 … gehören nicht diesem Feld" | Codestand `e79bffb1`, Zielversion 130, „90–130 vergeben … 130 die Anschlusslängen im Gebäudekatalog (#493); E18 ohne Schritt"; 130 in der Liste der fremden Schritte (`GebaeudeAnschlusslaengenReparatur.SCHRITT`, reines DML); „126 …, 128 … und 130 … gehören nicht diesem Feld; die Etappe E18 (#492) kommt ohne Schritt aus" |
| § 2.2, Gruppe 4 (nach Z. 309) | — | neuer Absatz „Erfasster Stromsteueranteil (gebaut #492, E18; § 6.3 Nr. 16)": Ort, Leseweg, Herleitungs- und Kohärenzzeile, Satzquelle, nur Anzeige, nur mit BHKW erreichbar |
| § 6.1 (Z. 2746) | Kurztafel bis E16 (#484) | Zeile „E18 Restpunkte Stromsteuer" (#492), ohne Schemaschritt |
| § 6.3 Nr. 14, 16, 18 (Z. 2884, 2886, 2888–2890) | Wortlaut in § 8.35 | Nr. 14 „überholt; die Wache Konstante gegen Katalog erledigt mit E18 (#492)"; Nr. 16 durchgestrichen, „erledigt mit E18 (#492)"; Nr. 18 offen mit den fünf Stellen, der Messung 5 von 13 und dem Vermerk im Code |
| § 6.3, neuer Block vor „Nachweis und Betrieb" (vor Z. 2925) | — | „Aus Etappe E18 (#492)", Nr. 33 „Unternehmensart ohne BHKW nicht pflegbar" (E18‑Q7), offen |
| § 6.5, Zeile Stromsteuersatz (Z. 2962) | Wortlaut in § 8.35 | „wertgleich und gekoppelt durch zwei Wachen (E18, #492)" — Saat und Testdatenbank, älteste Zeile, die Meldung mit den drei Orten, eine Novelle als spätere Jahreszeile |
| § 6.5, Zeile „Energieintensiv" an drei Orten (Z. 2963) | „seit B4 liest die Schnellwahl den Katalog und die Unternehmensart hebt den passenden Knopf hervor; gekoppelt ist weiterhin nichts" | dazu „umgekehrt zeigt der Dialog „BHKW-Wirtschaftlichkeit" den erfassten Stromsteueranteil gegen die gewählte Unternehmensart (E18, #492, § 2.2 Gruppe 4) — ein Hinweis ohne Sperre" |
| § 7 (Z. 3011) | endete mit „Damit ist die Gap-Tafel des § 2.11.2 geschlossen." | dazu der Satz zu E18 (#492): Nr. 14 und 16 erledigt, Nr. 18 offen, Nr. 33 neu, sechs Fragen entschieden (→ Register R‑E18) |
| Anhang (Z. 3073, 3104) | Kürzelzeilen bis #484; Etappenzeilen bis „E16 — Wiederholperiode je Kostenposition (V‑G3)" | Kürzelzeile der Welle (#492); Etappenzeile „E18 — Restpunkte Stromsteuer (§ 6.3 Nr. 14, 16, 18)" = #492 |

### 8.37 E19 — Restpunkte der Wirtschaftlichkeit: Nr. 15 überholt, Unternehmensart ohne BHKW im Parameterdialog (#498)

Protokoll [`E19_Unternehmensart_ohne_BHKW_Protokoll.md`](E19_Unternehmensart_ohne_BHKW_Protokoll.md); im Register die
neue Familie R‑E19. Die Welle folgt dem „fahre fort“ des Anwenders vom 25.09.2026 nach dem Statusbericht, auf den
Vorschlag des Orchestrators als nächste kleine Welle; sie ist keine Etappe des Plans E0–E12 und kommt ohne Schemaschritt
aus (140 ist Z5 #495, 141 die Folgeberichtigung der Anschlusslängen #496). Die Fragen E19‑Q1…Q6 hat der Orchestrator am
25.09.2026 (08:20) mit der Baufreigabe nach Empfehlung entschieden — Q4 b, die übrigen a.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E19** (#498, Merge `31a0b085` über `f55a4cd5`, Zweig `e19` = `bfa2c9e5` von `a0bbc633`) | Nr. 15 als überholt geschlossen, Wache `KatalogjahrJeOeffnungTests` (E19‑Q1 a); `WirtschaftlichkeitParameterHuelle` mit `Stromsteueranteil` und `Katalog`; die Unternehmensart ohne BHKW im Parameterdialog, Gruppe Strom, mit der Anzeige des erfassten Stromsteueranteils live und der § 9b-Erklärzeile, mit BHKW nur der Verweis (Q2 a, Q3 a, Q6 a: ein neuer Schlüssel `WPAR_UA_9B_HINWEIS`); das Bilanzjahr bleibt in der Gruppe Brennstoff (Q4 b); KI-Feld `unternehmensart` mit Sperre bei BHKW, Maskenwache 35 (Q5 a) | **nein** — Anker unberührt, Referenzlauf 13/13 gegen R14 byte-gleich; § 9b ohne BHKW erreichbar, der Kapitalwert ändert sich nur mit einer gewählten Unternehmensart |

*§ 6.3 Nr. 15 (Z. 2901):*

> 15. Bilanzjahr und Unternehmensart wirken erst beim nächsten Dialog-Öffnen

**Überholt, geschlossen mit E19 (#498):** Der Punkt stammt aus der Grenze 2 des B4-Protokolls — der WinForms-Trägerdialog
las Bilanzjahr und Unternehmensart beim Blockaufbau. Seit der Schalentrennung liest die Hülle der
Energieträgerverwaltung beide Werte bei jeder Öffnung (`EnergietraegerHuelle.Gaben`; jede Öffnung eine neue Hülle —
Windows das modale `EnergietraegerFenster`, die Kostenseite `KostenSeiteGaben.TraegerGaben`); aus dem Trägerdialog führt
kein Weg in Parameter- oder BHKW-Dialog; die Wirtschaftlichkeitsseite frischt nach jedem Unterdialog auf; die Anzeige des
Stromsteueranteils folgt im BHKW- und jetzt im Parameterdialog live. Kohärenzzeilen, Vorschau, Ergebnis und
§ 9b-Betrag gelten bewusst erst nach einem Rechenlauf. Die Wache `KatalogjahrJeOeffnungTests` hält fest: nach dem
Speichern von produzierendem Gewerbe und Bilanzjahr 2025 empfiehlt die nächste Öffnung den reduzierten Satz des Jahres
2025.

*§ 6.3 Nr. 33 (Z. 2945–2950):*

> **Aus Etappe E18 (#492)**
>
> 33. **Unternehmensart ohne BHKW nicht pflegbar** (E18‑Q7, → Register R‑E18): Die Unternehmensart steht nur im Dialog
>     „BHKW-Wirtschaftlichkeit", und die Wirtschaftlichkeitsseite bietet ihn nur an, wenn die Vergleichsgruppe ein BHKW
>     führt (`WirtschaftlichkeitSeite.razor`, `_stand.MitBhkw`). Für Projekte ohne BHKW sind damit die Wahl für § 9b und
>     die Anzeige des erfassten Stromsteueranteils (Nr. 16, § 2.2 Gruppe 4) nicht erreichbar — offen

**Erledigt mit E19 (#498):** Ohne BHKW pflegt der Parameterdialog in der Gruppe Strom die Unternehmensart samt der
Anzeige des erfassten Stromsteueranteils und einer § 9b-Erklärzeile; mit BHKW bleibt der Dialog
„BHKW-Wirtschaftlichkeit“ die Pflegestelle, die KI lehnt dort benannt ab (E19‑Q2, Q3, Q5, Q6 a). Kein Schema, kein
Kern-Umbau — § 9b ohne BHKW rechnet der Kern seit E5, er war über die Oberfläche nur nicht erreichbar. Benannte Grenze
(E19‑Q4 b): Das Bilanzjahr bleibt in der Gruppe Brennstoff; ohne Kessel und BHKW gilt der Rückfall 2026, nur für
Katalogjahr und Anzeige. Nebenbefund: 1017 führt ein BHKW, der Vorbehalt in A‑E18‑1 ist gegenstandslos.

*§ 2.2, Gruppe 4, letzter Satz des Absatzes „Erfasster Stromsteueranteil“ (Z. 323–324):*

> allein in „Strompreis Details" (§ 6.5); Entscheide E18‑Q4 und E18‑Q5 (→ Register R‑E18). Die Unternehmensart ist nur
> erreichbar, wenn die Vergleichsgruppe ein BHKW führt (§ 6.3 Nr. 33).

**Berichtigt mit E19 (#498):** Ohne BHKW pflegt der Parameterdialog die Unternehmensart in der Gruppe Strom mit
derselben Anzeige (§ 2.4).

### 8.38 Berichtigungen im gültigen Stand (#498)

Die Stellen, die mit E19 veraltet sind; „vorher“ ist der Wortlaut vor #498 (Stand `f55a4cd5`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3), Liste der fremden Schritte (Z. 5, Z. 18–20) und Schrittabsatz (Z. 45–46) | Codestand `e79bffb1`, Zielversion 130, „Schemaschritte 90–130 vergeben … E18 ohne Schritt“; „… 128 und 130 gehören nicht diesem Feld“; der Absatz endete mit „… und 130 … gehören nicht diesem Feld; die Etappe E18 (#492) kommt ohne Schritt aus“ | Codestand `31a0b085`, Zielversion 141, „90–141 vergeben … 131 Z4b, 132–139 G3/G4/AK1, 140 Z5, 141 die Folgeberichtigung; E19 ohne Schritt“; 131 bis 141 in der Liste der fremden Schritte; „… und 131 bis 141 … gehören nicht diesem Feld; die Etappen E18 (#492) und E19 (#498) kommen ohne Schritt aus“ |
| § 2.2, Gruppe 4 (Z. 323–324) | Wortlaut in § 8.37 | „Führt die Vergleichsgruppe kein BHKW, pflegt der Parameterdialog die Unternehmensart in der Gruppe Strom mit derselben Anzeige (§ 2.4; gebaut #498, E19; § 6.3 Nr. 33).“ |
| § 2.4 (nach Z. 382) | — | neuer Absatz „Unternehmensart ohne BHKW (gebaut #498, E19; § 6.3 Nr. 33)“: Ort, Wahlen, Anzeige, § 9b-Erklärzeile, eine Pflegestelle je Projektlage, KI-Sperre, Grenze Bilanzjahr |
| § 3.8 (nach Z. 2428) | — | neuer Absatz „§ 9b ist ohne BHKW erreichbar“: Bedingung, Pflegestellen, Nachweis an 1041 |
| § 6.1 (Z. 2762) | Kurztafel bis E18 (#492) | Zeile „E19 Restpunkte Unternehmensart“ (#498), ohne Schemaschritt |
| § 6.3 Nr. 15 und 33 (Z. 2901, 2945–2950) | Wortlaut in § 8.37 | Nr. 15 durchgestrichen, „überholt durch die Schalentrennung, Wache mit E19 (#498)“; Nr. 33 durchgestrichen, „erledigt mit E19 (#498)“ mit der Grenze Bilanzjahr; der Block heißt „Aus Etappe E18 (#492) — geschlossen“ |
| § 6.3 Nr. 10, 11, 13, 18, 19 (Z. 2896, 2897, 2899, 2904–2910, 2911–2914) | Wortlaut in § 8.39 | die Anwenderentscheide vom 25.09.2026 (→ Register R‑Rest): Nr. 10 präzisiert, offen; Nr. 11 und 13 geschlossen; Nr. 18 gemessen, offen; Nr. 19 belassen |
| § 6.5, Zeile „Energieintensiv“ an drei Orten (Z. 2990) | endete mit „… — ein Hinweis ohne Sperre; gekoppelt ist weiterhin nichts“ | dazu „ohne BHKW steht die Unternehmensart mit derselben Anzeige im Parameterdialog, Gruppe Strom (E19, #498, § 2.4) — je Projektlage eine Pflegestelle“ |
| § 7 (Z. 3042) | endete mit dem Satz zu E18 (#492) | dazu die Sätze zu E19 (#498) und zu den Anwenderentscheiden vom 25.09.2026 (→ Register R‑E19, R‑Rest) |
| Anhang (Z. 3105, 3137) | Kürzelzeilen bis #492; Etappenzeilen bis „E18 — Restpunkte Stromsteuer“ | Kürzelzeile der Welle (#498); Etappenzeile „E19 — Restpunkte Unternehmensart (§ 6.3 Nr. 15, 33)“ = #498 |

### 8.39 Anwenderentscheide vom 25.09.2026 zu § 6.3 Nr. 10, 11, 13, 18 und 19 (nachgetragen mit #498)

Der Anwender hat am 25.09.2026 zu fünf offenen Punkten des § 6.3 entschieden; der Orchestrator hat die Entscheide mit
den Papieren zu #498 nachgetragen (→ Register R‑Rest, eine neue Familie in der Folge von R‑NR). Kein Bau mit dieser
Statuszeile. Der Wortlaut vor #498 und der Entscheid:

*§ 6.3 Nr. 10 (Z. 2896):*

> 10. Bezugsgrößen der übrigen KD1-Bemessungsarten (H1-1b)

**Präzisiert, offen:** „Wärmepumpe beides“ nur bei den Investitionskosten nach kW elektrisch und kW thermisch — die
kWh-Bemessung der Wärmepumpe bleibt thermisch, Strom-kWh sind Energiekosten. Eine kleine Bauwelle folgt.

*§ 6.3 Nr. 11 (Z. 2897):*

> 11. Nachzieh-Migration für Bestandsprojekte — durch die Auto-Anlage entschärft, bleibt Option

**Geschlossen:** „nicht nachziehen“ — die Auto-Anlage deckt die Bestandsprojekte, eine Nachzieh-Migration wird nicht
gebaut.

*§ 6.3 Nr. 13 (Z. 2899):*

> 13. Pufferkapazität bleibt null — bewusste Grenze

**Geschlossen:** „nur Volumen“ — die Grenze ist bestätigt.

*§ 6.3 Nr. 18 (Z. 2904–2910):*

> 18. Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1) — **offen**, nachgemessen mit E18 (#492): Die fünf Rechenweg-Leser
>     `SimulationControl` (`WP_Liste_Laden`, `QuellbezuegeAufbauen`, `SenkenPufferDerAnlagen`) und `WaermesenkeClass`
>     (`SenkenLaden`, `SenkenlistenLaden`) sortieren `ORDER BY Prioritaet, ID` und damit ungepflegt (NULL) vor gepflegt;
>     48 von 60 Wärmeerzeugern der Testdatenbank tragen keine Priorität. Die 99er-Regel der Anzeige
>     (`Ladeordnung.SqlAnlagenprio`) änderte die Reihenfolge in 5 von 13 Referenzprojekten (1030, 1040, 1041, 1042, 1045;
>     in 1042 die Modulreihenfolge der Wärmepumpen) — der Umbau ist eine eigene Etappe mit neuem Referenzlauf (E18‑Q3 a,
>     → Register R‑E18); die fünf Stellen tragen im Code den Vermerk „HB1-O1, offen"

**Offen, nach Empfehlung:** zuerst eine Messwelle, danach der Entscheid über den Umbau. Gemessen 25.09.2026 (Probeumbau der fünf Rechenweg-Sortierungen auf die 99er-Regel, Worktree `mess18`, nicht gemergt): ohne Rechenwirkung — 12 von 13 Referenzprojekten byte-gleich, nur 1042 tauscht in `aggregate.csv` die Modulreihenfolge der beiden Wärmepumpen (10 Werte, Werte gleich, Index anders); Deckung, Endenergie, CO₂, Kapitalwert unverändert; kein Test rot. Nebenbefund: Die Modul-Lader für Kessel, Solarthermie und BHKW sortieren gar nicht. Empfehlung: den Umbau mit der nächsten ohnehin fälligen Neueinfrierung der Referenzbasis bündeln und dann über die drei unsortierten Lader mitentscheiden; der Anwenderentscheid steht aus.

*§ 6.3 Nr. 19 (Z. 2911–2914):*

> 19. Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung Kessel" — **dokumentiert mit E10 (#463)** (E10‑Q6, Lesart a,
>     → Register R‑E10): Der Kessel führt seine Wartung je Katalogeintrag in €/a, €/kWh oder %/a, und ein neuer Eintrag
>     in %/a übernimmt den Wartungssatz der Nutzungsdauertabelle; das BHKW führt sie fest in €/kWh el
>     (`Wartungskosten_kwhel`) und bekommt keine Vorbelegung. Behoben ist die Asymmetrie nicht.

**Belassen:** E10‑Q6 a ist bestätigt — €/kWh el. ist beim BHKW die übliche Vertragsform.

### 8.40 E21 — Pflegewelle: § 6.3 Nr. 23 resx-Sammelnachtrag erledigt, Nr. 24 und die Datenlücken benannt (#506)

Protokoll [`E21_Pflege_Ressourcen_Testdaten_Protokoll.md`](E21_Pflege_Ressourcen_Testdaten_Protokoll.md); im Register
die neue Familie R‑E21. Die Welle folgt dem Anwenderwort „sonst nach Empfehlung“ vom 25.09.2026; sie ist keine Etappe
des Plans E0–E12 und kommt ohne Schemaschritt aus, `SchemaStand.Zielversion` bleibt 141. Die Fragen E21‑Q1…Q9 hat der
Orchestrator am 25.09.2026 (09:45) mit der Baufreigabe nach Empfehlung entschieden — alle a.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E21** (#506, Merge `e7c2f8f7` über `b5cc1a61`, Zweig `e21` = `02ea7650` von `cbed6dba`) | zwei verwaiste Ressourcenschlüssel aus de/en/Designer gestrichen (E21‑Q1 a), drei Rückfall-Literale an die resx angeglichen (E21‑Q2 a), Kommentare `KiDialoge.cs` und `SteuerGutschriftRechner.cs` nachgezogen; die Datenlücken der Projekte 1018, 1024, 1023, 1030, 1026 gemessen und benannt, keine Datenpflege (E21‑Q3…Q8 a); ein PV-Projekt mit vollständigen Preisen bleibt eine spätere, eigene Welle (E21‑Q9 a) | **nein** — Anker unberührt, Referenzlauf 13/13 gegen R14 byte-gleich, Testdatenbank unverändert |

*§ 6.3 Nr. 23 (vor #506):*

> 23. resx-Sammelnachtrag der Textschlüssel aus B3a, B3b, B4 und der F-Serie

**Erledigt mit E21 (#506):** 26 Schlüssel aus B3a/B3b/B4/F2/FX1–FX5 geprüft; zwei ohne Leser gestrichen
(`PREIS_ST_GRUND_EINHEIT`, `STEUER_ENERGIEST_54_BEMESSUNG`), die übrigen tragen einen Leser, de/en sind
deckungsgleich, keine Dubletten; drei Rückfall-Literale in `EnergietraegerHuelle.cs` auf das schließende
Anführungszeichen der resx (U+201C) angeglichen — alle 22 Code-Rückfälle der B3/B4/F-Serie sind jetzt zeichengleich
mit der deutschen resx.

*§ 6.3 Nr. 24 (vor #506):*

> 24. Datenpflege: Projekt 1018 Kessel ohne Energieträger, Puffer ohne Temperaturpaar; WP-Kennlinie 1024 ohne
>     HT-Stützstellen

**Gemessen 25.09.2026, benannt, keine Pflege (E21):** An keinem der fünf geprüften Referenzprojekte (1018, 1024,
1023, 1030, 1026) bleibt eine Pflege der Lücke byte-gleich gegen die Basis; 1018 (Kessel 1018251 ohne Energieträger,
Puffer ohne Temperaturpaar) und 1023 (Kessel 11205 ohne Energieträger, keine eps-Zeile Erdgas) sind Kandidaten für
die nächste Neueinfrierung nach R15; 1024 (WP-Kennlinie 1034317 endet beim Herstellerkatalog bei 20 °C) ist kein
Datenfehler, der Kern kappt statt zu extrapolieren; 1030 bleibt Anker, nicht angefasst; 1026 ist ein gewollter
Prüffall ohne Stromträger. Ein PV-Projekt mit vollständigen Preisen folgt später als eigene Welle, außerhalb der
Referenzliste (E21‑Q9).

### 8.41 Berichtigungen im gültigen Stand (#506)

Die Stellen, die mit E21 veraltet sind; „vorher“ ist der Wortlaut vor #506 (Stand `cbed6dba`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3) | Codestand `31a0b085`; „… E19 ohne Schritt“ | Codestand `e7c2f8f7`; „… E19 und E21 ohne Schritt“ |
| § 6.3 Nr. 23 | Wortlaut in § 8.40 | durchgestrichen, „erledigt mit E21 (#506)“, siehe Protokoll |
| § 6.3 Nr. 24 | Wortlaut in § 8.40 | „gemessen 25.09.2026, benannt: …“ (Kurztafel), Kandidaten für die nächste Neueinfrierung, siehe Protokoll |

### 8.42 E20 — Investitionskosten der Wärmepumpe „je kW elektrisch“ aus der Kennlinie am Normpunkt (#502)

Protokoll [`E20_Waermepumpe_kW_elektrisch_Protokoll.md`](E20_Waermepumpe_kW_elektrisch_Protokoll.md); im Register die
neue Familie R‑E20 und die nachgezogene Zeile Nr. 10 von R‑Rest. Die Welle setzt den Anwenderentscheid zu § 6.3 Nr. 10
vom 25.09.2026 um (§ 8.39); sie ist keine Etappe des Plans E0–E12 und kommt ohne Schemaschritt aus,
`SchemaStand.Zielversion` bleibt 142 (#505). Die Fragen E20‑Q1…Q5, Q7 und Q8 hat der Orchestrator am 25.09.2026 (09:15) mit
der Baufreigabe nach Empfehlung entschieden — alle a; E20‑Q6 ist offen beim Anwender.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E20** (#502, Merge `49ea20e0` über `365143e1`, Zweig `e20` = `a414b768` von `cbed6dba`) | P_el der Wärmepumpe = Ptherm ÷ COP am Normpunkt der Kennlinie `Tab_Kenndaten` bei Vorlauf 35 (A2, B0, W10 je Bauart, interpoliert, nie extrapoliert; E20‑Q1…Q4 a); `EUR_PRO_KW_ELEKTRISCH` an der Wärmepumpe nur im Investitionsraster über den Schalter `investition` der Landkarte, die Betriebsseite bleibt GEWERK (E20‑Q5 a); Beschriftung unverändert, Grund GERAET bei fehlendem Normpunkt (E20‑Q7 a, Q8 a); Herleitung mit zwei neuen Schlüsseln; 13 Testfälle und die Kreuztafel je Raster | **nein** im Bestand — keine Zeile an der Wärmepumpe trägt die Art; Anker unberührt, Referenzlauf 13/13 gegen R14 byte-gleich; A/B an 1024: 1.000 €/kW → 4.000 € |

*§ 6.3 Nr. 10 (vor #502):*

> 10. Bezugsgrößen der übrigen KD1-Bemessungsarten (H1-1b) — **offen, präzisiert** (Anwender 25.09.2026, → Register
>     R‑Rest): „Wärmepumpe beides“ nur bei den Investitionskosten nach kW elektrisch und kW thermisch — die
>     kWh-Bemessung der Wärmepumpe bleibt thermisch, Strom-kWh sind Energiekosten; kleine Bauwelle folgt

**Anwenderregel umgesetzt mit E20 (#502):** Die Investitionskosten der Wärmepumpe bemessen sich je kW thermisch und
je kW elektrisch — P_el am Normpunkt der Kennlinie —, die Betriebskosten thermisch. **Offen bleibt E20‑Q6:** Das
Betriebsraster der Wärmepumpe bietet weiter „je kWh elektrisch“ (die Strommenge aus dem Lauf); ob die Art dort bleibt
(a, Empfehlung) oder entfällt (b), entscheidet der Anwender.

### 8.43 Berichtigungen im gültigen Stand (#502)

Die Stellen, die mit E20 veraltet sind; „vorher“ ist der Wortlaut vor #502 (Stand `365143e1`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3) | Codestand `e7c2f8f7`, Zielversion 141; „90–141 vergeben … E19 und E21 ohne Schritt“ | Codestand `49ea20e0`, Zielversion 142; „90–142 vergeben … 142 die dritte Reparatur der Anschlusslängen im Gebäudekatalog (#505); E19, E20 und E21 ohne Schritt“ |
| Schrittabsatz | „**131** bis **141** (… die Folgeberichtigung #496) …; die Etappen E18 (#492) und E19 (#498) kommen ohne Schritt aus“ | „**131** bis **142** (… die Folgeberichtigung #496, die dritte Reparatur #505) …; die Etappen E18 (#492), E19 (#498), E20 (#502) und E21 (#506) kommen ohne Schritt aus“ |
| § 3.2 Tafel der Runde 1 | ohne Zeile für die Wärmepumpe bei `EUR_PRO_KW_ELEKTRISCH` | neue Zeile „`EUR_PRO_KW_ELEKTRISCH` an der Wärmepumpe, nur Kategorie 1“ mit Σ (Ptherm ÷ COP am Normpunkt) × Satz aus `Tab_Kenndaten`; Satz „je Raster“ und Fußnote ¹ zur Normpunktregel |
| § 6.1 | — | neue Zeile E20 |
| § 6.3 Nr. 10 | Wortlaut in § 8.42 | „Anwenderregel umgesetzt mit E20 (#502), offen allein E20‑Q6“, siehe Protokoll |
| § 7 | „… Nr. 10 ist präzisiert — eine kleine Bauwelle folgt —“ | dahinter der Satz zur kleinen Welle E20 (#502) |
| Anhang | — | Kürzel- und Etappenzeile E20 |

### 8.44 E22 — Rechenweg-Sortierung nach der Regel „99“ des Hydraulikbilds, Referenzbasis R16 (#503)

Protokoll [`E22_Anlagenprio_Rechenweg_R16_Protokoll.md`](E22_Anlagenprio_Rechenweg_R16_Protokoll.md); im Register die
neue Familie R‑E22 und die nachgezogenen Zeilen Nr. 18 von R‑Rest und E18‑Q3 von R‑E18. Die Welle setzt den zweiten
Anwenderentscheid zu § 6.3 Nr. 18 vom 25.09.2026 um — „Nr. 18: so umsetzen, Umbau: Das Hydraulikbild wurde im August
(HB1) auf die richtige Regel umgestellt: ungepflegte Anlagen nach hinten, Regel ‚99‘“ —, gefallen nach der Messwelle,
die § 8.39 festhält; sie ist keine Etappe des Plans E0–E12 und kommt ohne Schemaschritt aus, `SchemaStand.Zielversion`
bleibt 142 (#505). E22‑Q1 ist im Bau nach der A/B-Messung entschieden, nach Empfehlung a.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E22** (#503, Merge `76f8661d` über `f83ce27d`, Zweig `e22` = `c6ee0961` von `cbed6dba`) | die fünf Rechenweg-Leser (`WP_Liste_Laden`, `QuellbezuegeAufbauen`, `SenkenPufferDerAnlagen`, `SenkenLaden`, `SenkenlistenLaden`) und die drei Modul-Lader (`SPK_Liste_Laden`, `Solar_Liste_Laden`, `BHKW_Liste_Laden`) nach `Ladeordnung.SqlAnlagenprio` (E22‑Q1 a), die Vermerke HB1-O1 entfernt, Wache `AnlagenprioRechenwegTests`; Neueinfrierung `2026-09-25_R16_Anlagenprio` (vierzehn Projekte) auf `2026-09-25_R15_Anlagenkopplung` | **nein** — Anker unberührt, alle Werte und Zeitreihen gleich; allein 1042 tauscht in `aggregate.csv` den Modulindex der beiden Wärmepumpen (10 Werte), deshalb die neue Basis |

*§ 6.3 Nr. 18 (vor #503, Z. 2950–2963):*

> 18. Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1) — **offen**, nachgemessen mit E18 (#492): Die fünf Rechenweg-Leser
>     `SimulationControl` (`WP_Liste_Laden`, `QuellbezuegeAufbauen`, `SenkenPufferDerAnlagen`) und `WaermesenkeClass`
>     (`SenkenLaden`, `SenkenlistenLaden`) sortieren `ORDER BY Prioritaet, ID` und damit ungepflegt (NULL) vor gepflegt;
>     48 von 60 Wärmeerzeugern der Testdatenbank tragen keine Priorität. Die 99er-Regel der Anzeige
>     (`Ladeordnung.SqlAnlagenprio`) änderte die Reihenfolge in 5 von 13 Referenzprojekten (1030, 1040, 1041, 1042, 1045;
>     in 1042 die Modulreihenfolge der Wärmepumpen) — der Umbau ist eine eigene Etappe mit neuem Referenzlauf (E18‑Q3 a,
>     → Register R‑E18); die fünf Stellen tragen im Code den Vermerk „HB1-O1, offen";
>     Anwender 25.09.2026 nach Empfehlung: zuerst eine Messwelle, danach der Entscheid (→ Register R‑Rest) —
>     **gemessen 25.09.2026** (Probeumbau der fünf Stellen auf die 99er-Regel, Worktree `mess18`, nicht gemergt): ohne
>     Rechenwirkung — 12 von 13 Referenzprojekten byte-gleich, nur 1042 tauscht in `aggregate.csv` die Modulreihenfolge
>     der beiden Wärmepumpen (10 Werte, Werte gleich, Index anders); Deckung, Endenergie, CO₂, Kapitalwert unverändert;
>     kein Test rot. Nebenbefund: Die Modul-Lader für Kessel, Solarthermie und BHKW sortieren gar nicht. Empfehlung: den
>     Umbau mit der nächsten ohnehin fälligen Neueinfrierung der Referenzbasis bündeln und dann über die drei
>     unsortierten Lader mitentscheiden; der Anwenderentscheid steht aus — offen

**Erledigt mit E22 (#503):** Rechenweg, Hydraulikbild und Erzeugerkarten folgen derselben Regel
`Ladeordnung.SqlAnlagenprio` — gepflegte Priorität zuerst, eine Anlage ohne Priorität (NULL oder 0) hinten, bei
Gleichstand die ID. Umgestellt sind die fünf Leser aus dem Wortlaut und — nach der A/B-Messung, E22‑Q1 a — die drei
Modul-Lader des Nebenbefunds. Die Messung des Baus bestätigt die Messwelle: allein 1042 tauscht in `aggregate.csv`
die Modulreihenfolge der beiden Wärmepumpen (10 Werte, Werte gleich), die drei Lader ändern in keinem Projekt etwas.
Die Empfehlung der Messwelle, den Umbau mit der nächsten ohnehin fälligen Neueinfrierung zu bündeln, hat der
Anwender nicht übernommen: E22 hat eine eigene Basis eingefroren.

**Die Basis-Kollision (Befund mit Lehre).** E22 fror zuerst auf R14 `2026-09-25_R15_Anlagenprio` ein (`0dd97e05`,
dreizehn Projekte); zeitgleich fror die Cloud-Sitzung der Anlagenkopplung, AK1 Welle 5 (`bcd61fe2`),
`2026-09-25_R15_Anlagenkopplung` ein (vierzehn Projekte, neu 1047, Einfrierregel „gesäte Auslegungsdaten der
Übergabe“, R14 entfernt) und stand zuerst auf `origin`. E22 ist beim Zusammenführen (`8cb5c69b`) auf diese Basis
gesetzt worden, der eigene Ordner ist verworfen, E22/6 (`c6ee0961`) hat R16 eingefroren und R15 archiviert. Lehre:
Zwei Sitzungen, die zugleich eine Basis einfrieren, brauchen Abstimmung; der Basisname ist erst beim Fetch vor dem
Push endgültig — wie Statusnummer und Schemaschritt.

### 8.45 Berichtigungen im gültigen Stand (#503)

Die Stellen, die mit E22 veraltet sind; „vorher“ ist der Wortlaut vor #503 (Stand `f83ce27d`). Je Stelle eine Zeile;
„mit dem Merge“ heißt: die Stelle hat E22/6 selbst nachgezogen.

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3) | Codestand `49ea20e0`; Referenzbasis `2026-09-25_R15_Anlagenkopplung`; „… E19, E20 und E21 ohne Schritt“ | Codestand `76f8661d`; Referenzbasis `2026-09-25_R16_Anlagenprio` (mit dem Merge); „… E19, E20, E21 und E22 ohne Schritt“ |
| Schrittabsatz | „die Etappen E18 (#492), E19 (#498), E20 (#502) und E21 (#506) kommen ohne Schritt aus“ | „…, E20 (#502), E21 (#506) und E22 (#503) kommen ohne Schritt aus“ |
| § 6.1 | — | neue Zeile E22 |
| § 6.2, Tafel, Zeile Referenzbasis | `Referenzlaeufe/2026-09-25_R15_Anlagenkopplung` | `Referenzlaeufe/2026-09-25_R16_Anlagenprio` (mit dem Merge) |
| § 6.3 Nr. 18 | Wortlaut in § 8.44 | durchgestrichen, „erledigt mit E22 (#503)“, siehe Protokoll |
| § 6.3 Nr. 21 | „heute gilt die Basis `2026-09-24_R14_Kaelteerzeuger`“ | „… `2026-09-25_R16_Anlagenprio`“ (mit dem Merge) |
| § 6.3 Nr. 24 | „Kandidaten für die nächste Neueinfrierung nach R15“ | „… nach R16“ |
| § 6.5, Zeile „Zwei Migrationsmechanismen“ | „aufgelöst bis auf drei Ergebnisspalten“ | „aufgelöst mit #501 (Cloud-Sitzung, 25.09.2026: ‚Ad-hoc-DDL der fünf Tabellen entfernt‘) bis auf drei Ergebnisspalten“ |
| § 7 | — | Satz zur kleinen Welle E22 (#503) |
| Anhang | — | Kürzel- und Etappenzeile E22 |

### 8.46 E23 — Betriebskosten der Wärmepumpe ohne kWh-Bemessung (#510)

Protokoll [`E23_Waermepumpe_Betrieb_ohne_kWh_Protokoll.md`](E23_Waermepumpe_Betrieb_ohne_kWh_Protokoll.md); im Register
die neue Familie R‑E23, die Zeile E20‑Q6 von R‑E20 und die Zeile Nr. 10 von R‑Rest. Die Welle setzt den
Anwenderentscheid E20‑Q6 vom 25.09.2026 um — b, während der Welle erweitert: „bei Wärmepumpe fixer Jahresbetrag (oder %
von Investitionskosten), nicht nach kWh/a — weder Strom noch Wärme“; sie ist keine Etappe des Plans E0–E12 und kommt
ohne Schemaschritt aus, `SchemaStand.Zielversion` bleibt 142. Die Fragen E23‑Q1…Q5 hat der Orchestrator am 25.09.2026
(13:15) mit der Baufreigabe nach Empfehlung entschieden — alle a; E23‑Q6 und E23‑Q7 sind offen beim Anwender.

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **E23** (#510, Merge `f7823b8e` über `c48de9e0`, Zweig `e23` = `d08446a0` von `7b92780d`, darin `7799772b` = Merge `fe32922c`) | `EUR_PRO_KWH_ELEKTRISCH` und `EUR_PRO_KWH_THERMISCH` an der Wärmepumpe GEWERK in der Landkarte `BasisGrund` (E23‑Q1 a), Auswahl, KI-Wahlliste und Kreuztafel folgen; eine Bestandszeile bleibt über `benutzt` wählbar und rechenfähig, ohne Lauf mit dem Grund „passt nicht zu diesem Gewerk“ (E23‑Q2 a), kein Umstellungsangebot (E23‑Q5 a); der Vermerk „Altbestand“ an der Herleitung im Kern, nur im Projektmodus (E23‑Q3 a, Q4 a), ein neuer Schlüssel; 8 Testfälle | **nein** im Bestand — keine Zeile an der Wärmepumpe trägt eine kWh-Art; Anker unberührt, Referenzlauf 14/14 gegen R16 byte-gleich |

*§ 6.3 Nr. 10 (vor #510):*

> 10. Bezugsgrößen der übrigen KD1-Bemessungsarten (H1-1b) — **Anwenderregel umgesetzt mit E20 (#502), offen allein
>     E20‑Q6** (Anwender 25.09.2026, → Register R‑Rest, R‑E20): die Investitionskosten der Wärmepumpe je kW thermisch
>     und je kW elektrisch — P_el am Normpunkt der Kennlinie (§ 3.2, Fußnote ¹) —, die Betriebskosten thermisch; Rest:
>     Das Betriebsraster der Wärmepumpe bietet weiter „je kWh elektrisch“ (Strommenge aus dem Lauf) — lassen
>     (a, Empfehlung, gebaut) oder entfernen (b), Anwenderentscheid; siehe Protokoll

**Erledigt mit E23 (#510):** Die Investitionskosten der Wärmepumpe bemessen sich je kW thermisch und je kW elektrisch
(E20), die Betriebskosten nicht je kWh — fester Jahresbetrag, Prozentbemessungen, je kW (E23); „je kWh elektrisch“ und
„je kWh thermisch“ sind im Betriebsraster der Wärmepumpe gesperrt, eine Bestandszeile rechnet weiter und trägt den
Vermerk „Altbestand“. Offen beim Anwender bleiben E23‑Q6 und E23‑Q7 (die Prozent- und Leistungsbemessungen an der
Wärmepumpe lassen, gebaut a, oder sperren). **Entscheide 25.09.2026:** E23‑Q6 und E23‑Q7 nach Empfehlung a (lassen),
siehe Register R‑E23.

### 8.47 Berichtigungen im gültigen Stand (#510)

Die Stellen, die mit E23 veraltet sind; „vorher“ ist der Wortlaut vor #510 (Stand `c48de9e0`). Je Stelle eine Zeile:

| Stelle im Konzept | vorher | nachher |
|---|---|---|
| Kopf (Z. 3) | Codestand `76f8661d`; „… E19, E20, E21 und E22 ohne Schritt“ | Codestand `f7823b8e`; „… E19, E20, E21, E22 und E23 ohne Schritt“ |
| Schrittabsatz | „…, E20 (#502), E21 (#506) und E22 (#503) kommen ohne Schritt aus“ | „…, E21 (#506), E22 (#503) und E23 (#510) kommen ohne Schritt aus“ |
| § 3.2 Tafel der Runde 1 | ohne Zeilen für die kWh-Arten an der Wärmepumpe | zwei Zeilen `EUR_PRO_KWH_ELEKTRISCH` und `EUR_PRO_KWH_THERMISCH` an der Wärmepumpe, Kategorie 2: „gesperrt (GEWERK), Bestandszeile rechnet aus dem Lauf, Herleitung ‚Altbestand‘“; der Satz „je Raster“ um die kWh-Arten ergänzt; Fußnote ² |
| § 6.1 | — | neue Zeile E23 |
| § 6.3 Nr. 10 | Wortlaut in § 8.46 | durchgestrichen, „erledigt mit E20 (#502) und E23 (#510)“, siehe Protokoll |
| § 7 | — | Satz zur kleinen Welle E23 (#510) |
| Anhang | — | Kürzel- und Etappenzeile E23 |

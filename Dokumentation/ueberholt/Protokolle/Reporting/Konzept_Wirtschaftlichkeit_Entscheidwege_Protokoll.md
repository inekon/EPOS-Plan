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
> dort mit dem Wortlaut des Konzepts **vor der jeweiligen Statuszeile** (#436, #437), jede Berichtigung
> mit einer Zeile.

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
| 9h–9k | 9i und 9k erledigt; 9h zum Teil (#357, #434); 9j erledigt mit E6 (#436), der Grund in § 8.1 | § 3.7, § 8.1 |
| 21 | erledigt mit #333 und E1 (#380); offen allein die Betriebskosten von 1030 | § 3.6 |
| 30–32 | 31 erledigt mit E5 (#434); 32 erledigt und 30 zum Teil erledigt mit E7a (#437), der Wortlaut vor #437 und der Grund in § 8.3 | § 5.1, § 8.3 |
| R4 | erledigt mit E1 (#380) | § 5.2 |
| 25–29 | 25–28 erledigt mit E2 (#405); 29 erledigt mit E7a (#437), der Wortlaut vor #437 und der Grund in § 8.3 | § 5.4, § 8.3 |
| 20 | entfällt (22.09.2026) | § 6.4 |
| 12, 14, 17 | erledigt bzw. überholt | § 7.1 |

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
[Nutzungsdauer-Konzept](../../../aktuell/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md).

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
#437** (Stand `befec9dc`) —, nicht vor dem Schnitt.*

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

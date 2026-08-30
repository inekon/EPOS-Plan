# B4 — Energieintensität als eine Wahrheit (Umsetzungsprotokoll)

Etappe B4 des Konzepts `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` (§ 3 BW4,
Befunde B3/A7; § 8: „Katalog löst die Konstanten in `StromAufschlagModel` ab" —
Ergebniswirkung „keine (wertgleich)"). Stand 30.08.2026, Branch `Pufferspeicher`.
Kein Schemaschritt, keine Saat — `ZIEL_VERSION` bleibt 61.

## 1. Befundlage: die drei Orte

| # | Ort | nach B4 |
|---|---|---|
| 1 | `Tab_ProjektWirtschaftlichkeit.Unternehmensart` (führendes Feld) | koppelt jetzt an Ort 2: hebt den fachlich passenden Schnellwahlknopf hervor |
| 2 | `StromAufschlagModel.STROMSTEUER_REGELFALL/_REDUZIERT` | **Rückfallebene statt Quelle** (Kommentare fortgeschrieben) |
| 3 | `Tab_Gesetzesparameter.STROMST_REGELSATZ` | **Quelle des Regelfall-Knopfes** (Befund A7 erledigt) |

Ein **reduzierter Katalogsatz existiert nicht** (die sieben STROMSTEUER-Schlüssel
wurden am Produktivbestand gemessen; L4-Regel „nie eine Differenz raten" steht am
Block). Deshalb: Regelfall-Knopf katalogbasiert; Reduziert-Knopf behält die
Konstante 0,05 als dokumentierte Rückfallebene — mit vorbereitetem **Lesepfad**
`DbWerte.GESETZ_STROMST_REDUZIERT = "STROMST_REDUZIERT_SATZ"` (reine
Schlüsselkonstante): Sobald der Anwender den Satz über „Gesetzliche Parameter"
nachpflegt, gewinnt der Katalog (Nachpflegeweg im Harness real bewiesen).

## 2. Umsetzung (`ucStromAufschlaege`)

- Schnellwahl liest den Katalog (`WertMitHerkunft`, Bilanzjahr-Logik wie der
  B2-Brennstoffblock), Knopftext = Jahressatz in ct/kWh (EUR/MWh ÷ 10);
  Herkunft/Rückfall als Hinweis. Die alten Zahlen-Ressourcen
  `PREIS_STROMSTEUER_REGELFALL/_REDUZIERT` (Zahlenliterale in der Anzeigeschicht =
  dritte Stelle derselben Wahrheit) werden nicht mehr gelesen, bleiben aber stehen.
- **Unternehmensart-Vorschlag ohne Zwang** (BF4): produzierendes Gewerbe /
  Land- und Forstwirtschaft heben den Reduziert-Knopf hervor, sonst den
  Regelfall-Knopf; eingetragen wird ein Satz erst per Klick. Warnungen bei
  Inkonsistenz liefert weiterhin die B2-Kohärenzprüfung (Fälle 3/4).
- **Der Lese-Rückfall der Bestandsdaten bleibt unverändert** (NULL → 2,05 aktiv,
  `StromAufschlagCtrl.Komponente`) — B4 ändert nur die Knopfquelle; sonst wäre die
  Etappe nicht wertgleich.

## 3. Nachweise (Harness `..\dev\b4\`, zweimal gegen frische Kopie reproduziert)

| Probe | Ergebnis |
|---|---|
| **Gleichstand** (Konzept-Nachweis) | Katalog 20,50 EUR/MWh ÷ 10 = 2,0500 == `STROMSTEUER_REGELFALL` — Delta 0,000000000, für 2026 und 2027 |
| Knopfquelle | 2026/2027 Regelfall aus Katalog (True); 2025 (vor JahrVon) → Rückfall; Katalogzeile gelöscht → Rückfall bewiesen; Nachpflege 0,60 EUR/MWh → Knopf 0,06 aus Katalog |
| Hervorhebung | KEIN_PROD_GEWERBE/leer → Regelfall; PROD_GEWERBE/LAND_FORST → reduziert; Produktivbestand: alle 5 Zeilen KEIN_PROD_GEWERBE (stützt Neutralität) |
| Ergebnisneutralität | `LiesBetriebskosten(1024)` = 99,00; KW 1024 = −2.220.322,32; Leseweg roh NULL → 2,050 aktiv — alles unverändert |
| Build/Sweep | Exit 0 (nur Altwarnungen); kein `<<<<<<<`-Treffer; Knopfmaße geprüft (62 px, Text 28 px) |

## 4. Grenzen

1. Der reduzierte Satz bleibt bis zur Katalog-Nachpflege Konstante (Weg bewiesen,
   Auslieferung ungesät — bewusst, L4).
2. Bilanzjahr/Unternehmensart werden beim Blockaufbau gelesen — Änderungen im
   Parameterdialog wirken beim nächsten Öffnen des Trägerdialogs.
3. Ort 1 erzwingt nichts am Preis (Vorschlag statt Automatik, BF4); der umgekehrte
   Weg (Parameterdialog zeigt den erfassten Preisanteil) existiert weiter nicht —
   Kandidat für B5/B6.
4. Sichtabnahme der Hervorhebung am lebenden Dialog steht aus (headless belegt).

## 5. Geänderte Dateien

```
Allgemein/DbWerte.cs                    GESETZ_STROMST_REDUZIERT (Lesepfad, keine Saat)
Model/StromAufschlagModel.cs            Konstanten als Rückfallebene dokumentiert
Views/Kosten/ucStromAufschlaege.cs      Katalog-Schnellwahl, Jahr/Unternehmensart, Hervorhebung
Allgemein/Reporting/B4_Energieintensitaet_Protokoll.md   dieses Protokoll
```

10 neue `PREIS_ST_*`-Textschlüssel (GetString-Rückfall) — resx-Sammelnachtrag folgt
gebündelt mit den F2-Schlüsseln. Harness (gitignored): `..\dev\b4\`.

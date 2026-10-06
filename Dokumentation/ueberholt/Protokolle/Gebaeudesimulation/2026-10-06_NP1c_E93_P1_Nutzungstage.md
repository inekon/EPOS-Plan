# Protokoll NP1c — E93: P1 im Profilweg, Nutzungstage abgeleitet (06.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle NP1c der Stufe NP, Entscheid E93 (Anwender 06.10.2026). Zweig `np1c-e93` auf `1a268145`: `a8120c20` NP1c-1, `4f541f27` NP1c-2, `b025c515` NP1c-3; Merge `ba5b5732` (gemeinsam mit NP2b). Die Statuszeile ist #756.

## 1 Ergebnis

1. **P1 im Profilweg:** `Konditionierungsarbeit.VorlageUebernehmen` mindert den Gerätewert nur, wenn die Vorlage keine eigene Personen-Nennwert-Zelle bringt. **Rote Probe:** Profil mit Geräte- und Personenkennwert ergab 684,7 W statt 960 W. Bisherige Konditionierung und EPOS-Muster ohne Nennwert unverändert (der Bestandswert enthält die Personen, B5).
2. **Nutzungstage abgeleitet:** `Raumnutzungsgenerator.Nutzungstage(profil, ferien, referenzjahr)` (Wochenmuster − Feiertage an Nutzungstagen − Ferien des Ziels). Das Blatt zeigt eine Lesezeile statt einer Eingabe, Kurzform „· 252 d“ (Büro Mo–Fr, Bezugsjahr 2025), die Übernahmevorschau „… abzüglich n Ferientage = m“. Die Spalte `Nutzungstage_Jahr` bleibt (null, kein Schemaschritt); KI-Feld `np_tage_jahr` lesend; Texte `RNP_TXT_NUTZUNGSTAGE(_FERIEN)`, `RNP_TXT_TAGE` entfallen; Eingabebilanz des Blatts 31 → 30.
3. **Feiertage** Lager/Verkehr wie Büro und Schule (keine Codeänderung).
4. **Papiere** (E93 im Register, P1-Satz, NP-F8/F18, 4.1, 6.1, 6.4, Q40, Q44) in `b025c515`.

## 2 Nachweise

Agentenabnahme: Kern-Filter und Schale 0 Fehler, UI 7585, Kern-Auswahl 970, SQL 0, Referenzlauf 21/21 PASS, CSV 646 byte-gleich. Gate `gate_haupt.sh NP2b` auf `ba5b5732` (NP2b und NP1c auf #752/#753): Kern-Filter rc=0; ChartProben 211 Hashes gleich; KiKern 549/549, SpeicherEngine 397/397, SpeicherPlanung 27/28 (1 übersprungen), EPOS.UI.Tests 7608/7608, EPOS.Kern.Tests 11274/11277 (3 übersprungen); Dokumentationswachen 35/35; Referenzlauf 21/21 gegen R38: GESAMT PASS (6 872 111 Werte), CSV byte-gleich 646/646; gestörter Lauf PASS; Windows-Schale rc=0; Designer 15 328 Einträge unverändert; SqlDialektPruefer 2 434 Texte, 0 Fundstellen; Werkzeugtests 124/49/24/39; Konfliktmarker keine; `.resx` nach Schlüsseln dreiwege vereinigt (`d58dba03`, `ba5b5732`, keine Doppelten). Merge mit origin (HC-4 #754) `39862a60` konfliktfrei geprüft.

## 3 Offenes

Sichtabnahme unter Windows (P1 bei Profilen mit Personenwert, Nutzungstage im Blatt); NP4 offen; kein Wiki-Upload vor NP4.

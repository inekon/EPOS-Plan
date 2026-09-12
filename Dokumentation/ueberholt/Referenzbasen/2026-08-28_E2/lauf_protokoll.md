# Basis 2026-08-28_E2 — Laufprotokoll

Eingefroren am 28.08.2026 nach Paket **E2** (Kanal-Ganglinien) und dem **produktiven
Scharfschalten der Booster-Kette** in Projekt 1042. **Dreizehn Projekte, 332 CSV** — drei
Dateien mehr als die P1-Basis, alle in 1042 (u. a. die neue Booster-Quelltemperatur-Ganglinie
`wp_quellentemperatur.csv`).

**Anlass (Daten + Code):**
1. **Booster produktiv** (Q1-O1/B1-O8/E2-O1 geschlossen): Der Anwender hat die Kette in 1042
   konfiguriert — CS6800iAW und Heizkessel laden „Puffer 3000Ltr", die Sole-Wasser-WP
   CS7800iLW 16 bezieht ihre Quellwärme aus diesem **geteilten** Puffer und lädt den
   Stora B 1000-6. Das Laufprotokoll trägt den Booster-Hinweis („Quelltemperatur folgt dem
   Speicherzustand"), `QUELLE_FEHLT` ist verschwunden; die frühere Basis kannte diesen Pfad
   nicht (Quelle unkonfiguriert).
2. **Codestand `babab27`** (Pakete E2 Kanal-Ganglinien + D-Check-Layoutfixes gegenüber P1;
   beide waren gegen die P1-Basis per A/B byte-gleich belegt — die CSV-Änderungen dieser
   Basis sind ausschließlich die 1042-Datenänderung).

**Codestand:** `babab27`, gebaut aus einem `git archive`-Export außerhalb des Repos
(`C:\Waermeplan\_e2b`; 0 Fehler) — der Arbeitsbaum trug parallel laufende Agentenarbeit.
**Datenquelle:** produktive `Kenndaten.accdb`, Zeitstempel **28.08.2026 09:05:30**, nur
gelesen (App des Anwenders lief; keine dauerhafte `laccdb`); **eine** feste Quellkopie
(`Referenzlauf.exe migration`, Schemastand 54, Exit 1 = bekannter Altschaden-Nachweis
„PufferHeizung ohne WS_ID_Puffer: 2"), beide Läufe im Modus `projekt` auf derselben Kopie.
**Selbstvergleich:** zweiter Lauf **332/332 CSV byte-/MD5-gleich** — reproduzierbar.
2 × 13 Projekte, 0 Fehlläufe.

**Booster-Kennzahlen 1042 (erstmals in einer Basis):** WP gesamt 55,76 MWh Produktion /
20,7 MWh Strom; `Kapazitaet_Pufferspeicher` 80,79 kWh (Summe der Senkenspeicher, S-1);
keine Kennlinien-Kappung unten (Quelltemperatur des geteilten Puffers blieb über der
untersten Stützstelle).

**Was diese Basis erstmals absichert:** die stundengekoppelte Booster-Rechnung (B1) mit
echter Konfiguration samt Quelltemperatur-Ganglinie; die E2-Kanalganglinien-Buchung läuft
mit (exportwirksam nur app-seitig). **Weiterhin offen:** Kessel-Quellkopplung mit Wert ≠ 0
(Tab_Heizkessel-Temperaturpaare ungepflegt, B1-O10), Wirtschaftlichkeit, Solar-Nenner-Fall
(E1-O2).

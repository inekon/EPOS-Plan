# Protokoll NP2b — Zonenbaum gruppiert, Herleitung, Einzonenhinweis, Reste aus NP3c (06.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle NP2b der Stufe NP. Zweig `np2b-zonenbaum` auf `1a268145` mit Merge `c8da73fb` (HC-2-Stand `07b2bd3d`): `136b8702` NP2b-1, `c1e544e4` NP2b-2, `776e3960` NP2b-3, `7c20c601` NP2b-4, `65a45578` NP2b-5b, `a186c823` und `d52f25cd` NP2b-5c, `4303b871` NP2b-5a; Merge `d58dba03`; gegatet gemeinsam mit NP1c auf `ba5b5732`. Die Statuszeile ist #755.

## 1 Ergebnis

1. **Gruppierte Auswahl** je Zone im Zonenbaum des Imports: „keine“, dann alte Kennungen und Texte mit „(nicht im Katalog)“, dann je Kategorie eine `<optgroup>` (`Raumnutzungsvorbelegung.Auswahl`, DTO `GebaeudeNutzungsgruppe`, Texte `RNP_IMP_*`).
2. **Herleitungszeile** je Zone: `Profilquelle` der Planzone (IFC-Klasse oder Raumtyp unter Z6, DIN-Nummer der Projektdatei, Datei, früherer Import, von Hand); Hülle `GebaeudeProfilherleitung` mit Profil, Kategorie, Quelle, Größen aus der Datei (NP-F16) und Kurzform der Kennwerte; Baustein `Herleitungszeile`.
3. **Neu lesen nach dem Blatt:** Kern und Hülle bauen den Plan je Anfrage neu (Probe als Wache). **Befund mit roter Probe:** die Nutzung in „Zone hinzufügen“ war als Listenstelle gespeichert; nach einer Katalogänderung legte OK die Zone mit WOHNEN statt BUERO an. Der Dialog hält jetzt den Schlüssel.
4. **Einzonenweg (Anwenderentscheid 06.10.2026):** `SqprojZonen.Gebaeudeprofilnummer`, `GebaeudeProjektdateiDaten.Einzonenvorschlag`; Hinweiszeile nur im Einzonenweg, kein Auswahlfeld, gesetzt wird nichts.
5. **Reste aus NP3c:** (a) der Gebäudedialog reicht `RaumnutzungKiZugang` über `GebaeudeImportDialog.RaumnutzungKi` an das Blatt, `GebaeudeKiSicht` löst `np_*` auf; (b) **rote Probe:** ein gesperrtes Gebäude lehnte `feld_setzen` auch für `np_*` ab, `KiAktionenDialog.Schreibschutz` nimmt sie aus; (c) **rote Probe:** ein Kalender aus dem Profil „Büro“ galt als Vorlage „Büro“, `Herkunft()` nennt nur echte Vorlagen, `HerkunftText` „Nutzungsprofil …“ (Texte `KOND_LBL_HERKUNFT_PROFIL`, `KOND_TXT_ZUSTAND_PROFIL`, wegen der Bündelwache als `KOND_*`).

## 2 Nachweise

Agentenabnahme: Kern-Filter und Schale 0 Fehler, EPOS.UI.Tests 7600/7600, Kern-Auswahl 1527 (2 übersprungen), Texte/Konditionierung/Ki 890/890, Designer gleich, SQL 0 Fundstellen. Gate `gate_haupt.sh NP2b` auf `ba5b5732` (NP2b und NP1c auf #752/#753): Kern-Filter rc=0; ChartProben 211 Hashes gleich; KiKern 549/549, SpeicherEngine 397/397, SpeicherPlanung 27/28 (1 übersprungen), EPOS.UI.Tests 7608/7608, EPOS.Kern.Tests 11274/11277 (3 übersprungen); Dokumentationswachen 35/35; Referenzlauf 21/21 gegen R38: GESAMT PASS (6 872 111 Werte), CSV byte-gleich 646/646; gestörter Lauf PASS; Windows-Schale rc=0; Designer 15 328 Einträge unverändert; SqlDialektPruefer 2 434 Texte, 0 Fundstellen; Werkzeugtests 124/49/24/39; Konfliktmarker keine; `.resx` nach Schlüsseln dreiwege vereinigt (`d58dba03`, `ba5b5732`, keine Doppelten). Merge mit origin (HC-4 #754) `39862a60` konfliktfrei geprüft.

## 3 Offenes

- `KalenderkarteInhaltTests.Das_Teppichbild_rechnet_…_entprellt_einmal` lief in einem Teillauf einmal rot (2 statt 1), einzeln und im Gesamtlauf grün (Zeitsteuerung, nicht diese Welle); wird eine von Hand gewählte Nutzung im Blatt gelöscht, zeigt die Absage den Rohschlüssel „#Id“.
- Sichtabnahme unter Windows (Zonenbaum gruppiert, Herleitung, Einzonenhinweis); NP4 offen (CSV-Import, Profile aus der Projektdatei, Bearbeitung von Zeilenbild und Stundenprofil, Wiki, Logbuch); kein Wiki-Upload vor NP4.

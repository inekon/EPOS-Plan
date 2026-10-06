# VB — VALERI-Darstellung des Wirtschaftlichkeitsberichts (Protokoll)

Etappen VB‑E1 bis VB‑E5 des Konzepts
[`Konzept_Berichtsseite_VALERI_Anordnung_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsseite_VALERI_Anordnung_EPOS-Plan.md)
(Teil A, Fachvorgabe VB, Entscheide VB‑Q1–Q9 nach Empfehlung, Register
[R‑E32](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)).
Statusnummer **#765**. Der gültige Stand steht im Code, hier steht, wie es geworden ist. Siehe
[Statusdatei](../../../aktuell/Status_iOS_Migration.md).

Sitzung „Berichterstellung“, 06.10.2026, Fable 5.1 orchestrierte, drei Opus-5.5-Agenten nacheinander im Worktree
`bs-valeri`. Kein Schemaschritt, kein Rechenweg, keine Referenzbasis berührt; `EPOS.iOS/` nicht berührt.

| Commit | Inhalt |
|---|---|
| `ce3caaf56` | VB‑E1: Szenariodarstellung VALERI in Konfiguration und Wertesatz |
| `ab85e6d8e` | VB‑E2: Tafel „Kennzahlen je Szenario“ je Stand im Wortbericht |
| `ec93ae112` | VB‑E3: Kopfzeile „Wortbericht in VALERI-Darstellung“ im Excel-Blatt |
| `3b7b4bb99` | VB‑E4: Platzhalter `stand.tabelle.wirtschaft_szenarien`, Katalog v15 |
| `9156f5505` | VB‑E5: Klappliste „Alle drei Szenarien (VALERI)“, Vorbelegung, KI |
| `78c95c5ad` | VB‑E5: eigene Vorbelegungszeile für die VALERI-Darstellung |
| `aee904683` | Merge origin (#760–#762) in VALERI-Zweig |


## 1 Anlass und Entscheide

Anwenderwunsch 06.10.2026: „Die Berichte sollen auch mit VALERI-Darstellung (best, worst, expected) erstellt
werden können (Auswahl)." Der Anwender hat am selben Tag „Alle Empfehlungen, Fachvorgabe hier schreiben" verfügt
— die neun Fragen VB‑Q1 bis VB‑Q9 des Konzepts (#761) sind damit alle nach Empfehlung entschieden, die
Fachvorgabe VB steht in Teil A, Abschnitt 6 des Konzepts, der Registereintrag R‑E32 im Entscheidungsregister
Wirtschaftlichkeit.


## 2 Befund Ist-Stand

Der Wortbericht und die Excel-Mappe lasen vor dieser Welle immer genau ein Szenario (`Berichtsszenario`, aus der
Projekteinstellung oder der Berichtskonfiguration). Eine VALERI-Bewertung — drei Szenarien Ungünstig, Erwartet,
Günstig — ließ sich nicht nebeneinander in einem Bericht zeigen; der Anwender musste den Bericht dreimal mit je
einem Szenario erstellen. Konzept Teil A, Abschnitt 1 hält den Befund im Einzelnen (Entwurf der Konfiguration,
der Kapitelstruktur und der Bedienung).


## 3 Umsetzung je Etappe

### VB‑E1 — Konfiguration und Wertesatz (`ce3caaf56`)

`EPOS.Kern/Allgemein/Bericht/BerichtsKonfiguration.cs` erhält das Feld `Szenariodarstellung`
(`DARSTELLUNG_EINZELN`/`DARSTELLUNG_VALERI`), die Eigenschaft `IstValeri` und die Normierung
`NormiereDarstellung`: Das JSON liest duldsam (ein fehlendes oder unbekanntes Feld gilt als `EINZELN`), geschrieben
wird das Feld nur bei VALERI, die Einzelwahl bleibt byte-gleich zum bisherigen Stand. Der Setter `Szenario` ist
unberührt — er bleibt die Quelle für das Leitszenario im Einzelweg.

`EPOS.Kern/Allgemein/Bericht/WirtschaftsBerichtswerte.cs` bekommt `IstValeri(konfig)`, `ValeriSpalten`,
`ValeriSzenarien(idProjekt, out fehlend)` und eine Änderung an `Berichtsszenario`: Bei VALERI liefert sie immer
Erwartet (das Leitszenario für alle Stellen, die nur ein Szenario kennen — etwa die Kapitelüberschrift).

Tests: `BerichtsKonfigurationJsonTests.Die_Szenariodarstellung_liest_sich_duldsam_und_uebersteht_den_Rundlauf`,
`BerichtWertesatzTests.In_VALERI_Darstellung_ist_das_Leitszenario_Erwartet`.


### VB‑E2 — Tafel „Kennzahlen je Szenario“ (`ab85e6d8e`)

`Berichtstabellen.WirtschaftskennzahlenSzenarien(daten, werte, stand, englisch, kultur)` baut eine Tafel mit den
Spalten Kennzahl | Ungünstig | Erwartet | Günstig; fehlt ein Szenario (zum Beispiel, weil es nie gerechnet wurde),
fällt die Tafel auf die Spalte Erwartet allein zurück und trägt einen Hinweis in `tafel.Hinweise[0]`
(`WIRT_BER_VALERI_RUECKFALL`). `Kennzahlstaende(daten)` liefert die Stände, über die iteriert wird.

Der Baustein `SchreibeVergleichValeri` in `EPOS.Kern/Allgemein/Bericht/BausteineWirtschaftlichkeit.cs` schreibt
eine Heading2 „Kennzahlen je Szenario“ (`WIRT_BER_VALERI_MAPPE`) mit je einer Heading3 und einer Tafel pro Stand;
ein Jahr‑1-Hinweis (`WIRT_BER_VALERI_WORTBERICHT`) steht vor den Tafeln, die Paarsicht-Deklarationszeile nur
einmal insgesamt. `AnhangECheckliste.AusBericht(…, szenario, bool valeri)` erhält die Lage `ChecklistenLage.Valeri`
— die Punkte 1, 7 und 9 nennen dann die neue Tafel statt des einzelnen Szenarios, Punkt 9 (Stand) bleibt
unverändert.

Ressourcen: `WIRT_BER_KENNZAHLEN_VALERI`, `WIRT_BER_VALERI_MAPPE`, `WIRT_BER_VALERI_RUECKFALL`,
`WIRT_BER_VALERI_WORTBERICHT`, `WIRT_AE_1/7/9_STELLE_WORT_VALERI` (de/en). Neue Messlatte
`EPOS.Kern.Tests/Messlatten/Bericht_Word_1030_Valeri.txt` (97 Zeilen). Proben in `BerichtSzenarioTests`: Kennzahlen
je Stand in drei Szenarien für Projekt 1030 und eine Gruppe, Rückfall ohne Ungünstig, Paarsicht je Stand A und B.

Abweichungen vom Entwurf: Die Spaltenköpfe kommen aus `VerlaufZeilen.Szenarioname` statt aus eigenen
`WIRT_SZ_SP_*`-Ressourcen (Wiederverwendung bestehender Texte); eine einzige Heading2 mit je einer Heading3 pro
Stand statt einer Heading2 je Stand; die Rückfallzeile trägt die Ressource `WIRT_BER_VALERI_RUECKFALL`.


### VB‑E3 — Excel-Kopfzeile (`ec93ae112`)

`ExcelBerichtGenerator` schreibt bei VALERI in Zeile 2 des Blatts Wirtschaftlichkeit fett den Text „Wortbericht in
VALERI-Darstellung“; die Fixierung des Blatts rutscht eine Zeile tiefer. Probe
`Die_Mappe_nennt_die_VALERI_Darstellung_in_einer_Kopfzeile` prüft Kopfzeile und unverändertes übriges Blatt.


### VB‑E4 — Platzhalter und Katalogfassung 15 (`3b7b4bb99`)

Neuer Katalogeintrag `stand.tabelle.wirtschaft_szenarien` in `Vorlagenfeldkatalog.Tabellen.cs` (Tabelle je Stand,
Word und Excel, „Seit 15“, Feldgröße `FASSUNG_WIRTSCHAFT_SZENARIEN`) und der Schalter
`hat.tabelle.wirtschaft_szenarien`; Ressource `VF_STAND__TABELLE__WIRTSCHAFT_SZENARIEN`.
`Berichtsbedarf.Wirtschaftsbereiche` führt den neuen Schlüssel. `KATALOGFASSUNG` steigt von 14 auf 15,
`KatalogfassungWord` liegt bei 15 (Maximum der Fassungen mit Word-Ausgabe). Neue Messlatte
`Vorlagenfeldkatalog_v15.txt`. Die Anhang-E-Tafel im Vorlagenweg und die Checkliste im Excel-Bericht sind auf die
Überladung mit `IstValeri(konfig)` umgestellt.

Alle zehn mitgelieferten Vorlagen wurden mit `Werkzeuge/Berichtsvorlage … alle` neu erzeugt; ein zweiter Lauf ist
byte-gleich, der `OpenXmlValidator` meldet 0 Fehler. Nur die Bausteinvorlagen `.dotx` führen den neuen Schlüssel.
Wachen `VorlagenfeldkatalogWacheTests`, `VorlagenfeldKesseltafelTests` nachgezogen; Test
`BerichtSzenarioTests.Der_Platzhalter_der_Kennzahlen_je_Szenario_gleicht_der_Tafel_des_Kapitels`.

**Befund:** Die ausführliche Excel-Vorlage nimmt den neuen Schlüssel nicht automatisch auf, weil
`ExcelAusfuehrlich.cs` ihre Elemente einzeln setzt statt über den Katalog zu laufen. Das ist als Entscheid offen
(Abschnitt 6).


### VB‑E5 — Bedienung: Klappliste, Vorbelegung, KI (`9156f5505`, Nachzug `78c95c5ad`)

`BerichtSeiteGaben.cs` erhält einen vierten Eintrag `BerichtStand.SZENARIO_VALERI = 3` (Konstante in
`BerichtDaten.cs`), Text `BK_BER_SZENARIO_VALERI`. `SzenarioNummer(konfig)` liest über `IstValeri` zurück,
`AusAuftrag` setzt `DARSTELLUNG_EINZELN` bzw. `DARSTELLUNG_VALERI`.

Vorbelegung: `WirtschaftlichkeitSeite.razor` (`ZumBerichtWechseln`) gibt bei Darstellung „ValERI-Bewertung“ die
Nummer 3 mit `Texte.ZumBerichtValeri` weiter. Der Nachzug `78c95c5ad` ersetzt dies durch einen eigenen
Vorbelegungssatz `BK_BER_VORBELEGT_VALERI` in `BerichtSeite.razor` (`VorbelegungUebernehmen`) — das Szenario bleibt
dabei stehen, nur die Darstellung wechselt. Das KI-Wahlfeld `szenario` erhält den vierten Wert automatisch, die
Erläuterung `KI_DLG_BKB_SZENARIO_ERL` ist ergänzt.

Tests: `BerichtsvorlagenHuelleTests.Der_vierte_Eintrag_setzt_die_VALERI_Darstellung_und_laesst_das_Szenario_stehen`,
`BerichtSeiteTests.Der_vierte_Eintrag_geht_in_den_Auftrag_und_wird_zurueckgelesen`,
`BerichteKostenSeiteTests.Zum_Bericht_aus_der_ValERI_Darstellung_belegt_alle_drei_Szenarien_vor` samt
Vorbelegungssatz-Proben.


## 4 Merge

Merge `aee904683` holt `origin/ios_migration_september` (Stand `ae3a48990`, #760–#762) ohne Konflikt in den
VALERI-Zweig. Ressourcen sind wohlgeformt (je 15 371 Einträge), der Resource-Designer ist unverändert und
wiederholbar.


## 5 Abnahme

Gate auf `78c95c5ad`: Kern-Filter Release 0 Fehler, Windows-Schale Debug x64 0 Fehler, `EPOS.UI.Tests` 7 633/7 633
grün, `EPOS.Kern.Tests` vollständig 11 313 grün, 3 übersprungen. Sieben Messlatten `Bericht_*` byte-gleich. Kein
Referenzlauf, kein ChartProben (kein Rechenweg, keine Bilder geändert), kein SQL-Dialekt-Fund, kein Schemaschritt.

**10 fremd rote Kern-Tests** (nicht aus dieser Welle, vor dem Merge schon rot):

| Test | Verursacher (vermutlich) |
|---|---|
| `GebaeudeImportHuelleTests.Ohne_Projekt_fragt_die_Huelle_keine_Datenbank` | NP2b‑4, `GebaeudeImportHuelle.Einzonenvorschlag()` |
| `GebaeudeImportBaustoffeHuelleTests.Ohne_Projekt_bildet_die_Huelle_den_Abschnitt_ohne_Datenbank` | NP2b‑4, dieselbe Ursache |
| `GebaeudeEinzonennetzTests.Die_Einzonenreihen_des_Bauteilwegs_bleiben_bitgleich` (7 Fälle) | RP2a `94e5d4d94`, Windows-Prüfsummen noch nicht nachgezogen |
| `ZonenuebergabeRechenwegTests.Ideal_an_einer_Zone_rechnet_ohne_Heizkreis` | AK1 `119de575d`, Abweichung 9. Nachkommastelle |

Diese zehn Tests sind nicht der VALERI-Welle zuzuschreiben und bedürfen keiner Korrektur in diesem Zweig.


## 6 Sichtprobe

Keine — der Bericht ist nur über die Messlatten (Word byte-gleich) und die Proben in `BerichtSzenarioTests`
belegt. Eine Sichtabnahme unter Windows (WebView2: Word-Bericht 1030 in VALERI-Darstellung, Excel-Kopfzeile,
Klappliste, Vorbelegung, KI-Feld) steht noch aus (Abschnitt 7).


## 7 Wiki und Logbuch

Die Wiki-Quellen „Wirtschaftlichkeit" (Abschnitt `bericht-szenario`) und „Berichtsvorlagen" entstehen parallel in
einem anderen Worktree (VB‑E6, Etappenplan des Konzepts). Ein Logbuchsatz ist vorgesehen (Version 1.2.0.7, beim
Anwender zu bestätigen). Upload ausstehend, folgt gebündelt mit der nächsten Sammelwelle (Konzept Hilfesystem
13.3).


## 8 Offen

- Sichtabnahme unter Windows (WebView2): Word-Bericht 1030 in VALERI-Darstellung, Excel-Kopfzeile, Klappliste,
  Vorbelegung, KI-Feld.
- Entschieden 06.10.2026 nach Empfehlung: **nein**, die ausführliche Excel-Vorlage bleibt Nachbildung des Standardberichts (die Bausteinvorlagen führen den Schlüssel). Frage war: Soll die ausführliche Excel-Vorlage um `stand.tabelle.wirtschaft_szenarien` ergänzt werden (Befund
  Abschnitt 3, VB‑E4)?
- Wiki-Upload und Logbuch-Version (VB‑E6, Version beim Anwender erfragen).
- Gepusht `cf0ec4442` am 06.10.2026; Nachweis: Kern-Lauf 37503127843 auf ubuntu grün (Stand `cc9faeffc` enthält den Push).
- Die zehn fremd roten Kern-Tests an ihre Sitzungen melden (NP2b‑4, RP2a, AK1z).

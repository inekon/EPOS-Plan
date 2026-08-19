# Konzept: Aktualisierung der Kosten- und Energieträgerstruktur in EPOS-Plan

**Rev. 2** · 19.08.2026 · Verfasser: KI-Sitzung mit Philipp · **Entscheidungen E1–E7 am 19.08.2026 von Philipp beschlossen** (§ 11) — Konzept umsetzungsreif
**Grundlagen:**
- `Bestandsaufnahme_Kosten-Energie-Dialogstruktur.md` (19.08.2026)
- Verifikationslauf „tote Tabellen/Abfragen" (19.08.2026, Agentenlauf über Repo + `Referenzlaeufe\Arbeitskopie\Kenndaten.accdb`)
- Kosteneingabe-Extraktion der Altanwendung `BHKWPlan\BHKW-WP-PLAN.XLSM` + `TABELLEN.XLS` (19.08.2026, UserForm-Designerdaten + VBA + Blattzellen)
- `WindowsFormsApplication1\Allgemein\Reporting\Analyse_Altanwendung_BHKW-Plan.md`
- `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` (18.08.2026)

**Pfadbasis:** `WindowsFormsApplication1\`, sofern nicht anders angegeben.

---

## 1 Anlass, Vorgaben, Abgrenzung

Vorgaben Philipps vom 19.08.2026, wörtlich zusammengefasst:

1. **Alttabellen und überflüssige Tabellen entfernen.**
2. **kWh-Konsistenz:** Ein Energieträger muss immer die Einheit kWh **oder** eine Umrechnungsregel in kWh haben — das ist heute nicht konsistent. Jede Einheit soll editierbar sein (mindestens eine kWh-Einheit). Jede Umrechnungsregel hat einen eingebbaren, änderbaren Umrechnungsfaktor; sein Name ist änderbar (Standard „Umrechnungsfaktor", bei Erdgas „z-Faktor"). Das Volumen gasförmiger Energieträger heißt **Normkubikmeter (Nm³)**.
3. **Initialbefüllung:** Alle Energieträger sind initial mit allen relevanten Einträgen (Umrechnungsfaktoren etc.) befüllt.
4. **`Form_Kosten` ergänzen:** Neuer Reiter **„Kostenprofil"** neben „Energiekosten"; die Knöpfe „Spotmarktpreise importieren" und „Kostenprofil bearbeiten" wandern dorthin — mit besserem Design (**nicht** als Buttons). Die blaue Kopfzeile „Energieträger" über der linken Liste entfällt (Überschrift steckt schon im Reiter).
5. **Komponenten:** Für die Komponenten die in **BHKW-Plan** aufgeführten Kostengruppen übernehmen (BHKW, Heizkessel, …); dazu die Kosteneingabe der Altdateien `BHKW-WP-PLAN.XLSM`/`TABELLEN.XLS` auswerten.
6. **KWKG/Steuern:** `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` für neue Dialoge, Eingaben und Datenstrukturen berücksichtigen.
7. **Nicht die Darstellung von BHKW-Plan übernehmen** — ein besseres, für die Integration in die EPOS-Plan-Oberfläche geeignetes Konzept erstellen.

**Kern-Lesart aus Vorgabe 5 + 7:** BHKW-Plan ist **Inhaltsquelle** (Kostengruppen, Positionsnamen, Empfehlungsbereiche, Zuschusslogik), **nicht** Gestaltungsvorlage. Die flache Blattliste, die modalen Dialogketten, „oder"-Doppelfelder und Kopf-Sonderfelder der Altanwendung werden nicht nachgebaut; Eingabe und Anzeige folgen durchgängig den vorhandenen EPOS-Plan-Mustern (Kategorie-Reiter + Positionszeilen in `Form_Kosten`, Karten, `SpeichernLeiste`, Best/Worst-Szenarien, Planwert-Übernahme). Details § 7.5.

**Abgrenzung:** Das Rechenverfahren bleibt unverändert (Kapitalwertmethode nach DIN EN 17463, Stammprojekt = Unterlassensalternative, `Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs`). Keine Messdatenhaltung, kein neues Berichtsformat. Die Brutto-Logik der Altanwendung (MwSt-Faktor 1,19 an 40 Stellen) wird **nicht** übernommen — EPOS-Plan rechnet netto.

---

## 2 Leitentscheidungen

| Nr. | Entscheidung | Begründung |
|---|---|---|
| **L1** | `energy_*` bleibt die führende Preis- und Trägerwelt. Kategorie 3 („Energie") in `Tab_ProjektWerte` wird endgültig stillgelegt. | Entscheidung „keine Doppelpflege" vom 11.08. (`WirtschaftlichkeitCtrl.cs:21-23`); Verifikation 19.08.: Kategorie 3 hat **keinen Schreiber** (der Energie-Reiter besitzt keinen Hinzufügen-Knopf) und nur einen Anzeige-Leser (Summen-Label). |
| **L2** | **kWh-Pflicht:** Jeder aktive Energieträger erfüllt beim Speichern: `billing_unit = 'kWh'` **oder** es existiert eine aktive Umrechnungsregel `billing_unit → kWh` mit Faktor > 0. Ein zentraler Prüfer erzwingt das; Verstöße werden als Liste gemeldet, nicht stumm repariert. | Vorgabe 2; heutiger Zustand inkonsistent (`ucFuelSettings.cs:243`: „Strom/Fernwärme hat keine Umrechnung über Faktoren"). |
| **L3** | Umrechnungsregeln bleiben in `energy_conversion` (kein neuer Einheitenkatalog). Neue Spalte `faktor_name`; Einheiten bleiben editierbare Textcodes (`from_unit`/`to_unit`). `energy_unit`/`energy_group` werden gelöscht (HF1). | Kleinster Eingriff; beide Tabellen sind im Code tot (Verifikation 19.08.). Die kWh-Konsistenz sichert der Prüfer aus L2, nicht ein Katalog. |
| **L4** | Gasförmige Träger führen die Volumeneinheit **`Nm³`**, ihr Faktor heißt **„z-Faktor"**; alle anderen Standard „Umrechnungsfaktor". | Vorgabe 2. |
| **L5** | Initialbefüllung ist **ergebnisneutral**: Seeds per DML-Migrationsschritt (kein DDL-DEFAULT, Hausregel `SchemaKatalog.cs:463-473`), z-Faktor-Seed = 1,0, `user_edited = true` wird nie überschrieben. Referenzläufe müssen vor/nach der Migration identisch rechnen. | Initialdaten dürfen keine Bestandsergebnisse verschieben; echte Zustandszahlen pflegt der Anwender. |
| **L6** | Komponentenliste wird um die BHKW-Plan-Kostengruppen erweitert (**Wärmezentrale, Bauliche Anlagen, Stromeinspeisung**; Nahwärmenetz **nicht** — E2); die sieben Technik-Komponenten bleiben (Planwert-Anbindung `TechnikPlanwertCtrl`). Die Alt-**Positionen** werden als `Tab_Kostenfaktor`-Katalog mit den Original-Beschriftungen gesät. | Vorgabe 5; die Altanwendung kennt die Gruppen nur implizit (Instandhaltungs-Bemessung), EPOS-Plan macht sie explizit. |
| **L7** | **Zuschuss** wird eine eigene Positionsart (`Kostenart = 'zuschuss'`): mindert die Anfangsauszahlung I₀ einmalig, ohne Ersatzbeschaffung und ohne Restwert. Die BAFA-Mini-KWK-Staffel der Altanwendung wird **nicht** übernommen (Programm 31.12.2020 ausgelaufen); die KWKG-Pauschale ≤ 2 kWel wird Bestandteil von HF6. | Schließt die dokumentierte Lücke „keine Förderung abbildbar" sauber im Verfahren nach DIN EN 17463 (Einzahlung in t=0). |
| **L8** | Gesetzeswerte (Steuersätze, Sockelbeträge, KWKG-Staffeln, CO₂-Preispfad) leben ausschließlich in `Tab_Gesetzesparameter` — **in der Einheit des Gesetzes** (€/MWh, €/1.000 l, €/1.000 kg). Die Umrechnung in €/kWh übernimmt die HF2-Regelkette. | `Grundlagen_KWKG…` § 5: Die Altanwendung hatte durch €/MWh-Vereinheitlichung einen Faktor-10-Fehler bei Öl. Einheitenrichtige Ablage + explizite Umrechnung verhindert die Wiederholung. |
| **L9** | **Inhalte aus BHKW-Plan, Darstellung nach EPOS-Plan.** Keine flache Positionsliste, keine modalen Dialogketten, keine „oder"-Doppelfelder; stattdessen komponentenweise gruppierte Erfassung in `Form_Kosten`, Bemessungswahl statt Parallelfeldern, Empfehlungsbereiche als Hinweise, Zuschuss als ausgewiesene Zeile. | Vorgabe 7 (19.08.2026); § 7.5. |

---

## 3 HF1 — Alttabellen und überflüssige Tabellen entfernen

### 3.1 Verfahren (drei Stufen)

1. **Belegen** — erledigt (Verifikationslauf 19.08.; Belege unten). Rest-Risiko: gespeicherte Access-Abfragen sind nur über Objektnamen belegt, ihr SQL ist aus dem Repo nicht lesbar. Darum gilt:
2. **Access-Objektabhängigkeiten prüfen** (manuell, Checkliste Anhang B) — erst danach:
3. **Entfernen** — Tabellen per neuem `SchemaMigration`-Schritt (idempotent: Existenzprüfung, erst Constraints, dann `DROP TABLE`); gespeicherte Abfragen und Beziehungen als dokumentierte manuelle Access-Schritte; Code-Aufräumung im selben Commit.

Wichtige Randbefunde der Verifikation:
- **`UpdateDB.ini` existiert im aktiven Repo nicht mehr** (kein Code liest `.ini`/`.sql`). Migrationsweg ist allein `SchemaKatalog`/`SchemaMigration`; `migration.manuell.sql` ist ein Handskript. `Grundlagen_4` ist hier veraltet; die Bestandsaufnahme wurde korrigiert.
- `ProjektDuplizierenCtrl` arbeitet **schema-getrieben** (`GetOleDbSchemaTable`, `ProjektDuplizierenCtrl.cs:225-262`): Projekttabellen mit Spalte `ID_Projekt` werden automatisch mitkopiert. Nach einem Drop entfällt das ohne Codeänderung; nur die **Katalog-Ausschlussliste** (`:47-48`) ist zu pflegen.

### 3.2 Löschliste Tabellen

| Tabelle | Status (Beleg) | Entfernen berührt |
|---|---|---|
| `Tab_Brennstoff_Projekt` | Altweg; kein C#-Zugriff; nur `migration.manuell.sql:239, 488-490` | vorher Constraints `Tab_ProjektTab_Brennstoff_Projekt` und `Tab_Brennstoff_StammTab_Brennstoff_Projekt` droppen; die zwei Skriptabschnitte streichen |
| `energy_unit` | tot; einziger Treffer Ausschlussliste `ProjektDuplizierenCtrl.cs:48` | Listeneintrag entfernen; **vorher** die Access-Abfrage löschen, die `energy_unit` vierfach joint (Kandidat `Abfrage_Neues_Kosten_Model`, s. Anhang B) |
| `energy_group` | tot; 0 C#-Treffer (Gruppencode kommt aus `energy_carrier.group_code`) | dieselbe Access-Abfrage |
| `Tab_KostenKategorie` | tot; Kategorien sind C#-Konstanten (`Form_Kosten.cs:18-22`); einziger Treffer Ausschlussliste `:47` | Listeneintrag; Access-Beziehung zu `Tab_ProjektWerte` fällt mit dem Drop |
| `Tab_KWKG_Staffel` | Altweg, write-only; ausdrücklich durch `Tab_Gesetzesparameter` abgelöst (`WirtschaftlichkeitCtrl.cs:1633-1638`) | Konstante `:60`, DDL+Saat `:227-251`, Doku-Absätze anpassen |
| `Tab_BHKW_neu`, `Tab_BHKW_Einf` | tot; 0 C#-Treffer, nur im DB-Katalog | in Access verifizieren, dann droppen |
| *(nach Access-Prüfung)* `Tab_KostenKategorien`, `Tab_ErgebnisKomponente`, `Tab_ErgebnisMonat`, `Tab_Gebaeude1` | Namen im DB-Katalog mehrdeutig (evtl. Extraktionsartefakte); 0 C#-Treffer | nur nach Sichtprüfung in Access |

**Ausdrücklich AKTIV — nicht anfassen:** `Tab_DBTagV(+Daten)(+_STAMM)` (Tagesverteilung, `GebaeudeStammCtrl.cs:339-358`, `SimulationWaermebedarf.cs:432`), `Tab_Kostenprofil`, `Tab_Preisreihe(+Daten)`, `pricing_model`, `energy_conversion`, `Tab_KostenGruppenKatalog`, `Tab_ErgebnisStromMatrix`, `Tab_ErgebnisWirtSensitivitaet`, `Tab_ProjektTarif`, `Tab_Kraftwerkspark`. Die in `Grundlagen_4` genannten `DBGebaeude`/`DB-Heizung` **existieren nicht** (Namensirrtum).

### 3.3 Löschliste gespeicherte Access-Abfragen (manuell, nach Abhängigkeitsprüfung)

Tot (0 C#-Treffer, im DB-Katalog vorhanden): `Abfrage_KostenKomponenten` (abgelöst durch `Form_Kosten.LiesKomponentenSummen`, `:280-291`), `Abfrage_ProjektKostenEnergie`, `Abfrage_ProjektKostenKomponenten`, `Abfrage_Neues_Kosten_Model`, `Abfrage_Kosten_WP/_Heizkessel/_BHKW/_Photovoltaik/_Solarthermie/_Pufferspeicher/_Stromspeicher`, `Abfrage_Heizkessel_Kosten`, `Abfrage_Erzeuger_Vorlauftemperaturen`, `Abfrage_Erzeuger_Ruecklauftemperaturen` (im Code als tot dokumentiert, `ProjektPuffer.cs:112-118`), `Abfrage_Max_Vorlauf`, `Abfrage_Min_Vorlauf`, `Abfrage_MaxMin_Vorlauf`, `Abfrage_Kuehlung_MaxLast`, `Abfrage_KenndatenKuehlung_Max`, `Abfrage_SST`.
**Beschlossen (E4):** `Abfrage_ProjektKostenInvestBetrieb` wird gelöscht — kein C#-Aufruf; der Kommentar in `KostenPositionCtrl.GruppeSichern` (`:198-217`) wird angepasst, `GruppeSichern` selbst bleibt (Katalogpflege weiter sinnvoll).
**AKTIV bleiben:** `Abfrage_Energietraeger_Effektiv`, `Abfrage_Kostenfaktoren`, `Abfrage_Gebaeudearten/-typen`, `Abfrage_Projektgebaeude`, `Abfrage_ProjektGebaeudeGanglinie`, `Abfrage_ProjektStromGanglinie`, `Abfrage_Tagverteilung`, `Abfrage_Monatsstrom`, `Abfrage_Monatswaerme_Prozesse/_Brauchwasser`.

### 3.4 Code-Aufräumliste

| Objekt | Maßnahme |
|---|---|
| `Allgemein\Import\IniFileParser.cs` | löschen (verwaister Rest der UpdateDB.ini-Ära) |
| `Controller\ProjektDuplizierenCtrl_bak2` | löschen (nicht kompilierte Altfassung) |
| `WirtschaftlichkeitCtrl.cs:60, 227-251` | KWKG-Staffel-Konstante + DDL/Saat entfernen; Doku `:1633-1638` anpassen |
| `Form_Kosten.KATEGORIE_ENERGIE` (`:22`) | bleibt vorerst (wird von `SchemaMigration.cs:1366`, Schritt 19b, historisch referenziert — Migrationsschritte werden nie rückwirkend geändert); Kommentar „stillgelegt" ergänzen |
| `Form_Kosten.Gesamtkosten()`-Leser Kategorie 3 (`:280-291`, `:308`) | Summen-Label „PROJEKT GESAMT (Energiekosten)" künftig aus `KostenEmissionRechner` speisen (heute zeigt es die tote Kategorie-3-Summe = 0,00 €) → HF4 |
| `migration.manuell.sql:239, 488-494` | Abschnitte `Tab_Brennstoff_Projekt` streichen; Kategorie-3-Import (`:492-494`) mit Umbau-Hinweis versehen |
| Kategorie-3-Altzeilen in Bestands-DBs | neuer Migrationsschritt löscht `Tab_ProjektWerte`-Zeilen mit `KategorieID = 3` (**beschlossen, E3**) — erst nachdem das Summen-Label umgestellt ist (HF4) |

---

## 4 HF2 — Energieträger: Einheiten und kWh-Konsistenz

### 4.1 Fachregel (Soll)

```
Menge[Abrechnungseinheit] ──(Umrechnungsfaktor, benannt)──► Menge[kWh-fähige Einheit]
                                        ──(Heizwert Hi bzw. Brennwert Hs [kWh/Einheit])──► kWh
```

- Träger mit `billing_unit = kWh` (Strom, Fernwärme): Regel entfällt, Bedingung erfüllt (Identität).
- Gasförmige Träger (`pricing_model = 'GAS'` bzw. `group_code` Gas): Abrechnungsvolumen **Nm³**; der benannte Faktor („**z-Faktor**") rechnet Betriebs- auf Normvolumen um; kWh über Hs (Abrechnungspfad) bzw. Hi (Simulations-/Bilanzpfad) — beide bleiben wie heute parallel geführt.
- Flüssige/feste Träger: Einheit l/kg/t…, Faktor Standardname „Umrechnungsfaktor" (z. B. Dichte-/Gebindeumrechnung), kWh über Hi/Hs.

### 4.2 Datenmodell (Migrationsschritt M-A)

`energy_conversion` (heute: `ID, id_brennstoff, from_unit, to_unit, factor, user_edited` — `migration.manuell.sql:498`):

| Änderung | Definition | Vorbelegung (DML) |
|---|---|---|
| **+ `faktor_name`** TEXT(50) | Anzeigename des Faktors | „Umrechnungsfaktor"; bei Gasträgern „z-Faktor" |
| **+ `aktiv`** YESNO | Regel abschaltbar statt löschbar (Historie bleibt) | true |
| Bestandsregeln | unverändert (`factor`, `user_edited` gibt es schon) | — |

**Konsistenzprüfer** `Controller\EnergieEinheitenPruefung.cs` (neu, DB-lesend, UI-frei):
- `PruefeKatalog()` / `PruefeProjekt(idProjekt)` → Befundliste `(Traeger, Problem)`;
- Regel je aktivem `energy_carrier`: `billing_unit == "kWh"` **oder** aktive Regel `from_unit = billing_unit → to_unit = "kWh"`-Kette (eine Stufe reicht; Kettenauflösung max. 2 Stufen für Nm³→kWh über Hs);
- Aufruf: beim Speichern in `ucFuelSettings` (blockierend mit Meldung), beim Wirtschaftlichkeits-Lauf (`WirtschaftlichkeitCtrl.LadeParameter`) als Protokollwarnung, und als KI-Leseaktion (`energietraeger_pruefen`) für den Chat.

**Klärung Semantik:** `energy_conversion` bleibt **Einheiten**-Umrechnung; die Energie-Umrechnung (→ kWh) leisten weiterhin Hi/Hs (`hi_/hs_kwh_per_unit`, `custom_hi/hs`, `eff_hi/eff_hs`). Die kWh-Bedingung aus L2 gilt als erfüllt, wenn die Einheitenkette bei einer Einheit endet, für die Hi/Hs gepflegt ist, oder direkt bei kWh. Der Prüfer prüft **beides** (Kette + Hi/Hs > 0).

### 4.3 Dialog (`ucFuelSettings`-Ausbau)

Heute: `cmbUnit` (Einheitenwahl aus `energy_conversion`), Hi/Hs-Felder, Preisfelder (`ucFuelSettings.cs:60-229`). Neu:

- **Umrechnungsblock** unter der Einheitenwahl: Tabelle der Regeln des Trägers — Spalten *Name* (editierbar, Standard „Umrechnungsfaktor"), *von-Einheit*, *nach-Einheit*, *Faktor* (editierbar, `ZahlPruefen`-Validierung), *aktiv*. Anlegen/Deaktivieren möglich; die kWh-Regel bzw. letzte Kette nach kWh ist **nicht deaktivierbar** (Riegel mit Meldung aus dem Prüfer).
- Anzeige **„effektiv: 1 <Einheit> = X kWh (Hi) / Y kWh (Hs)"** — live berechnet aus Kette × Hi/Hs; macht die Konsistenz sichtbar.
- Gasträger: Einheitenbeschriftung zeigt **Nm³**; Faktorzeile heißt „z-Faktor".
- Verstoß-Hinweis (rotes Textfeld statt MessageBox), solange der Träger die L2-Bedingung nicht erfüllt.

### 4.4 Code-Anbindung

| Stelle | Anpassung |
|---|---|
| `Views/Varianten/EnergieMengen.cs:63-79` | Mengenformel nutzt die Regelkette statt implizit `eff_hi` allein (Ergebnis identisch, solange Faktor = 1; Kommentar ergänzen) |
| `Allgemein/Bericht/KostenEmissionRechner.cs` | unverändert im Kern; liest weiterhin `Abfrage_Energietraeger_Effektiv` |
| `Abfrage_Energietraeger_Effektiv` (Access) | **unverändert lassen** (liefert eff_hi/eff_hs/billing_unit); die Regelkette setzt davor an. Kein Eingriff in die .accdb nötig |
| `Controller/StromPreisCtrl.cs:340-410` | unverändert (Strom ist kWh-direkt) |
| `Allgemein/Wirtschaftlichkeit/SteuerGutschriftRechner.cs` | nutzt die Regelkette für einheitenrichtige Steuersätze (→ HF6/L8) |
| KI-Aktionen | neue Leseaktion `energietraeger_pruefen` (Befundliste des Prüfers) im Bestandsmuster `Allgemein\KI\Aktionen\` |

---

## 5 HF3 — Initialbefüllung der Energieträger

Ein DML-Migrationsschritt (M-B) sät **je aktivem Katalogträger**:

| Trägergruppe | billing_unit | Regel-Seed | Faktorname | Hi/Hs |
|---|---|---|---|---|
| Erdgas E/LL, Biogas, Wasserstoff (Gase) | **Nm³** (Umbenennung von m³, reine Semantik) | Betriebs-m³ → Nm³, Faktor **1,0** | **z-Faktor** | vorhandene Katalogwerte bleiben (kWh/Nm³) |
| Flüssiggas | kg (Abrechnung je 1.000 kg → HF6) | kg → kWh über Hs/Hi | Umrechnungsfaktor | Katalog |
| Heizöl EL, Rapsöl, tierische Fette | l | l → kWh über Hi/Hs | Umrechnungsfaktor | Katalog |
| Holz, Pellets, Kohle, Koks | kg bzw. t | Einheit → kWh über Hi | Umrechnungsfaktor | Katalog |
| Strom, Fernwärme | kWh | keine Regel (Identität) | — | — |

Grundsätze (aus L5): **ergebnisneutral** (z-Faktor 1,0; keine Änderung von Hi/Hs-Zahlwerten), `user_edited = true` wird nie überschrieben, fehlende Regeln werden ergänzt, vorhandene nie ersetzt. Als **Gegenprobe** dienen die Alt-Heizwerte aus `TABELLEN.XLS` (`Tab_Kosten!A200-B205`: Erdgas 11,48 kWh/m³ · Flüssiggas 13,77 kWh/kg · Öl 10,08 kWh/l · Biogas 6 kWh/m³ · Rapsöl 8,75 kWh/l) — Abweichungen > 10 % zum EPOS-Katalog werden im Migrationsprotokoll gemeldet, **nicht** automatisch übernommen.

Nach M-B gilt: `EnergieEinheitenPruefung.PruefeKatalog()` liefert **null Befunde** — das ist das Abnahmekriterium der Etappe.

---

## 6 HF4 — `Form_Kosten`: Reiter „Kostenprofil" und Aufräumen

### 6.1 Neuer Reiter „Kostenprofil"

- **Vierter Reiter** nach „Energiekosten" (`tabMain`), programmatisch erzeugt (Hausregel „Designer unberührt", vgl. `Form_Start.cs:2003-2010`); Kategorie-Logik `KategorieID = SelectedIndex + 1` (`Form_Kosten.cs:20-22`) wird auf die drei Bestandsreiter begrenzt (Wächter statt Index-Arithmetik).
- Inhalt: **zwei Karten** statt Buttons, im Muster der KonfigUI-Karten (`Views\Simulation\ErzeugerKarte.cs`/`SpeicherKarte.cs`; der ungenutzte `Views\Kosten\SectionPanel.cs` kann als Rahmen dienen oder entfällt):
  - **Karte „Kostenprofil"** — Titel, Statuszeile („‹Name›, Monatsniveau X–Y ct/kWh" bzw. „Noch kein Kostenprofil"), Klick öffnet `Form_Kostenprofil` mit der Bestandslogik aus `KostenprofilBearbeiten()` (`Form_Kosten.cs:176-189`: erstes Projektprofil oder neu).
  - **Karte „Spotmarktpreise"** — Statuszeile aus `Tab_Preisreihe` (Anzahl Reihen, Zeitraum, Quelle), Klick öffnet `Form_SpotpreisImport`.
  - Karten: Rahmen, Titelzeile, Beschreibungstext, Kennwertzeile, Hover-Hervorhebung; Texte über `MyResource` (neue Schlüssel `KPROF_KARTE_*`; die Button-Schlüssel `PREIS_BTN_SPOTIMPORT`/`PREIS_BTN_KOSTENPROFIL` entfallen).
- **`BauePreisreihenEinstieg()` entfällt vollständig** (`Form_Kosten.cs:122-165`, graues Panel mit zwei Buttons im Energie-Reiter) samt Aufrufstelle.

### 6.2 Aufräumen im Reiter „Energiekosten"

- Blaue Kopfleiste **„Energieträger"** über der linken Liste entfernen: `panel2` + `label5` (`Form_Kosten.Designer.cs:125-139`); das zweite gleichlautende `label1` (`:225-233`) anhand der Parent-Kette prüfen und mitziehen. `listBox_Energieträger` rückt nach oben, Panel-Layout nachziehen. (Achtung: Designer-Datei → cp1252-Prüfung vor dem Edit, Bytes messen.)
- Fußzeile **„PROJEKT GESAMT (Energiekosten)"** speist sich künftig aus `KostenEmissionRechner` (Energiekosten p. a. des gewählten Projekts) statt aus der toten Kategorie-3-Summe (heute konstant „0,00 €").

---

## 7 HF5 — Komponenten und Kostenpositionen nach BHKW-Plan

### 7.1 Befund Altanwendung (Extraktion 19.08.)

Die Investitions-Eingabe der Altanwendung (`Dial_KostenEing` → Blatt `Tab_Kosten`) ist eine **flache Liste**; „Wärmezentrale" und „Bauliche Anlagen" existieren nur als **Bemessungsbasen der Instandhaltung** (`Mod_KostEing.bas:2300 ff.`). Positionskatalog und Betriebskostenkatalog mit Original-Beschriftungen: **Anhang A**. Dort auch die drei Zuschuss-Mechanismen und die bekannten Altfehler (Nebenkosten-Basis widersprüchlich, Zuschuss-Nutzungsdauer zufällig, „€/m³"-Fehlbeschriftung), die **nicht** nachgebaut werden.

### 7.2 Soll-Komponenten (`Tab_KostenKomponente`, Migrationsschritt M-C)

| Komponente | Herkunft | Technik-Planwert |
|---|---|---|
| BHKW · Heizkessel · Wärmepumpe · PV · Solarthermie · Stromspeicher · Pufferspeicher | Bestand (7) | ja (`TechnikPlanwertCtrl.cs:159-165`, unverändert) |
| **Wärmezentrale** | neu, BHKW-Plan | nein (Erfassungsgruppe) |
| **Bauliche Anlagen** | neu, BHKW-Plan | nein |
| **Stromeinspeisung** | neu, BHKW-Plan | nein |

**Nahwärmenetz wird nicht aufgenommen (E2)** — die Alt-Positionen Verteilnetz/Hausanschluss/Hausstation entfallen ersatzlos; Einzelfälle deckt „Sonstiges". Pufferspeicher bleibt eigene Komponente (**E1**, Planwert-Anbindung); in der Wärmezentrale wird er **nicht** doppelt gesät (Abweichung von der Alt-Bemessung).

### 7.3 Positionskatalog (`Tab_Kostenfaktor`-Seeds, M-C)

Je neue Komponente die Alt-Positionen als Nebenpositionen (`IsMainComponent = false`), Original-Beschriftungen aus Anhang A: Wärmezentrale → *BHKW-Einbindung, Heizungstechnik, Abgasanlage*; Bauliche Anlagen → *Heizraum, Schornstein, Bauliche Maßnahmen, Heizöllagerung, Erdgasanschluss*; Stromeinspeisung → *Stromeinspeisung*. Dazu je Komponente „Sonstiges" (frei benennbar — Mechanik über `Tab_KostenGruppenKatalog` existiert). Die Mengenlogik „Heizraum = spez. Kosten €/m³ × Raumbedarf" bildet die vorhandene Bemessung `Menge × Einheitpreis` ab (`Tab_ProjektWerte.Bemessung/Menge/Einheitpreis`, Schritt 19) — keine neue Mechanik nötig.

### 7.4 Zuschuss als Positionsart (M-C + Rechenweg)

- `DbWerte`: neue Konstante `KOSTENART_ZUSCHUSS = "zuschuss"` (Drei-Schichten-Regel; Anzeige über `MyResource`).
- Eingabe: normale Kostenposition (Kategorie 1) mit Kostenart „Zuschuss", Beschriftungsvorlage „Zuschuss (BAFA, KfW, …)"; positive Betragseingabe.
- Rechnung (`WirtschaftlichkeitCtrl.LiesInvestitionen` + `KapitalwertRechner`): Zuschuss mindert **I₀ einmalig**; **keine** Ersatzbeschaffung, **kein** Restwert (im Gegensatz zur Alt-Logik, die dem Zuschuss eine zufällige Nutzungsdauer gab — Anhang A(e)). Ausweis als eigene Zeile in `UcBkKosten`/Bericht („Zuschuss: −X €").
- Betriebskosten-Bemessungen „% der Investitionssumme" rechnen **vor** Zuschussabzug (klare Regel; die Altanwendung war hier widersprüchlich).

### 7.5 Darstellung in EPOS-Plan — bewusst anders als BHKW-Plan (L9)

Die Alt-Maske war eine flache Liste mit Kopf-Sonderfeldern und modalen Ketten. In EPOS-Plan wird stattdessen der **bestehende** Erfassungsweg ausgebaut:

1. **Komponentenweise Gruppierung im Reiter „Investitionskosten":** Positionszeilen (`ucKostenZeile`, Bestand) werden unter einklappbaren **Gruppenkopfzeilen je Komponente** angeordnet — Kopf mit Komponentenname, Gruppensumme und Zähler („Wärmezentrale · 3 Positionen · 42.500 €"). Als Träger bietet sich der bislang ungenutzte `Views\Kosten\ucKategorieHeader`/`SectionPanel` an (prüfen, sonst neu im Karten-Muster); Aufbau programmatisch, Designer unberührt.
2. **Eine Position statt „oder"-Doppelfeldern:** Die Alt-Zeilen „Vollwartung €/h **oder** €/kWhel **oder** % Invest" werden **eine** Position „Wartung/Instandhaltung BHKW" mit **Bemessungswahl** über das vorhandene Feld `Bemessung` (`EUR_PRO_H` / `EUR_PRO_KWH` / `PROZENT_INVESTITION`, `DbWerte.BEMESSUNG_*`) — der Alt-Widerspruch („oder" wurde tatsächlich addiert) ist damit strukturell weg.
3. **Best/Worst-Szenarien und Nutzungsdauer je Position** bleiben Bestandsfunktionen (`Form_CaseEingabe`, Nutzungsdauer-Spalten) — das hatte die Altanwendung nicht; keine Sonderbehandlung der BHKW-Module nötig (Zinsreduktions-Felder der Alt-App entfallen, der Kalkulationszins bleibt einer).
4. **Planwert-Übernahme statt Gerätedaten-Automatik:** BHKW-/Kessel-Investitionen kommen wie bisher über `Form_PlanwertUebernahme`/`TechnikPlanwertCtrl` aus den Technikdaten — als transparente Übernahme mit Abweichungsanzeige (`UcBkKosten`), nicht als stille Kopplung.
5. **Empfehlungsbereiche als Hinweis, nicht als Beschriftungsanhängsel:** dezenter Hinweistext/Tooltip am Satz-Feld („üblich 1,8–2,2 %"), Quelle Katalog (§ 7.6).
6. **Zuschuss sichtbar:** eigene Zeile mit negativem Ausweis in Gruppensumme, `UcBkKosten` und Bericht — keine Kopffelder wie im Alt.
7. **Mengen-Positionen** (z. B. Heizraum €/m³ × m³) über die vorhandene Bemessung `Menge × Einheitpreis` mit Einheitenanzeige in der Zeile.

### 7.6 Betriebskosten-Empfehlungsbereiche (M-C, optionaler Teil)

Die Alt-Empfehlungen existieren nur als Dialogtexte; sie werden als Katalogdaten übernommen: `Tab_Kostenfaktor` + Spalten `Empfehlung_von`/`Empfehlung_bis` (DOUBLE, nullable) für die VDI-2067-Positionen (Werte: BHKW 3,0–9,0 · Kessel 1,5–2,5 · Wärmezentrale 1,8–2,2 · Bauliche Anlagen 1,0–1,5 · Stromeinspeisung 1,8–2,2 · Personal 1,0–4,0 · Verwaltung 0,8–2,0 % — Anhang A(b)). `Form_Betriebskosten` zeigt sie als Hinweistext neben dem Satz-Feld.

---

## 8 HF6 — KWKG/Steuern: neue Eingaben und Datenstrukturen

Grundlage `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`; baut auf dem vorhandenen Stand auf (Migrationsschritt 20: `Unternehmensart, Raeumlicher_Zusammenhang, Hocheffizienz_Nachweis, Jahresnutzungsgrad, Energiesteuer_Wahl, Aufteilung_Methode`; `SteuerGutschriftRechner`; `GesetzKatalog`).

### 8.1 Neue Projektfelder (`Tab_ProjektWirtschaftlichkeit`, Migrationsschritt M-D)

| Spalte | Typ | Inhalt | Wirkung |
|---|---|---|---|
| `KWKG_Tatbestand` | TEXT(30) | `keiner` · `anlage_bis_100kw` · `kundenanlage` · `stromkostenintensiv` (DbWerte-Konstanten) | Eigenstrom-Zuschlag gibt es **nur** in den drei Fällen des § 6 Abs. 3; Satzwahl je Leistungsanteil nach § 7 Abs. 2 |
| `KWKG_Anlagenart` | TEXT(20) | `neu` · `modernisiert` · `nachgeruestet` | leitet das Vbh-Kontingent ab (§ 8): neu 30.000; modernisiert 15.000/30.000 (+6.000-Sonderfall); nachgerüstet 10.000/15.000/30.000 |
| `KWKG_Kostenanteil` | DOUBLE | Anteil an Neuherstellungskosten [%] | Staffelauswahl bei modernisiert/nachgerüstet |
| `KWKG_Pauschalmodus` | YESNO | § 9: Anlagen ≤ 2 kWel | Einmalerlös t0 = 0,04 € × 60.000 Vbh × P_el; laufender KWKG-Erlös = 0 (ersetzt die Alt-„PauschBonus"-Mechanik sauber als Erlös statt Investitionsminderung) |

`KWKG_Vbh_Kontingent` bleibt als **Override** erhalten (0 = automatisch aus Anlagenart). Der Jahresdeckel § 8 Abs. 4 liegt bereits als Staffel im Gesetzeskatalog.

### 8.2 Einheitenrichtige Steuersätze (L8, koppelt an HF2)

`Tab_Gesetzesparameter` führt die Sätze künftig in der **Gesetzeseinheit** (Spalte `Einheit` existiert): Erdgas 5,50 €/MWh · Heizöl 61,35 €/**1.000 l** (§ 53a Abs. 5: 40,35) · Flüssiggas je **1.000 kg** (60,60 / 19,60) · Stromsteuer 20,00 €/MWh (Restbelastung 0,50). `SteuerGutschriftRechner` rechnet über die HF2-Regelkette (l bzw. kg → kWh über Hi) in €/kWh um — der Faktor-10-Fehler der Altanwendung (Öl als €/MWh geführt) ist damit strukturell ausgeschlossen. Seeds als neue Katalogzeilen mit `JahrVon = 2026`, Quelle laut Grundlagen-Doku; Altzeilen bleiben (Stichtagslogik).

### 8.3 CO₂-Preispfad als Stützstellenreihe

- Rechnung: `KostenEmissionRechner`/`WirtschaftlichkeitCtrl` nutzen den **jahresgenauen Pfad** aus dem Gesetzeskatalog (Klasse CO₂-Preispfad, `JahrVon`-Stützstellen) statt des konstanten `CO2_Preis`; der Projektwert `CO2_Preis` bleibt als Override („konstanter Preis") mit 0 = Pfad.
- Editor: `Form_Gesetzesparameter` zeigt je Stützstelle den `Status` („gesichert bis 2026" · „Korridor 2027" · „**Prognose ab 2028**") sichtbar an; `Form_WirtschaftlichkeitParameter` erhält statt des nackten Zahlenfelds eine Zeile „CO₂-Preis: Pfad aus Gesetzeskatalog (Prognoseanteil ab 2028) / konstant: … €/t" mit Sprungknopf zur Pflege.
- Seeds: gesicherte Werte bis 2026, Korridor 2027 (55–65 €/t, Mittelwert als Vorschlag), ab 2028 Vorbelegung **konservativ 80 €/t** (beschlossen, E5); alle Stützstellen bleiben **frei editierbar** (E5). Die Szenarien „mittel"/„hoch" aus § 8.4 der Grundlagen-Doku bleiben als dokumentierte Alternativen vermerkt, werden aber nicht gesät.

### 8.4 Sockelbeträge und Antragshinweise

- Gesetzeskatalog: Sockel 250 €/a für § 9b StromStG und § 54 EnergieStG als Parameter; `SteuerGutschriftRechner` zieht den Sockel vor Ausweis der Gutschrift ab.
- Dialog `Form_WirtschaftlichkeitParameter`: Info-Bereich je aktivierter Entlastung mit Formularnummer und Frist („§ 9b → Formular 1453, bis 31.12. des Folgejahres" usw.) — reine Hinweistexte über `MyResource`, keine neue Datenhaltung.
- Bericht (optional, Ausbaustufe): Baustein „Anträge und Fristen" im Word-Bericht.

---

## 9 Code-Anbindung und Migrationsmechanik (Querschnitt)

1. **Migrationsschritte** in `SchemaKatalog`/`SchemaMigration` (fortlaufend nach Bestand, Reihenfolge): M-A (HF2-Spalten) → M-B (HF3-Seeds) → M-C (HF5-Komponenten/Positionen/Empfehlungen) → M-D (HF6-Felder/Katalogzeilen) → **M-E (HF1-Drops zuletzt)**. Jeder Schritt idempotent, kein DDL-DEFAULT, Vorbelegung per DML.
2. **Doppelte Schema-Wahrheit beachten:** `WirtschaftlichkeitCtrl.StelleTabellenSicher()` legt seine Tabellen selbst an — neue Wirtschaftlichkeits-Spalten (M-D) dort **und** im Migrationskatalog nachziehen (bekanntes Muster, `WirtschaftlichkeitCtrl.cs:290-295`).
3. **Access-manuelle Schritte** (gespeicherte Abfragen, Beziehungen, Sichtprüfungen): Checkliste Anhang B; Ausführung dokumentiert Philipp in der Produktiv-DB und in `Referenzlaeufe\Arbeitskopie`.
4. **KI-Aktionen:** `energietraeger_pruefen` (lesend, HF2); bestehende `kostenposition_setzen` bleibt kompatibel (neue Kostenart „zuschuss" in die Positivliste der `KiPruefung` aufnehmen).
5. **Arbeitsregeln:** cp1252-Falle bei Designer-/Altdateien (Bytes messen, byte-erhaltend editieren); nach Parallelsitzungen repoweiter `<<<<<<<`-Sweep; Sync-Automatik committet den ganzen Baum — chirurgische Commits je Etappe.

---

## 10 Etappen und Verifikation

| Etappe | Inhalt | Abnahmekriterium |
|---|---|---|
| **K1** | HF1: Code-Aufräumung + M-E-Vorbereitung (Drops erst in K6 ausführen), Access-Checkliste erstellen, Doku-Fixes | Build grün; Referenzläufe B5 byte-identisch; Duplizieren-Smoke |
| **K2** | HF2: M-A, `EnergieEinheitenPruefung`, KI-Leseaktion | Prüfer läuft, Befundliste plausibel; Ergebnisse unverändert |
| **K3** | HF3: M-B Seeds + `ucFuelSettings`-Umbau | `PruefeKatalog()` = 0 Befunde; Referenzläufe unverändert (z = 1,0) |
| **K4** | HF4: Reiter „Kostenprofil" (Karten), Kopfzeile weg, Energie-Summenlabel | UI-Abnahme Philipp am Screenshot-Fall; Kategorie-Wächter getestet |
| **K5** | HF5: M-C Komponenten/Positionen/Empfehlungen + Zuschuss-Rechenweg | Testprojekt „BHKW Test München": Zuschuss mindert I₀ einmalig, keine Ersatzbeschaffung; UcBkKosten zeigt neue Gruppen |
| **K6** | HF6: M-D + Rechner-Anpassungen + **M-E Drops** + manuelle Access-Schritte | Steuersätze einheitenrichtig (Öl-Gegenprobe ≈ Faktor 10 zur Altanwendung); CO₂-Pfad jahresgenau; danach Drops, Voll-Smoke |

Jede Etappe: eigener Commit-Block, Smoke beider Rechenwege, Referenzlauf-Vergleich, `<<<<<<<`-Sweep. K2/K3 sind zwingend **ergebnisneutral**; erste gewollte Ergebnisänderungen kommen mit K5 (Zuschuss) und K6 (Steuern/CO₂-Pfad) und werden im jeweiligen Protokoll ausgewiesen.

Mit den Entscheidungen vom 19.08.2026 (§ 11) ist das Konzept **umsetzungsreif**; K1 startet auf Zuruf.

---

## 11 Entscheidungen — beschlossen von Philipp am 19.08.2026

| Nr. | Frage | **Entscheidung** |
|---|---|---|
| E1 | Pufferspeicher: eigene Komponente oder alt-treu in „Wärmezentrale"? | **eigene Komponente behalten** (Wärmezentrale ohne Puffer-Doppelung) |
| E2 | Nahwärmenetz als neue Komponente aufnehmen? | **Nein** — Alt-Positionen Verteilnetz/Hausanschluss/Hausstation entfallen ersatzlos |
| E3 | Kategorie-3-Altzeilen in Bestands-DBs? | **löschen** (Migrationsschritt, nach Umstellung des Summen-Labels) |
| E4 | `Abfrage_ProjektKostenInvestBetrieb` (kein Code-Aufrufer)? | **löschen**; Kommentar in `GruppeSichern` anpassen |
| E5 | CO₂-Preispfad-Vorbelegung ab 2028? | **konservativ 80 €/t, frei editierbar** (Stützstellenreihe) |
| E6 | z-Faktor-Seed 1,0 (ergebnisneutral), echte Zustandszahlen später fachlich? | **ja** |
| E7 | BAFA-Mini-KWK-Staffel? | **nicht übernehmen** — nur Zuschuss-Positionsart (L7) |

---

## Anhang A — Kosteneingabe der Altanwendung (Extraktion 19.08.2026)

Quelle: `BHKWPlan\BHKW-WP-PLAN.XLSM` (VBA/UserForms) + `TABELLEN.XLS` (Blatt `Tab_Kosten`, `Tab_Wirtschaftlichkeit`); Passwort-geschützte Dateien rein lesend entschlüsselt; Belege je Zeile beim Extraktionslauf.

### A(a) Investitionspositionen (`Dial_KostenEing`, alle netto)

Kopf: Zinssatz (`Tab_Kosten!B3`) · **Zuschuss (BAFA, KfW, Baukosten usw.) [€]** (`F3`) · Pauschalierter Bonus < 2 kWel (`F2`) · Zinsreduktion BHKW-Module (`B4`) / Rest-Investition (`F4`).
Gruppenblöcke: **BHKW-Module** 1–10 (aus Gerätedaten) · **Spitzenkessel** 1–6 · **Heizraum** (spez. Kosten €/m³ × Raumbedarf BHKW/SPK/Puffer) · **Nahwärmenetz** (Verteilnetz; Hausanschluss × Anzahl; Hausstation × Anzahl) · **Heizzentrale**: BHKW-Einbindung, Heizungstechnik, Stromeinspeisung, Heizöllagerung, Erdgasanschluss, Schornstein, Abgasanlage, Pufferspeicher, Bauliche Maßnahmen, Sonstiges 1–3 (frei) · **Nebenkosten** (% der Investition, vor Zuschussabzug) · Summe (`B71`).
Je Position: Betrag + **Nutzungsdauer [a]** + errechnete Kapitalkosten (Annuität; BHKW-Module mit eigenem Zinsfaktor).

### A(b) Betriebskosten (`Dial_BetriebKost`, „nach VDI 2067", Prozent schlägt Absolut)

| # | Position (Original-Beschriftung) | Empfehlung | Bemessung |
|---|---|---|---|
| 1/2 | „Vollwartung / Wartung BHKW" | €/h **oder** €/kWhel | Betriebsstunden bzw. Stromproduktion |
| 3 | „oder Instandhaltung BHKW" | 3,0–9,0 % | Investition BHKW (netto) |
| 4 | „Instandhaltung Heizkessel" | 1,5–2,5 % | Investition Kessel (netto) |
| 5 | „Instandhaltung Wärmezentrale" | 1,8–2,2 % | Heizungstechnik + BHKW-Einbindung + **Pufferspeicher** + Abgasanlage |
| 6 | „Instandhaltung bauliche Anlagen" | 1,0–1,5 % | Heizraum + Schornstein + Baul. Maßnahmen + Heizöllagerung + Erdgasanschluss |
| 7 | „Instandhaltung Stromeinspeisung" | 1,8–2,2 % | Investition Stromeinspeisung |
| 8 | „Personalkosten" | 1,0–4,0 % | Investitionssumme |
| 9 | „Steuern, Versicherungs- und Verwaltungskosten" | 0,8–2,0 % | Investitionssumme |
| 10 | „Hilfsenergiekosten" | % | **Brennstoffkosten** |
| 11 | „Reserveleistungskosten" | €/a | absolut |
| 12 | Sonstiges (frei benennbar) | €/a | absolut |

### A(c) Steuer-/Rahmenfelder (`Dial_KonKosten`)

Energiesteuererstattung BHKW: Gas €/MWh (`Tab_Wirtschaftlichkeit!B94` = 5,5) · Öl „€/MWh" (`B95` = **61,35 — Einheitenfehler**, richtig €/1.000 l) · Flüssiggas (`B93` = 4,4, richtig je 1.000 kg) · Stromsteuer (`B97` = −20,5). MwSt-Schalter netto/brutto (`Tab_Kosten!B210`). Vergleichsheizung: alternativer Wärmepreis Winter/Sommer €/kWh + Grundpreis €/a. Heizwerte-Hinweisblock (`Tab_Kosten!A200-B205`).

### A(d) Vorgabewerte `TABELLEN.XLS`

Kein Investitions-Richtwertkatalog (kommt aus Gerätedaten: BHKW €/kWel, Kessel €, Wartung €/kWhel, Raumbedarf m³, Nutzungsdauer a). Empfehlungssätze existieren **nur** als Dialogtexte (A(b)); die Blattzellen `D81…D89` enthalten Testwerte 1…9. Preissteigerungs-Vorgaben der Vorlage: Brennstoff/Strom 8 %, Wartung 5 % (KWK) bzw. 3/4 % (WP).

### A(e) Zuschuss-Mechanismen (drei, mit Altfehlern)

1. **Freier Zuschuss** (`F3`): mindert Investitionssumme; Kapitalkosten-Gutschrift mit **zufälliger** Nutzungsdauer (Laufvariable des letzten BHKW-Moduls) — wird so **nicht** übernommen (L7: einmalige I₀-Minderung).
2. **BAFA Mini-KWK** (Staffel 1.900 € + 300/100/10 €/kW; Antrag nur bis 31.12.2020) — entfällt.
3. **KWKG-Pauschale < 2 kWel** (60.000 × 0,08 € = 4.800 €; setzt laufende Boni auf 0) — wird HF6-Pauschalmodus nach § 9 KWKG 2025 (4 ct × 60.000 Vbh × P_el, als Einmal**erlös**, nicht Investitionsminderung).

Bekannte Altfehler, die nicht nachgebaut werden: Nebenkosten-Basis Dialog ≠ Blatt; Zuschuss/MwSt-Bruch (Abzug roh von Bruttosumme, dann ÷1,19); Label „€/m³" am NW-Verteilnetz bei absolutem €-Betrag; „oder"-Positionen 1–3 werden tatsächlich **addiert**.

---

## Anhang B — Manuelle Access-Checkliste (vor/mit K6)

1. In Access **Objektabhängigkeiten** aktivieren (Extras → Name-AutoKorrektur-Info) und für jede Lösch-Abfrage aus § 3.3 prüfen, ob andere Abfragen sie referenzieren — besonders `Abfrage_MaxMin_Vorlauf` ↔ `Abfrage_Max/Min_Vorlauf` und `Abfrage_Kuehlung_MaxLast` ↔ `Abfrage_KenndatenKuehlung_Max`.
2. Die Abfrage identifizieren, die `energy_carrier + energy_group + energy_conversion + pricing_model + energy_unit (×4 Aliasse a_unit…a_unit_3)` joint (Kandidat: `Abfrage_Neues_Kosten_Model`) → löschen, **bevor** `energy_unit`/`energy_group` gedroppt werden.
3. Beziehungen löschen: `Tab_ProjektTab_Brennstoff_Projekt`, `Tab_Brennstoff_StammTab_Brennstoff_Projekt` (macht der Migrationsschritt per `ALTER TABLE … DROP CONSTRAINT`; falls Namen in der Produktiv-DB abweichen: Beziehungsfenster).
4. Sichtprüfung der mehrdeutigen Objekte: `Tab_KostenKategorien` (Plural), `Tab_ErgebnisKomponente`, `Tab_ErgebnisMonat`, `Tab_Gebaeude1` — falls vorhanden und leer → löschen.
5. Danach: Komprimieren/Reparieren; Kopie nach `Referenzlaeufe\Arbeitskopie\` aktualisieren; Referenzläufe erneut rechnen.

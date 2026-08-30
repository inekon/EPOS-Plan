# BK3 — Trägerzuordnung: Spaltendefaults der Projektzeile (Umsetzungsprotokoll)

Anlass: Messnachtrag aus Etappe BK2. Der manuelle Zuordnungsweg
`EnergietraegerKatalogCtrl.InsProjekt` (Ä10) nennt im INSERT nur Projekt und Träger und
verspricht im Kommentar darüber „alle `custom_`-Felder bleiben NULL, es GELTEN also die
Katalogwerte". Access füllt nicht genannte Spalten aber mit ihrem **Spaltendefault**, und
der ist in `energy_project_settings` bei neun Spalten **0**. Für die Emissionen war das
folgenlos (BK2), für Heizwert, Preise und Umrechnungssatz nicht. Stand 30.08.2026, Branch
`Pufferspeicher` (Basis `b2ad3e3`, inkl. BK1/BK2).

## 1. Befund

**Frage 1 — was schreibt der Weg wirklich?** Gemessen am Roh-INSERT gegen eine frische
Produktivkopie: `custom_hi`, `custom_Hs`, `custom_price_work`, `custom_price_base`,
`custom_price_power`, `co2`, `so2`, `nox`, `ID_Umrechnung` stehen danach alle auf **0** —
keine einzige NULL. Der Kommentar beschrieb also das Gegenteil des Verhaltens.

**Frage 2 — was richtet die 0 an?**

| Spalte | Wirkung der 0 | Fundstelle |
|---|---|---|
| `custom_hi` | gilt dem Kosten-Dialog als **gepflegt** (`??` prüft nur NULL): Heizwertfeld 0,00 statt Katalogwert, die kWh-Erreichbarkeitsprüfung lehnt das Speichern ab | `ucFuelSettings.cs` LoadData |
| `ID_Umrechnung` | 0 trifft keine Zeile (`energy_conversion` hat Autowert-IDs 1…72) → keine Einheiten-Vorwahl, der Dialog fällt still auf die **erste Listenregel** zurück | `ucFuelSettings.GetTargetUnitByConversionId` |
| `custom_price_base` | schattet den Katalog-Grundpreis still (`sGrund ?? kGrund`) | `KostenEmissionRechner.cs:504` |
| `custom_price_work` | Rechner und Fußzeile werten 0 als „ungepflegt", die Trägertabelle zeigte aber „0,0000" — zwei Lesarten derselben Zahl | `UcBkKosten.LiesPreise` gegen `KostenEmissionRechner.cs:502-503` |
| `co2`/`so2`/`nox` | folgenlos, die Lesekette zählt nur Werte **> 0** als gepflegt (BK2) | `EmissionsFaktorLader` |

Die **Rechenwege** sind gegen `hi = 0` gesichert: `Abfrage_Energietraeger_Effektiv` fängt
`Is Null Or = 0` per IIf ab (in der Produktiv-DB verifiziert), alle Verwerter prüfen `> 0`.

**Frage 3 — wer verträgt kein NULL?** Zwei Lesestellen in `ucFuelSettings.LoadData`:
die drei Preiszeilen casten `double?` **ohne** `??`-Rückfall nach `decimal`, und
`ID_Umrechnung` geht per `dynamic` in einen `int`-Parameter. Beide werfen bei NULL
(gemessen am formgleichen Nachbau: `RuntimeBinderException`, „Cannot convert null to
'decimal'" bzw. „no best overloaded method match"). Brisant, weil `SpeichereWerte` bei
leerer Einheitenauswahl **selbst** DBNull in `ID_Umrechnung` schreibt — der NULL-Fall
existierte also schon vor BK3.

**Bestand (34 Zeilen, frische Produktivkopie):** keine einzige NULL in den neun Spalten;
keine Zeile mit `custom_hi = 0` oder `ID_Umrechnung = 0`; 15 Zeilen mit
Arbeits-/Grund-/Leistungspreis **0/0/0**, 21 Zeilen mit Grundpreis 0, 31 Zeilen mit
Leistungspreis 0; genau **eine** `-1`-Waise (ID 10076, Projekt 1039 / Träger 63).
Katalog `energy_carrier`: `hi` bei 27/27 gepflegt, `price_work/_base/_power` bei allen 27
= 0.

## 2. Entscheidung

Das INSERT löst das Versprechen selbst ein. Die **acht Wertspalten** werden ausdrücklich
als typisierte `DBNull` geschrieben (Muster BK2) — NULL ist die ehrliche Aussage „nicht
gepflegt" und die einzige, die sich von einem echten Nullwert unterscheiden lässt.
`ID_Umrechnung` ist **keine Wertkopie**, sondern der Verweis auf die Recheneinheit; sie
wird über **dieselbe Identitätsregel wie der Wizard-Weg** ermittelt
(`WizardCtrl.ConvIdErmitteln`, `from_unit = to_unit` = Abrechnungseinheit aus
`Tab_Brennstoff_Stamm`) und bleibt damit Ä10-konform; ohne Regel `-1`.

Dazu die zwei NULL-Guards im Trägerdialog und die Angleichung der Arbeitspreis-Lesung der
Kosten-Seite an die Rechnerkette.

**Ausdrücklich nicht:** Bestandszeilen bleiben unangetastet (kein Heilungsschritt),
**kein Schemaschritt** (keine Spalte, kein Default geändert), und die **Preis-/Heizwert-Kopie
des Wizard-Wegs** (`TraegerSatzAnlegen`) bleibt Bestandsverhalten wie in BK2 entschieden.
Der **Grundpreis** wird in der Anzeige *nicht* umgestellt: 0 €/a ist dort ein gültiger
Vertragswert (Abgrenzung im Rechner, `KostenEmissionRechner.cs:488-499`).

## 3. Geänderte Dateien

```
Controller/EnergietraegerKatalogCtrl.cs   InsProjekt schreibt 8 × DBNull + ID_Umrechnung;
                                          neuer Helfer UmrechnungFuer; Ä10-Kommentar neu
Controller/WizardCtrl.cs                  ConvIdErmitteln private -> internal (+ XML-Doc)
Views/Kosten/ucFuelSettings.cs            LoadData: 3 Preiszeilen mit ?? _carrier.price_*,
                                          ID_Umrechnung mit ?? -1 vor dem int-Aufruf
Views/BerichteKosten/UcBkKosten.cs        LiesPreise: Arbeitspreis nach Rechnerkette
                                          (nur > 0), Grundpreis unverändert
Allgemein/Reporting/BK3_..._Protokoll.md  dieses Protokoll
Allgemein/Reporting/BK1_..._Protokoll.md  ein Satz zum manuellen Weg nachgezogen
```

## 4. Nachweise (Harness `..\dev\bk3\`, frische Laufkopie je Start)

| Probe | Soll | Ergebnis |
|---|---|---|
| Build x64 | Exit 0 | Exit 0, nur bekannte Altwarnungen |
| [0] Default-Beweis | Roh-INSERT (1017/63) → neun Spalten 0 | alle neun **0**, 0 NULL; Testzeile wieder gelöscht |
| [1] `InsProjekt(1017, 63)` | `true`, 8 × NULL, Umrechnung = Regel | `true`; **8 von 8** Wertspalten NULL; `ID_Umrechnung = -1` (siehe § 5) |
| [1] Idempotenz | zweiter Aufruf `true`, keine zweite Zeile | 0 → 1 → 1 |
| [1b] `InsProjekt(1017, 62)` | Träger **mit** Identitätsregel | 8 × NULL, `ID_Umrechnung = 35` (L→L) — Regel greift |
| [2] `Abfrage_Energietraeger_Effektiv` | eff_hi 10,5 / eff_hs 11,6 | 10,5 / 11,6 |
| [2] `EmissionsFaktorLader.Lade(1017,63)` | 201 g/kWh, Ebene `KATALOG` | 201,00 / `KATALOG`, SO₂ 0,30, NOₓ 110 |
| [2] `EnergieEinheitenPruefung` | Hi 10,5 aus Katalog, kein Befund | Trägersicht Hi 10,50 / Hs 11,60, Start „Nm³"; `PruefeProjekt(1017)` ohne Befund zu 63 |
| [3] `ucFuelSettings(1017, 63)` | kein Wurf, Heizwert 10,5 statt 0,00 | kein Wurf; 10,50 / 11,60; Preisfelder 0,0000 = Katalog |
| [3] Alt-Kontrast | alte Zeile hätte geworfen | Nachbau: `RuntimeBinderException` beim `(decimal)`-Cast |
| [3b] `ucFuelSettings(1017, 62)` | Vorwahl aus der Regel | `cmbUnit = L`, `id_conversion = 35` — Fehlvorwahl geheilt |
| [4] `ID_Umrechnung = NULL` | kein Wurf, keine Vorwahl | kein Wurf; keine Vorwahl; Alt-Nachbau wirft an genau dieser Stelle |
| [5] `UcBkKosten` 1011/1018/1026 | Arbeitspreis „—" statt „0,0000" | alle drei: Zelle **„—"**; Grundpreis weiter „0,00"; Fußzeile „⚠ Arbeitspreis 0,00 bei: Erdgas E" **unverändert vorhanden**, `TraegerOhnePreis = 1` — das ist allerdings eine Messung der heutigen Datenlage (alle 27 Katalogpreise 0), keine Struktureigenschaft: Trüge der Katalog einen Arbeitspreis > 0, fiele der Träger künftig aus der Fußzeile heraus, weil dann ein Preis gilt — Gleichlauf mit dem Rechner und die gewollte Richtung |
| [5] Alt/Neu-Kontrast | 0 → „—", gepflegter Preis bleibt | 1011/63 + 1018/63: ALT „0,0000" → NEU „—"; 1024/62: ALT „0,3500" → NEU „0,3500" |
| [6] `KostenEmissionRechner` 1026 | CO₂ 8,30 t/a unverändert | 8,30 t/a, CO₂ spez. 129,06 g/kWh, `CO2StrommixRueckfall = True`; NULL-Zeile in 1017 wirft nicht |
| [7] Bestand | nur die neuen Zeilen | 34 → 36 (zwei neue Zuordnungen); NULL-Zählung je Spalte 0 → 2; Gruppenbild der Preise sonst **identisch**; Zeile 10076 feldweise identisch |
| [8] Regressionsanker | Betrieb/Invest exakt | 1024 = 99,00; Invest 1018/1024/1042 = 45.312,50 / 12.001,00 / 13.000,00 |
| Sweep | kein `<<<<<<<` | kein Treffer |

## 5. Abweichung: der erwartete Umrechnungssatz 40 ist nicht erreichbar

Erwartet war für Erdgas E die Identitätsregel **40** (Nm³→Nm³). Gemessen wird **-1**, und
zwar richtigerweise: `Tab_Brennstoff_Stamm(3).Einheit` ist **„m³"**, `energy_conversion`
kennt für Brennstoff 3 nur `40 Nm³→Nm³`, `67 Nm³→kWh` und `70 m³→Nm³` — eine
Identitätsregel auf „m³" gibt es nicht. `energy_carrier(63).billing_unit` lautet dagegen
**„Nm³"**. Der Bruch verläuft also zwischen Brennstoff-Stamm und Umrechnungstabelle.

Erhebung über alle 25 Brennstoffe: **16 haben** eine Identitätsregel, **9 nicht** —
Stadtgas, Erdgas LL, Erdgas E, Biogas, Sonstige, Wasserstoff (alle „m³"), Flüssiggas
Propan/Butan („kg"), Holz („rm"). Das ist zugleich die Herkunft der einzigen `-1`-Waise im
Bestand (1039/63): Auch der Wizard-Weg schreibt für Erdgas `-1`.

Die Umsetzung folgt bewusst dem Code statt der Erwartung — **eine** Regel für beide
Schreibwege. Für die 16 auflösbaren Brennstoffe ist die Fehlvorwahl damit geheilt
(Probe [3b]), für die 9 übrigen bleibt es bei „keine Regel"; sichtbar unterscheidet sich
das nicht von der bisherigen 0, denn beide Werte treffen keine Zeile und alle Leser prüfen
`> 0`.

## 6. Offene Anwenderentscheide

1. **Bestandszeilen bleiben stehen.** Die 15 Zeilen mit Arbeits-/Grund-/Leistungspreis
   0/0/0 (21 mit Grundpreis 0, 31 mit Leistungspreis 0) und die eine `-1`-Waise
   1039/63 werden nicht geheilt. Ein Heilungsschritt wäre ein eigener Entscheid.
2. **Der Wizard-Weg schreibt weiter 0 statt NULL**, wenn der Brennstoff-Stamm keinen
   Preis führt (`TraegerSatzAnlegen` rundet die Stammwerte und schreibt sie unbesehen) —
   Bestandsverhalten aus BK2, in dieser Etappe bewusst nicht angefasst. Die beiden Wege
   legen damit unterschiedliche Zeilen an.
3. **Latente Grundpreis-Schattierung.** `custom_price_base = 0` schattet den Katalogwert
   auch weiterhin still. Solange alle 27 Katalogpreise 0 sind, ist das folgenlos; sobald
   Katalog-Grundpreise gepflegt werden, wird es sichtbar. Eine Umstellung wäre keine
   Anzeigefrage mehr, sondern eine Rechenänderung (`KostenEmissionRechner.cs:504`).
4. **Einheitenbruch „m³" / „Nm³"** (§ 5): Ob der Brennstoff-Stamm auf „Nm³" gezogen,
   eine Identitätsregel „m³→m³" ergänzt oder die Ableitung auf
   `energy_carrier.billing_unit` umgestellt wird, ist ein Datenmodell-Entscheid und
   berührt beide Schreibwege gleichermaßen.
5. **Das Rückschreiben des Kostendialogs verfestigt die Katalogrückfälle als
   Projektwerte** (Review-Befund zu BK3, **ernst zu nehmen**). Der
   Projekt-Settings-Upsert in `ucFuelSettings.SpeichereWerte` (`:1865-1905`) liegt
   **außerhalb** von `if (hasChanged)` (`:1803`) und schreibt beim Speichern **alle
   neun Spalten** aus den NumericUpDowns zurück — also genau die Werte, die LoadData
   soeben aus dem Katalog geholt hat. `Form_Kosten.OnFormClosing` (`:587-608`, Befund
   B6) ruft `SaveProjectAndHistory` für jede offene Trägerkarte **auch ohne
   Nutzeraktion**. Ein Blick in den Dialog plus Schließen friert damit Heizwert,
   Preise **und** Emissionen als Projektkopie ein — die Gegenrichtung zum
   Ä10/BK1-Versprechen „Katalogwahrheit". Neu ist der Weg nicht (BK1 öffnete ihn für
   die Wizard-Zeilen), aber für Ä10-Zeilen war er bisher ausgerechnet durch den
   `custom_hi = 0`-Fehler blockiert: Die kWh-Erreichbarkeitsprüfung lehnte das
   Speichern ab. BK3 räumt diese Blockade weg — der Weg steht jetzt in voller Breite
   offen. **Möglicher Fix als Entscheid:** `SpeichereWerte` schreibt je Feld `DBNull`,
   solange der Wert dem unveränderten Katalogrückfall entspricht. In BK3 bewusst
   **nicht** umgesetzt (Anwenderentscheid, und es wäre eine Verhaltensänderung des
   Speicherwegs, nicht der Zuordnung).

Harness (gitignored): `..\dev\bk3\` — Laufkopie je Start aus `…\scratchpad\bk3db`, die
Quelle bleibt unberührt.

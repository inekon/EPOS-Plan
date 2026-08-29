# Paket B2 — Kessel-Temperaturmodus und Booster-Lesepunkt: Umsetzungsprotokoll

Stand: 28.08.2026 · Branch `Pufferspeicher` · Bezug: die beiden **Nutzeraufträge vom
28.08.2026**, Vorleistung [`B1_Booster_Protokoll.md`](B1_Booster_Protokoll.md) (Tickets
**B1-O2** Lesezeitpunkt und **B1-O10** ungepflegte Kesseltemperaturen) sowie
[`P1_Schichtmodell_Protokoll.md`](P1_Schichtmodell_Protokoll.md) (Schichtschnittstelle).
Build `WP-Plan.sln` + `Referenzlauf.csproj` x64 Debug: **0 Fehler**, 5 Bestandswarnungen.
**Schema-Schritt 55** — `ZIEL_VERSION` 54 → **55**.

> **Beide offenen Rückfragen aus B1 sind beantwortet — und beide Antworten sind
> Anwenderentscheidungen, keine Herleitungen.** B1-O2 („liest der Booster vor oder nach der
> Ladephase?") wird zur **wählbaren Projekteinstellung** mit dem konservativen Wert als
> Vorbelegung. B1-O10 („kein einziger der 23 Kessel trägt ein Temperaturpaar") verschwindet
> ohne eine einzige Zeile Datenpflege: Das Bezugspaar kommt jetzt aus dem Lauf.

## 1. Die beiden Aufträge, wörtlich

1. **Kessel-Kaskade.** „Falls die Vorlauf- und die Rücklauftemperatur nicht gepflegt sind,
   gebe eine Warnung — aber nur wenn erforderlich. a) Die Vorlauf- und die Rücklauftemperatur
   sollen fest vorgegeben werden können und mit einer Default-Vorlauf- und Rücklauftemperatur
   gesetzt werden. b) oder entsprechend der berechneten Speichertemperatur (unter Beachtung
   Schichtspeicher) Verwendung finden können — im Falle berechnet ist die Vorgabe der Vor- und
   Rücklauftemperatur nicht erforderlich (keinen Hinweis geben)."
2. **Booster-Lesepunkt.** „Zu welchem Zeitpunkt innerhalb der Stunde liest der Booster die
   Quelltemperatur? Es soll eine Auswahl für den Nutzer möglich sein. Stelle ‚davor' als
   Default ein."

## 2. Bausteine

| Baustein | Inhalt | Stellen |
|---|---|---|
| **Schema-Schritt 55** | `Tab_Energieanlagen.WQ_TemperaturModus` TEXT(50), DML-Vorbelegung **`Berechnet`** für ALLE Bestandszeilen; `Tab_Einstellungen.Booster_Lesepunkt` TEXT(50) **angehängt** (Ordinalkette `row[0…22]`!), DML-Vorbelegung **`Davor`** über `WHERE … IS NULL OR Trim(…) = ''`. Vier Teile 55a–55d, Idempotenz über genau diese Bedingung | `SchemaMigration.Schritt_55_Temperaturbezug`, `SchemaKatalog.Schritt55_Temperaturmodus` / `SPALTE_BOOSTER_LESEPUNKT` |
| **Steuerwerte** | `DbWerte.WQ_TEMPMODUS_BERECHNET`/`_FEST`, `DbWerte.BOOSTER_LESEPUNKT_DAVOR`/`_DANACH` — deutsch und eingefroren, dazu die beiden toleranten Leser `TemperaturModusOderDefault` und `BoosterLesepunktOderDefault` (unbekannter Wert → Vorbelegung, nie Abbruch) | `Allgemein/DbWerte.cs` |
| **Bezugskette des Modus** | `KesselKopplungSetzen` + `BerechnetesBezugspaar` + `KesselTemperaturpaarGepflegt` — der GEKOPPELTE Fall bekommt einen eigenen Weg; der eigenständige Quellspeicher bleibt Zeichen für Zeichen der Bestandspfad | `SimulationControl.cs` |
| **Rückfallkonstante** | `KESSEL_VORLAUF_RUECKFALL` = 70 / `KESSEL_RUECKLAUF_RUECKFALL` = 50 — **eine** Stelle im Quelltext, benutzt von der dritten Stufe der Bezugskette und (als `VORSCHLAG_*`) vom Dialog | `SimulationControl.cs`, `Form_QuellePufferspeicher.cs` |
| **Lesepunkt-Weiche** | `Kaskadenkontext.BoosterLesepunkt` → `Kaskadenschleife._lesepunktDavor` (EINMAL je Lauf aufgelöst, nicht 8760-mal verglichen); zwei Leseorte in der Stundenschleife, von denen immer **genau einer** läuft | `SimulationKanaele.cs`, `Kaskadenschleife.cs` |
| **Alle-Ebenen-Abfrage** | `Quelltemperatur_Stunde(stunde, alleEbenen)` bei WP und Kessel. Der Modus `Davor` läuft am Stundenanfang und kennt die Rechenebene noch nicht; ein gekoppeltes Modul rechnet zwangsläufig auf einer Ebene > 0 und bliebe sonst ungelesen | `SimulationWaermepumpe.cs`, `SimulationSPK.cs` |
| **Einstellungszugriff** | `KonfigurationCtrl.BoosterLesepunktLesen`/`…Schreiben` — zielgenaues UPDATE, Nachreichung in `Insert` (Muster K2/Paket 8) | `KonfigurationCtrl.cs` |
| **Warnkriterium** | `Warnkriterien.KESSEL_TEMPERATURPAAR` (weich) + `KesselTemperaturpaarPruefen`; dazu im Projektbild `AnlagenTemperaturpaar` und der träge `TemperaturModus` | `Warnkriterien.cs` |
| **Kessel-Quellendialog** | Block „Temperaturbezug" mit zwei Auswahlknöpfen, zwei Eingabefeldern und Erklärzeile — programmatisch, nur im Kessel-Zweig, Vorschlag 70/50 beim Umschalten auf „fest" mit leeren Feldern | `Form_QuellePufferspeicher.cs` |
| **Lesepunkt-Schalter** | Fußzeilen-Checkbox neben „Extrapolation der WP-Kennlinie erlauben"; **nur sichtbar**, wenn `Warnkriterien.BoosterAnlagen` einen gekoppelten Booster meldet | `Form_Simulation_Config.Uebersicht.cs`, `.Karten.cs`, `.cs` |
| **Protokoll des Laufaufbaus** | Je gekoppeltem Kessel eine Zeile mit Modus, Herkunft und Bezugspaar; einmal je Lauf die Lesepunkt-Zeile (nur bei vorhandener Kopplung) | `SimulationControl.cs` |

**Neue Ressourcenschlüssel: 12**, 0 entfernt; Bestand danach **2633 `data`-Knoten** je `.resx`
(DE/EN deckungsgleich) und **2633 Designer-Eigenschaften**. Einzelnachweis in
[`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md), Abschnitt „Nachtrag Paket B2".

## 3. Die Bezugskette des Modus „Berechnet"

Der Modus steuert **allein das Bezugspaar** (VL/RL). Die Stundenabfrage der Quelltemperatur
selbst ist Zeichen für Zeichen die von B1/Q1 — `T_Quelle(h)` ist die Schichttemperatur des
Speichers an der Quell-Entnahmehöhe (`WQ_Anschlusshoehe`, Schritt 54):

```
Anteil(h) = ( T_Quelle(h) − RL ) / ( VL − RL ),   auf 0…1 geklemmt
```

**Modus `Berechnet`** (Vorbelegung) füllt VL/RL in dieser Reihenfolge:

| Stufe | Quelle | Begründung |
|---|---|---|
| **1** | `VL_eff` / `RL_eff` des **Rang-1-Senkenspeichers** des Kessels (Speicherinstanz des Laufs aus der Registry, nicht die Datenbankzeile) | Das ist die Temperatur, auf die der Kessel in **diesem** Lauf wirklich anheben muss. `VL_eff`/`RL_eff` tragen die Rückfall-ΔT-Regel aus Konzept 7.2 bereits in sich; die Rohspalten tun das nicht. **Schichtspeicher eingeschlossen:** Bei geschichtetem Senkenspeicher bleibt `VL_eff` die Obergrenze — keine Schicht trägt je mehr, die Schichtung ändert nur, *wo* die Wärme liegt. |
| **2** | die **gepflegte Kette** Anlage (`Tab_Energieanlagen.Vorlauf`/`[Rücklauf]`) → `Tab_Heizkessel` über `ID_Kessel` | Die W3-Kette aus Paket B1. Wer sie gepflegt hat, soll sie auch im Berechnet-Modus wirksam sehen. |
| **3** | **70 / 50 °C** (`SimulationControl.KESSEL_VORLAUF_RUECKFALL`) | Auslegung eines konventionellen Heizkesselsystems und dieselbe Zahl, die der Dialog beim Umschalten auf „fest" vorschlägt. Ein Wert wird hier **gesetzt** statt der Quellbezug abgeschaltet: Wer „berechnet" gewählt (oder geerbt) hat, hat ausdrücklich gesagt, dass er keine Vorgabe machen will. |

**Modus `Fest`** nimmt ausschließlich Stufe 2. Fehlt sie, meldet der Warnkatalog
`KESSEL_TEMPERATURPAAR` **einmal**, und der Lauf rechnet sichtbar auf dem Berechnet-Weg
weiter. Der Senkenpuffer gehört bewusst **nicht** zur Fest-Kette: Er ist ein abgeleiteter
Wert, keine Anwendervorgabe.

**In der Bestands-Datenbank greift Stufe 3 oder Stufe 1 — nie eine Pflegepflicht.** Genau
das ist die Auflösung von B1-O10: Bis B2 war der Quellbezug an einem Kessel ohne
Temperaturpaar **stumm wirkungslos** (Meldung „Temperaturpaar für den Hub nicht bestimmbar",
Anteil dauerhaft 0). Schritt 55 belegt alle 132 Anlagenzeilen mit `Berechnet` vor; damit
rechnet die Kessel-Kaskade ab sofort, ohne dass jemand 23 Katalogzeilen pflegt.

## 4. Der Lesepunkt — wo genau gelesen wird

Es gibt **genau einen Leseort je Modus**; der Steuerwert entscheidet nur, welcher es ist.
Eine zweite Abfrage innerhalb der Stunde wäre nicht reproduzierbar spezifiziert (der SOC
ändert sich zwischen den Phasen mehrfach) — dieselbe Begründung wie in B1, Abschnitt 3.

| Modus | Ort in `Kaskadenschleife.Rechnen` | Was der Booster sieht |
|---|---|---|
| **`Davor`** (Vorbelegung) | unmittelbar **vor Phase A**, nach `StundeBeginnen` und der Regeneration | Den Speicherzustand am **Ende der Vorstunde**. Die Regeneration trifft ausschließlich EIGENSTÄNDIGE Quellspeicher (`IstQuelle`), ein geteilter Puffer ist zu diesem Zeitpunkt unberührt — der gelesene Wert ist deshalb exakt der Zustand nach Phase G der Vorstunde. Zugleich ist der Ort *vor Phase B der ersten Rechenebene*; beide Beschreibungen treffen dieselbe Zeile. |
| **`Danach`** | je Rechenebene nach `ModulEbeneSetzen(ebene)`, vor deren Phase B | Den Speicher **nach den Ladephasen aller Vorebenen** dieser Stunde, also im am weitesten geladenen Zustand — das Verhalten von Paket B1. |

`Davor` ist die **konservative** Aussage: Was ein vorgelagerter Erzeuger erst in dieser Stunde
nachlädt, wird dem Booster nicht gutgeschrieben.

**Warum vor und nicht nach Phase A.** „Nach Phase A" wäre ebenfalls „vor Phase B der ersten
Ebene", aber es wäre kein Zustand, den man benennen kann: Phase A entlädt kanalweise und in
Entladereihenfolge, der gelesene Wert hinge damit an der Entladeordnung des Projekts. „Ende
der Vorstunde" ist dagegen ein Zustand, den auch die Ergebnisreihe `PUFFER_*_SOC` zeigt.

## 5. Verifikation

### 5.1 Build

`WP-Plan.sln` und `Referenzlauf.csproj`, Debug × x64: **0 Fehler**, **5 Warnungen** — die
Bestandswarnungen CS0108/CS0109/CS1998 außerhalb dieses Pakets. Gebaut mit
`-p:OutDir=<dev\b2_sln|b2_neu>`, weil die Anwendung des Anwenders während der Arbeit lief und
`bin\` gesperrt war.

### 5.2 Referenzmenge — und ein Befund, der VOR diesem Paket liegt

> **Die Basis `2026-08-28_E2` ist nicht mehr aktuell.** Ein Kontrolllauf mit **unverändertem
> Code** (Stand `249907e`, vor dem ersten B2-Eingriff) gegen die Basis ergab **321 von 332 CSV
> byte-gleich** — die elf abweichenden Dateien liegen **sämtlich in Projekt 1042**. Ursache
> ist eine **Datenänderung des Anwenders** zwischen dem Einfrieren der Basis (Datenstand
> 28.08. 09:05) und dem Beginn dieses Pakets (Datenstand 28.08. 12:36): 1042 ist neu
> verschaltet (Anlagen-IDs 14806/14807 → 14817/14818), und die Booster-Wärmepumpe steht jetzt
> auf **„Quelle unbegrenzt verfügbar"** mit konstant 45 °C. Damit führt **kein einziges** der
> dreizehn Referenzprojekte mehr einen gekoppelten Booster.

Der Byte-Nachweis für B2 läuft deshalb als **A/B-Vergleich auf EINER festen Datenbankkopie**
(Schemastand 55, aus der produktiven Datei migriert) statt gegen die veraltete Basis — das ist
zugleich der schärfere Nachweis, weil er von der Datenlage des Anwenders unabhängig ist:

```
A = Code vor B2  (Build 28.08. 14:26)   \  dieselbe Kopie, dieselben 13 Projekte,
B = Code mit B2  (Abschlussstand)       /  Modus "projekt" je Projekt

MD5-Vergleich A gegen B : 332 von 332 CSV byte-gleich,
                          0 Abweichungen, 0 fehlende, 0 zusätzliche Dateien
```

Der Vergleich wurde **dreimal** gefahren — nach dem Engine-Teil, nach der
Warnungs-Entdopplung und mit dem Abschlussstand auf einer frisch migrierten Kopie —, jedes
Mal mit demselben Ergebnis.

Damit ist belegt: **B2 ändert an der gesamten Referenzmenge keinen einzigen Wert.** Der
`Davor`-Default kann dort nichts bewirken, weil dort nichts gekoppelt ist; der
Berechnet-Modus kann nichts bewirken, weil kein Kessel der Menge einen Quellpuffer führt.

Der Vollständigkeit halber, gegen die alte Basis: `2026-08-28_E2` gegen den B2-Endstand ergibt
**dieselben 321/332** und **exakt dieselben elf Dateien** wie der Kontrolllauf mit
unverändertem Code — die Abweichung ist vollständig der Datenänderung zugeordnet und
enthält keinen Anteil aus diesem Paket.

Der Kontrolllauf-Ordner `Referenzlaeufe/2026-08-28_B2_VORLAUF` ist nach der Auswertung
**gelöscht**. **Die Basis `2026-08-28_E2` ist neu zu setzen** — nicht wegen B2, sondern wegen
der Datenänderung an 1042 (Ticket B2-O1).

### 5.3 Wirkprobe Booster — `Davor` gegen `Danach` (Projekt 1042, Wegwerf-Kopie)

Weil die Referenzmenge keinen Booster mehr trägt, wurde die Verschaltung auf der Wegwerf-Kopie
**rekonstruiert**: WP 14818 („CS7800iLW 16", Sole-Wasser, Kennlinie −5 … 25 °C) bezieht aus
dem geteilten Puffer 1054198 („Puffer 3000Ltr (2)"), den die Luft-WP 14817 auf Rang 3 lädt;
`WQ_Unbegrenzt` zurückgenommen, Temperaturband des Puffers 5 … 25 °C (innerhalb der Kennlinie,
damit der COP der Quelltemperatur folgt statt gekappt zu werden), Ladeleistung begrenzt.

| Ladeleistung | Modus | WP-Produktion [kWh] | WP-Strom [kWh] | JAZ | Booster-Modul: Strom / Betriebsstunden |
|---|---|---|---|---|---|
| 1,5 kW | **`Danach`** (B1) | 58 223,3993 | 21 172,2409 | **2,7500** | 1,51 MWh / 547,46 h |
| 1,5 kW | **`Davor`** (B2) | 58 219,8907 | **21 178,4694** | **2,7490** | 1,52 MWh / 547,20 h |
| | *Differenz* | −3,51 | **+6,23 (+0,029 %)** | −0,0010 | |
| 3,0 kW | **`Danach`** (B1) | 58 805,3554 | 21 276,8627 | **2,7638** | 1,42 MWh / 304,13 h |
| 3,0 kW | **`Davor`** (B2) | 58 798,9157 | **21 281,3267** | **2,7629** | 1,43 MWh / 305,02 h |
| | *Differenz* | −6,44 | **+4,46 (+0,021 %)** | −0,0009 | |

Ablesbar und in beiden Runden gleichgerichtet: **`Davor` kostet Strom.** Der Booster sieht den
kälteren Speicher, arbeitet mit niedrigerem COP, läuft länger und deckt geringfügig weniger —
genau die konservative Richtung, die der Nutzerauftrag wollte. Die **Größe** des Effekts hängt
an der Konfiguration: Sie ist null, wenn der Puffer in beiden Lesepunkten an der
Abschaltschwelle steht (reichliche Ladeleistung, Wirkprobe A1/A2 aus B1), und sie wächst mit
dem Anteil, den der Booster an der Gesamtdeckung trägt (hier 6,0 von 58,2 MWh).

**Gegenprobe — `Danach` ist Zeichen für Zeichen der B1-Stand:** derselbe Lauf auf derselben
Kopie, einmal mit dem Code **vor** B2 und einmal mit B2 im Modus `Danach`:

```
Altcode (B1) gegen B2/"Danach" : 34 von 34 CSV byte-gleich
Altcode (B1) gegen B2/"Davor"  : 23 von 34 byte-gleich, 11 abweichend
```

Der Wechsel des Standardwerts ist damit die **einzige** Verhaltensänderung am Booster; wer den
B1-Stand braucht, bekommt ihn über den Fußzeilenschalter exakt zurück.

### 5.4 Wirkprobe Kessel — die drei Modi (Projekt 1023, Wegwerf-Kopie)

Konstellation nach Konzept 8.4 und wie in B1, Abschnitt 4.5: Puffer **1018023**
(„Vitocell 140-E 600 Ltr", 65/45, Ladeleistung 6 kW) wird von den Wärmepumpen 11203/11204
geladen; der Heizkessel **11205** („ecoVIT VKK 186/5") bekommt ihn als Wärmequelle. Der Kessel
selbst trägt — wie alle 23 Kessel des Bestands — **kein** Temperaturpaar.

| Runde | Modus / Datenlage | Bezugspaar RL/VL | Herkunft laut Protokoll | Quellwärme | Kessel-Wärme | Gasverbrauch | Nutzungsgrad | Warnungen |
|---|---|---|---|---|---|---|---|---|
| **K0** | keine Quelle (Ausgangslage) | — | — | **0** MWh | 66,49 | 78,52 | 84,69 % | 0 |
| **K-B** | `Berechnet`, kein Paar, Kessel-Senke = Heizkreis | 50/70 | **Rückfall 70/50 °C** (Stufe 3) | **12,21** MWh | 112,09 | 128,85 | **87,00 %** | **0** |
| **K-F0** | `Fest`, kein Paar | 50/70 | Rückfall 70/50 °C (Rückfall auf den Berechnet-Weg) | 12,21 MWh | 112,09 | 128,85 | 87,00 % | **1** |
| **K-F70** | `Fest`, Anlage 70/50 (Dialogvorschlag) | 50/70 | **feste Vorgabe** | 12,21 MWh | 112,09 | 128,85 | 87,00 % | **0** |
| **K-F65** | `Fest`, Anlage 65/45 (gepflegt) | 45/65 | feste Vorgabe | **15,47** MWh | 109,85 | 126,26 | 87,00 % | **0** |
| **K-BS** | `Berechnet`, Kessel lädt Puffer 1018024 (75/55) auf Rang 1 | 55/75 | **Rang-1-Senkenspeicher 1018024** (Stufe 1) | **3,26** MWh | 96,43 | 110,33 | **87,40 %** | **0** |
| **K-FS** | `Fest` ohne Paar, gleiche Senkenlage | 55/75 | Rang-1-Senkenspeicher (Rückfall) | 3,26 MWh | 96,43 | 110,33 | 87,40 % | **1** |

Ablesbar:

- **Die Kessel-Kaskade rechnet ohne jede Datenpflege.** K0 → K-B: Aus 0 kWh Quellwärme werden
  12,21 MWh, der Jahresnutzungsgrad steigt von 84,69 % auf 87,00 %. Vor B2 war K-B der Fall,
  in dem die Engine „Temperaturpaar für den Hub ist nicht bestimmbar" meldete und **nichts**
  rechnete.
- **Der Modus wirkt und ist zuordenbar.** Die Quellwärme reicht von 3,26 bis 15,47 MWh, je
  nachdem, gegen welches Paar der Anteil gebildet wird — jede Runde nennt ihre Herkunft im
  Protokoll.
- **K-F70 ist zahlengleich mit K-B.** Das ist die Probe auf die Rückfallkonstante: Der
  dokumentierte Rückfall des Berechnet-Wegs ist **exakt** 70/50 °C und damit dieselbe Zahl,
  die der Dialog vorschlägt.
- **K-F0 ist zahlengleich mit K-B** — der Rückfall im Fest-Modus ist wirklich der
  Berechnet-Weg und keine dritte Rechnung.

### 5.5 Warnungs-Matrix

Gemessen wurde je Runde die Zahl der Protokollzeilen mit dem Kriteriumstext:

| Konstellation | erwartete Warnungen | gemessen |
|---|---|---|
| `Berechnet`, kein Paar (K-B) | 0 — Nutzerauftrag: „keinen Hinweis geben" | **0** |
| `Berechnet` mit Rang-1-Senkenspeicher (K-BS) | 0 | **0** |
| `Fest` mit Paar 70/50 (K-F70) | 0 | **0** |
| `Fest` mit Paar 65/45 (K-F65) | 0 | **0** |
| `Fest` ohne Paar (K-F0) | genau 1 | **1** |
| `Fest` ohne Paar, andere Senkenlage (K-FS) | genau 1 | **1** |

**Genau einmal, nicht zweimal.** Ein erster Stand meldete den Befund doppelt — einmal aus dem
Warnkriterienkatalog am Laufstart und einmal aus der Engine. Die Engine-Warnung ist entfallen;
sie war derselbe Wortlaut aus zwei Quellen und damit genau der Doppelbefund, den Paket S2 für
Kurzschluss und Ring ausdrücklich vermeidet. Was der Lauf beisteuert, ist die **Folge**, und
die steht in der Bezugspaar-Zeile („das Paar fehlt, es gilt der Berechnet-Weg").

### 5.6 Sprachgleichheitsprobe

Derselbe Lauf mit `EPOS_REFLAUF_UICULTURE=en-US` auf der Kessel-Wirkprobe (dem Fall, der das
neue Kriterium **und** den neuen Modus auslöst): **25 von 25 CSV byte-gleich**. Kein
Anzeigetext dieses Pakets dient als Steuerwert.

### 5.7 Dialoge — D-Check-Messlauf

Der D-Check-Harnisch wurde um drei Fälle erweitert (Quellendialog in der WP-Fassung, in der
Kessel-Fassung und in der Kessel-Fassung mit „fest vorgegeben") und gegen die Wegwerf-Kopie
gemessen, in beiden Fenstergrößen:

| Fall | ClientSize | Bewertung |
|---|---|---|
| `Form_QuellePufferspeicher_WP` | **620 × 566** | **Pixel für Pixel der Stand vor B2** (D3-Messlauf: 620 × 566). Die Wärmepumpe sieht von B2 nichts. |
| `Form_QuellePufferspeicher_Kessel` | 620 × 675 | +109 px für den Temperaturbezug-Block |
| `…_Kessel_fest` | 620 × 675 | gleich groß — die beiden Eingabefelder stehen in der bereits vorhandenen Zeile |

Befundprofil **unverändert gegenüber dem D3-Messlauf**: 27 Befunde, Klasse d = 6, Klasse e
(Fremdschriften) = 21 — sämtlich Bestand und keiner am Quellendialog.

> **Ein echter Fehler ist dabei gefunden und behoben worden.** Der erste Stand rechnete die
> Blockhöhe mit einer ausgeschriebenen Zahl (68 px) hoch. Sie war um 26 px zu klein: Die
> Fußknöpfe rückten weniger weit nach unten, als der Block hoch ist, und schnitten die
> Hinweiszeile an (Überlappung 110 × 20 px an `_btnOk` und `_btnAbbruch`, in beiden
> Fenstergrößen). Das ist dieselbe Falle wie die vier Selbstkorrekturen des alten Layouts
> (Befund N13a). Die Einpassung **misst** das Wachstum jetzt
> (`_lblTbHinweis.Bottom − _lblAnschlusshoeheHinweis.Bottom`), statt es zu behaupten.

### 5.8 Migration — Idempotenz und Marker-Rücksetzprobe

| Probe | Ergebnis |
|---|---|
| **Erstlauf** auf frischer Kopie der produktiven Datei | Schemastand 54 → **55**; **132** Anlagenzeilen auf `WQ_TemperaturModus = 'Berechnet'`, **25** Projekteinstellungen auf `Booster_Lesepunkt = 'Davor'` (Datenstand 14:45; im Abschlusslauf um 15:1x waren es **138** bzw. **26** — der Anwender hat währenddessen weitergearbeitet) |
| **Doppellauf** auf derselben Kopie | Schritt 55 „**bereits erledigt**", Schemastand bleibt 55 |
| **Marker-Rücksetzprobe** (`SchemaVersion` 55 → 54) | Schritt 55 läuft erneut: „**0 Spalten angelegt, 1 bereits vorhanden**", **0** Anlagenzeilen, **0** Projekteinstellungen vorbelegt; Schemastand wieder 55 |

Das DML greift ausschließlich auf noch nicht belegte Zeilen (`IS NULL OR Trim(…) = ''`) —
darauf ruht die Idempotenz unabhängig vom Marker.

### 5.9 Umgang mit den Daten

Alle Wirkproben liefen auf **Wegwerf-Kopien außerhalb des Repos** (`dev\b2_db`, `dev\b2_probe`,
`dev\b2_idem`), angelegt über `Referenzlauf.exe migration` aus der produktiven Datei.

**Dieses Paket hat die produktive `Kenndaten.accdb` nur GELESEN.** Jeder Zugriff lief über
`Referenzlauf.exe migration <quelle> <zielordner>` (kopiert und schreibt ausschließlich das
Ziel); die Läufe selbst bekamen ausdrücklich einen Kopie-Ordner und prüfen ihn hart
(`DbUmgebung.AufArbeitskopieUmschaltenUndPruefen`), der D-Check-Harnisch bricht auf dem
produktiven Pfad ab.

**Die Datei hat sich trotzdem verändert — durch die Anwendung des Anwenders.** Zeitschiene:

| Zeit | Ereignis | MD5 |
|---|---|---|
| 28.08. 12:36:09 | Stand bei Arbeitsbeginn | `CD03076D…` |
| 28.08. 14:29:18 | der Anwender startet EPOS-Plan (PID 65168) | — |
| 28.08. 14:45:37 | **letzter lesender Zugriff dieses Pakets** (Kopie nach `dev\b2_db`) | unverändert |
| 28.08. 14:57:09 | Schreibzugriff der laufenden Anwendung | `5DB74F29…` |
| 28.08. ~15:1x | zweite Kopie für den Abschlusslauf (**138** statt 132 Anlagenzeilen) | — |

Größe unverändert 151 949 312 Bytes. Ab 14:29 gingen alle Verifikationsbuilds über
`-p:OutDir=` an dem gesperrten `bin\` vorbei.

## 6. Was NICHT geändert wurde

- **Die Stundenabfrage der Quelltemperatur.** `SimulationPufferspeicher.QuellEntnahmeTemperatur`
  und die Schichthöhe (`WQ_Anschlusshoehe`) sind unangetastet. B2 tauscht beim Kessel die
  Herkunft des BEZUGSPAARS und beim Booster den ZEITPUNKT — nicht die Formel.
- **Die Mengenrechnung.** Weder `MaxAbgabe`/`QuellwaermeHolen` beim Kessel noch die
  Verdampferrechnung der Wärmepumpe ist angefasst.
- **Der eigenständige Quellspeicher.** Der statische Pfad in `KesselQuellbezugSetzen`
  (Erdsonden-Ersatz mit `WQ_Spreizung`) ist unverändert; er läuft nach wie vor über
  `KesselTemperaturpaar` (Tab_Heizkessel → Senkenpuffer).
- **Die Rechenebenen.** Wer nach wem rechnet, entscheidet unverändert D5a.
- **Die Wärmepumpen-Fassung des Quellendialogs** (5.7).

## 7. Offene Punkte / Notizen

| # | Punkt | Ziel |
|---|---|---|
| **B2-O1** | **Die Basis `2026-08-28_E2` ist neu zu setzen** — nicht wegen B2 (A/B 332/332 byte-gleich), sondern wegen der Datenänderung des Anwenders an Projekt 1042 zwischen 09:05 und 12:36 (5.2). Solange sie steht, meldet jeder Referenzlauf elf Abweichungen, die niemandem zuzuordnen sind | **Orchestrator** |
| **B2-O2** | **Kein Referenzprojekt trägt mehr einen gekoppelten Booster** (1042 steht auf „Quelle unbegrenzt verfügbar", 45 °C). Damit sichert kein Referenzlauf die B1/B2-Rechnung ab — nur die Wirkproben dieses Protokolls. Das ist die Fortschreibung von B1-O8: Empfehlung, die Booster-Kette in 1042 wieder scharf zu schalten (WQ_Unbegrenzt aus, Temperaturpaar am Puffer 1054198) und die Basis danach neu zu setzen | Anwender / Orchestrator |
| **B2-O3** | Der Referenzlauf exportiert die Quelltemperatur-Ganglinie eines gekoppelten Moduls weiterhin nicht als eigene CSV (B1-O3); die Wirkproben messen deshalb über Modul- und Vektorsummen. Aufnahme nur mit einem Basiswechsel | Basiswechsel |
| **B2-O4** | Die beiden neuen Protokollzeilen des Laufaufbaus sind **inline deutsch** wie ihr Nachbarbestand (B1-O9 / P1-O7 / S2-O9). Beim Sammelschnitt in Paket L ist zu beachten, dass die Modusangabe ein Persistenzwert ist und in einem lokalisierten Text nicht als Platzhalter auftauchen darf | Paket L |
| **B2-O5** | `Warnkriterien.KESSEL_TEMPERATURPAAR` prüft den Quellpuffer über `Projektbild.Quellpuffer` und damit auch dann, wenn der Puffer **nicht geteilt** ist (eigenständiger Quellspeicher). Dort braucht der statische Pfad das Paar ebenfalls, die Meldung ist also zutreffend — aber ihr Text nennt den Rückfall auf „berechnet", den es dort nicht gibt. Betrifft eine Konstellation, die der Bestand nicht führt | Nacharbeit |
| **B2-O6** | Der Fußzeilenschalter erscheint **nur** bei vorhandenem Booster. Wer den Lesepunkt vorab setzen will (bevor die Kopplung konfiguriert ist), kann das über die Oberfläche nicht — die Einstellung steht dann auf der Vorbelegung `Davor`. Bewusst so: ein Schalter für eine Konstellation, die es nicht gibt, wäre eine Zusage ohne Wirkung | bewusst |
| **B2-O7** | Der Temperaturbezug ist **je Anlage** gespeichert, der Lesepunkt **je Projekt**. Das ist die richtige Aufteilung (das eine ist eine Anlagenvorgabe, das andere eine Rechenkonvention), aber es bedeutet: Zwei Kessel desselben Projekts können verschiedene Modi haben, zwei Booster desselben Projekts nicht verschiedene Lesepunkte | bewusst |

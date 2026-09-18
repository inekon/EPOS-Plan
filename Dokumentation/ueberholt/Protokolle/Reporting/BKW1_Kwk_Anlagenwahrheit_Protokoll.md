# BKW1 — Der KWK-Zuschlag gehört der Anlage

Protokoll der Etappe BK1 des Wirtschaftlichkeitskonzepts (Anwenderwunsch 17.09.2026,
Anwenderentscheid `BK-E-1` (a) vom 18.09.2026), Schemaschritt 89.

> Der Dateiname trägt `BKW1`, weil `BK1_Traegerzuordnung_Protokoll.md` in diesem Ordner schon
> vergeben ist — dort geht es um die Zuordnung der Energieträger, hier um den KWK-Zuschlag.

---

## 1 Der Anlass

Der Anwender am 17.09.2026: „Werte unter ‚Angaben der gewählten Anlage' sollen … direkt mittels
Button an der Stelle des Wertes übernommen werden können (mit Hinweis auf die Grundlage) … Der
Teil ‚KWK-Zuschlag (Projektvorgabe)' entfällt."

Dahinter steckt mehr als eine Bedienfrage. Der KWK-Zuschlag hatte **zwei Wahrheiten**:

| Ort | Spalten | Schemaschritt |
|---|---|---|
| Anlage (`Tab_Energieanlagen`) | `KWKG_Stichtag`, `KWKG_Inbetriebnahme`, `KWKG_Anlagenart`, `KWKG_Eigenstromfall`, `KWKG_Satz_Einspeisung`, `KWKG_Satz_Eigen`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel` | 22 |
| Projekt (`Tab_ProjektWirtschaftlichkeit`) | `KWKG_Bonus`, `KWKG_Bonus_Einspeisung`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel`, `KWKG_Tatbestand`, `KWKG_Anlagenart`, `KWKG_Kostenanteil`, `KWKG_Abschlag_Negativ`, `KWKG_Pauschalmodus`, `KWKG_Stichtag`, `KWKG_Inbetriebnahme` | 28 |

Dazwischen lag eine **Rückfallkette**: Was an der Anlage leer war, holte der Rechenweg aus dem
Projekt. Drei Folgen:

1. **Das Gesetz kennt die Projektgrößen nicht.** § 7 KWKG bemisst den Satz an der Leistung der
   *einzelnen* Anlage (marginale Tranchen), § 8 das Kontingent an *ihrer* Anlagenart und *ihrem*
   Kostenanteil. Eine Kaskade aus einem neuen 9-kW-Modul und einem modernisierten 50-kW-Modul war
   nicht abbildbar — der Projektwert traf immer nur eines von beiden.
2. **Der Anwender pflegte Felder, deren Wirkung anderswo hing.** Ob „Vbh-Kontingent gesamt" wirkt,
   entschied sich daran, ob das Kontingentfeld der Anlage leer war.
3. **Der Aktivierungsschalter stand sechsmal da** — `p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0`
   im Rechenkern, im Word-Baustein, im Excel-Erzeuger, in der Nachweiszeile, in der
   Kapitalwert-Verlaufshülle und in der Wirtschaftlichkeitsseite. Sechs Kopien einer Regel sind
   sechs Orte, an denen sie auseinanderlaufen kann.

## 2 Der Entscheid

`BK-E-1` (a) vom 18.09.2026: **Die Wahrheit ist die Anlage.** Vorschlagsknöpfe am Feld, Gruppe 2
auf die wirklich projektweiten Angaben eingedampft, Rückfallkette aufgegeben, ein
Aktivierungsschalter statt sechs Kopien.

## 3 Schemaschritt 89 — Spalte und Datenschritt

**DDL:** `Tab_Energieanlagen.KWKG_Kostenanteil` (DOUBLE, nullbar) — die letzte Angabe, die § 8 für
die Kontingentstufe braucht und die bis dahin nur am Projekt stand. Die Anlagenart hing schon seit
Schritt 22 an der Anlage, hatte dort aber keine Rechenwirkung: Die Stufe entsteht erst aus dem
Paar.

**DML — und das ist neu.** Schritt 22 und Schritt 88 schrieben keinen Wert; dieser Schritt trägt
**neun Anweisungen** (`EPOS.Kern/Allgemein/Update/KwkAnlagenwahrheit.cs`). Jede schreibt einen
Projektwert in die BHKW-Anlagenzeilen, die an der betreffenden Stelle leer sind:

| Anlage | ← Projekt |
|---|---|
| `KWKG_Satz_Eigen` | `KWKG_Bonus` |
| `KWKG_Satz_Einspeisung` | `KWKG_Bonus_Einspeisung` |
| `KWKG_Vbh_Kontingent` | `KWKG_Vbh_Kontingent` |
| `KWKG_Vbh_Jahresdeckel` | `KWKG_Vbh_Jahresdeckel` |
| `KWKG_Kostenanteil` | `KWKG_Kostenanteil` |
| `KWKG_Anlagenart` | `KWKG_Anlagenart` |
| `KWKG_Eigenstromfall` | `KWKG_Tatbestand` |
| `KWKG_Stichtag` | `KWKG_Stichtag` |
| `KWKG_Inbetriebnahme` | `KWKG_Inbetriebnahme` |

**0 und leer sind dasselbe** — die Nullsemantik des Dialogs zieht mit: Die Zielbedingung fasst
`IS NULL OR = 0` (Zahlen) bzw. `IS NULL OR = ''` (Text), die Quellbedingung überspringt eine
Quelle, die selbst leer ist. Bei den beiden Datumsspalten ist nur `NULL` leer.

**Idempotent:** Nach dem ersten Lauf ist die Zielzelle gefüllt oder die Quelle war leer; der
zweite Lauf trifft keine Zeile. Gemessen am Werkzeug `Werkzeuge/Testdatenbankschema`: erster Lauf
2/2/2/0/0/0/0/2/2 Zeilen, zweiter Lauf neunmal 0.

**Drei Leser einer Quelle:** `SchemaMigration.Schritt_89_KwkAnlagenwahrheit` (Access-Zweig),
`Werkzeuge/Testdatenbankschema` und der Nachweis in `EPOS.Kern.Tests`.

## 4 Der Rechenweg ohne Rückfall

`WirtschaftlichkeitCtrl.ReiheJeAnlage` rechnet ab hier ausschließlich mit Anlagenwerten:

```
Satz_Einspeisung(A) = A.SatzEinspCt ?? 0
Satz_Eigen(A)       = A.SatzEigenCt ?? 0,  geprüft gegen A.Eigenfall
Kontingent(A)       = A.VbhKontingent > 0 ? dieser : KwkgKontingentRechner.Ableiten(A.Anlagenart, A.Kostenanteil, Jahr)
Deckel(A)           = A.VbhDeckel > 0 ? dieser : Staffel § 8 Abs. 4
```

**Die Strenge des Eigenstromsatzes ist mitgewandert — und dabei milder geworden.** Bis BK1 galt an
der Anlage: gepflegter Satz ohne Tatbestand ⇒ 0 mit Meldung. Diese Strenge stand ausdrücklich auf
der Annahme, ein Anlagensatz sei eine **ausdrückliche Eingabe**, die es im Bestand nirgends gibt
(geprüft: alle Anlagenzeilen NULL). Mit Schritt 89 ist die Annahme hinfällig — jede Bestandsanlage
bekommt einen Satz, den niemand an ihr eingegeben hat. Hätte die Strenge Bestand, nähme der
Schritt jedem Bestandsprojekt ohne gepflegten Tatbestand den Eigenverbrauchszuschlag; das ist eine
Rechenwirkung, die nirgends entschieden wurde. Es gilt deshalb je Anlage genau die Regel, die K6
am Projekt eingeführt hat: **leerer Tatbestand ⇒ Satz bleibt stehen, Meldung „ungeprüft"; nur die
ausdrückliche Wahl `KEINER` nimmt ihn weg.**

**`KwkgAktivierung` ist die eine Regel.** `SatzGefuehrt(eigen, einsp)` sagt, was ein geführter Satz
ist; `IstAktiv(idProjekt)` und `IstAktiv(idStamm, versionen)` wenden sie auf `Tab_Energieanlagen`
an — Muster und Toleranz wortgleich zu `KostenEmissionRechner.StromLeistungspreisGepflegt`. Alle
sechs Stellen holen sie von dort.

## 5 Die Maske

**Gruppe 1b — der Knopf steht am Feld.** Unter Satz Einspeisung, Satz Eigenstrom und
Vbh-Kontingent steht je eine `Vorschlagszeile` (neuer Baustein): links die Grundlage im Klartext
(Tranchen, Norm, Jahr; beim Kontingent Stufe, Schwelle, Norm), rechts der Knopf „Vorschlag
übernehmen". Er schreibt **nur sein eigenes Feld** und nur in den Arbeitsstand — der alte
Sammelknopf schrieb zwei Felder auf einmal, und wer ihn drückte, sah erst danach, was er getroffen
hatte. Neu dazu: das Feld „Anteil Neuherstellungskosten [%]" der Anlage.

**Gesperrt heißt begründet, und zwar weich.** Fehlt eine Grundlage, trägt der Knopf
`aria-disabled="true"` statt `disabled` (Hausregel `EPOS.UI`: ein `disabled`-Knopf zeigt seinen
`title` nie) und nennt den Grund: keine elektrische Nennleistung, kein Tatbestand nach § 6 Abs. 3,
keine Anlagenart. Die Stilregel `.epos-knopf[aria-disabled="true"]` stand schon — **keine neue
CSS-Regel**.

**Gruppe 2 heißt „Projektweite KWK-Angaben"** und führt sechs Felder: Einspeisevergütung KWK-Strom
(aus Auftrag #325, keine Zuschlagsgröße), Abschlag Negativstunden, Anteil Neuherstellungskosten
(Vorgabe für Anlagen ohne eigenen Wert), Pauschale § 9, Stichtag § 6 und **Förderbeginn (Startjahr
der Reihen)** — so heißt `KWKG_Inbetriebnahme` jetzt, weil er alle jahresscharfen Reihen startet,
auch ohne BHKW. Herausgenommen: Bonus Eigenstrom, Bonus Einspeisung, Vbh-Deckel-Override,
Vbh-Kontingent gesamt, Eigenstrom-Tatbestand, Anlagenart § 8. Eine leise Zeile sagt, wo sie jetzt
stehen — sonst sucht ein Anwender, der den Dialog kennt, die Sätze.

**Warum die Einspeisevergütung und der Stichtag geblieben sind.** Beide stehen in keiner der beiden
Listen des Auftrags. Der Einspeisesatz ist mit #325 gerade erst hierhergezogen und gehört zur
Erlösseite, die BK1 nicht anfasst; der Stichtag des § 6 trägt die Realisierungsfrist des Projekts
für Anlagen ohne eigenes Datum. Ohne sie gäbe es für beide Werte keinen Pflegeweg mehr.

## 6 Der Nachweis der Ergebnisgleichheit

Gemessen wurde mit einem Prüfstand, der Projekt für Projekt `WirtschaftlichkeitCtrl.Berechne` auf
einer Gruppe aus einem Projekt ruft — flache Stundenreihen aus den Jahressummen des gebuchten
Laufs, Szenario Erwartet, Kultur `de-DE`.

| Projekt | Stand B7 (mit Rückfall) | BK1 + Datenschritt | BK1 **ohne** Datenschritt |
|---|---|---|---|
| 1017 | kein Kapitalwert, KWKG 0,00 | gleich | gleich |
| 1018 | kein Kapitalwert, KWKG 0,00 | gleich | gleich |
| 1024 | −2.896.359,134805 €, KWKG 0,00 | gleich | gleich |
| **1030** | **−21.895.377,275113 €, KWKG 7.315,956634 €/a** | **gleich** | −21.954.815,753214 €, KWKG **0,00** |
| 1031 | kein Kapitalwert, KWKG 0,00 | gleich | gleich |

Die dritte Spalte ist die **Gegenprobe**: Ohne den Datenschritt fällt der Zuschlag von 1030 auf 0,
weil die Anlagen keinen Satz führen — damit ist belegt, dass die Gleichheit in Spalte 2 aus den
nachgetragenen Anlagenwerten kommt und nicht aus einem verbliebenen Rückfall.

**1030 ist der einzige messbare Fall.** Nur dieses Projekt der Testdatenbank führt KWKG-Vorgaben
(Bonus 4,00 / Einspeisung 8,00 ct/kWh, Kontingent 30.000 Vbh, Stichtag 01.09.2026, Inbetriebnahme
01.03.2027), und keine einzige Anlagenzeile trug vorher eine eigene Angabe. 1018 und 1024 sind die
im Konzept genannten Anker; sie rechnen ohne KWKG und sind deshalb trivial gleich.

**Referenzlauf:** 1030, 1007, 1017, 1045, 1046 **5/5 PASS** gegen
`2026-09-16_R8_Heizkessel_Kaskade`. Die Basis führt keine Geldgröße — der KWK-Zuschlag erscheint
dort ohnehin nicht.

## 7 Prüffälle und Gegenproben

**Neu in `EPOS.Kern.Tests/KwkAnlagenwahrheitTests`** (16 Fälle): Schemaschritt und die neun
Wertepaare · die Spalte in der Testdatenbank · die Projektvorgaben stehen an den Anlagen · der
Datenschritt wiederholt sich folgenlos · nur BHKW-Zeilen tragen KWKG-Angaben · die Regel auf zwei
Zahlen (fünf Fälle) · **der Schalter fragt die Anlagen und nicht das Projekt** · die Gruppenfassung
· der Zuschlag bleibt nach dem Datenschritt derselbe · **ohne Anlagensätze gibt es trotz
Projektsätzen keinen Zuschlag** · zwei Anlagen mit verschiedenem Kostenanteil bekommen verschiedene
Kontingente · der Kostenanteil je Anlage übersteht Speichern und Laden.

**Neu in `EPOS.UI.Tests/Dialoge/BhkwWirtschaftlichkeitDialogTests`**: drei Vorschlagsknöpfe mit
ihrer Grundlage · der Knopf am Einspeisesatz trifft nur sein Feld · zwei Anlagen verschiedener
Leistungsklasse bekommen verschiedene Sätze (40 kW → 8,00; 300 kW → 1670/300 = 5,5667 ct/kWh) ·
das Kontingent aus Anlagenart und Kostenanteil (30 % → 15.000, 60 % → 30.000 Vbh) · ein gesperrter
Knopf nennt seinen Grund · die BK1-Texte in beiden Sprachen · Gruppe 2 führt genau die sechs
projektweiten Angaben und keine Satzfelder mehr.

**Gegenproben** — gefahren und im Prüfstand geblieben:

| Gegenprobe | Erwartung | Ergebnis |
|---|---|---|
| Anlagensätze leeren, Projektsätze stehen lassen | Schalter inaktiv, Zuschlag 0 | rot ohne die Umstellung, grün mit ihr — bleibt als Prüffall |
| Datenschritt nicht ziehen | Zuschlag 1030 = 0 statt 7.315,96 € | gemessen (Tabelle § 6, Spalte 3) |
| Kontingent ohne Anlagenart | Knopf gesperrt, Feld unverändert | bleibt als Prüffall |

## 8 Was die Etappe offen lässt

- **BK1-1:** Sechs Projektspalten bleiben ungelesen stehen (`KWKG_Bonus`,
  `KWKG_Bonus_Einspeisung`, `KWKG_Tatbestand`, `KWKG_Anlagenart` ganz;
  `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel` bis auf den Ersatzweg). Ein Drop ist ein eigener
  Schemaschritt und braucht einen Anwenderentscheid.
- **BK1-2:** Der projektweite Ersatzweg rechnet weiter mit den Projektsätzen. Er greift nur, wenn
  sich Anlagen- und Ergebniszeilen nicht zuordnen lassen; der Aktivierungsschalter fragt aber schon
  die Anlagen. Der Fall ist konstruiert, bleibt aber benannt.
- **BK1-3:** Eine **Jahr-0-Zeile der KWKG-Pauschale in der Erlösrubrik** ist als Vorschlag
  aufgenommen und **nicht gebaut** — der Entscheid steht beim Anwender aus. Sie löste zugleich den
  offenen Punkt `B7-3`.

## 9 Was nicht angefasst wurde

Erlösrubrik (B7), Stromsteuer-Modus (B6), Energiesteuer- und Hilfsstromgruppen des Dialogs, die
iOS-Schale (`KwkgAnlagenCtrl.Speichere(g, true)` schreibt die zwölfte Spalte von selbst — keine
Signatur hat sich geändert), der Diagramm-Renderer, die Referenzbasis.

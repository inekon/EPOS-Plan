# Klarnamenschutz für Ablehnungsgründe — Fix zu H8-Befund 4

**Stand:** 29.08.2026
**Grundlage:** [`H8_ProjektAktiv_Protokoll.md`](H8_ProjektAktiv_Protokoll.md), § 9 Befund 4:
abgelehnte Aufrufe können Klarnamen an das Modell tragen.
**Ausgangsstand:** sauberer Arbeitsbaum, HEAD `68c0d95` (Branch `Pufferspeicher`).

---

## 1. Das Leck

Zwei Schwächen wirkten zusammen (Fachkonzept 4.2, Platzhalter-Datenschutzschicht):

1. **`KiRueckmeldung.Abgelehnt(name, grund)` säuberte den Grund gar nicht** — der Text ging
   wörtlich in das `functionResponse`-JSON (`KiKern\KiRueckmeldung.cs:300` alt).
2. **`KiRueckmeldung.Erzeuge` kann in einem SATZ nur ersetzen, was die Tabelle schon kennt**
   (`Saeubern`). Scheiterte die Vorbedingung `KiHilfe.ProjektMussAufloesbarSein`, stand im
   Grund über `KiHilfe.Aufzaehlen` die Kandidatenliste — bis zu **zwölf Projektnamen samt
   Kunde** — und bei leerer Tabelle ging sie ungeschützt an das Modell.

Der Prüflauf (§ 5, Block 4) zeigt den Altzustand wörtlich: `Projekt „Gibtsnichtxyz" gibt es
nicht. Zur Auswahl steht: Beispiel WP WG 1, …, BHKW Test München (123324), …` — echte
Projektnamen und Kundenfelder.

## 2. Entwurfsentscheid

Der Auftrag ließ zwei Wege zu: Ablehnungsgründe durch dieselbe Platzhalter-Ersetzung wie
Ergebniszeilen, **oder** eine Aufzählung ohne Klarnamen (nur Platzhalter/Anzahl). Umgesetzt ist
der erste Weg, weil er die Arbeitsteilung des Fachkonzepts erhält: **der Anwender sieht unter
dem Schritt weiterhin die Klartextkandidaten** („Meinten Sie …"), das Modell rechnet mit
`Name n` und kann sich in der Korrekturrunde wörtlich darauf beziehen; die Anzeige löst über
die Tabelle zurück auf (H8-Mechanik). Eine Aufzählung ohne Namen hätte beiden Seiten die
Kandidaten genommen.

Da sich aus einem Satz nicht ablesen lässt, welcher Teil ein Bezeichner ist, gilt die
Aufgabenteilung:

* **Anmelden** (wissen, was ein Name ist): Anwendungsprojekt, wo die Projektliste liegt.
* **Ersetzen** (Satz säubern): unverändert allein in `KiKern\KiRueckmeldung`.

## 3. Änderungen

| Fundstelle | Änderung |
|---|---|
| `KiKern\KiRueckmeldung.cs` `Abgelehnt(...)` | dritter, optionaler Parameter `KiPlatzhalter platzhalter = null`; der Grund läuft durch `Saeubern` — dieselbe Regel wie der Ergebnissatz in `Erzeuge`. Beide Bestandsaufrufe ohne Tabelle verhalten sich unverändert. |
| `Allgemein\KI\Aktionen\KiAktionen.cs` `KiHilfe.KlarnamenAnmelden(platzhalter, params texte)` | neu: meldet jeden Projekt- **und Kundennamen** (`ProjektKandidaten`, Name + Zusatz), der in einem der Texte vorkommt, per `KiPlatzhalter.Fuer` an. Angemeldet wird NUR, was vorkommt (sonst wüchse die Tabelle je Fehlschlag um alle Projekte, Obergrenze `MaxEintraege`); Namen unter **drei Zeichen** bleiben außen vor (ein Projekt „A" würde als Ersetzungsmuster jedes Wort zerschneiden). |
| `Allgemein\KI\KiChatService.cs` (drei Modellwege) | `Abgelehnt(…, platzhalter)` bei ungültigem Befund und beim Riegel; vor `Erzeuge` eines **gescheiterten** Laufs (`!ergebnis.Erfolg`) werden `ergebnis.Text` und `ergebnis.Meldungen` durch `KiHilfe.KlarnamenAnmelden` geschickt. `schritt.Grund` (Anzeige) bleibt unberührt. |

Damit ist auch der Weg über die Vorbedingung abgedeckt: `KiAusfuehrer` baut daraus
`KiErgebnis.Abgelehnt(grund)`, und der Chatdienst meldet die Namen vor dem Verdichten an.
Erfolgreiche Läufe bleiben beim Bestandsmuster („erst die Zeilen, dann der Satz") — dort
entsteht der Platzhalter in der Ergebniszeile.

**Grenzen (bewusst):** Namen unter drei Zeichen werden nicht ersetzt; ist die Tabelle voll
(`MaxEintraege` = 500), legt `Fuer` keinen neuen Eintrag an (Bestandsverhalten). Der
Werkzeugknopf („Werkzeuge…") sendet nichts an das Modell und braucht die Schicht nicht.

## 4. Kodierung

Alle vier berührten Codedateien strikt gelesen und unverändert zurückgeschrieben:
`KiRueckmeldung.cs`, `KiRueckmeldungTests.cs`, `KiAktionen.cs` UTF-8 **+BOM** CRLF;
`KiChatService.cs` UTF-8 **ohne** BOM CRLF. Das CP1252-Rezept kam nicht zum Einsatz
(keine der Dateien ist CP1252); `git diff` führt ausschließlich die gewollten Zeilen.

## 5. Beweise

**KiKern.Tests** (`dotnet test KiKern.Tests -p:ArtifactsPath=C:/Temp/kibart`): **449 bestanden**,
darunter neu `AbgelehntErsetztBekannteKlarnamenImGrund`,
`AbgelehntKannNurBekannteBezeichnerSchuetzen` (dokumentiert die Grenze der Kernschicht) und
`AbgelehntOhneTabelleWieBisher`.

**Wegwerf-Harnisch `..\..\..\dev\klarnamenprobe\`** (Muster `dev\h8probe`: Wegwerf-Kopie der
Datenbank, `Settings.DBPath` umgebogen, Werkzeugrunde über den eingespeisten
`KiChatService.Modellkanal`, kein Aufruf bei Google; App-Build x64 nach
`dev\build_klarnamen` per `-p:OutDir`). Gesät werden zwei Probeprojekte
`Klarnamenprobe Alpha Nordwerk (Vertraulich Alpha KG)` / `… Beta Suedwerk (Vertraulich Beta KG)`.
Ergebnis **ALLES GRUEN**, vier Blöcke:

1. **Kernschicht:** `Abgelehnt(aktion, grund, platzhalter)` liefert
   `"grund":"„Klarnamenprobe" ist mehrdeutig: Name 1 (Name 2)."` — kein Klarname, Fachsprache
   und Suchtext stehen.
2. **Anmeldeschicht:** `KlarnamenAnmelden` findet alle vier gesäten Bezeichner im Text
   (Tabelle = 4); ein Text ohne Klarnamen lässt die Tabelle **leer**.
3. **Ende zu Ende, mehrdeutig** (`projekt_lesen` mit Suchtext „Klarnamenprobe", Vorbedingung
   scheitert): der Anzeige-Grund unter dem Schritt nennt beide Projekte **und** Kunden im
   Klartext; in **keiner** gesendeten Runde steht einer der vier Namen; die Rückmeldung trägt
   `"status":"abgelehnt"` und Platzhalter; die Tabelle löst zurück auf. Tageszähler unverändert.
4. **Vollaufzählung, unbekannter Name** gegen die **echten** Projektdaten der Kopie: alle
   aufgezählten Namen der ersten zwölf Kandidaten samt Kundenfeld (15 geprüft, 0 unter
   Mindestlänge) sind ersetzt — an das Modell geht
   `Zur Auswahl steht: Name 1, Name 2, …, Name 5 (Name 6), Name 7 (Name 6), …`; derselbe Kunde
   bekommt denselben Platzhalter (Eineindeutigkeit), der Suchtext „Gibtsnichtxyz" bleibt für
   die Korrekturrunde stehen.

## 6. Offene Prüfpunkte für die Abnahme (nur am laufenden Programm)

1. Chatfenster mit Aktionsbetrieb, Frage mit **mehrdeutigem** Projektnamen (z. B. gemeinsamer
   Namensanfang zweier Projekte) → unter dem Schritt erscheint die Kandidatenliste im Klartext.
2. **„Was wird gesendet?"** unmittelbar danach → in der Vorschau stehen an ihrer Stelle
   `Name n`-Platzhalter, keine Projekt- oder Kundennamen.
3. Antwortet der Assistent mit einem Kandidaten („Meinten Sie …?"), zeigt der Chat den
   Klarnamen (Anzeige-Rückeinsetzung aus H8).

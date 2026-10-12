# GD-1 — Kopf des Gebäudedialogs zweigeteilt, Herkunft je Feld (12.10.2026)

Welle GD-1 aus dem [Entwurf GD](../../../aktuell/Gebaeudesimulation/2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md)
(Abschnitte 2.1, 4.1, 4.2, 7). Die Fragen F1 bis F8 hat der Anwender am 12.10.2026 nach Empfehlung entschieden; GD-1 setzt
F1 (Felder ohne Wirkung behalten und kennzeichnen), F2 (Raumhöhe und Luftwechsel bei Zonen ausblenden bzw. „Vorgabe für
n Zonen“) und F6 (nur ausblenden, der Rechenweg bleibt) um.

## Umsetzung

- **Kern-Auskunft** `EPOS.Kern/Allgemein/Simulation/Gebaeude/Gebaeudefeldherkunft.cs`: je Gebäudefeld Herkunft (Datei,
  Vorgabe, von Hand, geerbt, wirkungslos), Anzeige (Feld, Rückfall, je Zone) und die Zahl der Zonen, Fenster oder Gruppen,
  in denen der Wert wirkt. Sie **ruft** je Zone `Zonenvorgaben.Bilden` (Raumhöhe erbt, wenn Volumen „abgeleitet“ und Höhe
  „Gebäude“) und `Gebaeudemodellvorgaben.WirksamerLuftwechsel` mit Infiltration und Nutzerlüftung aus derselben Kaskade —
  derselbe Weg wie `GebaeudeModellEingang.Zonenwerte`. Rückfälle des Bauteilwegs: g für Fenster ohne eigenes g; Bauweise je
  Zone für die Außengruppe mit opaken Bauteilen ohne Aufbau und die Innengruppe ohne Aufbau (gezählt nach Aufbau; ob ein
  Aufbau Masse trägt, entscheidet der Lauf). Auf dem Tagesbilanz-Weg wirken alle Rechengrößen unmittelbar.
- **Dialog** `GebaeudeKatalogDialog.razor`: Block „Beschreibung und Einordnung“ (Name, Beschreibung, Gebäudeart,
  Baualtersklasse, Baujahr, Verwendung, Energiestandard) mit der Marke „wirkungslos“ und dem Hinweis „Bericht, Vorgaben des
  Imports, Pufferauslegung; der Rechenweg liest sie nicht“; Block „Rechengrößen“ (Bauart, Nutzfläche, Fläche/Nutzer,
  Fensterdurchlassgrad, Raumhöhe, Luftwechselrate) mit je einer Marke samt Herleitung. Die Auskunft fragt
  `GebaeudeArbeitsstand.Feldherkunft()`; die Herleitung des Luftwechsels auf Gebäudeebene steht nur ohne Zonen.
- **Herkunft „Datei“:** Die Herkunft der Gebäudefelder ist nicht gespeichert (Entwurf 5.1, Schemaschritt erst in GD-2).
  `GebaeudeArbeitsstand.FelderAusDatei` nimmt sie auf; bis GD-2 bleibt die Menge leer.
- **Nebenwunsch Rundung:** In der Vorgabe-Matrix der Konditionierung zeigen die Nennwerte von Geräten und Personen (W)
  keine Nachkommastellen (`KonditionierungMatrix.Stellen`, Zahlenfeld ohne Tausendertrennung, weil der Parser den Punkt
  als Dezimalzeichen liest). Die Zeile der Jahresmittel samt „Interne Wärmegewinne (Gesamtangabe)“ steht in Watt ohne
  Nachkommastellen mit Tausenderpunkt (`Zahlen.Watt`). Gespeichert wird der Wert unverändert.
- **Texte:** 16 neue Schlüssel `GEBK_*` in beiden Sprachen, `Resource.Designer.cs` neu erzeugt; sieben KI-Erklärungen
  (`KI_DLG_GEBK_*_ERL`: Gebäudeart, Baualtersklasse, Verwendung, Raumhöhe, Luftwechsel, Durchlassgrad, Bauart) um die
  Wirkung ergänzt. Neue Eingabefelder gibt es nicht, die Feldkarte in `KiDialoge` bleibt.
- **Stilblatt:** `.epos-gebk-marke` mit fünf Ausprägungen in den Farben der Importherkunft.

## Feldkarte vorher/nachher

| Feld | vorher | nachher |
|---|---|---|
| Gebäudeart, Baualtersklasse, Baujahr, Verwendung, Energiestandard | Block „Kenngrößen“ | Block „Beschreibung und Einordnung“, Marke „wirkungslos“ |
| Bauart, Nutzfläche, Fläche/Nutzer | „Kenngrößen“ | „Rechengrößen“, Marke |
| Fensterdurchlassgrad, Bauart mit Zonen | Feld ohne Hinweis | Feld, „Rückfall für n Fenster/Gruppen“ bzw. „wirkt derzeit nicht“ |
| Raumhöhe mit Zonen | Feld | Feld „Vorgabe für n von m Zonen“, oder Text „gilt je Zone“ |
| Luftwechselrate mit Zonen | Feld | Feld „Vorgabe für n von m Zonen ohne Lüftungswerte“, oder Text „je Zone“ |

## Nachweis

- Kern-Tests `GebaeudefeldherkunftTests` (12), bunit `GebaeudeKopfHerkunftTests` (4: Feldbestand einzonig, mehrzonig mit
  eigenen Werten, mehrzonig erbend, Rundung der Anzeige).
- Gefilterte Läufe und Referenzlauf: siehe Bericht der Welle; der Rechenweg ist unberührt.

# Messreihen für die Validierung des Zapfprofilgenerators

Dieser Ordner nimmt die **gemessenen** Trinkwarmwasser-Reihen freigegebener Objekte auf, mit denen
[`Werkzeuge/ZapfprofilValidierung`](../../Werkzeuge/ZapfprofilValidierung/LIESMICH.md) den
Zapfprofilgenerator nachrechnet (Umsetzungskonzept Zapfprofilgenerator, Kapitel 7 Stufe Z5, offener
Punkt **K5**).

**Nichts davon wird versioniert.** Der Ordner ist in `.gitignore` bis auf dieses `LIESMICH.md`
ausgenommen — genau wie [`../Normzahlen/`](../Normzahlen/LIESMICH.md). Gemessene Reihen sind
Objektdaten; das Konzept sagt dazu „Objektdaten nie im Repositorium" (Kapitel 9 K5). Ins Repositorium
kommt allein der **Bericht** des Werkzeugs, und der trägt nur Verhältniszahlen, Anteile und
Zählungen. Die Wache `EPOS.Kern.Tests/RepositoryOrdnungWacheTests` prüft die Regel und meldet jede
Datei, die hier doch im Index landet.

## Was hierher gehört

Je Messobjekt **ein Unterordner** mit zwei Dateien:

```
Referenzlaeufe/Messreihen_INEKON/
  MFH-01/
    objekt.json        Kennung, Nutzungsart, Bezugsmenge, Kalender, Bilanzgrenze, Stochastik
    messreihe.csv      Zeitstempel;Wert (kWh)   -- oder (m³) / (kW)
  MFH-02/
  NWG-01/
```

Format, Felder und Grenzen stehen im
[`LIESMICH.md des Werkzeugs`](../../Werkzeuge/ZapfprofilValidierung/LIESMICH.md), Abschnitt 3; ein
vollständig kommentiertes Beispiel liegt unter
[`Werkzeuge/ZapfprofilValidierung/Beispiel/BSP-WOHNEN-01/objekt.json`](../../Werkzeuge/ZapfprofilValidierung/Beispiel/BSP-WOHNEN-01/objekt.json).
Das Wichtigste in Kurzform:

* **Ordnername und `kennung` sind anonym** — `MFH-01`, nicht der Objekt- oder Kundenname. Das
  Werkzeug kann eine Kennung nicht von einem Namen unterscheiden; die Anonymisierung ist Sache des
  Anwenders.
* Raster **höchstens eine Stunde**, sonst tragen Band und Formabgleich nichts.
* Länge mindestens **30 Tage**, empfohlen **ein Messjahr**; ein Teiljahr wird benannt hochgerechnet.
* **Lücken unter 5 %**; darüber lehnt der Leser die Datei ab.
* **UTF-8**, Trenner `;`, Einheit in Klammern im Kopf der Wertspalte.
* Die **Bilanzgrenze des Zählers** muss stimmen: `Zapfstelle` (nur die Zapfung) oder `MitVerteilung`
  (Zapfung und Zirkulation) — sonst vergleicht das Werkzeug Ungleiches.

## Der Lauf

```
dotnet run --project Werkzeuge/ZapfprofilValidierung -c Release -- \
    Referenzlaeufe/Messreihen_INEKON --ziel <berichtordner ausserhalb>
```

Ohne Angabe nimmt das Werkzeug `Referenzlaeufe/Kenndaten_Test.sqlite` als Katalogquelle; für eine
Validierung gegen den Auslieferungskatalog `--katalog <Kenndaten.sqlite>` setzen.

Solange dieser Ordner kein Objekt führt, **schweigt** der Fall
`ZapfprofilValidierung.Tests/MessreihenordnerTests` — in der CI also immer.

## Offen lizenzierte Fremddaten

Frei verfügbare Messreihen (CC BY) gehören **nicht** hierher, sondern in einen Ordner außerhalb des
Repositoriums; Umsetzer und Quellenangaben stehen in
[`Werkzeuge/ZapfprofilValidierung/Konverter/LIESMICH.md`](../../Werkzeuge/ZapfprofilValidierung/Konverter/LIESMICH.md).
Auch von ihnen kommt nur der Bericht ins Repositorium — mit Namensnennung der Quelle.

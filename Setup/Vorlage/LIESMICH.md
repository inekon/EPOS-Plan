# `Setup/Vorlage/` — die Auslieferungsdatenbank

Hier liegt die Datenbank, die das Setup mitliefert: **`Kenndaten.sqlite`** — die bereinigte
Vorlage mit den Auslieferungskatalogen und den Beispielprojekten, aus der die Anwendung beim
Erststart die Arbeitsdatenbank des Kontos anlegt.

> **Die Datenbank wird nie eingecheckt.** `Kenndaten.sqlite`, ihre Beidateien und der
> Prüfbericht stehen in [`.gitignore`](../../.gitignore). Versioniert sind nur diese
> Liesmich-Datei, die von Hand gepflegte Lizenzhinweisseite und die IDS-Datei der Exportzusage. Der Grund: Die Vorlage entsteht
> aus der **produktiven** Entwicklungsdatenbank, die reale Kunden- und Objektdaten führt — sie in
> ein Repository zu legen wäre der Umweg, auf dem genau diese Daten doch wieder herauskommen.

---

## Was hier liegt

| Datei | Herkunft | Versioniert |
|---|---|---|
| `LIESMICH.md` | von Hand | **ja** |
| `Lizenzhinweise.txt` | von Hand — je ausgelieferter Fremdbibliothek Name, Fassung, Lizenz, Copyright-Vermerk und Quelltextverweis (E27, U10); das Setup legt sie nach `{app}`, der Wächter `EPOS.Kern.Tests/LizenzhinweiseWacheTests.cs` hält sie gegen `Directory.Packages.props` | **ja** |
| `EPOS_Export.ids` | von Hand — die Exportzusage des IFC-Exports (IDS 1.0, buildingSMART): je Entität die Attribute, Eigenschaften und Mengen, die eine EPOS-Datei sicher trägt, ohne Geometrie; sie reist mit der Auslieferung nach `{app}\Vorlage\`, nicht als zweite Datei je Export (Datenaustauschkonzept 6.4). Der Wächter `EPOS.Kern.Tests/IdsWacheTests.cs` hält sie gegen eine frisch geschriebene Datei; die Exportbilanz nennt sie im Beipackzettel | **ja** |
| `Kenndaten.sqlite` | erzeugt von `Werkzeuge/Auslieferungsvorlage` | nein |
| `Kenndaten.sqlite.bericht.txt` | erzeugt im selben Lauf — der Prüfbericht | nein |
| `Katalogpaket.json` | erzeugt im selben Lauf — das Katalogpaket der Fassung (Konzept Setup 6.5); `build-setup.ps1` löscht es vor jedem Lauf und prüft danach, dass es die Fassung des Laufs trägt | nein |

Die frühere `Kenndaten.accdb` gibt es hier nicht mehr: Access wurde beim Kunden nie produktiv
eingesetzt (Anwenderrahmen 09.09.2026), und mit Entscheid **#157‑E‑1** (Weg **W3**) ist die
Vorlage eine `.sqlite`-Datei; der Access-Weg fällt.

---

## Wie sie entsteht

```bash
dotnet run --project Werkzeuge/Auslieferungsvorlage -c Release -- \
    "C:\ProgramData\EPOS_PLAN\Kenndaten.sqlite" \
    "Setup\Vorlage\Kenndaten.sqlite" \
    --beispiele Beispiele\pakete --kataloge alle
```

`Setup/build-setup.ps1` ruft das Werkzeug ebenso auf und gibt dazu die **Fassung des
Katalogpakets** als `--katalogfassung` mit. Sie zählt als `JJJJMMTTnn` (Datum und Tageslauf,
z. B. `2026100601`) und kommt aus `-Katalogfassung <n>` oder der Umgebungsvariablen
`EPOS_KATALOGFASSUNG`. Ohne Angabe schlägt das Skript den nächsten Wert aus dem
Freigaberegister [`Setup/Katalogfassungen.txt`](../Katalogfassungen.txt) vor. Eine Fassung muss
größer sein als jede eingetragene und als die heutige Datumsfassung `JJJJMMTT`, sonst bricht das
Skript ab. Nach einem vollständig erfolgreichen Lauf trägt es Fassung, Datum und Programmversion
ins Register ein; mit `-Probe` (so läuft die CI) und im Trockenlauf `-VorlageNurPruefen` hält es
die Fassung nicht gegen das Register und schreibt nichts hinein. Die eingetragene Zeile wird nur
auf Auftrag committet.

Rückgabe `0` heißt: erzeugt **und** abgenommen. Jeder andere Wert ist ein Abbruch mit Grund auf
`stderr`, und dann liegt hier **keine** Datei — ein halber Auslieferungsstand soll beim nächsten
Setup-Lauf nicht als gültig durchgehen.

| Code | Bedeutung |
|---|---|
| 0 | Vorlage erzeugt und abgenommen |
| 2 | Aufruf falsch oder Quelle nicht lesbar |
| 3 | Ziel liegt im Repository außerhalb von `Setup/Vorlage/` |
| 4 | Die `ReadOnly`-Regel würde eine Katalogtabelle leeren |
| 5 | Fachlicher Abbruch (Beispielimport oder Abnahme rot) |
| 1 | Unerwarteter Fehler |

Was das Werkzeug tut, warum, und was `--kataloge alle` mit dem offenen Befund zur Marke
`ReadOnly` zu tun hat, steht in
[`Dokumentation/aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md`](../../Dokumentation/aktuell/Konzept_Setup_InnoSetup_EPOS-Plan.md) § 6.1.

---

## Vor jeder Auslieferung

Den **Prüfbericht lesen**, nicht nur die Rückgabe. Er steht als `Kenndaten.sqlite.bericht.txt`
daneben und nennt je Tabelle die Zeilen vorher und nachher, die Projektliste, den Schemastand,
`integrity_check`, `foreign_key_check` und den Datenschutzwächter. Drei Zeilen entscheiden:

1. **Projekte in der Vorlage** — dort dürfen nur die Beispiele stehen, kein Kundenname.
2. **Datenschutzwächter** — „keine Zeile in einer der 47 Projekttabellen außerhalb der
   Beispiele", keine Pfadangabe (`C:\Users\…`), keine Lizenz-/KI-Tabelle.
3. **Katalogzahlen** — plausibel, und keine `*_STAMM`-Tabelle unerwartet leer.

Dazu zwei Punkte zum Katalogpaket:

4. **Gesperrte Sätze der Quelle** — vor jeder Auslieferungsvorlage in der produktiven Quelle
   prüfen, dass Heizkessel (`Tab_Heizkessel_STAMM`) und PV (`Tab_PV_STAMM`) ihre ausgelieferten
   Sätze mit `ReadOnly = 1` führen. Nur gesperrte Sätze stehen im Paket und werden beim Anwender
   nachgeführt; ungesperrte verhalten sich dort wie eigene Sätze.
5. **Fassung des Katalogpakets** — der Lauf ohne `-Probe` hat die Fassung in
   `Setup/Katalogfassungen.txt` eingetragen; die Zeile gehört mit der Freigabe ins Repository.

Steht im Bericht eine Zeile mit `WARNUNG` oder `FEHLER`, wird sie erklärt, bevor die Datei in
ein Setup wandert.

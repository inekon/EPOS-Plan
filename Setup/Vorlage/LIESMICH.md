# `Setup/Vorlage/` — die Auslieferungsdatenbank

Hier liegt die Datenbank, die das Setup mitliefert: **`Kenndaten.sqlite`** — die bereinigte
Vorlage mit den Auslieferungskatalogen und den Beispielprojekten, aus der die Anwendung beim
Erststart die Arbeitsdatenbank des Kontos anlegt.

> **Nichts davon wird eingecheckt.** `Kenndaten.sqlite`, ihre Beidateien und der Prüfbericht
> stehen in [`.gitignore`](../.gitignore). Diese Liesmich-Datei ist der einzige versionierte
> Inhalt des Ordners. Der Grund: Die Vorlage entsteht aus der **produktiven**
> Entwicklungsdatenbank, die reale Kunden- und Objektdaten führt — sie in ein Repository zu
> legen wäre der Umweg, auf dem genau diese Daten doch wieder herauskommen.

---

## Was hier liegt

| Datei | Herkunft | Versioniert |
|---|---|---|
| `LIESMICH.md` | von Hand | **ja** |
| `Kenndaten.sqlite` | erzeugt von `Werkzeuge/Auslieferungsvorlage` | nein |
| `Kenndaten.sqlite.bericht.txt` | erzeugt im selben Lauf — der Prüfbericht | nein |

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
[`Setup/Konzept_Setup_InnoSetup_EPOS-Plan.md`](Konzept_Setup_InnoSetup_EPOS-Plan.md) § 6.1.

---

## Vor jeder Auslieferung

Den **Prüfbericht lesen**, nicht nur die Rückgabe. Er steht als `Kenndaten.sqlite.bericht.txt`
daneben und nennt je Tabelle die Zeilen vorher und nachher, die Projektliste, den Schemastand,
`integrity_check`, `foreign_key_check` und den Datenschutzwächter. Drei Zeilen entscheiden:

1. **Projekte in der Vorlage** — dort dürfen nur die Beispiele stehen, kein Kundenname.
2. **Datenschutzwächter** — „keine Zeile in einer der 47 Projekttabellen außerhalb der
   Beispiele", keine Pfadangabe (`C:\Users\…`), keine Lizenz-/KI-Tabelle.
3. **Katalogzahlen** — plausibel, und keine `*_STAMM`-Tabelle unerwartet leer.

Steht im Bericht eine Zeile mit `WARNUNG` oder `FEHLER`, wird sie erklärt, bevor die Datei in
ein Setup wandert.

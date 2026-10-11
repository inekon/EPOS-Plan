# Protokoll AS2b: Nachzug Quick Assist — Fernwartung über das Windows-Bordmittel

**Auftrag des Anwenders (11.10.2026, abends):** „nehme Quick Assist als Standardweg“. Konzept:
[`Konzept_Administration_Support_EPOS-Plan.md`](../../../aktuell/Konzept_Administration_Support_EPOS-Plan.md),
Entwicklerseite:
[`Entwicklerhandbuch_Administration_Support_EPOS-Plan.md`](../../../aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.md).
Statuszeile #995 (vorläufige Nummer). Kein Schemaschritt, kein Rechenweg. iOS bleibt zurückgestellt.

## 1 Anlass

Der Anwender bemerkte beim Kostenvergleich, dass RustDesk Pro in den Standardplänen jedes Gerät
zählt, das sich je mit dem Server verbunden hat, auch Quick-Support-Sitzungen. Mit Server,
Pro-Tarif, gestaltetem Client und Codesignatur wäre die Fernwartung laufender Betrieb (Konzept 4.8
des Standes AS2).

## 2 Recherche und Entscheid

Quick Assist ist in Windows 10 und 11 enthalten, von Microsoft signiert, ohne Lizenz und ohne
Server; der Helfer braucht ein Microsoft-Konto, der Anwender einen sechsstelligen Code (zehn
Minuten gültig). Entscheid (zweiter Entscheid zu AS-Q7): Quick Assist ist der Standardweg;
RustDesk quelloffen (eigener Server, Client mit Serverangabe) bleibt als Reserve beschrieben für
Kunden, bei denen Quick Assist gesperrt ist, ohne Betrieb bis zum Bedarf. AS-Q9: (c) bleibt, der
Dialog startet zusätzlich Quick Assist; die Fernwartungsseite wird zur Anleitung ohne Download. Das
iPad bleibt zurückgestellt (kein Quick Assist dort).

## 3 Zuschnitt

- Code-Welle (parallel): Zone 3 des Fernwartungsdialogs mit Erklärtext, drei Schritten, Knopf
  „Quick Assist starten“ (Start aus der Windows-Schale, Store-App, Rückfall auf `quickassist.exe`,
  Hinweis bei Fehlen), Fundstelle `WindowsFormsApplication1/Allgemein/Support/QuickAssistStart.cs`;
  Commit `⟨COMMIT-QA⟩`.
- Papier-Welle (diese): Konzept 4.1 bis 4.4, 4.7, 4.8, 7 (AS-Q7, AS-Q9), 8, Stand-Block;
  Handbuch 4 (auch 4.6 Betrieb), 6, 7; Wiki-Quelle `Programm Dokumentation - Fernwartung.wiki`;
  Status #995; dieses Protokoll; `Dokumentation/LIESMICH.md`. „Installation und Update“ und „Über
  EPOS-Plan“ blieben unverändert (keine Sätze zum alten Weg).

## 4 Gate

⟨GATE⟩

## 5 Offene Punkte

- Sichtabnahme des Anwenders unter Windows (Knopf startet Quick Assist, Hinweis bei Fehlen).
- Microsoft-Konto für den Support einrichten (Anwender).
- Fernwartungsseite als Anleitung anlegen (Anwender).
- Wiki-Upload gebündelt mit dem Logbuch-Satz; Versionsnummer beim Anwender erfragen.
- Word-Fassung des Entwicklerhandbuchs neu erzeugen; Push-SHA und CI-Kennung nachtragen.

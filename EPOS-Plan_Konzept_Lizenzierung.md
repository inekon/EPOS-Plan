# Konzept: Zeitlich beschränkte Lizenzierung für EPOS-Plan

**Stand:** 1. August 2026 · **Projekt:** EPOS-Plan (Windows-Desktopanwendung, C#/.NET WinForms)
**Modell:** Testversion + zeitlich befristete Voll-Lizenz · Firmenlizenz mit maximaler Benutzeranzahl, benutzergebunden · Online-Aktivierung mit Offline-Betrieb und periodischer Nachprüfung · Serverseite als WordPress-Plugin auf epos-plan.de mit Frontend-Portal

---

## 1. Ausgangslage und Ziele

EPOS-Plan ist eine eigenständige Windows-Anwendung, die weitgehend offline arbeitet — eine Internetverbindung wird bisher nur für Klimadaten und die Online-Dokumentation benötigt. Das Lizenzsystem muss sich in dieses Nutzungsbild einfügen: Es darf den Offline-Betrieb im Planungsbüro oder auf der Baustelle nicht behindern, muss die zeitliche Befristung aber trotzdem zuverlässig durchsetzen.

Die Anforderungen im Überblick:

1. **Zwei Lizenzformen:** eine kostenlose Testversion (z. B. 30 Tage) und eine kostenpflichtige Voll-Lizenz mit Laufzeit (z. B. 12 Monate), verlängerbar.
2. **Firmenlizenz mit Benutzerbindung:** Ein Unternehmen erwirbt eine Lizenz für maximal N benannte Benutzer. Jeder Benutzer aktiviert sich persönlich (E-Mail) und kann auf mehreren eigenen Geräten arbeiten; der Server wacht über die Obergrenze.
3. **Gelegentlich online:** Aktivierung und Verlängerung laufen online; danach arbeitet die Anwendung offline weiter und prüft die Lizenz periodisch nach (mit großzügiger Karenzzeit).
4. **Fairer Ablauf:** Nach Lizenzende keine harte Sperre mit Datenverlust, sondern ein Lesemodus — bestehende Projekte bleiben einsehbar und exportierbar.
5. **Angemessener Schutz:** Das Ziel ist nicht Unknackbarkeit (die gibt es bei Desktopsoftware nicht), sondern ein Schutz, der ehrliche Kunden nicht behindert und Gelegenheitsmissbrauch — Weitergabe von Schlüsseln, Zurückstellen der Systemuhr, mehr Nutzer als bezahlt — zuverlässig unterbindet.

---

## 2. Lizenzmodell

### 2.1 Testversion

- **Laufzeit:** 30 Tage ab erster Aktivierung, voller Funktionsumfang (ggf. mit Wasserzeichen „Testversion" auf Exporten/Berichten).
- **Aktivierung:** mit E-Mail-Adresse gegen den Lizenzserver. Der Server registriert die E-Mail und einen Geräte-Fingerabdruck; so lässt sich verhindern, dass dieselbe Person/Maschine beliebig oft neu testet. Eine Bestätigungs-E-Mail mit Aktivierungslink hält Wegwerf-Adressen in Grenzen und liefert zugleich einen Vertriebskontakt.
- **Übergang:** Beim Kauf wird das Testkonto zur Voll-Lizenz hochgestuft — Projekte, Einstellungen und Kataloge bleiben unverändert erhalten, es ist keine Neuinstallation nötig.

### 2.2 Voll-Lizenz (Firmenlizenz mit Laufzeit)

Eine Firmenlizenz besteht aus drei Bausteinen:

| Baustein | Inhalt |
|---|---|
| **Lizenzvertrag** (serverseitig) | Firma, Lizenzschlüssel, Laufzeitbeginn/-ende, max. Benutzeranzahl N, gebuchte Module/Edition |
| **Benutzer** (serverseitig) | Benannte Benutzer (E-Mail), je Benutzer Status aktiv/deaktiviert und registrierte Geräte |
| **Lizenz-Token** (clientseitig) | Signierte Datei je Benutzer und Gerät mit allen Prüfdaten für den Offline-Betrieb (siehe Kap. 4) |

**Ablauf aus Kundensicht:**

1. Die Firma erhält nach dem Kauf einen **Lizenzschlüssel** (z. B. `EPOS-XXXX-XXXX-XXXX`) und Zugang zu einem kleinen **Lizenzportal** (Weboberfläche).
2. Ein Administrator der Firma trägt die Benutzer (E-Mail-Adressen) im Portal ein — oder Benutzer aktivieren sich direkt in EPOS-Plan mit Lizenzschlüssel + eigener E-Mail, bis N erreicht ist.
3. Jeder Benutzer aktiviert EPOS-Plan auf seinem Arbeitsplatz einmalig online — durch Eingabe des Lizenzschlüssels oder durch Laden der Lizenzdatei `.lic` (Kap. 2.3). Mehrere Geräte je Benutzer (Büro-PC + Laptop) sind zulässig, z. B. bis zu 2–3 registrierte Geräte pro Benutzer; ausschlaggebend für die Abrechnung bleibt die Benutzeranzahl.
4. Der Firmen-Admin kann Benutzer im Portal **deaktivieren und ersetzen** (Mitarbeiterwechsel). Eine Sperrfrist (z. B. Wechsel frühestens nach 7 Tagen) verhindert, dass ein Platz im Tagesrhythmus zwischen vielen Personen rotiert.

**Verlängerung:** Eine Verlängerung setzt serverseitig nur das Ablaufdatum neu; beim nächsten Online-Kontakt erhält jeder Client automatisch ein frisches Token. Der Kunde muss nichts installieren oder eingeben.

**Personenbezogene Lizenz (Einzelplatz):** Neben der Firmenlizenz gibt es die auf eine Person ausgestellte Lizenz — technisch eine Lizenz mit N = 1, bei welcher der Benutzer zugleich sein eigener Verwalter im Portal ist (Geräte freigeben, Schlüssel neu erzeugen). Sie ist nicht auf andere Personen übertragbar; ein Wechsel des Inhabers läuft über den Support. Für den Client und das Token ist sie derselbe Mechanismus wie die Firmenlizenz, nur ohne Benutzerverwaltung.

### 2.3 Lizenzschlüssel: Aufbau, Ausgabe, Verlust und Neuerstellung

**Aufbau des Schlüssels.** Am Lizenzschlüssel ist der Lizenztyp auf den ersten Blick erkennbar — für Anwender, Support und die Anwendung selbst:

```
EPOS-D-00871-K3M9-QW2X-7C     Demoversion (Testlizenz)
EPOS-P-00123-H8RA-TN4E-2F     Personenbezogene Lizenz
EPOS-F-00457-B6WD-ZK9P-5M     Firmenlizenz
```

| Bestandteil | Beispiel | Bedeutung |
|---|---|---|
| Präfix | `EPOS` | Produktkennung |
| Typkennung | `D` / `P` / `F` | **D**emo, **P**ersonenbezogen, **F**irma |
| Lizenznummer | `00457` | Fortlaufende, öffentliche Nummer — dient dem Server zum Auffinden des Lizenzdatensatzes und dem Support als Referenz („Lizenz 457") |
| Geheimteil | `B6WD-ZK9P` | Zufallszeichen; nur dieser Teil wird gehasht gespeichert |
| Prüfzeichen | `5M` | Prüfsumme über den ganzen Schlüssel — die Anwendung erkennt Tippfehler sofort, ohne Serverkontakt |

Als Zeichenvorrat dient ein verwechslungsfreies Alphabet (A–Z, 2–9 ohne O, I) — wichtig für telefonische Durchgaben. Die Typkennung ist dabei **Anzeige, nicht Berechtigung**: Maßgeblich für die Rechte ist immer der Lizenzdatensatz auf dem Server und der `typ`-Eintrag im signierten Token. Ein von Hand von „D" auf „F" geändertes Präfix scheitert bereits an der Prüfsumme, spätestens am Server. Der Client nutzt die Kennung aber, um den Aktivierungsdialog passend zu beschriften (z. B. Hinweis auf die Testlaufzeit bei „D") und Verwechslungen früh abzufangen.

**Ausgabe, Verlust und Neuerstellung.** Der Geheimteil des Schlüssels wird wie ein Passwort behandelt:

- **Zustellung per E-Mail oder als Lizenzdatei (.lic):** Bei der Erstellung (Kauf oder Neuerzeugung) wählt der Empfänger in der Benutzerverwaltung, wie er den Schlüssel erhält — als **E-Mail** an die hinterlegte Adresse oder als **Download einer Lizenzdatei `EPOS-Plan.lic`**. Die .lic-Datei enthält den Lizenzschlüssel samt Begleitdaten (Firma, Benutzer-E-Mail, Portal-Adresse) in einem einfachen, signierten Format; in EPOS-Plan genügt dann „Lizenzdatei laden" (oder ein Doppelklick auf die Datei), und die Aktivierung läuft ohne Abtippen des Schlüssels.
- **Nur als Hash gespeichert:** Auf dem Server liegt der Schlüssel **nie im Klartext**, sondern nur als Hash (z. B. bcrypt). Zustellung und Download sind deshalb nur im Moment der Erzeugung möglich — danach kann der Schlüssel weder im Portal noch von INEKON „nachgeschlagen" werden. Ein Datenbankleck gibt keine verwendbaren Schlüssel preis.
- **Selbsthilfe bei Verlust:** Geht der Schlüssel (bzw. die .lic-Datei) verloren, meldet sich der Firmen-Admin (bei Test- und Einzelplatzlizenzen der Benutzer selbst) mit seinem WordPress-Konto im Lizenzportal an und erzeugt per Klick einen **neuen Schlüssel** — wieder wahlweise per E-Mail oder als .lic-Download. Der alte wird damit sofort ungültig. Typkennung und Lizenznummer bleiben bei der Neuerzeugung erhalten — es ändert sich nur der Geheimteil.
- **Keine Nebenwirkungen auf bestehende Arbeitsplätze:** Bereits aktivierte Geräte sind davon nicht betroffen — sie prüfen nach der Aktivierung über ihre Token-ID nach, nicht über den Lizenzschlüssel (siehe Kap. 4). Der Schlüssel wird nur für *neue* Aktivierungen gebraucht.
- **Schutz:** Neuerzeugung wird protokolliert und per E-Mail an den Firmen-Admin bestätigt; ein Rate-Limit (z. B. max. 3 Neuerzeugungen pro Tag) verhindert Missbrauch.

**Abgrenzung — zwei verschiedene Schlüssel:** Der *Lizenzschlüssel* ist das Kundengeheimnis und liegt nach diesem Modell nicht (im Klartext) auf dem Server. Davon zu unterscheiden ist der *Signaturschlüssel* (Kap. 4.1), mit dem der Server die Lizenz-Tokens signiert: Er ist ein rein technisches Geheimnis von INEKON, wird für Aktivierung und Nachprüfung zwingend serverseitig benötigt und wird dort geschützt außerhalb des Web-Roots abgelegt. Die beiden haben nichts miteinander zu tun — der Verlust oder Wechsel eines Lizenzschlüssels berührt den Signaturschlüssel nicht.

---

## 3. Architektur

Zwei Komponenten, bewusst schlank gehalten:

```
┌────────────────────────────┐          HTTPS (nur bei Aktivierung,
│  EPOS-Plan (Client)        │          Nachprüfung, Verlängerung)
│                            │        ┌──────────────────────────────────┐
│  LizenzManager (C#)        │◄──────►│  WordPress epos-plan.de          │
│  ├ Token-Prüfung (offline) │        │  ├ Plugin „epos-lizenz"          │
│  ├ Uhr-/Manipulationsschutz│        │  │  ├ REST-API /wp-json/epos/v1  │
│  └ Lizenzdialog (UI)       │        │  │  ├ Lizenzen & Geräte (CPT/DB) │
└────────────────────────────┘        │  │  └ Token-Signierung (Sodium)  │
                                      │  ├ WP-Benutzerverwaltung         │
      Browser (Kunde) ───────────────►│  ├ Frontend-Lizenzportal         │
                                      │  └ wp-admin (nur INEKON)         │
                                      └──────────────────────────────────┘
```

**Serverseite: WordPress-Plugin statt eigener Anwendung.** Auf epos-plan.de läuft bereits das WordPress, das den Hilfekatalog der Anwendung über seine REST-API ausliefert — die Anwendung spricht mit diesem Server also ohnehin schon. Die Lizenzverwaltung wird als eigenes Plugin **`epos-lizenz`** dort angesiedelt und nutzt die vorhandene Infrastruktur:

- **Benutzerkonten** sind WordPress-Benutzer (E-Mail, Passwort, Passwort-Reset inklusive) mit eigenen Rollen `epos_benutzer` und `epos_firmenadmin` — sauber getrennt vom Redaktionszugang.
- **Lizenzen** sind ein Custom Post Type (Firma, Schlüssel-Hash, Laufzeit, max. N, gebuchte Edition) mit zugeordneten Benutzern und Geräten; für INEKON direkt im wp-admin pflegbar.
- **Token-Signierung** läuft über die in PHP eingebaute Sodium-Erweiterung (`sodium_crypto_sign`, Ed25519) — keine Zusatzbibliothek nötig.
- **REST-Endpunkte** registriert das Plugin unter `/wp-json/epos/v1/`:

| Endpunkt | Zweck |
|---|---|
| `POST /activate` | Lizenzschlüssel + E-Mail + Geräte-ID → prüft Schlüssel-Hash und Kontingent, registriert Gerät, liefert signiertes Token |
| `POST /validate` | Periodische Nachprüfung: Token-ID + Geräte-ID → liefert frisches Token oder Sperrgrund |
| `POST /deactivate` | Gerät/Benutzer freigeben (aus der App oder dem Portal) |
| `POST /trial` | Testversion anfordern (E-Mail + Geräte-ID) |

Die App-Endpunkte sind zustandslos absichert: Die Aktivierung authentifiziert sich über den Lizenzschlüssel selbst, die Nachprüfung über die Token-ID. Es wird kein WordPress-Passwort in der Desktop-Anwendung gespeichert.

**Frontend-Lizenzportal.** Die Selbstverwaltung für Kunden läuft **nicht** über wp-admin, sondern über kleine Frontend-Seiten im WordPress (eigene Templates bzw. Shortcodes hinter dem normalen WP-Login):

| Sicht | Funktionen |
|---|---|
| **Benutzer** | Lizenzstatus einsehen („gültig bis …"), eigene registrierte Geräte sehen und freigeben |
| **Firmen-Admin** | zusätzlich: Benutzer anlegen/deaktivieren (bis max. N, mit Sperrfrist), Belegung einsehen, **Lizenzschlüssel neu erzeugen** und wahlweise **per E-Mail zusenden oder als .lic-Datei herunterladen** (Kap. 2.3) |
| **INEKON** | wp-admin: Lizenzen anlegen, verlängern, sperren; Protokolle einsehen. wp-admin bleibt ausschließlich INEKON vorbehalten. |

**Client.** In EPOS-Plan kommt eine in sich geschlossene Komponente hinzu — z. B. Namespace `Allgemein/Lizenz/` mit:

- `LizenzManager` — zentrale Fassade: Lizenzstatus laden, prüfen, Nachprüfung anstoßen. Wird beim Programmstart und vor lizenzpflichtigen Aktionen (Simulation starten, Projekt anlegen) befragt.
- `LizenzToken` — Parsen und Signaturprüfung der Token-Datei.
- `GeraeteId` — stabiler Geräte-Fingerabdruck.
- `LizenzServerClient` — HTTPS-Aufrufe mit Timeout und Fehlertoleranz (Serverausfall darf nie zum Arbeitsstopp führen, solange die Karenzzeit läuft).
- `Form_Lizenz` — Dialog für Aktivierung (Schlüsseleingabe oder „Lizenzdatei .lic laden"), Statusanzeige („gültig bis …, 3 von 5 Benutzern belegt") und die bestehende Anzeige der Lizenzvereinbarung. Optional wird die Dateiendung `.lic` bei der Installation mit EPOS-Plan verknüpft, sodass ein Doppelklick auf die Datei direkt den Aktivierungsdialog öffnet.

---

## 4. Das Lizenz-Token — Kern der zeitlichen Beschränkung

Die zeitliche Befristung wird **kryptografisch im Token verankert**, nicht in Programmlogik, die sich per Konfiguration aushebeln ließe.

### 4.1 Aufbau

Das Token ist eine kleine signierte Datenstruktur (JSON, signiert mit **Ed25519**; der öffentliche Schlüssel ist in die Anwendung einkompiliert, der private liegt ausschließlich auf dem Server):

Der zugehörige **private Signaturschlüssel** existiert nur auf dem Server — ohne ihn kann der Server weder Aktivierungen noch Nachprüfungen bedienen, er ist also unverzichtbar. Ablage außerhalb des Web-Roots (nicht in der Datenbank, nicht unter `wp-content`), nur für den PHP-Prozess lesbar, Offline-Backup an sicherem Ort. Er ist strikt vom Lizenzschlüssel des Kunden zu unterscheiden (Kap. 2.3) und von dessen Verlust oder Neuerzeugung nicht betroffen.

```json
{
  "lizenz_id":     "EPOS-2026-00123",
  "firma":         "Beispiel Ingenieure GmbH",
  "benutzer":      "m.mueller@beispiel.de",
  "geraete_id":    "SHA256:9f2c…",
  "typ":           "firma",             // "demo" | "person" | "firma"
  "edition":       "standard",
  "gueltig_ab":    "2026-08-01",
  "gueltig_bis":   "2027-07-31",        // Laufzeitende der Lizenz
  "token_bis":     "2026-09-14",        // Offline-Leine: Nachprüfung fällig
  "ausgestellt":   "2026-08-01T09:12:00Z",
  "signatur":      "…"
}
```

Entscheidend sind die **zwei Fristen**:

- **`gueltig_bis`** — das eigentliche Lizenzende. Danach greift der Lesemodus.
- **`token_bis`** — die „Offline-Leine": Das Token selbst ist nur begrenzt gültig (z. B. 30–45 Tage). Jede erfolgreiche Online-Nachprüfung liefert ein frisches Token mit neuem `token_bis`. So wirkt eine serverseitige Sperre (Zahlungsausfall, Benutzerwechsel, gestohlener Schlüssel) spätestens nach Ablauf dieser Frist — ohne dass die Anwendung dauernd online sein muss.

### 4.2 Ablauf der Prüfung beim Programmstart

1. Token-Datei laden, **Signatur prüfen** (offline, Millisekunden). Ungültig/fehlend → Aktivierungsdialog.
2. **Geräte-ID vergleichen** — Token gilt nur auf dem Gerät, für das es ausgestellt wurde.
3. **`gueltig_bis` prüfen** → abgelaufen: Lesemodus + Verlängerungshinweis.
4. **`token_bis` prüfen:**
   - Noch gültig → normal starten. Liegt die letzte Nachprüfung mehr als z. B. 14 Tage zurück, wird **im Hintergrund** (nicht blockierend) eine Nachprüfung versucht; gelingt sie, wird das Token still erneuert.
   - Abgelaufen → Nachprüfung ist jetzt Pflicht. Gelingt sie nicht (kein Netz), startet die Anwendung trotzdem in einer **Karenzzeit** (z. B. weitere 14 Tage) mit deutlichem Hinweis. Erst danach wird auf Lesemodus geschaltet, bis wieder Kontakt zum Server bestand.

Damit gilt: Wer normal arbeitet und alle paar Wochen irgendeine Internetverbindung hat, bemerkt vom Lizenzsystem nichts. Wer monatelang komplett offline ist, braucht einmal kurz Netz — dieselbe Bedingung, die schon heute für Klimadaten gilt.

### 4.3 Schutz gegen Zurückstellen der Systemuhr

Der klassische Angriff auf zeitbeschränkte Lizenzen ist das Zurückdrehen der Windows-Uhr. Drei einfache, kombinierte Gegenmaßnahmen:

1. **Monotoner Zeitanker:** Die Anwendung speichert bei jedem Start und periodisch im Betrieb den höchsten je gesehenen Zeitstempel (verschlüsselt, siehe 4.4). Liegt die aktuelle Systemzeit *vor* diesem Anker, wird die Uhr als manipuliert gewertet → Hinweis und Behandlung wie „Token abgelaufen" (Nachprüfung nötig).
2. **Zeitstempel in Dateien:** Zeitstempel zuletzt geöffneter Projekte/Logdateien dienen als Plausibilitätsquelle.
3. **Serverzeit ist Referenz:** Bei jeder Online-Nachprüfung übermittelt der Server seine Zeit; grobe Abweichungen werden protokolliert. Die Uhr dauerhaft zurückzustellen hieße, nie wieder online zu prüfen — dann greift ohnehin die Offline-Leine.

### 4.4 Sichere lokale Ablage

- Token + Zeitanker + Zähler liegen im AppData-Verzeichnis, verschlüsselt mit **Windows DPAPI** (Machine-Scope) — derselbe Mechanismus bietet sich auch für den bereits geplanten KI-API-Schlüssel an.
- Zusätzlich eine versteckte Zweitkopie des Zeitankers (z. B. Registry), damit schlichtes Löschen des AppData-Ordners den Anker nicht entfernt.
- Wichtig: Die Signaturprüfung schützt den *Inhalt* des Tokens; DPAPI erschwert nur das Auslesen/Kopieren. Ein auf ein anderes Gerät kopiertes Token scheitert ohnehin an der Geräte-ID.

### 4.5 Geräte-Fingerabdruck

Hash aus mehreren stabilen Merkmalen (Windows-Machine-GUID, Mainboard-Seriennummer, Volume-ID der Systempartition). Bewertung tolerant gestalten: Stimmen z. B. 2 von 3 Merkmalen, gilt das Gerät als identisch — so überlebt die Lizenz einen Festplattentausch. Bei komplettem Gerätewechsel: alte Registrierung im Portal oder per Support freigeben.

---

## 5. Durchsetzung der Benutzer-Obergrenze

Die Obergrenze N wird **serverseitig** durchgesetzt — nur dort ist die Gesamtsicht vorhanden:

- `POST /activate` zählt die aktiven Benutzer der Lizenz. Ist N erreicht, wird die Aktivierung abgelehnt; die Fehlermeldung nennt den Firmen-Admin als Ansprechpartner („Alle 5 Benutzerplätze sind belegt").
- Jeder Benutzer darf eine kleine Zahl Geräte registrieren (2–3). Auch das prüft der Server bei der Aktivierung.
- Benutzerwechsel (deaktivieren + neu anlegen) laufen über das Portal; die 7-Tage-Sperrfrist verhindert Platz-Rotation. Das deaktivierte Token stirbt spätestens mit Ablauf seiner Offline-Leine.
- Optional als Ausbaustufe: **gleichzeitige Sitzungen** je Benutzer begrenzen (Heartbeat bei bestehender Netzverbindung), falls sich zeigt, dass Zugangsdaten geteilt werden. Für den Start genügt die benannte Bindung.

---

## 6. Verhalten bei Ablauf — der Lesemodus

Eine abgelaufene Lizenz darf Bestandsdaten nicht in Geiselhaft nehmen. Vorschlag für die Abstufung:

| Phase | Verhalten |
|---|---|
| 30 / 14 / 7 Tage vor Ablauf | Dezenter Hinweis beim Start (einmal täglich), Statuszeile im Lizenzdialog |
| Ablauf bis +14 Tage | Deutlicher Hinweis, volle Funktion (Kulanzfenster für verspätete Verlängerung) |
| Danach | **Lesemodus:** Projekte öffnen, Ergebnisse ansehen, CSV-Export, Drucken — aber keine neuen Projekte, keine Änderungen, keine neuen Simulationen |
| Nach Verlängerung | Beim nächsten Online-Kontakt frisches Token, sofort wieder Vollbetrieb |

Der Lesemodus ist zugleich das Auffangnetz aller Fehlerfälle (Serverausfall über die Karenzzeit hinaus, Uhrmanipulation erkannt): Die Anwendung wird nie „zugenagelt", sie stellt nur das Erzeugen neuer Arbeitsergebnisse ein.

---

## 7. Datenschutz

- Übertragen werden ausschließlich: Lizenzschlüssel, E-Mail des Benutzers, Geräte-Hash, Programmversion, Zeitstempel. **Keine Projekt- oder Kundendaten** — dieselbe Linie wie beim KI-Assistenten.
- Der Geräte-Fingerabdruck verlässt das Gerät nur als Hash; Rückschluss auf Hardware ist nicht möglich.
- Aktivierungs- und Prüfprotokolle auf dem Server dienen Support und Missbrauchserkennung; Aufbewahrung befristen (z. B. 12 Monate über Lizenzende hinaus). AV-rechtlich sauber in der Lizenzvereinbarung/AGB verankern (Rechtsgrundlage: Vertragserfüllung, Art. 6 Abs. 1 lit. b DSGVO).
- Serverstandort Deutschland (ALL-INKL erfüllt das bereits).

---

## 8. Umsetzung in Schritten

**Schritt 1 — Client-Grundgerüst (offline-fähig).** `LizenzManager`, Token-Format, Signaturprüfung, Geräte-ID, DPAPI-Ablage, Zeitanker, Lesemodus. Testbar mit manuell ausgestellten Token-Dateien — bereits jetzt ließe sich damit eine per E-Mail verschickte, zeitlich befristete Lizenzdatei ausliefern, noch ganz ohne Server.

**Schritt 2 — WordPress-Plugin `epos-lizenz`.** Datenmodell (Lizenz-CPT, Benutzer-Rollen, Geräte, Protokoll), die vier REST-Endpunkte, Token-Signierung mit Sodium, Lizenzschlüssel-Hashing. Schlüsselpaar erzeugen; privaten Signaturschlüssel außerhalb des Web-Roots ablegen (Offline-Backup an sicherem Ort — sein Verlust wäre der GAU, seine Kompromittierung erforderte ein Anwendungsupdate mit neuem öffentlichen Schlüssel).

**Schritt 3 — Aktivierung & Trial im Client.** `Form_Lizenz` mit Aktivierungsdialog, Trial-Anforderung, Hintergrund-Nachprüfung, Statusanzeige.

**Schritt 4 — Frontend-Lizenzportal.** Die INEKON-Sicht gibt es über wp-admin praktisch geschenkt (Lizenzen anlegen, verlängern, sperren). Danach die Frontend-Seiten für Kunden: Benutzer-Sicht (Status, eigene Geräte), Firmen-Admin-Sicht (Benutzer verwalten, Lizenzschlüssel neu erzeugen). 

**Schritt 5 — Feinschliff.** Warnstufen, Kulanzfenster, Wasserzeichen in der Testversion, Auswertung der Aktivierungszahlen.

Schritt 1 ist der größte zusammenhängende Block im Client und sofort nutzbar; die Serverteile lassen sich unabhängig davon entwickeln und testen.

---

## 9. Risiken und Grenzen

- **Kein absoluter Schutz:** .NET-Anwendungen lassen sich dekompilieren; ein entschlossener Angreifer kann die Prüfung entfernen. Obfuskation (z. B. beim Release-Build) erhöht die Hürde, mehr nicht. Das ist branchenüblich und akzeptabel — die Zielgruppe sind Ingenieurbüros und Stadtwerke, keine anonyme Massenkundschaft; der Vertragstext trägt den Rest.
- **Serververfügbarkeit:** Durch Offline-Leine + Karenzzeit führt selbst ein mehrwöchiger Serverausfall nicht zu Arbeitsausfällen bei Kunden. Trotzdem: Monitoring und Backup für den Lizenzserver einplanen.
- **WordPress als Angriffsfläche:** Ein öffentlich erreichbares WordPress ist ein beliebtes Ziel. Pflichtprogramm: Kern und Plugins konsequent aktuell halten, 2FA für alle wp-admin-Konten, Signaturschlüssel außerhalb des Web-Roots, Lizenzschlüssel nur als Hash, Rate-Limits auf allen Lizenz-Endpunkten. Selbst bei einer Kompromittierung begrenzt die Offline-Leine den Schaden zeitlich, und ein Schlüsseltausch ist per Anwendungsupdate möglich.
- **Support-Aufwand:** Gerätewechsel, Tippfehler in E-Mails, Firmenumbenennungen erzeugen Supportfälle. Das Portal mit Selbstverwaltung für Firmen-Admins fängt den Großteil ab.
- **Uhr-Heuristiken:** Falsch-positive (z. B. BIOS-Batterie leer → Uhr springt zurück) freundlich behandeln: nie Daten sperren, sondern zur Online-Nachprüfung auffordern, die den Zustand sofort heilt.

---

## 10. Empfehlung

Mit **Schritt 1 beginnen**: signiertes Token, Offline-Prüfung, Zeitanker und Lesemodus im Client. Das ist die Substanz des Systems, funktioniert anfangs auch mit manuell ausgestellten Lizenzdateien und legt das Ablaufverhalten fest, bevor Serverinfrastruktur entsteht. Das Plugin `epos-lizenz` auf dem vorhandenen WordPress (epos-plan.de) folgt als zweiter Block und macht aus der Lizenzdatei den vollautomatischen Kreislauf aus Aktivierung, Nachprüfung und Verlängerung — mit der Firmenlizenz samt Benutzerobergrenze als serverseitig durchgesetztem Kern, dem Frontend-Portal für die Selbstverwaltung der Kunden und dem Lizenzschlüssel, der wie ein Passwort behandelt wird: nur als Hash gespeichert, bei Verlust per WordPress-Login selbst neu erzeugbar.

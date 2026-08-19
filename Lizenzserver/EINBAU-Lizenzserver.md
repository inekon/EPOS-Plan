# Einbau im Plugin `epos-lizenz`

Alle Änderungen sind **bereits in die Plugin-Quelle eingearbeitet** unter
`Y:\6-Material-Tools,Literatur,Vorträge,Kurse\9-Software\WordPress\EPOS-Plan\epos-lizenz\`
(die Arbeitskopie unter `C:\Temp\p\epos-lizenz\` ist identisch nachgezogen).
Zu tun bleibt: **hochladen und prüfen**.

Getestet ist nichts davon — auf diesem Rechner steht kein PHP zum Prüfen zur
Verfügung. Die Originale liegen als `*.original-2026-08-19` in diesem Ordner.

Geänderte und neue Dateien:

| Datei | Änderung |
|---|---|
| `includes/class-epos-aufbewahrung.php` | **neu** — Aufbewahrung und Datensparsamkeit |
| `epos-lizenz.php` | Modul eingebunden, Zeitplan bei Aktivierung/Deaktivierung, `init` |
| `includes/class-epos-geraete.php` | Gerätename nur noch laut Option |
| `includes/class-epos-rest.php` | Route und Methode `GET /epos/v1/vertrag` |
| `includes/class-epos-woocommerce.php` | Tarifpreise aus WooCommerce (Abschnitt 4) |

---

## Wie es auf den Server kommt

Das ist **kein neues Plugin**, sondern eine Aktualisierung des vorhandenen
„EPOS-Lizenz" — auf dem Server Version 1.3.0, hier jetzt **1.4.0**.

**Weg A — Dateien ersetzen (empfohlen).** Per FTP/SFTP nach
`wp-content/plugins/epos-lizenz/` kopieren:

- `epos-lizenz.php`
- `includes/class-epos-aufbewahrung.php` *(neu)*
- `includes/class-epos-geraete.php`
- `includes/class-epos-rest.php`
- `includes/class-epos-woocommerce.php`

Deaktivieren und Wiederaktivieren ist nicht nötig: `Epos_Aufbewahrung::init()`
plant den täglichen Lauf beim nächsten Seitenaufruf selbst ein, falls noch kein
Termin steht. Dieser Weg kann nichts überschreiben, was nur auf dem Server liegt.

**Weg B — Zip über wp-admin.** Ordner `epos-lizenz` zippen, unter *Plugins →
Installieren → Plugin hochladen* einspielen; WordPress erkennt denselben Slug und
bietet „Aktuelle Version ersetzen" an. Das ersetzt den **gesamten Ordner** — nur
gangbar, wenn die Quelle hier vollständig ist. Sie umfasst 14 Dateien und trug
vor der Änderung dieselbe Version wie der Server; ob dort dennoch etwas liegt,
das hier fehlt, konnte ich nicht prüfen.

Der Plugin-Editor in wp-admin scheidet aus — er kann keine neuen Dateien anlegen.

---

## 1 Aufbewahrung und Datensparsamkeit

Neu: `includes/class-epos-aufbewahrung.php`. In `epos-lizenz.php` eingehängt
(require, `Epos_Aufbewahrung::planen()` bei der Aktivierung, `::abmelden()` bei
der Deaktivierung, `add_action( 'init', … )`); in `class-epos-geraete.php` läuft
der Gerätename jetzt über `Epos_Aufbewahrung::geraetename( $name )`.

### Was der tägliche Lauf tut (03:20 UTC)

| Schritt | Standard | Wirkung |
|---|---|---|
| Abgelaufene Lizenzen samt Geräten löschen | 365 Tage nach `_epos_gueltig_bis`, bei Laufzeiten bis 6 Monaten 90 Tage | Lizenzen **ohne** Ablaufdatum bleiben unangetastet |
| Verwaiste Geräteeinträge löschen | — | Tabelle bleibt konsistent |
| Gerätenamen leeren | ein | Option `geraetename` auf 1 setzen, um Namen zu behalten |
| IP aus dem Abschlussprotokoll entfernen | 1095 Tage | Rest des Protokolls (Zeitstempel, Tarif, Fassung, SHA-256) bleibt |
| Erzeugte Vertrags-PDFs löschen | 30 Tage | werden bei Bedarf neu erzeugt |

Bestellungen und Rechnungsdaten werden **nicht** angefasst — § 147 AO und die
Beweissicherung nach Abschnitt 5 der Spezifikation.

### Werte ändern

```php
update_option( 'epos_aufbewahrung', array(
	'lizenz_tage'      => 365,   // Laufzeiten über 6 Monate
	'lizenz_tage_kurz' => 90,    // Tarif „Einzel (3 Monate)" und dergleichen
	'ip_tage'          => 1095,
	'pdf_tage'         => 30,
	'geraetename'      => 0,
) );
```

Die Laufzeit wird aus `_epos_gueltig_ab` und `_epos_gueltig_bis` bestimmt, nicht
aus dem Tarifnamen — neue Tarife laufen ohne Anpassung mit.

### Kontrolle nach dem Einspielen

1. `wp_next_scheduled( 'epos_aufbewahrung_taeglich' )` liefert einen Zeitstempel.
2. Einen Lauf von Hand auslösen: `do_action( 'epos_aufbewahrung_taeglich' );`
3. Ergebnis ansehen: `get_option( 'epos_aufbewahrung_bericht' )` — Zeitpunkt und
   Anzahl je Schritt.
4. **Vor dem ersten scharfen Lauf**: prüfen, wie viele Lizenzen betroffen wären.
   `lizenz_tage` notfalls hochsetzen, bis das Ergebnis plausibel ist.

**Achtung:** Der erste Lauf löscht alles, was die Grenze schon überschritten hat.
Vorher eine Datenbanksicherung ziehen.

---

## 2 Vertragsendpunkt

Route und Methode `vertrag()` stehen in `includes/class-epos-rest.php`.

Danach prüfen — ohne Zugangsdaten im Browser aufrufbar:

```
https://epos-plan.de/wp-json/epos/v1/vertrag
https://epos-plan.de/wp-json/epos/v1/vertrag?tarif=einzel
```

Erwartet: HTTP 200, `"ok":true`, je Tarif Stand-Datum, SHA-256 und URL.

---

## 3 Unabhängig davon: überholter Entwurf in der Mediathek

`epos-plan-lizenzvereinbarung-einzel-3-monate-2026-08-11.pdf` ist in der
Mediathek als „ÜBERHOLT – nicht verwenden" bezeichnet, aber ohne Anmeldung
abrufbar (HTTP 206). Datei löschen oder aus dem Uploads-Verzeichnis nehmen.

---

## 4 Tarifpreise kommen aus WooCommerce (bereits eingearbeitet)

Geändert in `epos-lizenz/includes/class-epos-woocommerce.php` — die Quelle auf
`Y:\...\WordPress\EPOS-Plan\epos-lizenz\` ist schon angepasst, sie muss nur
noch hochgeladen werden.

**Warum:** Am 13.08.2026 wurden die Shop-Preise neu festgesetzt, im Quelltext
blieben die alten Festwerte stehen. Der Kassen-Block wurde per Ausgabepuffer in
`epos-checkout-recht` v1.0.11 korrigiert — die **Bestellbestätigung** und der
**Abschlussprotokoll-Datensatz** lasen aber weiter aus `tarife()` und damit die
alten Preise.

**Was sich ändert:** `Epos_Vertrag::tarife()` liest den Preis jetzt aus dem
verknüpften Produkt (`produkt_<tarif>` aus den Optionen) über
`wc_get_price_excluding_tax()`. Die bisherige Tabelle heißt jetzt
`tarif_grunddaten()` und liefert nur noch den Rückfallwert, falls kein Produkt
hinterlegt oder WooCommerce nicht aktiv ist. Die Rückfallwerte sind zugleich auf
den Shop-Stand gehoben:

| Tarif | vorher | jetzt |
|---|---|---|
| Einzel | 100 € | 120 € |
| Einzel (3 Monate) | 35 € | 42 € |
| Team | 200 € | 700 € |
| Team Plus | 500 € | 1.200 € |
| Campus/Student | 50 € | 60 € |

`produkte_sicherstellen()` schreibt `_regular_price` weiterhin **nur beim
Neuanlegen** eines Produkts — bestehende Shop-Preise werden nicht überschrieben.

### Kontrolle nach dem Hochladen

1. Kasse mit einem Tarif im Warenkorb öffnen: Der Preis im Block
   „Vertragsschluss" muss dem Produktpreis entsprechen — jetzt auch ohne den
   Ausgabepuffer aus `epos-checkout-recht`.
2. Testbestellung: Der Preis in der Bestellbestätigung und im Protokoll
   (`_epos_vertrag_protokoll`) muss mit der Rechnung übereinstimmen.
3. Danach kann die Puffer-Korrektur der Tarifpreise in `epos-checkout-recht`
   entfallen (offener Punkt „Puffer-Fixes dauerhaft in epos-lizenz übernehmen").

### Weiterhin offen

Die Lizenzvereinbarungs-PDFs für **Einzel, Team und Team Plus** sind vom
02.08.2026 und nennen noch die alten Preise. Sie sind im Backend als
Vertragsdokumente hinterlegt, werden im Checkout verlinkt, mit SHA-256
protokolliert und der Bestellbestätigung angehängt — der Kunde akzeptiert also
ein Dokument mit einem anderen Preis als dem berechneten. Campus/Student und
Einzel (3 Monate) sind mit den Fassungen vom 11.08.2026 in Ordnung.

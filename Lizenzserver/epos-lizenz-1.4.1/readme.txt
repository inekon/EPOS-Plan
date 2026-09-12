=== EPOS-Lizenz ===
Contributors: inekon
Requires at least: 6.0
Tested up to: 6.8
Requires PHP: 7.4
Stable tag: 1.0.0
License: proprietär

Lizenzverwaltung für die Desktop-Anwendung EPOS-Plan.

== Description ==

* Zeitlich befristete Lizenzen in drei Typen: Demoversion (D), personenbezogene Lizenz (P), Firmenlizenz (F) mit maximaler Benutzeranzahl
* Lizenzschlüssel-Format EPOS-T-NNNNN-XXXX-XXXX-PP (Prüfsumme, Geheimteil nur als Hash gespeichert)
* Signierte Lizenz-Tokens (Ed25519 über PHP-Sodium) mit Laufzeitende und Offline-Leine (token_bis)
* REST-API für die Anwendung: /wp-json/epos/v1/activate | validate | deactivate | trial
* Frontend-Portal per Shortcode [epos_lizenzportal]: Status, Geräte, Benutzerverwaltung, Schlüssel-Neuerzeugung (E-Mail oder .lic-Download)
* wp-admin-Verwaltung unter „EPOS-Lizenzen" (nur Administratoren)

== Installation ==

1. ZIP unter Plugins → Installieren → Plugin hochladen installieren und aktivieren.
2. Unter EPOS-Lizenzen → Einstellungen den öffentlichen Signaturschlüssel kopieren (kommt in die Anwendung).
3. Eine Seite mit dem Shortcode [epos_lizenzportal] anlegen und in den Einstellungen als Portal-Seite wählen.
4. Erste Lizenz unter EPOS-Lizenzen → Neue Lizenz anlegen und „Neuen Schlüssel erzeugen" klicken.

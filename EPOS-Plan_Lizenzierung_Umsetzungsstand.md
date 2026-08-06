# Lizenzierung EPOS-Plan — Umsetzungsstand 01.08.2026 (Nachmittag)

## Server (erledigt, live)
- WordPress-Plugin **epos-lizenz 1.1.0** auf epos-plan.de installiert und aktiviert (Menü „EPOS-Lizenzen").
- REST-API: `https://epos-plan.de/wp-json/epos/v1/` → activate, validate, deactivate, trial (POST). End-to-End getestet.
- **Signaturschlüssel** (Ed25519) liegt außerhalb des Web-Roots: `/www/htdocs/w021c44d/epos-lizenz-schluessel/` — privaten Schlüssel extern sichern!
- **Öffentlicher Schlüssel (Base64):** `sMcmb2GQqE1cGv98J01FvJ/+W1faogMUQfK+lPfG3Kk=` (im C#-Client einkompiliert, `LizenzToken.OEFFENTLICHER_SCHLUESSEL_BASE64`).
- Token-Format **epos-signiert-1**: `{format, nutzdaten (Base64-JSON), signatur}` — Signatur über die exakten Bytes, Client braucht keine Kanonisierung. Innere Nutzdaten: epos-token-1 mit gueltig_bis (Lizenzende), token_bis (Offline-Leine 45 Tage), kulanz_tage (14).
- **Frontend-Portal:** https://epos-plan.de/lizenzportal/ (Seite mit Shortcode `[epos_lizenzportal]`, in den Plugin-Einstellungen hinterlegt).
- **Benutzerprofil-Felder (neu in 1.1.0):** Firmenname, USt-IdNr., Straße/Hausnr., PLZ, Ort, Land, Telefon — alle optional, für Rechnung und Lizenz. Eingebunden auf der Ultimate-Member-Konto-Seite (/account/, Tab „Konto"), im WooCommerce-Kontodetails-Formular (/mein-konto/) und im wp-admin-Benutzerprofil. Gespeichert in den WooCommerce-Feldern `billing_company`, `billing_vat_id`, `billing_address_1`, `billing_postcode`, `billing_city`, `billing_country` (ISO-Code), `billing_phone` — laufen damit direkt in spätere WooCommerce-Bestellungen/Rechnungen.
- **Testlizenz 04795** „TEST Musterfirma GmbH" (Firma, max. 2, bis 01.08.2027) mit Testaktivierung (lizenz-test@example.com, Gerät SHA256:E2ETESTGERAET0001) — kann nach Abschluss der Tests in den Papierkorb.
- Plugin-Quellcode + ZIP: `C:\Waermeplan\Licence\epos-lizenz.zip`.

## So wird der Lizenzschlüssel erstellt
Format: `EPOS-{D|P|F}-NNNNN-XXXX-XXXX-PP` — Beispiel `EPOS-F-04795-LFKP-XYYU-ML`.

1. **Auslöser:** INEKON klickt im wp-admin (EPOS-Lizenzen → Lizenz → „Neuen Schlüssel erzeugen"), der Firmen-Admin nutzt das Lizenzportal, oder der Trial-Endpunkt legt automatisch eine Demo-Lizenz an.
2. **Aufbau** (`Epos_Schluessel::erzeugen()` in `includes/class-epos-schluessel.php`):
   - Typkennung **D**emo / **P**erson / **F**irma aus dem Lizenzdatensatz;
   - Lizenznummer = WordPress-Post-ID der Lizenz, 5-stellig (öffentlich, dient Server-Lookup und Support);
   - **Geheimteil**: 2 × 4 Zufallszeichen aus `random_int()` über das Alphabet `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` (32 Zeichen, ohne O/I/0/1 — telefonierbar);
   - **Prüfzeichen**: CRC32 über den Schlüsselrumpf, auf 2 Zeichen desselben Alphabets abgebildet — die Anwendung erkennt Tippfehler ohne Serverkontakt.
3. **Speicherung:** Nur der Geheimteil, gehasht mit `password_hash()` (bcrypt), als `_epos_schluessel_hash` am Lizenzdatensatz. Der Klartext wird **genau einmal** angezeigt (Admin-Notice, 10 Min.) bzw. per E-Mail/.lic zugestellt und ist danach nicht mehr rekonstruierbar.
4. **Neuerzeugung:** ersetzt nur den Geheimteil (Typ + Nummer bleiben), macht den alten Schlüssel sofort ungültig, Rate-Limit 3/Tag je Lizenz. Bereits aktivierte Geräte laufen weiter (Nachprüfung läuft über die Token-ID, nicht über den Schlüssel).
5. **Prüfung bei Aktivierung:** Format + Prüfsumme → Lizenz-Lookup über die Nummer → Typvergleich → `password_verify()` des Geheimteils → Status/Laufzeit/Kontingent → signiertes Token.

## Client (Code erledigt, Build/Test läuft)
Projekt `C:\Waermeplan\WP_Plan\WindowsFormsApplication1` (net8.0-windows):
- Neu: `Allgemein/Lizenz/LizenzToken.cs` (Ed25519-Prüfung via BouncyCastle), `GeraeteId.cs`, `LizenzServerClient.cs`, `LizenzManager.cs` (DPAPI-Ablage %AppData%\wp-plan\lizenz.dat, Zeitanker, Karenz 14 Tage, Lesemodus-Logik `DarfSchreiben()`), `Views/Admin/Form_LizenzVerwaltung.cs`.
- `MDIMainForm.cs`: `InitLizenzMenue()` — Menüpunkt **Administration → Lizenz…** direkt unter Einstellungen (programmatisch, Designer/.resx unberührt) + stille Hintergrund-Nachprüfung.
- `csproj`: + BouncyCastle.Cryptography 2.7.0, System.Security.Cryptography.ProtectedData 8.0.0.
- C#-Signaturprüfung gegen echtes Server-Token verifiziert (inkl. Manipulationserkennung).
- **Fix 01.08.:** ArgumentOutOfRangeException beim ersten Start behoben (fehlender Zeitanker → `DateTime.MinValue.AddDays(-1)`; Prüfung entfällt jetzt ohne Anker).

## Offen
- Build unter Windows (`dotnet build -p:Platform=x86`) und UI-Test des Dialogs.
- Lesemodus-Durchsetzung in der App: `LizenzManager.DarfSchreiben()` an Simulation/Projektanlage anbinden.
- .lic-Dateiendung mit EPOS-Plan verknüpfen (Installer), E-Mail-Versand (wp_mail/SMTP) auf ALL-INKL prüfen.
- Konzept: `claude/EPOS-Plan_Konzept_Lizenzierung.md`.

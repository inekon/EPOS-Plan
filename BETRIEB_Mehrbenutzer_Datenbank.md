# Betrieb: Kenndaten.accdb mit mehreren Windows-Konten

**Stand:** 14.08.2026 · Befund aus der Codeanalyse (B0-Zeitraum)

## Symptom

Ist EPOS-Plan auf einem Konto geöffnet, scheitert der Programmstart auf einem
zweiten Konto desselben Rechners am ersten Datenbankzugriff („Datenbank kann
nicht geöffnet werden" / „bereits exklusiv geöffnet" / schreibgeschützt).

## Ursache — nicht Access, sondern NTFS

Die Anwendung öffnet die Datenbank **nirgends exklusiv**: kein `Mode=`-Parameter,
kein `Share Deny`; auch `RecordSet` läuft über den gemeinsamen Shared-Connection-
String von `DataRepository`, Verbindungen werden je Abfrage geöffnet und
geschlossen. Access/ACE ist mehrbenutzerfähig.

Blockiert wird über die **Standard-ACL von `C:\ProgramData`**: Dort dürfen normale
Benutzer neue Dateien anlegen, fremde aber nur lesen. Die Sperrdatei
`Kenndaten.laccdb` legt das **erste** Konto an — das zweite Konto kann sich nicht
in die Sperrdatei eintragen, und ACE verweigert das Öffnen. Dieselbe Wurzel wie
das bekannte „schreibgeschützt bis Komprimieren/Reparieren"-Problem nach der
Installation (siehe ADR-001, Abschnitt „Kräfte", Punkt 1).

## Lösung (einmalig, mit Adminrechten)

Der Gruppe „Benutzer" vererbend Änderungsrechte auf den Datenordner geben:

```bash
icacls "C:\ProgramData\EPOS_PLAN" /grant "*S-1-5-32-545:(OI)(CI)M" /T
```

`S-1-5-32-545` ist die sprachneutrale SID der Gruppe „Benutzer" (funktioniert
auch auf englischen Systemen). `(OI)(CI)` vererbt auf künftige Dateien — damit
sind auch neu entstehende `.laccdb`-Dateien erfasst. Danach teilen sich beliebig
viele Konten die Datenbank im normalen Shared-Modus, und die frisch installierte
`Kenndaten.accdb` ist sofort beschreibbar.

**Gehört dauerhaft in den Installer** (Post-Install-Schritt mit erhöhten Rechten).

## Optionale App-seitige Ergänzung (offen, ~0,5–1 PT)

Startvorprüfung statt Fehlerkaskade: Testverbindung beim Start; scheitert sie und
liegt eine `Kenndaten.laccdb` neben der DB, eine verständliche Meldung mit
„Erneut versuchen". Die `.laccdb` enthält Rechner- und Benutzernamen der offenen
Sitzungen im Klartext — die Meldung kann also benennen, **wer** blockiert.
Denkbar zusätzlich ein Lesemodus-Fallback (`Mode=Read` im Connection-String,
passend zum Lizenzzustand „Lesemodus"); ob ACE beim reinen Lesen ohne
Schreibrecht auf die Sperrdatei auskommt, ist vorab zu testen.

## Ausdrücklich nicht nötig

Exklusive Zugriffe aus dem Code entfernen — es gibt keine (verifiziert
14.08.2026: Volltextsuche `Mode=`/`Share Deny`/`Exclusive` ohne Treffer).

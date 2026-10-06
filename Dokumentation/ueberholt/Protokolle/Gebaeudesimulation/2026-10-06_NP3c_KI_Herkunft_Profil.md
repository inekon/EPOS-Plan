# Protokoll NP3c — KI-Feldkarte, Importprobe, Herkunft Profil gegen Vorlage (06.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Welle NP3c der Stufe NP. Zweig `np3c-reste` auf `74d2337a`: `353e2427` NP3c-1, `bb12f545` NP3c-2, `17557d11` NP3c-3; Merge `1a268145`.

## 1 Ergebnis

1. **KI-Feldkarte des Blatts „Nutzungsprofile“.** `KiNutzungsprofilfelder` (Kern, Muster `KiKonditionierungsfelder`) führt 29 Felder `np_*`: Kennwerte, Name, Kategorie nur lesend, Einheit der Außenluft als Wahl, Anteile in Prozent; die Zuordnungszeilen sind ein Leseraster. Der Zugang `RaumnutzungKiZugang` setzt nur den Entwurf, in den Katalog kommt er erst mit „Speichern“. Ohne offenes Blatt, ohne Profil und bei ausgelieferten Profilen gibt es eine benannte Absage. Texte `RNP_KI_*` in beiden Sprachen.
2. **bunit-Probe** `GebaeudeImportNutzungsprofileDialogTests` (4): Der Knopf erscheint nur mit Gabe, öffnet die Überlagerung, Esc schließt zuerst sie, danach wird neu zugeordnet.
3. **Herkunft Profil gegen gleichnamige Vorlage — Befund mit roter Probe.** Die Muster Wohnen, Büro, Schule heißen wie eine Vorlage; nach Übernahme des Profils Büro trugen 5 von 5 Kalendern „BUERO“ statt „Büro“. Behebung: Die Herkunft trägt ihre Art (`IstProfil`, Bemerkung „aus Nutzungsprofil {0}“) durch Generator, Übernahme, DTO und Hülle; beim Speichern gilt bei einem Profil dessen Nutzung. Test „Profil Büro und Vorlage Büro“; Pufferauslegung: Büro und BUERO ergeben dasselbe Pufferprofil.

## 2 Nachweise

Agentenabnahme: UI 7583/7583, Kern-Auswahl 1196/1196, Referenzlauf 21/21 PASS, CSV 646 byte-gleich. Gate `gate_haupt.sh NP2a` auf `1a268145` (NP2a und NP3c auf NP3b): alles grün, Zahlen wie im [Protokoll NP2a](2026-10-06_NP2a_Zuordnung_Import.md). Merge mit dem HC-2-Stand `7b771f82`: nur die `.resx` im Konflikt, vereinigt (15 309 Einträge je Sprache, keine Doppelten, XML gültig, Designer gleich). Die Statuszeile ist #753.

## 3 Offenes

Aus NP3c nach NP2b verschoben: KI-Zugang im Importdialog, Sperre der `np_*`-Felder bei gesperrtem Gebäude, Zeile „Vorlage“ über die Herkunftsart. NP4 offen; kein Wiki-Upload vor NP4.

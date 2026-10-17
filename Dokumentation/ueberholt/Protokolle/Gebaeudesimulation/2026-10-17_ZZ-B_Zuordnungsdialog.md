# Protokoll ZZ-B — Zuordnungsdialog „Nutzungsprofile zuordnen…“ (Hülle und Dialog)

Grundlage: [Entwurf Zonenzuordnung IFC](../../../aktuell/Gebaeudesimulation/2026-10-17_Entwurf_Zonenzuordnung_IFC.md),
Abschnitt 6 (Zuordnungsdialog) und Abschnitt 10 (F2, F3, F6); Kernschnittstelle aus
[ZZ-A](2026-10-17_ZZ-A_Zonenregel_Profilzuordnung.md). Zweig `zonb`, abgezweigt von `zon` bei `9043c10f0`. Kein Schema, keine
Änderung am Kern außer Ressourcen.

Anwenderwunsch: Ist die Zuordnung nicht eindeutig, sollen die Räume der eingelesenen Datei einfach und übersichtlich den
vorhandenen Nutzungsprofilen zugeordnet werden können — mit strukturierter Auswahl und mit Vorschlägen.

## 1. Aufbau

| Teil | Ort | Inhalt |
|---|---|---|
| DTO | `EPOS.UI/Dialoge/Import/GebaeudeProfilzuordnungDaten.cs` (neu) | `GebaeudeProfilzuordnungDaten` (Bündel, Katalog, Zähler in Räumen: eindeutig, Vorschlag, Rückfrage, ohne, von Hand; offene Bündel; Ampel), `GebaeudeProfilbuendelDaten`, `GebaeudeProfilknotenDaten`, `GebaeudeProfilwahl`, Schlüssel `GebaeudeProfilstand` und `GebaeudeProfilampel`, Textbündel `GebaeudeProfilzuordnungTexte`; `GebaeudeZonenplanDaten.Profilzuordnung` (`null` ohne Raumprofile) |
| Planschritte | `GebaeudePlanschrittArt` | `PROFIL` (Räume, Nutzung = Profilschlüssel, leer = „keine“), `PROFIL_ZURUECK` (Räume), `VORSCHLAEGE`, `VORSCHLAEGE_ZURUECK` — sie laufen wie alle Schritte über die Anfrage und werden in ihrer Reihenfolge auf den Regelvorschlag gelegt |
| Hülle | `EPOS.UI.Daten/Bedarf/GebaeudeImportProfilzuordnung.cs` (neu) | `Daten(Zonenplan)` aus `Buendel()`, `Katalogbaum()`, `Raumprofilzuordnung.Zaehlen`; `Schritt(plan, s)` ruft `ProfilZuordnen`, `ProfilzuordnungZuruecknehmen`, `VorschlaegeUebernehmen`, `VorschlaegeZuruecknehmen`; eingehängt in `GebaeudeImportHuelle.Schritt` und `GebaeudeImportZonen.PlanDaten` |
| Ampel (F6) | Hülle | rot bei Rückfragen (offen oder ohne Kandidat), gelb bei unbestätigten Vorschlägen, sonst grün; keine Sperre am OK |
| Komponente | `EPOS.UI/Dialoge/Import/GebaeudeProfilzuordnung.razor` (neu) | links `Baumansicht` (Kategorie → Gruppe der Norm → Profil, Kennwerte Sollwert und Nutzungszeit als Kennzeichen) mit Suchfeld; rechts eine Tabelle mit den Hausblattklassen `epos-raster` (Bündel, Räume, Fläche min–max, Stand, Profil bzw. Vorschlag mit Herleitungszeile der Begründung, Zeilenknopf) |
| Importdialog | `GebaeudeImportDialog.razor` | Knopf „Nutzungsprofile zuordnen… (n offen)“ in der Knopfzeile über dem Zonenbaum, neben „Nutzungsprofile…“; Ampelbanner darunter; `Ueberlagerung` mit Titel; Esc schließt die Überlagerung zuerst |
| Stil | `EPOS.UI/wwwroot/epos-ui.css` | `.epos-gebimport-pz-*`: zwei Spalten, im schmalen Fenster untereinander; Katalog mit eigener Rollhöhe |
| Ressourcen | `Resource.resx`, `Resource.en-US.resx` | 34 Schlüssel `GIMP_PZ_*`, Designer erzeugt |

## 2. Bedienung (Quelle für die Wiki-Seite „Gebäudeimport“)

1. Datei einlesen. Trägt sie Raumarten (IFC mit `RoomType`), steht über dem Zonenbaum der Knopf **„Nutzungsprofile
   zuordnen…“**; die Zahl in Klammern nennt die Bündel, die noch eine Rückfrage brauchen. Darunter zeigt ein Hinweis den
   Stand: gelb bei unbestätigten Vorschlägen (der Import ist trotzdem möglich), rot bei Räumen ohne eindeutiges Profil.
2. Knopf drücken. Rechts stehen die Räume der Datei als **Bündel** gleicher Raumart und gleichen Namens, offene zuerst, mit
   Zahl der Räume, Flächenspanne, Stand und Profil bzw. Vorschlag samt Begründung. **„nur offene“** blendet alles aus, was
   schon ein Profil trägt.
3. Ein oder mehrere Bündel **ankreuzen**, dann links im **Katalog** ein Profil anklicken (Suche über Nummer oder Name). Die
   Wahl gilt für alle Räume der gewählten Bündel. Bei einer Rückfrage stehen die **Kandidaten** der gewählten Bündel oben als
   Schnellwahl; „keine Nutzung“ setzt bewusst kein Profil, „Zuordnung aufheben“ nimmt die Wahl von Hand zurück.
4. Einen einzelnen Vorschlag mit **„übernehmen“** in seiner Zeile bestätigen oder alle mit **„Alle Vorschläge übernehmen“**;
   **„Zurücknehmen“** stellt alle Wahlen von Hand zurück.
5. Nach jeder Aktion bildet sich der Zonenbaum neu (unter Regel Z7 wandern die Räume in die Zone ihres Profils). Kreuz oder
   Esc schließen die Überlagerung; der Import wird wie gewohnt mit OK abgeschlossen.

## 3. Tests und Abnahme

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler |
| Windows-Schale (`-p:EnableWindowsTargeting=true`) | siehe Abschnitt 4 |
| `GebaeudeProfilzuordnungDialogTests` (bunit, neu, 5) | grün: Bündel offene zuerst, Zuordnen mehrerer Bündel per Profilklick, „keine“ und „aufheben“, Kandidaten-Schnellwahl, Alle übernehmen/zurücknehmen, Filter und Suche |
| `GebaeudeProfilzuordnungHuelleTests` (neu, 2) | grün, Verwaltung mit Arbeitskopie der Testdatenbank: 113 Räume in 17 Bündeln; vorher 66 eindeutig · 4 Vorschlag · 43 Rückfrage · 0 ohne · 0 von Hand (Ampel rot, 1 offenes Bündel); nach „Alle Vorschläge übernehmen“ 66 · 0 · 43 · 0 · 4; nach Zuordnung der Rückfrage über den ersten Kandidaten 66 · 0 · 0 · 0 · 47, Ampel grün, kein Raum unzugeordnet; „Zurücknehmen“ gibt den Ausgangsstand; ein unbekannter Profilschlüssel wird abgelehnt. Derselbe Weg im Importdialog: Knopf mit Zähler, Überlagerung, Schritte `VORSCHLAEGE` und `PROFIL`, Zähler sinkt |
| Filter `GebaeudeImport|Profilzuordnung|Zonenbaum|Modellpruefung|Wache|Ueberlagerungstitel` in `EPOS.UI.Tests` | siehe Abschnitt 4 |
| `DokumentationLinkWache|RepositoryOrdnungWache` in `EPOS.Kern.Tests` | siehe Abschnitt 4 |

## 4. Abnahmezahlen

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Windows-Schale `WindowsFormsApplication1.csproj -c Debug -p:Platform=x64 -p:EnableWindowsTargeting=true`: 0 Fehler
  (21 Warnungen, Bestand).
- `EPOS.UI.Tests` mit Filter `GebaeudeImport|Profilzuordnung|Zonenbaum|Modellpruefung|Wache|Ueberlagerungstitel`: 286 bestanden,
  0 übersprungen. Dafür nachgezogen: `KiMaskenabdeckungWacheTests` (Wirt `GebaeudeImportDialog` und drei Eingabestellen der
  Überlagerung mit Grund), Kommentare ohne Formatnamen (`Im_Quelltext_der_Komponente_steht_kein_Formatname`).
- `EPOS.Kern.Tests` mit Filter `DokumentationLinkWache|RepositoryOrdnungWache`: 24 bestanden.

## 5. Offene Reste

1. **Handänderungen am Zonenbaum vor einer Profilwahl:** Der Kern bildet den Plan bei jeder Profiloperation nach seiner Regel
   neu; frühere Schritte von Hand (Umhängen, Umbenennen) gehen dabei verloren, spätere bleiben. Eine Rückfrage wie vor dem
   Regelwechsel fehlt noch.
2. **Einzelraum im Bündel:** Das Aufklappen eines Bündels auf seine Räume (Abweichung eines Einzelraums, Anzeige
   „gemischt“) ist in der Hülle vorbereitet (`Gemischt`, Raumkennungen je Bündel), im Dialog aber nicht gebaut.
3. Filter „nur Vorschläge“ und „nur beheizte“ des Entwurfs fehlen; „nur offene“ ist gebaut.
4. Infoknopf auf die Hilfeseite „Gebäudeimport“ und der Abschnitt der Wiki-Quelle (`Projekte/Wiki/`) stehen aus; Bedienung
   oben ist die Vorlage.
5. Die Zähler der Hüllenprobe hängen am Stand der Rangfolge aus ZZ-A; ändert der Kern-Rest (Gebäudeart, F7) die Zählung der
   Verwaltung, prüft der Test nur Beziehungen und Summen (113, Übernahme verschiebt Vorschläge nach „von Hand“), nicht die
   absoluten Zahlen.
6. **Neue Mitglieder aus dem Kern-Rest ZZ-A (Zweig `zon`, nach dem Abzweig von `zonb`):** `Planzone.Dateivorschlag` (Zonen mit
   Bezeichnung der Datei als `RAUMPROFIL_VORSCHLAG`, Herkunft „Bezeichnung der Datei“) und `SqprojZonenergebnis.Raumvorschlaege`
   (F7, Vorschläge für Räume ohne Zone im Projektdatei-Weg) gibt es auf `zonb` nicht, deshalb zeigt der Dialog sie nicht. Nach
   dem Merge: die Herkunft im Zonenbaum zeigen und in der Ampel zählen, die Raumvorschläge des Projektdatei-Wegs als Bündel
   oder als Hinweis zeigen.

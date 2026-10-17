# Protokoll ZZ-B — Zuordnungsdialog „Nutzungsprofile zuordnen…“ (Hülle und Dialog)

Grundlage: [Entwurf Zonenzuordnung IFC](../../2026-10-17_Entwurf_Zonenzuordnung_IFC.md),
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

## 5. Offene Reste und Nachzug

Nachzug auf `zonb` nach dem Merge mit `zon` (ZZ-A Kern samt Rest). Kein Schemaschritt.

1. **Handschritte vor einer Profiloperation — erledigt (Rückfrage).** Die vier Profiloperationen (`PROFIL`,
   `PROFIL_ZURUECK`, `VORSCHLAEGE`, `VORSCHLAEGE_ZURUECK`) gehen im Importdialog über `Profilschritt`: stehen nach der letzten
   Neubildung Schritte von Hand (`GebaeudePlanschrittArt.Gefaehrdet`: alle Schritte nach der letzten Profiloperation außer
   `HAKEN`, den der Plan durch die Neubildung trägt; `PROJEKTDATEI` und `RESTPROFIL` zählen mit), fragt der Dialog wie vor dem
   Regelwechsel (`Rueckfrage`, Vorgabe „Nein“, Texte `GIMP_PZ_FRAGE_TITEL`, `GIMP_PZ_FRAGE` mit Zahl); „Ja“ führt den Schritt aus,
   „Nein“ und Esc lassen den Plan. Frühere Schritte sind schon verworfen und lösen keine neue Frage aus.
   **Entscheid:** Unberührte Handschritte erhalten wird nicht gebaut. Die Schritte von Hand tragen Zonenschlüssel des Plans zu
   ihrer Zeit; die Neubildung vergibt neue Schlüssel und verschiebt unter Z7 Räume zwischen Zonen, sodass ein Wiedereinspielen
   eine Abbildung alter auf neue Zonen bräuchte (Umbau des Zonenplans). Die Rückfrage genügt für den Schutz.
2. **Einzelraum im Bündel — erledigt.** Jedes Bündel trägt `Raumzeilen` (Kennung, Name, Fläche, Stand, Profil); der Knopf
   „Räume einzeln“ (`GIMP_PZ_AUFKLAPPEN`) klappt auf, jeder Raum ist einzeln wählbar und bekommt mit Katalogklick oder
   Schnellwahl sein eigenes Profil (`ProfilZuordnen` mit der Raumliste). Gemischte Bündel stehen aufgeklappt und zeigen ihre
   Räume mit Profil.
3. **Filter — erledigt.** „nur Vorschläge“ (`GIMP_PZ_NUR_VORSCHLAEGE`) und „nur beheizte“ (`GIMP_PZ_NUR_BEHEIZTE`, Bündel mit
   mindestens einem beheizten Raum, `GebaeudeProfilbuendelDaten.Beheizt`) neben „nur offene“; sie wirken zusammen.
4. Infoknopf auf die Hilfeseite „Gebäudeimport“ und der Abschnitt der Wiki-Quelle (`Projekte/Wiki/`) stehen aus; Bedienung
   oben ist die Vorlage, ergänzt um Einzelraum, Filter und die zwei Hinweisblöcke.
5. Die Hüllenprobe prüft Beziehungen und Summen. Verwaltung nach dem Merge unverändert: 113 Räume in 17 Bündeln; vorher
   66 · 4 · 43 · 0 · 0 (Ampel rot), nach „Alle Vorschläge übernehmen“ 66 · 0 · 43 · 0 · 4, nach den Kandidaten 66 · 0 · 0 · 0 · 47
   (grün); die Datei trägt keine Zone mit Bezeichnung der Datei.
6. **Mitglieder aus ZZ-A — erledigt.**
   - (a) `Planzone.Dateivorschlag`: Die Hülle zählt jeden beheizten Raum einer solchen Zone als Vorschlag (gelb), solange er
     keine Wahl von Hand trägt, auch ohne Befund je Raum (gbXML); dann liefert die Hülle die Profilzuordnung auch ohne
     Raumprofile. Der Hinweisblock „Zonen mit Bezeichnung der Datei“ (`GIMP_PZ_DATEIZONEN_TITEL`) zeigt Zone, Räume,
     Fläche, Vorschlag und Begründung mit „übernehmen“ (Schritt `PROFIL`); „Alle Vorschläge übernehmen“ bestätigt sie (Kern).
   - (b) `SqprojZonenergebnis.Raumvorschlaege`: Im Projektdatei-Weg (eine Zone mit `Projektdatei.Zonierung`) zeigt der
     Hinweisblock „Räume ohne Zone der Projektdatei“ (`GIMP_PZ_RESTRAEUME_TITEL`, `GIMP_PZ_RESTRAEUME_HINWEIS`) je Raum den
     Befund aus `Zonenplan.Restvorschlaege()` mit Vorschlag bzw. Kandidaten und Begründung; nie automatisch. Der Kern bot dafür
     nur `Zuordnen` in eine bestehende Zone und `ProfilzoneAnlegen` getrennt — eine Profiloperation hätte die Zonen der
     Projektdatei durch die Regelbildung ersetzt. **Neu im Kern:** `Zonenplan.InProfilzoneZuordnen(raeume, profilschluessel)`
     ordnet ohne Neubildung der ersten Zone mit dem Profil und gleicher Beheizung zu, sonst einer neuen Zone mit dem
     Profilnamen (alle oder keiner). Schritt `RESTPROFIL`, Knopf „in Zone „…““ (`GIMP_PZ_IN_ZONE`), ohne Rückfrage, weil er
     nichts neu bildet. Test in `SqprojRaumvorschlagTests`.

Neue Ressourcen (de/en, Designer erzeugt): `GIMP_PZ_NUR_VORSCHLAEGE`, `GIMP_PZ_NUR_BEHEIZTE`, `GIMP_PZ_AUFKLAPPEN`,
`GIMP_PZ_DATEIZONEN_TITEL`, `GIMP_PZ_RESTRAEUME_TITEL`, `GIMP_PZ_RESTRAEUME_HINWEIS`, `GIMP_PZ_IN_ZONE`, `GIMP_PZ_FRAGE_TITEL`,
`GIMP_PZ_FRAGE`.

Neue Tests: `SqprojRaumvorschlagTests.Raum_ohne_Zone_geht_in_die_Zone_seines_Profils_ohne_Neubildung` und die Hülle im
bestehenden F7-Fall; `GebaeudeProfilzuordnungDialogTests` (Einzelraum und gemischte Bündel, Filter, Hinweisblöcke);
`GebaeudeProfilzuordnungHuelleTests` (Raumzeilen, gemischtes Bündel nach Einzelwahl, Rückfrage im Importdialog mit „Nein“ und
„Ja“, `Gefaehrdet`).

Abnahme des Nachzugs:

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- Windows-Schale `WindowsFormsApplication1.csproj -c Debug -p:Platform=x64 -p:EnableWindowsTargeting=true`: 0 Fehler.
- `EPOS.UI.Tests` mit Filter `GebaeudeImport|Profilzuordnung|Zonenbaum|Modellpruefung|Wache|Ueberlagerungstitel`: 290 bestanden,
  0 übersprungen. Nachgezogen: `KiMaskenabdeckungWacheTests` (`GebaeudeProfilzuordnung` 3 → 6 Eingabestellen: zwei Filter und
  der Haken je Raum, Vermerk erweitert).
- `EPOS.Kern.Tests` mit Filter `Zonenplan|Raumprofil|Sqproj|Zonierung|Quellenprobe|DokumentationLinkWache|RepositoryOrdnungWache`:
  339 bestanden, 0 rot.

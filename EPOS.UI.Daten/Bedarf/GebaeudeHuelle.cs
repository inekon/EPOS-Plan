using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der GEBÄUDE eines Projekts (<c>GebaeudeDialog</c>) — seit Stufe G1 in
    /// <c>EPOS.UI.Daten</c> (Umsetzungskonzept Gebäudesimulation 2.8, Entscheid E27/A10).
    /// Das Fenster bleibt in der Windows-Schale (<c>GebaeudeFenster</c>); die Hülle führt
    /// keinen <c>IWin32Window</c> mehr — er wurde nur an die Katalogdialoge weitergereicht,
    /// die Überlagerungen sind, und an die beiden Wege der Naht <see cref="Gebaeudewege"/>.
    ///
    /// <para><b>Die Liste wird GETEILT, nicht kopiert.</b> Die <c>List&lt;Z_ProjGebModel&gt;</c>
    /// gehört dem Aufrufer; die Hülle baut sie nach jeder Änderung AN ORT UND STELLE aus der
    /// Anzeigeliste neu auf — der einzige Weg, der auch im Assistenten trägt.</para>
    ///
    /// <para><b>Vier Unterdialoge, vier Überlagerungen.</b> Katalogeditor, Skalierungsdialog,
    /// Gebäudetypen-Verwaltung und Wärmebedarf erscheinen IM selben Fenster; die Hülle reicht
    /// dafür nur die Parametersätze durch. Der Gebäudeimport (Stufe G4) kommt als fünfte dazu —
    /// sein Weg (<see cref="GebaeudeImportweg"/>) hält die ausstehende Herkunft einer neuen Zeile,
    /// bis die Liste gespeichert wird.</para>
    ///
    /// <para><b>Stufe G1 (Konzept 2.7):</b> Jede Projektzeile trägt den Rechenweg als
    /// Anzeigetext und den Wärmeleitwert H_ges — beides so, wie der Kern rechnet
    /// (<see cref="Gebaeuderechenweg"/>, <see cref="Gebaeudehuellbilanz"/>).</para>
    /// </summary>
    internal static class GebaeudeHuelle
    {
        /// <summary>
        /// Die vorläufige Id einer noch nicht gespeicherten Zuordnung — derselbe
        /// Startwert wie <c>Form_Gebaeude.startindex</c>.
        /// </summary>
        internal const int STARTINDEX = 100000;

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        internal static IReadOnlyDictionary<string, object> Gaben(
            int projektId, string projektName,
            List<Z_ProjGebModel> modelle, bool wizard)
        {
            // Stufe G4, Welle 4: die AUSSTEHENDEN Herkuenfte der Zeilen aus dem Gebaeudeimport,
            // je undurchsichtigem Schluessel der Zeile. Die Kern-Daten bleiben hier; NachModell
            // legt sie an das Modell, und erst der Speicherweg schreibt sie an die neue Kopie.
            // Ein Modell, das seine Herkunft noch traegt (Assistent: dieselbe Liste ueber mehrere
            // Seitenbesuche), bekommt beim Neuaufbau wieder einen Schluessel.
            var ausstehend = new Dictionary<string, GebaeudeImportHerkunft>(StringComparer.Ordinal);

            var zeilen = new List<GebaeudeProjektZeile>();
            foreach (Z_ProjGebModel m in modelle)
            {
                GebaeudeProjektZeile z = AusModell(m);
                KennwerteSetzen(z, projektId);
                if (m.Importherkunft != null) z.Herkunftsschluessel = Vormerken(ausstehend, m.Importherkunft);
                zeilen.Add(z);
            }

            int[] naechsteId = { STARTINDEX };

            // Stufe G6a: der benannte Grund der letzten Bedarfsauskunft ohne Zahl.
            string bedarfBefund = null;

            // Welche Anzeigezeile gehoert zu welchem Modell der Fachliste? Der Speicherweg
            // traegt einer neu angelegten Zeile nach dem Festschreiben ihre ECHTE
            // Zuordnungs-Id ins Modell ein (WizardCtrl.EchteIdsUebernehmen); die
            // Anzeigezeile zieht sie hier nach, bevor die Fachliste neu entsteht - sonst
            // kaeme die vorlaeufige Id zurueck, und ein zweites Speichern legte die Kopie
            // erneut an.
            var paare = new List<(Z_ProjGebModel Modell, GebaeudeProjektZeile Zeile)>();
            for (int i = 0; i < zeilen.Count; i++) paare.Add((modelle[i], zeilen[i]));

            Action idsNachziehen = () =>
            {
                foreach ((Z_ProjGebModel m, GebaeudeProjektZeile z) in paare)
                    if (z.IdZ >= STARTINDEX && m.ID_Z > 0 && m.ID_Z != z.IdZ) z.IdZ = m.ID_Z;
            };

            // Die Fachliste wird nach jeder Aenderung AN ORT UND STELLE neu aufgebaut -
            // dieselbe Liste, neue Zeilen. Der Assistent reicht dasselbe Objekt ueber
            // mehrere Seitenbesuche hinweg.
            Action geaendert = () =>
            {
                idsNachziehen();
                modelle.Clear();
                paare.Clear();
                foreach (GebaeudeProjektZeile z in zeilen)
                {
                    Z_ProjGebModel m = NachModell(z, projektId, ausstehend);
                    modelle.Add(m);
                    paare.Add((m, z));
                }
            };

            var gaben = new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,
                ["Geaendert"] = geaendert,

                // Stufe G3, Welle K: der Katalog ist die Katalogliste des Hauses - dieselben
                // Zeilen und dasselbe Profil wie die Gebaeudeverwaltung (GebaeudeAdminHuelle);
                // den Filterstand holt der Dialog aus dem Register, geteilt mit ihr.
                ["Katalogzeilen"] = new Func<IReadOnlyList<Katalogfilterzeile>>(
                    () => GebaeudeStammCtrl.Katalogfilterzeilen()),
                ["Katalogprofil"] = Katalogfilterprofil.FuerGebaeude(s => Text_(s, s)),
                ["StammDetail"] = new Func<string, GebaeudeStammDetail>(Stammdetail),
                ["StammSatz"] = new Func<string, GebaeudeProjektZeile>(
                    name => Aufnehmen(name, projektId, naechsteId)),
                // Die Loeschsperre der Gebaeudeverwaltung gilt auch hier - eine Wahrheit im
                // Kern (GebaeudeStammCtrl.Loeschsperrgrund): Auslieferungssatz oder von einem
                // Projekt gefuehrt heisst benannte Absage statt Rueckfrage. Geloescht wird
                // ueber GebaeudeStammCtrl.Loeschen, das dieselbe Sperre noch einmal haelt und
                // keinen Meldungskasten oeffnet.
                ["KatalogLoeschsperre"] = new Func<string, string>(GebaeudeStammCtrl.Loeschsperrgrund),
                ["KatalogLoeschen"] = new Func<string, bool>(GebaeudeStammCtrl.Loeschen),
                ["MeldungLoeschFehler"] = Text_("BADM_MSG_LOESCHEN_FEHLER",
                    "Der Datensatz konnte nicht aus der Datenbank gelöscht werden."),

                ["KatalogGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => GebaeudeKatalogHuelle.Gaben(name,
                        string.IsNullOrEmpty(name)
                            ? GebaeudeKatalogModus.Neu : GebaeudeKatalogModus.Bearbeiten)),

                // Stufe G4, Welle 4 (A17): der Gebaeudeimport - je Klick ein neuer Weg.
                ["ImportGaben"] = new Func<GebaeudeImportweg>(
                    () => Importweg(projektId, naechsteId, ausstehend, Vorgemerkt(zeilen, ausstehend))),
                ["BtnImportText"] = Text_("GEB_BTN_IMPORT", "Importieren (gbXML, IFC)…"),
                ["BtnImportHinweis"] = Text_("GEB_BTN_IMPORT_HINWEIS",
                    "Ein Gebäude aus einer gbXML- oder IFC-Datei als neuen Katalogsatz anlegen und in die Projektliste übernehmen"),
                ["MeldungImportAufgenommen"] = Text_("GEB_MSG_IMPORT_AUFGENOMMEN",
                    "Das Gebäude „{0}“ steht jetzt im Katalog und in der Projektliste."),

                // Nacharbeit G4b: die gemerkten Baustoff-Zuordnungen des Projekts ansehen und einzelne
                // entfernen - je Klick neu gelesen; die Ansicht schreibt mit ihrem eigenen OK.
                ["BaustoffzuordnungenGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => BaustoffzuordnungenHuelle.Gaben(projektId)),

                ["WohnflaecheGaben"] = new Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>(
                    Wohnflaechengaben),

                // Stufe G3, Welle D2: "Huelle und Zonen..." - der Editor der PROJEKTKOPIE samt Zonen.
                // Eine Zeile ohne Projektkopie (eben aufgenommen) hat keinen Parametersatz.
                ["ProjektGaben"] = new Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>(
                    z => { idsNachziehen(); return z == null || !z.HatProjektkopie ? null : GebaeudeKatalogHuelle.ProjektGaben(projektId, z.IdZ); }),
                ["ZeileAuffrischen"] = new Action<GebaeudeProjektZeile>(z => KennwerteSetzen(z, projektId)),

                // Die Gebaeudetypen-Verwaltung liegt noch in der Windows-Schale - ein
                // Haken der Naht (Gebaeudewege); ohne ihn kein Knopf.
                ["GebaeudetypGaben"] = Gebaeudewege.GebaeudetypGaben,

                // Anwenderwunsch W9-E-2 (05.09.2026): der Waermebedarf GENAU DIESES
                // Gebaeudes. Die Katalogverwaltung ist seit Stufe 5 der Neuordnung eine
                // eigene Komponente (GebaeudeAdminHuelle) und kennt diesen Weg nicht.
                ["BedarfGaben"] = new Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>(
                    z => { idsNachziehen(); return GebaeudeBedarfHuelle.Gaben(z, projektId, out bedarfBefund); }),
                // Stufe G6a: der benannte Grund des letzten Aufrufs ohne Zahl (etwa mehrere Zonen).
                ["BedarfBefund"] = new Func<string>(() => bedarfBefund),
                ["MeldungKeinBedarfGrund"] = Text_("GEB_MSG_KEIN_BEDARF_GRUND",
                    "Für dieses Gebäude lässt sich kein Wärmebedarf berechnen: {0}"),
                // Stufe G6a (Anwenderentscheid A2): „Aus dem Projekt entfernen" fragt bei Zonen nach.
                ["FrageAusProjekt"] = MyResource.Resource.GEBZ_FRAGE_AUS_PROJEKT,

                ["TitelText"] = Titel(),
                ["KopfbandText"] = Text_("GEB_KOPFBAND", "Eingabe der Energiedaten"),
                ["LabelProjektliste"] =
                    Text_("GEB_LBL_PROJEKTLISTE", "ausgewählte Gebäude im Projekt:"),
                ["LabelKatalog"] = Text_("GEB_LBL_KATALOG", "Gebäude in DB:"),
                ["GruppeVerbrauch"] = Text_("GEB_GRP_VERBRAUCH", "Gebäude: Verbrauch"),
                ["LabelGebaeudeart"] = Text_("GEB_LBL_GEBAEUDEART", "Gebäudeart"),
                ["LabelGebaeudename"] = Text_("GEB_LBL_GEBAEUDENAME", "Gebäudename:"),
                ["LabelBeschreibung"] = Text_("GEB_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelWohnflaeche"] = Text_("GEB_LBL_WOHNFLAECHE", "Nutzfläche:"),
                ["LabelEinheit"] = Text_("GEBW_LBL_ART_ANGABE", "Art der Angabe:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                // Stufe G1 (Konzept 2.7): Spalte und Detailkennzahlen des Rechenwegs.
                ["SpalteRechenweg"] = Text_("GEB_SP_RECHENWEG", "Rechenweg"),
                ["LabelHges"] = Text_("GEB_LBL_HGES", "Wärmeleitwert H_ges:"),
                ["LabelRechenweg"] = Text_("GEB_LBL_RECHENWEG", "Rechenweg:"),

                // Befund W9-B-3 (Windows-Abnahme 05.09.2026): Die zwei Pfeile
                // trugen bis hierher nur ihr Zeichen. Beschriftung UND Kurztext
                // kommen jetzt aus dem Ressourcenkatalog, beide Sprachen.
                // Entscheid #76 vom selben Tag: Das ZEICHEN steht nicht mehr im
                // Text - es haengt an der Anordnung und kommt aus dem Baustein.
                ["BtnHinzuText"] = Text_("GEB_BTN_UEBERNEHMEN", "In das Projekt übernehmen"),
                ["BtnHinzuHinweis"] = Text_("GEB_BTN_UEBERNEHMEN_HINWEIS",
                    "Das in „Gebäude in DB“ markierte Gebäude in die Projektliste übernehmen"),
                ["BtnEntfernenText"] = Text_("GEB_BTN_ENTFERNEN", "Aus dem Projekt entfernen"),
                ["BtnEntfernenHinweis"] = Text_("GEB_BTN_ENTFERNEN_HINWEIS",
                    "Das in der Projektliste markierte Gebäude aus dem Projekt entfernen"),
                ["BtnAendernText"] = Text_("GEB_BTN_AENDERN", "Fläche und Verbrauch…"),
                ["BtnSimulationText"] = Text_("GEB_BTN_SIMULATION", "Simulation..."),
                ["BtnSimulationHinweis"] = Text_("GEB_BTN_SIMULATION_HINWEIS",
                    "Den Wärmebedarf des in der Projektliste markierten Gebäudes "
                    + "berechnen und anzeigen"),
                ["BtnDbAendernText"] = Text_("GEB_BTN_DB_AENDERN", "Gebäude in DB ändern..."),
                ["BtnDbNeuText"] = Text_("GEB_BTN_DB_NEU", "Gebäude in DB neu..."),
                ["BtnDbLoeschenText"] = Text_("GEB_BTN_DB_LOESCHEN", "Gebäude in DB löschen"),
                ["BtnGebTypText"] = Text_("GEB_BTN_GEBTYP", "Gebäudetyp in DB ändern..."),

                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["JaText"] = MyResource.Resource.ALLG_BTN_JA,
                ["NeinText"] = MyResource.Resource.ALLG_BTN_NEIN,

                ["MeldungLoeschfrage"] =
                    Text_("GEB_MSG_LOESCHFRAGE", "Soll {0} wirklich gelöscht werden ?"),
                ["MeldungGeloescht"] = Text_("GEB_MSG_GELOESCHT", "Gebäude gelöscht!"),
                ["MeldungKeineWahl"] = Text_("GEB_MSG_KEINE_WAHL", "Gebäude in DB auswählen!"),
                ["MeldungKeinBedarf"] = Text_("GEB_MSG_KEIN_BEDARF",
                    "Für dieses Gebäude lässt sich kein Wärmebedarf berechnen. "
                    + "Bitte das Projekt speichern und eine Klimaregion auswählen."),

                ["HilfeSchluessel"] = "Form_Gebaeude.btn_Help"
            };

            // Stufe G7a (Welle W3): der Gebaeudeexport im Format gbXML - nur bei angeschaltetem
            // Freigabeschalter (vor einer Auslieferung aus); ohne Delegat kein Knopf. Eine Zeile ohne
            // Projektkopie bekommt keinen Satz, der Dialog meldet dann den Grund.
            if (GebaeudeExportRegeln.GbxmlExportFreigegeben)
            {
                gaben["ExportGaben"] = new Func<GebaeudeProjektZeile, bool, IReadOnlyDictionary<string, object>>(
                    (z, geaendert) => { idsNachziehen(); return GebaeudeExportHuelle.Gaben(projektId, z, geaendert); });
                gaben["BtnExportText"] = GebaeudeExportHuelle.Knopftext();
            }
            return gaben;
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        private static GebaeudeStammDetail Stammdetail(string name)
        {
            GebaeudeModel m = new GebaeudeStammCtrl().Lies(name);
            if (m == null) return null;

            return new GebaeudeStammDetail(m.Gebaeudename ?? "", m.Gebaeudeart ?? "",
                                           m.Beschreibung ?? "",
                                           m.Wohnflaeche_gesamt.ToString("F2"),
                                           Rechenwegtext(m.Gebaeude_Modell),
                                           Gebaeudehuellbilanz.GesamtWK(m));
        }

        /// <summary>
        /// „◀" — aus einem Katalogsatz entsteht eine Projektzeile mit den Vorbelegungen aus
        /// <c>btn_Hinzu_Click</c>:245-255: Einheit „Wohnfläche [m²]", Jahresnutzungsgrad 1,
        /// keine dezentrale Warmwasserbereitung. Die Projektkopie legt erst der
        /// Speicherweg an (<c>WizardCtrl</c>), nicht dieser Knopf.
        /// </summary>
        private static GebaeudeProjektZeile Aufnehmen(string name, int projektId, int[] naechsteId)
        {
            GebaeudeModel m = new GebaeudeStammCtrl().Lies(name);
            if (m == null) return null;

            return new GebaeudeProjektZeile
            {
                IdZ = naechsteId[0]++,      // noch nicht gespeichert, also noch unbekannt
                IdGebaeude = m.ID,
                IdKatalog = m.ID > 0 ? m.ID : (int?)null,
                Name = m.Gebaeudename ?? "",
                Art = m.Typ ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Baualtersklasse = m.Baualtersklasse ?? "",
                Wohnflaeche = m.Wohnflaeche_gesamt,
                Einheit = "Wohnfläche [m²]",
                Jahresnutzungsgrad = 1,
                DezentralWarmwasser = false,
                // Die Projektkopie entsteht erst beim Speichern - bis dahin gelten die
                // Werte des Katalogsatzes, aus dem sie entsteht.
                Rechenweg = Rechenwegtext(m.Gebaeude_Modell),
                HgesWK = Gebaeudehuellbilanz.GesamtWK(m)
            };
        }

        // =================================================================================
        // Stufe G4, Welle 4: der Gebaeudeimport (A17)
        // =================================================================================

        /// <summary>
        /// <b>Der Weg EINES Gebäudeimports</b> — je Klick auf „Importieren…" eine neue
        /// <see cref="GebaeudeImportHuelle"/> mit Profil nach Dateiwahl und dem Projekt für den
        /// Hinweis „schon importiert".
        ///
        /// <list type="number">
        /// <item><b>Übernehmen</b> (OK des Zuordnungsdialogs): derselbe Satz wie an der Prüfung,
        /// ein im Katalog schon vergebener Name ist die benannte Absage (der Dialog bleibt offen);
        /// sonst entstehen die Vorbelegung des Editors
        /// (<see cref="GebaeudeImportHuelle.Vorbelegung"/>) und die ausstehende Herkunft. Geschrieben
        /// wird nichts.</item>
        /// <item><b>Editor</b>: der Katalogeditor im Modus Neu, vorbelegt; sein Schreibweg ist der
        /// gewöhnliche (<see cref="GebaeudeKatalogHuelle.Schreiben"/>) — die Hülle merkt sich nur
        /// den Namen, unter dem er angelegt hat.</item>
        /// <item><b>Aufnehmen</b> (der Editor hat gespeichert): die neue Projektzeile über
        /// denselben Weg wie „In das Projekt übernehmen" (<see cref="Aufnehmen(string, int, int[])"/>),
        /// dazu der Schlüssel der ausstehenden Herkunft. Ohne Speichern im Editor keine Zeile, die
        /// Herkunft verfällt.</item>
        /// </list>
        /// </summary>
        private static GebaeudeImportweg Importweg(int projektId, int[] naechsteId,
                                                   Dictionary<string, GebaeudeImportHerkunft> ausstehend,
                                                   IReadOnlyDictionary<string, int?> vorgemerkt = null)
        {
            var import = new GebaeudeImportHuelle(projektId) { Vorgemerkt = vorgemerkt };
            GebaeudeVorbelegung vorbelegung = null;
            GebaeudeImportHerkunft herkunft = null;
            string angelegt = null;

            Func<GebaeudeImportErgebnis, Task<string>> uebernehmen = ergebnis =>
            {
                vorbelegung = null;
                herkunft = null;
                angelegt = null;
                if (import.SatzAusErgebnis(ergebnis) == null || import.Quelle == null)
                    return Task.FromResult(MyResource.Resource.GIMP_DLG_NICHT_GELESEN);

                string name = (ergebnis.Gebaeudename ?? "").Trim();
                if (name.Length > 0 && GebaeudeKatalogHuelle.Laden(name) != null)
                    return Task.FromResult(Text_("GEBK_MSG_NAME_VERGEBEN",
                        "Ein Gebäude mit diesem Namen steht schon im Katalog."));

                vorbelegung = import.Vorbelegung(ergebnis);
                herkunft = import.Herkunft;
                return Task.FromResult<string>(null);
            };

            Func<IReadOnlyDictionary<string, object>> editorGaben = () =>
            {
                if (vorbelegung == null) return null;
                var gaben = new Dictionary<string, object>(
                    GebaeudeKatalogHuelle.Gaben("", GebaeudeKatalogModus.Neu, vorbelegung));

                // Der Schreibweg bleibt der des Editors; gemerkt wird nur, unter welchem Namen
                // er angelegt hat - danach sucht "Aufnehmen" den neuen Katalogsatz.
                if (gaben.TryGetValue("Speichern", out object weg) &&
                    weg is Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis> speichern)
                    gaben["Speichern"] = new Func<GebaeudeKatalogDaten, bool, string, GebaeudeKatalogErgebnis>(
                        (daten, neu, bezeichner) =>
                        {
                            GebaeudeKatalogErgebnis e = speichern(daten, neu, bezeichner);
                            if (e != null && e.Erfolg && neu) angelegt = daten?.Name;
                            return e;
                        });
                return gaben;
            };

            Func<GebaeudeProjektZeile> aufnehmen = () =>
            {
                if (string.IsNullOrEmpty(angelegt) || herkunft == null) return null;
                GebaeudeProjektZeile zeile = Aufnehmen(angelegt, projektId, naechsteId);
                if (zeile == null) return null;
                zeile.Herkunftsschluessel = Vormerken(ausstehend, herkunft);
                herkunft = null;
                angelegt = null;
                return zeile;
            };

            return new GebaeudeImportweg(import.Gaben(uebernehmen), editorGaben, aufnehmen);
        }

        /// <summary>
        /// Die Baustoffzuordnungen der Importe, die noch in der Liste auf das Speichern warten — in der
        /// Reihenfolge der Zeilen, eine spätere gilt vor einer früheren. Ein weiterer Import derselben
        /// Liste sieht sie wie gemerkte; gespeichert wird jede mit ihrer Zeile. Gebildet wird je Klick auf
        /// „Importieren…" aus den Zeilen, die dann noch in der Liste stehen: Eine vor dem Speichern wieder
        /// entfernte Importzeile nimmt ihre Zuordnungen mit, auch wenn ihre Herkunft in
        /// <paramref name="ausstehend"/> liegen bleibt.
        /// </summary>
        private static IReadOnlyDictionary<string, int?> Vorgemerkt(IEnumerable<GebaeudeProjektZeile> zeilen,
                                                                    IReadOnlyDictionary<string, GebaeudeImportHerkunft> ausstehend)
        {
            var vorgemerkt = new Dictionary<string, int?>(StringComparer.Ordinal);
            foreach (GebaeudeProjektZeile z in zeilen)
                if (z?.Herkunftsschluessel != null && ausstehend.TryGetValue(z.Herkunftsschluessel, out GebaeudeImportHerkunft h)
                    && h?.Baustoffzuordnungen != null)
                    foreach (KeyValuePair<string, int?> paar in h.Baustoffzuordnungen) vorgemerkt[paar.Key] = paar.Value;
            return vorgemerkt.Count == 0 ? null : vorgemerkt;
        }

        /// <summary>Merkt eine ausstehende Herkunft unter einem neuen, undurchsichtigen Schlüssel vor.</summary>
        private static string Vormerken(Dictionary<string, GebaeudeImportHerkunft> ausstehend, GebaeudeImportHerkunft herkunft)
        {
            string schluessel = "import-" + Guid.NewGuid().ToString("N");
            ausstehend[schluessel] = herkunft;
            return schluessel;
        }

        /// <summary>
        /// Der Parametersatz der Wohnflächenangabe zu EINER Zeile. Das Feld „Baualtersklasse" dort
        /// zeigt den KLARTEXT der Klasse in der Sprache der Oberfläche, die Zeile führt den Buchstaben
        /// (<c>btn_Aendern_Click</c>:430-434); ohne Klasse bleibt es leer (E47).
        /// </summary>
        private static IReadOnlyDictionary<string, object> Wohnflaechengaben(GebaeudeProjektZeile z)
        {
            var modell = new Z_ProjGebModel
            {
                Gebaeudename = z.Name,
                Beschreibung = z.Beschreibung,
                Gebaeudeart = z.Art,
                Wohnflaeche = z.Wohnflaeche,
                Einheit = z.Einheit,
                Jahresnutzungsgrad = z.Jahresnutzungsgrad,
                DezentralWarmwasser = z.DezentralWarmwasser
            };

            string baujahr = Gebaeudeklassen.Text(z.Baualtersklasse);

            // Stufe G3 (Welle D2): Mit Zone entfaellt die Hochrechnung ueber die Angabe.
            return new Dictionary<string, object>(GebaeudeWohnflaecheHuelle.Gaben(modell, baujahr))
            {
                ["Zone"] = z.Zone ?? ""
            };
        }

        // =================================================================================
        // Stufe G1: Rechenweg und Waermeleitwert je Zeile (Konzept 2.7)
        // =================================================================================

        /// <summary>
        /// Der Rechenweg als ANZEIGETEXT — so, wie die Weiche für diesen Spaltenwert rechnet
        /// (<see cref="Gebaeuderechenweg.Wirksam"/>). Auf dem Altweg heißt er
        /// „Tagesbilanz (Bestandsweg)" (ADR-006). Ohne Angabe am Gebäude (NULL) trägt der
        /// Text den Zusatz „(Vorgabe)", wenn <paramref name="vorgabe"/> gesetzt ist.
        /// </summary>
        internal static string Rechenwegtext(string modell, bool vorgabe = true)
        {
            string text = Gebaeuderechenweg.IstVdi6007(modell)
                ? Text_("GEB_RECHENWEG_VDI6007", "VDI 6007")
                : Text_("GEB_RECHENWEG_TAGESBILANZ", "Tagesbilanz (Bestandsweg)");
            if (vorgabe && modell == null)
                text = Text_("GEB_RECHENWEG_VORGABE", "{0} (Vorgabe)").Replace("{0}", text);
            return text;
        }

        /// <summary>
        /// Rechenweg und H_ges einer Projektzeile aus der PROJEKTKOPIE (<c>Tab_Gebaeude</c>),
        /// so, wie der Lauf sie liest. Eine noch nicht gespeicherte Zeile hat keine Kopie —
        /// dann bleibt es beim Stand, den <c>Aufnehmen</c> aus dem Katalogsatz gesetzt hat.
        /// </summary>
        private static void KennwerteSetzen(GebaeudeProjektZeile z, int projektId)
        {
            if (projektId <= 0 || z.IdZ <= 0 || z.IdZ >= STARTINDEX) return;

            ProjektGebaeudeModel g = GebaeudeBedarfCtrl.Projektgebaeude(projektId, z.IdZ);
            if (g == null) return;

            z.Rechenweg = Rechenwegtext(g.Gebaeude_Modell);
            z.HgesWK = Gebaeudehuellbilanz.GesamtWK(g);
            // Stufe G3 (Welle D2): die Projektkopie traegt Zonen - der Name der Zone, ueber die sie rechnet;
            // ab zwei Zonen ihre Zahl (G6a), dazu die Zahl der Bauteile fuer die Rueckfrage beim Entfernen.
            z.HatProjektkopie = true;
            int zonen = g.Zonen?.Count ?? 0;
            z.Zonenzahl = zonen;
            z.Bauteilzahl = zonen == 0 ? 0 : g.Zonen.Sum(x => x?.Bauteile?.Count ?? 0);
            z.Zone = zonen == 0 ? null
                : zonen == 1 ? g.Zonen[0].Bezeichnung
                : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.GEBZ_ZONEN_ZAHL, zonen);
        }

        // =================================================================================
        // Abbildung Zeile <-> Modell
        // =================================================================================

        internal static GebaeudeProjektZeile AusModell(Z_ProjGebModel m)
        {
            return new GebaeudeProjektZeile
            {
                IdZ = m.ID_Z,
                IdGebaeude = m.ID_Gebaeude,
                IdKatalog = m.ID_Gebaeude_Stamm,
                Name = m.Gebaeudename ?? "",
                Art = m.Gebaeudeart ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Baualtersklasse = m.Baualtersklasse ?? "",
                Wohnflaeche = m.Wohnflaeche,
                Einheit = m.Einheit ?? "",
                Jahresnutzungsgrad = m.Jahresnutzungsgrad,
                DezentralWarmwasser = m.DezentralWarmwasser
            };
        }

        /// <summary>
        /// Zeile → Modell. Trägt die Zeile den Schlüssel einer ausstehenden Herkunft (Stufe G4,
        /// Welle 4), legt <paramref name="ausstehend"/> sie an das Modell
        /// (<see cref="Z_ProjGebModel.Importherkunft"/>); der Speicherweg schreibt sie dann an die
        /// neue Projektkopie. Ohne Schlüssel oder ohne Eintrag bleibt sie <c>null</c>.
        /// </summary>
        internal static Z_ProjGebModel NachModell(GebaeudeProjektZeile z, int projektId,
                                                  IReadOnlyDictionary<string, GebaeudeImportHerkunft> ausstehend = null)
        {
            GebaeudeImportHerkunft herkunft = null;
            if (z.Herkunftsschluessel != null && ausstehend != null)
                ausstehend.TryGetValue(z.Herkunftsschluessel, out herkunft);

            return new Z_ProjGebModel
            {
                ID_Z = z.IdZ,
                ID_Projekt = projektId,
                ID_Gebaeude = z.IdGebaeude,
                ID_Gebaeude_Stamm = z.IdKatalog,
                Gebaeudename = z.Name,
                Gebaeudeart = z.Art,
                Beschreibung = z.Beschreibung,
                Baualtersklasse = z.Baualtersklasse,
                Wohnflaeche = z.Wohnflaeche,
                Einheit = z.Einheit,
                Jahresnutzungsgrad = z.Jahresnutzungsgrad,
                DezentralWarmwasser = z.DezentralWarmwasser,
                Importherkunft = herkunft
            };
        }

        internal static string Titel()
        {
            return Text_("GEB_TITEL", "Eingabe der Gebäudedaten");
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}

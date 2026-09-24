using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

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
    /// dafür nur die Parametersätze durch.</para>
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
        private const int STARTINDEX = 100000;

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        internal static IReadOnlyDictionary<string, object> Gaben(
            int projektId, string projektName,
            List<Z_ProjGebModel> modelle, bool wizard)
        {
            var zeilen = new List<GebaeudeProjektZeile>();
            foreach (Z_ProjGebModel m in modelle)
            {
                GebaeudeProjektZeile z = AusModell(m);
                KennwerteSetzen(z, projektId);
                zeilen.Add(z);
            }

            int[] naechsteId = { STARTINDEX };

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
                    Z_ProjGebModel m = NachModell(z, projektId);
                    modelle.Add(m);
                    paare.Add((m, z));
                }
            };

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,
                ["Geaendert"] = geaendert,

                ["Katalog"] = new Func<bool, string, int?, bool, IReadOnlyList<GebaeudeKatalogZeile>>(
                    (wohn, art, klasse, ausBaujahr) => Katalogzeilen(wohn, art, klasse, ausBaujahr)),
                ["Gebaeudearten"] = new Func<bool, IReadOnlyList<string>>(
                    wohn => GebaeudeStammCtrl.Gebaeudearten(wohn)),
                ["Baualtersklassen"] = GebaeudeStammCtrl.Baualtersklassen(),
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

                ["WohnflaecheGaben"] = new Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>(
                    Wohnflaechengaben),

                // Die Gebaeudetypen-Verwaltung liegt noch in der Windows-Schale - ein
                // Haken der Naht (Gebaeudewege); ohne ihn kein Knopf.
                ["GebaeudetypGaben"] = Gebaeudewege.GebaeudetypGaben,

                // Anwenderwunsch W9-E-2 (05.09.2026): der Waermebedarf GENAU DIESES
                // Gebaeudes. Die Katalogverwaltung ist seit Stufe 5 der Neuordnung eine
                // eigene Komponente (GebaeudeAdminHuelle) und kennt diesen Weg nicht.
                ["BedarfGaben"] = new Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>(
                    z => { idsNachziehen(); return GebaeudeBedarfHuelle.Gaben(z, projektId); }),

                ["TitelText"] = Titel(),
                ["KopfbandText"] = Text_("GEB_KOPFBAND", "Eingabe der Energiedaten"),
                ["LabelProjektliste"] =
                    Text_("GEB_LBL_PROJEKTLISTE", "ausgewählte Gebäude im Projekt:"),
                ["LabelKatalog"] = Text_("GEB_LBL_KATALOG", "Gebäude in DB:"),
                ["GruppeFilter"] = Text_("GEB_GRP_FILTER", "Filter Gebäude DB"),
                ["GruppeVerbrauch"] = Text_("GEB_GRP_VERBRAUCH", "Gebäude: Verbrauch"),
                ["LabelVerwendung"] = Text_("GEBK_LBL_VERWENDUNG", "Verwendung"),
                ["LabelGebaeudeart"] = Text_("GEB_LBL_GEBAEUDEART", "Gebäudeart"),
                ["LabelBaujahr"] = Text_("GEB_LBL_BAUJAHR", "Baujahr"),
                ["LabelSuche"] = Text_("GEB_LBL_SUCHE", "Filter:"),
                ["PlatzhalterSuche"] = Text_("GEB_PLATZHALTER_SUCHE", "Suche, z. B. Haus*_1990*"),
                ["LabelGebaeudename"] = Text_("GEB_LBL_GEBAEUDENAME", "Gebäudename:"),
                ["LabelBeschreibung"] = Text_("GEB_LBL_BESCHREIBUNG", "Beschreibung:"),
                ["LabelWohnflaeche"] = Text_("GEB_LBL_WOHNFLAECHE", "Nutzfläche:"),
                ["LabelEinheit"] = Text_("GEBW_LBL_ART_ANGABE", "Art der Angabe:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                ["SpalteTypFlaeche"] = Text_("GEB_SP_TYP_FLAECHE", "Typ/Wohnfläche"),
                // Stufe G1 (Konzept 2.7): Spalte und Detailkennzahlen des Rechenwegs.
                ["SpalteRechenweg"] = Text_("GEB_SP_RECHENWEG", "Rechenweg"),
                ["LabelHges"] = Text_("GEB_LBL_HGES", "Wärmeleitwert H_ges:"),
                ["LabelRechenweg"] = Text_("GEB_LBL_RECHENWEG", "Rechenweg:"),
                ["TextAlle"] = Text_("GEB_TEXT_ALLE", "Alle"),
                ["TextWohngebaeude"] = Text_("GEBK_VERWENDUNG_WOHN", "Wohngebäude"),
                ["TextSonstige"] = Text_("GEB_TEXT_SONSTIGE", "Gewerbe+Sonstige"),

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
                ["BtnAendernText"] = Text_("GEB_BTN_AENDERN", "Ändern"),
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
        }

        // =================================================================================
        // Die Wege hinter den Delegaten
        // =================================================================================

        private static IReadOnlyList<GebaeudeKatalogZeile> Katalogzeilen(
            bool wohngebaeude, string art, int? klasse, bool ausBaujahrwahl)
        {
            var ctrl = new GebaeudeStammCtrl();
            IReadOnlyList<GebaeudeModel> saetze =
                ctrl.Filtern(wohngebaeude, art, klasse, ausBaujahrwahl);

            var zeilen = new List<GebaeudeKatalogZeile>(saetze.Count);
            foreach (GebaeudeModel m in saetze)
                zeilen.Add(new GebaeudeKatalogZeile(
                    m.Gebaeudename ?? "", m.Gebaeudeart ?? "",
                    m.Wohnflaeche_gesamt.ToString("F2") + " [m²]"));
            return zeilen;
        }

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

        /// <summary>
        /// Der Parametersatz der Wohnflächenangabe zu EINER Zeile. Das Baujahrfeld dort
        /// zeigt den KLARTEXT der Baualtersklasse, die Zeile führt den Buchstaben
        /// (<c>btn_Aendern_Click</c>:430-434).
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

            string baujahr = GebaeudeStammCtrl.BAUALTERSKLASSEN_DE[
                GebaeudeStammCtrl.KlassenIndex(z.Baualtersklasse)];

            return GebaeudeWohnflaecheHuelle.Gaben(modell, baujahr);
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

        internal static Z_ProjGebModel NachModell(GebaeudeProjektZeile z, int projektId)
        {
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
                DezentralWarmwasser = z.DezentralWarmwasser
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

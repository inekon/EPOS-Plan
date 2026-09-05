using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der GEBÄUDE eines Projekts (iU9-W9.2) — der Ersatz für
    /// <c>Form_Gebaeude</c>.
    ///
    /// <para><b>Drei Betriebsarten, zwei Einstiege.</b> <see cref="Oeffnen"/> trägt den
    /// Projekt- und den Assistentenweg, <see cref="Katalogverwaltung"/> den Admin-Weg aus
    /// <c>Dienste.Navigation</c> (<c>Masken.GebaeudeAdmin</c>). Der Vorläufer unterschied
    /// sie über zwei Felder und ein <c>Load</c>-Ereignis, das die halbe Maske versteckte.</para>
    ///
    /// <para><b>Die Liste wird GETEILT, nicht kopiert.</b> Wie in den Wellen 6 und 7
    /// gehört die <c>List&lt;Z_ProjGebModel&gt;</c> dem Aufrufer; die Hülle baut sie nach
    /// jeder Änderung AN ORT UND STELLE aus der Anzeigeliste neu auf. Das geht, weil eine
    /// Anzeigezeile ALLE Felder ihres Modells trägt — und es ist der einzige Weg, der
    /// auch im Assistenten trägt, wo dieselbe Liste über mehrere Seitenbesuche
    /// hinweg lebt.</para>
    ///
    /// <para><b>Drei Unterdialoge, drei Überlagerungen.</b> Katalogeditor (W9.1),
    /// Wohnflächenangabe (W9.3) und Gebäudetypen-Verwaltung (W8.4) erscheinen IM selben
    /// Fenster (Risiko R2); die Hülle reicht dafür nur die Parametersätze durch.</para>
    /// </summary>
    internal static class GebaeudeHuelle
    {
        /// <summary>Gewünschtes Innenmaß (Vorläufer: 812 × 573).</summary>
        private static readonly Size MASS = new Size(1060, 720);

        /// <summary>
        /// Die vorläufige Id einer noch nicht gespeicherten Zuordnung — derselbe
        /// Startwert wie <c>Form_Gebaeude.startindex</c>.
        /// </summary>
        private const int STARTINDEX = 100000;

        // =================================================================================
        // Einstiege
        // =================================================================================

        /// <summary>
        /// Zeigt die Gebäude eines Projekts als eigenes Fenster — der Weg von
        /// <c>Form_Start.pBox_Gebaude_Click</c> und den beiden Kontextmenüpunkten.
        /// </summary>
        /// <returns><c>true</c>, wenn mit OK geschlossen wurde.</returns>
        internal static bool Oeffnen(IWin32Window besitzer, int projektId, string projektName,
                                     List<Z_ProjGebModel> modelle)
        {
            bool ok = false;
            BlazorDialogForm<GebaeudeDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, projektId, projektName, modelle, wizard: false, admin: false))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<GebaeudeDialog>(Titel(), MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        /// <summary>
        /// Die KATALOGVERWALTUNG (<c>Masken.GebaeudeAdmin</c>): nur der Katalog, ohne
        /// Projektteil und ohne die beiden Pfeile.
        /// </summary>
        internal static bool Katalogverwaltung(IWin32Window besitzer)
        {
            bool ok = false;
            BlazorDialogForm<GebaeudeDialog> dlg = null;

            var werte = new Dictionary<string, object>(
                Gaben(besitzer, 0, "", new List<Z_ProjGebModel>(), wizard: false, admin: true))
            {
                ["Geschlossen"] = EventCallback.Factory.Create<bool>(new object(), b =>
                {
                    ok = b;
                    if (dlg != null) dlg.Schliessen(b);
                })
            };

            dlg = new BlazorDialogForm<GebaeudeDialog>(Titel(), MASS, werte);
            using (dlg)
            {
                if (besitzer != null) dlg.ShowDialog(besitzer); else dlg.ShowDialog();
            }
            return ok;
        }

        // iU9-W16a.5: Die Fabrikmethode AssistentSeite() ist entfallen - der
        // Assistent ist selbst eine Razor-Seite und braucht kein randloses
        // WinForms-Formular mehr. AssistentHuelle ruft direkt Gaben(...).

        // =================================================================================
        // Der Parametersatz
        // =================================================================================

        internal static IReadOnlyDictionary<string, object> Gaben(
            IWin32Window besitzer, int projektId, string projektName,
            List<Z_ProjGebModel> modelle, bool wizard, bool admin)
        {
            var zeilen = new List<GebaeudeProjektZeile>();
            foreach (Z_ProjGebModel m in modelle) zeilen.Add(AusModell(m));

            int[] naechsteId = { STARTINDEX };

            // Die Fachliste wird nach jeder Aenderung AN ORT UND STELLE neu aufgebaut -
            // dieselbe Liste, neue Zeilen. Der Assistent reicht dasselbe Objekt ueber
            // mehrere Seitenbesuche hinweg.
            Action geaendert = () =>
            {
                modelle.Clear();
                foreach (GebaeudeProjektZeile z in zeilen) modelle.Add(NachModell(z, projektId));
            };

            return new Dictionary<string, object>
            {
                ["Zeilen"] = zeilen,
                ["Wizard"] = wizard,
                ["Admin"] = admin,
                ["Geaendert"] = geaendert,

                ["Katalog"] = new Func<bool, string, int?, bool, IReadOnlyList<GebaeudeKatalogZeile>>(
                    (wohn, art, klasse, ausBaujahr) => Katalogzeilen(wohn, art, klasse, ausBaujahr)),
                ["Gebaeudearten"] = new Func<bool, IReadOnlyList<string>>(
                    wohn => GebaeudeStammCtrl.Gebaeudearten(wohn)),
                ["Baualtersklassen"] = GebaeudeStammCtrl.Baualtersklassen(),
                ["StammDetail"] = new Func<string, GebaeudeStammDetail>(Stammdetail),
                ["StammSatz"] = new Func<string, GebaeudeProjektZeile>(
                    name => Aufnehmen(name, projektId, naechsteId)),
                ["KatalogLoeschen"] = new Func<string, bool>(
                    name => new GebaeudeStammCtrl().Delete(name)),

                ["KatalogGaben"] = new Func<string, IReadOnlyDictionary<string, object>>(
                    name => GebaeudeKatalogHuelle.Gaben(besitzer, name,
                        string.IsNullOrEmpty(name)
                            ? GebaeudeKatalogModus.Neu : GebaeudeKatalogModus.Bearbeiten)),

                ["WohnflaecheGaben"] = new Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>(
                    Wohnflaechengaben),

                ["GebaeudetypGaben"] = new Func<IReadOnlyDictionary<string, object>>(
                    () => GebaeudetypHuelle.Gaben()),

                // Anwenderwunsch W9-E-2 (05.09.2026): der Waermebedarf GENAU DIESES
                // Gebaeudes. In der Katalogverwaltung gibt es kein Projekt; dort zeigt
                // die Komponente den Knopf ohnehin nicht (Admin), und der Delegat
                // antwortet mit null.
                ["BedarfGaben"] = new Func<GebaeudeProjektZeile, IReadOnlyDictionary<string, object>>(
                    z => Bedarfsgaben(z, projektId)),

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
                ["LabelWohnflaeche"] = Text_("GEB_LBL_WOHNFLAECHE", "Wohn-/Nutzfläche:"),
                ["LabelEinheit"] = Text_("GEBW_LBL_ART_ANGABE", "Art der Angabe:"),
                ["SpalteWahl"] = Text_("KFAK_SP_WAHL", "Wahl"),
                ["SpalteName"] = Text_("BHKWV_SP_NAME", "Name"),
                ["SpalteTypFlaeche"] = Text_("GEB_SP_TYP_FLAECHE", "Typ/Wohnfläche"),
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
            var ctrl = new GebaeudeStammCtrl();
            ctrl.ReadAll("Bezeichner='" + name + "'");
            if (ctrl.rows == 0) return null;

            GebaeudeModel m = ctrl.items[0];
            return new GebaeudeStammDetail(m.Gebaeudename ?? "", m.Gebaeudeart ?? "",
                                           m.Beschreibung ?? "",
                                           m.Wohnflaeche_gesamt.ToString("F2"));
        }

        /// <summary>
        /// „◀" — aus einem Katalogsatz entsteht eine Projektzeile mit den Vorbelegungen aus
        /// <c>btn_Hinzu_Click</c>:245-255: Einheit „Wohnfläche [m²]", Jahresnutzungsgrad 1,
        /// keine dezentrale Warmwasserbereitung. Die Projektkopie legt erst der
        /// Speicherweg an (<c>WizardCtrl</c>), nicht dieser Knopf.
        /// </summary>
        private static GebaeudeProjektZeile Aufnehmen(string name, int projektId, int[] naechsteId)
        {
            var ctrl = new GebaeudeStammCtrl();
            ctrl.ReadAll("Bezeichner='" + name + "'");
            if (ctrl.rows == 0) return null;

            GebaeudeModel m = ctrl.items[0];
            return new GebaeudeProjektZeile
            {
                IdZ = naechsteId[0]++,      // noch nicht gespeichert, also noch unbekannt
                IdGebaeude = m.ID,
                Name = m.Gebaeudename ?? "",
                Art = m.Typ ?? "",
                Beschreibung = m.Beschreibung ?? "",
                Baualtersklasse = m.Baualtersklasse ?? "",
                Wohnflaeche = m.Wohnflaeche_gesamt,
                Einheit = "Wohnfläche [m²]",
                Jahresnutzungsgrad = 1,
                DezentralWarmwasser = false
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
        // Der Wärmebedarf EINES Gebäudes (Anwenderwunsch W9-E-2, 05.09.2026)
        // =================================================================================

        /// <summary>
        /// Der Parametersatz des Bedarfsdialogs zu EINER Projektzeile — <c>null</c>, wenn
        /// es dafür keine Zahl gibt.
        ///
        /// <para><b>Drei Gründe für <c>null</c>:</b> kein Projekt (Katalogverwaltung), das
        /// Projekt führt keine Klimaregion (dieselbe Sperre wie im Lauf,
        /// <c>SimulationLaufCtrl.Vorpruefen</c>) oder die Zeile ist eben erst aufgenommen
        /// und hat noch keine Projektkopie (<c>IdZ</c> ab 100000, siehe
        /// <see cref="STARTINDEX"/>). Der Dialog MELDET das, statt eine leere
        /// Überlagerung aufzumachen.</para>
        ///
        /// <para><b>Gerechnet wird im Kern</b> (<c>GebaeudeBedarfCtrl</c>), gezeichnet
        /// auch (<c>ChartRenderer.GanglinieNormiert</c>) — die Komponente bekommt Zahlen
        /// und ein PNG.</para>
        /// </summary>
        private static IReadOnlyDictionary<string, object> Bedarfsgaben(
            GebaeudeProjektZeile zeile, int projektId)
        {
            if (zeile == null || projektId <= 0) return null;

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(projektId);

            GebaeudeBedarfErgebnis ergebnis =
                GebaeudeBedarfCtrl.Rechnen(projektId, projekt.m_ID_Klimaregion, zeile.IdZ);
            if (!ergebnis.Erfolgreich) return null;

            var monate = new double[12];
            for (int m = 0; m < 12 && m < ergebnis.MonatswerteMwh.Length; m++)
                monate[m] = ergebnis.MonatswerteMwh[m];

            var daten = new GebaeudeBedarfDaten
            {
                Name = ergebnis.Name,
                HeizwaermeMwh = ergebnis.HeizwaermeMwh,
                MaxLastKw = ergebnis.MaxLastKw,
                VollbenutzungsstundenH = ergebnis.VollbenutzungsstundenH,
                MonatswerteMwh = monate
            };

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Bildauftrag"] = new Func<bool, Diagrammbereich, byte[]>(
                    (sortiert, bereich) => Bedarfsbild(ergebnis, sortiert, bereich)),

                // Die Anzeigeeinheit (Entscheid W8-O-5): dieselbe gemerkte Wahl wie im
                // Bedarfsprofil- und im Bedarfsergebnisdialog.
                ["Einheit"] = BedarfEinheitWahl.Lies(),
                ["EinheitGewaehlt"] = new Action<Energieeinheit>(BedarfEinheitWahl.Schreib),

                ["TitelText"] = Text_("GEBB_TITEL", "Wärmebedarf Gebäude"),
                ["GruppeKennzahlen"] = Text_("GEBB_GRP_KENNZAHLEN", "Kennzahlen"),
                ["GruppeMonate"] = Text_("BERG_GRP_MONAT", "monatlicher Verlauf:"),
                ["LabelHeizwaerme"] = Text_("GEBB_LBL_HEIZWAERME", "Wärmebedarf Heizung:"),
                ["LabelMaxLast"] = Text_("SIMERG_LBL_MAX_WAERMELAST", "max. Wärmelast"),
                ["LabelVollbenutzung"] =
                    Text_("GEBB_LBL_VOLLBENUTZUNG", "Vollbenutzungsstunden:"),
                ["LabelSortiert"] = Text_("SIM_CHK_SORTIERT", "sortiert"),
                ["LabelEinheit"] = Text_("ALLG_LBL_EINHEIT", "Einheit:"),
                ["EinheitStunden"] = Text_("GEBB_EINHEIT_STUNDEN", "h/a"),
                ["Bildtext"] = Text_("CHART_TITEL_WAERMELAST_JAHRESGANGLINIE",
                                     "Wärmelast Jahresganglinie"),
                ["Monatsnamen"] = Monatsnamen(),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["HilfeSchluessel"] = "Form_Gebaeude.btn_Help"
            };
        }

        /// <summary>
        /// Die Jahresganglinie des Gebäudes — <b>dasselbe Bild wie B1 der Ergebnisseite</b>
        /// (<c>SimulationErgebnisHuelle.BildBedarfWaerme</c>): normiert auf den
        /// Jahreshöchstwert, x wahlweise Monatsgrenzen oder die vier Stundenmarken,
        /// Farbe <c>F_BEDARF</c> = Rot. Nur die Reihe ist eine andere — hier steht
        /// GENAU EINE, die Heizwärme dieses Gebäudes.
        /// </summary>
        /// <param name="sortiert">Dauerlinie statt Ganglinie.</param>
        /// <param name="bereich">Der aufgezogene Bildausschnitt (Datenzoom, Befund A-1);
        /// <c>null</c> = das ganze Jahr. Was an dieser Stelle des Bildes steht, weiß nur
        /// der Renderer — deshalb rechnet <c>ChartRenderer.FensterAusBild</c>.</param>
        private static byte[] Bedarfsbild(GebaeudeBedarfErgebnis ergebnis, bool sortiert,
                                          Diagrammbereich bereich)
        {
            float[] werte = ergebnis.Stundenwerte;

            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(Text_("CHART_ACHSE_WAERMELAST", "Wärmelast"),
                                        Array.ConvertAll(werte, x => (double)x),
                                        SkiaSharp.SKColors.Red)
            };

            ChartRenderer.Achsenfenster fenster = bereich == null
                ? null
                : ChartRenderer.FensterAusBild(
                    new ChartRenderer.Bildausschnitt(bereich.XVon, bereich.XBis,
                                                     bereich.YVon, bereich.YBis),
                    werte.Length);

            return ChartRenderer.GanglinieNormiert(
                Text_("CHART_TITEL_WAERMELAST_JAHRESGANGLINIE", "Wärmelast Jahresganglinie"),
                reihen,
                Text_("CHART_ACHSE_WAERMELAST", "Wärmelast"),
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                sortiert, fenster);
        }

        /// <summary>Die zwölf Zeilenbeschriftungen der Monatstabelle (mit Doppelpunkt).</summary>
        private static string[] Monatsnamen()
        {
            var namen = new string[12];
            for (int m = 0; m < 12; m++)
                namen[m] = Text_("ALLG_MONAT_" + (m + 1), MONATE_DE[m]) + ":";
            return namen;
        }

        private static readonly string[] MONATE_DE =
        { "Januar", "Februar", "März", "April", "Mai", "Juni",
          "Juli", "August", "September", "Oktober", "November", "Dezember" };

        // =================================================================================
        // Abbildung Zeile <-> Modell
        // =================================================================================

        internal static GebaeudeProjektZeile AusModell(Z_ProjGebModel m)
        {
            return new GebaeudeProjektZeile
            {
                IdZ = m.ID_Z,
                IdGebaeude = m.ID_Gebaeude,
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

        private static string Titel()
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

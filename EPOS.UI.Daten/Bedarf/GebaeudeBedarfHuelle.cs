using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE des Bedarfsdialogs EINES Gebäudes (<c>GebaeudeBedarfDialog</c>,
    /// Anwenderwunsch W9‑E‑2) — seit Stufe G1 eine eigene Datei in <c>EPOS.UI.Daten</c>
    /// (Umsetzungskonzept Gebäudesimulation 2.8).
    ///
    /// <para><b>Gerechnet wird im Kern</b> (<c>GebaeudeBedarfCtrl</c>), gezeichnet auch
    /// (<c>ChartRenderer.GanglinieNormiertModell</c>) — die Komponente bekommt Zahlen und
    /// ein Zeichenmodell. Seit G1 trägt das DTO den Rechenweg (Ausweis
    /// „Tagesbilanz (Bestandsweg)" nach ADR-006) und die Kennzahlen des Vergleichs; ihre
    /// Darstellung und der Vergleich alt/neu über <c>modellErzwungen</c> folgen mit G2.</para>
    /// </summary>
    internal static class GebaeudeBedarfHuelle
    {
        /// <summary>
        /// Der Parametersatz des Bedarfsdialogs zu EINER Projektzeile — <c>null</c>, wenn
        /// es dafür keine Zahl gibt: kein Projekt (Katalogverwaltung), keine Klimaregion
        /// oder eine eben erst aufgenommene Zeile ohne Projektkopie. Der Dialog MELDET das.
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(
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
                MonatswerteMwh = monate,

                Modelltext = GebaeudeHuelle.Rechenwegtext(ergebnis.Modell, vorgabe: false),
                SpitzeTagesmittelKw = ergebnis.SpitzeTagesmittelKw,
                SpitzeQuantil95Kw = ergebnis.SpitzeQuantil95Kw,
                KuehlenergieMwh = ergebnis.KuehlenergieMwh,
                KuehlstundenH = ergebnis.KuehlstundenH,
                MittlereRaumtemperaturC = ergebnis.MittlereRaumtemperaturC
            };

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Bildauftrag"] = new Func<bool, Zeichenmodell>(
                    sortiert => Bedarfsmodell(ergebnis, sortiert)),
                ["FarbeSetzen"] = new Func<Farbrolle, Farbe, Task>(FarbeSetzen),
                ["FarbeZuruecksetzen"] = new Func<Farbrolle, Task>(FarbeZuruecksetzen),

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
                ["LabelRechenweg"] = Text_("GEB_LBL_RECHENWEG", "Rechenweg:"),
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
        /// Die Jahresganglinie des Gebäudes — dasselbe Bild wie B1 der Ergebnisseite:
        /// normiert auf den Jahreshöchstwert, x wahlweise Monatsgrenzen oder die vier
        /// Stundenmarken, Farbe <c>F_BEDARF</c>.
        /// </summary>
        /// <param name="sortiert">Dauerlinie statt Ganglinie.</param>
        private static Zeichenmodell Bedarfsmodell(GebaeudeBedarfErgebnis ergebnis, bool sortiert)
        {
            double[] werte = ergebnis.Stundenwerte;

            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(Text_("CHART_ACHSE_WAERMELAST", "Wärmelast"),
                                        Array.ConvertAll(werte, x => (double)x),
                                        Farbrolle.BEDARF)
            };

            return ChartRenderer.GanglinieNormiertModell(
                Text_("CHART_TITEL_WAERMELAST_JAHRESGANGLINIE", "Wärmelast Jahresganglinie"),
                reihen,
                Text_("CHART_ACHSE_WAERMELAST", "Wärmelast"),
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                sortiert);
        }

        /// <summary>Der Klick auf das Farbfeld eines Legendeneintrags.</summary>
        private static Task FarbeSetzen(Farbrolle rolle, Farbe farbe)
        {
            Diagrammfarben.Setze(rolle, farbe);
            return Task.CompletedTask;
        }

        /// <summary>„Hausfarbe": Der Eintrag fällt aus der Einstellung.</summary>
        private static Task FarbeZuruecksetzen(Farbrolle rolle)
        {
            Diagrammfarben.Zuruecksetzen(rolle);
            return Task.CompletedTask;
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

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}

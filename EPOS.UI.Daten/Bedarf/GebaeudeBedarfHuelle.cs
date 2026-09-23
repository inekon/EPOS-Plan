using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// (<c>ChartRenderer.GanglinieNormiertModell</c>, <c>ChartRenderer.RaumtemperaturModell</c>)
    /// — die Komponente bekommt Zahlen und Zeichenmodelle. Das DTO trägt den Rechenweg (Ausweis
    /// „Tagesbilanz (Bestandsweg)" nach ADR-006), die Kennzahlen des VDI-Wegs und den
    /// <b>Vergleich alt/neu</b>: zwei Aufrufe desselben Controllers, der zweite mit
    /// <c>modellErzwungen</c> auf den jeweils anderen Weg (Umsetzungskonzept 2.7) — bis Stufe GA.</para>
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

            // Der Vergleich alt/neu: derselbe Controller, der andere Rechenweg. Liefert er
            // nichts (etwa ein benannt abgelehntes Gebaeude auf dem VDI-Weg), bleibt die
            // Vergleichstabelle weg - die Meldung steht im Protokoll des Aufrufs.
            string anderer = ergebnis.Modell == DbWerte.GEBAEUDE_MODELL_VDI6007
                ? DbWerte.GEBAEUDE_MODELL_TAGESBILANZ
                : DbWerte.GEBAEUDE_MODELL_VDI6007;
            GebaeudeBedarfErgebnis gegen =
                GebaeudeBedarfCtrl.Rechnen(projektId, projekt.m_ID_Klimaregion, zeile.IdZ, anderer);

            GebaeudeBedarfDaten daten = Daten(ergebnis, gegen.Erfolgreich ? Daten(gegen, null) : null);

            // Das Bild "Raumtemperatur" gibt es nur auf dem VDI-Weg (Konzept 8.2).
            Func<Zeichenmodell> raumbild = ergebnis.RaumtemperaturC != null
                ? () => Raumtemperaturmodell(ergebnis)
                : null;

            // Das Bild der Kaeltelast (Stufe KU1) - nur mit Kuehlreihe; auf dem Bestandsweg gibt
            // es keine Reihe, der Abschnitt steht dort mit 0 und Hinweis.
            Func<bool, Zeichenmodell> kaeltebild = ergebnis.KuehlbedarfKwh != null
                ? sortiert => Kaeltemodell(ergebnis, sortiert)
                : null;

            return new Dictionary<string, object>
            {
                ["Daten"] = daten,
                ["Bildauftrag"] = new Func<bool, Zeichenmodell>(
                    sortiert => Bedarfsmodell(ergebnis, sortiert)),
                ["BildauftragRaumtemperatur"] = raumbild,
                ["BildauftragKaelte"] = kaeltebild,
                ["GruppeKaelte"] = Text_("GEBB_GRP_KAELTE", "Kältebedarf"),
                ["LabelKaeltelastMax"] = Text_("GEBB_LBL_KAELTELAST_MAX", "max. Kältelast"),
                ["LabelHeizenUndKuehlen"] = Text_("GEBB_LBL_HEIZEN_UND_KUEHLEN", "Stunden mit Heizen und Kühlen:"),
                ["BildtextKaelte"] = Text_("CHART_TITEL_KAELTELAST_JAHRESGANGLINIE", "Kältelast Jahresganglinie"),
                ["SpalteHeizung"] = MyResource.Resource.KANAL_HEIZUNG_ANZEIGE,
                ["SpalteKuehlung"] = MyResource.Resource.KANAL_KUEHLUNG_ANZEIGE,
                ["BildtextRaumtemperatur"] = Text_("GEBB_BILD_RAUMTEMPERATUR", "Raumtemperatur"),
                ["GruppeVergleich"] = Text_("GEBB_GRP_VERGLEICH", "Vergleich der Rechenwege"),
                ["SpalteKennzahl"] = Text_("GEBB_SP_KENNZAHL", "Kennzahl"),
                ["SpalteTagesbilanz"] = Text_("GEBB_SP_TAGESBILANZ", "Tagesbilanz"),
                ["SpalteVdi6007"] = Text_("GEBB_SP_VDI6007", "VDI 6007"),
                ["SpalteAbweichung"] = Text_("GEBB_SP_ABWEICHUNG", "Abweichung"),
                ["LabelSpitzeStunde"] = Text_("GEBB_LBL_SPITZE_STUNDE", "Spitzenlast (Stunde):"),
                ["LabelSpitzeTagesmittel"] = Text_("GEBB_LBL_SPITZE_TAGESMITTEL", "Spitzenlast (Tagesmittel):"),
                ["LabelSpitzeQuantil95"] = Text_("GEBB_LBL_SPITZE_QUANTIL95", "95-%-Wert der Stundenlast:"),
                ["LabelKuehlbedarf"] = Text_("GEBB_LBL_KUEHLBEDARF", "Kühlbedarf:"),
                ["LabelKuehlstunden"] = Text_("GEBB_LBL_KUEHLSTUNDEN", "Stunden mit Kühlbedarf:"),
                ["LabelMittlereRaumtemperatur"] =
                    Text_("GEBB_LBL_MITTLERE_RAUMTEMPERATUR", "mittlere Raumtemperatur (Nutzungszeit):"),
                ["LabelUeberhitzung"] = Text_("GEBB_LBL_UEBERHITZUNG", "Überhitzungsstunden:"),
                ["LabelSommerlueftung"] = Text_("GEBB_LBL_SOMMERLUEFTUNG", "Stunden mit Sommerlüftung:"),
                ["EinheitStundenZahl"] = Text_("GEBB_EINHEIT_H", "h"),
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
        /// Das DTO eines Ergebnisses; <paramref name="vergleich"/> ist der andere Rechenweg
        /// (<c>null</c> = keiner). Energiemengen bleiben in MWh, umgerechnet wird an der
        /// Anzeigekante.
        /// </summary>
        private static GebaeudeBedarfDaten Daten(GebaeudeBedarfErgebnis ergebnis, GebaeudeBedarfDaten vergleich)
        {
            var monate = new double[12];
            for (int m = 0; m < 12 && m < ergebnis.MonatswerteMwh.Length; m++)
                monate[m] = ergebnis.MonatswerteMwh[m];

            return new GebaeudeBedarfDaten
            {
                Name = ergebnis.Name,
                HeizwaermeMwh = ergebnis.HeizwaermeMwh,
                MaxLastKw = ergebnis.MaxLastKw,
                VollbenutzungsstundenH = ergebnis.VollbenutzungsstundenH,
                MonatswerteMwh = monate,

                Modelltext = GebaeudeHuelle.Rechenwegtext(ergebnis.Modell, vorgabe: false),
                IstVdi6007 = ergebnis.Modell == DbWerte.GEBAEUDE_MODELL_VDI6007,
                SpitzeTagesmittelKw = ergebnis.SpitzeTagesmittelKw,
                SpitzeQuantil95Kw = ergebnis.SpitzeQuantil95Kw,
                // Stufe KU1 (F-K18): Auf dem Bestandsweg bucht der Lauf Kaeltebedarf 0 mit
                // Hinweis - die Auskunft zeigt dieselbe 0, nicht „—" (bis Stufe GA).
                KuehlenergieMwh = ergebnis.KaelteBestandsweg ? 0.0 : ergebnis.KuehlenergieMwh,
                KuehlstundenH = ergebnis.KaelteBestandsweg ? 0 : ergebnis.KuehlstundenH,
                MittlereRaumtemperaturC = ergebnis.MittlereRaumtemperaturC,
                UeberhitzungsstundenH = ergebnis.UeberhitzungsstundenH,
                SommerlueftungsstundenH = ergebnis.SommerlueftungsstundenH,
                Vergleich = vergleich,

                // Stufe KU1 (Kuehlkonzept 8.4): der Abschnitt „Kaeltebedarf".
                KaelteAbschnitt = ergebnis.KuehlbedarfKwh != null || ergebnis.KaelteBestandsweg,
                KaeltelastMaxKw = ergebnis.KaelteBestandsweg ? 0.0 : ergebnis.KaeltelastMaxKw,
                VollbenutzungsstundenKaelteH = ergebnis.VollbenutzungsstundenKaelteH,
                StundenHeizenUndKuehlenH = ergebnis.StundenHeizenUndKuehlen,
                KuehlMonatswerteMwh = ergebnis.KuehlMonatswerteMwh != null
                    ? (IReadOnlyList<double>)(double[])ergebnis.KuehlMonatswerteMwh.Clone()
                    : ergebnis.KaelteBestandsweg ? new double[12] : new List<double>(),
                KaelteHerleitung = Kaelteherleitung(ergebnis)
            };
        }

        /// <summary>
        /// Die Herleitungszeilen des Abschnitts „Kältebedarf" (Stufe KU1, Kühlkonzept 8.3, 8.4):
        /// wie der Kältebedarf dieses Gebäudes entsteht — Bestandsweg (0 mit Hinweis, F-K18),
        /// wirksame Kühlung (Sollwert, Grenze, Kanal), eingeschaltet ohne Sollwert, oder
        /// informativ (Projekt rechnet keine Kälte bzw. Gebäude nicht gekühlt) — und danach die
        /// Grenze der Zahl (K5), die an jeder Kältezahl steht. Die Sätze des Laufs werden
        /// wiederverwendet, wo es sie gibt: ein Text, eine Stelle.
        /// </summary>
        internal static List<string> Kaelteherleitung(GebaeudeBedarfErgebnis e)
        {
            var zeilen = new List<string>();
            if (e == null || !e.Erfolgreich) return zeilen;

            CultureInfo k = CultureInfo.CurrentCulture;
            string obere = (e.ObereRaumtemperaturC ?? 0.0).ToString("N1", k);

            if (e.KaelteBestandsweg)
                zeilen.Add(string.Format(k, MyResource.Resource.SIMENG_KAELTE_BESTANDSWEG, e.Name));
            else if (e.KuehlSollwertC is double soll)
                zeilen.Add(string.Format(k, Text_("GEBB_HRL_KAELTE_WIRKSAM",
                                                  "Gekühlt auf {0} °C, Kühlleistungsgrenze {1}. Der Kältebedarf geht in den Kanal Kühlung; Wärmeerzeuger decken keine Kälte."),
                                         soll.ToString("N1", k),
                                         e.KuehlleistungMaxKw is double kw
                                             ? kw.ToString("N1", k) + " kW"
                                             : Text_("GEBB_UNBEGRENZT", "unbegrenzt")));
            else if (!e.KuehlbetriebProjekt)
                zeilen.Add(string.Format(k, Text_("GEBB_HRL_KAELTE_PROJEKT_AUS",
                                                  "Das Projekt rechnet keine Kälte (Projekteinstellung „Kühlung rechnen“ aus): Der Kühlbedarf ist informativ — die Wärme, die abgeführt werden müsste, damit die Raumluft {0} °C nicht überschreitet; er geht in keinen Kanal."),
                                         obere));
            else if (e.KuehlungAktiv)
                zeilen.Add(string.Format(k, MyResource.Resource.SIMENG_KAELTE_OHNE_SOLLWERT, e.Name));
            else
                zeilen.Add(string.Format(k, Text_("GEBB_HRL_KAELTE_NICHT_GEKUEHLT",
                                                  "Das Gebäude wird nicht gekühlt: Der Kühlbedarf ist informativ — die Wärme, die abgeführt werden müsste, damit die Raumluft {0} °C nicht überschreitet; er geht in keinen Kanal."),
                                         obere));

            zeilen.Add(SimulationKaeltebedarf.GrenzeFeuchte);
            return zeilen;
        }

        /// <summary>
        /// Das Bild der Kältelast (Stufe KU1, Kühlkonzept 8.4) — dieselbe Bildform wie die
        /// Wärmelast (normiert auf den Jahreshöchstwert), aber ein EIGENES Bild: im Wärmebild
        /// normierte der Kältewert die Wärmelinie mit (4.2).
        /// </summary>
        private static Zeichenmodell Kaeltemodell(GebaeudeBedarfErgebnis ergebnis, bool sortiert)
        {
            var reihen = new List<ChartRenderer.Reihe>
            {
                new ChartRenderer.Reihe(Text_("CHART_ACHSE_KAELTELAST", "Kältelast"),
                                        (double[])ergebnis.KuehlbedarfKwh.Clone(),
                                        Farbrolle.BEDARF)
            };

            return ChartRenderer.GanglinieNormiertModell(
                Text_("CHART_TITEL_KAELTELAST_JAHRESGANGLINIE", "Kältelast Jahresganglinie"),
                reihen,
                Text_("CHART_ACHSE_KAELTELAST", "Kältelast"),
                sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate,
                sortiert);
        }

        /// <summary>
        /// Das Bild „Raumtemperatur" (Stufe G2): Raumluft und operativ mit dem Sollwertband —
        /// gezeichnet im Kern (<c>ChartRenderer.RaumtemperaturModell</c>).
        /// </summary>
        private static Zeichenmodell Raumtemperaturmodell(GebaeudeBedarfErgebnis ergebnis)
        {
            return ChartRenderer.RaumtemperaturModell(
                Text_("GEBB_BILD_RAUMTEMPERATUR", "Raumtemperatur"),
                ergebnis.RaumtemperaturC, ergebnis.OperativeTemperaturC,
                ergebnis.HeizsollwertC, ergebnis.ObereRaumtemperaturC,
                new ChartRenderer.Raumtemperaturnamen
                {
                    Raumluft = Text_("GEBB_REIHE_RAUMLUFT", "Raumluft"),
                    Operativ = Text_("GEBB_REIHE_OPERATIV", "operative Temperatur"),
                    Heizsollwert = Text_("GEBB_REIHE_HEIZSOLLWERT", "Heizsollwert"),
                    ObereGrenze = Text_("GEBB_REIHE_OBERE_GRENZE", "obere Raumtemperatur"),
                    Achse = "°C"
                });
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

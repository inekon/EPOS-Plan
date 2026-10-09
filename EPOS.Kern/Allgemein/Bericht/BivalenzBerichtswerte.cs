using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Bivalenzwerte eines Stands für den Bericht</b> (Fachkonzept Übergabegrenze 7.2, 7.3; UB‑E4): die
    /// Herleitung aus den Projektdaten (Gebäude, Übergabe, Kennfeld bei Höchstvorlauf) der ersten Wärmepumpe mit
    /// gesetzter Einbindung und die Schalter der Prüfhinweise (UB‑Q10). Erhoben im Sammler
    /// (<see cref="BivalenzBerichtsquelle.Lade"/>), gelesen von Bild und Tafel — die Quellen des Vorlagenkatalogs lesen
    /// die Datenbank nie.
    /// </summary>
    public sealed class BivalenzBerichtswerte
    {
        /// <summary>Die gerechnete Herleitung; <c>null</c> ohne Kopplung, ohne Kennfeld oder ohne Einbindung.</summary>
        internal Bivalenzherleitung Herleitung { get; init; }

        /// <summary>Der Name der Wärmepumpe, deren Herleitung gilt.</summary>
        public string Waermepumpe { get; init; } = "";

        /// <summary>Höchstvorlauf θ_WP,max [°C]; NaN ohne Herleitung.</summary>
        public double HoechstvorlaufC => Herleitung?.HoechstvorlaufC ?? double.NaN;

        /// <summary>Übergabegrenze Φ_UE,max [kW]; NaN ohne Herleitung.</summary>
        public double UebergabeKw => Herleitung?.PhiUeMax ?? double.NaN;

        /// <summary>Anteil der Übergabegrenze an der Heizlast [%]; NaN ohne Herleitung.</summary>
        public double UebergabeAnteilProzent => Herleitung == null ? double.NaN : Herleitung.Anteil * 100.0;

        /// <summary>Rücklauf an der Übergabegrenze [°C]; NaN ohne Herleitung.</summary>
        public double RuecklaufC => Herleitung?.RuecklaufUeC ?? double.NaN;

        /// <summary>θ_biv,1 nach der Herleitung [°C]; NaN ohne Punkt.</summary>
        public double ErsterC => Herleitung?.Punkte.ErsterC ?? double.NaN;

        /// <summary>θ_biv,2 nach der Herleitung [°C]; NaN ohne Punkt.</summary>
        public double ZweiterC => Herleitung?.Punkte.ZweiterC ?? double.NaN;

        /// <summary>Der eingegebene Abschaltpunkt [°C], sofern er nach der Betriebsart gilt; sonst <c>null</c>.</summary>
        public double? EingegebenC => Herleitung?.Punkte.AbschaltpunktC;

        /// <summary>Der maßgebende Bivalenzpunkt [°C]; NaN ohne Herleitung.</summary>
        public double MassgebendC => Herleitung?.Punkte.MassgebendC ?? double.NaN;

        /// <summary>Anteil der Wärmepumpenleistung bei −7 °C an der Kesselleistung nach § 43 GModG [%]; NaN ohne Kessel.</summary>
        public double HybridAnteilProzent => Herleitung == null ? double.NaN : Herleitung.HybridAnteil * 100.0;

        /// <summary>Der Mindestanteil nach § 43 GModG [%]; NaN ohne Herleitung.</summary>
        public double HybridMindestanteilProzent => Herleitung == null ? double.NaN : Herleitung.HybridMindestanteil * 100.0;

        /// <summary>Prüfhinweis Anfahrgrenze (UB‑Q10): Luft/Wasser-Wärmepumpe und ein Raumsollwert unter 15 °C.</summary>
        public bool HinweisAnfahrgrenze { get; init; }

        /// <summary>Prüfhinweis Mindestrücklauf (UB‑Q10): ein Niedertemperatur- oder Biomassekessel im Projekt.</summary>
        public bool HinweisMindestruecklauf { get; init; }

        /// <summary>Trägt der Stand Übergabedaten (Herleitung mit Kopplung)?</summary>
        public bool MitUebergabe => Herleitung != null && Herleitung.Zustand != Herleitungszustand.OhneKopplung;

        /// <summary>
        /// Das Modell des Bivalenzdiagramms: aus der Herleitung samt den Stundenpunkten des Laufs, ohne Übergabedaten
        /// der Platzhalter.
        /// </summary>
        public BivalenzdiagrammModell Diagramm(IReadOnlyList<BivalenzStundenpunkt> stundenpunkte = null)
        {
            BivalenzdiagrammTexte texte = BivalenzdiagrammTexte.AusRessourcen();
            return MitUebergabe ? BivalenzdiagrammModell.Aus(Herleitung, stundenpunkte, texte)
                                : BivalenzdiagrammModell.Platzhalter(texte);
        }

        /// <summary>
        /// Die Stundenpunkte des Laufs aus dem Zeitreihensatz — Wärmeleistung der Wärmepumpe über der Außentemperatur, nur
        /// Stunden mit Leistung; <c>null</c>, wenn der Satz die Reihen nicht hergibt.
        /// </summary>
        public static IReadOnlyList<BivalenzStundenpunkt> Stundenpunkte(ZeitreihenSatz z)
        {
            double[] t = z?.Hole(ZeitreihenSatz.TEMPERATUR);
            double[] wp = z?.Hole(ZeitreihenSatz.WP_WAERME);
            if (t == null || wp == null) return null;
            var l = new List<BivalenzStundenpunkt>();
            for (int n = 0; n < ZeitreihenSatz.Stunden && n < t.Length && n < wp.Length; n++)
                if (wp[n] > 0.0) l.Add(new BivalenzStundenpunkt(Math.Round(t[n], 1), wp[n]));
            return l;
        }
    }

    /// <summary>
    /// <b>Die Quelle der Bivalenzwerte für den Bericht</b> (UB‑E4): liest Projekt- und Anlagendaten wie der Lauf
    /// (<see cref="BivalenzQuelle"/>, <c>SimulationWaermepumpe.BivalenzAufbauen</c>) und rechnet die Herleitung der ersten
    /// Wärmepumpe mit gesetzter Einbindung. Keine Rechenwirkung: Der Lauf rechnet seinen Weg selbst; hier entsteht nur
    /// der Ausweis.
    /// </summary>
    internal static class BivalenzBerichtsquelle
    {
        /// <summary>Raumsollwert, unter dem der Prüfhinweis Anfahrgrenze erscheint [°C] (Fachkonzept 3 (iv), UB‑Q10).</summary>
        internal const double ANFAHRGRENZE_SOLLWERT_C = 15.0;

        /// <summary>Die Werte des Projekts; <c>null</c> ohne Wärmepumpe mit Einbindung. Wirft nicht.</summary>
        internal static BivalenzBerichtswerte Lade(int idProjekt)
        {
            if (idProjekt <= 0) return null;
            try
            {
                List<WErzeugerModel> wps = WErzeugerCtrl.ModelleJeTyp(idProjekt, WizardItemClass.WP_TYP)
                    .Where(m => m != null && !string.IsNullOrWhiteSpace(m.Einbindung)).OrderBy(m => m.ID).ToList();
                if (wps.Count == 0) return null;
                BivalenzProjektdaten p = BivalenzQuelle.Projekt(idProjekt);
                bool mindestruecklauf = KesselMitMindestruecklauf(idProjekt);
                WErzeugerModel model = wps[0];
                Bivalenzherleitung h = Herleitung(model, p);
                bool luft = wps.Any(m => string.Equals((m.WQ_Typ ?? "").Trim(), "Aussenluft", StringComparison.OrdinalIgnoreCase));
                bool kalt = p?.Gebaeude != null && p.Gebaeude.AuslegungRaumC < ANFAHRGRENZE_SOLLWERT_C;
                return new BivalenzBerichtswerte
                {
                    Herleitung = h,
                    Waermepumpe = model.Bezeichner ?? "",
                    HinweisAnfahrgrenze = luft && kalt,
                    HinweisMindestruecklauf = mindestruecklauf,
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Die Herleitung einer Anlage — dieselben Eingänge wie das Bivalenzobjekt des Laufs: Höchstvorlauf
        /// <c>Vorlauf_Max</c> (Rückfall <c>Vorlauf</c>), σ_min aus den Gerätegrenzen, Betriebsart, Abschaltpunkt,
        /// Vorwärmbetrieb, Kesselleistung, Einbindung. <c>null</c> ohne Gebäudeseite, Höchstvorlauf oder Kennfeld.
        /// </summary>
        internal static Bivalenzherleitung Herleitung(WErzeugerModel model, BivalenzProjektdaten p)
        {
            BivalenzGebaeudedaten g = p?.Gebaeude;
            if (model == null || g == null || p.Unvollstaendig || !string.IsNullOrEmpty(p.Befund)) return null;
            double hoechstvorlauf = model.Vorlauf_Max ?? model.Vorlauf;
            if (!(hoechstvorlauf > 0.0)) return null;
            IReadOnlyList<Kennfeldpunkt> kennfeld = BivalenzQuelle.Kennfeld(model.ID_WP, hoechstvorlauf);
            if (kennfeld.Count == 0) return null;
            Bivalenzbetriebsart art = !model.Bivalenter_Betrieb ? Bivalenzbetriebsart.Parallel
                : model.Betriebsart == DbWerte.WP_BETRIEBSART_TEILPARALLEL ? Bivalenzbetriebsart.Teilparallel
                : model.Betriebsart == DbWerte.WP_BETRIEBSART_ALTERNATIV ? Bivalenzbetriebsart.Alternativ
                : Bivalenzbetriebsart.Parallel;
            Geraetespalten spalten = null;
            try { spalten = WaermepumpeGeraeteCtrl.GeraetespaltenLesen(model.ID_WP, false); }
            catch (Exception) { spalten = null; }
            Geraetegrenzen grenzen = Geraetegrenzen.Bilden(spalten, hoechstvorlauf);
            return Bivalenzherleitung.Rechnen(g, new BivalenzGeraetedaten
            {
                HoechstvorlaufC = hoechstvorlauf,
                Kennfeld = kennfeld,
                SpreizungMinK = grenzen.SpreizungMinK,
                Betriebsart = art,
                AbschaltpunktC = art == Bivalenzbetriebsart.Parallel ? null : (double?)model.Abschaltpunkt,
                Vorwaermbetrieb = model.Vorwaermbetrieb,
                Kesselleistung = p.KesselleistungKw,
                Einbindung = model.Einbindung,
            });
        }

        /// <summary>
        /// Führt das Projekt einen Niedertemperatur- oder Biomassekessel (UB‑Q10)? Bauart wie
        /// <see cref="Kesselkennlinie.Bauart"/>; Biomasse nach Brennstoff (Holz und Pellets, 12 und 15); Elektrokessel zählen nicht.
        /// </summary>
        internal static bool KesselMitMindestruecklauf(int idProjekt)
        {
            var dt = DataRepository.GetDataTable(
                "SELECT Brennwert, Beschreibung, Brennstoff FROM Tab_Heizkessel WHERE ID_Projekt = ?",
                new DbParam("@p", idProjekt));
            if (dt == null) return false;
            foreach (System.Data.DataRow r in dt.Rows)
            {
                bool brennwert = r["Brennwert"] != DBNull.Value && Convert.ToInt32(r["Brennwert"]) != 0;
                string beschreibung = r["Beschreibung"] == DBNull.Value ? "" : Convert.ToString(r["Beschreibung"]);
                int brennstoff = r["Brennstoff"] == DBNull.Value ? 0 : Convert.ToInt32(r["Brennstoff"]);
                if (brennstoff == 12 || brennstoff == 15) return true;
                // Ein Elektrokessel kennt keine Taupunktkorrosion - kein Mindestruecklauf.
                if (brennstoff == SimulationSPK.BRENNSTOFF_STROM) continue;
                if (Kesselkennlinie.Bauart(brennwert, beschreibung) == KesselBauart.Niedertemperatur) return true;
            }
            return false;
        }
    }
}

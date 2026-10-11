using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Stunde des Rückkühlwerks</b> (K-F2; Entwurf Split/VRF/Rückkühlwerk 5.3, 5.8): abgeführte Wärme, Last- und
    /// Luftanteil, Ventilatorstrom, Wasserbilanz und die Rückkühltemperatur, mit der die Maschine die Stunde gerechnet hat.
    /// </summary>
    public readonly struct RueckkuehlwerkStunde
    {
        /// <summary>Abgeführte Wärme der Anlagenzeile [kWh] — Kälte plus Verdichterstrom, in freier Kühlung die Kälte.</summary>
        public readonly double AbwaermeKwh;

        /// <summary>Lastanteil x = Abwärme / Nennleistung des Rückkühlwerks [—]; über 1 = Überlast.</summary>
        public readonly double Lastanteil;

        /// <summary>Luftanteil der Ventilatorregelung [—] (0…1).</summary>
        public readonly double Luftanteil;

        /// <summary>Ventilatorstrom [kWh]; 0 ohne <c>Ventilator_Nenn_kW</c>.</summary>
        public readonly double VentilatorKwh;

        /// <summary>Verdunstetes Wasser [m³].</summary>
        public readonly double VerdunstungM3;

        /// <summary>Abflutung [m³] = Verdunstung / (Eindickung − 1).</summary>
        public readonly double AbflutungM3;

        /// <summary>Drift (Tropfenauswurf) [m³].</summary>
        public readonly double DriftM3;

        /// <summary><c>true</c> = die Stunde lief nass (Kühlturm, adiabat oder hybrid befeuchtet).</summary>
        public readonly bool Nass;

        /// <summary>Die Rückkühltemperatur, mit der die Maschine die Stunde gerechnet hat [°C].</summary>
        public readonly double RueckkuehlC;

        /// <summary>Legt die Stunde an.</summary>
        public RueckkuehlwerkStunde(double abwaerme, double lastanteil, double luftanteil, double ventilator,
                                    double verdunstung, double abflutung, double drift, bool nass, double rueckkuehlC)
        {
            AbwaermeKwh = abwaerme;
            Lastanteil = lastanteil;
            Luftanteil = luftanteil;
            VentilatorKwh = ventilator;
            VerdunstungM3 = verdunstung;
            AbflutungM3 = abflutung;
            DriftM3 = drift;
            Nass = nass;
            RueckkuehlC = rueckkuehlC;
        }

        /// <summary>Wasser der Stunde [m³]: Verdunstung, Abflutung und Drift.</summary>
        public double WasserM3 => VerdunstungM3 + AbflutungM3 + DriftM3;

        /// <summary><c>true</c> = das Rückkühlwerk lag über seiner Nennleistung.</summary>
        public bool Ueberlast => Lastanteil > 1.0;
    }

    /// <summary>
    /// <b>Das Rückkühlwerk einer Kältemaschine</b> (K-F1, K-F2; Entwurf Split/VRF/Rückkühlwerk 5.1–5.4, 5.6): ein eigenes
    /// Glied, kein Erzeuger und ohne Platz in der Kältefolge. Rein, ohne Datenbank — gebaut aus der Projektkopie
    /// (<see cref="AusModell"/>).
    ///
    /// <para><b>Bezugstemperatur</b> je Bauart: <c>TROCKEN</c> Außenluft; Kühltürme die Feuchtkugel nach Stull (ohne Feuchte
    /// der Ausweichweg Außentemperatur − 3 K); <c>ADIABAT</c> und <c>HYBRID</c> oberhalb <c>Befeuchtung_Ab_C</c> die befeuchtete
    /// Lufteintrittstemperatur Trocken − η · (Trocken − Feuchtkugel), darunter und ohne Feuchte trocken. Leere Felder rechnen
    /// mit den Vorgaben in <see cref="KaelteFestwerte"/>; <c>TROCKEN</c> und die Kühltürme mit leeren Feldern rechnen die
    /// Rückkühltemperatur Bit für Bit wie die Rückkühlart (5.6).</para>
    ///
    /// <para><b>Annäherung:</b> <c>FEST</c> = <see cref="AnnaeherungK"/> in jeder Stunde. <c>LASTABHAENGIG</c>: ΔT = ΔT_Nenn ·
    /// x / L mit dem Lastanteil x = Abwärme / Nennleistung und dem Luftanteil L der Regelung (Ein-Aus und ohne Regelung 1,
    /// Stufen die Stufe, Drehzahl max(x, kleinste Drehzahl)) — die Drehzahlregelung hält so die Nenn-Annäherung, Ein-Aus
    /// lässt sie mit der Last sinken. Über Nennleistung (x &gt; 1) steigt sie linear über den Nennwert (der Übertrager ist
    /// überlastet, die Luft läuft voll), höchstens bis <see cref="RueckkuehlwerkSchema.ANNAEHERUNG_MAX_K"/>. Gegen die
    /// Zirkularität rechnet die Maschine genau einen Schritt (<see cref="Kaeltemaschine.Stunde(int, double, double)"/>).</para>
    ///
    /// <para><b>Ventilator</b> (nur mit <c>Ventilator_Nenn_kW</c>, er ersetzt dann den Hilfsstrom der Rückkühlung); seine
    /// Energie folgt dem Luftanteil der Annäherungsrechnung: <c>EIN_AUS</c> unter <c>FEST</c> taktend P · min(x, 1) (hält
    /// ΔT_Nenn), unter <c>LASTABHAENGIG</c> voll, solange die Maschine läuft (P · Laufanteil der Maschine, ΔT = ΔT_Nenn · x);
    /// <c>STUFEN</c> n gleich große Ventilatorgruppen, die nächsthöhere
    /// Stufe des nötigen Luftanteils, Leistung ∝ Stufe; <c>DREHZAHL</c> Luftanteil max(x, kleinste Drehzahl), Leistung ∝
    /// Luftanteil³ (Ähnlichkeitsgesetz).</para>
    ///
    /// <para><b>Wasser</b> (nur nass): Verdunstung = nass abgeführte Wärme / Verdampfungsenthalpie × Faktor — am Kühlturm die
    /// ganze Abwärme, adiabat und hybrid die Vorkühlwärme der Luft (Abwärme · Vorkühlung / Lufterwärmung, höchstens die
    /// Abwärme); Abflutung = Verdunstung / (Eindickung − 1); Drift am Kühlturm als Anteil des Umlaufwassers, adiabat und
    /// hybrid als Anteil der Verdunstung.</para>
    /// </summary>
    public sealed class Rueckkuehlwerk
    {
        /// <summary>Merkmal: Annäherung <c>LASTABHAENGIG</c> (gerechnet seit K-F2; bleibt als Schlüssel der Meldung).</summary>
        public const string MERKMAL_LASTABHAENGIG = "LASTABHAENGIG";

        /// <summary>Merkmal: Ventilatorleistung und -regelung (gerechnet seit K-F2).</summary>
        public const string MERKMAL_VENTILATOR = "VENTILATOR";

        /// <summary>Merkmal: Befeuchtung bzw. adiabate Vorkühlung (gerechnet seit K-F2).</summary>
        public const string MERKMAL_BEFEUCHTUNG = "BEFEUCHTUNG";

        /// <summary>Merkmal: Wasserbilanz (gerechnet seit K-F2).</summary>
        public const string MERKMAL_WASSERBILANZ = "WASSERBILANZ";

        /// <summary>Merkmal: freie Kühlung in Reihe (Teil-Freikühlung, gerechnet seit K-F3).</summary>
        public const string MERKMAL_REIHE = "REIHE";

        /// <summary>
        /// K-F3: Schaltung der freien Kühlung <c>REIHE</c> — der Rückkühler kühlt den Kaltwasserrücklauf vor, der Verdichter
        /// deckt den Rest (Teil-Freikühlung, <see cref="TeilFreikuehlungKwh"/>); sonst <c>PARALLEL</c> (heutige freie Kühlung).
        /// </summary>
        public bool Reihe;

        /// <summary>ID der Projektkopie.</summary>
        public int Id;

        /// <summary>Name des Rückkühlwerks.</summary>
        public string Bezeichner = "";

        /// <summary>Bauart (<see cref="RueckkuehlwerkSchema.BAUARTEN"/>); leer oder unbekannt = trocken.</summary>
        public string Bauart = RueckkuehlwerkSchema.BAUART_TROCKEN;

        /// <summary>Gepflegte Annäherung im Nennpunkt [K]; <c>null</c> = Festwert der passenden Rückkühlart.</summary>
        public double? AnnaeherungNennK;

        /// <summary>Abzuführende Wärme im Nennpunkt [kW]; <c>null</c> = der Nennpunkt ist die volle Leistung der Maschinen der Stunde.</summary>
        public double? NennleistungKw;

        /// <summary>Annäherung <c>LASTABHAENGIG</c>?</summary>
        public bool Lastabhaengig;

        /// <summary>Nennleistung des Ventilators [kW]; <c>null</c> = der Hilfsstrom der Rückkühlung an der Maschine gilt.</summary>
        public double? VentilatorNennKw;

        /// <summary>Wirksame Ventilatorregelung; leer gepflegt = <c>EIN_AUS</c>.</summary>
        public string Regelung = RueckkuehlwerkSchema.REGELUNG_EIN_AUS;

        /// <summary>Stufen bei <c>STUFEN</c>.</summary>
        public int Stufen = KaelteFestwerte.RKW_VENTILATOR_STUFEN;

        /// <summary>Kleinster Luftanteil bei <c>DREHZAHL</c>.</summary>
        public double DrehzahlMin = KaelteFestwerte.RKW_VENTILATOR_DREHZAHL_MIN;

        /// <summary>Sättigungswirkungsgrad der Befeuchtung.</summary>
        public double BefeuchtungWirkungsgrad = KaelteFestwerte.RKW_BEFEUCHTUNG_WIRKUNGSGRAD;

        /// <summary>Außentemperatur, oberhalb der befeuchtet wird [°C] (Schaltgrenze des Hybridkühlers).</summary>
        public double BefeuchtungAbC = KaelteFestwerte.RKW_BEFEUCHTUNG_AB_C;

        /// <summary>Gepflegter Verdunstungsfaktor; <c>null</c> = Vorgabe der Bauart.</summary>
        public double? VerdunstungFaktorGepflegt;

        /// <summary>Eindickungszahl.</summary>
        public double Eindickung = KaelteFestwerte.RKW_EINDICKUNG;

        /// <summary>Gepflegter Driftanteil; <c>null</c> = Vorgabe der Bauart.</summary>
        public double? DriftAnteilGepflegt;

        /// <summary>Wasserpreis der Anlagenzeile [€/m³] (<c>Wasserpreis_EUR_m3</c>); <c>null</c> = keine Wasserkosten.</summary>
        public double? WasserpreisEurM3;

        private readonly List<string> _nichtGerechnet = new List<string>();

        private bool[] _befeuchtet;
        private double[] _vorkuehlungK;

        /// <summary>Die gepflegten Merkmale, die noch nicht gerechnet werden (<c>MERKMAL_*</c>); leer = alles gerechnet.</summary>
        public IReadOnlyList<string> NichtGerechnet => _nichtGerechnet;

        /// <summary>Nasse Bauart mit Bezug Feuchtkugel (die Kühltürme).</summary>
        public bool Nass =>
            Bauart == RueckkuehlwerkSchema.BAUART_KUEHLTURM_OFFEN || Bauart == RueckkuehlwerkSchema.BAUART_KUEHLTURM_GESCHLOSSEN;

        /// <summary>Befeuchtende Bauart (adiabat oder hybrid).</summary>
        public bool Befeuchtend => Bauart == RueckkuehlwerkSchema.BAUART_ADIABAT || Bauart == RueckkuehlwerkSchema.BAUART_HYBRID;

        /// <summary>Verdunstungskühlanlage im Sinn der 42. BImSchV (Kühlturm, adiabat, hybrid) — nur Hinweis.</summary>
        public bool Verdunstungskuehlanlage => Nass || Befeuchtend;

        /// <summary>Mit gepflegtem Ventilator: sein Strom ersetzt den Hilfsstrom der Rückkühlung.</summary>
        public bool MitVentilator => VentilatorNennKw.HasValue;

        /// <summary>
        /// Die Rückkühlart, als die das Rückkühlwerk an der Maschine gilt (Entwurf 5.2): Kühltürme
        /// <see cref="KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER"/>, sonst <see cref="KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER"/>.
        /// </summary>
        public string Rueckkuehlart => Nass ? KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER
                                            : KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER;

        /// <summary>Die Vorgabe der Annäherung [K]: der Festwert der passenden Rückkühlart.</summary>
        public double AnnaeherungVorgabeK => Nass ? KaelteFestwerte.GRAEDIGKEIT_NASSKUEHLER_K
                                                  : KaelteFestwerte.GRAEDIGKEIT_TROCKENKUEHLER_K;

        /// <summary>Die wirksame Annäherung im Nennpunkt [K]: gepflegt oder die Vorgabe.</summary>
        public double AnnaeherungK => AnnaeherungNennK ?? AnnaeherungVorgabeK;

        /// <summary>Wirksamer Verdunstungsfaktor: gepflegt oder die Vorgabe der Bauart.</summary>
        public double VerdunstungFaktor => VerdunstungFaktorGepflegt ??
            (Nass ? KaelteFestwerte.RKW_VERDUNSTUNG_FAKTOR : KaelteFestwerte.RKW_VERDUNSTUNG_FAKTOR_BEFEUCHTUNG);

        /// <summary>Wirksamer Driftanteil: gepflegt oder die Vorgabe der Bauart (Kühlturm 0,01 % des Umlaufs, sonst 0).</summary>
        public double DriftAnteil => DriftAnteilGepflegt ?? (Nass ? KaelteFestwerte.RKW_DRIFT_ANTEIL : 0.0);

        /// <summary>Stunden des letzten <see cref="RueckkuehltemperaturenBilden"/>, in denen adiabat/hybrid ohne Feuchte trocken blieb.</summary>
        public int StundenOhneFeuchteTrocken { get; private set; }

        private static double? Endlich(double? v) =>
            v.HasValue && !double.IsNaN(v.Value) && !double.IsInfinity(v.Value) ? v : null;

        /// <summary>
        /// Baut die Rechenklasse aus der Projektkopie; <c>null</c> ohne Modell. Eine leere oder unbekannte Bauart rechnet
        /// trocken, ein nicht endlicher Wert wie ein leerer, ein Wert außerhalb seines Bereichs wie ein leerer.
        /// </summary>
        public static Rueckkuehlwerk AusModell(RueckkuehlwerkModel m)
        {
            if (m == null) return null;
            string bauart = m.Bauart != null && ((IList<string>)RueckkuehlwerkSchema.BAUARTEN).Contains(m.Bauart)
                ? m.Bauart : RueckkuehlwerkSchema.BAUART_TROCKEN;
            var r = new Rueckkuehlwerk
            {
                Id = m.Id,
                Bezeichner = string.IsNullOrEmpty(m.Bezeichner) ? m.Id.ToString(System.Globalization.CultureInfo.CurrentCulture) : m.Bezeichner,
                Bauart = bauart,
                AnnaeherungNennK = Endlich(m.Annaeherung_Nenn_K),
                NennleistungKw = Endlich(m.Nennleistung_kW) is double n && n > 0 ? n : (double?)null,
                Lastabhaengig = string.Equals(m.Annaeherung_Weg, RueckkuehlwerkSchema.WEG_LASTABHAENGIG, StringComparison.Ordinal),
                VentilatorNennKw = Endlich(m.Ventilator_Nenn_kW) is double v && v >= 0 ? v : (double?)null,
                VerdunstungFaktorGepflegt = Endlich(m.Verdunstung_Faktor) is double f && f > 0 ? f : (double?)null,
                DriftAnteilGepflegt = Endlich(m.Drift_Anteil) is double d && d >= 0 && d <= 1 ? d : (double?)null,
            };
            if (m.Ventilator_Regelung == RueckkuehlwerkSchema.REGELUNG_STUFEN ||
                m.Ventilator_Regelung == RueckkuehlwerkSchema.REGELUNG_DREHZAHL)
                r.Regelung = m.Ventilator_Regelung;
            if (m.Ventilator_Stufen.HasValue && m.Ventilator_Stufen.Value >= 2) r.Stufen = m.Ventilator_Stufen.Value;
            if (Endlich(m.Ventilator_Drehzahl_Min) is double dz && dz > 0 && dz <= 1) r.DrehzahlMin = dz;
            if (Endlich(m.Befeuchtung_Wirkungsgrad) is double eta && eta >= 0 && eta <= 1) r.BefeuchtungWirkungsgrad = eta;
            if (Endlich(m.Befeuchtung_Ab_C) is double ab) r.BefeuchtungAbC = ab;
            if (Endlich(m.Eindickung) is double e && e > 1) r.Eindickung = e;
            // K-F3: REIHE wird gerechnet (Teil-Freikühlung) - kein Merkmal mehr in NichtGerechnet.
            r.Reihe = string.Equals(m.Freikuehlung_Schaltung, RueckkuehlwerkSchema.SCHALTUNG_REIHE, StringComparison.Ordinal);
            return r;
        }

        // =====================================================================
        //  K-F3: Teil-Freikühlung in Reihe
        // =====================================================================

        /// <summary>
        /// K-F3 (Entwurf 5.5): Greift die Teil-Freikühlung? Ja, wenn die Fluidtemperatur des Rückkühlers
        /// <paramref name="fluidC"/> (Bezug + Annäherung) mindestens <see cref="KaelteFestwerte.FREIE_KUEHLUNG_ABSTAND_K"/> unter
        /// dem Kaltwasserrücklauf <paramref name="ruecklaufC"/> liegt — verglichen über <see cref="Rechenrand.SchwelleErreicht"/>
        /// (die Schwelle ist der Rücklauf minus Abstand, der Wert die Fluidtemperatur; Untergrenze, daher vertauscht).
        /// </summary>
        public static bool TeilFreikuehlungGreift(double ruecklaufC, double fluidC)
        {
            if (double.IsNaN(ruecklaufC) || double.IsNaN(fluidC) || double.IsInfinity(ruecklaufC) || double.IsInfinity(fluidC))
                return false;
            return Rechenrand.SchwelleErreicht(ruecklaufC - KaelteFestwerte.FREIE_KUEHLUNG_ABSTAND_K, fluidC);
        }

        /// <summary>
        /// K-F3 (Entwurf 5.5): die Kälte der Teil-Freikühlung einer Stunde [kWh]. Annahme: Die Spreizung der Stunde
        /// θ_R − θ_V ist gegeben, der Kaltwasserstrom folgt der Last — ṁ·c_p = Last / (θ_R − θ_V). Der Vorkühler kühlt den
        /// Rücklauf von θ_R höchstens auf θ_FK und deckt so den Anteil (θ_R − θ_FK) / (θ_R − θ_V) der Last, begrenzt auf 0…1:
        /// Q̇_FK = min(Last · clamp((θ_R − θ_FK) / (θ_R − θ_V), 0, 1), Rückkühlerleistung). c_p kürzt sich, keine Iteration.
        /// Die Variante mit konstantem (Nenn-)Massenstrom ist nicht gebaut. 0, wenn die Teil-Freikühlung nicht greift
        /// (<see cref="TeilFreikuehlungGreift"/>), die Spreizung nicht über 0 liegt (<see cref="Rechenrand.SchwelleErreicht"/>)
        /// oder eine Größe fehlt.
        /// </summary>
        /// <param name="kaelteKwh">Kälte der Stunde, die die Maschine decken soll [kWh].</param>
        /// <param name="vorlaufC">Kaltwasservorlauf der Stunde [°C].</param>
        /// <param name="ruecklaufC">Kaltwasserrücklauf der Stunde [°C] (ohne AK3 Vorlauf + Auslegungsspreizung, auf AK3 gerechnet).</param>
        /// <param name="fluidC">Fluidtemperatur des Rückkühlers (Bezug + Annäherung) [°C].</param>
        /// <param name="rueckkuehlerKw">Leistungsgrenze des Rückkühlers [kW].</param>
        public static double TeilFreikuehlungKwh(double kaelteKwh, double vorlaufC, double ruecklaufC, double fluidC,
                                                 double rueckkuehlerKw)
        {
            if (!(kaelteKwh > 0) || !(rueckkuehlerKw > 0) || double.IsNaN(vorlaufC) || double.IsInfinity(vorlaufC)) return 0.0;
            if (!TeilFreikuehlungGreift(ruecklaufC, fluidC)) return 0.0;
            double spreizung = ruecklaufC - vorlaufC;
            // Spreizung der Stunde ≤ 0 (am Rand): kein Kaltwasserstrom, den der Rückkühler vorkühlen könnte.
            if (Rechenrand.SchwelleErreicht(0.0, spreizung)) return 0.0;
            double anteil = (ruecklaufC - fluidC) / spreizung;
            if (!(anteil > 0)) return 0.0;
            if (anteil > 1.0) anteil = 1.0;
            double q = kaelteKwh * anteil;
            return q < rueckkuehlerKw ? q : rueckkuehlerKw;
        }

        // =====================================================================
        //  Bezugs- und Rückkühltemperatur
        // =====================================================================

        /// <summary>
        /// Die Rückkühltemperatur einer Stunde [°C] mit der Nenn-Annäherung (Weg <c>FEST</c>). <paramref name="ohneFeuchte"/>
        /// ist <c>true</c>, wenn ein Kühlturm ohne Luftfeuchte auf den Ausweichweg Außentemperatur −
        /// <see cref="KaelteFestwerte.NASSKUEHLER_OHNE_FEUCHTE_ABSCHLAG_K"/> geht; eine gepflegte Annäherung verschiebt ihn um
        /// ihren Abstand zur Vorgabe.
        /// </summary>
        public double Rueckkuehltemperatur(double aussenC, double? feuchteProzent, out bool ohneFeuchte) =>
            Rueckkuehltemperatur(aussenC, feuchteProzent, out ohneFeuchte, out _, out _, out _);

        /// <summary>
        /// Die Rückkühltemperatur einer Stunde [°C] samt Befeuchtung: <paramref name="befeuchtet"/> = adiabat/hybrid über der
        /// Schaltgrenze mit Feuchte, <paramref name="vorkuehlungK"/> die Absenkung der Lufteintrittstemperatur,
        /// <paramref name="trockenOhneFeuchte"/> = adiabat/hybrid über der Schaltgrenze, aber ohne Feuchte trocken gerechnet.
        /// </summary>
        public double Rueckkuehltemperatur(double aussenC, double? feuchteProzent, out bool ohneFeuchte,
                                           out bool befeuchtet, out double vorkuehlungK, out bool trockenOhneFeuchte)
        {
            ohneFeuchte = false;
            befeuchtet = false;
            vorkuehlungK = 0.0;
            trockenOhneFeuchte = false;
            bool mitFeuchte = feuchteProzent.HasValue && !double.IsNaN(feuchteProzent.Value);
            if (Nass)
            {
                if (mitFeuchte)
                    return Kaeltemaschine.Feuchtkugeltemperatur(aussenC, feuchteProzent.Value) + AnnaeherungK;
                ohneFeuchte = true;
                double ausweich = aussenC - KaelteFestwerte.NASSKUEHLER_OHNE_FEUCHTE_ABSCHLAG_K;
                return AnnaeherungNennK.HasValue ? ausweich + (AnnaeherungNennK.Value - AnnaeherungVorgabeK) : ausweich;
            }
            if (Befeuchtend && aussenC > BefeuchtungAbC)
            {
                if (!mitFeuchte)
                {
                    trockenOhneFeuchte = true;
                    return aussenC + AnnaeherungK;
                }
                double tw = Kaeltemaschine.Feuchtkugeltemperatur(aussenC, feuchteProzent.Value);
                vorkuehlungK = BefeuchtungWirkungsgrad * (aussenC - tw);
                befeuchtet = true;
                return aussenC - vorkuehlungK + AnnaeherungK;
            }
            return aussenC + AnnaeherungK;
        }

        /// <summary>
        /// Die Rückkühltemperaturen eines Jahres mit der Nenn-Annäherung — dieselbe Schleife wie
        /// <see cref="Kaeltemaschine.RueckkuehltemperaturenBilden"/>; <paramref name="stundenOhneFeuchte"/> zählt die Stunden
        /// des Ausweichwegs der Kühltürme, <see cref="StundenOhneFeuchteTrocken"/> die trockenen Stunden von adiabat/hybrid
        /// ohne Feuchte. Merkt sich je Stunde Befeuchtung und Vorkühlung für die Wasserbilanz.
        /// </summary>
        public double[] RueckkuehltemperaturenBilden(double[] aussenC, double[] feuchteProzent, out int stundenOhneFeuchte)
        {
            stundenOhneFeuchte = 0;
            int trocken = 0;
            int n = aussenC != null ? aussenC.Length : 0;
            var r = new double[n];
            _befeuchtet = new bool[n];
            _vorkuehlungK = new double[n];
            for (int h = 0; h < n; h++)
            {
                double? f = feuchteProzent != null && h < feuchteProzent.Length && !double.IsNaN(feuchteProzent[h])
                    ? feuchteProzent[h] : (double?)null;
                r[h] = Rueckkuehltemperatur(aussenC[h], f, out bool ohne, out bool bef, out double vk, out bool ohneTrocken);
                _befeuchtet[h] = bef;
                _vorkuehlungK[h] = vk;
                if (ohne) stundenOhneFeuchte++;
                if (ohneTrocken) trocken++;
            }
            StundenOhneFeuchteTrocken = trocken;
            return r;
        }

        /// <summary>Läuft das Rückkühlwerk in Stunde <paramref name="h"/> nass (Kühlturm immer, adiabat/hybrid befeuchtet)?</summary>
        public bool NassInStunde(int h) =>
            Nass || (_befeuchtet != null && h >= 0 && h < _befeuchtet.Length && _befeuchtet[h]);

        /// <summary>Die Vorkühlung der Luft in Stunde <paramref name="h"/> [K]; 0 ohne Befeuchtung.</summary>
        public double VorkuehlungK(int h) =>
            _vorkuehlungK != null && h >= 0 && h < _vorkuehlungK.Length ? _vorkuehlungK[h] : 0.0;

        // =====================================================================
        //  Last- und Luftanteil, lastabhängige Annäherung
        // =====================================================================

        /// <summary>
        /// Der Lastanteil x [—]: Abwärme / Nennleistung; ohne Nennleistung der Lastanteil der Maschinen (Kälte / Kapazität),
        /// der Nennpunkt ist dann die volle Leistung der Stunde. 0 ohne Wärme.
        /// </summary>
        public double Lastanteil(double abwaermeKwh, double kaelteKwh, double kapazitaetKw)
        {
            if (!(abwaermeKwh > 0)) return 0.0;
            if (NennleistungKw.HasValue) return abwaermeKwh / NennleistungKw.Value;
            return kapazitaetKw > 0 ? kaelteKwh / kapazitaetKw : 1.0;
        }

        /// <summary>Der Luftanteil L der Regelung beim Lastanteil <paramref name="x"/> [—]; 0 ohne Last, 1 bei Überlast.</summary>
        public double Luftanteil(double x)
        {
            if (!(x > 0)) return 0.0;
            if (x >= 1.0) return 1.0;
            switch (Regelung)
            {
                case RueckkuehlwerkSchema.REGELUNG_STUFEN:
                    {
                        int n = Math.Max(2, Stufen);
                        double stufe = Math.Ceiling(x * n - Rechenrand.ABSOLUT);
                        return Math.Max(1.0, Math.Min(n, stufe)) / n;
                    }
                case RueckkuehlwerkSchema.REGELUNG_DREHZAHL:
                    return Math.Max(x, DrehzahlMin);
                default:
                    return 1.0;
            }
        }

        /// <summary>
        /// Die lastabhängige Rückkühltemperatur [°C] aus der Rückkühltemperatur mit Nenn-Annäherung
        /// <paramref name="rkNennC"/> und dem Lastanteil <paramref name="x"/>: Bezug + ΔT_Nenn · x / L, höchstens
        /// <see cref="RueckkuehlwerkSchema.ANNAEHERUNG_MAX_K"/> über dem Bezug (bzw. die Nenn-Annäherung, wenn sie größer
        /// ist). Weg <c>FEST</c> oder x / L = 1: genau <paramref name="rkNennC"/>; x = 0: die Bezugstemperatur.
        /// </summary>
        public double RueckkuehltemperaturLastabhaengig(double rkNennC, double x)
        {
            if (!Lastabhaengig) return rkNennC;
            double l = Luftanteil(x);
            double verhaeltnis = l > 0 ? x / l : 0.0;
            if (verhaeltnis == 1.0) return rkNennC;
            double dt = Math.Min(AnnaeherungK * verhaeltnis, Math.Max(AnnaeherungK, RueckkuehlwerkSchema.ANNAEHERUNG_MAX_K));
            return rkNennC - AnnaeherungK + dt;
        }

        // =====================================================================
        //  Ventilator und Wasser einer Stunde
        // =====================================================================

        /// <summary>
        /// Der Ventilatorstrom der Stunde [kWh] beim Lastanteil <paramref name="x"/>; 0 ohne <c>Ventilator_Nenn_kW</c> oder
        /// ohne Last. Die Energie folgt dem Luftanteil der Annäherungsrechnung: <c>STUFEN</c> P · Stufe, <c>DREHZAHL</c> P · L³.
        /// <c>EIN_AUS</c> (auch leer gepflegt) hängt am Weg: Unter <c>FEST</c> taktet der Ventilator auf den Lastanteil und
        /// hält so im Mittel die Nenn-Annäherung — P · min(x, 1). Unter <c>LASTABHAENGIG</c> läuft er voll, solange die
        /// Maschine läuft (nur so sinkt die Annäherung auf ΔT_Nenn · x) — P · <paramref name="laufanteilMaschine"/>; ohne
        /// Laufanteil (<c>NaN</c>, etwa in freier Kühlung an der Nenn-Annäherung) gilt der feste Weg.
        /// </summary>
        public double VentilatorKwh(double x, double laufanteilMaschine = double.NaN)
        {
            if (!VentilatorNennKw.HasValue || !(x > 0)) return 0.0;
            double p = VentilatorNennKw.Value;
            double l = Luftanteil(x);
            switch (Regelung)
            {
                case RueckkuehlwerkSchema.REGELUNG_STUFEN: return p * l;
                case RueckkuehlwerkSchema.REGELUNG_DREHZAHL: return p * l * l * l;
                default:
                    if (Lastabhaengig && !double.IsNaN(laufanteilMaschine))
                        return p * Math.Max(0.0, Math.Min(1.0, laufanteilMaschine));
                    return p * Math.Min(x, 1.0);
            }
        }

        /// <summary>
        /// Die Bilanz einer Stunde: Abwärme <paramref name="abwaermeKwh"/>, Kälte und Kapazität der Maschinen (für den
        /// Lastanteil ohne Nennleistung), die Rückkühltemperatur, mit der die Stunde gerechnet wurde, und der Laufanteil der
        /// Maschine in der Stunde (<c>NaN</c> = an der Nenn-Annäherung gerechnet, siehe <see cref="VentilatorKwh"/>).
        /// </summary>
        public RueckkuehlwerkStunde Stunde(int h, double abwaermeKwh, double kaelteKwh, double kapazitaetKw, double rueckkuehlC,
                                           double laufanteilMaschine = double.NaN)
        {
            if (!(abwaermeKwh > 0)) return new RueckkuehlwerkStunde(0, 0, 0, 0, 0, 0, 0, false, rueckkuehlC);
            double x = Lastanteil(abwaermeKwh, kaelteKwh, kapazitaetKw);
            double l = Luftanteil(x);
            double ventilator = VentilatorKwh(x, laufanteilMaschine);
            bool nass = NassInStunde(h);
            double verdunstung = 0, abflutung = 0, drift = 0;
            if (nass)
            {
                double nasswaerme = Nass ? abwaermeKwh
                    : Math.Min(abwaermeKwh, abwaermeKwh * VorkuehlungK(h) / KaelteFestwerte.RKW_LUFTERWAERMUNG_K);
                verdunstung = WasserAusWaerme(nasswaerme) * VerdunstungFaktor;
                abflutung = verdunstung / (Eindickung - 1.0);
                drift = Nass ? DriftAnteil * UmlaufM3(abwaermeKwh) : DriftAnteil * verdunstung;
            }
            return new RueckkuehlwerkStunde(abwaermeKwh, x, l, ventilator, verdunstung, abflutung, drift, nass, rueckkuehlC);
        }

        /// <summary>Wasser, das die Wärme <paramref name="kwh"/> verdampft [m³]: kWh · 3 600 / h_fg / ρ.</summary>
        public static double WasserAusWaerme(double kwh) =>
            kwh * 3600.0 / KaelteFestwerte.RKW_VERDAMPFUNGSENTHALPIE_KJ_KG / KaelteFestwerte.RKW_WASSER_DICHTE_KG_M3;

        /// <summary>Umlaufwasser des Kühlturms für die Wärme <paramref name="kwh"/> [m³]: kWh · 3 600 / (c_p · Spreizung) / ρ.</summary>
        public static double UmlaufM3(double kwh) =>
            kwh * 3600.0 / (KaelteFestwerte.RKW_WASSER_CP_KJ_KGK * KaelteFestwerte.RKW_UMLAUF_SPREIZUNG_K)
            / KaelteFestwerte.RKW_WASSER_DICHTE_KG_M3;

        /// <summary>Wasserkosten [€/a] = Menge × Preis; <c>null</c> ohne Preis (keine Kosten, Hinweis).</summary>
        public static double? WasserkostenEur(double wasserM3, double? preisEurM3) =>
            preisEurM3.HasValue && !double.IsNaN(preisEurM3.Value) && preisEurM3.Value >= 0 ? wasserM3 * preisEurM3.Value : (double?)null;
    }
}

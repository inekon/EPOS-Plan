using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Rückkühlwerk einer Kältemaschine</b> (K-F1; Entwurf Split/VRF/Rückkühlwerk 5.1, 5.3, 5.6): ein eigenes Glied,
    /// kein Erzeuger und ohne Platz in der Kältefolge. Rein, ohne Datenbank — gebaut aus der Projektkopie
    /// (<see cref="AusModell"/>), gerechnet wird die Rückkühltemperatur der Stunde.
    ///
    /// <para><b>Weg <c>FEST</c>:</b> Rückkühltemperatur = Bezugstemperatur + Annäherung. Leere Annäherung = der Festwert der
    /// passenden Rückkühlart (<see cref="KaelteFestwerte.GRAEDIGKEIT_TROCKENKUEHLER_K"/> bzw.
    /// <see cref="KaelteFestwerte.GRAEDIGKEIT_NASSKUEHLER_K"/>); die Bezugstemperatur ist genau die von
    /// <see cref="Kaeltemaschine.Rueckkuehltemperatur(string, double, double?, out bool)"/> — so rechnet ein Rückkühlwerk mit
    /// leeren Feldern Bit für Bit wie die Rückkühlart (5.6).</para>
    ///
    /// <para><b>Bauart und Rückkühlart:</b> <c>TROCKEN</c> rechnet als Trockenkühler (Bezug Außenluft). Die Kühltürme
    /// (<c>KUEHLTURM_OFFEN</c>, <c>KUEHLTURM_GESCHLOSSEN</c>) rechnen als Nasskühler (Bezug Feuchtkugel nach Stull, ohne
    /// Feuchte der heutige Ausweichweg). <c>ADIABAT</c> und <c>HYBRID</c> rechnen in K-F1 auf ihrem trockenen Ast nach 5.3
    /// (adiabat unterhalb <c>Befeuchtung_Ab_C</c>, hybrid bis zur Schaltgrenze) — ohne Befeuchtung, also ohne neue Physik
    /// und auf der sicheren Seite; die Befeuchtung rechnet K-F2 und wird bis dahin benannt (<see cref="NichtGerechnet"/>).</para>
    ///
    /// <para><b>Was K-F1 nicht rechnet</b> — Annäherung <c>LASTABHAENGIG</c>, Ventilator, Befeuchtung, Wasserbilanz,
    /// Teil-Freikühlung in Reihe —, rechnet den Weg <c>FEST</c> und steht in <see cref="NichtGerechnet"/>; der Lauf meldet
    /// es einmal je Anlage.</para>
    /// </summary>
    public sealed class Rueckkuehlwerk
    {
        /// <summary>Merkmal: Annäherung <c>LASTABHAENGIG</c> (K-F2).</summary>
        public const string MERKMAL_LASTABHAENGIG = "LASTABHAENGIG";

        /// <summary>Merkmal: Ventilatorleistung und -regelung (K-F2).</summary>
        public const string MERKMAL_VENTILATOR = "VENTILATOR";

        /// <summary>Merkmal: Befeuchtung bzw. adiabate Vorkühlung (K-F2).</summary>
        public const string MERKMAL_BEFEUCHTUNG = "BEFEUCHTUNG";

        /// <summary>Merkmal: Wasserbilanz (Verdunstung, Eindickung, Drift; K-F2).</summary>
        public const string MERKMAL_WASSERBILANZ = "WASSERBILANZ";

        /// <summary>Merkmal: freie Kühlung in Reihe (Teil-Freikühlung, K-F3).</summary>
        public const string MERKMAL_REIHE = "REIHE";

        /// <summary>ID der Projektkopie.</summary>
        public int Id;

        /// <summary>Name des Rückkühlwerks.</summary>
        public string Bezeichner = "";

        /// <summary>Bauart (<see cref="RueckkuehlwerkSchema.BAUARTEN"/>); leer oder unbekannt = trocken.</summary>
        public string Bauart = RueckkuehlwerkSchema.BAUART_TROCKEN;

        /// <summary>Gepflegte Annäherung im Nennpunkt [K]; <c>null</c> = Festwert der passenden Rückkühlart.</summary>
        public double? AnnaeherungNennK;

        private readonly List<string> _nichtGerechnet = new List<string>();

        /// <summary>
        /// Die gepflegten Merkmale, die K-F1 noch nicht rechnet (<c>MERKMAL_*</c>), in fester Reihenfolge; leer = alles gerechnet.
        /// </summary>
        public IReadOnlyList<string> NichtGerechnet => _nichtGerechnet;

        /// <summary>Nasse Bauart: Bezug Feuchtkugel (die Kühltürme).</summary>
        public bool Nass =>
            Bauart == RueckkuehlwerkSchema.BAUART_KUEHLTURM_OFFEN || Bauart == RueckkuehlwerkSchema.BAUART_KUEHLTURM_GESCHLOSSEN;

        /// <summary>
        /// Die Rückkühlart, als die das Rückkühlwerk rechnet und die an der Maschine statt ihrer eigenen gilt (Entwurf 5.2):
        /// nass = <see cref="KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER"/>, sonst
        /// <see cref="KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER"/>.
        /// </summary>
        public string Rueckkuehlart => Nass ? KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER
                                            : KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER;

        /// <summary>Die Vorgabe der Annäherung [K]: der Festwert der passenden Rückkühlart.</summary>
        public double AnnaeherungVorgabeK => Nass ? KaelteFestwerte.GRAEDIGKEIT_NASSKUEHLER_K
                                                  : KaelteFestwerte.GRAEDIGKEIT_TROCKENKUEHLER_K;

        /// <summary>Die wirksame Annäherung [K]: gepflegt oder die Vorgabe.</summary>
        public double AnnaeherungK => AnnaeherungNennK ?? AnnaeherungVorgabeK;

        /// <summary>
        /// Baut die Rechenklasse aus der Projektkopie; <c>null</c> ohne Modell. Eine leere oder unbekannte Bauart rechnet
        /// trocken, eine nicht endliche Annäherung wie eine leere.
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
                AnnaeherungNennK = m.Annaeherung_Nenn_K.HasValue && !double.IsNaN(m.Annaeherung_Nenn_K.Value)
                                   && !double.IsInfinity(m.Annaeherung_Nenn_K.Value)
                    ? m.Annaeherung_Nenn_K : null,
            };
            if (string.Equals(m.Annaeherung_Weg, RueckkuehlwerkSchema.WEG_LASTABHAENGIG, StringComparison.Ordinal))
                r._nichtGerechnet.Add(MERKMAL_LASTABHAENGIG);
            if (m.Ventilator_Nenn_kW.HasValue || !string.IsNullOrEmpty(m.Ventilator_Regelung) ||
                m.Ventilator_Stufen.HasValue || m.Ventilator_Drehzahl_Min.HasValue)
                r._nichtGerechnet.Add(MERKMAL_VENTILATOR);
            if (bauart == RueckkuehlwerkSchema.BAUART_ADIABAT || bauart == RueckkuehlwerkSchema.BAUART_HYBRID ||
                m.Befeuchtung_Wirkungsgrad.HasValue || m.Befeuchtung_Ab_C.HasValue)
                r._nichtGerechnet.Add(MERKMAL_BEFEUCHTUNG);
            if (m.Verdunstung_Faktor.HasValue || m.Eindickung.HasValue || m.Drift_Anteil.HasValue)
                r._nichtGerechnet.Add(MERKMAL_WASSERBILANZ);
            if (string.Equals(m.Freikuehlung_Schaltung, RueckkuehlwerkSchema.SCHALTUNG_REIHE, StringComparison.Ordinal))
                r._nichtGerechnet.Add(MERKMAL_REIHE);
            return r;
        }

        /// <summary>
        /// Die Rückkühltemperatur einer Stunde [°C] auf dem Weg <c>FEST</c>: Bezugstemperatur + Annäherung.
        /// <paramref name="ohneFeuchte"/> ist <c>true</c>, wenn eine nasse Bauart ohne Luftfeuchte auf den Ausweichweg
        /// Außentemperatur − <see cref="KaelteFestwerte.NASSKUEHLER_OHNE_FEUCHTE_ABSCHLAG_K"/> geht; eine gepflegte Annäherung
        /// verschiebt ihn um ihren Abstand zur Vorgabe.
        /// </summary>
        public double Rueckkuehltemperatur(double aussenC, double? feuchteProzent, out bool ohneFeuchte)
        {
            ohneFeuchte = false;
            if (!Nass) return aussenC + AnnaeherungK;
            if (feuchteProzent.HasValue && !double.IsNaN(feuchteProzent.Value))
                return Kaeltemaschine.Feuchtkugeltemperatur(aussenC, feuchteProzent.Value) + AnnaeherungK;
            ohneFeuchte = true;
            double ausweich = aussenC - KaelteFestwerte.NASSKUEHLER_OHNE_FEUCHTE_ABSCHLAG_K;
            return AnnaeherungNennK.HasValue ? ausweich + (AnnaeherungNennK.Value - AnnaeherungVorgabeK) : ausweich;
        }

        /// <summary>
        /// Die Rückkühltemperaturen eines Jahres — dieselbe Schleife wie
        /// <see cref="Kaeltemaschine.RueckkuehltemperaturenBilden"/>; <paramref name="stundenOhneFeuchte"/> zählt die Stunden
        /// des Ausweichwegs.
        /// </summary>
        public double[] RueckkuehltemperaturenBilden(double[] aussenC, double[] feuchteProzent, out int stundenOhneFeuchte)
        {
            stundenOhneFeuchte = 0;
            int n = aussenC != null ? aussenC.Length : 0;
            var r = new double[n];
            for (int h = 0; h < n; h++)
            {
                double? f = feuchteProzent != null && h < feuchteProzent.Length && !double.IsNaN(feuchteProzent[h])
                    ? feuchteProzent[h] : (double?)null;
                r[h] = Rueckkuehltemperatur(aussenC[h], f, out bool ohne);
                if (ohne) stundenOhneFeuchte++;
            }
            return r;
        }
    }
}

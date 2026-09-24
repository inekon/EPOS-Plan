using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// ETAPPE E15 (V‑G7, Konzept Wirtschaftlichkeit § 2.11.2) — die <b>Arten der
    /// Risikoberücksichtigung</b> nach DIN EN 17463, Abschnitt 6.5: sprachneutrale
    /// ASCII-Schlüssel, so wie sie in <c>Tab_ProjektWirtschaftlichkeit.Risiko_Art</c> stehen.
    /// Leer, NULL und jeder unbekannte Wert heißen „kein Risiko angesetzt" (Vorgabe aus).
    /// </summary>
    public static class Risikoart
    {
        /// <summary>Risikozuschlag auf den Kalkulationszins [%-Punkte] (6.5, erste Möglichkeit).</summary>
        public const string ZINS = "ZINS";

        /// <summary>Zahlungsstromabzug R_loss × p_loss je Periode t ≥ 1 (6.5, zweite Möglichkeit;
        /// Anhang F).</summary>
        public const string ABZUG = "ABZUG";

        /// <summary>
        /// Der normierte Schlüssel: <see cref="ZINS"/>, <see cref="ABZUG"/> oder <c>null</c>
        /// (kein Risiko). Groß-/Kleinschreibung und Leerzeichen zählen nicht; ein
        /// unbekannter Bestandswert heißt „aus" — dieselbe tolerante Leseregel wie beim
        /// Modus der Stromsteuerbefreiung.
        /// </summary>
        public static string Normiert(string roh)
        {
            if (string.IsNullOrWhiteSpace(roh)) return null;
            string t = roh.Trim().ToUpperInvariant();
            if (string.Equals(t, ZINS, StringComparison.Ordinal)) return ZINS;
            if (string.Equals(t, ABZUG, StringComparison.Ordinal)) return ABZUG;
            return null;
        }
    }

    /// <summary>
    /// ETAPPE E15 (V‑G7) — das <b>Risikomodul</b> der Wirtschaftlichkeit: die EINE Stelle, an
    /// der aus den vier gepflegten Größen des Parametersatzes
    /// (<see cref="WirtschaftlichkeitParameter.RisikoArt"/> und die drei Zahlen) die
    /// rechenwirksamen Größen werden.
    ///
    /// <para><b>Zwei Wege nach DIN EN 17463, 6.5</b> — der Anwender wählt einen:</para>
    /// <list type="bullet">
    ///   <item><description><b>Zinszuschlag</b> (<see cref="Risikoart.ZINS"/>): Der
    ///     Kalkulationszins jedes Szenarios wird um den Zuschlag erhöht — angewandt in
    ///     <see cref="WirtschaftlichkeitParameter.FuerSzenario"/>, aus dem ALLE
    ///     Barwertrechnungen ihren Zins lesen (Kapitalwert, Annuität, Amortisation,
    ///     Differenzreihe und Zinsfuß, Verlauf, Gliederung, Sensitivität).</description></item>
    ///   <item><description><b>Zahlungsstromabzug</b> (<see cref="Risikoart.ABZUG"/>, Anhang F,
    ///     dort bevorzugt): Je Periode t ≥ 1 mindert <c>R_loss × p_loss / 100</c> die
    ///     Nettozahlung des Standes — nicht im Jahr 0, nicht auf den Restwert
    ///     (<see cref="KapitalwertRechner.Rechne"/>, Parameter <c>risikoAbzugJahr</c>). Der
    ///     interne Zinsfuß ist unverändert definiert und liest damit die risikobereinigte
    ///     Reihe.</description></item>
    /// </list>
    ///
    /// <para><b>Wen der Abzug trifft</b> (<see cref="AbzugFuerStand"/>): jeden Stand der Gruppe
    /// außer der Referenz des Laufs — die Referenz ist die Alternative, gegen die das Risiko
    /// der Investition gemessen wird (Anhang F: ein Risikoabzug, wenn die Investition nicht
    /// dasselbe Risiko trägt wie die Alternative). Rechnet die Gruppe nur einen Stand, ist er
    /// selbst die Investition und trägt den Abzug. Der Zinszuschlag gilt dagegen für die ganze
    /// Gruppe — er diskontiert die Differenzreihe mit dem risikoangepassten Zins.</para>
    ///
    /// <para><b>Szenarien</b> (E15‑Q1, Lesart a): Das Risiko gilt in allen drei Szenarien
    /// gleich.</para>
    ///
    /// <para><b>Vorgabe aus:</b> Ohne gepflegte Art, mit Zuschlag 0 oder ohne Verlust bzw.
    /// Wahrscheinlichkeit liefert jede Größe hier eine echte 0, und die Aufrufer fassen
    /// nichts an — der Rechenweg bleibt bitgleich der von vorher.</para>
    /// </summary>
    public static class RisikoModul
    {
        /// <summary>Die normierte Art des Parametersatzes (<c>null</c> = kein Risiko).</summary>
        public static string Art(WirtschaftlichkeitParameter p)
        {
            return p != null ? Risikoart.Normiert(p.RisikoArt) : null;
        }

        /// <summary>true, wenn der Zinszuschlag rechnet: Art ZINS und ein Zuschlag &gt; 0.</summary>
        public static bool ZinsAktiv(WirtschaftlichkeitParameter p)
        {
            return string.Equals(Art(p), Risikoart.ZINS, StringComparison.Ordinal) &&
                   p.RisikoZinszuschlag.HasValue && Endlich(p.RisikoZinszuschlag.Value) &&
                   p.RisikoZinszuschlag.Value > 0;
        }

        /// <summary>Der wirksame Zinszuschlag [%-Punkte]; 0 ohne Zinsrisiko.</summary>
        public static double Zinszuschlag(WirtschaftlichkeitParameter p)
        {
            return ZinsAktiv(p) ? p.RisikoZinszuschlag.Value : 0.0;
        }

        /// <summary>true, wenn der Zahlungsstromabzug rechnet: Art ABZUG, R_loss &gt; 0 und
        /// p_loss &gt; 0.</summary>
        public static bool AbzugAktiv(WirtschaftlichkeitParameter p)
        {
            return string.Equals(Art(p), Risikoart.ABZUG, StringComparison.Ordinal) &&
                   p.RisikoVerlust.HasValue && Endlich(p.RisikoVerlust.Value) && p.RisikoVerlust.Value > 0 &&
                   p.RisikoWahrscheinlichkeit.HasValue && Endlich(p.RisikoWahrscheinlichkeit.Value) &&
                   p.RisikoWahrscheinlichkeit.Value > 0;
        }

        /// <summary>Die wirksame Eintrittswahrscheinlichkeit p_loss [%], auf 100 % begrenzt.</summary>
        public static double Wahrscheinlichkeit(WirtschaftlichkeitParameter p)
        {
            return AbzugAktiv(p) ? Math.Min(100.0, p.RisikoWahrscheinlichkeit.Value) : 0.0;
        }

        /// <summary>
        /// Der Abzug je Periode [€/a] = R_loss × p_loss / 100 (DIN EN 17463, Anhang F:
        /// <c>ded_risk,t = f_ded_risk × P_t</c> mit dem Faktor aus R_loss und p_loss — hier mit
        /// R_loss als Betrag je Periode). 0 ohne Abzug.
        /// </summary>
        public static double AbzugJeJahr(WirtschaftlichkeitParameter p)
        {
            return AbzugAktiv(p) ? p.RisikoVerlust.Value * Wahrscheinlichkeit(p) / 100.0 : 0.0;
        }

        /// <summary>true, wenn überhaupt ein Risiko rechnet (Nachweiszeile, Annahmentafel,
        /// Parameterblock nennen es nur dann).</summary>
        public static bool Gepflegt(WirtschaftlichkeitParameter p)
        {
            return ZinsAktiv(p) || AbzugAktiv(p);
        }

        /// <summary>
        /// Trägt dieser Stand den Zahlungsstromabzug? Jeder Stand außer der Referenz des
        /// Laufs; rechnet die Gruppe nur einen Stand, trägt er ihn (Begründung an der Klasse).
        /// </summary>
        /// <param name="istReferenz">Der Stand ist die Referenz dieses Laufs.</param>
        /// <param name="anzahlStaende">Zahl der Stände, die der Lauf rechnet.</param>
        public static bool StandTraegtAbzug(bool istReferenz, int anzahlStaende)
        {
            return !istReferenz || anzahlStaende <= 1;
        }

        /// <summary>Der Abzug je Periode [€/a] für EINEN Stand — die Stelle, die
        /// <c>WirtschaftlichkeitCtrl</c> in Lauf und Verlauf fragt.</summary>
        public static double AbzugFuerStand(WirtschaftlichkeitParameter p, bool istReferenz, int anzahlStaende)
        {
            return StandTraegtAbzug(istReferenz, anzahlStaende) ? AbzugJeJahr(p) : 0.0;
        }

        /// <summary>
        /// Die Kurzfassung für Nachweiszeile, Deklaration und Annahmentafel — leer ohne
        /// Risiko. Beispiel: „Zinszuschlag 1,0 %-Punkte" bzw. „Abzug 10.000 € × 10 % =
        /// 1.000 € je Periode ab Jahr 1".
        /// </summary>
        public static string Kurz(WirtschaftlichkeitParameter p, CultureInfo kultur)
        {
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            if (ZinsAktiv(p))
                return string.Format(kultur, MyResource.Resource.WIRT_RISIKO_KURZ_ZINS,
                                     Zinszuschlag(p).ToString("N1", kultur));
            if (AbzugAktiv(p))
                return string.Format(kultur, MyResource.Resource.WIRT_RISIKO_KURZ_ABZUG,
                                     p.RisikoVerlust.Value.ToString("N0", kultur),
                                     Wahrscheinlichkeit(p).ToString("0.#", kultur),
                                     AbzugJeJahr(p).ToString("N0", kultur));
            return "";
        }

        /// <summary>
        /// Der Anhang an eine Nachweiszeile: „ · Risiko (DIN EN 17463, 6.5): …" — leer ohne
        /// Risiko, damit jede Zeile ohne Pflege Zeichen für Zeichen die von vorher bleibt.
        /// </summary>
        public static string Nachweis(WirtschaftlichkeitParameter p, CultureInfo kultur)
        {
            string kurz = Kurz(p, kultur);
            return kurz.Length == 0 ? "" : " · " + string.Format(kultur ?? CultureInfo.CurrentCulture,
                                                                 MyResource.Resource.WIRT_RISIKO_NACHWEIS, kurz);
        }

        /// <summary>
        /// Die Herleitungszeile der Dialoggruppe „Risiko": sagt, womit gerechnet wird — ohne
        /// Risiko, mit dem Zins i + Zuschlag oder mit dem Abzug je Periode.
        /// </summary>
        public static string Herleitung(WirtschaftlichkeitParameter p, CultureInfo kultur)
        {
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            if (ZinsAktiv(p))
                return string.Format(kultur, MyResource.Resource.WIRT_RISIKO_HERLEITUNG_ZINS,
                                     p.Zinssatz.ToString("N2", kultur),
                                     Zinszuschlag(p).ToString("N2", kultur),
                                     (p.Zinssatz + Zinszuschlag(p)).ToString("N2", kultur));
            if (AbzugAktiv(p))
                return string.Format(kultur, MyResource.Resource.WIRT_RISIKO_HERLEITUNG_ABZUG,
                                     p.RisikoVerlust.Value.ToString("N0", kultur),
                                     Wahrscheinlichkeit(p).ToString("0.##", kultur),
                                     AbzugJeJahr(p).ToString("N2", kultur));
            return MyResource.Resource.WIRT_RISIKO_HERLEITUNG_AUS;
        }

        private static bool Endlich(double w)
        {
            return !double.IsNaN(w) && !double.IsInfinity(w);
        }
    }
}

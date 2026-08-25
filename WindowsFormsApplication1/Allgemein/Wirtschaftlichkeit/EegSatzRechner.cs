using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Anzulegender Wert und feste Einspeisevergütung einer PV-Anlage samt
    /// Herleitung (PV-Konzept § 4.2/§ 6.2, Etappe P2). Alle Sätze in ct/kWh.
    /// </summary>
    public sealed class EegSatzErgebnis
    {
        /// <summary>Leistungsanteiliger anzulegender Wert AW_mix, gerundet auf
        /// 2 Nachkommastellen (Anzeige-/Anwendungswert).</summary>
        public double AwMixCt;

        /// <summary>AW_mix als UNGERUNDETER Quotient über den (gerundeten)
        /// Klassen-Tabellenwerten. Die N1-Regel „unrundet" gilt der
        /// DEGRESSIONSKETTE je Klasse (<see cref="EegSatzRechner.AwKlasseUnrundet"/>);
        /// der anzuwendende Klassenwert ist der gerundete BNetzA-Tabellenwert,
        /// und erst der Mix-Quotient darüber bleibt hier unrundet.</summary>
        public double AwMixCtUnrundet;

        /// <summary>Feste Einspeisevergütung EV_mix = AW_mix − Abschlag (§ 53 Abs. 1),
        /// gerundet; nur gültig, wenn <see cref="EvZulaessig"/>.</summary>
        public double EvMixCt;

        /// <summary>EV_mix unrundet.</summary>
        public double EvMixCtUnrundet;

        /// <summary>false, wenn kWp über der 100-kW-Grenze liegt (§ 21 Abs. 1 Nr. 1)
        /// — dann gibt es keine feste Einspeisevergütung.</summary>
        public bool EvZulaessig;

        /// <summary>Ausfallvergütung (§ 53 Abs. 3): AW_mix × (1 − Abschlag), gerundet.
        /// Nur für Anlagen &gt; 100 kW zulässig (<see cref="AusfallvergZulaessig"/>, N3).</summary>
        public double AusfallvergCt;

        /// <summary>true nur für Anlagen über der EV-Grenze (N3 — pv@now weist den
        /// Satz fälschlich auch darunter aus; nicht nachbauen).</summary>
        public bool AusfallvergZulaessig;

        /// <summary>Anzahl der angewandten Degressions-Halbjahresschritte.</summary>
        public int DegressionsSchritte;

        /// <summary>Leistungsanteil oberhalb der Ausschreibungsgrenze [kW] — für ihn
        /// gibt es keinen Katalogwert (AW = Zuschlagswert, manuelle Eingabe/Override).</summary>
        public double AusschreibungsAnteilKw;

        /// <summary>Klassenzerlegung für die Herleitungsanzeige:
        /// (Klassengrenze, Anteil kW, AW der Klasse unrundet).</summary>
        public List<(double GrenzeKw, double AnteilKw, double AwKlasseCt)> Zerlegung =
            new List<(double, double, double)>();

        /// <summary>Herleitung im Klartext (Klassen, Degressionsstand).</summary>
        public string Herleitung = "";

        /// <summary>true, wenn ein gebrauchter Katalogwert fehlt — die Herleitung
        /// nennt den Schlüssel; der betroffene Satz ist dann nicht belastbar.</summary>
        public bool Unvollstaendig;
    }

    /// <summary>
    /// Bildet anzulegenden Wert, feste Einspeisevergütung und Ausfallvergütung
    /// einer PV-Gebäudeanlage aus dem Katalog gesetzlicher Parameter
    /// (PV-Konzept § 4.2, Etappe P2; Muster <see cref="KwkgSatzRechner"/>:
    /// reine Funktion, Katalog als Delegat, UI-frei und damit testbar).
    ///
    /// <para><b>Degression auf UNRUNDETER Basis (Nachtrag N1).</b> Die BNetzA
    /// veröffentlicht neben den gerundeten auch die unrundeten Werte — sie beweisen,
    /// dass die Basis unrundet fortgeschrieben wird (Basis × 0,99ⁿ) und nur der
    /// ANZUWENDENDE Wert auf 2 Nachkommastellen gerundet ist: 8,60 × 0,99⁵ =
    /// 8,17851 (Fenster 02–07/2026), × 0,99⁶ = 8,09679 → 8,10 (ab 08/2026).
    /// Schrittweises Runden lieferte an Zwischenstichtagen abweichende Werte.</para>
    ///
    /// <para><b>Leistungsanteilige Mischrechnung (§ 23c EEG)</b> — Klassen sind
    /// marginale Tranchen, kein Stufentarif: 300 kWp Überschuss (IBN 08/2026) =
    /// (10×8,10 + 30×7,06 + 60×5,84 + 200×5,84) / 300 = 6,04 ct/kWh.</para>
    ///
    /// <para><b>Der AW ist über die Laufzeit fest</b> (Inbetriebnahmeprinzip):
    /// Die Degression bestimmt nur den Stichtagswert zur Inbetriebnahme.</para>
    /// </summary>
    public static class EegSatzRechner
    {
        /// <summary>Einspeiseart-Steuerwerte (Tab_ProjektPhotovoltaik.Einspeiseart).</summary>
        public const string EINSPEISEART_UEBERSCHUSS = "PV_UEBERSCHUSS";
        public const string EINSPEISEART_VOLL = "PV_VOLL";

        /// <summary>Erster Degressionsstichtag (§ 49 EEG i. d. F. Solarpaket).</summary>
        private static readonly DateTime DegressionsBeginn = new DateTime(2024, 2, 1);

        private static readonly double[] KlassenGrenzen = { 10, 40, 100, 400, 1000 };

        /// <summary>
        /// AW_mix, EV_mix und Ausfallvergütung für eine Anlage.
        /// </summary>
        /// <param name="kwp">installierte Leistung [kWp] (V3-Herleitung oder Override)</param>
        /// <param name="inbetriebnahme">Inbetriebnahmedatum — bestimmt den Degressionsstand</param>
        /// <param name="einspeiseart"><see cref="EINSPEISEART_UEBERSCHUSS"/> / <see cref="EINSPEISEART_VOLL"/></param>
        /// <param name="katalog">Lesefassade auf <c>Tab_Gesetzesparameter</c>: (Schlüssel, Jahr) → Wert</param>
        /// <param name="kultur">Zahlenformat der Herleitung</param>
        public static EegSatzErgebnis AnzulegenderWert(double kwp, DateTime inbetriebnahme,
                                                       string einspeiseart,
                                                       Func<string, int, double?> katalog,
                                                       CultureInfo kultur)
        {
            var e = new EegSatzErgebnis();
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            if (katalog == null || kwp <= 0)
            {
                e.Unvollstaendig = true;
                e.Herleitung = "Keine Leistung bzw. kein Katalog — kein anzulegender Wert.";
                return e;
            }

            int jahr = inbetriebnahme.Year;
            bool voll = string.Equals(einspeiseart, EINSPEISEART_VOLL, StringComparison.Ordinal);

            e.DegressionsSchritte = Degressionsschritte(inbetriebnahme);
            double? degression = katalog(DbWerte.GESETZ_EEG_DEGRESSION_HALBJAHR, jahr);
            double faktor = Math.Pow(1.0 - (degression ?? 1.0) / 100.0, e.DegressionsSchritte);
            if (!degression.HasValue) e.Unvollstaendig = true;

            var sb = new StringBuilder();
            sb.AppendFormat(kultur, "IBN {0:d}: {1} Degressionsschritte (1 %/Halbjahr ab 01.02.2024), Faktor {2:0.####}. ",
                            inbetriebnahme, e.DegressionsSchritte, faktor);

            string[] basisSchluessel =
            {
                DbWerte.GESETZ_EEG_AW_BASIS_UE_10, DbWerte.GESETZ_EEG_AW_BASIS_UE_40,
                DbWerte.GESETZ_EEG_AW_BASIS_UE_100, DbWerte.GESETZ_EEG_AW_BASIS_UE_400,
                DbWerte.GESETZ_EEG_AW_BASIS_UE_1000
            };
            string[] zuschlagSchluessel =
            {
                DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_10, DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_40,
                DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_100, DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_400,
                DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_1000
            };

            double ausschreibungsGrenze =
                katalog(DbWerte.GESETZ_EEG_AUSSCHREIBUNG_GRENZE_KW, jahr) ?? 1000.0;

            // Marginale Tranchen bis zur Ausschreibungsgrenze; der Rest hat keinen
            // Katalogwert (AW = Zuschlagswert → Override im Dialog).
            double summe = 0, verteilt = 0, untergrenze = 0;
            for (int k = 0; k < KlassenGrenzen.Length && verteilt < kwp; k++)
            {
                double obergrenze = Math.Min(KlassenGrenzen[k], ausschreibungsGrenze);
                double anteil = Math.Min(kwp, obergrenze) - untergrenze;
                if (anteil <= 0) { untergrenze = obergrenze; continue; }

                double? basis = katalog(basisSchluessel[k], jahr);
                double? zuschlag = voll ? katalog(zuschlagSchluessel[k], jahr) : 0.0;
                if (!basis.HasValue || (voll && !zuschlag.HasValue))
                {
                    e.Unvollstaendig = true;
                    sb.AppendFormat(kultur, "[Katalogwert {0} fehlt] ",
                                    basis.HasValue ? zuschlagSchluessel[k] : basisSchluessel[k]);
                    untergrenze = obergrenze;
                    continue;
                }

                // Der ANZUWENDENDE Klassenwert ist der auf 2 Nachkommastellen
                // gerundete BNetzA-Tabellenwert (N1: unrundet bleibt nur die
                // Degressionskette selbst); die Mischrechnung des § 23c setzt auf
                // diesen Tabellenwerten auf — Konzept § 3.1: (10×8,10 + 30×7,06 +
                // 260×5,84)/300 = 6,04, nicht 6,03 aus der unrundeten Kette.
                double awKlasse = Math.Round(
                    (basis.Value + (voll ? zuschlag.Value : 0.0)) * faktor, 2,
                    MidpointRounding.AwayFromZero);
                e.Zerlegung.Add((KlassenGrenzen[k], anteil, awKlasse));
                summe += anteil * awKlasse;
                verteilt += anteil;
                untergrenze = obergrenze;
            }

            e.AusschreibungsAnteilKw = Math.Max(0, kwp - verteilt);
            if (e.AusschreibungsAnteilKw > 0.0001)
            {
                e.Unvollstaendig = true;
                sb.AppendFormat(kultur,
                    "{0:0.#} kW liegen über der Ausschreibungsgrenze ({1:0} kW) — AW dafür ist der " +
                    "Zuschlagswert (manuelle Eingabe/Override). ",
                    e.AusschreibungsAnteilKw, ausschreibungsGrenze);
            }

            e.AwMixCtUnrundet = verteilt > 0 ? summe / verteilt : 0;
            e.AwMixCt = Math.Round(e.AwMixCtUnrundet, 2, MidpointRounding.AwayFromZero);

            foreach (var z in e.Zerlegung)
                sb.AppendFormat(kultur, "bis {0:0} kW: {1:0.#} kW × {2:0.00###} ct/kWh; ",
                                z.GrenzeKw, z.AnteilKw, z.AwKlasseCt);
            sb.AppendFormat(kultur, "AW_mix = {0:0.00} ct/kWh ({1}).",
                            e.AwMixCt, voll ? "Volleinspeisung" : "Überschusseinspeisung");

            // Feste Einspeisevergütung (§ 53 Abs. 1) — nur bis zur EV-Grenze.
            double evGrenze = katalog(DbWerte.GESETZ_EEG_EV_GRENZE_KW, jahr) ?? 100.0;
            double evAbschlag = katalog(DbWerte.GESETZ_EEG_EV_ABSCHLAG, jahr) ?? 0.4;
            e.EvZulaessig = kwp <= evGrenze + 0.0001;
            e.EvMixCtUnrundet = Math.Max(0, e.AwMixCtUnrundet - evAbschlag);
            e.EvMixCt = Math.Round(e.EvMixCtUnrundet, 2, MidpointRounding.AwayFromZero);

            // Ausfallvergütung (§ 53 Abs. 3) — nur > EV-Grenze (N3).
            double ausfallAbschlag = katalog(DbWerte.GESETZ_EEG_AUSFALLVERG_ABSCHLAG, jahr) ?? 20.0;
            e.AusfallvergZulaessig = kwp > evGrenze + 0.0001;
            e.AusfallvergCt = Math.Round(e.AwMixCtUnrundet * (1.0 - ausfallAbschlag / 100.0), 2,
                                         MidpointRounding.AwayFromZero);

            e.Herleitung = sb.ToString();
            return e;
        }

        /// <summary>
        /// AW einer EINZELNEN Größenklasse (unrundet) — der Baustein der
        /// BNetzA-Tabellen-Tests: Basiswert (+ Voll-Zuschlag) × 0,99ⁿ.
        /// <paramref name="klasseKw"/> ∈ {10, 40, 100, 400, 1000}.
        /// </summary>
        public static double? AwKlasseUnrundet(double klasseKw, DateTime inbetriebnahme, bool voll,
                                               Func<string, int, double?> katalog)
        {
            int idx = Array.IndexOf(KlassenGrenzen, klasseKw);
            if (idx < 0 || katalog == null) return null;

            string[] basis = { DbWerte.GESETZ_EEG_AW_BASIS_UE_10, DbWerte.GESETZ_EEG_AW_BASIS_UE_40,
                               DbWerte.GESETZ_EEG_AW_BASIS_UE_100, DbWerte.GESETZ_EEG_AW_BASIS_UE_400,
                               DbWerte.GESETZ_EEG_AW_BASIS_UE_1000 };
            string[] zu = { DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_10, DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_40,
                            DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_100, DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_400,
                            DbWerte.GESETZ_EEG_AW_VOLL_ZUSCHLAG_1000 };

            int jahr = inbetriebnahme.Year;
            double? b = katalog(basis[idx], jahr);
            double? z = voll ? katalog(zu[idx], jahr) : 0.0;
            if (!b.HasValue || !z.HasValue) return null;

            double? degression = katalog(DbWerte.GESETZ_EEG_DEGRESSION_HALBJAHR, jahr);
            double faktor = Math.Pow(1.0 - (degression ?? 1.0) / 100.0,
                                     Degressionsschritte(inbetriebnahme));
            return (b.Value + z.Value) * faktor;
        }

        /// <summary>Gerundete Fassung von <see cref="AwKlasseUnrundet"/> (Anwendungswert).</summary>
        public static double? AwKlasse(double klasseKw, DateTime inbetriebnahme, bool voll,
                                       Func<string, int, double?> katalog)
        {
            double? u = AwKlasseUnrundet(klasseKw, inbetriebnahme, voll, katalog);
            return u.HasValue ? Math.Round(u.Value, 2, MidpointRounding.AwayFromZero) : (double?)null;
        }

        /// <summary>
        /// Zahl der Halbjahresstichtage (1.2. und 1.8., erstmals 01.02.2024) bis
        /// EINSCHLIESSLICH des Inbetriebnahmedatums: IBN 01.08.2026 → 6,
        /// IBN 25.07.2026 → 5, IBN vor dem 01.02.2024 → 0.
        /// </summary>
        public static int Degressionsschritte(DateTime inbetriebnahme)
        {
            int n = 0;
            DateTime stichtag = DegressionsBeginn;
            while (stichtag <= inbetriebnahme.Date)
            {
                n++;
                stichtag = stichtag.Month == 2
                    ? new DateTime(stichtag.Year, 8, 1)
                    : new DateTime(stichtag.Year + 1, 2, 1);
            }
            return n;
        }
    }
}

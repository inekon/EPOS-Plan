using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Zeitstruktur einer Zone, normiert und geprüft (Konzept 4.2): zwölf Monatsfaktoren
    /// (Katalog oder Auslastungsgang der Zone), sieben Wochenfaktoren (Σ 1), Ferienfaktor
    /// (<c>null</c> = wie Sonntag) und vier Tagesgänge zu 24 Anteilen (je Σ 1; ein Tagesgang mit
    /// Σ 0 bleibt 0 und ist in <see cref="TagtypLeer"/> gekennzeichnet).
    ///
    /// <para><b>Nur intern:</b> Die Felder sind Arrays und damit veränderlich; die Zeitstruktur
    /// verlässt den Rechenweg nicht (weder Ergebnis noch Hülle sehen sie) und wird nach
    /// <see cref="Formvektor.Bilden"/> nur gelesen.</para>
    /// </summary>
    internal sealed record Zeitstruktur(double[] Monatsfaktoren, double[] Wochenfaktoren, double? Ferienfaktor,
                                        double[,] Tagesgaenge, bool[] TagtypLeer);

    /// <summary>
    /// <b>Schicht S2 — der Formvektor</b> (Umsetzungskonzept Zapfprofilgenerator 2.1, 4.2).
    ///
    /// <code>
    /// w_T(d) = w(Wochentag(d))       Werktag
    ///        = w(Samstag)            Samstag
    ///        = w(Sonntag)            SonnFeiertag (auch ein Feiertag am Montag)
    ///        = f_F · w̄, w̄ = Σ w / 7  Ruhetag; Ferienfaktor NULL -> w(Sonntag)
    /// g(d)   = f_Monat(m(d)) · f_KW(m(d)) · 7 · w_T(d)
    /// Q_d    = Q_a · g(d) / Σ_d g(d)                    -> Σ_d Q_d = Q_a
    /// q_h    = Q_d · φ_Tagtyp(d)(h), Σ_h φ = 1         -> Σ_h q_h = Q_a
    /// </code>
    ///
    /// <para>Ein Tag, dessen Tagesgang leer ist (Σ φ = 0), erhält das Gewicht 0 — die Menge
    /// geht an die übrigen Tage, die Jahresmenge bleibt erhalten, und ein Hinweis nennt den
    /// Tagtyp. Verteilt die Zeitstruktur gar nichts (jedes g(d) = 0) bei positiver Jahresmenge,
    /// wird benannt abgelehnt. Der Formvektor ist die Stundenreihe zur Jahresmenge 1.</para>
    /// </summary>
    internal static class Formvektor
    {
        /// <summary>
        /// Baut die Zeitstruktur einer Zone aus Nutzungsart, Tagesgangsatz und den
        /// Überschreibungen der Zone; normiert Wochenfaktoren und Tagesgänge und meldet eine
        /// Abweichung der Summe über der Warnschwelle als Hinweis. Ungültige Raster (falsche
        /// Länge, negativ, nicht endlich) werden benannt abgelehnt.
        /// </summary>
        internal static Zeitstruktur Bilden(ZonenStand z, Nutzungsart n, Tagesgangsatz satz, Parametersatz ps,
                                            Herkunftsprotokoll p, ICollection<ZapfHinweis> hinweise)
        {
            string zone = z.Name ?? "";
            double? warnschwelle = ps != null && ps.Enthaelt(ZapfParameter.FORMVEKTOR_WARNSCHWELLE)
                                   ? ps.Wert(ZapfParameter.FORMVEKTOR_WARNSCHWELLE) : (double?)null;
            if (!warnschwelle.HasValue)
                ZapfHinweis.Einmal(hinweise, ZapfHinweis.ParameterFehlt(ZapfParameter.FORMVEKTOR_WARNSCHWELLE,
                    ZapfSatz.Neu("FOLGE_SUMMEN_NICHT_GEPRUEFT")));

            // --- Monate: Katalog, je Monat überschreibbar durch den Auslastungsgang ------------
            double[] monateKatalog = Raster(n.Monatsfaktoren, Zapfkalender.MONATE, zone, ZapfSatz.Neu("BEGRIFF_MONATSFAKTOREN"));
            var monate = new double[Zapfkalender.MONATE];
            bool ueberschrieben = false;
            for (int m = 0; m < Zapfkalender.MONATE; m++)
            {
                double? a = z.Auslastung != null && m < z.Auslastung.Length ? z.Auslastung[m] : null;
                if (a.HasValue)
                {
                    monate[m] = NichtNegativ(a.Value, zone, ZapfSatz.Neu("BEGRIFF_AUSLASTUNGSGANG"));
                    ueberschrieben = true;
                }
                else monate[m] = monateKatalog[m];
            }
            p?.Vermerken(zone, ZapfFeld.MONATSFAKTOREN, null, "-",
                         ueberschrieben ? Wertstatus.Ueberschrieben : Wertstatus.Vorgabe,
                         ueberschrieben ? null : n.Herkunft?.Jahresgang,
                         ueberschrieben ? "Auslastungsgang der Zone" : "");

            // --- Woche: Σ 1 ---------------------------------------------------------------
            double[] woche = Raster(n.Wochenfaktoren, Zapfkalender.WOCHENTAGE, zone, ZapfSatz.Neu("BEGRIFF_WOCHENFAKTOREN"));
            double summeWoche = 0.0;
            for (int i = 0; i < Zapfkalender.WOCHENTAGE; i++) summeWoche += woche[i];
            if (!(summeWoche > 0))
                throw new ZapfprofilEingabeException(ZapfEingabefehler.KeineVerteilung, zone,
                    ZapfSatz.Neu("EINGABE_WOCHENFAKTOREN_NULL", n.Name ?? ""));
            SummeWarnen(summeWoche, warnschwelle, zone, "WOCHENFAKTOREN_SUMME", ZapfSatz.Neu("BEGRIFF_WOCHENFAKTOREN"), hinweise);
            var wocheNormiert = new double[Zapfkalender.WOCHENTAGE];
            for (int i = 0; i < Zapfkalender.WOCHENTAGE; i++) wocheNormiert[i] = woche[i] / summeWoche;
            p?.Vermerken(zone, ZapfFeld.WOCHENFAKTOREN, summeWoche, "-", Wertstatus.Vorgabe, n.Herkunft?.Wochengang,
                         "Summe vor der Normierung");

            // --- Ferienfaktor ---------------------------------------------------------------
            double? ferien = n.Ferienfaktor;
            if (ferien.HasValue) NichtNegativ(ferien.Value, zone, ZapfSatz.Neu("BEGRIFF_FERIENFAKTOR"));
            p?.Vermerken(zone, ZapfFeld.FERIENFAKTOR, ferien, "-", Wertstatus.Vorgabe, n.Herkunft?.Jahresgang,
                         ferien.HasValue ? "" : "wie Sonntag");

            // --- Tagesgänge: je Σ 1 ---------------------------------------------------------
            if (satz == null || !satz.Vollstaendig || satz.Anteile == null
                || satz.Anteile.GetLength(0) != Tagesgangsatz.TAGTYPEN || satz.Anteile.GetLength(1) != Tagesgangsatz.STUNDEN)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.TagesgangsatzFehlt, zone,
                    ZapfSatz.Neu("EINGABE_TAGESGANGSATZ_UNVOLLSTAENDIG", zone));
            var gaenge = new double[Tagesgangsatz.TAGTYPEN, Tagesgangsatz.STUNDEN];
            var leer = new bool[Tagesgangsatz.TAGTYPEN];
            for (int t = 0; t < Tagesgangsatz.TAGTYPEN; t++)
            {
                double s = 0.0;
                for (int h = 0; h < Tagesgangsatz.STUNDEN; h++)
                    s += NichtNegativ(satz.Anteile[t, h], zone, ZapfSatz.Neu("BEGRIFF_TAGESGANG"));
                if (s == 0.0)
                {
                    leer[t] = true;
                    hinweise?.Add(new ZapfHinweis(zone, "TAGESGANG_LEER",
                        ZapfSatz.Neu("HINWEIS_TAGESGANG_LEER", Tagtyp((ZapfTagtyp)(t + 1)), zone)));
                    continue;
                }
                SummeWarnen(s, warnschwelle, zone, "TAGESGANG_SUMME",
                            ZapfSatz.Neu("BEGRIFF_TAGESGANG_TAGTYP", Tagtyp((ZapfTagtyp)(t + 1))), hinweise);
                for (int h = 0; h < Tagesgangsatz.STUNDEN; h++) gaenge[t, h] = satz.Anteile[t, h] / s;
            }
            p?.Vermerken(zone, ZapfFeld.TAGESGANGSATZ, satz.Id, "ID",
                         z.IdTagesgangsatz.HasValue ? Wertstatus.Ueberschrieben : Wertstatus.Vorgabe,
                         satz.JeTagtyp[0], satz.Bezeichner);

            return new Zeitstruktur(monate, wocheNormiert, ferien, gaenge, leer);
        }

        /// <summary>Das Tagesgewicht w_T(d) nach Tagtyp (4.2).</summary>
        internal static double Tagesgewicht(Zeitstruktur s, ZapfTagtyp typ, int wochentag)
        {
            switch (typ)
            {
                case ZapfTagtyp.Werktag: return s.Wochenfaktoren[wochentag];
                case ZapfTagtyp.Samstag: return s.Wochenfaktoren[Zapfkalender.SAMSTAG];
                case ZapfTagtyp.SonnFeiertag: return s.Wochenfaktoren[Zapfkalender.SONNTAG];
                case ZapfTagtyp.Ruhetag:
                    if (!s.Ferienfaktor.HasValue) return s.Wochenfaktoren[Zapfkalender.SONNTAG];
                    double summe = 0.0;
                    for (int i = 0; i < Zapfkalender.WOCHENTAGE; i++) summe += s.Wochenfaktoren[i];
                    return s.Ferienfaktor.Value * (summe / Zapfkalender.WOCHENTAGE);
            }
            throw new ArgumentOutOfRangeException(nameof(typ));
        }

        /// <summary>
        /// <b>Die 365 Tagesmengen</b> [kWh]: <c>Q_d = Q_a · g(d) / Σ g</c>. Verteilt die
        /// Zeitstruktur nichts bei positiver Jahresmenge, benannte Ablehnung.
        /// </summary>
        internal static double[] Tagesmengen(double jahresKwh, Zeitstruktur s, ZapfTagtyp[] kalender, int wochentagJan1,
                                             double[] kaltwasserfaktor, string zone = "")
        {
            if (kalender == null || kalender.Length != Zapfkalender.TAGE)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.KalenderUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_KALENDER_TAGE"));
            if (kaltwasserfaktor == null || kaltwasserfaktor.Length != Zapfkalender.MONATE)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_KALTWASSERFAKTOR_RASTER"));

            var g = new double[Zapfkalender.TAGE];
            double summe = 0.0;
            for (int d = 1; d <= Zapfkalender.TAGE; d++)
            {
                ZapfTagtyp typ = kalender[d - 1];
                double gewicht = 0.0;
                if (!s.TagtypLeer[(int)typ - 1])
                {
                    int m = Zapfkalender.Monat(d) - 1;
                    double wt = Tagesgewicht(s, typ, Zapfkalender.Wochentag(wochentagJan1, d));
                    gewicht = s.Monatsfaktoren[m] * kaltwasserfaktor[m] * Zapfkalender.WOCHENTAGE * wt;
                }
                g[d - 1] = gewicht;
                summe += gewicht;
            }

            var mengen = new double[Zapfkalender.TAGE];
            if (summe == 0.0)
            {
                if (jahresKwh != 0.0)
                    throw new ZapfprofilEingabeException(ZapfEingabefehler.KeineVerteilung, zone,
                        ZapfSatz.Neu("EINGABE_KEINE_VERTEILUNG", zone));
                return mengen;
            }
            for (int d = 0; d < Zapfkalender.TAGE; d++) mengen[d] = jahresKwh * g[d] / summe;
            return mengen;
        }

        /// <summary>
        /// <b>Die Stundenreihe der Bilanz</b> [kWh je Stunde], 8760 Werte:
        /// <c>q_h = Q_d · φ_Tagtyp(d)(h)</c>. Nur für die Bilanz — die Auslegung liest sie nie.
        /// </summary>
        internal static double[] Stundenreihe(double[] tagesmengen, Zeitstruktur s, ZapfTagtyp[] kalender)
        {
            if (tagesmengen == null || tagesmengen.Length != Zapfkalender.TAGE)
                throw new ArgumentException("Die Tagesmengen tragen nicht 365 Tage.", nameof(tagesmengen));
            var reihe = new double[Zapfkalender.STUNDEN_JAHR];
            for (int d = 0; d < Zapfkalender.TAGE; d++)
            {
                int t = (int)kalender[d] - 1;
                for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
                    reihe[d * Zapfkalender.STUNDEN_TAG + h] = tagesmengen[d] * s.Tagesgaenge[t, h];
            }
            return reihe;
        }

        /// <summary>
        /// <b>Der normierte Formvektor</b> je Stunde: die Stundenreihe zur Jahresmenge 1
        /// (Σ = 1 bis auf die Rundung der Summation).
        /// </summary>
        internal static double[] Form(Zeitstruktur s, ZapfTagtyp[] kalender, int wochentagJan1, double[] kaltwasserfaktor,
                                      string zone = "")
        {
            return Stundenreihe(Tagesmengen(1.0, s, kalender, wochentagJan1, kaltwasserfaktor, zone), s, kalender);
        }

        // =================================================================================
        // Hilfen
        // =================================================================================

        private static double[] Raster(double[] werte, int laenge, string zone, ZapfSatz was)
        {
            if (werte == null || werte.Length != laenge)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_RASTER_LAENGE", was, laenge, zone));
            foreach (double w in werte) NichtNegativ(w, zone, was);
            return werte;
        }

        private static double NichtNegativ(double w, string zone, ZapfSatz was)
        {
            if (double.IsNaN(w) || double.IsInfinity(w) || w < 0)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.RasterUngueltig, zone,
                    ZapfSatz.Neu("EINGABE_RASTER_NEGATIV", was, zone));
            return w;
        }

        /// <summary>Der Tagtyp als Begriff (<c>BEGRIFF_TAGTYP_…</c>) — sprachfrei für die Sätze des Kerns.</summary>
        internal static ZapfSatz Tagtyp(ZapfTagtyp t) => ZapfSatz.Neu("BEGRIFF_TAGTYP_" + ((int)t).ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// Hinweis, wenn eine Summe über der Warnschwelle des Parametersatzes von 1 abweicht.
        /// Ohne Parameter keine Prüfung und kein Rückfallwert — den fehlenden Schlüssel nennt
        /// <see cref="Bilden"/> einmal als Hinweis (N7).
        /// </summary>
        private static void SummeWarnen(double summe, double? schwelle, string zone, string code, ZapfSatz was,
                                        ICollection<ZapfHinweis> hinweise)
        {
            if (!schwelle.HasValue) return;
            if (hinweise != null && Math.Abs(summe - 1.0) > schwelle.Value)
                hinweise.Add(new ZapfHinweis(zone, code, ZapfSatz.Neu("HINWEIS_SUMME_NORMIERT", was, zone, summe)));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Was beim Bilden eines Ersatzaufbaus vom Regelfall abwich (Flaggen).</summary>
    [Flags]
    internal enum Ersatzvermerk
    {
        /// <summary>Regelfall: Vorgabetyp, abgeglichen im Band.</summary>
        Keiner = 0,

        /// <summary>Der Typ folgt dem Ziel-U statt der Baualtersklasse (Außenwand massiv: mit bzw. ohne Dämmung).</summary>
        TypNachU = 1,

        /// <summary>Das Ziel-U liegt über dem U des Typs ohne Dämmschicht — die Dämmschicht entfällt (Konzept 5.3).</summary>
        DaemmungEntfaellt = 2,

        /// <summary>Der Abgleich läge außerhalb des Bands (Dämmdicke, λ-Faktor) — der Typaufbau bleibt ohne Abgleich.</summary>
        AusserhalbBand = 4,
    }

    /// <summary>
    /// Ein Ersatzaufbau: der Typ, das Aufbaumodell (Werte der Normsaat, abgeglichen), das Ziel-U, das U aus den
    /// Schichten, die Art des Abgleichs, der abgeglichene Wert (Dämmdicke [m] bzw. λ [W/(mK)]; <c>null</c> = ohne
    /// Abgleich), der Bedarf des Abgleichs vor dem Band und die Vermerke.
    /// </summary>
    internal sealed record Ersatzergebnis(TypaufbauSaat Typ, BauteilaufbauModel Aufbau, double UZiel, double? USchichten,
                                          Typabgleich Abgleich, double? Wert, double? Bedarf, Ersatzvermerk Vermerk);

    /// <summary>
    /// <b>Der Ersatzaufbau eines Außenbauteils ohne vollständigen Aufbau</b> (Konzept Bauteilaufbau beim Import 5.3,
    /// Entscheid E95-3/4/5, BA-2) — reine Daten, keine Rechenregel; ohne Datenbank.
    /// <list type="bullet">
    /// <item><b>Wer einen bekommt</b> (<see cref="Erhaelt"/>): opake Hüllbauteile (Außenwand, Dach, Bodenplatte, Decke,
    /// Innenwand gegen unbeheizt) an Außenluft, Erdreich oder unbeheizt. Innenbauteile bleiben im Klassenweg (E95-5), Türen und
    /// transparente Bauteile ohne.</item>
    /// <item><b>Wahl des Typs</b> (<see cref="Vorgabetyp"/>, E95-4): Bauart leicht → Holzleichtbau bzw. Sparrendach;
    /// sonst Außenwand bis Klasse F ungedämmt, ab G (und ohne Klasse bei einem Ziel-U unter dem des ungedämmten
    /// Typs) außen gedämmt; Dach Stahlbeton gedämmt; Boden gegen Erdreich mit Dämmung oben, sonst unten.</item>
    /// <item><b>Abgleich</b> (<see cref="Abgleichen"/>), geschlossen über R: R_rest = 1/U₀ − d₀/λ (U₀ des Typs über
    /// <see cref="Bauteilreduktion.UWertAusSchichten"/>, also mit R_si/R_se nach DIN EN ISO 6946 wie im Lauf);
    /// gedämmte Typen: d = λ·(1/U_Ziel − R_rest) im Band <see cref="TypaufbauSaat.DAEMMDICKE_MIN_M"/> … <see cref="TypaufbauSaat.DAEMMDICKE_MAX_M"/>;
    /// Typen ohne Dämmung: λ′ = d/(1/U_Ziel − R_rest) im Band <see cref="TypaufbauSaat.LAMBDA_FAKTOR_MIN"/> …
    /// <see cref="TypaufbauSaat.LAMBDA_FAKTOR_MAX"/>·λ. d unter <see cref="TypaufbauSaat.DAEMMDICKE_MIN_M"/>: die massive Außenwand wechselt auf den ungedämmten
    /// Typ, jeder andere Typ verliert die Dämmschicht; λ′ unter dem Band: die ungedämmte Außenwand wechselt auf
    /// den gedämmten Typ; sonst außerhalb des Bands der Typ ohne Abgleich mit Vermerk.</item>
    /// </list>
    /// </summary>
    internal static class Ersatzaufbau
    {
        /// <summary>Bekommt ein Bauteil dieser Art an diesem Rand einen Ersatzaufbau?</summary>
        internal static bool Erhaelt(Bauteilart art, Bauteilrand rand)
        {
            if (rand != Bauteilrand.Aussenluft && rand != Bauteilrand.Erdreich && rand != Bauteilrand.Unbeheizt) return false;
            // Innenwand und Decke zählen hier nur an einem äußeren Rand (gegen unbeheizt): Sie gehören zur Hülle.
            return art == Bauteilart.Aussenwand || art == Bauteilart.Innenwand || art == Bauteilart.Dach || art == Bauteilart.Bodenplatte
                   || art == Bauteilart.Decke;
        }

        /// <summary>Gehört das Bauteil zur Familie Dach (Wärmestrom nach oben) — Dach oder Decke mit Neigung unter 90°?</summary>
        private static bool IstDach(Bauteilart art, double neigung)
            => art == Bauteilart.Dach || (art == Bauteilart.Decke && neigung < 90.0);

        /// <summary>Gehört das Bauteil zur Familie Boden — Bodenplatte oder Decke mit Neigung ab 90°?</summary>
        private static bool IstBoden(Bauteilart art, double neigung)
            => art == Bauteilart.Bodenplatte || (art == Bauteilart.Decke && neigung >= 90.0);

        /// <summary>Der Vorgabetyp nach Bauteilart, Rand, Bauart und Baualtersklasse (E95-4).</summary>
        internal static TypaufbauSaat Vorgabetyp(Bauteilart art, Bauteilrand rand, double neigung, string bauart, char? klasse, double uZiel)
        {
            bool leicht = string.Equals(bauart, GebaeudeZielfelder.BAUART_LEICHT, StringComparison.Ordinal);
            if (IstDach(art, neigung))
                return TypaufbauSaattabelle.Zu(leicht ? TypaufbauSaattabelle.DA_SPARRENDACH : TypaufbauSaattabelle.DA_STAHLBETON_GEDAEMMT);
            if (IstBoden(art, neigung))
                return TypaufbauSaattabelle.Zu(rand == Bauteilrand.Erdreich ? TypaufbauSaattabelle.BO_DAEMMUNG_OBEN
                                                                            : TypaufbauSaattabelle.BO_DAEMMUNG_UNTEN);
            if (leicht) return TypaufbauSaattabelle.Zu(TypaufbauSaattabelle.AW_HOLZLEICHTBAU);
            TypaufbauSaat ungedaemmt = TypaufbauSaattabelle.Zu(TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT);
            bool gedaemmt = klasse is char k
                ? char.ToUpperInvariant(k) > 'F'
                : !(U(TypaufbauSaattabelle.AlsModell(ungedaemmt), neigung, rand) is double u0) || uZiel < u0;
            return gedaemmt ? TypaufbauSaattabelle.Zu(TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT) : ungedaemmt;
        }

        /// <summary>
        /// Der Ersatzaufbau: Vorgabetyp, abgeglichen; bei der massiven Außenwand der Wechsel zwischen gedämmt
        /// und ungedämmt, wenn der Vorgabetyp das Ziel-U nicht erreicht.
        /// </summary>
        internal static Ersatzergebnis Bilden(Bauteilart art, Bauteilrand rand, double neigung, string bauart, char? klasse, double uZiel)
        {
            TypaufbauSaat t = Vorgabetyp(art, rand, neigung, bauart, klasse, uZiel);
            Ersatzergebnis e = Abgleichen(t, rand, neigung, uZiel);
            string anderer = null;
            if (t.Code == TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT && e.Bedarf is double d && d < TypaufbauSaat.DAEMMDICKE_MIN_M)
                anderer = TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT;
            else if (t.Code == TypaufbauSaattabelle.AW_MASSIV_UNGEDAEMMT && e.Bedarf is double l
                     && l < TypaufbauSaat.LAMBDA_FAKTOR_MIN * t.Schichten[t.Abgleichschicht].Baustoff.Lambda)
                anderer = TypaufbauSaattabelle.AW_MASSIV_AUSSENGEDAEMMT;
            if (anderer == null) return e;
            Ersatzergebnis w = Abgleichen(TypaufbauSaattabelle.Zu(anderer), rand, neigung, uZiel);
            return w with { Vermerk = w.Vermerk | Ersatzvermerk.TypNachU };
        }

        /// <summary>Gleicht einen Typ auf <paramref name="uZiel"/> ab (ohne Typwechsel) — Regel im Klassenkopf.</summary>
        internal static Ersatzergebnis Abgleichen(TypaufbauSaat t, Bauteilrand rand, double neigung, double uZiel)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            BauteilaufbauModel m = TypaufbauSaattabelle.AlsModell(t);
            double? u0 = U(m, neigung, rand);
            if (!(u0 > 0.0) || !(uZiel > 0.0) || double.IsNaN(uZiel) || double.IsInfinity(uZiel))
                return new Ersatzergebnis(t, m, uZiel, u0, t.Abgleich, null, null, Ersatzvermerk.AusserhalbBand);

            BauteilschichtModel s = m.Schichten[t.Abgleichschicht];
            double rRest = 1.0 / u0.Value - s.Dicke / s.Lambda.Value;
            double rNoetig = 1.0 / uZiel - rRest;
            Ersatzvermerk vermerk = Ersatzvermerk.Keiner;
            double? wert = null, bedarf;

            if (t.Abgleich == Typabgleich.Daemmdicke)
            {
                double d = s.Lambda.Value * rNoetig;
                bedarf = d;
                if (d < TypaufbauSaat.DAEMMDICKE_MIN_M)
                {
                    m.Schichten.RemoveAt(t.Abgleichschicht);
                    for (int i = 0; i < m.Schichten.Count; i++) m.Schichten[i].Reihenfolge = i + 1;
                    vermerk |= Ersatzvermerk.DaemmungEntfaellt;
                    wert = 0.0;
                }
                else if (d > TypaufbauSaat.DAEMMDICKE_MAX_M) vermerk |= Ersatzvermerk.AusserhalbBand;
                else
                {
                    s.Dicke = d;
                    wert = d;
                }
            }
            else
            {
                double lambda = rNoetig > 0.0 ? s.Dicke / rNoetig : double.PositiveInfinity;
                bedarf = lambda;
                double faktor = lambda / s.Lambda.Value;
                if (faktor < TypaufbauSaat.LAMBDA_FAKTOR_MIN || faktor > TypaufbauSaat.LAMBDA_FAKTOR_MAX) vermerk |= Ersatzvermerk.AusserhalbBand;
                else
                {
                    s.Lambda = lambda;
                    wert = lambda;
                }
            }
            return new Ersatzergebnis(t, m, uZiel, U(m, neigung, rand), t.Abgleich, wert, bedarf, vermerk);
        }

        /// <summary>Das U eines Aufbaus aus seinen Schichten wie im Lauf; <c>null</c> bei einem Fehler.</summary>
        internal static double? U(BauteilaufbauModel a, double neigung, Bauteilrand rand)
        {
            try
            {
                var schichten = a.Schichten.Select((x, i) => GebaeudeZonenabbildung.AlsSchicht(x, i + 1, a.Bezeichner)).ToList();
                return Bauteilreduktion.UWertAusSchichten(schichten, neigung, rand, a.Bezeichner).U_WM2K;
            }
            catch (GebaeudeModellException)
            {
                return null;
            }
        }
    }
}

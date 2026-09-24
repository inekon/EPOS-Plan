using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Zapfkategorie</b> einer Nutzungsart (Umsetzungskonzept Zapfprofilgenerator 3.1, T2
    /// <c>Tab_TwwZapfkategorie_STAMM</c>; 4.4 nach der frei dokumentierten Jordan/Vajen-Parametrik):
    /// mittlerer Volumenstrom μ [l/min], Streuung σ [l/min], Dauer eines Ereignisses [min, ganz],
    /// Anteil an der Tagesmenge [-] und die Provenienz. Die Werte stehen im Katalog, nie im
    /// Quelltext; bis zum Schemaschritt T2 kommen sie als Eingabe herein (im Test erfunden).
    ///
    /// <para><b>Häufigkeit je Tag ist Ergebnis, nicht Eingabe:</b> Die Tagesmenge liegt durch
    /// Mengengerüst und Formvektor fest; mit dem Anteil folgt die Rate jeder Kategorie aus der
    /// λ-Kalibrierung (<see cref="Zapfereignisgenerator.Rate"/>). Eine eigene Häufigkeit machte die
    /// Kategorie überbestimmt.</para>
    /// </summary>
    internal sealed record Zapfkategorie(int IdNutzungsart, string Name, double VolumenstromLJeMin, double StreuungLJeMin,
                                         int DauerMin, double Anteil, Provenienz Herkunft)
    {
        /// <summary>
        /// Die obere Kappung des Volumenstroms [l/min]; <c>null</c> = nur die Kappung bei 0.
        /// Das gestutzte Mittel der λ-Kalibrierung rechnet mit beiden Kappungen.
        /// </summary>
        public double? KappungLJeMin { get; init; }
    }

    /// <summary>
    /// <b>Die Schlüssel der Stochastik im Parametersatz</b> (Konzept 2.1, 4.4). Die Schlüssel
    /// stehen im Code, die Werte in <c>Tab_TwwParameter_STAMM</c> (INEKON-Setzungen, im Testkatalog
    /// erfunden). Fehlt ein Parameter, den eine Rechnung braucht, lehnt sie benannt ab; die
    /// Schwellen der Hinweise entscheiden die Rechnung nicht (N7 (d)).
    /// </summary>
    internal static class ZapfStochastikParameter
    {
        /// <summary>
        /// Größter Versatz der Ferienfenster je Einheit [d] (Entkopplung der Urlaube, 4.4): Jede
        /// Einheit einer Zone mit Kalenderart Wohnen verschiebt die Fenster der Zone um eine
        /// gleichverteilte ganze Zahl in [−v; v]. Nur gelesen, wenn eine solche Zone Ferien trägt.
        /// </summary>
        internal const string URLAUBSVERSATZ = "Zapfprofil.Stochastik.Urlaubsversatz";

        /// <summary>
        /// Vielfaches der Mindestzahl <c>1/(1 − p)</c> für die Vorgabe der Realisierungen des
        /// Bedarfstags (4.4, INEKON-Setzung); gilt, wenn das Projekt keine Zahl nennt.
        /// </summary>
        internal const string AUSLEGUNG_VIELFACHES = "Zapfprofil.Stochastik.Auslegung.Vielfaches";

        /// <summary>
        /// Schwelle des Konsistenzhinweises [-] (4.7, N11 (f)): Hinweis, wenn das Perzentil der
        /// größten Stundenleistung des gezogenen Bedarfstags über Schwelle · Φ_N des
        /// Summenlinienpunkts liegt. Entscheidet die Rechnung nicht.
        /// </summary>
        internal const string KONSISTENZSCHWELLE = "Zapfprofil.Stochastik.Konsistenzschwelle";

        /// <summary>
        /// Präfix des Quantils z der Einzelstatistik je Perzentil (<c>…Quantil.P95</c>,
        /// <c>…Quantil.P99</c>) für den Vergleich <c>μ + z · σ / √N</c> (4.5 b, Konzept S4c).
        /// Entscheidet die Rechnung nicht.
        /// </summary>
        internal const string QUANTIL = "Zapfprofil.Stochastik.Quantil.P";
    }

    /// <summary>
    /// Eine geprüfte Kategorie für die Ziehung: der auf Σ 1 normierte Anteil, das gestutzte Mittel
    /// des Volumenstroms [l/min] (<see cref="Zapfverteilung.GestutztesMittel"/>) und die mittlere
    /// Energie eines Ereignisses je Kelvin Spreizung [kWh/K] = Mittel · Dauer · c_w / 1000.
    /// </summary>
    internal sealed record Zapfkategoriewert(Zapfkategorie Kategorie, double AnteilNormiert, double MittelLJeMin,
                                             double EnergieJeKelvinKwh);

    /// <summary>
    /// <b>Die Kategorien einer Nutzungsart, geprüft und kalibriert</b> (4.4). Ohne Kategorie, mit
    /// einem ungültigen Wert oder ohne Anteil wird benannt abgelehnt
    /// (<see cref="ZapfEingabefehler.StochastikUngueltig"/>) — die Zone rechnet dann nicht
    /// stochastisch, nie still deterministisch. Reihenfolge wie im Katalog. Eine Kategorie ohne
    /// Anteil ist erlaubt — auch mit gestutztem Mittel 0 — und zieht nie
    /// (<see cref="Zapfereignisgenerator.Rate"/> = 0).
    /// </summary>
    internal sealed class Zapfkategoriensatz
    {
        /// <summary>
        /// Kennung der Ablehnung „keine Kategorien für die Nutzungsart" (Grund
        /// <see cref="ZapfEingabefehler.StochastikUngueltig"/>); die Werte sind Bezeichner und
        /// Katalogversion der Nutzungsart, getrennt (Platzhalter {0} und {1} des Textes).
        /// </summary>
        internal const string KENNUNG_KATEGORIEN_FEHLEN = "STOCHASTIK_KATEGORIEN_FEHLEN";

        private readonly Zapfkategoriewert[] _werte;

        private Zapfkategoriensatz(int idNutzungsart, string zone, Zapfkategoriewert[] werte)
        {
            IdNutzungsart = idNutzungsart;
            Zone = zone ?? "";
            _werte = werte;
        }

        /// <summary>Die Nutzungsart der Kategorien.</summary>
        internal int IdNutzungsart { get; }

        /// <summary>Die Zone, für die der Satz geprüft ist (für benannte Ablehnungen der Ziehung).</summary>
        internal string Zone { get; }

        /// <summary>Die Kategorien in der Reihenfolge des Katalogs — nur lesbar.</summary>
        internal IReadOnlyList<Zapfkategoriewert> Werte => Array.AsReadOnly(_werte);

        /// <summary>
        /// Die Kategorien der Nutzungsart <paramref name="idNutzungsart"/> aus dem Katalog,
        /// geprüft: μ und σ endlich und nicht negativ, Dauer 1 … 1440 Minuten, Anteil endlich und
        /// nicht negativ mit Σ &gt; 0, Kappung positiv, gestutztes Mittel positiv.
        /// </summary>
        internal static Zapfkategoriensatz Aus(IReadOnlyList<Zapfkategorie> katalog, int idNutzungsart, string zone)
            => Aus(katalog, idNutzungsart, zone, null, null);

        /// <summary>
        /// Wie <see cref="Aus(IReadOnlyList{Zapfkategorie}, int, string)"/> für die Nutzungsart
        /// <paramref name="art"/> — die Ablehnung ohne Kategorien nennt sie mit Bezeichner und
        /// Katalogversion statt mit ihrer Id und trägt beide getrennt als Werte der Kennung.
        /// </summary>
        internal static Zapfkategoriensatz Aus(IReadOnlyList<Zapfkategorie> katalog, Nutzungsart art, string zone)
        {
            if (art == null) throw new ArgumentNullException(nameof(art));
            return Aus(katalog, art.Id, zone, art.Name ?? "", art.Katalogversion ?? "");
        }

        private static Zapfkategoriensatz Aus(IReadOnlyList<Zapfkategorie> katalog, int idNutzungsart, string zone,
                                              string name, string version)
        {
            var eigene = new List<Zapfkategorie>();
            if (katalog != null)
                foreach (Zapfkategorie k in katalog)
                    if (k != null && k.IdNutzungsart == idNutzungsart) eigene.Add(k);
            if (eigene.Count == 0)
            {
                // Der Klartext des Kerns (Protokoll) ist deutsch; die Oberfläche baut den Satz aus
                // Kennung und den getrennten Werten in ihrer Sprache.
                string art = name == null
                    ? idNutzungsart.ToString(CultureInfo.InvariantCulture)
                    : "„" + name + "“" + (string.IsNullOrEmpty(version) ? "" : " (Katalogversion " + version + ")");
                throw new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, zone,
                    "Nicht rechenbar — für die Nutzungsart " + art + " der Zone „" + zone
                    + "“ stehen keine Zapfkategorien im Katalog.")
                {
                    Kennung = name == null ? null : KENNUNG_KATEGORIEN_FEHLEN,
                    Argumente = name == null ? null : new[] { name, version ?? "" }
                };
            }

            double summe = 0.0;
            foreach (Zapfkategorie k in eigene)
            {
                string was = "Die Zapfkategorie „" + (k.Name ?? "") + "“ der Zone „" + zone + "“";
                if (!Endlich(k.VolumenstromLJeMin) || k.VolumenstromLJeMin < 0)
                    throw Fehler(zone, "Nicht rechenbar — " + was + " trägt keinen gültigen Volumenstrom.");
                if (!Endlich(k.StreuungLJeMin) || k.StreuungLJeMin < 0)
                    throw Fehler(zone, "Nicht rechenbar — " + was + " trägt keine gültige Streuung.");
                if (k.DauerMin < 1 || k.DauerMin > Bedarfstag.MINUTEN)
                    throw Fehler(zone, "Nicht rechenbar — " + was + " trägt keine Dauer von 1 bis 1440 Minuten.");
                if (!Endlich(k.Anteil) || k.Anteil < 0)
                    throw Fehler(zone, "Nicht rechenbar — " + was + " trägt keinen gültigen Anteil.");
                if (k.KappungLJeMin.HasValue && (!Endlich(k.KappungLJeMin.Value) || !(k.KappungLJeMin.Value > 0)))
                    throw Fehler(zone, "Nicht rechenbar — " + was + " trägt keine positive Kappung.");
                summe += k.Anteil;
            }
            if (!(summe > 0))
                throw Fehler(zone, "Nicht rechenbar — die Anteile der Zapfkategorien der Zone „" + zone + "“ summieren zu 0.");

            var werte = new Zapfkategoriewert[eigene.Count];
            for (int i = 0; i < werte.Length; i++)
            {
                Zapfkategorie k = eigene[i];
                double mittel = Zapfverteilung.GestutztesMittel(k.VolumenstromLJeMin, k.StreuungLJeMin, k.KappungLJeMin);
                if (!(mittel > 0) && k.Anteil > 0)
                    throw Fehler(zone, "Nicht rechenbar — die Zapfkategorie „" + (k.Name ?? "") + "“ der Zone „" + zone
                                       + "“ hat kein positives gestutztes Mittel des Volumenstroms.");
                double energie = mittel * k.DauerMin * Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K / Mengengeruest.WH_JE_KWH;
                werte[i] = new Zapfkategoriewert(k, k.Anteil / summe, mittel, energie);
            }
            return new Zapfkategoriensatz(idNutzungsart, zone, werte);
        }

        private static bool Endlich(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        private static ZapfprofilEingabeException Fehler(string zone, string text)
            => new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, zone, text);
    }

    /// <summary>
    /// <b>Das gestutzte Mittel des Volumenstroms</b> (4.4, λ-Kalibrierung): Die Ziehung kappt
    /// <c>V̇ = μ + σ · z</c> bei 0 (und bei der Kappung der Kategorie); damit trifft der
    /// Erwartungswert die Tagesmenge nur, wenn die Kalibrierung mit dem Mittel des GEKAPPTEN
    /// Volumenstroms rechnet.
    ///
    /// <para><b>Exakt für die Ziehung.</b> <c>z = Σ_{j=1..12} u_j − 6</c> ist nicht normal-, sondern
    /// Irwin-Hall-verteilt (auf [−6; 6) beschränkt). Statt der Normalformel
    /// <c>μ · F(μ/σ) + σ · f(μ/σ)</c> des Papiers — sie bräuchte erf und exp und träfe die Ziehung
    /// nur auf etwa 0,5 % — rechnet die Klasse das Mittel der tatsächlich gezogenen Verteilung als
    /// Stückpolynom (für −6 &lt; c ≤ 0, b = 6 + c):</para>
    /// <code>
    /// E[max(0, c + z)] = 1/13! · Σ_{k &lt; b} (−1)^k · C(12, k) · (b − k)^13
    /// E[max(0, c + z)] = c + E[max(0, −c + z)]        für 0 &lt; c &lt; 6 (Symmetrie von z)
    /// E[max(0, c + z)] = c für c ≥ 6,   0 für c ≤ −6
    /// E[min(max(0, μ + σz), M)] = σ · g(μ/σ) − σ · g((μ − M)/σ)
    /// </code>
    /// <para>Nur Grundrechenarten — die Kalibrierung ist bitgleich auf jeder Plattform, und der
    /// Erwartungswert des Generators trifft die Tagesmenge ohne Modellfehler.</para>
    /// </summary>
    internal static class Zapfverteilung
    {
        /// <summary>(n + 1)! für n = 12 Summanden — exakt als Produkt der ganzen Zahlen 1 … 13.</summary>
        private static readonly double Fakultaet = FakultaetBilden(ZapfZufall.NORMAL_SUMMANDEN + 1);

        /// <summary>
        /// <c>g(c) = E[max(0, c + z)]</c> für <c>z = Σ_{j=1..12} u_j − 6</c>, exakt als Stückpolynom
        /// (Formel oben); ein nicht endliches <paramref name="c"/> wird abgelehnt.
        /// </summary>
        internal static double PositivteilMittel(double c)
        {
            if (double.IsNaN(c)) throw new ArgumentOutOfRangeException(nameof(c));
            double rand = ZapfZufall.NORMAL_VERSATZ;
            if (!(c > -rand)) return 0.0;
            if (c >= rand) return c;
            if (c > 0) return c + PositivteilMittel(-c);

            int n = ZapfZufall.NORMAL_SUMMANDEN;
            double b = rand + c;
            double summe = 0.0, binom = 1.0;
            for (int k = 0; k < n; k++)
            {
                double x = b - k;
                if (!(x > 0)) break;
                double p = 1.0;
                for (int i = 0; i <= n; i++) p *= x;
                summe += ((k & 1) == 0 ? binom : -binom) * p;
                binom = binom * (n - k) / (k + 1);
            }
            return summe / Fakultaet;
        }

        /// <summary>
        /// Das Mittel des gekappten Volumenstroms <c>E[min(max(0, μ + σz), M)]</c> [l/min];
        /// ohne Kappung <c>E[max(0, μ + σz)]</c>, ohne Streuung <c>min(max(0, μ), M)</c>.
        /// </summary>
        internal static double GestutztesMittel(double mittel, double streuung, double? kappung)
        {
            if (!(streuung > 0))
            {
                double v = mittel > 0 ? mittel : 0.0;
                return kappung.HasValue && v > kappung.Value ? kappung.Value : v;
            }
            double e = streuung * PositivteilMittel(mittel / streuung);
            if (kappung.HasValue) e -= streuung * PositivteilMittel((mittel - kappung.Value) / streuung);
            return e;
        }

        private static double FakultaetBilden(int n)
        {
            double f = 1.0;
            for (int i = 2; i <= n; i++) f *= i;
            return f;
        }
    }

    /// <summary>
    /// <b>Die Zahl der unabhängigen Einheiten n_E einer Zone</b> (4.4: „je Einheit i = 1..n_E";
    /// Festlegung der Umsetzung, das Papier nennt keine Regel): Wohnungstabelle → Σ Anzahl;
    /// Bezugsart Wohneinheiten → Bezugsmenge; Personen → Bezugsmenge / Personen je WE (ohne
    /// Belegung jede Person); Fläche → Bezugsmenge / Wohnfläche je WE (Zone, sonst Parameter);
    /// sonst (Betten, Duschplätze, Sitzplätze, Beschäftigte) die Bezugsmenge. Kaufmännisch auf
    /// eine ganze Zahl gerundet, mindestens 1.
    /// </summary>
    internal static class Zapfeinheiten
    {
        /// <summary>Höchstzahl der Einheiten einer Zone (numerische Setzung gegen eine unsinnige Laufzeit).</summary>
        internal const int HOECHSTENS = 1000000;

        /// <summary>Die Einheiten der Zone zur wirksamen Bezugsmenge <paramref name="bezugsmenge"/>.</summary>
        internal static int Anzahl(ZonenStand z, Nutzungsart n, double bezugsmenge, Parametersatz ps)
        {
            if (z == null) throw new ArgumentNullException(nameof(z));
            if (n == null) throw new ArgumentNullException(nameof(n));
            string zone = z.Name ?? "";
            double x;
            if (z.Wohnungen != null && z.Wohnungen.Count > 0)
            {
                x = 0.0;
                foreach (WohnungstypStand w in z.Wohnungen) x += w.Anzahl;
            }
            else switch (n.Bezug)
            {
                case ZapfBezugsart.Personen:
                    x = z.PersonenJeWe.HasValue && z.PersonenJeWe.Value > 0 ? bezugsmenge / z.PersonenJeWe.Value : bezugsmenge;
                    break;
                case ZapfBezugsart.Flaeche:
                    double flaeche = z.WohnflaecheJeWeM2 ?? ps.Wert(ZapfParameter.WOHNEN_FLAECHE_JE_WE);
                    if (!(flaeche > 0))
                        throw new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, zone,
                            "Nicht rechenbar — die Wohnfläche je WE der Zone „" + zone + "“ ist nicht positiv; die Einheiten sind nicht bestimmbar.");
                    x = bezugsmenge / flaeche;
                    break;
                default:
                    x = bezugsmenge;
                    break;
            }
            if (double.IsNaN(x) || double.IsInfinity(x) || x < 0 || x > HOECHSTENS)
                throw new ZapfprofilEingabeException(ZapfEingabefehler.StochastikUngueltig, zone,
                    "Nicht rechenbar — die Zahl der Einheiten der Zone „" + zone + "“ ist nicht bestimmbar oder größer als "
                    + HOECHSTENS.ToString(CultureInfo.InvariantCulture) + ".");
            int anzahl = (int)Math.Round(x, MidpointRounding.AwayFromZero);
            return anzahl < 1 ? 1 : anzahl;
        }
    }
}

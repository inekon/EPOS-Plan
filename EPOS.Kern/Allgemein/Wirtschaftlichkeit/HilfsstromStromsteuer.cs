using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// 6b (Anwenderentscheid 16.10.2026) — <b>die Stromsteuer des Hilfsstroms</b> von BHKW und
    /// Brennstoffkesseln, gesondert ausgewiesen und, wo nötig, gesondert angesetzt.
    ///
    /// <para><b>Die Menge.</b> Der Hilfsstrom aus dem Hilfsenergieanteil der Anlagen
    /// (<see cref="VariantenDaten.HilfsstromMWh"/>, <see cref="HilfsstromRechner.SummeMWh"/>) — dieselbe
    /// Menge, die die CO₂-Bilanz mit dem Netzstromfaktor ansetzt.</para>
    ///
    /// <para><b>Nie doppelt.</b> Die Hilfsenergiekosten bewerten den Hilfsstrom mit dem
    /// Arbeitspreis des Netzstromträgers (E30‑Q4 a). Enthält dieser Preis die Stromsteuer,
    /// steckt sie schon in den Kosten; angesetzt wird deshalb nur, was der Arbeitspreis NICHT
    /// enthält: <c>Satz gesondert = Regelsatz − Stromsteueranteil des Arbeitspreises</c>, nie
    /// unter 0. Der Stromsteueranteil ist derselbe wie beim Deckel des § 9b-Abzugs
    /// (Strompreis Details des Netzträgers): gepflegt → dieser Wert; Komponente abgeschaltet →
    /// 0 (der Preis enthält keine Stromsteuer, sie wird voll angesetzt); nicht gepflegt → der
    /// Regelsatz (ein Bezugspreis enthält die Stromsteuer, angesetzt wird nichts zusätzlich).
    /// Ausgewiesen wird stets die ganze Steuer samt dem enthaltenen Teil.</para>
    ///
    /// <para>Der Satz ist der Regelsatz des Gesetzkatalogs im ersten Betrachtungsjahr. Gebucht
    /// wird der gesonderte Betrag im Endenergie-Topf (p_E) — dort, wo die Hilfsenergiekosten
    /// selbst stehen.</para>
    /// </summary>
    internal static class HilfsstromStromsteuer
    {
        /// <summary>Herkunft der Nachweiszeile (<see cref="KostenPositionNachweis.SatzHerkunft"/>).</summary>
        internal const string HERKUNFT = "STROMSTEUER_HILFSSTROM";

        internal sealed class Ergebnis
        {
            /// <summary>Hilfsstrom [MWh/a].</summary>
            public double MengeMWh;

            /// <summary>Regelsatz der Stromsteuer [€/MWh]; <c>null</c> = im Katalog nicht gepflegt.</summary>
            public double? RegelsatzEurJeMWh;

            /// <summary>Im Arbeitspreis enthaltener Satz [€/MWh] (0 … Regelsatz).</summary>
            public double EnthaltenEurJeMWh;

            /// <summary>Gesondert angesetzter Satz [€/MWh] = Regelsatz − enthalten.</summary>
            public double GesondertEurJeMWh;

            /// <summary>Stromsteuer des Hilfsstroms insgesamt [€/a] (Ausweis).</summary>
            public double SteuerEur => MengeMWh * (RegelsatzEurJeMWh ?? 0.0);

            /// <summary>Davon im Arbeitspreis enthalten [€/a] (steht in den Hilfsenergiekosten).</summary>
            public double EnthaltenEur => MengeMWh * EnthaltenEurJeMWh;

            /// <summary>Gesondert angesetzt [€/a] (Endenergie-Topf).</summary>
            public double GesondertEur => MengeMWh * GesondertEurJeMWh;
        }

        /// <summary>
        /// Die Rechnung; <c>null</c> ohne Hilfsstrom. Fehlt der Regelsatz, bleibt der Satz 0 und
        /// <see cref="Ergebnis.RegelsatzEurJeMWh"/> leer — der Aufrufer nennt es.
        /// </summary>
        /// <param name="mengeMWh">Hilfsstrom [MWh/a].</param>
        /// <param name="regelsatzEurJeMWh">Regelsatz der Stromsteuer [€/MWh].</param>
        /// <param name="anteilImPreisEurJeMWh">Stromsteueranteil des Arbeitspreises [€/MWh];
        /// <c>null</c> = nicht gepflegt (gilt als im Preis enthalten).</param>
        internal static Ergebnis Rechne(double mengeMWh, double? regelsatzEurJeMWh,
                                        double? anteilImPreisEurJeMWh)
        {
            if (!(mengeMWh > 0)) return null;
            var e = new Ergebnis { MengeMWh = mengeMWh, RegelsatzEurJeMWh = regelsatzEurJeMWh };
            if (!regelsatzEurJeMWh.HasValue || !(regelsatzEurJeMWh.Value > 0)) return e;

            double regel = regelsatzEurJeMWh.Value;
            double enthalten = anteilImPreisEurJeMWh.HasValue
                ? Math.Min(regel, Math.Max(0.0, anteilImPreisEurJeMWh.Value))
                : regel;
            e.EnthaltenEurJeMWh = enthalten;
            e.GesondertEurJeMWh = regel - enthalten;
            return e;
        }

        /// <summary>
        /// Die Nachweiszeile unter den Betriebskosten — Menge [kWh] × gesonderter Satz [€/kWh];
        /// die Bezeichnung nennt Regelsatz und enthaltenen Teil. <c>null</c> ohne Ergebnis oder
        /// ohne Regelsatz.
        /// </summary>
        internal static KostenPositionNachweis Nachweis(Ergebnis e)
        {
            if (e == null || !e.RegelsatzEurJeMWh.HasValue) return null;
            CultureInfo k = BerichtTexte.Kultur;
            var n = new KostenPositionNachweis
            {
                Id = 0,
                Komponente = 0,
                Anlage = 0,
                Bezeichnung = string.Format(k,
                    Text("HILFS_STROMSTEUER_POSITION",
                         "Stromsteuer auf Hilfsstrom ({0} €/MWh, davon {1} €/MWh im Arbeitspreis enthalten)"),
                    e.RegelsatzEurJeMWh.Value.ToString("N2", k), e.EnthaltenEurJeMWh.ToString("N2", k)),
                Gruppe = DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI,
                Kostenart = DbWerte.KOSTENART_BEDARFSGEBUNDEN,
                Bemessung = DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH,
                Menge = e.MengeMWh * 1000.0,
                Einheitpreis = e.GesondertEurJeMWh / 1000.0,
                SatzHerkunft = HERKUNFT
            };
            n.BetragJahr = e.GesondertEur;
            return n;
        }

        /// <summary>Hinweis, wenn Hilfsstrom anfällt, der Katalog aber keinen Regelsatz führt.</summary>
        internal static string HinweisOhneSatz(Ergebnis e)
        {
            if (e == null || e.RegelsatzEurJeMWh.HasValue) return null;
            return string.Format(BerichtTexte.Kultur,
                Text("HILFS_STROMSTEUER_OHNE_SATZ",
                     "Stromsteuer auf {0} MWh/a Hilfsstrom nicht angesetzt: Der Gesetzkatalog führt keinen Regelsatz der Stromsteuer."),
                e.MengeMWh.ToString("N2", BerichtTexte.Kultur));
        }

        private static string Text(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel, BerichtTexte.Kultur);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch (System.Resources.MissingManifestResourceException) { return rueckfall; }
        }
    }
}

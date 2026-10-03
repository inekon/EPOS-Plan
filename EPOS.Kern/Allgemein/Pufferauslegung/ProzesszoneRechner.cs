using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Prozesszone (Konzept 3.2): D1 und D2 auf der Prozessreihe Q_P(t) mit dem Temperaturpaar des
    /// Puffers und dem bemessenden Erzeuger. Die Kriterien laufen unabhängig von den Vorlagen-Schaltern.
    /// Tragen die Prozesse ein Temperaturpaar (<see cref="PufferAuslegungEingang.ProzessVorlaufC"/>), rechnet
    /// die Zone mit dessen Spreizung; liegt der Prozessvorlauf über 95 °C oder über dem Vorlauf aller
    /// Erzeuger, meldet sie <see cref="PufferWarncode.PROZESS_TEMPERATUR"/>.
    /// </summary>
    public static class ProzesszoneRechner
    {
        /// <summary>Obergrenze des Prozessvorlaufs für einen drucklosen Wasserpuffer [°C].</summary>
        public const double PROZESS_VORLAUF_GRENZE_C = 95.0;

        /// <summary>
        /// Ist der Prozessvorlauf aus dem Puffer nicht sicher erreichbar (über 95 °C oder über dem höchsten
        /// Erzeugervorlauf)? <c>false</c> ohne gepflegten Prozessvorlauf.
        /// </summary>
        public static bool ProzessvorlaufKritisch(double? prozessVorlaufC, double? erzeugerVorlaufMaxC)
        {
            if (!prozessVorlaufC.HasValue) return false;
            if (prozessVorlaufC.Value > PROZESS_VORLAUF_GRENZE_C) return true;
            return erzeugerVorlaufMaxC > 0 && prozessVorlaufC.Value > erzeugerVorlaufMaxC.Value;
        }

        internal static PufferZonenergebnis Rechnen(PufferRechengroessen g)
        {
            const PufferZone ZONE = PufferZone.Prozess;
            PufferErzeuger z = g.E.Erzeuger ?? new PufferErzeuger();
            double? pVl = g.E.ProzessVorlaufC, pRl = g.E.ProzessRuecklaufC;
            if (ProzessvorlaufKritisch(pVl, g.E.ErzeugerVorlaufMaxC))
                g.Warnung(PufferWarncode.PROZESS_TEMPERATUR, PufferStufe.Warnung,
                          g.E.ErzeugerVorlaufMaxC > 0
                              ? "Der Prozessvorlauf " + PufferRechengroessen.Zahl(pVl.Value) + " °C liegt über 95 °C oder über dem höchsten Erzeugervorlauf " +
                                PufferRechengroessen.Zahl(g.E.ErzeugerVorlaufMaxC.Value) + " °C: Der Puffer kann ihn nicht sicher liefern."
                              : "Der Prozessvorlauf " + PufferRechengroessen.Zahl(pVl.Value) + " °C liegt über 95 °C: Der Puffer kann ihn nicht sicher liefern.",
                          Textbaustein.T("PAUS_HERK_PROZESS_TEMPERATUR", "Temperaturpaar der Prozesswärme"), ZONE);
            if (pVl.HasValue && pRl.HasValue && pVl.Value > pRl.Value)
                g = g.MitSpreizung(pVl.Value - pRl.Value);
            if (!(g.DeltaT > 0))
                throw new System.ArgumentException("Die Spreizung des Puffers ϑ_VL − ϑ_RL muss positiv sein.");
            var k = new List<PufferKriterium>();
            if (!HeizzoneRechner.HatReihe(g.E.ReiheProzess))
            {
                g.Warnung(PufferWarncode.KEINE_REIHE, PufferStufe.Warnung,
                          "Die Prozessreihe ist leer: Die Prozesszone bleibt leer.", HeizzoneRechner.HERKUNFT_D1, ZONE);
                return HeizzoneRechner.Bemessen(ZONE, k, false);
            }
            k.AddRange(HeizzoneRechner.Simulationskriterien(g, g.E.ReiheProzess, z, true, true, ZONE));
            PufferZonenergebnis zone = HeizzoneRechner.Bemessen(ZONE, k, false);
            if (zone.VolumenL > 0)
                zone = zone with { Betriebsbild = HeizzoneRechner.Betriebsbild(g, g.E.ReiheProzess, z, zone.VolumenL, ZONE) };
            return zone;
        }
    }
}

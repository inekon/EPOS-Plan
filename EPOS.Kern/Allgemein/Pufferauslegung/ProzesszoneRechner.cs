using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Prozesszone (Konzept 3.2): D1 und D2 auf der Prozessreihe Q_P(t) mit dem Temperaturpaar des
    /// Puffers und dem bemessenden Erzeuger. Die Kriterien laufen unabhängig von den Vorlagen-Schaltern.
    /// </summary>
    public static class ProzesszoneRechner
    {
        internal static PufferZonenergebnis Rechnen(PufferRechengroessen g)
        {
            const PufferZone ZONE = PufferZone.Prozess;
            PufferErzeuger z = g.E.Erzeuger ?? new PufferErzeuger();
            if (!(g.DeltaT > 0))
                throw new System.ArgumentException("Die Spreizung des Puffers ϑ_VL − ϑ_RL muss positiv sein.");
            var k = new List<PufferKriterium>();
            if (!HeizzoneRechner.HatReihe(g.E.ReiheProzess))
            {
                g.Warnung(PufferWarncode.KEINE_REIHE, PufferStufe.Warnung,
                          Textbaustein.T("PA_KEINE_REIHE_PROZESS_TEXT", "Die Prozessreihe ist leer: Die Prozesszone bleibt leer."), HeizzoneRechner.HERKUNFT_D1, ZONE);
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

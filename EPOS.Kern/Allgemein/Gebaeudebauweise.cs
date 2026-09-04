namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Rundweg BAUART ↔ BAUWEISE des Gebäudekatalogs (iU9‑W9.0b, öffentlich seit
    /// dem Entscheid W9‑O‑2 vom 04.09.2026).
    ///
    /// <para><b>Wozu öffentlich.</b> Die Rechnung stand als statisches Paar in
    /// <c>GebaeudeStammCtrl</c>, und der Controller ist <c>internal</c>. Seit die
    /// <b>Bauart</b>-Klappliste die Bauweise BESTIMMT (und sie nicht mehr nur anzeigt),
    /// braucht der Katalogeditor in <c>EPOS.UI</c> die Rechnung selbst — dieselbe Lage
    /// wie bei <see cref="Ferienzeit"/> und <see cref="Suchmuster"/>, und dieselbe
    /// Antwort: eine reine, plattformfreie Hilfsklasse ohne Datenbank.
    /// <c>GebaeudeStammCtrl.BauartAusBauweise</c> und
    /// <c>GebaeudeStammCtrl.BauweiseAusBauart</c> reichen ihre Aufrufe hierher
    /// durch — es gibt nur EINE Rechnung.</para>
    ///
    /// <para><b>Die Größe.</b> <c>Bauweise = Wohnfläche × 20 / 50 / 100</c>, je nach
    /// Bauart; zurück wird sie über den spezifischen Wert
    /// <c>Bauweise / Wohnfläche</c> gelesen (&lt; 30 leicht, &gt; 75 sehr schwer,
    /// sonst schwer).</para>
    /// </summary>
    public static class Gebaeudebauweise
    {
        /// <summary>Bauart „leichte Bauart" — Wohnfläche × 20.</summary>
        public const int LEICHT = 0;

        /// <summary>Bauart „schwere Bauart" — Wohnfläche × 50.</summary>
        public const int SCHWER = 1;

        /// <summary>Bauart „sehr schwere Bauart" — Wohnfläche × 100.</summary>
        public const int SEHR_SCHWER = 2;

        /// <summary>
        /// Die BAUART aus der gespeicherten Bauweise — <c>Form_Gebaeude1.SetControls</c>
        /// :107-110.
        ///
        /// <para>Der Vorlaeufer teilte ohne Nullpruefung; eine Wohnflaeche 0 ergab dort
        /// <c>NaN</c> bzw. <c>Infinity</c>. <c>NaN</c> ist weder kleiner 30 noch groesser
        /// 75, die Anzeige stand also auf „schwer" — genau darauf faellt eine Wohnflaeche
        /// von 0 hier ausdruecklich zurueck.</para>
        /// </summary>
        public static int BauartAusBauweise(double bauweise, double wohnflaeche)
        {
            if (wohnflaeche == 0) return SCHWER;
            double spez = bauweise / wohnflaeche;
            if (spez < 30) return LEICHT;
            if (spez > 75) return SEHR_SCHWER;
            return SCHWER;
        }

        /// <summary>
        /// Der Rueckweg — <c>InitModelFromControls</c>:188-191. Die Indizes 0/1/2 ergeben
        /// Wohnflaeche × 20 / 50 / 100, jeder andere Index ergibt 50.
        ///
        /// <para>Der Rueckfall 50 stammt aus dem Vorlaeufer, der hier den Index der
        /// GEBAEUDEART-Klappliste hereinreichte (Befund W9-B6) und damit regelmaessig
        /// ausserhalb von 0..2 landete. Seit dem Entscheid W9-O-2 kommt der Index aus der
        /// Bauartliste, die nur 0, 1 und 2 kennt; der Rueckfall bleibt trotzdem stehen —
        /// eine leere Auswahl (−1) darf keine Ausnahme werfen.</para>
        /// </summary>
        public static double BauweiseAusBauart(int index, double wohnflaeche)
        {
            if (index == LEICHT) return wohnflaeche * 20;
            if (index == SCHWER) return wohnflaeche * 50;
            if (index == SEHR_SCHWER) return wohnflaeche * 100;
            return 50;
        }
    }
}

using EPOS.UI.Dialoge.Waermepumpe;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Gruppe „Betriebszeiten" zwischen Anlagenzeile und Feldsatz</b> (Anlagenkopplung 9.3, AK2-3):
    /// <c>Tab_Energieanlagen.Zeitprogramm</c> und <c>Vorlauf_Max</c> ↔ <see cref="WaermepumpeAnlageDaten"/>.
    /// Beide Werte reisen <b>NULL-erhaltend</b>: leer bleibt leer (immer verfügbar bzw. Vorgabe Vorlauf), und ein
    /// ungültiger Text bleibt stehen, wie er gelesen wurde — er wird im Dialog benannt, nie still verworfen.
    /// Plattformfrei; die Windows-Hülle der Wärmepumpe und der Schreibweg der Simulationskonfiguration rufen sie.
    /// </summary>
    public static class BetriebszeitenAbbildung
    {
        /// <summary>Aus der Anlagenzeile in den Feldsatz.</summary>
        public static void Lesen(WErzeugerModel m, WaermepumpeAnlageDaten d)
        {
            if (m == null || d == null) return;
            d.Zeitprogramm = string.IsNullOrWhiteSpace(m.Zeitprogramm) ? null : m.Zeitprogramm;
            d.VorlaufMax = m.Vorlauf_Max;
        }

        /// <summary>Zurück in die Anlagenzeile — leer als NULL.</summary>
        public static void Schreiben(WaermepumpeAnlageDaten d, WErzeugerModel m)
        {
            if (m == null || d == null) return;
            m.Zeitprogramm = string.IsNullOrWhiteSpace(d.Zeitprogramm) ? null : d.Zeitprogramm;
            m.Vorlauf_Max = d.VorlaufMax;
        }

        /// <summary>Die Felder des schmalen Schreibwegs (<see cref="WErzeugerCtrl.KonfigurationSchreiben"/>).</summary>
        internal static WErzeugerCtrl.KonfigurationFelder MitBetriebszeiten(WErzeugerCtrl.KonfigurationFelder felder,
                                                                         WaermepumpeAnlageDaten d)
            => d == null
                ? felder
                : felder with
                {
                    Betriebszeiten = true,
                    Zeitprogramm = string.IsNullOrWhiteSpace(d.Zeitprogramm) ? null : d.Zeitprogramm,
                    VorlaufMax = d.VorlaufMax,
                };
    }
}

using System.Collections.Generic;
using EPOS.UI.Dialoge.Waermepumpe;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kühlgaben der Wärmepumpen-Konfiguration</b> (Stufe KU2 Welle 3; Kühlkonzept 8.2;
    /// E15, E33, E34) — gebaut aus Kern-Controllern, plattformfrei. Beide Wirte des Bausteins
    /// <c>WaermepumpeKonfiguration</c> nehmen sie: der Anlagendialog der Verwaltung (Hülle der
    /// Windows-Schale) und Simulation › Konfiguration (<c>SimulationErgebnisHuelle</c>).
    ///
    /// <list type="bullet">
    /// <item><b>Stützstellen</b> je Gerät: Projektkopie vor Katalog — eine frisch gewählte
    /// Wärmepumpe trägt noch die Katalog-Id, ihre Kennlinie reist mit dem Speichern ins Projekt.
    /// Ein Vorlauf in Heizlage oder mit vertauschten Achsen (K22) steht gesperrt samt Grund.</item>
    /// <item><b>Sperrgrund</b> je Gerät: derselbe wie im Schreibweg des Kerns
    /// (<c>WPCtrl.KuehlbetriebSperrgrund</c>) — für die Projektkopie; für einen Katalogsatz nur die
    /// fehlende Kühlkennlinie (einen Quellspeicher gibt es vor dem Speichern nicht).</item>
    /// <item>die <b>Stromträger des Projekts</b> und der Träger, der den Netzbezug bepreist
    /// (<c>Kaeltestromabrechnung</c>).</item>
    /// </list>
    /// </summary>
    internal static class WaermepumpeKuehlGabenBau
    {
        /// <summary>Die Kühlgaben für ein Projekt; <c>null</c> ohne Projekt.</summary>
        internal static WaermepumpeKuehlGaben Bauen(int idProjekt)
        {
            if (idProjekt <= 0) return null;

            var traeger = new List<(int Id, string Text)>();
            foreach (KeyValuePair<int, string> t in Kaeltestromabrechnung.StromtraegerDesProjekts(idProjekt))
                traeger.Add((t.Key, t.Value));

            return new WaermepumpeKuehlGaben
            {
                Vorlaeufe = idWp => Vorlaeufe(idWp, idProjekt),
                Sperrgrund = idWp => Sperrgrund(idWp, idProjekt),
                Stromtraeger = traeger,
                ProjektStromtraeger = Kaeltestromabrechnung.Projekttraeger(idProjekt)
            };
        }

        /// <summary>Die Vorlauf-Stützstellen der Kühlkennlinie — Projektkopie vor Katalog, mit dem Befund je Vorlauf.</summary>
        internal static IReadOnlyList<KuehlVorlaufEintrag> Vorlaeufe(int idWp, int idProjekt)
        {
            var liste = new List<KuehlVorlaufEintrag>();
            if (idWp <= 0) return liste;

            List<KuehlkennlinienZeile> zeilen = WPCtrl.ProjektgeraetVorhanden(idWp, idProjekt)
                ? KenndatenKuehlungCtrl.ZeilenProjekt(idWp)
                : KenndatenKuehlungCtrl.ZeilenStamm(idWp);
            if (zeilen == null || zeilen.Count == 0) return liste;

            foreach (KeyValuePair<int, KuehlblockBefund> b in Kuehlkennlinie.Befunde(zeilen))
            {
                string grund = b.Value == KuehlblockBefund.Heizlage
                    ? Text("WPK_VORLAUF_HEIZLAGE", "Kennlinie in Heizlage — wird nicht gerechnet")
                    : b.Value == KuehlblockBefund.AchsenVertauscht
                        ? Text("WPK_VORLAUF_ACHSEN", "Achsen der Kennlinie vertauscht — wird nicht gerechnet")
                        : "";
                liste.Add(new KuehlVorlaufEintrag(b.Key, grund));
            }
            return liste;
        }

        /// <summary>Der Sperrgrund des Kühlbetriebs; <c>null</c> = frei.</summary>
        internal static string Sperrgrund(int idWp, int idProjekt)
        {
            if (idWp <= 0) return null;
            if (WPCtrl.ProjektgeraetVorhanden(idWp, idProjekt))
                return WPCtrl.KuehlbetriebSperrgrund(idWp, idProjekt);
            return KenndatenKuehlungCtrl.HatKenndatenStamm(idWp)
                ? null
                : Text("WP_PROJ_MSG_KUEHL_OHNE_KENNLINIE",
                       "Zu diesem Gerät liegen keine Kühlkenndaten vor — der Kühlbetrieb bleibt gesperrt.");
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}

using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Rückkühlwerk einer Kältemaschinen-Anlage für den Bericht</b> (K-F5b, Entwurf Split/VRF/Rückkühlwerk 5.8):
    /// Gerät und Name der Anlage (zum Abgleich mit der Ergebniszeile), Name und Bauart der Projektkopie. Gesammelt einmal
    /// vom <see cref="BerichtsDatenSammler"/>; die Berichtsschreiber lesen nur die Liste in <see cref="VariantenDaten.Rueckkuehlwerke"/>.
    /// </summary>
    public sealed class RueckkuehlwerkBerichtszeile
    {
        /// <summary>Projektkopie der Kältemaschine (<c>ID_Kaeltemaschine</c>) der Anlage.</summary>
        public int? IdKaeltemaschine { get; init; }

        /// <summary>Name der Anlage — derselbe wie <see cref="ErgebnisKaeltemaschineModel.Bezeichner"/>.</summary>
        public string Anlage { get; init; } = "";

        /// <summary>Name des Rückkühlwerks.</summary>
        public string Bezeichner { get; init; } = "";

        /// <summary>Bauart (<see cref="RueckkuehlwerkSchema.BAUARTEN"/>); <c>null</c> = trocken.</summary>
        public string Bauart { get; init; }

        /// <summary>
        /// Je Kältemaschinen-Anlage des Projekts mit Rückkühlwerk eine Zeile; leer ohne Schemaschritt oder ohne Rückkühlwerk.
        /// </summary>
        public static List<RueckkuehlwerkBerichtszeile> Sammeln(int projektId)
        {
            var liste = new List<RueckkuehlwerkBerichtszeile>();
            if (projektId <= 0 || !RueckkuehlwerkSchema.AnlagenspaltenVorhanden()) return liste;
            foreach (KaeltemaschineAnlageModel a in KaeltemaschineAnlageCtrl.Liste(projektId))
            {
                if (a?.IdRueckkuehlwerk is not int id) continue;
                RueckkuehlwerkModel m = RueckkuehlwerkCtrl.LadenStill(id);
                if (m == null) continue;
                liste.Add(new RueckkuehlwerkBerichtszeile
                {
                    IdKaeltemaschine = a.IdKaeltemaschine, Anlage = a.Bezeichner ?? "", Bezeichner = m.Bezeichner ?? "", Bauart = m.Bauart
                });
            }
            return liste;
        }

        /// <summary>
        /// Der deutsche Berichtstext einer Bauart (die Übersetzung über <see cref="BerichtTexte"/>); leer = trocken, ein
        /// unbekannter Wert bleibt, wie er ist.
        /// </summary>
        public static string BauartText(string bauart)
        {
            switch (bauart ?? RueckkuehlwerkSchema.BAUART_TROCKEN)
            {
                case RueckkuehlwerkSchema.BAUART_TROCKEN: return "Trockenkühler";
                case RueckkuehlwerkSchema.BAUART_ADIABAT: return "Adiabatischer Kühler";
                case RueckkuehlwerkSchema.BAUART_HYBRID: return "Hybridkühler";
                case RueckkuehlwerkSchema.BAUART_KUEHLTURM_OFFEN: return "Kühlturm, offen";
                case RueckkuehlwerkSchema.BAUART_KUEHLTURM_GESCHLOSSEN: return "Kühlturm, geschlossen";
                default: return bauart;
            }
        }
    }
}

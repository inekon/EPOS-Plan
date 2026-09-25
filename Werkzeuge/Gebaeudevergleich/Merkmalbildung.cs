using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b>Die Merkmale einer Gebäudezeile</b>, gelesen aus derselben Modellinstanz, die der Lauf
    /// rechnet (<c>GebaeudeBedarfCtrl.Projektgebaeude</c>, samt Zonen). Nichts davon wird
    /// geschrieben; der Spaltenwert <c>Gebaeude_Modell</c> ist nur ein Merkmal — gerechnet
    /// werden immer beide Wege.
    /// </summary>
    internal static class Merkmalbildung
    {
        /// <summary>
        /// Felder des Modells, die NICHT zur Bauform gehören: IDs, Namen, Beschreibung, die
        /// Werte der Zuordnung (Bezugswert, Einheit, Nutzungsgrad, dezentrales Warmwasser — sie
        /// hängen am Projekt, nicht am Gebäude), die Bewohnerzahl, die der Lauf aus der Fläche
        /// bildet, und der Rechenweg <c>Gebaeude_Modell</c> — eine Wahl des Rechenwegs, keine
        /// Bauform; er steht als Merkmal und Regel U‑TB für sich.
        /// </summary>
        internal static readonly HashSet<string> OHNE_BAUFORM = new HashSet<string>(StringComparer.Ordinal)
        {
            nameof(ProjektGebaeudeModel.items),
            nameof(ProjektGebaeudeModel.ID_Projekt),
            nameof(ProjektGebaeudeModel.ID_Gebaeude),
            nameof(ProjektGebaeudeModel.Gebaeudename),
            nameof(ProjektGebaeudeModel.Beschreibung),
            nameof(ProjektGebaeudeModel.Z_AuswahlWohnflaeche),
            nameof(ProjektGebaeudeModel.Einheit),
            nameof(ProjektGebaeudeModel.Jahresnutzungsgrad),
            nameof(ProjektGebaeudeModel.DezentralWarmwasser),
            nameof(ProjektGebaeudeModel.Bewohner),
            nameof(ProjektGebaeudeModel.Gebaeude_Modell),
        };

        internal static Merkmale Bilden(ProjektGebaeudeModel g)
        {
            var m = new Merkmale();
            m.Einheit = g.Einheit ?? "";
            m.IstFlaeche = m.Einheit == GebaeudeVorbereitung.EINHEIT_FLAECHE;
            m.Bezugswert = g.Z_AuswahlWohnflaeche;
            m.SpezWaermeverbrauch = g.spez_Waermeverbrauch;
            m.Nutzflaeche = g.Nutzflaeche;
            m.Skalierung = m.IstFlaeche && g.Nutzflaeche > 0.0 ? g.Z_AuswahlWohnflaeche / g.Nutzflaeche : (double?)null;
            m.Bauweise = g.Bauweise;
            m.BauweiseJeM2 = g.Nutzflaeche > 0.0 ? g.Bauweise / g.Nutzflaeche : (double?)null;

            double fenster = g.gesamte_Fensterflaeche;
            double huelle = g.Flaeche_Außenwand + fenster;
            m.FensteranteilProzent = huelle > 0.0 ? fenster / huelle * 100.0 : (double?)null;
            m.SuedanteilProzent = fenster > 0.0 ? g.Fensterflaeche_Sued / fenster * 100.0 : (double?)null;

            m.InnereGewinneW = g.Interne_Waermegewinne;
            m.InnereGewinneWm2 = g.Nutzflaeche > 0.0 ? g.Interne_Waermegewinne / g.Nutzflaeche : (double?)null;
            m.WgNwg = g.Wohngebaeude_Nicht_Wohngebaeude ?? "";
            m.IstNwg = m.WgNwg.StartsWith("Nicht", StringComparison.OrdinalIgnoreCase);
            m.Gebaeudeart = g.Gebaeudeart ?? "";
            m.Baualtersklasse = g.Baualtersklasse ?? "";
            m.SollTagC = g.Raumsolltemperatur_Tag;
            m.SollNachtC = g.Raumsolltemperatur_Nachtabsenkung;
            m.AbsenkungK = g.Raumsolltemperatur_Tag - g.Raumsolltemperatur_Nachtabsenkung;
            m.Luftwechsel = g.Luftwechselrate;
            m.Zone = GebaeudeZonensatz.HatZonen(g);
            m.HeizkreisAktiv = g.Heizkreis_Aktiv;
            m.FesteNennleistung = g.Uebergabe_Leistung_Nenn.HasValue || g.Kuehl_Uebergabe_Leistung_Nenn.HasValue;
            m.SpalteModell = g.Gebaeude_Modell ?? "";
            m.VerbrauchNeuKwh = GebaeudeVorbereitung.Bilden(null, g).VerbrauchNeu;
            m.Bauform = Bauform(g);
            return m;
        }

        /// <summary>
        /// Die Bauform-Gruppe: die ersten acht Hexziffern eines SHA-256 über alle öffentlichen
        /// Felder des Modells außer <see cref="OHNE_BAUFORM"/>, nach Namen geordnet, invariant
        /// geschrieben, dazu die Zahl der Zonen. Gleiche Gebäudezeilen in verschiedenen Projekten
        /// fallen so in eine Gruppe, auch wenn Bezugswert und Einheit verschieden sind.
        /// </summary>
        internal static string Bauform(ProjektGebaeudeModel g)
        {
            var sb = new StringBuilder();
            foreach (FieldInfo f in typeof(ProjektGebaeudeModel)
                         .GetFields(BindingFlags.Public | BindingFlags.Instance)
                         .Where(f => !OHNE_BAUFORM.Contains(f.Name))
                         .OrderBy(f => f.Name, StringComparer.Ordinal))
            {
                sb.Append(f.Name).Append('=').Append(Wert(f.GetValue(g))).Append(';');
            }
            sb.Append("Zonen=").Append((g.Zonen?.Count ?? 0).ToString(CultureInfo.InvariantCulture));
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
            return Convert.ToHexStringLower(hash).Substring(0, 8);
        }

        private static string Wert(object v) => v switch
        {
            null => "∅",
            double d => d.ToString("R", CultureInfo.InvariantCulture),
            IFormattable z => z.ToString(null, CultureInfo.InvariantCulture),
            _ => v.ToString(),
        };
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Wertband</b> [Min, Max] einer Stoffgröße — beide Grenzen eingeschlossen.
    /// </summary>
    public readonly record struct Wertband(double Min, double Max)
    {
        /// <summary>Liegt <paramref name="wert"/> im Band? <c>null</c> („nicht angegeben") gilt als darin.</summary>
        public bool Enthaelt(double? wert) => !wert.HasValue || (wert.Value >= Min && wert.Value <= Max);
    }

    /// <summary>
    /// <b>Eine feste Stoffgruppe des Baustoffkatalogs</b> — der gespeicherte Text in
    /// <c>Tab_Baustoff(_STAMM).Gruppe</c>, der Ressourcenschlüssel des Anzeigenamens und die
    /// üblichen Bänder von λ, ρ und c_p der Gruppe. Ohne Bänder (<c>null</c>) gilt allein der harte
    /// Bereich des Kerns (<see cref="BaustoffCtrl.StoffwertePruefen"/>).
    /// </summary>
    public sealed record Baustoffgruppe(string Wert, string Ressource, Wertband? Lambda, Wertband? Rho, Wertband? Cp)
    {
        /// <summary>Der Anzeigename in der Sprache der Oberfläche; ohne Ressource der gespeicherte Text.</summary>
        public string Anzeigename
        {
            get
            {
                string t = null;
                try { t = MyResource.Resource.ResourceManager.GetString(Ressource); }
                catch (System.Resources.MissingManifestResourceException) { }
                return string.IsNullOrEmpty(t) ? Wert : t;
            }
        }
    }

    /// <summary>
    /// <b>Die festen Stoffgruppen</b> des Baustoffkatalogs samt ihrer Plausibilitätsbänder
    /// (Entwurf Bauteilkatalog, Abschnitt 2.2). Die Bänder sind <b>Hinweise, keine Sperren</b>: Ein
    /// Wert außerhalb wird genannt, gespeichert wird trotzdem; die harten Bänder in
    /// <see cref="BaustoffCtrl"/> bleiben, wie sie sind. Die Bänder fassen die Bemessungswerte der
    /// jeweiligen Stoffart nach DIN 4108-4 und DIN EN ISO 10456 mit Rand.
    ///
    /// <para><b>Die Gruppe bleibt Text:</b> Ein Satz, dessen Gruppe keiner festen entspricht, bleibt
    /// lesbar und wird nur benannt. Die zweistufige Gliederung in Gruppe und Klasse (E132) kommt mit
    /// dem Schemaschritt von BK-2 — sie hängt sich hier an (Naht).</para>
    /// </summary>
    public static class Baustoffgruppen
    {
        /// <summary>Der gespeicherte Text der Auffanggruppe für alles Übrige.</summary>
        public const string SONSTIGE = "Sonstige";

        private static Wertband B(double min, double max) => new(min, max);

        /// <summary>Die festen Gruppen in Anzeigereihenfolge; „Sonstige" steht zuletzt und hat keine Bänder.</summary>
        public static readonly IReadOnlyList<Baustoffgruppe> Alle = new[]
        {
            new Baustoffgruppe("Mauerwerk", "BST_GRUPPE_MAUERWERK", B(0.06, 2.5), B(250, 2800), B(800, 1200)),
            new Baustoffgruppe("Beton", "BST_GRUPPE_BETON", B(0.1, 2.6), B(300, 2600), B(800, 1200)),
            new Baustoffgruppe("Estriche", "BST_GRUPPE_ESTRICHE", B(0.3, 1.8), B(1000, 2500), B(800, 1200)),
            new Baustoffgruppe("Putze und Mörtel", "BST_GRUPPE_PUTZE", B(0.05, 1.6), B(200, 2200), B(800, 1200)),
            new Baustoffgruppe("Dämmstoffe", "BST_GRUPPE_DAEMMSTOFFE", B(0.015, 0.1), B(8, 500), B(700, 2200)),
            new Baustoffgruppe("Holz und Holzwerkstoffe", "BST_GRUPPE_HOLZ", B(0.04, 0.3), B(100, 1200), B(1200, 2500)),
            new Baustoffgruppe("Platten", "BST_GRUPPE_PLATTEN", B(0.04, 0.8), B(150, 1600), B(800, 2000)),
            new Baustoffgruppe("Bodenbeläge", "BST_GRUPPE_BODENBELAEGE", B(0.04, 1.6), B(100, 2800), B(700, 2000)),
            new Baustoffgruppe("Abdichtungen", "BST_GRUPPE_ABDICHTUNGEN", B(0.1, 0.5), B(700, 1500), B(800, 2300)),
            new Baustoffgruppe("Metalle und Glas", "BST_GRUPPE_METALLE", B(0.5, 400), B(2000, 8000), B(350, 950)),
            new Baustoffgruppe("Erdreich", "BST_GRUPPE_ERDREICH", B(0.5, 3.5), B(1200, 2400), B(800, 2200)),
            new Baustoffgruppe(SONSTIGE, "BST_GRUPPE_SONSTIGE", null, null, null)
        };

        /// <summary>Die feste Gruppe zu einem gespeicherten Text (ohne Rand, ohne Groß-/Kleinschreibung); sonst <c>null</c>.</summary>
        public static Baustoffgruppe Finden(string gruppe)
        {
            string t = gruppe?.Trim();
            if (string.IsNullOrEmpty(t)) return null;
            return Alle.FirstOrDefault(g => string.Equals(g.Wert, t, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Entspricht der Text einer festen Gruppe?</summary>
        public static bool IstFest(string gruppe) => Finden(gruppe) != null;

        /// <summary>Der Anzeigename einer Gruppe — der übersetzte Name einer festen, sonst der Text selbst.</summary>
        public static string Anzeigename(string gruppe) => Finden(gruppe)?.Anzeigename ?? (gruppe ?? "");

        /// <summary>
        /// <b>Die Hinweise eines Satzes</b> — je Stoffwert außerhalb des Bandes seiner Gruppe ein Satz,
        /// dazu einer, wenn die Gruppe keiner festen entspricht; leer, wenn alles passt. Nie eine Sperre.
        /// </summary>
        public static IReadOnlyList<string> Hinweise(string gruppe, double? lambda, double? rho, double? cp)
        {
            var liste = new List<string>(3);
            CultureInfo c = CultureInfo.CurrentCulture;
            string t = gruppe?.Trim() ?? "";
            Baustoffgruppe g = Finden(t);
            if (g == null)
            {
                if (t.Length > 0)
                    liste.Add(string.Format(c, MyResource.Resource.BST_HINWEIS_GRUPPE_FREI, t, Anzeigename(SONSTIGE)));
                return liste;
            }
            Pruefen(liste, c, g, MyResource.Resource.KFLT_SP_LAMBDA, lambda, g.Lambda, "0.0##");
            Pruefen(liste, c, g, MyResource.Resource.KFLT_SP_RHO, rho, g.Rho, "0");
            Pruefen(liste, c, g, MyResource.Resource.KFLT_SP_CP, cp, g.Cp, "0");
            return liste;
        }

        /// <summary>Die Hinweise als ein Text (Sätze mit Leerzeichen verbunden); leer, wenn alles passt.</summary>
        public static string Hinweis(string gruppe, double? lambda, double? rho, double? cp)
            => string.Join(" ", Hinweise(gruppe, lambda, rho, cp));

        private static void Pruefen(List<string> liste, CultureInfo c, Baustoffgruppe g, string feld,
                                    double? wert, Wertband? band, string format)
        {
            if (!band.HasValue || !wert.HasValue || double.IsNaN(wert.Value) || band.Value.Enthaelt(wert)) return;
            liste.Add(string.Format(c, MyResource.Resource.BST_HINWEIS_BAND, feld, wert.Value.ToString(format, c),
                                    g.Anzeigename, band.Value.Min.ToString(format, c), band.Value.Max.ToString(format, c)));
        }
    }
}

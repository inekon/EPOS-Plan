namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der ÜBERSETZER, den die Katalogfilterprofile des Kerns entgegennehmen
    /// (<c>Katalogfilterprofil.FuerBedarf</c>, <c>…FuerZeitreihe</c>,
    /// <c>…MitVerwendungsspalte</c>): Schlüssel rein, Anzeigetext raus.
    ///
    /// <para><b>Warum er hier steht.</b> Er lag als <c>BedarfAdminHuelle.Filtertext</c>
    /// in der Windows-Schale und wurde von acht Hüllen benutzt; damit hing jede
    /// Katalogliste, die ihn braucht, an der Schale — auch die, deren Datenseite
    /// plattformfrei ist (Auftrag KI‑F8, Stromganglinien-Verwaltung). Hier ist er
    /// eine Stelle für beide Plattformen; <c>BedarfAdminHuelle.Filtertext</c> ruft
    /// ihn.</para>
    ///
    /// <para><b>Ein fehlender Schlüssel bleibt als Schlüssel stehen</b>, damit er
    /// auffällt — Muster <c>KatalogBrowserProfil.Finde</c>.</para>
    /// </summary>
    internal static class Katalogtexte
    {
        /// <summary>Der Anzeigetext zu einem Ressourcenschlüssel; Rückfall ist der Schlüssel.</summary>
        internal static string Fuer(string schluessel)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { /* ein fehlender Katalog darf keine Liste mitreissen */ }
            return string.IsNullOrEmpty(t) ? schluessel : t;
        }
    }
}

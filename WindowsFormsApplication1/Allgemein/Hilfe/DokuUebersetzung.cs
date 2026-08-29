using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// A6 / Entscheid 7.1a - Englisch entsteht beim Anzeigen, nicht im Wiki.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Es werden bewusst KEINE englischen Wiki-Seiten gepflegt. Steht die
    /// Oberflaeche auf Englisch (<c>Program.nLanguage != 0</c>), leitet die
    /// Anwendung die Ziel-URL beim Oeffnen durch den Google-Uebersetzungs-Proxy:
    /// </para>
    /// <code>
    ///   https://wiki.epos-plan.de/wiki/Pufferspeicher
    ///   -> https://wiki-epos--plan-de.translate.goog/wiki/Pufferspeicher
    ///      ?_x_tr_sl=de&amp;_x_tr_tl=en&amp;_x_tr_hl=en
    /// </code>
    /// <para>
    /// Host-Regel: vorhandene Bindestriche verdoppeln, dann Punkte zu
    /// Bindestrichen, dann <c>.translate.goog</c> anhaengen. Die Reihenfolge ist
    /// zwingend - erst verdoppeln, dann ersetzen, sonst liesse sich der Host
    /// nicht mehr eindeutig zurueckrechnen.
    /// </para>
    /// <para>
    /// <b>Der Proxy ist ein Fremddienst.</b> Das <c>translate.goog</c>-Schema ist
    /// inoffiziell (empirisch geprueft am 29.08.2026, Seite "Pufferspeicher"
    /// vollstaendig uebersetzt). Deshalb faellt JEDER Fehler und jeder fremde
    /// Host stillschweigend auf die deutsche Original-URL zurueck - ein toter
    /// Link darf hier nie entstehen.
    /// </para>
    /// <para>
    /// Der Hilfe-Assistent ist davon unabhaengig: Er antwortet in der Sprache
    /// der Oberflaeche und uebersetzt die deutschen Auszuege selbst.
    /// </para>
    /// </remarks>
    internal static class DokuUebersetzung
    {
        /// <summary>Nur Adressen dieses Hosts werden umgeleitet.</summary>
        private const string WikiHost = "wiki.epos-plan.de";

        /// <summary>Kennung des Uebersetzungs-Proxys.</summary>
        private const string ProxySuffix = ".translate.goog";

        /// <summary>Quellsprache, Zielsprache und Oberflaechensprache des Proxys.</summary>
        private const string ProxyAbfrage = "_x_tr_sl=de&_x_tr_tl=en&_x_tr_hl=en";

        /// <summary>
        /// Die Adresse, wie sie geoeffnet werden soll: deutsch im Original,
        /// englisch durch den Uebersetzungs-Proxy.
        /// </summary>
        /// <param name="url">Die aufgeloeste Ziel-URL, ggf. mit Sprungmarke.</param>
        /// <returns>
        /// Die umgeleitete Adresse - oder unveraendert <paramref name="url"/>,
        /// wenn die Oberflaeche deutsch ist, der Host nicht das Wiki ist oder
        /// die Umleitung aus irgendeinem Grund scheitert.
        /// </returns>
        internal static string FuerAnzeige(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;

            try
            {
                if (Program.nLanguage == 0) return url;   // deutsche Oberflaeche

                if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri adresse)) return url;
                if (!string.Equals(adresse.Host, WikiHost, StringComparison.OrdinalIgnoreCase)) return url;

                string proxyHost = ProxyHost(adresse.Host);
                if (string.IsNullOrEmpty(proxyHost)) return url;

                string hafen = adresse.IsDefaultPort ? "" : ":" + adresse.Port;

                // Die Abfrage haengt an Pfad und vorhandener Abfrage; die
                // Sprungmarke bleibt HINTER der Abfrage - andersherum wuerde der
                // Browser sie als Teil der Marke lesen.
                string abfrage = string.IsNullOrEmpty(adresse.Query)
                    ? "?" + ProxyAbfrage
                    : adresse.Query + "&" + ProxyAbfrage;

                return adresse.Scheme + "://" + proxyHost + hafen +
                       adresse.AbsolutePath + abfrage + adresse.Fragment;
            }
            catch (Exception ex)
            {
                // Nie ein toter Link: im Zweifel das deutsche Original.
                System.Diagnostics.Debug.WriteLine(
                    "[Help] WARNUNG: Uebersetzungs-Proxy nicht anwendbar, oeffne das Original: " + ex.Message);
                return url;
            }
        }

        /// <summary>
        /// Bildet den Proxy-Host: <c>wiki.epos-plan.de</c> ->
        /// <c>wiki-epos--plan-de.translate.goog</c>.
        /// </summary>
        private static string ProxyHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host)) return "";

            // Reihenfolge zwingend: erst die vorhandenen Bindestriche verdoppeln,
            // erst danach die Punkte zu Bindestrichen machen.
            string umgebaut = host.Trim().ToLowerInvariant()
                                  .Replace("-", "--")
                                  .Replace(".", "-");

            return umgebaut + ProxySuffix;
        }
    }
}

using System;
using System.Globalization;
using System.Threading;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die GEMEINSAME Vorrichtung für die Kultur der Testklassen in
    /// <c>EPOS.Kern.Tests</c>, die deutsche Ressourcentexte gegen <c>Contains</c>/<c>Equal</c>
    /// halten — Auftrag #230 (11.09.2026), Befund „Windows-CI rot seit Lauf 262": Der
    /// Windows-Läufer läuft unter der Kultur <c>en-US</c>, und die Satellitenressourcen
    /// (<c>MyResource/Resource.en.resx</c>) folgen <c>CurrentUICulture</c> — nicht
    /// <c>CurrentCulture</c>, die nur die Zahlenformatierung bestimmt. Fünf Testklassen
    /// (<c>FlottenPlanerLageTests</c>, <c>SpeicherFlottenProjektKostenTests</c>,
    /// <c>SpeicherFlottenLaufSnapshotTests</c>, <c>KiMaskenbrueckeTests</c>,
    /// <c>KiDialogaufrufTests</c>) pinnten die Kultur gar nicht oder nur
    /// <c>CurrentCulture</c> — auf Linux (invariante Kultur, Gate <c>kern.yml</c>) unsichtbar,
    /// auf dem Windows-Läufer zwölf rote Fälle (Lauf 315).
    ///
    /// <para>Baugleich zu <c>EPOS.UI.Tests/Kulturvorrichtung.cs</c> (Auftrag #168, Befund
    /// #167) — bewusst eine EIGENE Datei statt einer projektübergreifenden Quelle: Die
    /// UI-Fassung trägt zusätzlich <c>EposBunitContext</c>, das <c>bunit</c> voraussetzt,
    /// welches <c>EPOS.Kern.Tests</c> nicht referenziert (und aus gutem Grund nicht
    /// referenzieren soll — der Kern kennt keine Oberfläche). Merkt beim Anlegen die VIER
    /// Werte EINZELN (nicht die UI-Kultur aus dem Kultur-Merkwert) und stellt in
    /// <c>Dispose</c> jeden aus seinem eigenen Merkwert zurück — dieselbe Lehre aus #167.
    /// </summary>
    public sealed class Kulturvorrichtung : IDisposable
    {
        private readonly CultureInfo _defaultThreadCurrentCultureVorher;
        private readonly CultureInfo _defaultThreadCurrentUICultureVorher;
        private readonly CultureInfo _threadCurrentCultureVorher;
        private readonly CultureInfo _threadCurrentUICultureVorher;

        /// <summary>
        /// Merkt die vier Werte und setzt <paramref name="kultur"/> (Standard
        /// <c>de-DE</c>) auf <c>CultureInfo.DefaultThreadCurrentCulture</c>,
        /// <c>…DefaultThreadCurrentUICulture</c>, <c>Thread.CurrentThread.CurrentCulture</c>
        /// und <c>…CurrentUICulture</c>.
        /// </summary>
        public Kulturvorrichtung(string kultur = "de-DE")
        {
            _defaultThreadCurrentCultureVorher = CultureInfo.DefaultThreadCurrentCulture;
            _defaultThreadCurrentUICultureVorher = CultureInfo.DefaultThreadCurrentUICulture;
            _threadCurrentCultureVorher = Thread.CurrentThread.CurrentCulture;
            _threadCurrentUICultureVorher = Thread.CurrentThread.CurrentUICulture;

            CultureInfo neu = CultureInfo.GetCultureInfo(kultur);
            CultureInfo.DefaultThreadCurrentCulture = neu;
            CultureInfo.DefaultThreadCurrentUICulture = neu;
            Thread.CurrentThread.CurrentCulture = neu;
            Thread.CurrentThread.CurrentUICulture = neu;
        }

        /// <summary>Stellt jeden der vier Werte aus seinem eigenen Merkwert zurück.</summary>
        public void Dispose()
        {
            CultureInfo.DefaultThreadCurrentCulture = _defaultThreadCurrentCultureVorher;
            CultureInfo.DefaultThreadCurrentUICulture = _defaultThreadCurrentUICultureVorher;
            Thread.CurrentThread.CurrentCulture = _threadCurrentCultureVorher;
            Thread.CurrentThread.CurrentUICulture = _threadCurrentUICultureVorher;
        }
    }
}

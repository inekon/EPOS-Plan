using System;
using System.IO;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Pfade"/>: die Ordner des Betriebssystems
    /// über <see cref="Environment.SpecialFolder"/>, mit ZEICHENGLEICHEN Unterordnern
    /// zum Bestand.
    ///
    /// <para><b>Warum das schon ohne Adapter richtig ist.</b>
    /// <see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/> gibt es auf
    /// jeder Plattform; unter Windows liefert es dieselben Pfade wie bisher, unter Linux
    /// und macOS die dortigen Entsprechungen (<c>~/.config</c>, <c>~/.local/share</c>).
    /// Die Windows-Fassung <c>WindowsPfade</c> muss deshalb nur eine Angabe wirklich
    /// überschreiben: <see cref="Produktdaten"/>, das im Bestand über
    /// <c>Application.ProductName</c> gebildet wird und damit an WinForms hängt.</para>
    /// </summary>
    public class StandardPfade : IPfade
    {
        /// <summary>Der Unterordner unter <c>%APPDATA%</c> — klein geschrieben, gewachsener Bestand.</summary>
        protected const string OrdnerKlein = "wp-plan";

        /// <summary>Der Unterordner unter den übrigen Wurzeln, und der Rückfall für <see cref="Produktdaten"/>.</summary>
        protected const string OrdnerGross = "WP-Plan";

        /// <inheritdoc/>
        public virtual string Anwendungsdaten
        {
            get { return Path.Combine(Wurzel(Environment.SpecialFolder.ApplicationData), OrdnerKlein); }
        }

        /// <inheritdoc/>
        public virtual string Produktdaten
        {
            get { return Path.Combine(Wurzel(Environment.SpecialFolder.ApplicationData), OrdnerGross); }
        }

        /// <inheritdoc/>
        public virtual string BenutzerLokal
        {
            get { return Path.Combine(BenutzerLokalBasis, OrdnerGross); }
        }

        /// <inheritdoc/>
        public virtual string BenutzerLokalBasis
        {
            get { return Wurzel(Environment.SpecialFolder.LocalApplicationData); }
        }

        /// <inheritdoc/>
        public virtual string Gemeinsam
        {
            get { return Path.Combine(Wurzel(Environment.SpecialFolder.CommonApplicationData), OrdnerGross); }
        }

        /// <inheritdoc/>
        public virtual string Dokumente
        {
            get { return Wurzel(Environment.SpecialFolder.MyDocuments); }
        }

        /// <summary>Der Name des ausgelieferten Herstellerdatenordners.</summary>
        protected const string OrdnerHerstellerdaten = "VDI-3805-Daten";

        /// <summary>
        /// <inheritdoc cref="IPfade.Herstellerdaten"/>
        /// </summary>
        /// <remarks>
        /// <para><b>Eine Suche, zwei Lagen — und deshalb KEINE Windows-Sonderfassung.</b>
        /// Beim Anwender liegt der Ordner unmittelbar neben der Anwendung
        /// (<c>{app}\VDI-3805-Daten</c>, so legt ihn das Setup hin, W6‑O‑9); im
        /// Entwicklungsstand liegt er in der Wurzel des Repositorys, also einige Ebenen
        /// ÜBER dem Ausgabeordner <c>bin\x64\Release\net10.0-windows</c>. Beides findet
        /// derselbe Aufstieg von <see cref="AppContext.BaseDirectory"/> aus — der
        /// installierte Fall trifft schon auf der ersten Stufe. Eine eigene
        /// <c>WindowsPfade</c>-Fassung brächte hier nichts als eine zweite Stelle, die
        /// auseinanderlaufen kann.</para>
        ///
        /// <para><b>Acht Ebenen</b> — dieselbe Grenze, die
        /// <c>CecWechselrichterAuslieferungTests</c> für seine Dateisuche zieht: Sie
        /// reicht vom Prüfstand bis zur Repowurzel und läuft nicht bis zum
        /// Laufwerksstamm.</para>
        ///
        /// <para><b>Der Wert wird gemerkt</b>, denn er wird bei jedem Öffnen eines
        /// Dateiwählers geholt und ändert sich zur Laufzeit nicht.</para>
        /// </remarks>
        public virtual string Herstellerdaten
        {
            get
            {
                if (_herstellerdaten != null) return _herstellerdaten;

                string gefunden = "";
                try
                {
                    var d = new DirectoryInfo(AppContext.BaseDirectory);
                    for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
                    {
                        string kandidat = Path.Combine(d.FullName, OrdnerHerstellerdaten);
                        if (Directory.Exists(kandidat)) { gefunden = kandidat; break; }
                    }
                }
                catch { gefunden = ""; }

                _herstellerdaten = gefunden;
                return _herstellerdaten;
            }
        }

        private string _herstellerdaten;

        /// <summary>Der Ordner, in dem die Auslieferungsvorlage liegt.</summary>
        protected const string OrdnerVorlage = "Vorlage";

        /// <summary>Dateiname der Auslieferungsvorlage — gleichlautend mit der Arbeitsdatenbank.</summary>
        protected const string DateiVorlage = "Kenndaten.sqlite";

        /// <summary>Der Ordner des Setups im Entwicklungsstand.</summary>
        private const string OrdnerSetup = "Setup";

        /// <summary>
        /// <inheritdoc cref="IPfade.Auslieferungsvorlage"/>
        /// </summary>
        /// <remarks>
        /// <para><b>Derselbe Aufstieg wie bei <see cref="Herstellerdaten"/>, nur mit zwei
        /// Kandidaten je Stufe.</b> Beim Anwender liegt die Vorlage unmittelbar neben der
        /// Anwendung (<c>{app}\Vorlage\Kenndaten.sqlite</c>) — das trifft schon die erste
        /// Stufe. Im Entwicklungsstand liegt sie unter <c>Setup\Vorlage\</c> in der Wurzel
        /// des Repositorys, also einige Ebenen ÜBER dem Ausgabeordner; deshalb wird je
        /// Stufe auch <c>Setup\Vorlage\Kenndaten.sqlite</c> geprüft. Acht Ebenen, dieselbe
        /// Grenze wie oben: vom Prüfstand bis zur Repowurzel, nicht bis zum
        /// Laufwerksstamm.</para>
        ///
        /// <para><b>Der Rückfall ist der erwartete Ort, nicht die leere Zeichenkette</b>
        /// (siehe Schnittstelle): <c>&lt;Programmordner&gt;\Vorlage\Kenndaten.sqlite</c>.
        /// So kann die Startmeldung immer sagen, wo gesucht wurde.</para>
        ///
        /// <para><b>Der Wert wird NICHT gemerkt.</b> Er wird genau einmal je
        /// Programmstart geholt, und ein Merker würde in den Prüfständen zwischen den
        /// Fällen hängen bleiben — anders als bei <see cref="Herstellerdaten"/>, das bei
        /// jedem Öffnen eines Dateiwählers gefragt wird.</para>
        /// </remarks>
        public virtual string Auslieferungsvorlage
        {
            get
            {
                string grund = "";
                try
                {
                    grund = AppContext.BaseDirectory ?? "";

                    var d = new DirectoryInfo(grund);
                    for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
                    {
                        // Beim Anwender: {app}\Vorlage\Kenndaten.sqlite
                        string neben = Path.Combine(d.FullName, OrdnerVorlage, DateiVorlage);
                        if (File.Exists(neben)) return neben;

                        // Im Entwicklungsstand: <Repowurzel>\Setup\Vorlage\Kenndaten.sqlite
                        string imSetup = Path.Combine(d.FullName, OrdnerSetup, OrdnerVorlage, DateiVorlage);
                        if (File.Exists(imSetup)) return imSetup;
                    }
                }
                catch { }

                // Nichts gefunden: der ERWARTETE Ort neben dem Programm.
                try { return Path.Combine(grund, OrdnerVorlage, DateiVorlage); }
                catch { return ""; }
            }
        }

        /// <inheritdoc/>
        public string Verbinde(string wurzel, params string[] teile)
        {
            string pfad = wurzel ?? "";
            if (teile != null)
            {
                for (int i = 0; i < teile.Length; i++)
                {
                    if (string.IsNullOrEmpty(teile[i])) continue;
                    pfad = Path.Combine(pfad, teile[i]);
                }
            }
            return pfad;
        }

        /// <inheritdoc/>
        public string Unterordner(string wurzel, params string[] teile)
        {
            string pfad = Verbinde(wurzel, teile);
            try { if (!string.IsNullOrEmpty(pfad)) Directory.CreateDirectory(pfad); } catch { }
            return pfad;
        }

        private static string Wurzel(Environment.SpecialFolder ordner)
        {
            try { return Environment.GetFolderPath(ordner) ?? ""; } catch { return ""; }
        }
    }
}

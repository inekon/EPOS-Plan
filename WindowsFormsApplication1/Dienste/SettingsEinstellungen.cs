using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die BRÜCKE zwischen <see cref="IEinstellungen"/> und
    /// <c>Properties.Settings</c>: Gelesen wird zuerst dort, dann in der Registry.
    ///
    /// <para><b>Warum es diese Brücke gibt.</b> Der Kern kennt genau eine
    /// Einstellungsablage, der Bestand hat zwei. <c>Properties.Settings</c> hält die neun
    /// Werte des Einstellungsdialogs (<c>DBPath</c>, <c>DBName</c>, <c>PVGISUrl</c>,
    /// <c>GeoKodierung</c>, <c>WordPressUrl</c>, <c>VDI3805Path</c>, <c>DBExportPath</c>,
    /// <c>DBImportPath</c>, <c>AllgemeinPath</c>) in der <c>user.config</c>; die Registry
    /// hält Sprache, KI-Einstellungen, Einwilligung, CSV-Pfad und Lizenzanker. Ein
    /// Kernaufruf <c>Dienste.Einstellungen.Lies("PVGISUrl", …)</c> muss beides finden.
    /// Die Reihenfolge ist eindeutig: <c>Properties.Settings</c> zuerst, weil dort die
    /// Werksvorgaben der <c>app.config</c> hinterlegt sind und der Einstellungsdialog
    /// dorthin schreibt.</para>
    ///
    /// <para><b>Geschrieben wird ausschließlich in die Registry.</b> In
    /// <c>Properties.Settings</c> schreiben nur der Einstellungsdialog und die
    /// Erststart-Migration — das bleibt so; ein Kernaufruf würde sonst die
    /// <c>user.config</c> hinter dem Rücken des Dialogs verändern.</para>
    /// </summary>
    public sealed class SettingsEinstellungen : IEinstellungen
    {
        private readonly RegistryEinstellungen _registry;

        /// <summary>Legt die Brücke über den Standardzweig <c>HKCU\Software\wp-plan</c>.</summary>
        public SettingsEinstellungen() : this(new RegistryEinstellungen())
        {
        }

        /// <summary>Legt die Brücke über eine vorgegebene Registry-Ablage.</summary>
        public SettingsEinstellungen(RegistryEinstellungen registry)
        {
            _registry = registry ?? new RegistryEinstellungen();
        }

        /// <inheritdoc/>
        public string Lies(string schluessel, string vorgabe = null)
        {
            string wert = AusSettings(schluessel);
            if (wert != null) return wert;

            return _registry.Lies(schluessel, vorgabe);
        }

        /// <inheritdoc/>
        public int LiesZahl(string schluessel, int vorgabe = 0)
        {
            return _registry.LiesZahl(schluessel, vorgabe);
        }

        /// <inheritdoc/>
        public void Schreib(string schluessel, string wert)
        {
            _registry.Schreib(schluessel, wert);
        }

        /// <inheritdoc/>
        public void SchreibZahl(string schluessel, int wert)
        {
            _registry.SchreibZahl(schluessel, wert);
        }

        /// <inheritdoc/>
        public void Loesche(string schluessel)
        {
            _registry.Loesche(schluessel);
        }

        /// <inheritdoc/>
        public string LiesMaschine(string schluessel, string vorgabe = null)
        {
            return _registry.LiesMaschine(schluessel, vorgabe);
        }

        /// <summary>
        /// Liest einen Wert aus <c>Properties.Settings</c>; <c>null</c>, wenn es den
        /// Schlüssel dort nicht gibt oder er leer ist. Der Zugriff läuft über den
        /// Namensindex der <c>ApplicationSettingsBase</c> — ein unbekannter Name wirft
        /// dort, deshalb der Fangblock.
        /// </summary>
        private static string AusSettings(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return null;
            try
            {
                object wert = Properties.Settings.Default[schluessel];
                string text = wert as string;
                return string.IsNullOrEmpty(text) ? null : text;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

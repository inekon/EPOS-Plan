using Microsoft.Win32;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="ISprache"/>: dieselbe Kultur wie
    /// <see cref="StandardSprache"/>, zusätzlich der Registry-Wert, aus dem der nächste
    /// Programmstart liest.
    ///
    /// <para><b>Der Registry-Pfad steht hier ZEICHENGLEICH zum Bestand</b> — mit dem
    /// doppelten Gegenschrägstrich, den <c>Program.Main</c> und <c>MDIMainForm</c> seit
    /// jeher schreiben. Die Registry-Klasse von .NET fasst mehrfache Trennzeichen
    /// zusammen, der Wert liegt also im selben Schlüssel wie alle übrigen Einstellungen;
    /// verlassen wird sich darauf trotzdem nicht: Der Sprachwert wird mit genau der
    /// Zeichenkette gelesen und geschrieben, mit der er angelegt wurde.</para>
    ///
    /// <para><b>Wirksam wird eine Umstellung erst beim Neustart.</b> Das ist der Bestand:
    /// Die Menüpunkte „Deutsch"/„Englisch" schreiben den Wert und rufen
    /// <c>Application.Restart</c>; die Textressourcen der bereits geöffneten Masken
    /// wechseln nicht mehr.</para>
    /// </summary>
    public sealed class WindowsSprache : StandardSprache
    {
        /// <summary>
        /// Der Registry-Zweig der Sprache — zeichengleich zu <c>Program.Main</c> und
        /// <c>MDIMainForm</c>, siehe den Klassenkommentar.
        /// </summary>
        private const string RegistryPfad = @"Software\\wp-plan";

        /// <summary>Name des Registry-Werts.</summary>
        private const string RegistryWert = "Language";

        /// <summary>
        /// Übernimmt die beim letzten Mal eingestellte Sprache aus der Registry und legt
        /// den Zweig an, falls es ihn noch nicht gibt. Das ist der Startweg aus
        /// <c>Program.Main</c>, unverändert.
        /// </summary>
        public void AusRegistryUebernehmen()
        {
            int nummer = 0;
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryPfad, true);
                if (key == null) key = Registry.CurrentUser.CreateSubKey(RegistryPfad);
                if (key != null)
                {
                    using (key) { nummer = (int)key.GetValue(RegistryWert, 0); }
                }
            }
            catch { nummer = 0; }

            KulturUebernehmen(nummer != 0);
        }

        /// <inheritdoc/>
        public override void Setzen(string kuerzel)
        {
            bool englisch = IstEnglischesKuerzel(kuerzel);

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPfad))
                {
                    if (key != null) key.SetValue(RegistryWert, englisch ? 1 : 0, RegistryValueKind.DWord);
                }
            }
            catch { }

            KulturUebernehmen(englisch);
        }
    }
}

using System;
using System.Globalization;
using Microsoft.Win32;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="IEinstellungen"/> — <c>HKCU\Software\wp-plan</c>.
    ///
    /// <para><b>Derselbe Zweig wie bisher.</b> Sprache, KI-Einstellungen, KI-Einwilligung,
    /// CSV-Exportpfad, Lizenzzustimmung und Lizenz-Zeitanker liegen seit jeher unter
    /// diesem einen Schlüssel; die Schreibweisen (Zeichenkette bzw. <c>DWord</c>) bleiben
    /// unverändert, damit vorhandene Werte nach dem Umbau gelesen werden.</para>
    ///
    /// <para><b>Still, ohne Fehlerdialoge.</b> Jeder Zugriff ist eingefangen — genau wie
    /// im Bestand (<c>KiChatService</c>, <c>KiEinwilligung</c>, <c>CsvExportClass</c>).
    /// Eine gesperrte oder fehlende Registry darf die Anwendung nicht anhalten; sie
    /// arbeitet dann mit den Vorgaben weiter.</para>
    /// </summary>
    public sealed class RegistryEinstellungen : IEinstellungen
    {
        /// <summary>Der Registry-Zweig aller Anwendungseinstellungen.</summary>
        public const string StandardPfad = @"Software\wp-plan";

        private readonly string _pfad;

        /// <summary>Legt die Ablage auf <see cref="StandardPfad"/>.</summary>
        public RegistryEinstellungen() : this(StandardPfad)
        {
        }

        /// <summary>Legt die Ablage auf einen abweichenden Zweig unter <c>HKCU</c>.</summary>
        public RegistryEinstellungen(string pfad)
        {
            _pfad = string.IsNullOrEmpty(pfad) ? StandardPfad : pfad;
        }

        /// <inheritdoc/>
        public string Lies(string schluessel, string vorgabe = null)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_pfad))
                {
                    if (key == null) return vorgabe;
                    object wert = key.GetValue(schluessel);
                    if (wert == null) return vorgabe;
                    return wert as string ?? Convert.ToString(wert, CultureInfo.InvariantCulture);
                }
            }
            catch { return vorgabe; }
        }

        /// <inheritdoc/>
        public int LiesZahl(string schluessel, int vorgabe = 0)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_pfad))
                {
                    if (key == null) return vorgabe;
                    object wert = key.GetValue(schluessel);
                    if (wert == null) return vorgabe;
                    if (wert is int) return (int)wert;

                    int n;
                    string text = wert as string ?? Convert.ToString(wert, CultureInfo.InvariantCulture);
                    return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out n)
                        ? n : vorgabe;
                }
            }
            catch { return vorgabe; }
        }

        /// <inheritdoc/>
        public void Schreib(string schluessel, string wert)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_pfad))
                {
                    if (key != null) key.SetValue(schluessel, wert ?? "", RegistryValueKind.String);
                }
            }
            catch { }
        }

        /// <inheritdoc/>
        public void SchreibZahl(string schluessel, int wert)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(_pfad))
                {
                    if (key != null) key.SetValue(schluessel, wert, RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        /// <inheritdoc/>
        public void Loesche(string schluessel)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(_pfad, true))
                {
                    if (key != null) key.DeleteValue(schluessel, false);
                }
            }
            catch { }
        }

        /// <summary>
        /// Maschinenweiter Lesezugriff aus <c>HKLM</c>, in BEIDEN Registry-Sichten.
        ///
        /// <para>Grund für die zwei Sichten: Die x86-Fassung der Anwendung landete über
        /// die WOW6432Node-Umleitung tatsächlich in <c>HKLM\SOFTWARE\WOW6432Node\wp-plan</c>,
        /// die x64-Fassung liest dagegen <c>HKLM\SOFTWARE\wp-plan</c>. Ohne beide Sichten
        /// würden Alteinträge aus der x86-Zeit stillschweigend wirkungslos
        /// (<c>Konzept_Umstellung_64Bit_EPOS-Plan.md</c>, P1.1).</para>
        /// </summary>
        public string LiesMaschine(string schluessel, string vorgabe = null)
        {
            string wert = AusMaschine(RegistryView.Registry64, schluessel);
            if (wert != null) return wert;

            wert = AusMaschine(RegistryView.Registry32, schluessel);
            return wert ?? vorgabe;
        }

        private string AusMaschine(RegistryView sicht, string schluessel)
        {
            try
            {
                using (RegistryKey basis = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, sicht))
                using (RegistryKey key = basis.OpenSubKey(_pfad))
                {
                    return key == null ? null : key.GetValue(schluessel) as string;
                }
            }
            catch { return null; }
        }
    }
}

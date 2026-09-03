using System;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="IPfade"/>.
    ///
    /// <para><b>Fast alles erbt sie.</b> <c>Environment.GetFolderPath</c> liefert unter
    /// Windows bereits die richtigen Wurzeln, und die Unterordnernamen stehen in
    /// <see cref="StandardPfade"/>. Zu überschreiben ist genau eine Angabe:
    /// <see cref="Produktdaten"/>, weil der Bestand sie über
    /// <c>Application.ProductName</c> bildet — eine WinForms-Eigenschaft, die im Kern
    /// nicht zur Verfügung steht.</para>
    ///
    /// <para><b>Warum das nicht zusammengelegt wird.</b> <c>Application.ProductName</c>
    /// liefert hier <c>EPOS-Plan</c> (aus <c>AssemblyProduct</c>), nicht
    /// <c>wp-plan</c> und nicht <c>WP-Plan</c>. Unter <c>%APPDATA%\EPOS-Plan</c> liegen
    /// auf jedem Bestandsrechner <c>help_cache.json</c> und der Startbestand des
    /// Wiki-Katalogs. Ein Zusammenlegen mit <see cref="IPfade.Anwendungsdaten"/> würde
    /// beides wegwerfen.</para>
    /// </summary>
    public sealed class WindowsPfade : StandardPfade
    {
        /// <inheritdoc/>
        public override string Produktdaten
        {
            get
            {
                string wurzel;
                try { wurzel = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
                catch { wurzel = ""; }

                return Path.Combine(wurzel ?? "", Application.ProductName ?? OrdnerGross);
            }
        }
    }
}

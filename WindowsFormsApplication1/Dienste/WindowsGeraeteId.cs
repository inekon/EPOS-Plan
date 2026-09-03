using System;
using System.IO;
using Microsoft.Win32;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Fassung von <see cref="IGeraeteId"/>: Machine-GUID und
    /// Systemlaufwerk.
    ///
    /// <para><b>Die Zeichenkette von <see cref="Kennung"/> ist eingefroren.</b> Sie geht
    /// unverändert in den SHA-256-Abdruck ein, an den der Lizenzserver ein Gerät bindet.
    /// Reihenfolge, Trennzeichen und Bestandteile entsprechen zeichengleich dem Stand vor
    /// iU5 (<c>GeraeteId.Ermitteln</c>): Machine-GUID, senkrechter Strich, Laufwerksname,
    /// senkrechter Strich, Gesamtgröße.</para>
    ///
    /// <para><b>Warum ausdrücklich die 64-bit-Registry-Sicht.</b> Sie schützte zur
    /// x86-Zeit vor der WOW6432Node-Umleitung und hält die Geräte-ID über beide Ären
    /// hinweg stabil — vorhandene Lizenz-Token bleiben nach der x64-Umstellung gültig.</para>
    ///
    /// <para><b>Kein WMI.</b> Die Geräte-Identität kam nie über
    /// <c>System.Management</c>/<c>Win32_*</c>; die Paketreferenz war tot und ist mit
    /// iU5 entfernt.</para>
    /// </summary>
    public sealed class WindowsGeraeteId : IGeraeteId
    {
        /// <inheritdoc/>
        public string Kennung
        {
            get { return MachineGuid() + "|" + SystemlaufwerkSerie(); }
        }

        /// <inheritdoc/>
        public string Anzeige
        {
            get
            {
                try { return Environment.MachineName + " (" + Environment.UserName + ")"; }
                catch { return Environment.MachineName; }
            }
        }

        private static string MachineGuid()
        {
            try
            {
                using (RegistryKey basis = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine,
                           Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32))
                using (RegistryKey key = basis.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography"))
                {
                    return key?.GetValue("MachineGuid") as string ?? "";
                }
            }
            catch { return ""; }
        }

        private static string SystemlaufwerkSerie()
        {
            try
            {
                string wurzel = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows));
                var info = new DriveInfo(wurzel);
                // VolumeLabel kann sich ändern; die Kombination aus Name und
                // Gesamtgröße ist ein hinreichend stabiles Zweitmerkmal.
                return info.Name + "|" + info.TotalSize;
            }
            catch { return ""; }
        }
    }
}

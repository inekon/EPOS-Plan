using Microsoft.Win32;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Stabiler Geräte-Fingerabdruck für die Lizenzbindung.
    ///
    /// Grundlage sind die Windows-Machine-GUID und die Seriennummer des
    /// Systemlaufwerks. Übertragen wird ausschließlich ein SHA-256-Hash —
    /// ein Rückschluss auf die Hardware ist nicht möglich.
    /// </summary>
    public static class GeraeteId
    {
        private static string _cache;

        /// <summary>Geräte-ID im Format "SHA256:&lt;hex&gt;".</summary>
        public static string Ermitteln()
        {
            if (_cache != null) return _cache;

            var merkmale = new StringBuilder();
            merkmale.Append("epos-plan|");
            merkmale.Append(MachineGuid()).Append('|');
            merkmale.Append(SystemlaufwerkSerie());

            using (var sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(merkmale.ToString()));
                _cache = "SHA256:" + Convert.ToHexString(hash);
            }
            return _cache;
        }

        /// <summary>Anzeigename des Geräts (Rechnername + Benutzer).</summary>
        public static string Anzeigename()
        {
            try { return Environment.MachineName + " (" + Environment.UserName + ")"; }
            catch { return Environment.MachineName; }
        }

        /// <summary>
        /// Windows-Machine-GUID. Seit der x64-Umstellung läuft die Anwendung als x64;
        /// die ausdrückliche 64-bit-Registry-Sicht bleibt trotzdem richtig — sie schützte
        /// zur x86-Zeit vor der WOW6432Node-Umleitung und hält die Geräte-ID dadurch über
        /// beide Ären hinweg stabil (vorhandene Lizenz-Token bleiben gültig).
        /// </summary>
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

        /// <summary>Volume-Seriennummer des Systemlaufwerks (z. B. C:).</summary>
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

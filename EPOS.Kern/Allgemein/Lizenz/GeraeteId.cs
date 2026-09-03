using System;
using System.Security.Cryptography;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Stabiler Geräte-Fingerabdruck für die Lizenzbindung.
    ///
    /// Grundlage sind die Merkmale, die <c>Dienste.GeraeteId</c> liefert — unter
    /// Windows die Machine-GUID und die Kennung des Systemlaufwerks. Übertragen wird
    /// ausschließlich ein SHA-256-Hash — ein Rückschluss auf die Hardware ist nicht
    /// möglich.
    ///
    /// <para><b>Die gehashte Zeichenkette ist eingefroren.</b> Sie lautet unverändert
    /// <c>"epos-plan|" + Machine-GUID + "|" + Laufwerk + "|" + Groesse</c>. Jede
    /// Änderung daran ergibt einen anderen Abdruck und macht bereits ausgestellte
    /// Lizenz-Token ungültig; die plattformabhängige Hälfte steht deshalb seit iU5 in
    /// <c>WindowsGeraeteId.Kennung</c> und wird dort nicht angerührt.</para>
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
            merkmale.Append(Dienste.GeraeteId.Kennung);

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
            return Dienste.GeraeteId.Anzeige;
        }
    }
}

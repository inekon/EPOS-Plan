using System;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Repräsentiert ein PV-Modul aus der CEC-Datenbank.
    /// Kein NuGet erforderlich – Mapping erfolgt manuell im CSV-Parser.
    /// </summary>
    public class PVModule
    {
        public PanModule Source;
        public string Database { get; set; } = string.Empty;
        public string Name         { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public string Technology   { get; set; } = string.Empty;
        public string Bifacial     { get; set; } = string.Empty;
        public double STC          { get; set; }
        public double PTC          { get; set; }
        public double A_c          { get; set; }
        public double Length       { get; set; }
        public double Width        { get; set; }
        public int    N_s          { get; set; }
        public double I_sc_ref     { get; set; }
        public double V_oc_ref     { get; set; }
        public double I_mp_ref     { get; set; }
        public double V_mp_ref     { get; set; }
        public double alpha_sc     { get; set; }
        public double beta_oc      { get; set; }
        public double T_NOCT       { get; set; }
        public double a_ref        { get; set; }
        public double I_L_ref      { get; set; }
        public double I_o_ref      { get; set; }
        public double R_s          { get; set; }
        public double R_sh_ref     { get; set; }
        public double Adjust       { get; set; }
        public double gamma_pmp    { get; set; }
        public string BIPV { get; set; } = string.Empty;
        public string Version      { get; set; } = string.Empty;
        public int    Date         { get; set; }

        public double Efficiency   => A_c > 0 ? STC / (A_c * 1000.0) * 100.0 : 0.0;

        private static string ExtractManufacturer(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unbekannt";
            return name.Split('_')[0].Trim();
        }

        private static int ExtractYear(string name)
        {
            var matches = Regex.Matches(name, @"\b(19|20)\d{2}\b");

            // In C# 7.3 greifen wir auf das letzte Element über [Count - 1] zu
            if (matches.Count > 0)
            {
                var lastMatch = matches[matches.Count - 1];
                if (int.TryParse(lastMatch.Value, out int y))
                {
                    return y;
                }
            }

            return 0;
        }

        public override string ToString() => Name;
    }
}

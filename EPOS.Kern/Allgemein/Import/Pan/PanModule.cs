using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// Repräsentiert ein PV-Modul aus einer PVsyst PAN-Datei.
    /// Alle Parameter entsprechen dem PVsyst-Format Version 6.x / 7.x.
    /// </summary>
    public class PanModule
    {
        // ── Herkunft ──────────────────────────────────────────────────────
        public string SourceFile { get; set; } = "";    // Dateiname der .pan-Datei

        // ── Identifikation ────────────────────────────────────────────────
        public string Manufacturer { get; set; } = "";
        public string Model { get; set; } = "";
        public string DataSource { get; set; } = "";
        public int YearBegin { get; set; }

        // ── Abmessungen ───────────────────────────────────────────────────
        public double Width { get; set; }          // m
        public double Height { get; set; }          // m
        public double Depth { get; set; }          // m  (Rahmendicke)
        public double Weight { get; set; }          // kg

        // ── Technologie ───────────────────────────────────────────────────
        public string Technol { get; set; } = "";    // z.B. mtSiMono, mtSiPolyHE, etc.
        public string Technology => MapTechnology(Technol);
        public int NCelS { get; set; }          // Zellen in Reihe
        public int NCelP { get; set; }          // Stränge parallel
        public string SubModuleLayout { get; set; } = "";    // z.B. slTwinHalfCells
        public string FrontSurface { get; set; } = "";    // z.B. fsARCoating
        public bool Bifacial { get; set; }
        public double BifacialityFactor { get; set; }

        // ── STC-Bedingungen (Referenz) ────────────────────────────────────
        public double GRef { get; set; } = 1000;  // W/m²
        public double TRef { get; set; } = 25.0;  // °C

        // ── Nennleistung ─────────────────────────────────────────────────
        public double PNom { get; set; }          // W
        public double PNomTolLow { get; set; }          // % (negativer Toleranzwert)
        public double PNomTolUp { get; set; }          // % (positiver Toleranzwert)

        // ── STC-Kennwerte ─────────────────────────────────────────────────
        public double Isc { get; set; }          // A
        public double Voc { get; set; }          // V
        public double Imp { get; set; }          // A
        public double Vmp { get; set; }          // V
        public double Pmp => Imp * Vmp;          // W (berechnet)

        // ── Temperaturkoeffizienten ───────────────────────────────────────
        public double muISC { get; set; }          // mA/GradC (PVsyst-Konvention; A/K = muISC/1000)
        public double muVocSpec { get; set; }          // mV/GradC (V/K = muVocSpec/1000; relativ in %/K: /Voc/10, siehe muVocPerc)
        public double muPmpReq { get; set; }          // %/°C  (Leistungs-TK)

        // Umrechnung in die DB-Konvention von Tab_PV_STAMM: alpha_SC in A/K, beta_OC
        // in V/K. Beleg 02.09.2026: Jinko JKM260P-60 fuehrt muISC=3.40 bei Isc=9.014 A
        // - als A/K gelesen waere das der rund 400-fache plausible Wert.
        public double muIscAK => muISC / 1000.0;       // A/K
        public double muVocVK => muVocSpec / 1000.0;   // V/K

        // ── Diodenmodell (5-Parameter) ────────────────────────────────────
        public double RShunt { get; set; }          // Ω   Parallelwiderstand
        public double Rserie { get; set; }          // Ω   Serienwiderstand
        public double Gamma1 { get; set; }          // Dioden-Idealitätsfaktor
        public double mIsc0 { get; set; }          // AM-Korrekturkoeff.
        public double EgRef { get; set; }          // eV  Bandlückenenergie
        public double GammaTh { get; set; }          // Temp.-Koeff. von Gamma
        public double TCoef_Gamma { get; set; }          // K⁻¹

        // ── Berechnete Größen ─────────────────────────────────────────────
        public double Area => Width * Height;                  // m²
        public double Efficiency => Area > 0 ? PNom / (Area * GRef) * 100.0 : 0; // %
        public double FillFactor => (Isc > 0 && Voc > 0) ? Pmp / (Isc * Voc) : 0;
        public double muVocPerc => Voc > 0 ? muVocSpec / Voc / 10.0 : 0; // %/°C

        // ── Name für Anzeige ─────────────────────────────────────────────
        public string Name => string.IsNullOrWhiteSpace(Model)
                                        ? SourceFile : $"{Manufacturer} {Model}";

        // ── Hilfsmethode: PVsyst-Technologie → lesbarer Text ─────────────
        public static string MapTechnology(string t) => t switch
        {
            "mtSiMono" => "Mono-Si",
            "mtSiMonoHE" => "Mono-Si HE (PERC/HJT)",
            "mtSiPoly" => "Poly-Si",
            "mtSiPolyHE" => "Poly-Si HE",
            "mtCIS" => "CIS / CIGS",
            "mtCdTe" => "CdTe",
            "mtAmorphous" => "a-Si (amorph)",
            "mtHIT" => "HJT (Heteroübergang)",
            "mtTOPCon" => "TOPCon (N-Typ)",
            _ => t.Replace("mt", "").Replace("HE", " HE")
        };
    }
}

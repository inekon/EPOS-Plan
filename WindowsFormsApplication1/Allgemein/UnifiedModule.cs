using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Einheitliches Anzeigemodell für CEC-, Sandia- und PAN-Module im Hauptgitter.
    /// Normalisiert alle gemeinsamen Spalten; hält Referenz auf das Original.
    /// </summary>
    public class UnifiedModule
    {
        // ── Herkunft ───────────────────────────────────────────────────
        // C# 7.3: 'set' statt 'init'
        public string Database { get; set; } = "";  // "CEC", "Sandia" oder "PAN"
        public PVModule CecModule { get; set; }

        // ── Gemeinsame Anzeigespalten ──────────────────────────────────
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public string Technology { get; set; }
        public double Pmp { get; set; }           // W  Nennleistung
        public double Efficiency { get; set; }    // %
        public double Area { get; set; }          // m²
        public double Isc { get; set; }           // A
        public double Voc { get; set; }           // V
        public double Imp { get; set; }           // A
        public double Vmp { get; set; }           // V
        public string Bifacial { get; set; }
        public int Year { get; set; }

        // ── Konstruktoren ──────────────────────────────────────────────

        public static UnifiedModule FromCec(PVModule m)
        {
            // C# 7.3: Expliziter Typname bei 'new'
            return new UnifiedModule()
            {
                Database = "CEC",
                CecModule = m,
                Name = m.Name,
                Manufacturer = m.Manufacturer,
                Technology = m.Technology,
                Pmp = m.I_mp_ref * m.V_mp_ref,
                Efficiency = m.Efficiency,
                Area = m.A_c,
                Isc = m.I_sc_ref,
                Voc = m.V_oc_ref,
                Imp = m.I_mp_ref,
                Vmp = m.V_mp_ref,
                // Ternärer Operator bleibt gleich
                Bifacial = (m.Bifacial == "1" || m.Bifacial.Equals("true", StringComparison.OrdinalIgnoreCase)) ? "Ja" : "Nein",
                Year = m.Year,
            };
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
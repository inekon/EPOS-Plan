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
        public PanModule PanModule { get; set; }

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
        /// <summary>
        /// Beidseitig? Ein WAHRHEITSWERT und kein Anzeigetext (iU9-W13.0j).
        ///
        /// <para>Bis Welle 13 stand hier „Ja" bzw. „Nein" — ein DEUTSCHER
        /// Anzeigetext im Kern, gebildet aus dem Rohwert der CSV-Spalte
        /// (Befund W13-B50). Die Oberflaeche uebersetzt das jetzt selbst; der
        /// Bifazialitaetsfaktor steht getrennt daneben.</para>
        /// </summary>
        public bool Bifacial { get; set; }

        /// <summary>Der Bifazialitaetsfaktor der PAN-Datei; 0, wo es keinen gibt.</summary>
        public double BifazialFaktor { get; set; }
        public int Date { get; set; }

        // ── Konstruktoren ──────────────────────────────────────────────

        public static UnifiedModule FromPanCec(PVModule m)
        {
            // C# 7.3: Expliziter Typname bei 'new'
            return new UnifiedModule()
            {
                Database = m.Database,
                CecModule = m,
                PanModule = m.Source, // Falls die PVModule-Instanz eine Referenz zum Original PanModule enthält
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
                Bifacial = m.Bifazial,
                BifazialFaktor = m.Source != null ? m.Source.BifacialityFactor : 0.0,
                Date = m.Date,
            };
        }

        // ── Die Quellenweiche (iU9-W13.0j) ─────────────────────────────
        //
        // Die dreizehn Ternaere um.Database == "CEC" ? … : … standen in
        // Form_CECImport.ShowDetail :408-440 - eine Fachaussage im Anzeigecode.
        // Sie gehoeren ans Modell, das die beiden Quellen ohnehin kennt.

        /// <summary>Kommt das Modul aus der CEC-Modulliste?</summary>
        public bool AusCec => Database == "CEC";

        /// <summary>Modulflaeche in m² — bei PAN aus Breite × Hoehe gerechnet.</summary>
        public double Flaeche => AusCec ? (CecModule?.A_c ?? 0) : (PanModule?.Area ?? 0);

        /// <summary>Modullaenge in m.</summary>
        public double Laenge => AusCec ? (CecModule?.Length ?? 0) : (PanModule?.Height ?? 0);

        /// <summary>Modulbreite in m.</summary>
        public double Breite => AusCec ? (CecModule?.Width ?? 0) : (PanModule?.Width ?? 0);

        /// <summary>Nennleistung unter STC in W.</summary>
        public double Stc => AusCec ? (CecModule?.STC ?? 0) : (PanModule?.PNom ?? 0);

        /// <summary>
        /// Leistung unter PTC-Bedingungen in W. Der CEC-Katalog fuehrt sie, eine
        /// PAN-Datei nicht — dort wird sie geschaetzt
        /// (<see cref="PanModule.PtcGeschaetzt"/>, Befund W13-B43).
        /// </summary>
        public double Ptc => AusCec ? (CecModule?.PTC ?? 0) : (PanModule?.PtcGeschaetzt ?? 0);

        /// <summary>Leistungs-Temperaturkoeffizient in %/K.</summary>
        public double GammaPmp => AusCec ? (CecModule?.gamma_pmp ?? 0) : (PanModule?.muPmpReq ?? 0);

        /// <summary>
        /// Strom-Temperaturkoeffizient in A/K. Eine PAN-Datei fuehrt ihn als
        /// <c>muISC</c> in mA/K; der Bestand uebernahm ihn NICHT und schrieb 0
        /// (Befund W13-B44, Anwenderfrage). Woertlich behalten.
        /// </summary>
        public double? AlphaSc => AusCec ? (CecModule?.alpha_sc ?? 0) : (double?)null;

        /// <summary>
        /// Spannungs-Temperaturkoeffizient in V/K; bei PAN aus demselben Grund
        /// nicht belegt (Befund W13-B44).
        /// </summary>
        public double? BetaOc => AusCec ? (CecModule?.beta_oc ?? 0) : (double?)null;

        /// <summary>Nennbetriebszelltemperatur in °C; eine PAN-Datei fuehrt sie nicht.</summary>
        public double? TNoct => AusCec ? (CecModule?.T_NOCT ?? 0) : (double?)null;

        /// <summary>
        /// <b>Der Katalogsatz aus dem gewaehlten Modul</b> (iU9-W13.0j) — woertlich
        /// der Rumpf von <c>Form_CECImport.InitDatensatzUpdate</c> :569-600.
        ///
        /// <para><b>Woertlich behalten trotz Befund W13-B44:</b> Ein PAN-Modul kommt
        /// OHNE Temperaturkoeffizienten in den Katalog — <c>alpha_SC</c>,
        /// <c>beta_OC</c> und <c>T_NOCT</c> bleiben 0, obwohl <c>muISC</c> und
        /// <c>muVocSpec</c> in der Datei stehen und im Vorlaeufer auskommentiert
        /// danebenstanden. Ob sie umgerechnet uebernommen werden sollen, ist eine
        /// ANWENDERFRAGE (offener Punkt W13-O-2) und keine Portentscheidung.</para>
        /// </summary>
        public PhotovoltaikModel NachModell(PhotovoltaikModel model = null)
        {
            if (model == null) model = new PhotovoltaikModel();

            model.m_szName = Name;
            model.m_szFirma = Manufacturer;
            model.m_Leistung = Pmp;
            model.m_Wirkungsgrad = Efficiency;
            model.m_U_Mpp = Vmp;
            model.m_U_Leerlauf = Voc;
            model.m_I_Mpp = Imp;
            model.m_I_Kurzschluss = Isc;

            model.m_alpha_SC = AlphaSc ?? 0;
            model.m_beta_OC = BetaOc ?? 0;
            model.m_Temp_Coeff_Pmax = GammaPmp;
            model.m_T_NOCT = TNoct ?? 0;
            model.m_Laenge = Laenge;
            model.m_Breite = Breite;

            return model;
        }

        /// <summary>
        /// Die dreizehn Werte, die in den <see cref="ImportKandidat"/> der
        /// Vorpruefung gehen — genau die <c>ImportSpalten</c> des Katalogs
        /// <c>PV</c> (<c>KatalogRegistry</c> :168-170).
        /// </summary>
        public System.Collections.Generic.IDictionary<string, object> Vergleichswerte(string bezeichner)
        {
            PhotovoltaikModel m = NachModell();
            return new System.Collections.Generic.Dictionary<string, object>(
                StringComparer.OrdinalIgnoreCase)
            {
                { "Firma", m.m_szFirma },
                { "Leistung", m.m_Leistung },
                { "Wirkungsgrad", m.m_Wirkungsgrad },
                { "U_Mpp", m.m_U_Mpp },
                { "U_Leerlauf", m.m_U_Leerlauf },
                { "I_Mpp", m.m_I_Mpp },
                { "I_Kurzschluss", m.m_I_Kurzschluss },
                { "alpha_SC", m.m_alpha_SC },
                { "beta_OC", m.m_beta_OC },
                { "gamma_PMP", m.m_Temp_Coeff_Pmax },
                { "T_NOCT", m.m_T_NOCT },
                { "Laenge", m.m_Laenge },
                { "Breite", m.m_Breite }
            };
        }

        public override string ToString()
        {
            return Name;
        }
  
    }
}
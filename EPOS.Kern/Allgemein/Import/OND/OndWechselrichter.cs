using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Gerät aus einer PVsyst-<c>.OND</c>-Datei — die Rohwerte der Datei und ihre
    /// Übersetzung in einen Katalogsatz (Konzept Wechselrichter 5.2, Anwenderentscheid
    /// <b>W6‑O‑1</b> vom 06.09.2026: „der OND-Import soll umgesetzt werden").
    ///
    /// <para>Zwilling zu <see cref="CecWechselrichter"/> auf der CEC-Seite und zu
    /// <see cref="PanModule"/> auf der Modulseite: Der Dienst
    /// (<see cref="OndWechselrichterDienst"/>) liest die Datei, dieser Satz trägt ihre
    /// Werte, und <see cref="NachModell"/> macht daraus einen
    /// <see cref="WechselrichterModel"/>.</para>
    ///
    /// <para><b>Der OND-Import ist der einzige, der die Kennlinie DIREKT liefert</b>
    /// (Konzept 5.2): <c>ProfilPIO</c> ist eine Wertetabelle aus Paaren
    /// <c>P_in / P_out</c>, aus der die sechs Stützstellen durch lineare Interpolation
    /// entstehen — ohne den Modellumweg über die Sandia-Koeffizienten, den die CEC-Liste
    /// verlangt. Er ist damit die <i>bessere</i> Quelle, aber die seltenere: OND-Dateien
    /// kommen vom Hersteller oder aus PVsyst, nicht aus einem offenen Verzeichnis.</para>
    ///
    /// <para><b>Die Einheiten der Datei sind die von PVsyst</b> und nicht die des
    /// Katalogs: Die Leistungen des Wandlers (<c>PNomConv</c>, <c>PMaxOUT</c>,
    /// <c>PNomDC</c>, <c>PMaxDC</c>) stehen in <b>kW</b>, die Schwellen
    /// (<c>PSeuil</c>, <c>Pnight</c>) und die Punkte der Kennlinie in <b>W</b>, die
    /// Wirkungsgrade (<c>EfficMax</c>, <c>EfficEuro</c>) in <b>Prozent</b>. Der Katalog
    /// führt kW, W und Faktoren 0…1 — umgerechnet wird deshalb HIER, an der Stelle, an
    /// der beide Konventionen nebeneinanderstehen.</para>
    ///
    /// <para><b>Der Bezeichner trägt den Hersteller</b> — wörtlich das Muster des
    /// PAN-Imports (<c>PanDataService.Aufnehmen</c>: <c>Manufacturer + " " + Model</c>).
    /// Konzept 5.2 nennt nur „<c>Model</c> → <c>Bezeichner</c>"; ein bloßes „2500TL"
    /// stünde im Katalog aber neben CEC-Sätzen der Form „Hersteller: Modell" und wäre
    /// zwischen zwei Herstellern nicht unterscheidbar. <c>Firma</c> bleibt der reine
    /// <c>Manufacturer</c>.</para>
    /// </summary>
    public sealed class OndWechselrichter
    {
        // =================================================================
        //  Herkunft
        // =================================================================

        /// <summary>Der Dateiname ohne Endung — Rückfall für den Bezeichner.</summary>
        public string Quelldatei = "";

        /// <summary>Hersteller (<c>Manufacturer</c> im Block <c>pvCommercial</c>).</summary>
        public string Manufacturer = "";

        /// <summary>Typbezeichnung (<c>Model</c>).</summary>
        public string Model = "";

        /// <summary>Bemerkung der Datei (<c>Comment</c>) — geht in die Beschreibung.</summary>
        public string Comment = "";

        /// <summary>Datenquelle laut Datei (<c>DataSource</c>).</summary>
        public string DataSource = "";

        /// <summary>Baujahr (<c>YearBeg</c>); 0 = nicht gepflegt.</summary>
        public int YearBeg;

        // =================================================================
        //  Leistungen — in den Einheiten der DATEI
        // =================================================================

        /// <summary>AC-Nennwirkleistung [kW] (<c>PNomConv</c>).</summary>
        public double PNomConv;

        /// <summary>Maximale AC-Leistung [kW] (<c>PMaxOUT</c>) — Ausweis für <c>S_AC_Max</c>.</summary>
        public double PMaxOUT;

        /// <summary>DC-Nennleistung [kW] (<c>PNomDC</c>).</summary>
        public double PNomDC;

        /// <summary>Maximale DC-Leistung [kW] (<c>PMaxDC</c>).</summary>
        public double PMaxDC;

        /// <summary>Einschaltschwelle [W] (<c>PSeuil</c>).</summary>
        public double PSeuil;

        /// <summary>Nachtverbrauch [W] (<c>Pnight</c> bzw. <c>Night_Loss</c>).</summary>
        public double Pnight;

        // =================================================================
        //  Eingang
        // =================================================================

        /// <summary>Untere Grenze des MPP-Fensters [V] (<c>VMppMin</c>).</summary>
        public double VMppMin;

        /// <summary>Bezugsspannung der Kennlinie [V] (<c>VMppNom</c>).</summary>
        public double VMppNom;

        /// <summary>Obere Grenze des MPP-Fensters [V] (<c>VMPPMax</c>).</summary>
        public double VMPPMax;

        /// <summary>Absolut zulässige DC-Spannung [V] (<c>VAbsMax</c>).</summary>
        public double VAbsMax;

        /// <summary>Einschaltspannung [V] (<c>VStart</c>).</summary>
        public double VStart;

        /// <summary>Maximaler DC-Strom [A] (<c>IMaxDC</c>).</summary>
        public double IMaxDC;

        /// <summary>Zahl der MPP-Tracker (<c>NbMPPT</c>).</summary>
        public int NbMPPT;

        /// <summary>Zahl der DC-Eingänge (<c>NbInputs</c>) — Rückfall für <c>NbMPPT</c>.</summary>
        public int NbInputs;

        // =================================================================
        //  Wirkungsgrad
        // =================================================================

        /// <summary>Maximalwirkungsgrad [%] (<c>EfficMax</c>) — der Ausweis der Datei.</summary>
        public double EfficMax;

        /// <summary>Europäischer Wirkungsgrad [%] (<c>EfficEuro</c>) — der Ausweis der Datei.</summary>
        public double EfficEuro;

        /// <summary>
        /// Der Name der Kennlinienfassung, die genommen wurde — <c>ProfilPIOV2</c> bei
        /// drei Fassungen, sonst <c>ProfilPIO</c>. Leer, wenn die Datei keine führt.
        /// </summary>
        public string Kennlinienfassung = "";

        /// <summary>
        /// Die Wertepaare der gewählten Kennlinienfassung: <c>P_in</c> und <c>P_out</c>
        /// in W, in der Reihenfolge der Datei.
        /// </summary>
        public List<(double PIn, double POut)> Kennlinienpunkte = new List<(double, double)>();

        // =================================================================
        //  Abgeleitetes
        // =================================================================

        /// <summary>
        /// Der Bezeichner des Katalogsatzes: <c>Manufacturer Model</c>, sonst was davon
        /// da ist, sonst der Dateiname.
        /// </summary>
        public string Name
        {
            get
            {
                string n = ((Manufacturer ?? "") + " " + (Model ?? "")).Trim();
                return n.Length > 0 ? n : (Quelldatei ?? "");
            }
        }

        /// <summary>Der Hersteller — der reine <c>Manufacturer</c> der Datei.</summary>
        public string Hersteller => (Manufacturer ?? "").Trim();

        /// <summary>
        /// Die sechs Stützstellen dieses Geräts (Konzept 3.3.1) — durch lineare
        /// Interpolation aus <see cref="Kennlinienpunkte"/> auf 5, 10, 20, 30, 50 und
        /// 100 % der AC-Nennleistung.
        /// </summary>
        /// <remarks>
        /// <b>Der Bezug ist die AC-Seite.</b> Eine Stützstelle ist der Wirkungsgrad bei
        /// einem Anteil der AC-NENNLEISTUNG; gesucht wird deshalb der Punkt, an dem
        /// <c>P_out = x · P_AC,nenn</c> ist, und dort gilt <c>η = P_out / P_in</c>.
        /// Ohne Punkte oder ohne Nennleistung sind alle sechs <c>null</c> — <b>eine
        /// erfundene Zahl wird nicht geschrieben</b>.
        /// </remarks>
        public double?[] Stuetzstellen()
        {
            return WechselrichterKennlinie.AusProfil(Kennlinienpunkte, PNomConv * 1000.0);
        }

        /// <summary>
        /// Der Katalogsatz zu diesem Gerät — die Feldzuordnung aus Konzept 5.2.
        /// </summary>
        /// <param name="ziel">
        /// Optional ein vorhandenes Modell (bzw. ein Stamm-Controller), das gefüllt
        /// werden soll; <c>null</c> legt ein neues an. Dasselbe Muster wie
        /// <see cref="CecWechselrichter.NachModell"/>.
        /// </param>
        /// <remarks>
        /// <para><b>Was der OND-Import besser kann als der CEC-Import:</b> Er füllt
        /// <c>Anzahl_Mppt</c>, <c>S_AC_Max</c>, <c>P_DC_Max</c> und <c>U_Start</c> —
        /// die vier Größen, die die CEC-Liste nicht führt (offener Punkt W6‑O‑2) — und
        /// er bringt die Kennlinie als Messwerttabelle mit, statt sie zu rechnen.</para>
        ///
        /// <para><b>Was er NICHT kann:</b> Die <c>Sandia_*</c>-Spalten bleiben NULL —
        /// eine OND-Datei führt kein Sandia-Modell. Der Katalog trägt sie als
        /// mitgeschriebenes Wissen des CEC-Imports (Konzept 3.3.3); für ein
        /// OND-Gerät gibt es sie schlicht nicht.</para>
        ///
        /// <para><b><c>Eta_Euro</c> und <c>Eta_Max</c> kommen aus der DATEI</b>
        /// (<c>EfficEuro</c>, <c>EfficMax</c>) und nicht aus der Rechnung: Anders als
        /// bei CEC nennt das Datenblatt sie hier selbst, und der genannte Wert ist der
        /// belegte. Fehlt er, wird <c>Eta_Euro</c> aus den sechs Stützstellen gewichtet
        /// (<see cref="WechselrichterKennlinie.EuroWirkungsgrad"/>) und <c>Eta_Max</c>
        /// bleibt das Maximum der Stützstellen — dieselbe untere Schranke wie bei
        /// CEC.</para>
        /// </remarks>
        public WechselrichterModel NachModell(WechselrichterModel ziel = null)
        {
            WechselrichterModel m = ziel ?? new WechselrichterModel();

            m.m_szName = Name;
            m.m_szFirma = Hersteller;
            m.m_szBeschreibung = Beschreibung();

            m.m_P_AC_Nenn = PNomConv > 0.0 ? (double?)PNomConv : null;          // kW
            m.m_S_AC_Max = PMaxOUT > 0.0 ? (double?)PMaxOUT : null;             // kVA-Ausweis
            m.m_P_DC_Max = Groesser(PMaxDC, PNomDC);                            // kW
            m.m_U_Mpp_Min = VMppMin > 0.0 ? (double?)VMppMin : null;
            m.m_U_Mpp_Max = VMPPMax > 0.0 ? (double?)VMPPMax : null;
            m.m_U_Dc_Max = VAbsMax > 0.0 ? (double?)VAbsMax : null;
            m.m_U_Start = VStart > 0.0 ? (double?)VStart : null;
            m.m_I_Dc_Max = IMaxDC > 0.0 ? (double?)IMaxDC : null;
            m.m_Anzahl_Mppt = Tracker();
            m.m_Straenge_Je_Mppt = null;

            double?[] etas = Stuetzstellen();
            m.m_Eta05 = etas[0];
            m.m_Eta10 = etas[1];
            m.m_Eta20 = etas[2];
            m.m_Eta30 = etas[3];
            m.m_Eta50 = etas[4];
            m.m_Eta100 = etas[5];

            m.m_Eta_Euro = EfficEuro > 0.0
                ? (double?)(EfficEuro / 100.0)
                : WechselrichterKennlinie.EuroWirkungsgrad(etas);
            m.m_Eta_Max = EfficMax > 0.0 ? (double?)(EfficMax / 100.0) : Hoechster(etas);

            m.m_P_Standby = PSeuil != 0.0 ? (double?)PSeuil : null;             // W
            m.m_P_Nacht = Pnight != 0.0 ? (double?)Pnight : null;               // W
            m.m_Kosten = null;

            m.m_Sandia_Pdco = null;
            m.m_Sandia_Vdco = VMppNom > 0.0 ? (double?)VMppNom : null;
            m.m_Sandia_Pso = null;
            m.m_Sandia_C0 = null;
            m.m_Sandia_C1 = null;
            m.m_Sandia_C2 = null;
            m.m_Sandia_C3 = null;

            m.m_Herkunft = DbWerte.WR_HERKUNFT_OND;
            return m;
        }

        /// <summary>
        /// Die Zahl der MPP-Tracker: <c>NbMPPT</c>, ersatzweise <c>NbInputs</c>;
        /// <c>null</c>, wenn die Datei beides nicht führt.
        /// </summary>
        private int? Tracker()
        {
            if (NbMPPT > 0) return NbMPPT;
            if (NbInputs > 0) return NbInputs;
            return null;
        }

        /// <summary>Der größere der zwei DC-Werte; <c>null</c>, wenn beide fehlen.</summary>
        private static double? Groesser(double a, double b)
        {
            double max = Math.Max(a, b);
            return max > 0.0 ? (double?)max : null;
        }

        /// <summary>
        /// Der Beschreibungstext des Katalogsatzes — Herkunft, Kennlinienfassung,
        /// Baujahr und die Bemerkung der Datei.
        /// </summary>
        /// <remarks>
        /// Wie bei <see cref="CecWechselrichter"/> ohne Anzeigetext: Der Kern kennt
        /// keine Sprache. Die Kennlinienfassung steht darin, weil sie eine ENTSCHEIDUNG
        /// des Imports ist — bei drei Fassungen nimmt er die nominale (Konzept 5.2),
        /// und der Anwender soll das im Katalog nachlesen können.
        /// </remarks>
        private string Beschreibung()
        {
            var teile = new List<string> { DbWerte.WR_HERKUNFT_OND };
            if (Kennlinienfassung.Length > 0) teile.Add(Kennlinienfassung);
            if (YearBeg > 0) teile.Add(YearBeg.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(Comment)) teile.Add(Comment.Trim());
            else if (!string.IsNullOrWhiteSpace(DataSource)) teile.Add(DataSource.Trim());
            return string.Join(" - ", teile);
        }

        /// <summary>Der größte vorhandene Wert der sechs Stützstellen; <c>null</c>, wenn keine da ist.</summary>
        private static double? Hoechster(double?[] etas)
        {
            double? max = null;
            foreach (double? e in etas)
                if (e.HasValue && (!max.HasValue || e.Value > max.Value)) max = e;
            return max;
        }

        /// <summary>
        /// Die Werte, die die DUBLETTENPRÜFUNG vergleicht — genau die
        /// <c>ImportSpalten</c> der <see cref="KatalogRegistry"/>-Definition
        /// „WECHSELRICHTER", wörtlich wie <see cref="CecWechselrichter.Vergleichswerte"/>.
        /// </summary>
        public IDictionary<string, object> Vergleichswerte(string name)
        {
            WechselrichterModel m = NachModell();
            var werte = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bezeichner"] = name ?? m.m_szName,
                ["Firma"] = m.m_szFirma
            };

            Nimm(werte, WechselrichterSchema.SPALTE_P_AC_NENN, m.m_P_AC_Nenn);
            Nimm(werte, WechselrichterSchema.SPALTE_S_AC_MAX, m.m_S_AC_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_P_DC_MAX, m.m_P_DC_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_U_MPP_MIN, m.m_U_Mpp_Min);
            Nimm(werte, WechselrichterSchema.SPALTE_U_MPP_MAX, m.m_U_Mpp_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_U_DC_MAX, m.m_U_Dc_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_U_START, m.m_U_Start);
            Nimm(werte, WechselrichterSchema.SPALTE_I_DC_MAX, m.m_I_Dc_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_ANZAHL_MPPT, m.m_Anzahl_Mppt);
            Nimm(werte, WechselrichterSchema.SPALTE_STRAENGE_JE_MPPT, m.m_Straenge_Je_Mppt);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA05, m.m_Eta05);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA10, m.m_Eta10);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA20, m.m_Eta20);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA30, m.m_Eta30);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA50, m.m_Eta50);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA100, m.m_Eta100);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA_EURO, m.m_Eta_Euro);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA_MAX, m.m_Eta_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_P_STANDBY, m.m_P_Standby);
            Nimm(werte, WechselrichterSchema.SPALTE_P_NACHT, m.m_P_Nacht);
            werte[WechselrichterSchema.SPALTE_HERKUNFT] = m.m_Herkunft;

            return werte;
        }

        private static void Nimm(IDictionary<string, object> werte, string spalte, double? wert)
        {
            werte[spalte] = wert.HasValue ? (object)wert.Value : null;
        }

        private static void Nimm(IDictionary<string, object> werte, string spalte, int? wert)
        {
            werte[spalte] = wert.HasValue ? (object)wert.Value : null;
        }

        /// <summary>Der Gerätename — für Listen und Meldungen.</summary>
        public override string ToString()
        {
            return Name;
        }
    }
}

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Wechselrichter — Katalogsatz (<c>Tab_Wechselrichter_STAMM</c>) oder
    /// Projektkopie (<c>Tab_Wechselrichter</c>), Stufe S1 des
    /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c> (Anwenderentscheid <b>W6‑E‑2</b>
    /// vom 06.09.2026).
    ///
    /// <para><b>Warum <c>double?</c> und nicht <c>double</c>.</b> Das Vorbild
    /// <see cref="PhotovoltaikModel"/> führt alle Zahlen als <c>double</c> und weicht
    /// bei NULL auf 0 aus — dort ist das eingeübt, hier wäre es falsch: Bei einem
    /// Wechselrichter bedeutet NULL bei fast jedem Feld „keine Prüfung" bzw. „keine
    /// Grenze", und eine 0 hieße „Grenze null Volt" (Konzept 3.1, Spalte „NULL
    /// bedeutet"). Wer beides gleichsetzt, sperrt jeden Strang. Die einzigen Felder
    /// ohne diese Unterscheidung sind Bezeichner, Firma, Beschreibung und Herkunft —
    /// Text.</para>
    ///
    /// <para><b>Die Stützstellen sind Faktoren 0…1</b>, nicht Prozent — so, wie sie
    /// <c>PvErweitertesModell.EtaWechselrichter</c> und
    /// <c>Tab_Energieanlagen.PV_WrEta10/50/100</c> führen. Die Stützstellen 10, 50 und
    /// 100 % sind mit diesen drei Anlagenspalten deckungsgleich; ein Katalogsatz füllt
    /// sie ohne Rechnung (Konzept 3.3.1).</para>
    /// </summary>
    public class WechselrichterModel
    {
        /// <summary>Primärschlüssel der Zeile (<c>Tab_Wechselrichter(_STAMM).ID</c>).</summary>
        public int m_ID;

        /// <summary>Das Projekt der KOPIE; 0 im Katalog.</summary>
        public int m_ID_Projekt;

        /// <summary>Gerätename (<c>Bezeichner</c>).</summary>
        public string m_szName;

        /// <summary>Hersteller (<c>Firma</c>); leer = unbekannt.</summary>
        public string m_szFirma;

        /// <summary>Freitext.</summary>
        public string m_szBeschreibung;

        /// <summary>AC-Nennwirkleistung [kW] — das einzige Pflichtfeld (Konzept 6).</summary>
        public double? m_P_AC_Nenn;

        /// <summary>Maximale AC-Scheinleistung [kVA]; NULL = wie <see cref="m_P_AC_Nenn"/>.</summary>
        public double? m_S_AC_Max;

        /// <summary>Maximale DC-Eingangsleistung [kW]; NULL = keine Grenze.</summary>
        public double? m_P_DC_Max;

        /// <summary>Untere Grenze des MPP-Fensters [V]; NULL = keine Prüfung.</summary>
        public double? m_U_Mpp_Min;

        /// <summary>Obere Grenze des MPP-Fensters [V]; NULL = keine Prüfung.</summary>
        public double? m_U_Mpp_Max;

        /// <summary>Maximale DC-Eingangsspannung [V]; NULL = keine Prüfung.</summary>
        public double? m_U_Dc_Max;

        /// <summary>Einschaltspannung [V]; NULL = keine Prüfung.</summary>
        public double? m_U_Start;

        /// <summary>Maximaler DC-Strom <b>je MPPT</b> [A]; NULL = keine Prüfung.</summary>
        public double? m_I_Dc_Max;

        /// <summary>Zahl der MPP-Tracker; NULL = 1 (der konservative Fall, Konzept 5.1).</summary>
        public int? m_Anzahl_Mppt;

        /// <summary>Zulässige Stränge je MPPT; NULL = keine Prüfung.</summary>
        public int? m_Straenge_Je_Mppt;

        /// <summary>Wirkungsgrad bei 5 % der AC-Nennleistung (0…1).</summary>
        public double? m_Eta05;

        /// <summary>Wirkungsgrad bei 10 % (0…1).</summary>
        public double? m_Eta10;

        /// <summary>Wirkungsgrad bei 20 % (0…1).</summary>
        public double? m_Eta20;

        /// <summary>Wirkungsgrad bei 30 % (0…1).</summary>
        public double? m_Eta30;

        /// <summary>Wirkungsgrad bei 50 % (0…1).</summary>
        public double? m_Eta50;

        /// <summary>Wirkungsgrad bei 100 % (0…1).</summary>
        public double? m_Eta100;

        /// <summary>Europäischer Wirkungsgrad (0…1) — Ausweis, siehe
        /// <see cref="WechselrichterKennlinie.EuroWirkungsgrad"/>.</summary>
        public double? m_Eta_Euro;

        /// <summary>Maximalwirkungsgrad (0…1), nur Ausweis.</summary>
        public double? m_Eta_Max;

        /// <summary>Einschaltschwelle / Eigenverbrauch [W]; NULL = 0.</summary>
        public double? m_P_Standby;

        /// <summary>Nachtverbrauch [W]; NULL = 0.</summary>
        public double? m_P_Nacht;

        /// <summary>Gerätepreis [€] — Anwenderfeld.</summary>
        public double? m_Kosten;

        /// <summary>Sandia <c>Pdco</c> [W] — mitgeschriebenes Katalogwissen (Konzept 3.3.3).</summary>
        public double? m_Sandia_Pdco;

        /// <summary>Sandia <c>Vdco</c> [V].</summary>
        public double? m_Sandia_Vdco;

        /// <summary>Sandia <c>Pso</c> [W].</summary>
        public double? m_Sandia_Pso;

        /// <summary>Sandia <c>C0</c> [1/W].</summary>
        public double? m_Sandia_C0;

        /// <summary>Sandia <c>C1</c> [1/V].</summary>
        public double? m_Sandia_C1;

        /// <summary>Sandia <c>C2</c> [1/V].</summary>
        public double? m_Sandia_C2;

        /// <summary>Sandia <c>C3</c> [1/V].</summary>
        public double? m_Sandia_C3;

        /// <summary>
        /// Woher der Satz stammt: <see cref="DbWerte.WR_HERKUNFT_CEC"/>,
        /// <see cref="DbWerte.WR_HERKUNFT_OND"/> oder
        /// <see cref="DbWerte.WR_HERKUNFT_HAND"/>; <c>null</c> = unbekannt.
        /// </summary>
        public string m_Herkunft;

        /// <summary>„Gehört zur Auslieferung" — nur im Katalog belegt.</summary>
        public bool m_bReadOnly;

        public WechselrichterModel()
        {
            m_ID = 0;
            m_ID_Projekt = 0;
            m_szName = "";
            m_szFirma = "";
            m_szBeschreibung = "";
            m_Herkunft = null;   // null = unbekannt (siehe Feldkommentar)
            m_bReadOnly = false;
        }

        /// <summary>
        /// Eine flache Kopie der Fachwerte — der gemeinsame Weg von
        /// <c>WechselrichterStammCtrl.CopyFrom</c> und
        /// <c>WechselrichterCtrl.CopyFrom</c>.
        /// </summary>
        /// <remarks>
        /// <c>ID</c>, <c>ID_Projekt</c> und <c>ReadOnly</c> bleiben ABSICHTLICH außen
        /// vor: Sie gehören der Zeile, nicht dem Gerät. Genau daran ist bei
        /// <c>PhotovoltaikStammCtrl.CopyFrom</c> nichts anders.
        /// </remarks>
        public void UebernimmVon(WechselrichterModel m)
        {
            if (m == null) return;

            m_szName = m.m_szName;
            m_szFirma = m.m_szFirma;
            m_szBeschreibung = m.m_szBeschreibung;
            m_P_AC_Nenn = m.m_P_AC_Nenn;
            m_S_AC_Max = m.m_S_AC_Max;
            m_P_DC_Max = m.m_P_DC_Max;
            m_U_Mpp_Min = m.m_U_Mpp_Min;
            m_U_Mpp_Max = m.m_U_Mpp_Max;
            m_U_Dc_Max = m.m_U_Dc_Max;
            m_U_Start = m.m_U_Start;
            m_I_Dc_Max = m.m_I_Dc_Max;
            m_Anzahl_Mppt = m.m_Anzahl_Mppt;
            m_Straenge_Je_Mppt = m.m_Straenge_Je_Mppt;
            m_Eta05 = m.m_Eta05;
            m_Eta10 = m.m_Eta10;
            m_Eta20 = m.m_Eta20;
            m_Eta30 = m.m_Eta30;
            m_Eta50 = m.m_Eta50;
            m_Eta100 = m.m_Eta100;
            m_Eta_Euro = m.m_Eta_Euro;
            m_Eta_Max = m.m_Eta_Max;
            m_P_Standby = m.m_P_Standby;
            m_P_Nacht = m.m_P_Nacht;
            m_Kosten = m.m_Kosten;
            m_Sandia_Pdco = m.m_Sandia_Pdco;
            m_Sandia_Vdco = m.m_Sandia_Vdco;
            m_Sandia_Pso = m.m_Sandia_Pso;
            m_Sandia_C0 = m.m_Sandia_C0;
            m_Sandia_C1 = m.m_Sandia_C1;
            m_Sandia_C2 = m.m_Sandia_C2;
            m_Sandia_C3 = m.m_Sandia_C3;
            m_Herkunft = m.m_Herkunft;
        }
    }
}

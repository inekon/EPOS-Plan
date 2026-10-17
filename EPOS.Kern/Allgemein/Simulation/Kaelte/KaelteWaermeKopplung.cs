using System;

namespace WindowsFormsApplication1
{
    /// <summary>Die Führung der Kälte-Wärme-Kopplung (Kühlkonzept 5.6.4, KWN‑Q3).</summary>
    public enum KopplungsFuehrung
    {
        /// <summary>Vorgabe: gekoppelt wird, was beide Seiten abnehmen; der Rest geht in Einzelbetrieb und Kaskaden.</summary>
        Schnittmenge = 0,

        /// <summary>Die Kälte führt; Abwärme über dem Wärmebedarf ist Überschuss an die Rückkühlung.</summary>
        Kaeltegefuehrt = 1,

        /// <summary>Die Wärme führt; Kälte über dem Kältebedarf ist Überschuss an die Umweltquelle (nur Wärmepumpe).</summary>
        Waermegefuehrt = 2
    }

    /// <summary>Der Eingang einer Stunde der Kälte-Wärme-Kopplung: Bedarf und Temperaturpaar beider gekoppelten Senken.</summary>
    public readonly struct KopplungsEingabe
    {
        /// <summary>Kältebedarf der gekoppelten Kältesenke [kWh].</summary>
        public readonly double KaeltebedarfKwh;

        /// <summary>Kaltwasservorlauf der Kältesenke [°C] — die Kaltwasserachse der Kennlinie.</summary>
        public readonly double KaltwasserVorlaufC;

        /// <summary>Kaltwasserrücklauf der Kältesenke [°C]; in KWN‑1a ohne Wirkung auf die Rechnung (Kennzahlen, Probe).</summary>
        public readonly double KaltwasserRuecklaufC;

        /// <summary>Wärmebedarf der gekoppelten Wärmesenke [kWh].</summary>
        public readonly double WaermebedarfKwh;

        /// <summary>Vorlauf der Wärmesenke [°C] — Kondensatortemperatur = Vorlauf + Grädigkeit.</summary>
        public readonly double WaermeVorlaufC;

        /// <summary>Rücklauf der Wärmesenke [°C]; in KWN‑1a ohne Wirkung auf die Rechnung (Kennzahlen, Probe).</summary>
        public readonly double WaermeRuecklaufC;

        /// <summary>Rückkühltemperatur der Stunde [°C] für den Restbetrieb; NaN = kein Restbetrieb in der Kopplung.</summary>
        public readonly double RueckkuehlC;

        /// <summary>Legt den Eingang an.</summary>
        public KopplungsEingabe(double kaeltebedarfKwh, double kaltwasserVorlaufC, double kaltwasserRuecklaufC,
                                double waermebedarfKwh, double waermeVorlaufC, double waermeRuecklaufC, double rueckkuehlC)
        {
            KaeltebedarfKwh = kaeltebedarfKwh;
            KaltwasserVorlaufC = kaltwasserVorlaufC;
            KaltwasserRuecklaufC = kaltwasserRuecklaufC;
            WaermebedarfKwh = waermebedarfKwh;
            WaermeVorlaufC = waermeVorlaufC;
            WaermeRuecklaufC = waermeRuecklaufC;
            RueckkuehlC = rueckkuehlC;
        }
    }

    /// <summary>
    /// Die reine Aufteilung einer Stunde nach der Führung bei fester Leistungszahl (Kühlkonzept 5.6.4) — für Kältemaschine
    /// und Wärmepumpe gleich; die Bilanz <c>Q_W = Q_K + P_el</c> gilt mit der erzeugten Wärme und Kälte genau.
    /// </summary>
    public readonly struct Kopplungsaufteilung
    {
        /// <summary>Erzeugte Kälte am Verdampfer [kWh].</summary>
        public double KaelteErzeugtKwh { get; init; }

        /// <summary>Davon von der Kältesenke abgenommen [kWh].</summary>
        public double KaelteGenutztKwh { get; init; }

        /// <summary>Erzeugte Wärme am Kondensator [kWh].</summary>
        public double WaermeErzeugtKwh { get; init; }

        /// <summary>Davon von der Wärmesenke abgenommen [kWh].</summary>
        public double WaermeGenutztKwh { get; init; }

        /// <summary>Strom des Verdichters [kWh].</summary>
        public double StromKwh { get; init; }

        /// <summary>Kälte über dem Kältebedarf [kWh] (nur wärmegeführt).</summary>
        public double UeberschussKaelteKwh { get; init; }

        /// <summary>Wärme über dem Wärmebedarf [kWh] (nur kältegeführt).</summary>
        public double UeberschussWaermeKwh { get; init; }

        /// <summary><c>true</c> = die Kapazität hat die Kopplung begrenzt.</summary>
        public bool AnGrenze { get; init; }
    }

    /// <summary>Das Ergebnis einer Stunde der Kälte-Wärme-Kopplung an einer Kältemaschine.</summary>
    public readonly struct KopplungsStunde
    {
        /// <summary>Gekoppelte, von der Kältesenke genutzte Kälte Q_K [kWh].</summary>
        public double KaelteGekoppeltKwh { get; init; }

        /// <summary>Von der Wärmesenke genutzte Kondensatorwärme Q_W [kWh].</summary>
        public double WaermeGenutztKwh { get; init; }

        /// <summary>Verdichterstrom des Kopplungsanteils P_el [kWh] — einmal gebucht.</summary>
        public double StromGekoppeltKwh { get; init; }

        /// <summary>Wärmeverlust des Geräts Q_V [kWh] — Festlegung 0 (die Kennlinie enthält ihn, Kühlkonzept 5.6.3).</summary>
        public double WaermeverlustKwh { get; init; }

        /// <summary>Abwärme über dem Wärmebedarf an die Rückkühlung [kWh] (kältegeführt).</summary>
        public double UeberschussWaermeKwh { get; init; }

        /// <summary>Kälte über dem Kältebedarf [kWh]; an der Kältemaschine stets 0.</summary>
        public double UeberschussKaelteKwh { get; init; }

        /// <summary>Hilfsstrom der Rückkühlung für die Überschusswärme [kWh] — außerhalb der Kopplungsbilanz.</summary>
        public double HilfsstromUeberschussKwh { get; init; }

        /// <summary>Anteil des Stroms für die Wärme [kWh]: P_el · Q_W / (Q_W + Q_K) (nach Nutzenergie, KWN‑Q4).</summary>
        public double StromAnteilWaermeKwh { get; init; }

        /// <summary>Anteil des Stroms für die Kälte [kWh]: P_el · Q_K / (Q_W + Q_K); mit dem Wärmeanteil genau P_el.</summary>
        public double StromAnteilKaelteKwh { get; init; }

        /// <summary>Kälte des Restbetriebs im Einzelbetrieb mit Rückkühlung [kWh].</summary>
        public double KaelteEinzelKwh { get; init; }

        /// <summary>Strom des Restbetriebs [kWh] — Verdichter und Hilfsstrom der Rückkühlung.</summary>
        public double StromEinzelKwh { get; init; }

        /// <summary>Kältebedarf, den weder Kopplung noch Restbetrieb deckt [kWh] — an die übrige Kältefolge.</summary>
        public double RestkaelteKwh { get; init; }

        /// <summary>Wärmebedarf, den die Kopplung nicht deckt [kWh] — an die übrigen Wärmeerzeuger.</summary>
        public double RestwaermeKwh { get; init; }

        /// <summary>Zeitanteil der Kopplung τ_kop = Q_K / Kapazität im Kopplungspunkt [—].</summary>
        public double ZeitanteilKopplung { get; init; }

        /// <summary>Zeitanteil des Restbetriebs [—]; mit <see cref="ZeitanteilKopplung"/> höchstens 1.</summary>
        public double ZeitanteilEinzel { get; init; }

        /// <summary>Kälteleistung der Anlagenzeile im Kopplungspunkt [kW].</summary>
        public double KapazitaetKw { get; init; }

        /// <summary>EER des Kopplungsanteils [—] (Q_K / P_el); 0 ohne Kopplung.</summary>
        public double Eer { get; init; }

        /// <summary>Kondensatoraustritt [°C] = Vorlauf der Wärmesenke + Grädigkeit — Bezug der Hubgrenze.</summary>
        public double KondensatorC { get; init; }

        /// <summary>
        /// Kondensatoreintritt [°C] = max(Rücklauf der Wärmesenke; Vorlauf − <see cref="KaelteFestwerte.KOPPLUNG_KONDENSATOR_SPREIZUNG_K"/>)
        /// + Grädigkeit — der Lesepunkt der Rückkühlachse der Kennlinie.
        /// </summary>
        public double KondensatorEintrittC { get; init; }

        /// <summary>Hub [K] = Kondensatoraustritt − Kaltwasservorlauf.</summary>
        public double HubK { get; init; }

        /// <summary>Grund ohne Kopplung (<c>KaelteWaermeKopplung.GRUND_*</c>); <c>null</c> = gekoppelt.</summary>
        public string Grund { get; init; }

        /// <summary>Die Kennlinie wurde im Kopplungspunkt am Rand gelesen.</summary>
        public bool Randwert { get; init; }

        /// <summary>Der EER wurde mit dem Gütegrad über den Kennfeldrand fortgesetzt (KM3).</summary>
        public bool Extrapoliert { get; init; }

        /// <summary>Der Kopplungsanteil lag unter der Mindestteillast; die Maschine taktet.</summary>
        public bool Takt { get; init; }

        /// <summary>Mehrstrom des Taktens im Kopplungsanteil [kWh] (KM3); Teil von <see cref="StromGekoppeltKwh"/>.</summary>
        public double MehrstromKwh { get; init; }

        /// <summary>Die Kapazität im Kopplungspunkt hat die Kopplung begrenzt.</summary>
        public bool AnGrenze { get; init; }

        /// <summary>Schnittmenge: der Teillast-EER lag unter dem Volllast-EER; der Anteil wurde einmal auf den Wärmebedarf gesenkt.</summary>
        public bool Korrigiert { get; init; }

        /// <summary>Lief die Kopplung in dieser Stunde?</summary>
        public bool Gekoppelt => Grund == null && KaelteGekoppeltKwh > 0;

        /// <summary>Gesamte Kondensatorwärme des Kopplungsanteils [kWh] — genutzt und Überschuss.</summary>
        public double WaermeKondensatorKwh => WaermeGenutztKwh + UeberschussWaermeKwh;

        /// <summary>Gesamtleistungszahl (Q_W + Q_K) / P_el der genutzten Energien [—]; 0 ohne Kopplung.</summary>
        public double Gesamtleistungszahl =>
            StromGekoppeltKwh > 0 ? (WaermeGenutztKwh + KaelteGekoppeltKwh) / StromGekoppeltKwh : 0.0;
    }

    /// <summary>Ein Jahr der Kälte-Wärme-Kopplung: Stundenreihen [kWh], Jahressummen [MWh], Stunden.</summary>
    public sealed class KopplungsJahr
    {
        /// <summary>Gekoppelte Kälte je Stunde [kWh].</summary>
        public double[] KaelteGekoppeltKwh;

        /// <summary>Genutzte Wärme je Stunde [kWh].</summary>
        public double[] WaermeGenutztKwh;

        /// <summary>Strom des Kopplungsanteils je Stunde [kWh].</summary>
        public double[] StromGekoppeltKwh;

        /// <summary>Überschusswärme je Stunde [kWh].</summary>
        public double[] UeberschussWaermeKwh;

        /// <summary>Überschusskälte je Stunde [kWh].</summary>
        public double[] UeberschussKaelteKwh;

        /// <summary>Kälte des Restbetriebs je Stunde [kWh].</summary>
        public double[] KaelteEinzelKwh;

        /// <summary>Strom des Restbetriebs je Stunde [kWh].</summary>
        public double[] StromEinzelKwh;

        /// <summary>Restkälte an die übrige Kältefolge je Stunde [kWh].</summary>
        public double[] RestkaelteKwh;

        /// <summary>Restwärme an die übrigen Wärmeerzeuger je Stunde [kWh].</summary>
        public double[] RestwaermeKwh;

        /// <summary>Grund ohne Kopplung je Stunde; <c>null</c> = gekoppelt.</summary>
        public string[] Grund;

        /// <summary>Genutzte Abwärme im Jahr [MWh].</summary>
        public double WaermeGenutztMwh;

        /// <summary>Gekoppelte Kälte im Jahr [MWh].</summary>
        public double KaelteGekoppeltMwh;

        /// <summary>Strom des Kopplungsanteils im Jahr [MWh].</summary>
        public double StromGekoppeltMwh;

        /// <summary>Überschusswärme im Jahr [MWh].</summary>
        public double UeberschussWaermeMwh;

        /// <summary>Überschusskälte im Jahr [MWh].</summary>
        public double UeberschussKaelteMwh;

        /// <summary>Stunden mit gekoppelter Kälte [h].</summary>
        public int Kopplungsstunden;

        /// <summary>Stunden mit Bedarf auf beiden Seiten, aber ohne Kopplung wegen der Hubgrenze [h].</summary>
        public int Hubstunden;

        /// <summary>Gesamtleistungszahl des Jahres (Σ Q_W + Σ Q_K) / Σ P_el [—]; 0 ohne Kopplung.</summary>
        public double Gesamtleistungszahl =>
            StromGekoppeltMwh > 0 ? (WaermeGenutztMwh + KaelteGekoppeltMwh) / StromGekoppeltMwh : 0.0;
    }

    /// <summary>
    /// <b>Die Kälte-Wärme-Kopplung an einer Kältemaschine</b> (KWN‑1a; Kühlkonzept 5.6.3, 5.6.4, 5.6.8; Entscheid E127).
    /// Rein, ohne Datenbank und ohne Dienste, deterministisch und ohne Iteration.
    ///
    /// <para><b>Temperaturen:</b> Die Rückkühlachse der Kennlinie ist wie im Einzelbetrieb der Kühlwassereintritt in den
    /// Kondensator. Er ist max(Rücklauf der Wärmesenke; Vorlauf − <see cref="KaelteFestwerte.KOPPLUNG_KONDENSATOR_SPREIZUNG_K"/>)
    /// + Grädigkeit (leer <see cref="KaelteFestwerte.KOPPLUNG_GRAEDIGKEIT_K"/>); der Kondensatoraustritt Vorlauf + Grädigkeit
    /// ist Bezug von Hub und Hubgrenze. Der Verdampfer steht am Kaltwasservorlauf der Kältesenke. Gelesen wird über die Wege
    /// der <see cref="Kaeltemaschine"/> (Randwert bzw. Gütegrad, Teillast und Takt nach KM3).</para>
    ///
    /// <para><b>Grenzen:</b> Die Kondensatortemperatur darf <c>Kopplung_Kond_Vorlauf_Max</c> nicht übersteigen, ohne ihn
    /// den Rand der Kennlinie (mit <c>GUETEGRAD</c> um <see cref="KaeltemaschinenRand.EXTRAPOLATIONSWEITE_K"/> darüber);
    /// sonst Grund <see cref="GRUND_HUB"/>. Die Kapazität im Kopplungspunkt begrenzt die Kopplung. Eine luftgekühlte
    /// Maschine koppelt nicht (<see cref="GRUND_LUFTGERAET"/>), wärmegeführt ebenfalls nicht (<see cref="GRUND_FUEHRUNG"/>:
    /// ihr Verdampfer hat keine Umweltquelle für die überschüssige Kälte).</para>
    ///
    /// <para><b>Bilanz</b> je Stunde: Q_W = Q_K + P_el − Q_V mit Q_V = 0. Der Strom wird nach Nutzenergie aufgeteilt
    /// (KWN‑Q4). Der Rest der Kälte läuft im Restzeitanteil 1 − τ_kop im Einzelbetrieb an der Rückkühltemperatur der
    /// Stunde, der Rest danach geht an die übrige Kältefolge, der Rest der Wärme an die übrigen Wärmeerzeuger.</para>
    /// </summary>
    public sealed class KaelteWaermeKopplung
    {
        /// <summary>Grund: eine der beiden Seiten hat keinen Bedarf.</summary>
        public const string GRUND_KEIN_BEDARF = "KEIN_BEDARF";

        /// <summary>Grund: Kondensator über der Einsatzgrenze, Kaltwasser unter der Verdampfergrenze oder kein positiver Hub.</summary>
        public const string GRUND_HUB = "HUB";

        /// <summary>Grund: die Kennlinie liefert im Kopplungspunkt keine Leistung oder keinen EER.</summary>
        public const string GRUND_GRENZE = "GRENZE";

        /// <summary>Grund: luftgekühlte Maschine — keine wasserseitige Gegenseite (Kühlkonzept 5.6.3).</summary>
        public const string GRUND_LUFTGERAET = "LUFTGERAET";

        /// <summary>Grund: wärmegeführt an einer Kältemaschine — die überschüssige Kälte hätte keinen Abnehmer.</summary>
        public const string GRUND_FUEHRUNG = "FUEHRUNG";

        /// <summary>Persistenzwert der Führung Schnittmenge (leer gilt gleich).</summary>
        public const string FUEHRUNG_SCHNITTMENGE = "SCHNITTMENGE";

        /// <summary>Persistenzwert der kältegeführten Kopplung.</summary>
        public const string FUEHRUNG_KAELTE = "KAELTE";

        /// <summary>Persistenzwert der wärmegeführten Kopplung.</summary>
        public const string FUEHRUNG_WAERME = "WAERME";

        /// <summary>Die gekoppelte Kältemaschine (Anlagenzeile samt Anzahl).</summary>
        public Kaeltemaschine Maschine { get; }

        /// <summary>Die Führung.</summary>
        public KopplungsFuehrung Fuehrung { get; }

        /// <summary>Einsatzgrenze des Kondensators [°C]; NaN = Rand der Kennlinie.</summary>
        public double KondensatorVorlaufMaxC { get; }

        /// <summary>Grädigkeit des Übertragers zur Wärmesenke [K].</summary>
        public double GraedigkeitK { get; }

        /// <summary>
        /// Legt die Kopplung an. <paramref name="kondVorlaufMaxC"/> leer = Rand der Kennlinie; <paramref name="graedigkeitK"/>
        /// leer oder negativ = <see cref="KaelteFestwerte.KOPPLUNG_GRAEDIGKEIT_K"/>.
        /// </summary>
        public KaelteWaermeKopplung(Kaeltemaschine maschine, KopplungsFuehrung fuehrung, double? kondVorlaufMaxC,
                                    double? graedigkeitK)
        {
            Maschine = maschine ?? throw new ArgumentNullException(nameof(maschine));
            Fuehrung = fuehrung;
            KondensatorVorlaufMaxC = kondVorlaufMaxC.HasValue && !double.IsNaN(kondVorlaufMaxC.Value)
                ? kondVorlaufMaxC.Value : double.NaN;
            GraedigkeitK = graedigkeitK.HasValue && graedigkeitK.Value >= 0
                ? graedigkeitK.Value : KaelteFestwerte.KOPPLUNG_GRAEDIGKEIT_K;
        }

        /// <summary>
        /// Liest die Führung aus dem Persistenzwert: leer oder <c>SCHNITTMENGE</c> = Schnittmenge, <c>KAELTE</c> bzw.
        /// <c>KAELTEGEFUEHRT</c>, <c>WAERME</c> bzw. <c>WAERMEGEFUEHRT</c>; ein unbekannter Wert gilt als Schnittmenge und
        /// setzt <paramref name="unbekannt"/>.
        /// </summary>
        public static KopplungsFuehrung FuehrungLesen(string wert, out bool unbekannt)
        {
            unbekannt = false;
            string w = string.IsNullOrWhiteSpace(wert) ? "" : wert.Trim().ToUpperInvariant();
            switch (w)
            {
                case "":
                case FUEHRUNG_SCHNITTMENGE:
                    return KopplungsFuehrung.Schnittmenge;
                case FUEHRUNG_KAELTE:
                case "KAELTEGEFUEHRT":
                    return KopplungsFuehrung.Kaeltegefuehrt;
                case FUEHRUNG_WAERME:
                case "WAERMEGEFUEHRT":
                    return KopplungsFuehrung.Waermegefuehrt;
                default:
                    unbekannt = true;
                    return KopplungsFuehrung.Schnittmenge;
            }
        }

        /// <summary>
        /// Die reine Aufteilung nach der Führung bei fester Leistungszahl <paramref name="eer"/> und Kapazität
        /// <paramref name="kapazitaetKaelteKw"/> (Kühlkonzept 5.6.4): Schnittmenge
        /// <c>Q_K = min(Q_K,Bedarf; Q_W,Bedarf · EER/(EER+1); Kapazität)</c> ohne Überschuss; kältegeführt
        /// <c>Q_K = min(Q_K,Bedarf; Kapazität)</c>, Wärmeüberschuss; wärmegeführt
        /// <c>Q_W = min(Q_W,Bedarf; Kapazität · (1 + 1/EER))</c>, Kälteüberschuss.
        /// </summary>
        public static Kopplungsaufteilung Aufteilen(KopplungsFuehrung fuehrung, double kaeltebedarfKwh, double waermebedarfKwh,
                                                    double eer, double kapazitaetKaelteKw)
        {
            double kBed = Positiv(kaeltebedarfKwh), wBed = Positiv(waermebedarfKwh), kap = Positiv(kapazitaetKaelteKw);
            if (!(eer > 0) || !(kap > 0)) return default;
            double faktor = eer / (eer + 1.0);
            switch (fuehrung)
            {
                case KopplungsFuehrung.Kaeltegefuehrt:
                {
                    double q = Math.Min(kBed, kap);
                    double p = q / eer;
                    double w = q + p;
                    double wGen = Math.Min(w, wBed);
                    return new Kopplungsaufteilung
                    {
                        KaelteErzeugtKwh = q, KaelteGenutztKwh = q, WaermeErzeugtKwh = w, WaermeGenutztKwh = wGen,
                        StromKwh = p, UeberschussWaermeKwh = w - wGen, AnGrenze = Rechenrand.SchwelleErreicht(kBed, kap)
                    };
                }
                case KopplungsFuehrung.Waermegefuehrt:
                {
                    double wKap = kap + kap / eer;
                    double w = Math.Min(wBed, wKap);
                    double q = w * faktor;
                    double kGen = Math.Min(q, kBed);
                    return new Kopplungsaufteilung
                    {
                        KaelteErzeugtKwh = q, KaelteGenutztKwh = kGen, WaermeErzeugtKwh = w, WaermeGenutztKwh = w,
                        StromKwh = w - q, UeberschussKaelteKwh = q - kGen, AnGrenze = Rechenrand.SchwelleErreicht(wBed, wKap)
                    };
                }
                default:
                {
                    double kAusW = wBed * faktor;
                    double q = Math.Min(kBed, Math.Min(kAusW, kap));
                    // Bindet die Wärmeseite, ist die Wärme genau der Bedarf; der Strom schließt die Bilanz.
                    bool waermeBindet = kAusW <= kBed && kAusW <= kap;
                    double w = waermeBindet ? wBed : q + q / eer;
                    return new Kopplungsaufteilung
                    {
                        KaelteErzeugtKwh = q, KaelteGenutztKwh = q, WaermeErzeugtKwh = w, WaermeGenutztKwh = w,
                        StromKwh = w - q, AnGrenze = Rechenrand.SchwelleErreicht(Math.Min(kBed, kAusW), kap)
                    };
                }
            }
        }

        /// <summary>Rechnet eine Stunde der Kopplung samt Restbetrieb.</summary>
        public KopplungsStunde Stunde(in KopplungsEingabe e)
        {
            double kBed = Positiv(e.KaeltebedarfKwh), wBed = Positiv(e.WaermebedarfKwh);
            double tVerd = e.KaltwasserVorlaufC;
            double tKond = e.WaermeVorlaufC + GraedigkeitK;
            double hub = tKond - tVerd;
            double tEin = KondensatoreintrittC(e.WaermeVorlaufC, e.WaermeRuecklaufC);

            KaeltemaschinenRandpunkt p = default;
            string grund = Pruefen(kBed, wBed, tVerd, tKond, tEin, ref p);
            int anzahl = Math.Max(1, Maschine.Anzahl);
            double kap = p.LeistungKw * anzahl;

            double qK = 0.0, pel = 0.0, wGen = 0.0, ueberW = 0.0, hilfsUeber = 0.0, mehr = 0.0;
            bool takt = false, anGrenze = false, korrigiert = false;
            if (grund == null)
            {
                Kopplungsaufteilung a = Aufteilen(Fuehrung, kBed, wBed, p.Eer, kap);
                anGrenze = a.AnGrenze;
                KaeltemaschinenStunde s = Maschine.VerdichterstundeBei(a.KaelteErzeugtKwh, tVerd, tEin);
                if (!(s.KaelteKwh > 0) || !(s.VerdichterKwh > 0))
                {
                    grund = GRUND_GRENZE;
                }
                else
                {
                    qK = s.KaelteKwh;
                    pel = s.VerdichterKwh;
                    takt = s.Takt;
                    mehr = s.MehrstromKwh;
                    double w = qK + pel;
                    if (Fuehrung == KopplungsFuehrung.Kaeltegefuehrt)
                    {
                        wGen = Math.Min(w, wBed);
                        ueberW = w - wGen;
                        if (ueberW > 0) hilfsUeber = s.HilfsstromKwh * ueberW / w;
                    }
                    else
                    {
                        // Schnittmenge: liegt der Teillast-EER unter dem Volllast-EER, übersteigt die Abwärme den
                        // Wärmebedarf. Genau ein Schritt senkt den Anteil bei der Leistungszahl der Stunde — keine Iteration.
                        if (!Rechenrand.SchwelleErreicht(wBed, w))
                        {
                            double eerS = qK / pel;
                            double qVorher = qK;
                            qK = wBed * eerS / (eerS + 1.0);
                            pel = wBed - qK;
                            mehr *= qK / qVorher;
                            korrigiert = true;
                        }
                        wGen = qK + pel;
                    }
                }
            }

            double tauKop = grund == null && kap > 0 ? Math.Min(1.0, qK / kap) : 0.0;
            if (grund != null) { qK = 0.0; pel = 0.0; wGen = 0.0; ueberW = 0.0; hilfsUeber = 0.0; mehr = 0.0; takt = false; }

            // Restbetrieb: die offene Kälte im Restzeitanteil mit Rückkühlung an der Rückkühltemperatur der Stunde.
            double kOffen = Rest(kBed - qK);
            double kEinzel = 0.0, pEinzel = 0.0, tauEinzel = 0.0;
            if (kOffen > 0 && !double.IsNaN(e.RueckkuehlC) && !double.IsInfinity(e.RueckkuehlC) && 1.0 - tauKop > 0)
            {
                double kw = Maschine.KaltwasserAmVorlauf(tVerd);
                double kapE = Maschine.PunktBei(e.RueckkuehlC, kw).LeistungKw * anzahl;
                if (kapE > 0)
                {
                    double last = Math.Min(kOffen, (1.0 - tauKop) * kapE);
                    KaeltemaschinenStunde sE = Maschine.VerdichterstundeBei(last, kw, e.RueckkuehlC);
                    kEinzel = sE.KaelteKwh;
                    pEinzel = sE.StromKwh;
                    tauEinzel = Math.Min(1.0 - tauKop, kEinzel / kapE);
                }
            }

            double summe = wGen + qK;
            double pW = summe > 0 ? pel * wGen / summe : 0.0;
            return new KopplungsStunde
            {
                KaelteGekoppeltKwh = qK,
                WaermeGenutztKwh = wGen,
                StromGekoppeltKwh = pel,
                WaermeverlustKwh = 0.0,
                UeberschussWaermeKwh = ueberW,
                UeberschussKaelteKwh = 0.0,
                HilfsstromUeberschussKwh = hilfsUeber,
                StromAnteilWaermeKwh = pW,
                StromAnteilKaelteKwh = pel - pW,
                KaelteEinzelKwh = kEinzel,
                StromEinzelKwh = pEinzel,
                RestkaelteKwh = Rest(kOffen - kEinzel),
                RestwaermeKwh = Rest(wBed - wGen),
                ZeitanteilKopplung = tauKop,
                ZeitanteilEinzel = tauEinzel,
                KapazitaetKw = kap,
                Eer = pel > 0 ? qK / pel : 0.0,
                KondensatorC = tKond,
                KondensatorEintrittC = tEin,
                HubK = hub,
                Grund = grund,
                Randwert = p.Randwert,
                Extrapoliert = p.Extrapoliert,
                Takt = takt,
                MehrstromKwh = mehr,
                AnGrenze = grund == null && anGrenze,
                Korrigiert = korrigiert
            };
        }

        /// <summary>
        /// Rechnet ein Jahr aus Stundenreihen. Die Temperaturreihen dürfen kürzer sein oder fehlen: Eine fehlende
        /// Rückkühltemperatur heißt kein Restbetrieb, eine fehlende Vorlauftemperatur keine Kopplung (Grund
        /// <see cref="GRUND_HUB"/>). Die Länge gibt der Kältebedarf vor.
        /// </summary>
        public KopplungsJahr Jahr(double[] kaeltebedarfKwh, double[] kaltwasserVorlaufC, double[] kaltwasserRuecklaufC,
                                  double[] waermebedarfKwh, double[] waermeVorlaufC, double[] waermeRuecklaufC,
                                  double[] rueckkuehlC)
        {
            int n = kaeltebedarfKwh != null ? kaeltebedarfKwh.Length : 0;
            var j = new KopplungsJahr
            {
                KaelteGekoppeltKwh = new double[n], WaermeGenutztKwh = new double[n], StromGekoppeltKwh = new double[n],
                UeberschussWaermeKwh = new double[n], UeberschussKaelteKwh = new double[n], KaelteEinzelKwh = new double[n],
                StromEinzelKwh = new double[n], RestkaelteKwh = new double[n], RestwaermeKwh = new double[n],
                Grund = new string[n]
            };
            double sW = 0, sK = 0, sP = 0, sUW = 0, sUK = 0;
            for (int h = 0; h < n; h++)
            {
                var e = new KopplungsEingabe(kaeltebedarfKwh[h], Wert(kaltwasserVorlaufC, h), Wert(kaltwasserRuecklaufC, h),
                                             Wert(waermebedarfKwh, h, 0.0), Wert(waermeVorlaufC, h), Wert(waermeRuecklaufC, h),
                                             Wert(rueckkuehlC, h));
                KopplungsStunde s = Stunde(e);
                j.KaelteGekoppeltKwh[h] = s.KaelteGekoppeltKwh;
                j.WaermeGenutztKwh[h] = s.WaermeGenutztKwh;
                j.StromGekoppeltKwh[h] = s.StromGekoppeltKwh;
                j.UeberschussWaermeKwh[h] = s.UeberschussWaermeKwh;
                j.UeberschussKaelteKwh[h] = s.UeberschussKaelteKwh;
                j.KaelteEinzelKwh[h] = s.KaelteEinzelKwh;
                j.StromEinzelKwh[h] = s.StromEinzelKwh;
                j.RestkaelteKwh[h] = s.RestkaelteKwh;
                j.RestwaermeKwh[h] = s.RestwaermeKwh;
                j.Grund[h] = s.Grund;
                sW += s.WaermeGenutztKwh;
                sK += s.KaelteGekoppeltKwh;
                sP += s.StromGekoppeltKwh;
                sUW += s.UeberschussWaermeKwh;
                sUK += s.UeberschussKaelteKwh;
                if (s.Gekoppelt) j.Kopplungsstunden++;
                if (s.Grund == GRUND_HUB) j.Hubstunden++;
            }
            j.WaermeGenutztMwh = sW / 1000.0;
            j.KaelteGekoppeltMwh = sK / 1000.0;
            j.StromGekoppeltMwh = sP / 1000.0;
            j.UeberschussWaermeMwh = sUW / 1000.0;
            j.UeberschussKaelteMwh = sUK / 1000.0;
            return j;
        }

        /// <summary>
        /// Prüft die Stunde in fester Reihenfolge: Luftgerät, Führung, Bedarf, Hub; danach den Kennlinienpunkt
        /// <paramref name="p"/> (Grenze). <c>null</c> = die Stunde koppelt.
        /// </summary>
        private string Pruefen(double kBed, double wBed, double tVerd, double tKond, double tEin, ref KaeltemaschinenRandpunkt p)
        {
            if (Maschine.Rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_LUFT) return GRUND_LUFTGERAET;
            if (Fuehrung == KopplungsFuehrung.Waermegefuehrt) return GRUND_FUEHRUNG;
            if (!(kBed > Rechenrand.ABSOLUT) || !(wBed > Rechenrand.ABSOLUT)) return GRUND_KEIN_BEDARF;
            if (double.IsNaN(tVerd) || double.IsInfinity(tVerd) || double.IsNaN(tKond) || double.IsInfinity(tKond))
                return GRUND_HUB;
            if (!(tKond > tVerd)) return GRUND_HUB;
            if (!double.IsNaN(Maschine.KaltwasserVorlaufMinC) && !Rechenrand.SchwelleErreicht(tVerd, Maschine.KaltwasserVorlaufMinC))
                return GRUND_HUB;
            KaeltemaschinenKennlinie k = Maschine.Kennlinie;
            if (k == null || k.Leer) return GRUND_GRENZE;
            // Die Einsatzgrenze gilt am Austritt, der Rand der Kennlinie an ihrem Lesepunkt, dem Eintritt.
            if (!double.IsNaN(KondensatorVorlaufMaxC) && !Rechenrand.SchwelleErreicht(KondensatorVorlaufMaxC, tKond))
                return GRUND_HUB;
            double rand = k.RueckkuehlMax + (Maschine.GuetegradWirksam ? KaeltemaschinenRand.EXTRAPOLATIONSWEITE_K : 0.0);
            if (double.IsNaN(KondensatorVorlaufMaxC) && !Rechenrand.SchwelleErreicht(rand, tEin)) return GRUND_HUB;
            p = Maschine.PunktBei(tEin, tVerd);
            if (!(p.LeistungKw > 0) || !(p.Eer > 0)) return GRUND_GRENZE;
            return null;
        }

        /// <summary>
        /// Der Kondensatoreintritt [°C]: max(Rücklauf; Vorlauf − Δθ_Kond) + Grädigkeit — ein fehlender Rücklauf zählt als
        /// Vorlauf − Δθ_Kond. So liegt der Austritt (Eintritt + Δθ_Kond) nie unter Vorlauf + Grädigkeit.
        /// </summary>
        internal double KondensatoreintrittC(double vorlaufC, double ruecklaufC)
        {
            double ein = vorlaufC - KaelteFestwerte.KOPPLUNG_KONDENSATOR_SPREIZUNG_K;
            if (!double.IsNaN(ruecklaufC) && !double.IsInfinity(ruecklaufC) && ruecklaufC > ein) ein = ruecklaufC;
            return ein + GraedigkeitK;
        }

        private static double Positiv(double x) => x > 0 && !double.IsInfinity(x) ? x : 0.0;

        private static double Rest(double x) => x > Rechenrand.ABSOLUT ? x : 0.0;

        private static double Wert(double[] r, int h, double ohne = double.NaN) =>
            r != null && h < r.Length ? r[h] : ohne;
    }
}

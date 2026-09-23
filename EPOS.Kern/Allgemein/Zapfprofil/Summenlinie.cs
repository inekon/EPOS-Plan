using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Zeitfenster des Tages [h]: Beginn 0 ≤ t_B &lt; 24, Länge 0 ≤ t_F ≤ 24; ein Fenster über
    /// Mitternacht läuft am Tagesanfang weiter. Belegung je Stunde bzw. Minute als Anteil 0 … 1
    /// (Überlappung des Intervalls mit dem Fenster).
    /// </summary>
    internal readonly record struct Tagesfenster(double BeginnH, double LaengeH)
    {
        /// <summary>Das leere Fenster.</summary>
        internal static Tagesfenster Leer => new Tagesfenster(0.0, 0.0);

        /// <summary>Beginn und Länge im Bereich, sonst benannte Ablehnung.</summary>
        internal Tagesfenster Geprueft(string was)
        {
            if (double.IsNaN(BeginnH) || BeginnH < 0 || BeginnH >= Zapfkalender.STUNDEN_TAG
                || double.IsNaN(LaengeH) || LaengeH < 0 || LaengeH > Zapfkalender.STUNDEN_TAG)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — " + was + " liegt nicht im Tag (Beginn 0 … 24 h, Länge 0 … 24 h).");
            return this;
        }

        /// <summary>Belegung der Stunde h (0 … 23) als Anteil der Stunde.</summary>
        internal double AnteilStunde(int h) => Ueberlappung(h, h + 1, BeginnH, LaengeH, Zapfkalender.STUNDEN_TAG);

        /// <summary>Belegung der Minute i (0 … 1439) als Anteil der Minute.</summary>
        internal double AnteilMinute(int i)
            => Ueberlappung(i, i + 1, BeginnH * Bedarfstag.MINUTEN_JE_STUNDE, LaengeH * Bedarfstag.MINUTEN_JE_STUNDE,
                            Bedarfstag.MINUTEN);

        private static double Ueberlappung(double a, double b, double beginn, double laenge, double periode)
            => Stueck(a, b, beginn, laenge) + Stueck(a, b, beginn - periode, laenge);

        private static double Stueck(double a, double b, double s, double laenge)
        {
            double von = a > s ? a : s;
            double ende = s + laenge;
            double bis = b < ende ? b : ende;
            return bis > von ? bis - von : 0.0;
        }
    }

    /// <summary>Die Zirkulation als Last des Speichers: Leistung [kW] in den Laufzeitstunden des Tages (4.3).</summary>
    internal sealed record Zirkulationslast(double LeistungKw, Tagesfenster Laufzeit)
    {
        /// <summary>Keine Zirkulation.</summary>
        internal static Zirkulationslast Keine => new Zirkulationslast(0.0, Tagesfenster.Leer);
    }

    /// <summary>
    /// Der Übertrager des Speichers (4.5 a): Leistung, U·A oder Fläche aus dem Projekt; sonst U
    /// und die Schätzformel der Fläche <c>A_HE = Steigung · V + Achsabschnitt</c> aus dem Katalog.
    /// Die Übertemperatur Δθ_Ü gilt für U·A, Fläche und Schätzformel.
    /// </summary>
    internal sealed record Uebertrager(double? LeistungKw, double? UaWJeK, double? FlaecheM2, double? UWJeM2K,
                                       double? UebertemperaturK, double? FlaecheSteigungM2JeL,
                                       double? FlaecheAchsabschnittM2)
    {
        /// <summary>Der Wärmeübertrag U·A [W/K] beim Volumen V; <c>null</c>, wenn nur eine Leistung bekannt ist.</summary>
        internal double? UaBeiWJeK(double volumenL, out bool unplausibel)
        {
            unplausibel = false;
            if (LeistungKw.HasValue) return UaWJeK;
            if (UaWJeK.HasValue) return UaWJeK.Value;
            double a;
            if (FlaecheM2.HasValue) a = FlaecheM2.Value;
            else
            {
                a = FlaecheSteigungM2JeL.Value * volumenL + FlaecheAchsabschnittM2.Value;
                if (!(a > 0))
                {
                    // Plausibilitätswächter (4.5 a): Die Schätzformel wird für kleine Speicher
                    // unplausibel — dann keine Übertragung statt einer negativen Fläche.
                    unplausibel = true;
                    a = 0.0;
                }
            }
            return UWJeM2K.Value * a;
        }

        /// <summary>Die Übertragerleistung Φ_Ü [kW] beim Volumen V: <c>U·A · Δθ_Ü / 1000</c> oder die Projektangabe.</summary>
        internal double LeistungBeiKw(double volumenL, out bool unplausibel)
        {
            unplausibel = false;
            if (LeistungKw.HasValue) return LeistungKw.Value;
            double ua = UaBeiWJeK(volumenL, out unplausibel).Value;
            return ua * UebertemperaturK.Value / Zirkulationskanal.W_JE_KW;
        }
    }

    /// <summary>
    /// Die Größen der Summenlinie (4.5 a), aus Projekt und Parametersatz aufgelöst
    /// (<see cref="Summenlinie.Parameter"/>). Temperaturen in °C, Leistungen in kW, Verzögerung in
    /// Minuten; <see cref="ErzeugerKw"/> <c>null</c> = Φ_N allein aus dem Übertrager.
    /// </summary>
    internal sealed record Summenlinienparameter
    {
        public double KaltwasserAuslegungC { get; init; }
        public double SpeicherC { get; init; }
        public double Ladungsfaktor { get; init; }
        public double SensorhoeheAnteil { get; init; }
        public ZapfSpeicherart Speicherart { get; init; } = ZapfSpeicherart.Ladespeicher;

        /// <summary>Mischwassertemperatur θ_w,draw [°C] — nur beim gemischten Speicher.</summary>
        public double? MischwasserC { get; init; }

        public double VerzoegerungMin { get; init; }
        public double SpeicherverlustKw { get; init; }
        public Zirkulationslast Zirkulation { get; init; } = Zirkulationslast.Keine;
        public double? ErzeugerKw { get; init; }

        /// <summary>Der Übertrager; <c>null</c> = keiner bekannt, Φ_N allein aus dem Erzeuger.</summary>
        public Uebertrager Uebertrager { get; init; }

        /// <summary>
        /// Koeffizient k_τ der Zeitkonstante nach A1 [min·W/kJ] (für c_w in kJ/(kg·K));
        /// <c>null</c> = keine Anzeige.
        /// </summary>
        public double? ZeitkonstanteKoeffizient { get; init; }

        /// <summary>Gilt der Schnellpfad des Vereinfachungsverfahrens?</summary>
        public bool Schnellpfad { get; init; }

        /// <summary>Δθ_Speicher = θ_Speicher − θ_KW,Auslegung [K].</summary>
        internal double SpreizungK => SpeicherC - KaltwasserAuslegungC;
    }

    /// <summary>Der Nachweis eines Wertepaars (V, Φ_N) über den Bedarfstag (4.5 a).</summary>
    internal sealed record Summenliniennachweis(
        double VolumenL,
        double LeistungKw,
        bool Erfuellt,
        double KleinsterAbstandKwh,
        int ErsteVerletzungMinute,
        double LadezeitH,
        double SpeicherMaxKwh,
        double EinschaltpunktKwh,
        double MindestinhaltKwh,
        IReadOnlyList<double> InhaltKwh);

    /// <summary>Wie das kleinste Volumen gefunden wurde.</summary>
    internal enum Summenliniensuche
    {
        /// <summary>Ohne Bedarf und Verlust: V = 0.</summary>
        Trivial = 0,

        /// <summary>Grobes Raster monoton, dann Bisektion.</summary>
        Bisektion = 1,

        /// <summary>Grobes Raster nicht monoton: feiner Rasterlauf, kleinstes V mit Nachweis.</summary>
        Rasterlauf = 2
    }

    /// <summary>Ein Punkt der Summenlinie: kleinstes Volumen [l] zur Leistung Φ_N [kW], Ladezeit [h/d].</summary>
    internal sealed record Summenlinienpunkt(double VolumenL, double LeistungKw, double LadezeitH,
                                             Summenliniensuche Suche, bool UebertragerUnplausibel);

    /// <summary>Das Ergebnis der Summenlinie einer Topologiegruppe Speicher (4.5 a).</summary>
    internal sealed record Summenlinienergebnis
    {
        /// <summary>Der Auslegungspunkt — die einzige Empfehlung der Gruppe.</summary>
        public Summenlinienpunkt Punkt { get; init; }

        /// <summary>Die Wertepaarkurve (eigene Erweiterung des Nachweisverfahrens); leer ohne Parameter.</summary>
        public IReadOnlyList<Summenlinienpunkt> Wertepaare { get; init; } = new Summenlinienpunkt[0];

        /// <summary>Der Nachweis am Auslegungspunkt samt Speicherinhalt je Minute (1441 Werte).</summary>
        public Summenliniennachweis Nachweis { get; init; }

        /// <summary>Die Zeitkonstante τ [min] am Auslegungspunkt — nur informativ; <c>null</c> ohne U·A oder k_τ.</summary>
        public double? ZeitkonstanteMin { get; init; }

        /// <summary>Schnellpfad des Vereinfachungsverfahrens („Schnellauslegung").</summary>
        public bool Schnellpfad { get; init; }

        /// <summary>Vermerk des Entwurfsstands.</summary>
        public string Vermerk { get; init; } = Summenlinie.VERMERK_ENTWURF;

        /// <summary>Hinweise des Verfahrens.</summary>
        public IReadOnlyList<Auslegungshinweis> Hinweise { get; init; } = new Auslegungshinweis[0];
    }

    /// <summary>
    /// <b>Die Summenlinie nach DIN EN 12831-3 mit den Rechenregeln der Entwürfe A100/A1</b>
    /// (Umsetzungskonzept Zapfprofilgenerator 4.5 a; Formeln nach Grundlagen 3, Koeffizienten als
    /// Parameter). Minutenbilanz über den Bedarfstag, ein Nachweis je Wertepaar (V, Φ_N):
    ///
    /// <code>
    /// Φ_Ü      = U·A · Δθ_Ü / 1000 (oder Projektangabe);  Φ_N = min(Φ_Erzeuger, Φ_Ü)          [kW]
    /// Q_max    = V · c_w · Δθ_Speicher · f_l / 1000;  Q_on = Q_max · (1 − h_sensor/h_sto)       [kWh]
    /// Q_min    = 0 (Ladespeicher);  V · c_w · (1 − h_sensor/(2 h_sto)) · (θ_draw − θ_KW,A) · f_l / 1000 (gemischt)
    /// Φ_V(i)   = Φ_Speicher + P_zirk · Laufzeitanteil(i)
    /// Q(0)     = Q_max, Erzeuger aus
    /// i = 0 … 1439: ein, sobald Q(i) ≤ Q_on (t_on = i); aus, sobald Q(i) ≥ Q_max
    ///   Φ_eff  = Φ_N − Φ_V(i), wenn ein und i − t_on ≥ t_lag; sonst −Φ_V(i)   (darf negativ sein)
    ///   Q(i+1) = min(Q_max, Q(i) − q(i) + Φ_eff / 60)                                     (NA.3)
    /// Nachweis: Q(i) − q(i) ≥ Q_min für alle i;  Ladezeit = Minuten „ein" / 60            [h/d]
    /// τ        = m · c_w / (U·A) · k_τ = V · c_w · 3,6 / (U·A) · k_τ           [min], nur informativ
    ///            (A1: c_w in kJ/(kg·K), m = V bei 1 kg/l; hier c_w in Wh/(l·K), 1 Wh = 3,6 kJ)
    /// </code>
    ///
    /// <para><b>Kleinstes Volumen.</b> Startwert: das Volumen, das Tagesbedarf und Tagesverlust
    /// fasst; verdoppelt, bis der Nachweis gelingt. Ein grobes Raster über (0; V] prüft die
    /// Monotonie des Nachweises; ist sie gegeben, sucht eine Bisektion zwischen dem letzten
    /// Rasterpunkt ohne und dem ersten mit Nachweis. Sonst nimmt ein feiner Rasterlauf das
    /// kleinste Volumen mit Nachweis, mit Hinweis. Rasterzahlen und Schrittzahl sind numerische
    /// Setzungen des Verfahrens, keine Normzahlen.</para>
    ///
    /// <para><b>Wertepaarkurve</b> — eine eigene Erweiterung des Nachweisverfahrens, als solche
    /// beschriftet: für die Erzeugerleistung auf einem gleichmäßigen Raster bis zur
    /// Erzeugerleistung des Projekts (ohne Erzeuger: bis zur Leistung des Auslegungspunkts) das
    /// kleinste Volumen, jeweils mit <c>Φ_N(V) = min(Φ_Erzeuger, Φ_Ü(V))</c> — jedes Paar ist
    /// mit dem Übertrager baubar, der letzte ist der Auslegungspunkt. Das Ergebnis trägt den
    /// Vermerk des Entwurfsstands.</para>
    /// </summary>
    internal static class Summenlinie
    {
        /// <summary>Vermerk jedes Ergebnisses (A100 und A1 sind Entwürfe).</summary>
        internal const string VERMERK_ENTWURF = "Entwurfsstand, Anwendung besonders zu vereinbaren";

        /// <summary>Beschriftung der Wertepaarkurve.</summary>
        internal const string VERMERK_WERTEPAARKURVE = "Wertepaarkurve: eigene Erweiterung des Nachweisverfahrens";

        /// <summary>Punkte des groben Monotonierasters (numerische Setzung).</summary>
        internal const int RASTER_GROB = 20;

        /// <summary>Punkte des feinen Rasterlaufs (numerische Setzung).</summary>
        internal const int RASTER_FEIN = 400;

        /// <summary>Schritte der Bisektion (numerische Setzung, bis an die Stellenzahl von double).</summary>
        internal const int BISEKTIONSSCHRITTE = 60;

        /// <summary>Höchstzahl der Verdopplungen des Startvolumens.</summary>
        internal const int VERDOPPLUNGEN = 60;

        /// <summary>Kilojoule je Wattstunde (Einheitenumrechnung, keine Normzahl): c_w [Wh/(l·K)] · 3,6 = c_w [kJ/(l·K)].</summary>
        internal const double KJ_JE_WH = 3.6;

        // =================================================================================
        // Parameter
        // =================================================================================

        /// <summary>
        /// Löst die Größen der Summenlinie aus Projekt und Parametersatz auf (4.0, 4.5 a). Pflicht
        /// sind θ_KW,Auslegung, θ_Speicher (Vorgabe W 551), f_l, Sensorhöhe, t_lag und beim
        /// gemischten Speicher θ_draw; der Übertrager braucht aus dem Katalog nur, was das
        /// Projekt nicht nennt. <paramref name="erzeugerRueckfallKw"/> gilt, wenn das Projekt
        /// keine Erzeugerleistung trägt (die Fassade reicht die angesetzte Ladeleistung, 4.7).
        /// </summary>
        internal static Summenlinienparameter Parameter(ProjektStand p, Parametersatz ps, double? erzeugerRueckfallKw,
                                                        Zirkulationslast zirkulation, Herkunftsprotokoll prot,
                                                        ICollection<Auslegungshinweis> hinweise)
        {
            if (p == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — die Projektgrößen der Auslegung fehlen.");
            double kw = ZapfAuslegungParameter.ProjektOderParameter(p.KaltwasserAuslegungC,
                ZapfAuslegungParameter.KALTWASSER_AUSLEGUNG, ps, prot, "Auslegung.KaltwasserC", "°C");
            double speicher = ZapfAuslegungParameter.ProjektOderParameter(p.SpeicherC,
                ZapfAuslegungParameter.W551_MINDESTTEMPERATUR, ps, prot, "Auslegung.SpeicherC", "°C");
            double sensor = ZapfAuslegungParameter.ProjektOderParameter(p.SensorhoeheAnteil,
                ZapfAuslegungParameter.SENSORHOEHE, ps, prot, "Auslegung.Sensorhoehe", "-");
            double? misch = p.Speicherart == ZapfSpeicherart.GemischterSpeicher
                ? ps.Wert(ZapfAuslegungParameter.MISCHWASSERTEMPERATUR) : (double?)null;

            double verlustKw = 0.0;
            if (p.SpeicherverlustW.HasValue)
                verlustKw = Auslegungspruefung.NichtNegativ(p.SpeicherverlustW.Value, "der Speicherverlust")
                            / Zirkulationskanal.W_JE_KW;
            else
                hinweise?.Add(new Auslegungshinweis("SPEICHERVERLUST_NULL",
                    "Ohne Angabe des Bereitschaftsverlusts rechnet die Summenlinie ohne Speicherverlust."));

            Uebertrager ue = null;
            if (p.UebertragerKw.HasValue)
                ue = new Uebertrager(Auslegungspruefung.NichtNegativ(p.UebertragerKw.Value, "die Übertragerleistung"),
                                     p.UebertragerUaWJeK, null, null, null, null, null);
            else if (p.UebertragerUaWJeK.HasValue)
                ue = new Uebertrager(null, Auslegungspruefung.NichtNegativ(p.UebertragerUaWJeK.Value, "U·A des Übertragers"),
                                     null, null, ps.Wert(ZapfAuslegungParameter.UEBERTRAGER_UEBERTEMPERATUR), null, null);
            else if (p.UebertragerFlaecheM2.HasValue)
                ue = new Uebertrager(null, null, Auslegungspruefung.NichtNegativ(p.UebertragerFlaecheM2.Value, "die Übertragerfläche"),
                                     ps.Wert(ZapfAuslegungParameter.UEBERTRAGER_U),
                                     ps.Wert(ZapfAuslegungParameter.UEBERTRAGER_UEBERTEMPERATUR), null, null);
            else
                ue = new Uebertrager(null, null, null, ps.Wert(ZapfAuslegungParameter.UEBERTRAGER_U),
                                     ps.Wert(ZapfAuslegungParameter.UEBERTRAGER_UEBERTEMPERATUR),
                                     ps.Wert(ZapfAuslegungParameter.UEBERTRAGERFLAECHE_STEIGUNG),
                                     ps.Wert(ZapfAuslegungParameter.UEBERTRAGERFLAECHE_ACHSABSCHNITT));

            double? erzeuger = p.ErzeugerKw ?? erzeugerRueckfallKw;
            if (p.ErzeugerKw.HasValue)
                prot?.Vermerken("", "Auslegung.ErzeugerKw", p.ErzeugerKw.Value, "kW", Wertstatus.Ueberschrieben, null);
            else if (erzeugerRueckfallKw.HasValue)
                prot?.Vermerken("", "Auslegung.ErzeugerKw", erzeugerRueckfallKw.Value, "kW", Wertstatus.Vorgabe, null,
                                "angesetzte Ladeleistung der Speicherauslegung");

            return Pruefen(new Summenlinienparameter
            {
                KaltwasserAuslegungC = kw,
                SpeicherC = speicher,
                Ladungsfaktor = ps.Wert(ZapfAuslegungParameter.LADUNGSFAKTOR),
                SensorhoeheAnteil = sensor,
                Speicherart = p.Speicherart,
                MischwasserC = misch,
                VerzoegerungMin = ps.Wert(ZapfAuslegungParameter.VERZOEGERUNG),
                SpeicherverlustKw = verlustKw,
                Zirkulation = zirkulation ?? Zirkulationslast.Keine,
                ErzeugerKw = erzeuger,
                Uebertrager = ue,
                ZeitkonstanteKoeffizient = ZapfAuslegungParameter.Wahlweise(ps, ZapfAuslegungParameter.ZEITKONSTANTE_KOEFFIZIENT,
                    "Die Zeitkonstante des Speichers wird nicht angezeigt.", hinweise)
            });
        }

        /// <summary>Prüft die Größen; jede Verletzung ist eine benannte Ablehnung.</summary>
        internal static Summenlinienparameter Pruefen(Summenlinienparameter p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            Auslegungspruefung.Spreizung(p.SpeicherC, p.KaltwasserAuslegungC, "Speicher − Kaltwasser der Auslegung");
            if (!(p.Ladungsfaktor > 0) || p.Ladungsfaktor > 1)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — der Ladungsfaktor liegt nicht in (0; 1].");
            if (double.IsNaN(p.SensorhoeheAnteil) || p.SensorhoeheAnteil < 0 || p.SensorhoeheAnteil > 1)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.GroesseUngueltig,
                    "Nicht rechenbar — die Sensorhöhe liegt nicht in [0; 1].");
            if (p.Speicherart == ZapfSpeicherart.GemischterSpeicher)
            {
                if (!p.MischwasserC.HasValue)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.TemperaturUngueltig,
                        "Nicht rechenbar — der gemischte Speicher braucht die Mischwassertemperatur.");
                Auslegungspruefung.Spreizung(p.MischwasserC.Value, p.KaltwasserAuslegungC, "Mischwasser − Kaltwasser der Auslegung");
            }
            Auslegungspruefung.NichtNegativ(p.VerzoegerungMin, "die Verzögerung des Erzeugers");
            Auslegungspruefung.NichtNegativ(p.SpeicherverlustKw, "der Speicherverlust");
            Auslegungspruefung.NichtNegativ(p.Zirkulation.LeistungKw, "die Zirkulationsleistung");
            p.Zirkulation.Laufzeit.Geprueft("Die Laufzeit der Zirkulation");
            if (p.ErzeugerKw.HasValue) Auslegungspruefung.NichtNegativ(p.ErzeugerKw.Value, "die Erzeugerleistung");
            if (!p.ErzeugerKw.HasValue && p.Uebertrager == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.LeistungFehlt,
                    "Nicht rechenbar — weder Erzeuger- noch Übertragerleistung ist bekannt.");
            return p;
        }

        /// <summary>
        /// Die Setzungen des Vereinfachungsverfahrens der A100 (Sensorhöhe, Speichertemperatur)
        /// über die Größen legen; gilt nur für Wohnen bis zur Anwendungsgrenze
        /// (<see cref="SchnellpfadGilt"/>).
        /// </summary>
        internal static Summenlinienparameter Schnellpfad(Summenlinienparameter p, Parametersatz ps)
            => Pruefen(p with
            {
                SensorhoeheAnteil = ps.Wert(ZapfAuslegungParameter.VEREINFACHUNG_SENSORHOEHE),
                SpeicherC = ps.Wert(ZapfAuslegungParameter.VEREINFACHUNG_SPEICHERTEMPERATUR),
                Schnellpfad = true
            });

        /// <summary>Gilt der Schnellpfad: nur Wohnen mit höchstens so vielen WE wie die Anwendungsgrenze.</summary>
        internal static bool SchnellpfadGilt(bool wohnen, double wohneinheiten, Parametersatz ps)
            => wohnen && wohneinheiten > 0 && wohneinheiten <= ps.Wert(ZapfAuslegungParameter.VEREINFACHUNG_GRENZE);

        // =================================================================================
        // Nachweis
        // =================================================================================

        /// <summary>Die Leistung Φ_N [kW] beim Volumen V: <c>min(Φ_Erzeuger, Φ_Ü(V))</c> über die bekannten.</summary>
        internal static double LeistungKw(double volumenL, Summenlinienparameter p, out bool unplausibel)
        {
            unplausibel = false;
            double? ue = p.Uebertrager?.LeistungBeiKw(volumenL, out unplausibel);
            if (p.ErzeugerKw.HasValue && ue.HasValue) return Math.Min(p.ErzeugerKw.Value, ue.Value);
            if (p.ErzeugerKw.HasValue) return p.ErzeugerKw.Value;
            return ue.Value;
        }

        /// <summary>
        /// <b>Der Nachweis eines Wertepaars</b> (Formel oben). Mit <paramref name="mitVerlauf"/>
        /// trägt das Ergebnis den Speicherinhalt Q(i) für i = 0 … 1440.
        /// </summary>
        internal static Summenliniennachweis Nachweis(Bedarfstag tag, double volumenL, double leistungKw,
                                                      Summenlinienparameter p, bool mitVerlauf = false)
        {
            if (tag == null)
                throw new ZapfAuslegungException(ZapfAuslegungsfehler.BedarfstagUngueltig,
                    "Nicht rechenbar — die Summenlinie braucht einen Bedarfstag.");
            IReadOnlyList<double> q = tag.MinutenKwh;
            double cw = Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K;
            double qMax = volumenL * cw * p.SpreizungK * p.Ladungsfaktor / Mengengeruest.WH_JE_KWH;
            double qOn = qMax * (1.0 - p.SensorhoeheAnteil);
            double qMin = p.Speicherart == ZapfSpeicherart.GemischterSpeicher
                ? volumenL * cw * (1.0 - p.SensorhoeheAnteil / 2.0) * (p.MischwasserC.Value - p.KaltwasserAuslegungC)
                  * p.Ladungsfaktor / Mengengeruest.WH_JE_KWH
                : 0.0;

            double[] verlauf = mitVerlauf ? new double[Bedarfstag.MINUTEN + 1] : null;
            double inhalt = qMax;
            bool ein = false;
            int tOn = 0, laufMinuten = 0, erste = -1;
            double kleinster = double.PositiveInfinity;
            for (int i = 0; i < Bedarfstag.MINUTEN; i++)
            {
                if (verlauf != null) verlauf[i] = inhalt;
                if (!ein && inhalt <= qOn)
                {
                    ein = true;
                    tOn = i;
                }
                else if (ein && Rechenrand.SchwelleErreicht(inhalt, qMax))
                {
                    ein = false;
                }
                double phiV = p.SpeicherverlustKw + p.Zirkulation.LeistungKw * p.Zirkulation.Laufzeit.AnteilMinute(i);
                double phiEff = ein && i - tOn >= p.VerzoegerungMin ? leistungKw - phiV : -phiV;
                double abstand = inhalt - q[i] - qMin;
                if (abstand < kleinster) kleinster = abstand;
                if (abstand < 0 && erste < 0) erste = i;
                if (ein) laufMinuten++;
                double neu = inhalt - q[i] + phiEff / Bedarfstag.MINUTEN_JE_STUNDE;
                inhalt = neu < qMax ? neu : qMax;
            }
            if (verlauf != null) verlauf[Bedarfstag.MINUTEN] = inhalt;

            return new Summenliniennachweis(volumenL, leistungKw, erste < 0, kleinster, erste,
                (double)laufMinuten / Bedarfstag.MINUTEN_JE_STUNDE, qMax, qOn, qMin,
                verlauf != null ? Array.AsReadOnly(verlauf) : null);
        }

        // =================================================================================
        // Kleinstes Volumen, Wertepaarkurve
        // =================================================================================

        /// <summary>
        /// <b>Das kleinste Volumen mit Nachweis</b>; Φ_N(V) gilt immer aus Erzeuger und
        /// Übertrager (<see cref="LeistungKw"/>) — auch in der Wertepaarkurve.
        /// </summary>
        internal static Summenlinienpunkt KleinstesVolumen(Bedarfstag tag, Summenlinienparameter p,
                                                           ICollection<Auslegungshinweis> hinweise)
        {
            bool Gelingt(double v, out double phi, out double ladezeit, out bool unplausibel)
            {
                phi = LeistungKw(v, p, out unplausibel);
                Summenliniennachweis n = Nachweis(tag, v, phi, p);
                ladezeit = n.LadezeitH;
                return n.Erfuellt;
            }

            double phi0, lz0;
            bool u0;
            if (Gelingt(0.0, out phi0, out lz0, out u0))
                return new Summenlinienpunkt(0.0, phi0, lz0, Summenliniensuche.Trivial, u0);

            // Startwert: Tagesbedarf und Tagesverlust passen in den Speicher.
            double tagesenergie = tag.TagessummeKwh;
            for (int i = 0; i < Bedarfstag.MINUTEN; i++)
                tagesenergie += (p.SpeicherverlustKw + p.Zirkulation.LeistungKw * p.Zirkulation.Laufzeit.AnteilMinute(i))
                                / Bedarfstag.MINUTEN_JE_STUNDE;
            double hoch = tagesenergie * Mengengeruest.WH_JE_KWH
                          / (Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * p.SpreizungK * p.Ladungsfaktor);
            if (!(hoch > 0)) hoch = 1.0;
            int verdopplung = 0;
            while (!Gelingt(hoch, out _, out _, out _))
            {
                if (++verdopplung > VERDOPPLUNGEN)
                    throw new ZapfAuslegungException(ZapfAuslegungsfehler.KeinVolumen,
                        "Nicht rechenbar — die Summenlinie findet kein Volumen mit Nachweis.");
                hoch *= 2.0;
            }

            // Grobes Raster: Monotonie des Nachweises über (0; hoch].
            var gelingt = new bool[RASTER_GROB + 1];
            int erster = -1;
            for (int k = 1; k <= RASTER_GROB; k++)
            {
                gelingt[k] = Gelingt(hoch * k / RASTER_GROB, out _, out _, out _);
                if (gelingt[k] && erster < 0) erster = k;
            }
            bool monoton = erster > 0;
            for (int k = erster; monoton && k <= RASTER_GROB; k++) monoton = gelingt[k];

            double v;
            Summenliniensuche suche;
            if (monoton)
            {
                double unten = erster == 1 ? 0.0 : hoch * (erster - 1) / RASTER_GROB;
                double oben = hoch * erster / RASTER_GROB;
                for (int s = 0; s < BISEKTIONSSCHRITTE; s++)
                {
                    double mitte = 0.5 * (unten + oben);
                    if (mitte <= unten || mitte >= oben) break;
                    if (Gelingt(mitte, out _, out _, out _)) oben = mitte;
                    else unten = mitte;
                }
                v = oben;
                suche = Summenliniensuche.Bisektion;
            }
            else
            {
                v = hoch;
                for (int k = 1; k <= RASTER_FEIN; k++)
                {
                    double kandidat = hoch * k / RASTER_FEIN;
                    if (Gelingt(kandidat, out _, out _, out _))
                    {
                        v = kandidat;
                        break;
                    }
                }
                suche = Summenliniensuche.Rasterlauf;
                Auslegungshinweis.Einmal(hinweise, new Auslegungshinweis("SUMMENLINIE_NICHT_MONOTON",
                    "Der Nachweis der Summenlinie ist über das Volumen nicht monoton; das kleinste Volumen stammt aus einem feinen Rasterlauf."));
            }

            Gelingt(v, out double phi, out double ladezeit, out bool unplausibelV);
            if (unplausibelV)
                Auslegungshinweis.Einmal(hinweise, new Auslegungshinweis("UEBERTRAGER_UNPLAUSIBEL",
                    "Die Schätzformel der Übertragerfläche ergibt beim Auslegungsvolumen keine positive Fläche; bitte die Fläche angeben.",
                    true));
            return new Summenlinienpunkt(v, phi, ladezeit, suche, unplausibelV);
        }

        /// <summary>
        /// Die Wertepaarkurve (eigene Erweiterung des Nachweisverfahrens): für die
        /// Erzeugerleistung Φ_E,k = Φ_max · k / n (k = 1 … n) das kleinste Volumen V_k mit
        /// <c>Φ_N(V) = min(Φ_E,k, Φ_Ü(V))</c>; der Punkt trägt Φ_N(V_k). So ist jedes Paar mit
        /// dem Übertrager baubar — ein festes Φ_N über Φ_Ü(V) wäre es nicht. Ohne Übertrager ist
        /// Φ_N = Φ_E,k. Φ_max ist die Erzeugerleistung des Projekts, ohne Erzeuger die Leistung
        /// des Auslegungspunkts (<see cref="Rechnen"/>).
        /// </summary>
        internal static IReadOnlyList<Summenlinienpunkt> Wertepaarkurve(Bedarfstag tag, Summenlinienparameter p,
                                                                        double leistungMaxKw, int punkte,
                                                                        ICollection<Auslegungshinweis> hinweise)
        {
            if (punkte < 1 || !(leistungMaxKw > 0)) return new Summenlinienpunkt[0];
            var kurve = new List<Summenlinienpunkt>(punkte);
            for (int k = 1; k <= punkte; k++)
            {
                try
                {
                    kurve.Add(KleinstesVolumen(tag, p with { ErzeugerKw = leistungMaxKw * k / punkte }, hinweise));
                }
                catch (ZapfAuslegungException)
                {
                    // Eine zu kleine Leistung findet kein Volumen — der Punkt fehlt in der Kurve.
                }
            }
            return kurve.AsReadOnly();
        }

        /// <summary>
        /// Die Zeitkonstante τ [min] = m · c_w / (U·A) · k_τ nach A1 — nur informativ. A1 setzt
        /// c_w in kJ/(kg·K) an; mit m = V (1 kg/l) und c_w in Wh/(l·K) steht
        /// <c>V · c_w · 3,6 / (U·A)</c> [kJ/W], k_τ macht daraus Minuten.
        /// </summary>
        internal static double ZeitkonstanteMin(double volumenL, double uaWJeK, double koeffizient)
        {
            Auslegungspruefung.Positiv(uaWJeK, "U·A des Übertragers");
            return volumenL * Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * KJ_JE_WH / uaWJeK * koeffizient;
        }

        /// <summary>
        /// <b>Die Summenlinie einer Topologiegruppe Speicher:</b> Auslegungspunkt (kleinstes
        /// Volumen bei Φ_N aus Erzeuger und Übertrager), Nachweis samt Verlauf, Wertepaarkurve
        /// (<paramref name="wertepaare"/> Punkte; 0 = keine), Zeitkonstante, Vermerk.
        /// </summary>
        internal static Summenlinienergebnis Rechnen(Bedarfstag tag, Summenlinienparameter p, int wertepaare)
        {
            Pruefen(p);
            var hinweise = new List<Auslegungshinweis>();
            if (tag != null && tag.SpitzenUnterschaetzt)
                hinweise.Add(new Auslegungshinweis(Bedarfstag.VERMERK_SPITZEN_UNTERSCHAETZT,
                    "Der Bedarfstag stammt aus einem Stundenprofil — Spitzen unter einer Stunde sind unterschätzt.", true));
            Summenlinienpunkt punkt = KleinstesVolumen(tag, p, hinweise);
            Summenliniennachweis nachweis = Nachweis(tag, punkt.VolumenL, punkt.LeistungKw, p, true);
            IReadOnlyList<Summenlinienpunkt> kurve = Wertepaarkurve(tag, p, p.ErzeugerKw ?? punkt.LeistungKw, wertepaare, hinweise);
            if (kurve.Count > 0)
                hinweise.Add(new Auslegungshinweis("WERTEPAARKURVE", VERMERK_WERTEPAARKURVE + "."));

            double? tau = null;
            if (p.ZeitkonstanteKoeffizient.HasValue && p.Uebertrager != null)
            {
                double? ua = p.Uebertrager.UaBeiWJeK(punkt.VolumenL, out _);
                if (ua.HasValue && ua.Value > 0) tau = ZeitkonstanteMin(punkt.VolumenL, ua.Value, p.ZeitkonstanteKoeffizient.Value);
            }

            return new Summenlinienergebnis
            {
                Punkt = punkt,
                Wertepaare = kurve,
                Nachweis = nachweis,
                ZeitkonstanteMin = tau,
                Schnellpfad = p.Schnellpfad,
                Hinweise = hinweise.AsReadOnly()
            };
        }
    }
}

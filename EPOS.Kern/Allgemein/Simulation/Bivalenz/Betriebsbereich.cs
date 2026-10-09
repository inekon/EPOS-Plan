#nullable enable

using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Betriebsbereich einer Stunde (Fachkonzept Übergabegrenze 4.5). Die Zahlwerte sind fest
    /// (Zähler je Bereich); B0 und B4 zählen in den Ergebnisspalten gemeinsam als „nur Kessel".
    /// </summary>
    internal enum Betriebsbereich
    {
        /// <summary>B0 <c>NUR_KESSEL</c>: Die Wärmepumpe ist nicht verfügbar (Sperrzeit, Zeitprogramm, Abschaltpunkt, Rücklaufgrenze).</summary>
        NichtVerfuegbar = 0,

        /// <summary>B1 <c>WP_ALLEIN</c>: θ_V,soll ≤ θ_WP,max und der Bedarf liegt unter Φ_WP,grenz.</summary>
        WpAllein = 1,

        /// <summary>B2 <c>PARALLEL</c>: θ_V,soll ≤ θ_WP,max, die Wärmepumpe liefert Φ_WP,grenz, der Kessel den Rest am selben Vorlauf.</summary>
        Parallel = 2,

        /// <summary>B3 <c>VORWAERMUNG</c>: θ_V,soll &gt; θ_WP,max mit Vorwärmbetrieb, der Kessel hebt in Reihe auf θ_V,soll.</summary>
        Vorwaermung = 3,

        /// <summary>B4 <c>NUR_KESSEL</c>: θ_V,soll &gt; θ_WP,max ohne Vorwärmbetrieb (bzw. alternativ: Φ_WP,grenz reicht nicht).</summary>
        NurKessel = 4,
    }

    /// <summary>
    /// Die Eingänge der Bereichsregel einer Stunde (Fachkonzept Übergabegrenze 4.4, 4.5). Leistungen in
    /// einer gemeinsamen Einheit (kW im Lauf), Temperaturen in °C.
    /// </summary>
    internal readonly record struct Bereichseingang
    {
        /// <summary>false: Sperrzeit, Zeitprogramm, Umschaltung oder Abschaltpunkt nehmen die Wärmepumpe aus der Stunde (B0).</summary>
        internal bool Verfuegbar { get; init; }

        /// <summary>Der Grund, wenn <see cref="Verfuegbar"/> false ist.</summary>
        internal Verfuegbarkeitsgrund GrundNichtVerfuegbar { get; init; }

        /// <summary>θ_V,soll — der Vorlauf, den der Heizkreis in der Stunde verlangt.</summary>
        internal double VorlaufSollC { get; init; }

        /// <summary>θ_R — der Rücklauf des Heizkreises der Stunde; NaN = unbekannt.</summary>
        internal double RuecklaufC { get; init; }

        /// <summary>Φ_Bedarf — der offene Heizbedarf, den die Wärmepumpe in der Stunde sieht.</summary>
        internal double BedarfKw { get; init; }

        /// <summary>Φ_KF(min(θ_V,soll, θ_WP,max)) bei der Quelltemperatur der Stunde.</summary>
        internal double KennfeldKw { get; init; }

        /// <summary>Φ_UE,max — die Übergabegrenze bei θ_WP,max und der Raumtemperatur der Stunde; NaN = ohne.</summary>
        internal double UebergabeMaxKw { get; init; }

        /// <summary>Φ_Hydraulik (<see cref="Hydraulikgrenze"/>); +∞ oder NaN = unbegrenzt (ohne Hydraulikdaten).</summary>
        internal double HydraulikKw { get; init; }

        /// <summary>
        /// Der Grund, wenn Φ_Hydraulik bindet (<c>SPREIZUNG_MAX</c>, an der Weiche auch <c>UEBERGABE_HOECHSTVORLAUF</c>);
        /// <see cref="Verfuegbarkeitsgrund.KeineBegrenzung"/> heißt <c>SPREIZUNG_MAX</c>.
        /// </summary>
        internal Verfuegbarkeitsgrund HydraulikGrund { get; init; }

        /// <summary>W_H = Σ Φ_N/(θ_V,N − θ_R,N) der gekoppelten Übergabe [Leistung je K].</summary>
        internal double WH { get; init; }

        /// <summary>θ_WP,max — der Höchstvorlauf der Wärmepumpe (<c>Vorlauf_Max</c>, Rückfall <c>Vorlauf</c>).</summary>
        internal double HoechstvorlaufC { get; init; }

        /// <summary>σ_min [K].</summary>
        internal double SpreizungMinK { get; init; }

        /// <summary>Schalter <c>Vorwaermbetrieb</c> der Anlage.</summary>
        internal bool Vorwaermbetrieb { get; init; }

        /// <summary>Die Betriebsart des bivalenten Betriebs.</summary>
        internal Bivalenzbetriebsart Betriebsart { get; init; }

        /// <summary>Steht ein zweiter Wärmeerzeuger bereit (Kessel in der Kaskade oder Heizstab)?</summary>
        internal bool ZweiterErzeuger { get; init; }

        /// <summary>Steht die Wärmepumpe in der Kaskade vor dem Kessel (Pflicht des Vorwärmbetriebs)?</summary>
        internal bool VorDemKessel { get; init; }
    }

    /// <summary>Das Ergebnis der Bereichsregel einer Stunde.</summary>
    /// <param name="Bereich">B0 … B4.</param>
    /// <param name="LeistungKw">Was die Wärmepumpe in der Stunde höchstens liefert (ersetzt die Kennfeldkapazität).</param>
    /// <param name="Anteil">B3: a = (θ_WP,max − θ_R)/(θ_V,soll − θ_R); sonst 1 in B1/B2, 0 in B0/B4.</param>
    /// <param name="Grund">Der Grund einer Begrenzung; <see cref="Verfuegbarkeitsgrund.KeineBegrenzung"/> ohne.</param>
    /// <param name="VorwaermvorlaufC">B3 mit laufender Wärmepumpe: θ_WP,max (Kesselrücklauf); sonst NaN.</param>
    /// <param name="AmHoechstvorlauf">Rechnet die Leistung am Kennfeld bei θ_WP,max (nicht bei θ_V,soll)?</param>
    /// <param name="KaskadeVerletzt">Vorwärmbetrieb verlangt, die Wärmepumpe steht aber hinter dem Kessel (Stunde als B4).</param>
    internal readonly record struct Bereichsergebnis(Betriebsbereich Bereich, double LeistungKw, double Anteil,
                                                     Verfuegbarkeitsgrund Grund, double VorwaermvorlaufC,
                                                     bool AmHoechstvorlauf, bool KaskadeVerletzt)
    {
        /// <summary>R744-Faktor f auf Leistung und COP (UB‑E3, <see cref="Ruecklaufgrenze"/>); 1 ohne Abwertung.</summary>
        internal double RuecklaufFaktor { get; init; } = 1.0;

        /// <summary>σ_WP &lt; σ_min in einer Stunde mit laufender Wärmepumpe (B1/B2): die Stunde taktet (Zähler).</summary>
        internal bool Taktet { get; init; }

        /// <summary>θ_R,WP — der Rücklauf zur Wärmepumpe der Stunde (NaN = unbekannt).</summary>
        internal double RuecklaufWpC { get; init; } = double.NaN;
    }

    internal static partial class Bivalenzrechner
    {
        /// <summary>
        /// <b>Der Betriebsbereich einer Stunde</b> (Fachkonzept Übergabegrenze 4.5; Umsetzungskonzept 3.2
        /// Schritt 5). Reine Zahlen, deterministisch. Φ_WP,grenz = min(Φ_KF(min(θ_V,soll, θ_WP,max)), Φ_Hydraulik).
        /// <list type="bullet">
        /// <item>B0: nicht verfügbar (Sperrzeit, Zeitprogramm, Abschaltpunkt als Deckel nach UB‑Q4 a).</item>
        /// <item>θ_V,soll ≤ θ_WP,max: B1, wenn der Bedarf unter Φ_WP,grenz liegt, sonst B2 — die Leistung ist
        /// Φ_WP,grenz (heutiges Verhalten). Alternativ kennt kein B2: Reicht Φ_WP,grenz nicht, deckt der
        /// Kessel allein (B4).</item>
        /// <item>θ_V,soll &gt; θ_WP,max mit Vorwärmbetrieb (nicht alternativ, Wärmepumpe vor dem Kessel): B3,
        /// Φ_WP = min(W_H·(θ_WP,max − θ_R), Φ_KF(θ_WP,max), Φ_Hydraulik), 0 mit Grund <c>SPREIZUNG_MIN</c>,
        /// wenn θ_WP,max − θ_R &lt; σ_min oder θ_R unbekannt ist.</item>
        /// <item>sonst B4 mit Grund <c>UEBERGABE_HOECHSTVORLAUF</c>.</item>
        /// <item>Ohne zweiten Erzeuger über θ_WP,max (FK 4.5 „Ohne Kessel"): die Wärmepumpe allein am Höchstvorlauf,
        /// höchstens min(Φ_UE,max, Φ_KF(θ_WP,max)); der Fehlbetrag ist Komfortverlust (B1 mit Grund
        /// <c>UEBERGABE_HOECHSTVORLAUF</c>).</item>
        /// </list>
        /// </summary>
        internal static Bereichsergebnis Bereich(in Bereichseingang e)
        {
            double hydraulik = double.IsNaN(e.HydraulikKw) ? double.PositiveInfinity : e.HydraulikKw;
            Verfuegbarkeitsgrund hg = e.HydraulikGrund == Verfuegbarkeitsgrund.KeineBegrenzung
                ? Verfuegbarkeitsgrund.SpreizungMax : e.HydraulikGrund;
            if (!e.Verfuegbar)
                return new Bereichsergebnis(Betriebsbereich.NichtVerfuegbar, 0.0, 0.0, e.GrundNichtVerfuegbar,
                                            double.NaN, false, false);

            if (!(e.VorlaufSollC > e.HoechstvorlaufC))
            {
                double grenz = Math.Min(e.KennfeldKw, hydraulik);
                Verfuegbarkeitsgrund g = hydraulik < e.KennfeldKw ? hg : Verfuegbarkeitsgrund.KeineBegrenzung;
                if (!(e.BedarfKw > grenz))
                    return new Bereichsergebnis(Betriebsbereich.WpAllein, grenz, 1.0, g, double.NaN, false, false);
                if (e.Betriebsart == Bivalenzbetriebsart.Alternativ && e.ZweiterErzeuger)
                    return new Bereichsergebnis(Betriebsbereich.NurKessel, 0.0, 0.0, Verfuegbarkeitsgrund.Leistungsgrenze,
                                                double.NaN, false, false);
                return new Bereichsergebnis(Betriebsbereich.Parallel, grenz, 1.0, g, double.NaN, false, false);
            }

            if (!e.ZweiterErzeuger)
            {
                double ue = double.IsNaN(e.UebergabeMaxKw) ? double.PositiveInfinity : e.UebergabeMaxKw;
                double allein = Math.Min(Math.Min(ue, e.KennfeldKw), hydraulik);
                return new Bereichsergebnis(Betriebsbereich.WpAllein, allein > 0.0 ? allein : 0.0, 1.0,
                                            Verfuegbarkeitsgrund.UebergabeHoechstvorlauf, double.NaN, true, false);
            }

            bool vorwaermen = e.Vorwaermbetrieb && e.Betriebsart != Bivalenzbetriebsart.Alternativ;
            if (vorwaermen && !e.VorDemKessel)
                return new Bereichsergebnis(Betriebsbereich.NurKessel, 0.0, 0.0, Verfuegbarkeitsgrund.UebergabeHoechstvorlauf,
                                            double.NaN, false, true);
            if (!vorwaermen)
                return new Bereichsergebnis(Betriebsbereich.NurKessel, 0.0, 0.0, Verfuegbarkeitsgrund.UebergabeHoechstvorlauf,
                                            double.NaN, false, false);

            double spreizung = e.HoechstvorlaufC - e.RuecklaufC;
            if (double.IsNaN(spreizung) || spreizung < e.SpreizungMinK)
                return new Bereichsergebnis(Betriebsbereich.Vorwaermung, 0.0, 0.0, Verfuegbarkeitsgrund.SpreizungMin,
                                            double.NaN, true, false);
            double wasser = e.WH * spreizung;
            double leistung = Math.Min(Math.Min(wasser, e.KennfeldKw), hydraulik);
            if (!(leistung > 0.0)) leistung = 0.0;
            double nenner = e.VorlaufSollC - e.RuecklaufC;
            double anteil = nenner > 0.0 ? spreizung / nenner : 0.0;
            Verfuegbarkeitsgrund grund = hydraulik < Math.Min(wasser, e.KennfeldKw) ? hg
                                       : Verfuegbarkeitsgrund.UebergabeHoechstvorlauf;
            return new Bereichsergebnis(Betriebsbereich.Vorwaermung, leistung, anteil, grund,
                                        leistung > 0.0 ? e.HoechstvorlaufC : double.NaN, true, false);
        }
    }

    /// <summary>
    /// <b>Das Bivalenzobjekt einer Wärmepumpe im Lauf</b> (Umsetzungskonzept 3.2, U‑1): entsteht nur, wenn
    /// <c>Einbindung</c> gesetzt und die Kopplung aktiv ist; ohne es rechnet die Wärmepumpe den Bestandsweg.
    /// Trägt die kalibrierte Übergabe (UB‑a), die Vorgaben der Anlage, den Zwischenspeicher der
    /// Übergabegrenze je Stunde und Zone (Startwert aus der Vorstunde) und die Zähler je Bereich.
    /// </summary>
    internal sealed class Bivalenzmodul
    {
        private readonly Uebergabezone[] _zonen;
        private readonly Func<int, int, double>? _raumtemperatur;
        private readonly double[] _start;
        private int _gespeicherteStunde = -1;
        private double _gespeicherterWert = double.NaN;

        /// <param name="zonen">Die kalibrierte Übergabe je gekoppeltem Gebäude bzw. Zone [kW].</param>
        /// <param name="raumtemperatur">θ_i der Stunde je Zone (Index der Zone, Stunde); NaN oder null → Auslegungsraumtemperatur der Zone.</param>
        /// <param name="grenzen">Die Gerätegrenzen (UB‑E3); <c>null</c> = ohne Hydraulik- und Rücklaufgrenze (Φ_Hydraulik unbegrenzt).</param>
        /// <param name="einbindung">Die Einbindung der Anlage (<c>DIREKT</c>/<c>PUFFER</c>/<c>WEICHE</c>).</param>
        /// <param name="nennleistungKw">Φ_WP,N [kW] für ṁ_N·c_p = Φ_WP,N/σ_A; NaN = unbekannt.</param>
        internal Bivalenzmodul(IReadOnlyList<Uebergabezone> zonen, Func<int, int, double>? raumtemperatur,
                               double hoechstvorlaufC, double spreizungMinK, bool vorwaermbetrieb,
                               Bivalenzbetriebsart betriebsart, bool zweiterErzeuger, bool vorDemKessel,
                               Geraetegrenzen? grenzen = null, string? einbindung = null, double nennleistungKw = double.NaN)
        {
            if (zonen == null) throw new ArgumentNullException(nameof(zonen));
            _zonen = new Uebergabezone[zonen.Count];
            double wh = 0.0;
            for (int z = 0; z < zonen.Count; z++)
            {
                _zonen[z] = zonen[z];
                wh += zonen[z].WH;
            }
            _raumtemperatur = raumtemperatur;
            _start = new double[_zonen.Length];
            for (int z = 0; z < _start.Length; z++) _start[z] = double.NaN;
            WH = wh;
            HoechstvorlaufC = hoechstvorlaufC;
            SpreizungMinK = spreizungMinK;
            Vorwaermbetrieb = vorwaermbetrieb;
            Betriebsart = betriebsart;
            ZweiterErzeuger = zweiterErzeuger;
            VorDemKessel = vorDemKessel;
            Grenzen = grenzen;
            Einbindung = Hydraulikgrenze.Art(einbindung);
            NennleistungKw = nennleistungKw;
            double nenn = grenzen != null ? Hydraulikgrenze.Nennstrom(nennleistungKw, grenzen.SpreizungAuslegungK) : double.NaN;
            // Weiche mit ṁ_WP < ṁ_HK: r = ṁ_HK/ṁ_WP > 1 — die Übergabegrenze am gemischten Vorlauf (FK 4.4).
            MischungR = Einbindung == Einbindungsart.Weiche && nenn > 0.0 && wh > nenn ? wh / nenn : 1.0;
        }

        /// <summary>Die Gerätegrenzen (UB‑E3); <c>null</c> = ohne Hydraulik- und Rücklaufgrenze.</summary>
        internal Geraetegrenzen? Grenzen { get; }

        /// <summary>Die Einbindung der Anlage.</summary>
        internal Einbindungsart Einbindung { get; }

        /// <summary>Φ_WP,N [kW]; NaN = unbekannt.</summary>
        internal double NennleistungKw { get; }

        /// <summary>r = ṁ_HK/ṁ_WP an der Weiche, wenn größer als 1; sonst 1 (ungemischte Übergabegrenze).</summary>
        internal double MischungR { get; }

        /// <summary>
        /// U‑1 (Opt-in): Ein Bivalenzobjekt entsteht nur mit gesetzter <c>Einbindung</c> und aktiver Kopplung
        /// (Kennlinienwahl am gerechneten Heizkreisvorlauf); sonst rechnet die Wärmepumpe den Bestandsweg.
        /// </summary>
        internal static bool Wirksam(string? einbindung, bool kopplungAktiv)
            => kopplungAktiv && !string.IsNullOrWhiteSpace(einbindung);

        /// <summary>W_H = Σ_z W_H,z [kW/K].</summary>
        internal double WH { get; }

        /// <summary>θ_WP,max [°C].</summary>
        internal double HoechstvorlaufC { get; }

        /// <summary>σ_min [K] aus den Gerätegrenzen (leer: Vorgabe <see cref="Bivalenzvorgaben.SPREIZUNG_MIN_K"/>).</summary>
        internal double SpreizungMinK { get; }

        /// <summary>Vorwärmbetrieb der Anlage.</summary>
        internal bool Vorwaermbetrieb { get; }

        /// <summary>Betriebsart des bivalenten Betriebs.</summary>
        internal Bivalenzbetriebsart Betriebsart { get; }

        /// <summary>Steht ein zweiter Erzeuger bereit (Kessel in der Kaskade oder Heizstab)?</summary>
        internal bool ZweiterErzeuger { get; }

        /// <summary>Steht die Wärmepumpe in der Kaskade vor dem Kessel?</summary>
        internal bool VorDemKessel { get; }

        /// <summary>Verlangt der Vorwärmbetrieb eine andere Kaskadenfolge (Wärmepumpe hinter dem Kessel)?</summary>
        internal bool KaskadeVerletzt => Vorwaermbetrieb && Betriebsart != Bivalenzbetriebsart.Alternativ && ZweiterErzeuger && !VorDemKessel;

        /// <summary>
        /// Der Rücklauf des Kreises je Stunde für das Angebot im AK3-Kreis (<see cref="WaermepumpeKapazitaet"/>);
        /// der Kreis schreibt ihn je Durchlauf, NaN = noch unbekannt. <c>null</c> im Profilweg.
        /// </summary>
        internal double[]? KreisruecklaufC { get; set; }

        /// <summary>Die berechneten Bivalenzpunkte bei Auslegungsraumtemperatur (Ergebnisspalten); null = nicht gerechnet.</summary>
        internal Bivalenzpunkte? Punkte { get; set; }

        /// <summary>Φ_UE,max bei Auslegungsraumtemperatur [kW] (Ergebnisspalte <c>Uebergabe_Max_kW</c>).</summary>
        internal double UebergabeMaxAuslegungKw
        {
            get
            {
                double s = 0.0;
                foreach (Uebergabezone z in _zonen) s += ZoneRechnen(z, z.AuslegungRaumC, double.NaN).PhiUeMax;
                return s;
            }
        }

        /// <summary>
        /// Φ_UE,max der Stunde [kW] bei θ_WP,max und der Raumtemperatur der Stunde, Σ über die Zonen —
        /// einmal je Stunde gerechnet und zwischengespeichert, Startwert je Zone aus der zuletzt gerechneten Stunde.
        /// </summary>
        internal double UebergabeMaxKw(int stunde)
        {
            if (stunde == _gespeicherteStunde) return _gespeicherterWert;
            double s = 0.0;
            for (int z = 0; z < _zonen.Length; z++)
            {
                double raum = _raumtemperatur != null ? _raumtemperatur(z, stunde) : double.NaN;
                if (double.IsNaN(raum) || double.IsInfinity(raum)) raum = _zonen[z].AuslegungRaumC;
                Zonengrenze g = ZoneRechnen(_zonen[z], raum, _start[z]);
                _start[z] = g.PhiUeMax;
                s += g.PhiUeMax;
            }
            _gespeicherteStunde = stunde;
            _gespeicherterWert = s;
            return s;
        }

        private Zonengrenze ZoneRechnen(Uebergabezone z, double raumC, double start)
            => MischungR > 1.0 ? Uebergabegrenze.ZoneGemischt(z, HoechstvorlaufC, raumC, MischungR, start)
                               : Uebergabegrenze.Zone(z, HoechstvorlaufC, raumC, start);

        /// <summary>
        /// Φ_Hydraulik der Stunde (<see cref="Hydraulikgrenze"/>) am Vorlauf min(θ_V,soll, θ_WP,max); ohne
        /// <see cref="Grenzen"/> unbegrenzt, der Rücklauf der Wärmepumpe dann der Heizkreisrücklauf.
        /// </summary>
        internal Hydraulikergebnis Hydraulik(double vorlaufSollC, double ruecklaufC, double kennfeldKw, double pufferUntenC)
        {
            Geraetegrenzen? g = Grenzen;
            if (g == null) return Hydraulikergebnis.Unbegrenzt with { RuecklaufWpC = ruecklaufC };
            return Hydraulikgrenze.Rechnen(new Hydraulikeingang
            {
                Einbindung = Einbindung,
                WH = WH,
                VorlaufWpC = Math.Min(vorlaufSollC, HoechstvorlaufC),
                RuecklaufHeizkreisC = ruecklaufC,
                PufferUntenC = pufferUntenC,
                HoechstvorlaufC = HoechstvorlaufC,
                NennleistungKw = NennleistungKw,
                KennfeldKw = kennfeldKw,
                SpreizungAuslegungK = g.SpreizungAuslegungK,
                SpreizungMaxK = g.SpreizungMaxK,
                SpreizungMinK = g.SpreizungMinK,
                MindestvolumenstromAnteil = g.MindestvolumenstromAnteil,
            });
        }

        /// <summary>
        /// Die Bereichsregel mit den Werten dieser Anlage (Umsetzungskonzept 3.2 Schritte 3 bis 5): nach der Verfügbarkeit
        /// die Rücklaufgrenze am Rücklauf der Wärmepumpe (B0, Grund <c>RUECKLAUF_MAX</c>), danach Φ_WP,grenz =
        /// min(Φ_KF(min(θ_V,soll, θ_WP,max))·f, Φ_Hydraulik) mit dem R744-Faktor f.
        /// <paramref name="pufferUntenC"/>: unterste Pufferzone (Einbindung <c>PUFFER</c>); NaN = ohne.
        /// </summary>
        internal Bereichsergebnis Bereich(int stunde, bool verfuegbar, Verfuegbarkeitsgrund grund, double vorlaufSollC,
                                          double ruecklaufC, double bedarfKw, double kennfeldKw, double pufferUntenC = double.NaN)
        {
            Hydraulikergebnis hy = Hydraulik(vorlaufSollC, ruecklaufC, kennfeldKw, pufferUntenC);
            double faktor = 1.0;
            if (verfuegbar && Grenzen != null)
            {
                Ruecklaufpruefung rp = Ruecklaufgrenze.Pruefen(Grenzen, hy.RuecklaufWpC);
                if (rp.Aus)
                    return new Bereichsergebnis(Betriebsbereich.NichtVerfuegbar, 0.0, 0.0, Verfuegbarkeitsgrund.RuecklaufMax,
                                                double.NaN, false, false) { RuecklaufFaktor = 0.0, RuecklaufWpC = hy.RuecklaufWpC };
                faktor = rp.Faktor;
            }
            bool ueber = vorlaufSollC > HoechstvorlaufC;
            Bereichsergebnis e = Bivalenzrechner.Bereich(new Bereichseingang
            {
                Verfuegbar = verfuegbar,
                GrundNichtVerfuegbar = grund,
                VorlaufSollC = vorlaufSollC,
                RuecklaufC = ruecklaufC,
                BedarfKw = bedarfKw,
                KennfeldKw = faktor < 1.0 ? kennfeldKw * faktor : kennfeldKw,
                UebergabeMaxKw = ueber && !ZweiterErzeuger ? UebergabeMaxKw(stunde) : double.NaN,
                HydraulikKw = hy.LeistungKw,
                HydraulikGrund = hy.Grund,
                WH = WH,
                HoechstvorlaufC = HoechstvorlaufC,
                SpreizungMinK = SpreizungMinK,
                Vorwaermbetrieb = Vorwaermbetrieb,
                Betriebsart = Betriebsart,
                ZweiterErzeuger = ZweiterErzeuger,
                VorDemKessel = VorDemKessel,
            });
            bool laeuft = (e.Bereich == Betriebsbereich.WpAllein || e.Bereich == Betriebsbereich.Parallel)
                          && !e.AmHoechstvorlauf && e.LeistungKw > 0.0;
            return e with { RuecklaufFaktor = faktor, Taktet = hy.Taktet && laeuft, RuecklaufWpC = hy.RuecklaufWpC };
        }

        /// <summary>
        /// Die Kapazität am Vorlauf ≤ θ_WP,max im AK3-Kreis (<see cref="WaermepumpeKapazitaet"/>): min(Φ_KF·f, Φ_Hydraulik),
        /// 0 über der Rücklaufgrenze. Ohne <see cref="Grenzen"/> unverändert.
        /// </summary>
        internal double KapazitaetUnterHoechstvorlauf(int stunde, double vorlaufC, double ruecklaufC, double kennfeldKw)
        {
            if (Grenzen == null) return kennfeldKw;
            Bereichsergebnis e = Bereich(stunde, true, Verfuegbarkeitsgrund.KeineBegrenzung, vorlaufC, ruecklaufC, 0.0, kennfeldKw);
            if (e.Bereich == Betriebsbereich.NichtVerfuegbar || e.Bereich == Betriebsbereich.NurKessel) return 0.0;
            return Math.Min(kennfeldKw, e.LeistungKw);
        }

        // ---- Zähler (Umsetzungskonzept 3.2 Schritt 9) ----

        private readonly int[] _stunden = new int[5];
        private readonly double[] _waermeKwh = new double[5];
        private readonly int[] _grundStunden = new int[Enum.GetValues(typeof(Verfuegbarkeitsgrund)).Length];

        /// <summary>Zählt eine Stunde mit Heizbedarf: Bereich, Wärme der Wärmepumpe [kWh], Grund.</summary>
        internal void Zaehlen(Bereichsergebnis b, double waermeKwh)
        {
            _stunden[(int)b.Bereich]++;
            if (waermeKwh > 0.0) _waermeKwh[(int)b.Bereich] += waermeKwh;
            _grundStunden[(int)b.Grund]++;
            if (b.KaskadeVerletzt) KaskadeVerletztStunden++;
            if (b.Taktet) SpreizungUnterschrittenStunden++;
            if (b.Grund == Verfuegbarkeitsgrund.RuecklaufMax) RuecklaufUeberschrittenStunden++;
        }

        /// <summary>Ergebnisspalte <c>Spreizung_Unterschritten_h</c>: Stunden mit σ_WP &lt; σ_min (die Stunde taktet).</summary>
        internal int SpreizungUnterschrittenStunden { get; private set; }

        /// <summary>Ergebnisspalte <c>Ruecklauf_Ueberschritten_h</c>: Stunden über der Rücklaufgrenze (B0, <c>RUECKLAUF_MAX</c>).</summary>
        internal int RuecklaufUeberschrittenStunden { get; private set; }

        /// <summary>Wärme der Wärmepumpe nachtragen (zur zuletzt gezählten Stunde in Bereich <paramref name="bereich"/>).</summary>
        internal void WaermeNachtragen(Betriebsbereich bereich, double waermeKwh)
        {
            if (waermeKwh > 0.0) _waermeKwh[(int)bereich] += waermeKwh;
        }

        /// <summary>Stunden mit verletzter Kaskadenfolge (als B4 gerechnet).</summary>
        internal int KaskadeVerletztStunden { get; private set; }

        /// <summary>Stunden je Bereich.</summary>
        internal int Stunden(Betriebsbereich b) => _stunden[(int)b];

        /// <summary>Wärme der Wärmepumpe je Bereich [kWh].</summary>
        internal double WaermeKwh(Betriebsbereich b) => _waermeKwh[(int)b];

        /// <summary>Stunden je Grund (nur Stunden mit Heizbedarf).</summary>
        internal int GrundStunden(Verfuegbarkeitsgrund g) => _grundStunden[(int)g];

        /// <summary>Ergebnisspalte „nur Kessel": B0 und B4 zusammen [h].</summary>
        internal int NurKesselStunden => _stunden[(int)Betriebsbereich.NichtVerfuegbar] + _stunden[(int)Betriebsbereich.NurKessel];

        /// <summary>Ergebnisspalte „nur Kessel": Wärme der Wärmepumpe in B0 und B4 [kWh] (0 bis auf Ladung).</summary>
        internal double NurKesselKwh => _waermeKwh[(int)Betriebsbereich.NichtVerfuegbar] + _waermeKwh[(int)Betriebsbereich.NurKessel];
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// DIE AUSLEGUNGSHILFE zu <see cref="StrangPlausibilitaet"/> (Anwenderwunsch vom
    /// 08.09.2026: „eine Methode zur Unterstützung der passenden Auswahl"). Dieselben
    /// Regeln P1 bis P7 des <c>Konzept_Wechselrichter_EPOS-Plan.md</c> (Kapitel 4.2),
    /// nur RÜCKWÄRTS gerechnet: Statt eine gegebene Zuordnung zu bewerten, nennt sie den
    /// Bereich, in dem eine Zuordnung grün wird.
    ///
    /// <list type="bullet">
    ///   <item><description><see cref="Reihe"/>: wie viele Module in Reihe zu Modul UND
    ///     Gerät passen — die Schnittmenge aus P1 (Leerlaufspannung bei −10 °C unter
    ///     <c>U_Dc_Max</c>), P2 (MPP-Spannung bei +70 °C über <c>U_Mpp_Min</c>) und P3
    ///     (MPP-Spannung bei −10 °C unter <c>U_Mpp_Max</c>).</description></item>
    ///   <item><description><see cref="ParallelJeMppt"/>: wie viele Stränge ein MPP-Tracker
    ///     verträgt — P4 (Kurzschlussstrom bei +70 °C unter <c>I_Dc_Max</c>) und P5
    ///     (<c>Straenge_Je_Mppt</c>).</description></item>
    ///   <item><description><see cref="ModuleJeGeraet"/>: wie viele Module ein Gerät trägt —
    ///     P6 (DC/AC im Band 1,0…1,5) und P7 (<c>P_Dc_Max</c>).</description></item>
    ///   <item><description><see cref="Vorschlagen"/>: eine ganze Aufteilung für eine
    ///     Modulzahl — Module in Reihe, Stränge parallel, Zahl der Geräte.</description></item>
    ///   <item><description><see cref="GeraeteBewerten"/>: alle Geräte eines Katalogs für ein
    ///     Modulfeld, sortiert nach Eignung (wenige Geräte, DC/AC nahe
    ///     <see cref="DCAC_MITTE"/>).</description></item>
    ///   <item><description><see cref="Aufteilen"/>: derselbe Vorschlag als TABELLE — je
    ///     Gerät und MPP-Tracker eine Zeile (<b>W6‑B‑8</b>).</description></item>
    /// </list>
    ///
    /// <para><b>Dieselben Temperaturen, dieselbe Näherung.</b> Vorgabe sind −10 °C für den
    /// kalten und +70 °C für den heißen Fall (<see cref="StrangPlausibilitaet.T_KALT"/>,
    /// <see cref="StrangPlausibilitaet.T_HEISS"/>); seit <b>W6‑B‑11</b> nehmen
    /// <see cref="Reihe(PhotovoltaikModel, WechselrichterModel, double, double)"/> und
    /// <see cref="ParallelJeMppt(PhotovoltaikModel, WechselrichterModel, double)"/> die
    /// AUSLEGUNGSTEMPERATUREN des Projekts entgegen — die Hilfe rät auf derselben
    /// Grundlage, auf der die Ampel prüft. Für die MPP-Spannung steht <c>beta_OC</c> ein
    /// (<c>Befund.NaeherungMpp</c>); für die Leerlaufspannung ohne <c>beta_OC</c> steht
    /// seit <b>W6‑B‑9</b> der Faktor 1,15 ein
    /// (<see cref="StrangPlausibilitaet.FAKTOR_UOC_OHNE_KOEFFIZIENT"/>). Ein fehlender
    /// Wert macht den betroffenen Bereich „nicht prüfbar" — die Hilfe rät dann
    /// nicht.</para>
    ///
    /// <para><b>Die Sätze</b> (<see cref="ReiheEmpfehlung"/>, <see cref="GeraetEmpfehlung"/>)
    /// stehen in der Ampel unter der Strangtabelle hinter dem Befund, in der Kultur des
    /// Anwenders — dieselbe Stelle, die den Befund nennt, sagt, was passen würde.</para>
    /// </summary>
    public static class StrangAuslegung
    {
        /// <summary>Der Bereich für „Module in Reihe" zu einem Modul an einem Gerät.</summary>
        public sealed class Reihenbereich
        {
            /// <summary>Untere Grenze (P2). 1, wenn P2 nicht prüfbar ist.</summary>
            public int Min = 1;
            /// <summary>Obere Grenze (kleinere von P1 und P3); null = keine prüfbar.</summary>
            public int? Max;
            /// <summary>Obergrenze aus P1 (Leerlaufspannung kalt).</summary>
            public int? MaxUoc;
            /// <summary>Untergrenze aus P2 (MPP-Spannung heiß).</summary>
            public int? MinMpp;
            /// <summary>Obergrenze aus P3 (MPP-Spannung kalt).</summary>
            public int? MaxMpp;
            /// <summary>Wenigstens eine der drei Grenzen ließ sich rechnen.</summary>
            public bool Pruefbar;
            /// <summary>Es gibt eine Reihe, die alle prüfbaren Grenzen einhält.</summary>
            public bool Moeglich { get { return Pruefbar && (!Max.HasValue || Min <= Max.Value); } }
        }

        /// <summary>Der Bereich für „Module je Gerät" aus DC/AC-Band und DC-Eingangsgrenze.</summary>
        public sealed class Modulbereich
        {
            public int? Min;
            public int? Max;
            public bool Pruefbar;
            public bool Moeglich { get { return Pruefbar && (!Min.HasValue || !Max.HasValue || Min.Value <= Max.Value); } }
        }

        /// <summary>Eine vollständige Aufteilung eines Modulfelds.</summary>
        public sealed class Vorschlag
        {
            public bool Moeglich;
            public int Reihe;
            public int Parallel;          // Stränge je Gerät
            public int Geraete;
            public int Straenge;          // Stränge gesamt
            public double DcAc;
            /// <summary>Warum es keinen Vorschlag gibt (leer, wenn <see cref="Moeglich"/>).</summary>
            public string Grund = "";
        }

        /// <summary>Ein Gerät des Katalogs mit seinem Vorschlag für ein Modulfeld.</summary>
        public sealed class Bewertung
        {
            public WechselrichterModel Geraet;
            public Vorschlag Vorschlag;
            /// <summary>Abstand des DC/AC-Verhältnisses zur Mitte des Bandes (1,25) — je kleiner, desto besser.</summary>
            public double Abstand;
        }

        /// <summary>
        /// EINE Zeile der vorgeschlagenen Strangtabelle: welches Gerät, welcher Tracker,
        /// wie viele Module in Reihe und wie viele Stränge parallel
        /// (Anwenderwunsch 08.09.2026, <b>W6‑B‑8</b>).
        ///
        /// <para><b>Warum eine eigene Klasse und nicht die Strangzeile der Maske.</b>
        /// Der Kern kennt die Oberfläche nicht; er nennt nur die vier Zahlen, die eine
        /// Strangzeile ausmachen. Bezeichner, Gerätekopie und die geerbten Werte
        /// Neigung/Azimut setzt die Maske — sie bleiben leer und heißen damit
        /// „der Anlagenwert".</para>
        /// </summary>
        public sealed class Strangvorgabe
        {
            /// <summary>Welches physische Gerät, 1…n.</summary>
            public int Geraetenummer;
            /// <summary>MPP-Tracker dieses Geräts, 1…m.</summary>
            public int Mppt;
            /// <summary>Module in Reihe.</summary>
            public int ModuleReihe;
            /// <summary>Parallel geschaltete Stränge an diesem Tracker.</summary>
            public int StraengeParallel;
        }

        /// <summary>Die Mitte des DC/AC-Bandes, an der die Rangfolge misst.</summary>
        public const double DCAC_MITTE = 1.25;

        // -----------------------------------------------------------------
        //  Module in Reihe (P1, P2, P3)
        // -----------------------------------------------------------------

        /// <summary>
        /// Der Reihenbereich bei den VORGABE-Temperaturen — der Weg von bisher.
        /// </summary>
        public static Reihenbereich Reihe(PhotovoltaikModel modul, WechselrichterModel geraet)
        {
            return Reihe(modul, geraet, StrangPlausibilitaet.T_KALT, StrangPlausibilitaet.T_HEISS);
        }

        /// <summary>
        /// Der Reihenbereich bei den AUSLEGUNGSTEMPERATUREN des Projekts
        /// (<b>W6‑B‑11</b>) — dieselben Regeln P1 bis P3, rückwärts gerechnet.
        ///
        /// <para><b>P1 kennt denselben Rückfall wie die Ampel</b> (<b>W6‑B‑9</b>):
        /// Fehlt <c>beta_OC</c>, rechnet die Obergrenze mit
        /// <c>1,15 · U_oc,STC</c> statt zu entfallen. Eine Hilfe, die eine Grenze
        /// verschweigt, die die Ampel prüft, schickte den Anwender in genau das Rot,
        /// das sie vermeiden soll.</para>
        /// </summary>
        public static Reihenbereich Reihe(PhotovoltaikModel modul, WechselrichterModel geraet,
                                          double tKalt, double tHeiss)
        {
            var r = new Reihenbereich();
            if (modul == null || geraet == null) return r;

            double? uoc = StrangPlausibilitaet.UocKaltReihe(1, modul.m_U_Leerlauf, modul.m_beta_OC, tKalt);
            double? heiss = StrangPlausibilitaet.SpannungReihe(1, modul.m_U_Mpp, modul.m_beta_OC, tHeiss);
            double? kalt = StrangPlausibilitaet.SpannungReihe(1, modul.m_U_Mpp, modul.m_beta_OC, tKalt);

            if (uoc.HasValue && uoc.Value > 0.0 && Gesetzt(geraet.m_U_Dc_Max))
                r.MaxUoc = (int)Math.Floor(geraet.m_U_Dc_Max.Value / uoc.Value + 1e-9);
            if (heiss.HasValue && heiss.Value > 0.0 && Gesetzt(geraet.m_U_Mpp_Min))
                r.MinMpp = (int)Math.Ceiling(geraet.m_U_Mpp_Min.Value / heiss.Value - 1e-9);
            if (kalt.HasValue && kalt.Value > 0.0 && Gesetzt(geraet.m_U_Mpp_Max))
                r.MaxMpp = (int)Math.Floor(geraet.m_U_Mpp_Max.Value / kalt.Value + 1e-9);

            r.Pruefbar = r.MaxUoc.HasValue || r.MinMpp.HasValue || r.MaxMpp.HasValue;
            r.Min = Math.Max(1, r.MinMpp ?? 1);
            if (r.MaxUoc.HasValue) r.Max = r.MaxUoc;
            if (r.MaxMpp.HasValue) r.Max = r.Max.HasValue ? Math.Min(r.Max.Value, r.MaxMpp.Value) : r.MaxMpp;
            return r;
        }

        // -----------------------------------------------------------------
        //  Stränge je MPP-Tracker (P4, P5)
        // -----------------------------------------------------------------

        /// <summary>Wie viele Stränge ein Tracker verträgt, bei der Vorgabetemperatur.</summary>
        public static int? ParallelJeMppt(PhotovoltaikModel modul, WechselrichterModel geraet)
        {
            return ParallelJeMppt(modul, geraet, StrangPlausibilitaet.T_HEISS);
        }

        /// <summary>
        /// Wie viele Stränge ein Tracker verträgt; <c>null</c>, wenn Strom oder Grenze
        /// fehlen.
        ///
        /// <para><b>Die Grenze ist der KURZSCHLUSSSTROM, wenn er gepflegt ist</b>
        /// (<b>W6‑B‑10</b>): <c>I_Sc_Max</c> ist die Grenze, ab der das Gerät Schaden
        /// nimmt — die Auslegungshilfe darf nicht mehr Stränge vorschlagen, als das
        /// Gerät verträgt. Ohne <c>I_Sc_Max</c> bleibt es bei <c>I_Dc_Max</c> wie
        /// bisher. Der Deckel aus P5 (<c>Straenge_Je_Mppt</c>) gilt zusätzlich.</para>
        /// </summary>
        public static int? ParallelJeMppt(PhotovoltaikModel modul, WechselrichterModel geraet,
                                          double tHeiss)
        {
            if (modul == null || geraet == null) return null;
            double? js = StrangPlausibilitaet.StromJeStrang(modul, tHeiss);
            double? grenze = Gesetzt(geraet.m_I_Sc_Max) ? geraet.m_I_Sc_Max : geraet.m_I_Dc_Max;
            int? p = null;
            if (js.HasValue && js.Value > 0.0 && Gesetzt(grenze))
                p = Math.Max(0, (int)Math.Floor(grenze.Value / js.Value + 1e-9));
            if (geraet.m_Straenge_Je_Mppt.HasValue && geraet.m_Straenge_Je_Mppt.Value >= 1)
                p = p.HasValue ? Math.Min(p.Value, geraet.m_Straenge_Je_Mppt.Value) : geraet.m_Straenge_Je_Mppt.Value;
            return p;
        }

        // -----------------------------------------------------------------
        //  Module je Gerät (P6, P7)
        // -----------------------------------------------------------------

        public static Modulbereich ModuleJeGeraet(PhotovoltaikModel modul, WechselrichterModel geraet)
        {
            var m = new Modulbereich();
            if (modul == null || geraet == null || modul.m_Leistung <= 0.0) return m;
            double wattJeModul = modul.m_Leistung;

            if (Gesetzt(geraet.m_P_AC_Nenn))
            {
                m.Min = (int)Math.Ceiling(StrangPlausibilitaet.DCAC_MIN * geraet.m_P_AC_Nenn.Value * 1000.0 / wattJeModul - 1e-9);
                m.Max = (int)Math.Floor(StrangPlausibilitaet.DCAC_MAX * geraet.m_P_AC_Nenn.Value * 1000.0 / wattJeModul + 1e-9);
                m.Pruefbar = true;
            }
            if (Gesetzt(geraet.m_P_DC_Max))
            {
                int ausDc = (int)Math.Floor(geraet.m_P_DC_Max.Value * 1000.0 / wattJeModul + 1e-9);
                m.Max = m.Max.HasValue ? Math.Min(m.Max.Value, ausDc) : ausDc;
                m.Pruefbar = true;
            }
            return m;
        }

        // -----------------------------------------------------------------
        //  Die ganze Aufteilung
        // -----------------------------------------------------------------

        /// <summary>
        /// Eine Aufteilung für <paramref name="anzahlModule"/> Module: möglichst wenige Geräte,
        /// darunter das DC/AC-Verhältnis nächst der Bandmitte, darunter die längste Reihe
        /// (höhere Spannung, kleinerer Strom). Alle Stränge gleich lang, alle Geräte gleich
        /// belegt — die Aufteilung, die ein Planer von Hand wählen würde.
        /// </summary>
        public static Vorschlag Vorschlagen(PhotovoltaikModel modul, WechselrichterModel geraet, int anzahlModule)
        {
            var v = new Vorschlag();
            if (modul == null || geraet == null) { v.Grund = "Modul oder Gerät fehlt."; return v; }
            if (anzahlModule <= 0) { v.Grund = "Keine Module."; return v; }

            Reihenbereich rb = Reihe(modul, geraet);
            if (!rb.Pruefbar) { v.Grund = "Spannungswerte des Moduls oder Grenzen des Geräts fehlen."; return v; }
            if (!rb.Moeglich) { v.Grund = "Keine Reihe passt zu diesem Gerät (Spannungsfenster)."; return v; }

            Modulbereich mb = ModuleJeGeraet(modul, geraet);
            int? pMax = ParallelJeMppt(modul, geraet);
            int mppts = geraet.m_Anzahl_Mppt.HasValue && geraet.m_Anzahl_Mppt.Value >= 1 ? geraet.m_Anzahl_Mppt.Value : 1;
            int obereReihe = rb.Max ?? anzahlModule;

            Vorschlag best = null;
            for (int reihe = Math.Min(obereReihe, anzahlModule); reihe >= rb.Min; reihe--)
            {
                if (anzahlModule % reihe != 0) continue;
                int straenge = anzahlModule / reihe;
                int jeGeraetMax = pMax.HasValue ? pMax.Value * mppts : straenge;
                if (jeGeraetMax <= 0) continue;

                for (int geraete = 1; geraete <= straenge; geraete++)
                {
                    if (straenge % geraete != 0) continue;
                    int p = straenge / geraete;
                    if (p > jeGeraetMax) continue;
                    int moduleJeGeraet = reihe * p;
                    if (mb.Pruefbar)
                    {
                        if (mb.Min.HasValue && moduleJeGeraet < mb.Min.Value) continue;
                        if (mb.Max.HasValue && moduleJeGeraet > mb.Max.Value) continue;
                    }
                    double dcac = Gesetzt(geraet.m_P_AC_Nenn)
                        ? moduleJeGeraet * modul.m_Leistung / 1000.0 / geraet.m_P_AC_Nenn.Value
                        : 0.0;
                    var k = new Vorschlag
                    {
                        Moeglich = true, Reihe = reihe, Parallel = p, Geraete = geraete,
                        Straenge = straenge, DcAc = dcac
                    };
                    if (Besser(k, best)) best = k;
                    break;   // für diese Reihe ist die kleinste Gerätezahl gefunden
                }
            }
            if (best == null) { v.Grund = "Die Modulzahl lässt sich nicht in gleich lange Stränge und gleich belegte Geräte teilen."; return v; }
            return best;
        }

        private static bool Besser(Vorschlag k, Vorschlag best)
        {
            if (best == null) return true;
            if (k.Geraete != best.Geraete) return k.Geraete < best.Geraete;
            double ak = Math.Abs(k.DcAc - DCAC_MITTE), ab = Math.Abs(best.DcAc - DCAC_MITTE);
            if (Math.Abs(ak - ab) > 1e-9) return ak < ab;
            return k.Reihe > best.Reihe;
        }

        /// <summary>
        /// <b>Der Vorschlag als TABELLE</b> — je Gerät und MPP-Tracker eine Zeile
        /// (Anwenderwunsch 08.09.2026, <b>W6‑B‑8</b>: der Knopf „Auslegung vorschlagen"
        /// füllt damit die Strangtabelle der Maske).
        ///
        /// <para><b>Gleichmäßig auf die Tracker.</b> <see cref="Vorschlag.Parallel"/>
        /// nennt die Stränge JE GERÄT; sie verteilen sich auf die
        /// <paramref name="mppts"/> Tracker des Geräts so gleichmäßig wie möglich —
        /// die ersten Tracker bekommen einen Strang mehr, wenn die Zahl nicht aufgeht.
        /// Damit hält die Aufteilung die Grenze aus <see cref="ParallelJeMppt"/> von
        /// selbst ein: <see cref="Vorschlagen"/> hat bereits geprüft, dass
        /// <c>Parallel ≤ ParallelJeMppt · mppts</c> gilt, und die größte Belegung eines
        /// Trackers ist danach ⌈Parallel ÷ mppts⌉ — also höchstens
        /// <c>ParallelJeMppt</c>. Ein zusätzlicher Deckel wäre eine zweite Wahrheit.</para>
        ///
        /// <para><b>Kein leerer Tracker.</b> Mehr Tracker als Stränge lassen die
        /// überzähligen unbelegt: Ein Gerät mit vier Trackern und einem Strang bekommt
        /// EINE Zeile, nicht vier. Zeilen ohne Modul hätte die Ampel sonst als „Werte
        /// fehlen: Module in Reihe" zu melden.</para>
        /// </summary>
        /// <param name="vorschlag">Das Ergebnis von <see cref="Vorschlagen"/>.</param>
        /// <param name="mppts">Zahl der MPP-Tracker des Geräts; kleiner als 1 gilt als 1.</param>
        /// <returns>
        /// Die Zeilen in Lesereihenfolge (Gerät 1 Tracker 1, Gerät 1 Tracker 2, …);
        /// leer, wenn es keinen Vorschlag gibt.
        /// </returns>
        public static List<Strangvorgabe> Aufteilen(Vorschlag vorschlag, int mppts)
        {
            var zeilen = new List<Strangvorgabe>();
            if (vorschlag == null || !vorschlag.Moeglich) return zeilen;
            if (vorschlag.Geraete < 1 || vorschlag.Parallel < 1 || vorschlag.Reihe < 1) return zeilen;

            int tracker = mppts >= 1 ? mppts : 1;
            if (tracker > vorschlag.Parallel) tracker = vorschlag.Parallel;

            int grund = vorschlag.Parallel / tracker;
            int rest = vorschlag.Parallel % tracker;

            for (int geraet = 1; geraet <= vorschlag.Geraete; geraet++)
                for (int m = 1; m <= tracker; m++)
                    zeilen.Add(new Strangvorgabe
                    {
                        Geraetenummer = geraet,
                        Mppt = m,
                        ModuleReihe = vorschlag.Reihe,
                        StraengeParallel = grund + (m <= rest ? 1 : 0)
                    });

            return zeilen;
        }

        /// <summary>Alle Geräte eines Katalogs für ein Modulfeld, beste zuerst; unpassende am Ende.</summary>
        public static List<Bewertung> GeraeteBewerten(PhotovoltaikModel modul, int anzahlModule,
                                                      IEnumerable<WechselrichterModel> katalog)
        {
            var liste = new List<Bewertung>();
            if (katalog == null) return liste;
            foreach (WechselrichterModel g in katalog)
            {
                if (g == null) continue;
                Vorschlag v = Vorschlagen(modul, g, anzahlModule);
                liste.Add(new Bewertung
                {
                    Geraet = g, Vorschlag = v,
                    Abstand = v.Moeglich ? Math.Abs(v.DcAc - DCAC_MITTE) : double.MaxValue
                });
            }
            liste.Sort((a, b) =>
            {
                if (a.Vorschlag.Moeglich != b.Vorschlag.Moeglich) return a.Vorschlag.Moeglich ? -1 : 1;
                if (a.Vorschlag.Moeglich && a.Vorschlag.Geraete != b.Vorschlag.Geraete)
                    return a.Vorschlag.Geraete.CompareTo(b.Vorschlag.Geraete);
                int c = a.Abstand.CompareTo(b.Abstand);
                return c != 0 ? c : string.Compare(a.Geraet.m_szName, b.Geraet.m_szName, StringComparison.CurrentCulture);
            });
            return liste;
        }

        // -----------------------------------------------------------------
        //  Die Sätze der Ampel (Kultur des Anwenders, wie StrangPlausibilitaet)
        // -----------------------------------------------------------------

        /// <summary>„passend wären 4…14 Module in Reihe" — bei den Vorgabetemperaturen.</summary>
        public static string ReiheEmpfehlung(PhotovoltaikModel modul, WechselrichterModel geraet)
        {
            return ReiheEmpfehlung(modul, geraet, StrangPlausibilitaet.T_KALT, StrangPlausibilitaet.T_HEISS);
        }

        /// <summary>
        /// „passend wären 4…14 Module in Reihe" bei den AUSLEGUNGSTEMPERATUREN des
        /// Projekts (<b>W6‑B‑11</b>) — leer, wenn nichts prüfbar ist.
        /// </summary>
        public static string ReiheEmpfehlung(PhotovoltaikModel modul, WechselrichterModel geraet,
                                             double tKalt, double tHeiss)
        {
            Reihenbereich r = Reihe(modul, geraet, tKalt, tHeiss);
            if (!r.Pruefbar) return "";
            if (!r.Moeglich) return MyResource.Resource.PVS_EMPF_REIHE_KEINE;
            if (r.Max.HasValue)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_EMPF_REIHE,
                                     Ganz(r.Min), Ganz(r.Max.Value));
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_EMPF_REIHE_MIN, Ganz(r.Min));
        }

        /// <summary>„passend wären 10…13 Module je Gerät" — leer, wenn nichts prüfbar ist.</summary>
        public static string GeraetEmpfehlung(PhotovoltaikModel modul, WechselrichterModel geraet)
        {
            Modulbereich m = ModuleJeGeraet(modul, geraet);
            if (!m.Pruefbar) return "";
            if (!m.Moeglich || (m.Max.HasValue && m.Max.Value < 1)) return MyResource.Resource.PVS_EMPF_GERAET_KEINE;
            if (m.Min.HasValue && m.Max.HasValue)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_EMPF_GERAET,
                                     Ganz(m.Min.Value), Ganz(m.Max.Value));
            if (m.Max.HasValue)
                return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_EMPF_GERAET_MAX, Ganz(m.Max.Value));
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_EMPF_GERAET_MIN, Ganz(m.Min.Value));
        }

        private static bool Gesetzt(double? wert) { return wert.HasValue && wert.Value > 0.0; }
        private static string Ganz(int wert) { return wert.ToString("N0", CultureInfo.CurrentCulture); }
    }
}

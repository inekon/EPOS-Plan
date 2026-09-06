using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die AMPEL einer Strangzeile — Modul gegen Gerät, die acht Auslegungsprüfungen
    /// <b>P1 bis P8</b> aus <c>Konzept_Wechselrichter_EPOS-Plan.md</c> 4.2 (Stufe S2,
    /// Anwenderentscheid <b>W6‑E‑2</b> vom 06.09.2026).
    ///
    /// <para><b>Warum hier und nicht in <c>Allgemein/Simulation/</c>.</b> Die Klasse ist
    /// die dritte Plausibilitätsprüfung der PV-Familie und steht bei ihren zwei
    /// Geschwistern: <see cref="PvModulPlausibilitaet"/> prüft einen Modulsatz,
    /// <see cref="WechselrichterPlausibilitaet"/> einen Gerätesatz, diese hier die
    /// ZUORDNUNG beider. Alle drei laufen beim BEARBEITEN, nicht beim Rechnen — die
    /// Prüfung entscheidet nichts, sie sagt etwas. Ein eigener Ordner für eine Datei
    /// wäre eine vierte Ablage für dieselbe Sache.</para>
    ///
    /// <para><b>Was sie NICHT tut: rechnen.</b> Stufe S2 hat keine Rechenwirkung; der
    /// Rechenweg der Stränge folgt mit S3. Diese Klasse liest ausschliesslich, was der
    /// Anwender eingegeben und die Kataloge gepflegt haben, und liefert Farbe und Satz.
    /// Der Referenzlauf bleibt davon unberührt.</para>
    ///
    /// <para><b>Ein fehlender Wert ist kein Fehler, aber er ist auch kein Grün.</b> Der
    /// Modulbestand ist an genau diesen Stellen nachweislich vergiftet (Paket-A-Befund
    /// A1: In allen sechs Referenzmodulen steht der Kurzschlussstrom in
    /// <c>alpha_SC</c>, <c>beta_OC</c> und <c>T_NOCT</c>), und die CEC-Liste führt weder
    /// <c>Anzahl_Mppt</c> noch <c>S_AC_Max</c> (offener Punkt <b>W6‑O‑2</b>). Eine
    /// Prüfung, die auf schlechten Daten ROT leuchtet, wird weggeklickt statt gelesen
    /// (Konzept 4.2); eine, die auf fehlenden Daten GRÜN leuchtet, behauptet etwas.
    /// Deshalb: <b>Die Prüfung entfällt, der Strang wird gelb, und der Satz sagt, welche
    /// Angabe fehlt.</b></para>
    ///
    /// <para><b>Eine Näherung, ausdrücklich benannt.</b> Der Modulkatalog führt keinen
    /// eigenen Temperaturkoeffizienten für die MPP-Spannung. P2 und P3 setzen dafür
    /// <c>beta_OC</c> ein — die Auslegungspraxis tut dasselbe, der Fehler liegt bei
    /// wenigen Prozent und auf der sicheren Seite (Konzept 4.2). Der Satz sagt es über
    /// <see cref="Befund.NaeherungMpp"/>, damit es im Werkzeugtipp der Ampel steht und
    /// nicht nur im Protokoll.</para>
    ///
    /// <para><b>Die Zahlen stehen im Befund, nicht nur im Satz.</b> Ein Prüfstand, der
    /// Text vergleicht, prüft die Sprache; hier sind die Größen selbst nachzurechnen
    /// (Anhang A des Konzepts). Der Satz entsteht daraus.</para>
    ///
    /// <para><b>Kultur: die des Anwenders.</b> Anders als bei den zwei Geschwistern —
    /// deren Meldungen Import- und Speicherprotokolle sind und deshalb invariant
    /// formatieren — steht dieser Satz im PV-Dialog unter der Strangzeile. Er wird
    /// GELESEN, nicht verglichen, und zeigt deshalb Zahlen in der Oberflächensprache
    /// (Muster <c>PhotovoltaikStammCtrl.Parameterzeilen</c>).</para>
    /// </summary>
    public static class StrangPlausibilitaet
    {
        // =================================================================
        //  Auslegungstemperaturen und Grenzen (Konzept 4.2)
        // =================================================================

        /// <summary>Kalter Fall [°C] — höchste Spannung. Übliche Auslegungspraxis.</summary>
        public const double T_KALT = -10.0;

        /// <summary>Heisser Fall, ZELLtemperatur [°C] — niedrigste Spannung, höchster Strom.</summary>
        public const double T_HEISS = 70.0;

        /// <summary>Bezugstemperatur der Katalogwerte [°C] (STC).</summary>
        public const double T_STC = 25.0;

        /// <summary>Untere Grenze des empfohlenen DC/AC-Bandes (P6).</summary>
        public const double DCAC_MIN = 1.0;

        /// <summary>Obere Grenze des empfohlenen DC/AC-Bandes (P6).</summary>
        public const double DCAC_MAX = 1.5;

        // =================================================================
        //  Die Ampel
        // =================================================================

        /// <summary>
        /// Die drei Farben. <see cref="Gruen"/> = alle anwendbaren Prüfungen bestanden,
        /// <see cref="Gelb"/> = P3/P5/P6/P7/P8 verletzt ODER Werte fehlen,
        /// <see cref="Rot"/> = P1/P2/P4 verletzt.
        ///
        /// <para><b>Rot verhindert das Speichern NICHT</b> — ein Planer darf einen
        /// Zwischenstand ablegen (Konzept 7). Die Ampel sagt etwas, sie verhindert
        /// nichts.</para>
        /// </summary>
        public enum Ampel
        {
            /// <summary>Alle anwendbaren Prüfungen bestanden.</summary>
            Gruen = 0,

            /// <summary>Eine weiche Prüfung verletzt oder eine Angabe fehlt.</summary>
            Gelb = 1,

            /// <summary>P1, P2 oder P4 verletzt — die Auslegung ist so nicht zulässig.</summary>
            Rot = 2
        }

        /// <summary>Die schlechtere der zwei Farben.</summary>
        private static Ampel Schlechter(Ampel a, Ampel b) => a > b ? a : b;

        // =================================================================
        //  Die Eingaben
        // =================================================================

        /// <summary>
        /// Was der Prüfstand braucht — alles bereits gelesen, nichts wird hier
        /// nachgeschlagen (der Kern-Prüfteil bleibt ohne Datenbank).
        /// </summary>
        public sealed class Gaben
        {
            /// <summary>Die Strangzeilen der Anlage in Rangfolge; <c>null</c> = keine.</summary>
            public IReadOnlyList<AnlageStrangModel> Straenge;

            /// <summary>
            /// Das Modul der ANLAGE (Projektkopie <c>Tab_PV</c>); <c>null</c> = unbekannt,
            /// dann sind P1 bis P4 nicht prüfbar.
            /// </summary>
            public PhotovoltaikModel Modul;

            /// <summary>
            /// Die Projektkopien der zugeordneten Wechselrichter, je
            /// <c>Tab_Wechselrichter.ID</c>; ein fehlender Eintrag heisst „Gerät
            /// unbekannt".
            /// </summary>
            public IReadOnlyDictionary<int, WechselrichterModel> Geraete;

            /// <summary>
            /// „Anzahl Module" der Anlagenzeile (<c>Tab_Energieanlagen.PV_Leistung</c>)
            /// — die Bezugsgrösse von P8.
            /// </summary>
            public double AnzahlModuleAnlage;
        }

        // =================================================================
        //  Die Ergebnisse
        // =================================================================

        /// <summary>Der Befund EINER Strangzeile.</summary>
        public sealed class Strangbefund
        {
            /// <summary>Rang der Zeile (1…n).</summary>
            public int Rang;

            /// <summary>Farbe dieser Zeile.</summary>
            public Ampel Farbe;

            /// <summary>Der Satz unter der Zeile — fertig, in der Oberflächensprache.</summary>
            public string Satz = "";

            /// <summary>P1: Leerlaufspannung des Strangs bei −10 °C [V]; <c>null</c> = nicht prüfbar.</summary>
            public double? UocKalt;

            /// <summary>P2: MPP-Spannung des Strangs bei 70 °C [V]; <c>null</c> = nicht prüfbar.</summary>
            public double? UmppHeiss;

            /// <summary>P3: MPP-Spannung des Strangs bei −10 °C [V]; <c>null</c> = nicht prüfbar.</summary>
            public double? UmppKalt;

            /// <summary>Module dieses Strangs (Reihe × parallel).</summary>
            public int Modulzahl;

            /// <summary>Nennleistung dieses Strangs [kWp]; 0 ohne Modulwert.</summary>
            public double Kwp;
        }

        /// <summary>Der Befund EINES MPP-Trackers eines Geräts (P4 und P5).</summary>
        public sealed class Mpptbefund
        {
            /// <summary>Nummer des Trackers (1…n).</summary>
            public int Mppt;

            /// <summary>Summe der parallelen Stränge an diesem Tracker.</summary>
            public int Straenge;

            /// <summary>P4: Eingangsstrom bei 70 °C [A]; <c>null</c> = nicht prüfbar.</summary>
            public double? Strom;
        }

        /// <summary>Der Befund EINES physischen Geräts (P6 und P7, dazu seine Tracker).</summary>
        public sealed class Geraetebefund
        {
            /// <summary>Die Projektkopie des Geräts; <c>null</c> = kein Gerät zugeordnet.</summary>
            public int? ID_Wechselrichter;

            /// <summary>Anzeigename des Geräts; leer, wenn keins zugeordnet ist.</summary>
            public string Bezeichner = "";

            /// <summary>Welches physische Gerät dieses Typs (1…n).</summary>
            public int Geraetenummer;

            /// <summary>Summe der Strang-Nennleistungen an diesem Gerät [kWp].</summary>
            public double Kwp;

            /// <summary>P6: <c>Kwp / P_AC_Nenn</c>; <c>null</c> = nicht prüfbar.</summary>
            public double? DcAc;

            /// <summary>Farbe dieses Geräts.</summary>
            public Ampel Farbe;

            /// <summary>Der Satz im Kopf des Abschnitts — fertig.</summary>
            public string Satz = "";

            /// <summary>Die Tracker dieses Geräts, nach Nummer.</summary>
            public List<Mpptbefund> Mppts = new List<Mpptbefund>();
        }

        /// <summary>Das Ergebnis der Prüfung einer ganzen Anlage.</summary>
        public sealed class Befund
        {
            /// <summary>Je Strangzeile ein Eintrag, in Rangfolge.</summary>
            public List<Strangbefund> Straenge = new List<Strangbefund>();

            /// <summary>Je physischem Gerät ein Eintrag.</summary>
            public List<Geraetebefund> Geraete = new List<Geraetebefund>();

            /// <summary>Die schlechteste Farbe über alles.</summary>
            public Ampel Farbe;

            /// <summary>Summe aus Reihe × parallel über alle Stränge — die abgeleitete „Anzahl Module".</summary>
            public int Modulsumme;

            /// <summary>Nennleistung aller Stränge [kWp].</summary>
            public double Kwp;

            /// <summary>
            /// P8: Stimmt die Modulsumme mit dem Anlagenwert überein? <c>false</c> färbt
            /// gelb. Ohne Strangzeile ist die Frage sinnlos und die Antwort <c>true</c>.
            /// </summary>
            public bool ModulsummeStimmt = true;

            /// <summary>
            /// Der Satz zur Näherung von P2/P3 (<c>beta_OC</c> statt eines eigenen
            /// MPP-Koeffizienten) — für den Werkzeugtipp der Ampel.
            /// </summary>
            public string NaeherungMpp = "";
        }

        // =================================================================
        //  Der Prüflauf
        // =================================================================

        /// <summary>
        /// Prüft die Strangzuordnung einer Anlage. Ohne Strangzeile ist der Befund leer
        /// und grün — dann rechnet die Anlage wie bisher.
        /// </summary>
        public static Befund Pruefe(Gaben gaben)
        {
            var b = new Befund();
            if (gaben == null || gaben.Straenge == null || gaben.Straenge.Count == 0) return b;

            b.NaeherungMpp = MyResource.Resource.PVS_NAEHERUNG_MPP;

            foreach (AnlageStrangModel s in gaben.Straenge)
            {
                if (s == null) continue;
                b.Straenge.Add(StrangPruefen(s, gaben, b));
                b.Modulsumme += s.Modulzahl;
                b.Kwp += StrangKwp(s, gaben.Modul);
            }

            GeraetePruefen(gaben, b);
            ModulsummePruefen(gaben, b);

            foreach (Strangbefund s in b.Straenge) b.Farbe = Schlechter(b.Farbe, s.Farbe);
            foreach (Geraetebefund g in b.Geraete) b.Farbe = Schlechter(b.Farbe, g.Farbe);
            return b;
        }

        // -----------------------------------------------------------------
        //  P1 bis P3 — je Strang
        // -----------------------------------------------------------------

        private static Strangbefund StrangPruefen(AnlageStrangModel s, Gaben gaben, Befund b)
        {
            var sb = new Strangbefund
            {
                Rang = s.Rang,
                Modulzahl = s.Modulzahl,
                Kwp = StrangKwp(s, gaben.Modul)
            };

            var teile = new List<string>();
            var fehlt = new List<string>();

            int reihe = s.Module_Reihe ?? 0;
            if (reihe <= 0) fehlt.Add(MyResource.Resource.PVS_FEHLT_REIHE);

            WechselrichterModel g = Geraet(s, gaben);
            if (g == null) fehlt.Add(MyResource.Resource.PVS_FEHLT_GERAET);

            teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P_MODULE,
                                    Ganz(reihe), Ganz(s.ParallelOderEins)));

            // --- P1: Leerlaufspannung im kalten Fall -> ROT ---------------------------
            double? uoc = SpannungReihe(reihe, gaben.Modul?.m_U_Leerlauf, gaben.Modul?.m_beta_OC, T_KALT);
            sb.UocKalt = uoc;

            if (!uoc.HasValue) FehltEinmal(fehlt, gaben.Modul, MyResource.Resource.PVS_FEHLT_UOC);
            else if (g != null && Gesetzt(g.m_U_Dc_Max))
            {
                if (uoc.Value > g.m_U_Dc_Max.Value)
                {
                    sb.Farbe = Schlechter(sb.Farbe, Ampel.Rot);
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P1_ROT,
                                            Z(uoc.Value, 0), Z(g.m_U_Dc_Max.Value, 0)));
                }
                else
                {
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P1,
                                            Z(uoc.Value, 0), Z(g.m_U_Dc_Max.Value, 0)));
                }
            }

            // --- P2 und P3: das MPP-Fenster ------------------------------------------
            double? heiss = SpannungReihe(reihe, gaben.Modul?.m_U_Mpp, gaben.Modul?.m_beta_OC, T_HEISS);
            double? kalt = SpannungReihe(reihe, gaben.Modul?.m_U_Mpp, gaben.Modul?.m_beta_OC, T_KALT);
            sb.UmppHeiss = heiss;
            sb.UmppKalt = kalt;

            if (!heiss.HasValue) FehltEinmal(fehlt, gaben.Modul, MyResource.Resource.PVS_FEHLT_UMPP);
            else if (g != null && (Gesetzt(g.m_U_Mpp_Min) || Gesetzt(g.m_U_Mpp_Max)))
            {
                bool p2Verletzt = Gesetzt(g.m_U_Mpp_Min) && heiss.Value < g.m_U_Mpp_Min.Value;
                bool p3Verletzt = Gesetzt(g.m_U_Mpp_Max) && kalt.HasValue && kalt.Value > g.m_U_Mpp_Max.Value;

                if (p2Verletzt)
                {
                    sb.Farbe = Schlechter(sb.Farbe, Ampel.Rot);
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P2_ROT,
                                            Z(heiss.Value, 0), Z(g.m_U_Mpp_Min.Value, 0)));
                }

                if (p3Verletzt)
                {
                    sb.Farbe = Schlechter(sb.Farbe, Ampel.Gelb);
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P3_GELB,
                                            Z(kalt.Value, 0), Z(g.m_U_Mpp_Max.Value, 0)));
                }

                if (!p2Verletzt && !p3Verletzt && kalt.HasValue)
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P23,
                                            Z(heiss.Value, 0), Z(kalt.Value, 0),
                                            Grenze(g.m_U_Mpp_Min), Grenze(g.m_U_Mpp_Max)));
            }

            if (fehlt.Count > 0)
            {
                sb.Farbe = Schlechter(sb.Farbe, Ampel.Gelb);
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WERTE_FEHLEN,
                                        string.Join(", ", fehlt)));
            }

            sb.Satz = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_SATZ_STRANG,
                                    Ganz(s.Rang),
                                    string.IsNullOrEmpty(s.Bezeichner) ? "" : s.Bezeichner,
                                    string.Join(MyResource.Resource.PVS_TRENNER, teile));
            return sb;
        }

        // -----------------------------------------------------------------
        //  P4 bis P7 — je Gerät und MPPT
        // -----------------------------------------------------------------

        private static void GeraetePruefen(Gaben gaben, Befund b)
        {
            // Gruppierung nach (ID_Wechselrichter, Geraetenummer) - das ist die
            // Einheit, an der Clipping und Kosten haengen (Konzept 3.4, Q6).
            var reihenfolge = new List<string>();
            var gruppen = new Dictionary<string, List<AnlageStrangModel>>();

            foreach (AnlageStrangModel s in gaben.Straenge)
            {
                if (s == null) continue;
                string k = (s.ID_Wechselrichter ?? 0).ToString(CultureInfo.InvariantCulture) +
                           "/" + s.GeraetenummerOderEins.ToString(CultureInfo.InvariantCulture);
                if (!gruppen.ContainsKey(k)) { gruppen[k] = new List<AnlageStrangModel>(); reihenfolge.Add(k); }
                gruppen[k].Add(s);
            }

            foreach (string k in reihenfolge)
            {
                List<AnlageStrangModel> straenge = gruppen[k];
                AnlageStrangModel erster = straenge[0];
                WechselrichterModel g = Geraet(erster, gaben);

                var gb = new Geraetebefund
                {
                    ID_Wechselrichter = (erster.ID_Wechselrichter ?? 0) > 0 ? erster.ID_Wechselrichter : null,
                    Geraetenummer = erster.GeraetenummerOderEins,
                    Bezeichner = g?.m_szName ?? ""
                };

                foreach (AnlageStrangModel s in straenge) gb.Kwp += StrangKwp(s, gaben.Modul);

                var teile = new List<string>();

                if (g == null)
                {
                    gb.Farbe = Ampel.Gelb;
                    teile.Add(MyResource.Resource.PVS_KEIN_GERAET);
                }
                else
                {
                    MpptPruefen(straenge, g, gaben, gb, teile);
                    DcAcPruefen(g, gb, teile);
                }

                gb.Satz = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_SATZ_GERAET,
                                        gb.Bezeichner, Ganz(gb.Geraetenummer),
                                        string.Join(MyResource.Resource.PVS_TRENNER, teile));
                b.Geraete.Add(gb);
            }
        }

        /// <summary>
        /// P4 (Eingangsstrom je MPPT, rot) und P5 (Strangzahl je MPPT, gelb).
        ///
        /// <para><b>Fehlt <c>Anzahl_Mppt</c></b> — die CEC-Liste führt sie nicht,
        /// offener Punkt <b>W6‑O‑2</b> —, wird auf EINEM Tracker gerechnet: dem
        /// konservativen Fall. Der Satz sagt „Angabe fehlt", damit niemand die daraus
        /// folgende Farbe für eine Messung hält.</para>
        /// </summary>
        private static void MpptPruefen(List<AnlageStrangModel> straenge, WechselrichterModel g,
                                        Gaben gaben, Geraetebefund gb, List<string> teile)
        {
            bool mpptBekannt = g.m_Anzahl_Mppt.HasValue && g.m_Anzahl_Mppt.Value >= 1;

            var reihenfolge = new List<int>();
            var jeMppt = new Dictionary<int, int>();     // Tracker -> Summe paralleler Straenge

            foreach (AnlageStrangModel s in straenge)
            {
                int t = mpptBekannt ? s.MpptOderEins : 1;
                if (!jeMppt.ContainsKey(t)) { jeMppt[t] = 0; reihenfolge.Add(t); }
                jeMppt[t] += s.ParallelOderEins;
            }

            reihenfolge.Sort();

            double? jeStrang = StromJeStrang(gaben.Modul);
            bool p4Gemeldet = false, p5Gemeldet = false;
            double groesster = 0;

            foreach (int t in reihenfolge)
            {
                var mb = new Mpptbefund { Mppt = t, Straenge = jeMppt[t] };
                if (jeStrang.HasValue) mb.Strom = jeMppt[t] * jeStrang.Value;
                gb.Mppts.Add(mb);

                if (mb.Strom.HasValue && mb.Strom.Value > groesster) groesster = mb.Strom.Value;

                // --- P4: Eingangsstrom je MPPT -> ROT --------------------------------
                if (mb.Strom.HasValue && Gesetzt(g.m_I_Dc_Max) && mb.Strom.Value > g.m_I_Dc_Max.Value)
                {
                    gb.Farbe = Schlechter(gb.Farbe, Ampel.Rot);
                    if (!p4Gemeldet)
                    {
                        teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P4_ROT,
                                                Z(mb.Strom.Value, 2), Z(g.m_I_Dc_Max.Value, 1), Ganz(t)));
                        p4Gemeldet = true;
                    }
                }

                // --- P5: Strangzahl je MPPT -> GELB ----------------------------------
                if (g.m_Straenge_Je_Mppt.HasValue && g.m_Straenge_Je_Mppt.Value >= 1 &&
                    mb.Straenge > g.m_Straenge_Je_Mppt.Value)
                {
                    gb.Farbe = Schlechter(gb.Farbe, Ampel.Gelb);
                    if (!p5Gemeldet)
                    {
                        teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P5_GELB,
                                                Ganz(mb.Straenge), Ganz(t), Ganz(g.m_Straenge_Je_Mppt.Value)));
                        p5Gemeldet = true;
                    }
                }
            }

            if (!jeStrang.HasValue)
            {
                gb.Farbe = Schlechter(gb.Farbe, Ampel.Gelb);
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WERTE_FEHLEN,
                                        MyResource.Resource.PVS_FEHLT_ISC));
            }
            else if (!p4Gemeldet && Gesetzt(g.m_I_Dc_Max))
            {
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P4,
                                        Z(groesster, 2), Z(g.m_I_Dc_Max.Value, 1)));
            }

            if (!mpptBekannt)
            {
                gb.Farbe = Schlechter(gb.Farbe, Ampel.Gelb);
                teile.Add(MyResource.Resource.PVS_FEHLT_MPPT);
            }
        }

        /// <summary>P6 (DC/AC-Verhältnis, gelb) und P7 (DC-Eingangsleistung, gelb).</summary>
        private static void DcAcPruefen(WechselrichterModel g, Geraetebefund gb, List<string> teile)
        {
            if (!Gesetzt(g.m_P_AC_Nenn))
            {
                gb.Farbe = Schlechter(gb.Farbe, Ampel.Gelb);
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_WERTE_FEHLEN,
                                        MyResource.Resource.PVS_FEHLT_P_AC));
            }
            else if (gb.Kwp > 0.0)
            {
                gb.DcAc = gb.Kwp / g.m_P_AC_Nenn.Value;

                if (gb.DcAc.Value < DCAC_MIN || gb.DcAc.Value > DCAC_MAX)
                {
                    gb.Farbe = Schlechter(gb.Farbe, Ampel.Gelb);
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P6_GELB,
                                            Z(gb.DcAc.Value, 2), Z(DCAC_MIN, 1), Z(DCAC_MAX, 1)));
                }
                else
                {
                    teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P6,
                                            Z(gb.DcAc.Value, 2)));
                }
            }

            // --- P7: DC-Eingangsleistung -> GELB -------------------------------------
            if (Gesetzt(g.m_P_DC_Max) && gb.Kwp > g.m_P_DC_Max.Value)
            {
                gb.Farbe = Schlechter(gb.Farbe, Ampel.Gelb);
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P7_GELB,
                                        Z(gb.Kwp, 3), Z(g.m_P_DC_Max.Value, 2)));
            }
        }

        // -----------------------------------------------------------------
        //  P8 — die Modulsumme gegen den Anlagenwert
        // -----------------------------------------------------------------

        /// <summary>
        /// P8: Die Summe aus Reihe × parallel muss der „Anzahl Module" der Anlage
        /// entsprechen — gelb, wenn nicht.
        ///
        /// <para><b>Seit Entscheidungsfrage Q9 ist das eine Zusicherung, keine
        /// Warnung:</b> Die Oberfläche LEITET „Anzahl Module" aus der Strangtabelle ab
        /// und schreibt den Anlagenwert mit, sobald ein Strang besteht. P8 darf deshalb
        /// nur noch anschlagen, wenn ein Bestand von Hand auseinandergelaufen ist — und
        /// genau dafür gibt es sie.</para>
        /// </summary>
        private static void ModulsummePruefen(Gaben gaben, Befund b)
        {
            if (b.Modulsumme <= 0) return;

            int anlage = (int)Math.Round(gaben.AnzahlModuleAnlage, MidpointRounding.AwayFromZero);
            if (anlage == b.Modulsumme) return;

            b.ModulsummeStimmt = false;

            // Die Meldung haengt an der ANLAGE, nicht an einem Strang - sie steht
            // deshalb am ersten Strangbefund, wo der Anwender sie liest.
            if (b.Straenge.Count == 0) return;

            Strangbefund erster = b.Straenge[0];
            erster.Farbe = Schlechter(erster.Farbe, Ampel.Gelb);
            erster.Satz += MyResource.Resource.PVS_TRENNER +
                           string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P8_GELB,
                                         Ganz(b.Modulsumme), Ganz(anlage));
        }

        // =================================================================
        //  Rechenhilfen
        // =================================================================

        /// <summary>
        /// Die Spannung eines Strangs bei <paramref name="temperatur"/> [V], oder
        /// <c>null</c>, wenn eine Angabe fehlt.
        ///
        /// <para><c>U(T) = Reihe · [U_STC + beta_OC · (T − 25)]</c>. <c>beta_OC</c> ist
        /// NEGATIV (V/K) — im kalten Fall steigt die Spannung deshalb.</para>
        /// </summary>
        public static double? SpannungReihe(int reihe, double? uStc, double? betaOc, double temperatur)
        {
            if (reihe <= 0 || !Gesetzt(uStc) || !Gesetzt(betaOc)) return null;
            return reihe * (uStc.Value + betaOc.Value * (temperatur - T_STC));
        }

        /// <summary>
        /// Der Kurzschlussstrom EINES Strangs im heissen Fall [A], oder <c>null</c>.
        /// <para><c>I(70 °C) = I_sc + alpha_SC · (70 − 25)</c>; <c>alpha_SC</c> ist
        /// positiv (A/K).</para>
        /// </summary>
        public static double? StromJeStrang(PhotovoltaikModel modul)
        {
            if (modul == null || !Gesetzt(modul.m_I_Kurzschluss) || !Gesetzt(modul.m_alpha_SC)) return null;
            return modul.m_I_Kurzschluss + modul.m_alpha_SC * (T_HEISS - T_STC);
        }

        /// <summary>Die Nennleistung eines Strangs [kWp]; 0 ohne Modulleistung.</summary>
        public static double StrangKwp(AnlageStrangModel s, PhotovoltaikModel modul)
        {
            if (s == null || modul == null || modul.m_Leistung <= 0.0) return 0.0;
            return s.Modulzahl * modul.m_Leistung / 1000.0;
        }

        private static WechselrichterModel Geraet(AnlageStrangModel s, Gaben gaben)
        {
            int id = s.ID_Wechselrichter ?? 0;
            if (id <= 0 || gaben.Geraete == null) return null;
            WechselrichterModel g;
            return gaben.Geraete.TryGetValue(id, out g) ? g : null;
        }

        /// <summary>
        /// Ein Katalogwert gilt als GESETZT, wenn er belegt und von null verschieden ist
        /// — „0 oder NULL" heisst beides „nicht gepflegt" (Konzept 4.2).
        /// </summary>
        private static bool Gesetzt(double? wert) => wert.HasValue && Math.Abs(wert.Value) > 1e-12;

        /// <summary>Dasselbe für eine Modulzahl, die als <c>double</c> im Modell steht.</summary>
        private static bool Gesetzt(double wert) => Math.Abs(wert) > 1e-12;

        /// <summary>
        /// Nimmt eine fehlende MODULangabe genau EINMAL auf: Fehlt das Modul ganz, ist
        /// nicht die einzelne Spalte der Befund, sondern das Modul.
        /// </summary>
        private static void FehltEinmal(List<string> fehlt, PhotovoltaikModel modul, string spalte)
        {
            string text = modul == null ? MyResource.Resource.PVS_FEHLT_MODUL : spalte;
            if (!fehlt.Contains(text)) fehlt.Add(text);
        }

        private static string Grenze(double? wert)
            => Gesetzt(wert) ? Z(wert.Value, 0) : MyResource.Resource.PVS_OHNE_GRENZE;

        private static string Z(double wert, int stellen)
            => wert.ToString("N" + stellen.ToString(CultureInfo.InvariantCulture),
                             CultureInfo.CurrentCulture);

        private static string Ganz(int wert)
            => wert.ToString(CultureInfo.CurrentCulture);
    }
}

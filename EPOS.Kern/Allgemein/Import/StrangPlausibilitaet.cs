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

        /// <summary>
        /// Kalter Fall [°C] — höchste Spannung. Übliche Auslegungspraxis und die
        /// <b>VORGABE</b> des Projektparameters (<see cref="Gaben.TKalt"/>,
        /// <b>W6‑B‑11</b>, Anwenderentscheid vom 09.09.2026).
        /// </summary>
        public const double T_KALT = -10.0;

        /// <summary>
        /// Heisser Fall, ZELLtemperatur [°C] — niedrigste Spannung, höchster Strom;
        /// die <b>VORGABE</b> des Projektparameters (<see cref="Gaben.THeiss"/>,
        /// <b>W6‑B‑11</b>).
        /// </summary>
        public const double T_HEISS = 70.0;

        /// <summary>
        /// <b>Der Rückfall von P1, wenn <c>beta_OC</c> fehlt</b> — Anwenderentscheid
        /// <b>W6‑B‑9</b> vom 09.09.2026 (Vorschlag V4 des Prüfberichts vom 08.09.2026,
        /// offener Punkt <b>O‑4</b>).
        ///
        /// <para><b>Woher die Zahl kommt.</b> Die Auslegungspraxis für kristallines
        /// Silizium rechnet die Leerlaufspannung im kalten Fall ersatzweise als
        /// <c>1,15 · U_oc,STC</c>, wenn kein Temperaturkoeffizient vorliegt (IEC 62548
        /// bzw. DIN VDE 0100-712 in ihrer verbreiteten Anwendung; für sehr kalte
        /// Standorte ist 1,25 gebräuchlich). Sie ersetzt die Koeffizientenrechnung
        /// nicht — sie ist der Wert, mit dem man rechnet, solange der Koeffizient
        /// fehlt.</para>
        ///
        /// <para><b>Warum überhaupt ein Rückfall.</b> P1 ist die einzige
        /// zerstörungsrelevante Prüfung, und der Modulbestand ist an genau dieser
        /// Stelle nachweislich lückenhaft (Paket-A-Befund A1). Ohne Rückfall entfiel
        /// P1 im Regelfall ganz und der Strang wurde nur gelb — die Ampel schwieg
        /// also genau dort, wo ein Gerät Schaden nehmen kann. Der Satz sagt
        /// ausdrücklich, dass ohne Koeffizient gerechnet wurde
        /// (<c>PVS_P1_OHNE_BETA</c>), damit niemand die Zahl für eine Messung
        /// hält.</para>
        /// </summary>
        public const double FAKTOR_UOC_OHNE_KOEFFIZIENT = 1.15;

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
            /// Das Modul der ANLAGE, also der GEWÄHLTEN Projektzeile (Projektkopie
            /// <c>Tab_PV</c>); <c>null</c> = unbekannt, dann sind P1 bis P4 nicht prüfbar.
            ///
            /// <para><b>Anwenderentscheid W6‑O‑5 vom 06.09.2026 („Modul der gewählten
            /// Zeile"):</b> Bis dahin gab die Hülle das ERSTE Modul herein, das der
            /// Katalog kannte. Führt ein Projekt mehrere PV-Zeilen mit verschiedenen
            /// Modulen, prüfte die Ampel damit gegen das falsche. Welche Zeile gewählt
            /// ist, weiß nur die Oberfläche — sie sagt es jetzt.</para>
            /// </summary>
            public PhotovoltaikModel Modul;

            /// <summary>
            /// Die ABWEICHENDEN Modultypen der Stränge je <c>Tab_PV.ID</c>
            /// (<c>Z_AnlageStrang.ID_PV</c>) — Anwenderentscheid <b>W6‑O‑6</b>: „jeder
            /// Strang mit nur einem Modultyp, unterschiedliche Stränge können jeweils
            /// einen anderen Modultyp haben."
            ///
            /// <para><c>null</c>, ein fehlender Eintrag und <c>ID_PV</c> = NULL sind
            /// DERSELBE Fall: Der Strang prüft gegen <see cref="Modul"/>. Damit ändert
            /// sich für eine Anlage mit einem Modultyp — den Regelfall — nichts.</para>
            /// </summary>
            public IReadOnlyDictionary<int, PhotovoltaikModel> Module;

            /// <summary>
            /// Die Projektkopien der zugeordneten Wechselrichter, je
            /// <c>Tab_Wechselrichter.ID</c>; ein fehlender Eintrag heisst „Gerät
            /// unbekannt".
            /// </summary>
            public IReadOnlyDictionary<int, WechselrichterModel> Geraete;

            /// <summary>
            /// „Anzahl Module" der Anlagenzeile (<c>Tab_Energieanlagen.PV_Leistung</c>)
            /// — die Bezugsgrösse von P8.
            ///
            /// <para><b>Der GESPEICHERTE Anlagenwert, nicht die abgeleitete Summe</b>
            /// (<b>W6‑B‑12</b>, Anwenderentscheid vom 09.09.2026; Befund A11 des
            /// Prüfberichts, offener Punkt <b>O‑8</b>). Bis dahin gab der einzige
            /// Aufrufer — die PV-Hülle — dieselbe Summe herein, aus der der Kern seine
            /// <see cref="Befund.Modulsumme"/> bildet; der Vergleich war damit immer
            /// erfüllt und P8 unerreichbar. Seither trifft P8 genau den Fall, für den
            /// sie gedacht ist: einen ALTBESTAND, dessen Anlagenwert und Strangtabelle
            /// auseinandergelaufen sind. Der Q9-Abgleich der Maske (jede Änderung
            /// schreibt die Summe in den Anlagenwert zurück) macht die Meldung im
            /// nächsten Zug wieder still.</para>
            /// </summary>
            public double AnzahlModuleAnlage;

            /// <summary>
            /// <b>Auslegungstemperatur kalt [°C]</b> — Projektparameter
            /// <c>Tab_Einstellungen.Ausleg_T_Kalt</c> (<b>W6‑B‑11</b>,
            /// Anwenderentscheid vom 09.09.2026; Vorschlag V7 des Prüfberichts,
            /// offener Punkt <b>O‑3</b>). <c>null</c> = die Vorgabe
            /// <see cref="T_KALT"/>.
            ///
            /// <para><b>Warum ein Parameter und keine Konstante mehr.</b> IEC 62548
            /// verlangt die am STANDORT niedrigste zu erwartende Temperatur, nicht
            /// eine feste Zahl; −10 °C ist eine für Deutschland verbreitete
            /// Planungsannahme und für das Alpenvorland knapp. Wer den Wert setzt,
            /// verschiebt P1 und P3 — und nur diese beiden.</para>
            /// </summary>
            public double? TKalt;

            /// <summary>
            /// <b>Auslegungstemperatur heiss [°C], ZELLtemperatur</b> — Projektparameter
            /// <c>Tab_Einstellungen.Ausleg_T_Heiss</c> (<b>W6‑B‑11</b>). <c>null</c> =
            /// die Vorgabe <see cref="T_HEISS"/>. Sie verschiebt P2 und P4.
            /// </summary>
            public double? THeiss;

            /// <summary>Der kalte Fall, den diese Prüfung rechnet — Parameter oder Vorgabe.</summary>
            public double TKaltOderVorgabe => TKalt ?? T_KALT;

            /// <summary>Der heisse Fall, den diese Prüfung rechnet — Parameter oder Vorgabe.</summary>
            public double THeissOderVorgabe => THeiss ?? T_HEISS;
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
            /// <summary>Was passen würde (Auslegungshilfe, 08.09.2026) — leer, wenn der Strang grün ist.</summary>
            public string Empfehlung = "";

            /// <summary>P1: Leerlaufspannung des Strangs im kalten Fall [V]; <c>null</c> = nicht prüfbar.</summary>
            public double? UocKalt;

            /// <summary>
            /// P1 wurde ÜBER DEN FAKTOR gerechnet, weil <c>beta_OC</c> fehlt
            /// (<b>W6‑B‑9</b>): <c>U_oc,kalt = 1,15 · U_oc,STC</c>. Der Satz sagt es
            /// mit; hier steht es als Zahl für den Prüfstand.
            /// </summary>
            public bool UocOhneKoeffizient;

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

            /// <summary>P4: Eingangsstrom im heissen Fall [A]; <c>null</c> = nicht prüfbar.</summary>
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
            /// <summary>Was passen würde (Auslegungshilfe, 08.09.2026) — leer ohne Befund an P6/P7.</summary>
            public string Empfehlung = "";

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

            /// <summary>
            /// <b>Was die Prüfung NICHT bemisst</b> (<b>W6‑B‑9</b>, Anwenderentscheid
            /// vom 09.09.2026): P4 rechnet ausschliesslich die thermische Korrektur
            /// (<c>I_sc + alpha_SC · ΔT</c>, rund +2 %), <b>ohne</b> den in der Praxis
            /// üblichen Faktor 1,25. Dieser Faktor gehört zur Bemessung von
            /// DC-Leitungen, Sicherungen und Schaltern — und die liegt ausserhalb
            /// dieses Werkzeugs.
            ///
            /// <para>Der Satz steht im Werkzeugtipp, weil die Word-Fassung genau hier
            /// eine falsche Zusicherung machte („der 1,25-fache Kurzschlussstrom ist
            /// impliziter Bestandteil der thermischen Korrektur" — Befund A2 des
            /// Prüfberichts vom 08.09.2026). Er behauptet keine Norm; er sagt, wo die
            /// Grenze dieses Werkzeugs verläuft.</para>
            /// </summary>
            public string HinweisStrombemessung = "";

            /// <summary>
            /// Der vollständige Werkzeugtipp der Ampel: die Näherung von P2/P3 und der
            /// Hinweis zur Strombemessung, durch ein Leerzeichen getrennt. Leere Teile
            /// entfallen.
            /// </summary>
            public string Werkzeugtipp
            {
                get
                {
                    if (string.IsNullOrEmpty(NaeherungMpp)) return HinweisStrombemessung ?? "";
                    if (string.IsNullOrEmpty(HinweisStrombemessung)) return NaeherungMpp;
                    return NaeherungMpp + " " + HinweisStrombemessung;
                }
            }
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

            // W6-B-9: Was P4 NICHT tut - der Faktor 1,25 der Leitungs- und
            // Sicherungsbemessung steht ausserhalb dieses Werkzeugs.
            b.HinweisStrombemessung = MyResource.Resource.PVS_HINWEIS_ISC_125;

            foreach (AnlageStrangModel s in gaben.Straenge)
            {
                if (s == null) continue;
                b.Straenge.Add(StrangPruefen(s, gaben, b));
                b.Modulsumme += s.Modulzahl;
                b.Kwp += StrangKwp(s, ModulDesStrangs(s, gaben));
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
            // W6-O-6: SEIN Modul - das abweichende der Strangzeile, sonst das der
            // gewaehlten Projektzeile (W6-O-5).
            PhotovoltaikModel modul = ModulDesStrangs(s, gaben);

            var sb = new Strangbefund
            {
                Rang = s.Rang,
                Modulzahl = s.Modulzahl,
                Kwp = StrangKwp(s, modul)
            };

            var teile = new List<string>();
            var fehlt = new Fehlliste();

            int reihe = s.Module_Reihe ?? 0;
            if (reihe <= 0) fehlt.Add(MyResource.Resource.PVS_FEHLT_REIHE);

            WechselrichterModel g = Geraet(s, gaben);
            if (g == null) fehlt.Add(MyResource.Resource.PVS_FEHLT_GERAET);

            teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P_MODULE,
                                    Ganz(reihe), Ganz(s.ParallelOderEins)));

            // --- P1: Leerlaufspannung im kalten Fall -> ROT ---------------------------
            // W6-B-9: Fehlt beta_OC, entfaellt P1 NICHT mehr - dann rechnet sie ueber
            // den Faktor 1,15 (FAKTOR_UOC_OHNE_KOEFFIZIENT), und der Satz sagt es.
            double? uoc = UocKaltReihe(reihe, modul?.m_U_Leerlauf, modul?.m_beta_OC,
                                       gaben.TKaltOderVorgabe);
            sb.UocKalt = uoc;
            sb.UocOhneKoeffizient = uoc.HasValue && !Gesetzt(modul?.m_beta_OC);

            if (!uoc.HasValue)
                SpannungsWerteFehlen(fehlt, modul, modul?.m_U_Leerlauf,
                                     MyResource.Resource.PVS_FEHLT_UOC_WERT);
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

                // Der Zusatz steht HINTER dem Befund, nicht statt seiner: Die Farbe
                // gilt, die Herkunft der Zahl steht dabei.
                if (sb.UocOhneKoeffizient)
                    teile.Add(string.Format(CultureInfo.CurrentCulture,
                                            MyResource.Resource.PVS_P1_OHNE_BETA,
                                            Z(FAKTOR_UOC_OHNE_KOEFFIZIENT, 2)));
            }

            // --- P2 und P3: das MPP-Fenster ------------------------------------------
            double? heiss = SpannungReihe(reihe, modul?.m_U_Mpp, modul?.m_beta_OC,
                                          gaben.THeissOderVorgabe);
            double? kalt = SpannungReihe(reihe, modul?.m_U_Mpp, modul?.m_beta_OC,
                                         gaben.TKaltOderVorgabe);
            sb.UmppHeiss = heiss;
            sb.UmppKalt = kalt;

            if (!heiss.HasValue)
                SpannungsWerteFehlen(fehlt, modul, modul?.m_U_Mpp,
                                     MyResource.Resource.PVS_FEHLT_UMPP_WERT);
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
                fehlt.Anhaengen(teile);
            }

            // AUSLEGUNGSHILFE (08.09.2026): Wer P1 bis P3 reisst, bekommt gesagt, welche
            // Reihe passen wuerde - dieselben Regeln, rueckwaerts gerechnet.
            if (sb.Farbe != Ampel.Gruen && g != null && modul != null)
                sb.Empfehlung = StrangAuslegung.ReiheEmpfehlung(modul, g,
                                                                gaben.TKaltOderVorgabe,
                                                                gaben.THeissOderVorgabe);

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

                foreach (AnlageStrangModel s in straenge) gb.Kwp += StrangKwp(s, ModulDesStrangs(s, gaben));

                var teile = new List<string>();

                if (g == null)
                {
                    gb.Farbe = Ampel.Gelb;
                    teile.Add(MyResource.Resource.PVS_KEIN_GERAET);
                }
                else
                {
                    MpptPruefen(straenge, g, gaben, gb, teile);
                    DcAcPruefen(g, gb, teile, straenge.Count > 0 ? ModulDesStrangs(straenge[0], gaben) : null);
                }

                gb.Satz = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_SATZ_GERAET,
                                        gb.Bezeichner, Ganz(gb.Geraetenummer),
                                        string.Join(MyResource.Resource.PVS_TRENNER, teile));
                b.Geraete.Add(gb);
            }
        }

        /// <summary>
        /// P4 (Eingangsstrom je MPPT) und P5 (Strangzahl je MPPT, gelb).
        ///
        /// <para><b>P4 ist seit <b>W6‑B‑10</b> ZWEISTUFIG</b> (Anwenderentscheid vom
        /// 09.09.2026, Vorschlag V6 des Prüfberichts, offener Punkt <b>O‑9</b>). Viele
        /// Datenblätter führen zwei Ströme je Tracker: den maximalen
        /// <b>Kurzschluss</b>strom, ab dem das Gerät Schaden nehmen kann
        /// (<c>I_Sc_Max</c>), und den maximalen <b>Arbeits</b>strom, ab dem es
        /// abregelt (<c>I_Dc_Max</c>). Danach:</para>
        ///
        /// <list type="bullet">
        ///   <item><description>über <c>I_Sc_Max</c> → <b>ROT</b>: das Gerät kann
        ///     Schaden nehmen.</description></item>
        ///   <item><description>nur über <c>I_Dc_Max</c> → <b>GELB</b>: Leistung wird
        ///     abgeregelt — ein Ertragsverlust, kein Schaden.</description></item>
        ///   <item><description><b>Ohne</b> gepflegten <c>I_Sc_Max</c> bleibt es beim
        ///     Verhalten von bisher: über <c>I_Dc_Max</c> → ROT. Bestandsprojekte
        ///     färben damit nicht um — den Unterschied macht erst der gepflegte neue
        ///     Wert.</description></item>
        /// </list>
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
            var jeMppt = new Dictionary<int, int>();       // Tracker -> Summe paralleler Straenge
            var stromJeMppt = new Dictionary<int, double>();  // Tracker -> Strom [A]

            // W6-O-6: Der Strom eines Trackers ist die Summe SEINER Straenge, und jeder
            // Strang bringt den Kurzschlussstrom SEINES Moduls mit. Fuehrt auch nur ein
            // Strang kein prüfbares Modul, ist der Tracker nicht prüfbar - eine halbe
            // Summe waere schlimmer als keine.
            bool stromBekannt = true;

            // W6-B-4: WELCHER Wert fehlt, sagt die Meldung einzeln - je Wert einmal,
            // auch wenn mehrere Straenge dasselbe Modul fuehren.
            var fehlt = new Fehlliste();

            foreach (AnlageStrangModel s in straenge)
            {
                int t = mpptBekannt ? s.MpptOderEins : 1;
                if (!jeMppt.ContainsKey(t)) { jeMppt[t] = 0; stromJeMppt[t] = 0.0; reihenfolge.Add(t); }
                jeMppt[t] += s.ParallelOderEins;

                PhotovoltaikModel m = ModulDesStrangs(s, gaben);
                double? js = StromJeStrang(m, gaben.THeissOderVorgabe);
                if (js.HasValue) stromJeMppt[t] += s.ParallelOderEins * js.Value;
                else { stromBekannt = false; StromWerteFehlen(fehlt, m); }
            }

            reihenfolge.Sort();

            bool p4Gemeldet = false, p5Gemeldet = false;
            double groesster = 0;

            foreach (int t in reihenfolge)
            {
                var mb = new Mpptbefund { Mppt = t, Straenge = jeMppt[t] };
                if (stromBekannt) mb.Strom = stromJeMppt[t];
                gb.Mppts.Add(mb);

                if (mb.Strom.HasValue && mb.Strom.Value > groesster) groesster = mb.Strom.Value;

                // --- P4: Eingangsstrom je MPPT, ZWEISTUFIG (W6-B-10) -----------------
                if (mb.Strom.HasValue)
                {
                    bool hatIsc = Gesetzt(g.m_I_Sc_Max);
                    bool hatIdc = Gesetzt(g.m_I_Dc_Max);

                    if (hatIsc && mb.Strom.Value > g.m_I_Sc_Max.Value)
                    {
                        // Stufe 1: ueber dem Kurzschlussstrom - das Geraet kann Schaden nehmen.
                        gb.Farbe = Schlechter(gb.Farbe, Ampel.Rot);
                        if (!p4Gemeldet)
                        {
                            teile.Add(string.Format(CultureInfo.CurrentCulture,
                                                    MyResource.Resource.PVS_P4_ROT_ISC,
                                                    Z(mb.Strom.Value, 2), Z(g.m_I_Sc_Max.Value, 1), Ganz(t)));
                            p4Gemeldet = true;
                        }
                    }
                    else if (hatIdc && mb.Strom.Value > g.m_I_Dc_Max.Value)
                    {
                        // Stufe 2: ueber dem Arbeitsstrom. MIT gepflegtem I_Sc_Max ist das
                        // Abregeln, also gelb; OHNE ihn bleibt es beim Rot von bisher -
                        // dann traegt I_Dc_Max beide Bedeutungen und darf nicht
                        // stillschweigend entschaerft werden.
                        gb.Farbe = Schlechter(gb.Farbe, hatIsc ? Ampel.Gelb : Ampel.Rot);
                        if (!p4Gemeldet)
                        {
                            teile.Add(string.Format(CultureInfo.CurrentCulture,
                                                    hatIsc ? MyResource.Resource.PVS_P4_GELB
                                                           : MyResource.Resource.PVS_P4_ROT,
                                                    Z(mb.Strom.Value, 2), Z(g.m_I_Dc_Max.Value, 1), Ganz(t)));
                            p4Gemeldet = true;
                        }
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

            if (!stromBekannt)
            {
                gb.Farbe = Schlechter(gb.Farbe, Ampel.Gelb);
                fehlt.Anhaengen(teile);
            }
            else if (!p4Gemeldet && (Gesetzt(g.m_I_Dc_Max) || Gesetzt(g.m_I_Sc_Max)))
            {
                // Der gruene Satz nennt die Grenze, an der der Strom als naechstes
                // anschlagen wuerde: den Arbeitsstrom, wenn er gepflegt ist, sonst den
                // Kurzschlussstrom.
                double grenze = Gesetzt(g.m_I_Dc_Max) ? g.m_I_Dc_Max.Value : g.m_I_Sc_Max.Value;
                teile.Add(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_P4,
                                        Z(groesster, 2), Z(grenze, 1)));
            }

            if (!mpptBekannt)
            {
                gb.Farbe = Schlechter(gb.Farbe, Ampel.Gelb);
                teile.Add(MyResource.Resource.PVS_FEHLT_MPPT);
            }
        }

        /// <summary>P6 (DC/AC-Verhältnis, gelb) und P7 (DC-Eingangsleistung, gelb).</summary>
        private static void DcAcPruefen(WechselrichterModel g, Geraetebefund gb, List<string> teile,
                                        PhotovoltaikModel modul)
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
                    if (modul != null) gb.Empfehlung = StrangAuslegung.GeraetEmpfehlung(modul, g);
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
                if (modul != null && gb.Empfehlung.Length == 0)
                    gb.Empfehlung = StrangAuslegung.GeraetEmpfehlung(modul, g);
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
        ///
        /// <para><b>Und genau dafür kann sie es seit <b>W6‑B‑12</b> auch</b>
        /// (Anwenderentscheid vom 09.09.2026, Befund A11 des Prüfberichts). Bis dahin
        /// gab die Hülle die ABGELEITETE Summe als <see cref="Gaben.AnzahlModuleAnlage"/>
        /// herein — dieselbe Zahl, aus derselben Liste, nach derselben Formel. Der
        /// Vergleich war damit immer erfüllt, und die Meldung
        /// <c>PVS_P8_GELB</c> war über die Oberfläche unerreichbar. Seither übergibt
        /// die Hülle den GESPEICHERTEN Anlagenwert; P8 trifft damit ausschliesslich
        /// Altdaten, und der Q9-Abgleich macht sie beim nächsten Handgriff wieder
        /// still.</para>
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
        /// <b>Die Leerlaufspannung eines Strangs im KALTEN Fall [V]</b> — mit
        /// Koeffizient, und ohne ihn über den Faktor
        /// <see cref="FAKTOR_UOC_OHNE_KOEFFIZIENT"/> (<b>W6‑B‑9</b>).
        ///
        /// <para><c>beta_OC</c> gesetzt: <c>Reihe · [U_oc + beta_OC · (T − 25)]</c> —
        /// unverändert. <c>beta_OC</c> fehlt: <c>Reihe · U_oc · 1,15</c>, unabhängig
        /// von der Auslegungstemperatur; der Faktor IST die Ersatzannahme über den
        /// kalten Fall und lässt sich nicht noch einmal temperieren.</para>
        ///
        /// <para><c>null</c> nur noch, wenn die Reihe oder <c>U_oc</c> selbst
        /// fehlt — dann meldet der Satz „Werte fehlen", wie bisher.</para>
        /// </summary>
        public static double? UocKaltReihe(int reihe, double? uOc, double? betaOc, double tKalt)
        {
            if (reihe <= 0 || !Gesetzt(uOc)) return null;
            if (Gesetzt(betaOc)) return reihe * (uOc.Value + betaOc.Value * (tKalt - T_STC));
            return reihe * uOc.Value * FAKTOR_UOC_OHNE_KOEFFIZIENT;
        }

        /// <summary>
        /// Der Kurzschlussstrom EINES Strangs im heissen Fall [A] bei der VORGABE
        /// <see cref="T_HEISS"/>, oder <c>null</c>.
        /// </summary>
        public static double? StromJeStrang(PhotovoltaikModel modul)
        {
            return StromJeStrang(modul, T_HEISS);
        }

        /// <summary>
        /// Der Kurzschlussstrom EINES Strangs bei <paramref name="tHeiss"/> [A], oder
        /// <c>null</c>.
        /// <para><c>I(T) = I_sc + alpha_SC · (T − 25)</c>; <c>alpha_SC</c> ist
        /// positiv (A/K). Die Temperatur ist seit <b>W6‑B‑11</b> ein
        /// Projektparameter.</para>
        /// </summary>
        public static double? StromJeStrang(PhotovoltaikModel modul, double tHeiss)
        {
            if (modul == null || !Gesetzt(modul.m_I_Kurzschluss) || !Gesetzt(modul.m_alpha_SC)) return null;
            return modul.m_I_Kurzschluss + modul.m_alpha_SC * (tHeiss - T_STC);
        }

        /// <summary>Die Nennleistung eines Strangs [kWp]; 0 ohne Modulleistung.</summary>
        public static double StrangKwp(AnlageStrangModel s, PhotovoltaikModel modul)
        {
            if (s == null || modul == null || modul.m_Leistung <= 0.0) return 0.0;
            return s.Modulzahl * modul.m_Leistung / 1000.0;
        }

        /// <summary>
        /// Das Modul, gegen das DIESER Strang prüft (<b>W6‑O‑6</b>): sein abweichender
        /// Modultyp (<c>ID_PV</c>), sonst das Modul der gewählten Projektzeile
        /// (<b>W6‑O‑5</b>).
        ///
        /// <para>Eine <c>ID_PV</c>, die die Ablage nicht kennt, ist derselbe Fall wie
        /// keine: Eine gelöschte Modulkopie darf die Ampel nicht anders prüfen lassen,
        /// sondern höchstens dieselbe Meldung erzeugen wie eine fehlende Angabe.</para>
        /// </summary>
        public static PhotovoltaikModel ModulDesStrangs(AnlageStrangModel s, Gaben gaben)
        {
            if (s == null || gaben == null) return null;

            int id = s.ID_PV ?? 0;
            if (id > 0 && gaben.Module != null &&
                gaben.Module.TryGetValue(id, out PhotovoltaikModel eigenes) && eigenes != null)
                return eigenes;

            return gaben.Modul;
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

        // =================================================================
        //  W6-B-4 — was genau fehlt, und wo man es pflegt
        // =================================================================

        /// <summary>
        /// <b>Die Sammelstelle der fehlenden Angaben einer Meldung</b> — Befund
        /// <b>W6‑B‑4</b> der Windows-Abnahme vom 07.09.2026.
        ///
        /// <para><b>Der Befund.</b> Die Meldung nannte ein PAAR mit „oder"
        /// („Leerlaufspannung oder beta_OC des Moduls"), obwohl der Prüfstand jeden der
        /// beiden Werte einzeln abfragt — er WEISS, welcher fehlt, und sagte es nicht.
        /// Der Anwender fragte darauf: „Welche Werte?"</para>
        ///
        /// <para><b>Zwei Regeln stecken hier drin.</b> Erstens: jede Angabe genau
        /// EINMAL, auch wenn sie mehrere Prüfungen unbrauchbar macht (<c>beta_OC</c>
        /// trägt P1, P2 und P3) oder mehrere Stränge dasselbe Modul führen. Zweitens:
        /// Steht ein MODULwert in der Liste, folgt der PFLEGEWEG — ein Satz, der sagt,
        /// wo man den Wert einträgt. Ein Hinweis, der immer dasteht, wird nicht
        /// gelesen; deshalb hängt er an <see cref="Modulwert"/> und nicht an der
        /// Zeilenzahl.</para>
        /// </summary>
        private sealed class Fehlliste
        {
            private readonly List<string> _angaben = new List<string>();

            /// <summary>Ist ein MODULwert darunter? Nur dann folgt der Pflegeweg.</summary>
            public bool Modulwert;

            /// <summary>Wie viele Angaben fehlen.</summary>
            public int Count { get { return _angaben.Count; } }

            /// <summary>Nimmt eine Angabe auf — genau einmal.</summary>
            public void Add(string text)
            {
                if (!string.IsNullOrEmpty(text) && !_angaben.Contains(text)) _angaben.Add(text);
            }

            /// <summary>Nimmt einen MODULwert auf und merkt sich, dass es einer war.</summary>
            public void Modul(string text)
            {
                Add(text);
                Modulwert = true;
            }

            /// <summary>
            /// Hängt „Werte fehlen: …" und — bei einem Modulwert — den Pflegeweg an die
            /// Satzteile an.
            /// </summary>
            public void Anhaengen(List<string> teile)
            {
                if (_angaben.Count == 0) return;

                teile.Add(string.Format(CultureInfo.CurrentCulture,
                                        MyResource.Resource.PVS_WERTE_FEHLEN,
                                        string.Join(", ", _angaben)));

                if (Modulwert) teile.Add(MyResource.Resource.PVS_PFLEGEWEG);
            }
        }

        /// <summary>
        /// Welche MODULwerte fehlen der Spannungsrechnung? Fehlt das Modul ganz, ist
        /// nicht die einzelne Spalte der Befund, sondern das Modul.
        ///
        /// <para><b>Die Reihe meldet sich selbst.</b> <see cref="SpannungReihe"/>
        /// liefert auch dann <c>null</c>, wenn allein „Module in Reihe" 0 ist — genau
        /// das stand im Bildschirmfoto des Anwenders, dessen Modul die zwei Spannungen
        /// sehr wohl führte. Deshalb wird hier nur nachgesehen, was am MODUL fehlt.</para>
        /// </summary>
        private static void SpannungsWerteFehlen(Fehlliste fehlt, PhotovoltaikModel modul,
                                                 double? spannung, string spannungstext)
        {
            if (modul == null) { fehlt.Add(MyResource.Resource.PVS_FEHLT_MODUL); return; }

            if (!Gesetzt(spannung)) fehlt.Modul(spannungstext);
            if (!Gesetzt(modul.m_beta_OC)) fehlt.Modul(MyResource.Resource.PVS_FEHLT_BETA_OC);
        }

        /// <summary>Dasselbe für die Stromrechnung: <c>I_SC</c> und <c>alpha_SC</c>.</summary>
        private static void StromWerteFehlen(Fehlliste fehlt, PhotovoltaikModel modul)
        {
            if (modul == null) { fehlt.Add(MyResource.Resource.PVS_FEHLT_MODUL); return; }

            if (!Gesetzt(modul.m_I_Kurzschluss)) fehlt.Modul(MyResource.Resource.PVS_FEHLT_ISC_WERT);
            if (!Gesetzt(modul.m_alpha_SC)) fehlt.Modul(MyResource.Resource.PVS_FEHLT_ALPHA_SC);
        }

        /// <summary>
        /// <b>Der Satz eines Befunds als LAUFHINWEIS</b> (<b>W6‑B‑13</b>,
        /// Anwenderentscheid vom 09.09.2026): „PV-Anlage „Dach Süd": Strang 2: …".
        ///
        /// <para>Er steht HIER und nicht im Rechenweg, weil die Sätze hier entstehen:
        /// Der Laufhinweis wiederholt den Befund der Ampel Wort für Wort und setzt nur
        /// den Vorspann davor. Zwei Formulierungen für denselben Befund wären zwei
        /// Wahrheiten — und der Anwender müsste zweimal lernen, was ihm gesagt
        /// wird.</para>
        /// </summary>
        /// <param name="anlage">Bezeichner der PV-Anlage.</param>
        /// <param name="satz">Der fertige Satz aus <see cref="Strangbefund.Satz"/> oder
        /// <see cref="Geraetebefund.Satz"/>.</param>
        public static string Laufhinweis(string anlage, string satz)
        {
            return string.Format(CultureInfo.CurrentCulture, MyResource.Resource.PVS_LAUF_VORSPANN,
                                 anlage ?? "", satz ?? "");
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

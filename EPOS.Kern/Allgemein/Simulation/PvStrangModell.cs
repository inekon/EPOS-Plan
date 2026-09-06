using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Rechenvorschriften der STRANGEBENE (Stufe S3 des
    /// <c>Konzept_Wechselrichter_EPOS-Plan.md</c>, Kapitel 4.1, Anwenderentscheid
    /// <b>W6‑E‑2</b> vom 06.09.2026) — <b>ohne Datenbank und ohne Oberflaeche</b>,
    /// damit sie sich einzeln nachrechnen lassen (Bauart
    /// <see cref="PvErweitertesModell"/> und <c>SolarZeitbasis</c>).
    ///
    /// <para>Enthalten sind drei Dinge: die <b>Kennlinie mit sechs Stuetzstellen</b>
    /// (5/10/20/30/50/100 % der AC-Nennleistung), die <b>Gruppierung der Straenge</b>
    /// nach (<c>ID_Wechselrichter</c>, <c>Geraetenummer</c>) — dem Gruppierungsmerkmal
    /// des Clippings (Konzept 3.4, Q6) — und der <b>Stundenschritt eines Geraets</b>
    /// mit Kennlinie, Clipping und Nachtverbrauch (Konzept 4.1, Schritt 4).</para>
    ///
    /// <para><b>Was hier NICHT steht:</b> die Modulformel und die Transposition. Sie
    /// sind je Modell verschieden (isotrop + linearer Gamma-Gang in EINFACH,
    /// Hay-Davies + Huld in ERWEITERT) und stehen unveraendert dort, wo sie schon
    /// stehen — <c>SimulationPV.BerechnePV</c>, <c>SolarCalculator</c>,
    /// <see cref="PvErweitertesModell.LeistungHuld"/>. Diese Klasse bekommt die
    /// Gleichstromleistung des Geraets als ZAHL.</para>
    ///
    /// <para><b>Und was sie nicht tut: klemmen am MPPT.</b> Entscheidungsfrage
    /// <b>W6‑E‑2‑Q7</b> (Empfehlung angenommen) laesst den MPP-Tracker als reine
    /// PRUEFGROESSE (P4/P5 in <c>StrangPlausibilitaet</c>); eine eigene
    /// Eingangsleistungsgrenze je Tracker ist bei ueblicher Auslegung wirkungslos und
    /// kostete eine Klemmstelle mehr in der Stundenschleife. Schritt 2 des Konzepts
    /// ist deshalb eine reine Summe — sie steht in
    /// <see cref="Geraetegruppe.Straenge"/> und braucht keinen eigenen Code.</para>
    /// </summary>
    public static class PvStrangModell
    {
        // =================================================================================
        // Kennlinie mit sechs Stuetzstellen (Konzept 3.3.1 und 4.1, Schritt 4)
        // =================================================================================

        /// <summary>
        /// Die sechs Auslastungen der Kennlinie — dieselbe Reihenfolge und dieselben
        /// Zahlen wie <see cref="WechselrichterKennlinie.STUETZSTELLEN"/>, aus der auch
        /// der Katalog seine Spalten <c>Eta05…Eta100</c> fuellt. <b>Eine Wahrheit</b>:
        /// Wer hier eine siebte Stuetzstelle braeuchte (die kalifornische 75-%-Wichtung,
        /// Q1), aendert sie dort.
        /// </summary>
        public static double[] Stuetzstellen => WechselrichterKennlinie.STUETZSTELLEN;

        /// <summary>
        /// Der Wirkungsgrad des Wechselrichters bei der Auslastung
        /// <paramref name="auslastung"/> = <c>P_DC,ger / P_AC,nenn</c> — stueckweise
        /// lineare Interpolation ueber die VORHANDENEN Stuetzstellen.
        ///
        /// <list type="bullet">
        ///   <item><description>unter der kleinsten vorhandenen Stuetzstelle: linear
        ///     von (0; 0) — der Wechselrichter braucht eine Mindestleistung, um
        ///     ueberhaupt einzuspeisen;</description></item>
        ///   <item><description>ueber der groessten (also ueber 100 %): konstant —
        ///     dahinter greift das Clipping, nicht ein weiter fallender
        ///     Wirkungsgrad.</description></item>
        /// </list>
        ///
        /// <para><b>Die Rueckfallregel bei NULL</b> (Konzept 3.3.1): Eine fehlende
        /// Stuetzstelle wird UEBERSPRUNGEN — interpoliert wird dann zwischen den beiden
        /// naechsten vorhandenen Punkten. Fehlen alle sechs, gilt die Dreipunkt-Vorgabe
        /// eines typischen Strang-Wechselrichters
        /// (<see cref="PvErweitertesModell.WR_ETA10_VORGABE"/> und Geschwister) — dieselbe
        /// Vorgabe und dieselbe Protokollmeldung wie im Anlagenweg
        /// (<c>SimulationPV.KennlinieMelden</c>).</para>
        ///
        /// <para><b>An den Stuetzstellen ist das Ergebnis exakt der eingegebene
        /// Wert</b>, und ein Geraet mit NUR den Stuetzstellen 10/50/100 % rechnet
        /// <b>zeichengleich</b> zu <see cref="PvErweitertesModell.EtaWechselrichter"/>:
        /// Die Interpolationsformel ist dieselbe, und die Stuetzstellen 0,10 / 0,50 /
        /// 1,00 sind dieselben Gleitkommazahlen wie
        /// <c>AUSLASTUNG_UNTEN/MITTE/OBEN</c>. Genau daran haengt die Abnahme S3 (2)
        /// des Konzepts.</para>
        /// </summary>
        /// <param name="auslastung">P_DC,sys je Geraet / P_AC,nenn.</param>
        /// <param name="etas">
        /// Sechs Stuetzstellen in der Reihenfolge von <see cref="Stuetzstellen"/>;
        /// <c>null</c>-Eintraege sind zulaessig und bedeuten „nicht gepflegt".
        /// </param>
        public static double EtaWechselrichter(double auslastung, double?[] etas)
        {
            double[] x, y;
            Kennlinie(etas, out x, out y);
            return EtaAusPunkten(auslastung, x, y);
        }

        /// <summary>
        /// Die Kennlinie als zwei gleich lange Felder (Auslastungen, Wirkungsgrade) —
        /// einmal je Geraet gebildet, damit die Stundenschleife nicht 8 760-mal
        /// dieselbe Liste zusammensucht.
        /// </summary>
        /// <returns>
        /// <c>true</c>, wenn die Punkte aus dem KATALOG stammen; <c>false</c>, wenn die
        /// Dreipunkt-Vorgabe eingesprungen ist (dann gehoert eine Protokollmeldung
        /// dazu).
        /// </returns>
        public static bool Kennlinie(double?[] etas, out double[] auslastungen, out double[] wirkungsgrade)
        {
            var xs = new List<double>();
            var ys = new List<double>();

            if (etas != null)
            {
                int n = Math.Min(etas.Length, Stuetzstellen.Length);
                for (int i = 0; i < n; i++)
                    if (etas[i].HasValue && etas[i].Value > 0.0)
                    {
                        xs.Add(Stuetzstellen[i]);
                        ys.Add(etas[i].Value);
                    }
            }

            if (xs.Count > 0)
            {
                auslastungen = xs.ToArray();
                wirkungsgrade = ys.ToArray();
                return true;
            }

            // Rueckfallebene: die Dreipunkt-Vorgabe des Anlagenwegs. Sie steht
            // ABSICHTLICH nicht als vierte Konstante hier, sondern kommt aus
            // PvErweitertesModell - eine Vorgabe, zwei Wege.
            auslastungen = new[]
            {
                PvErweitertesModell.AUSLASTUNG_UNTEN,
                PvErweitertesModell.AUSLASTUNG_MITTE,
                PvErweitertesModell.AUSLASTUNG_OBEN
            };
            wirkungsgrade = new[]
            {
                PvErweitertesModell.WR_ETA10_VORGABE,
                PvErweitertesModell.WR_ETA50_VORGABE,
                PvErweitertesModell.WR_ETA100_VORGABE
            };
            return false;
        }

        /// <summary>
        /// Der Wirkungsgrad zu einer fertigen Kennlinie (siehe <see cref="Kennlinie"/>).
        /// <b>Zeichengleich zu <see cref="PvErweitertesModell.EtaWechselrichter"/></b>,
        /// wenn die Punkte (0,1; η10), (0,5; η50), (1,0; η100) sind.
        /// </summary>
        public static double EtaAusPunkten(double auslastung, double[] x, double[] y)
        {
            if (x == null || y == null || x.Length == 0) return 0.0;
            if (auslastung <= 0.0) return 0.0;

            // Unterhalb der kleinsten Stuetzstelle: linear von (0; 0).
            if (auslastung < x[0]) return y[0] * auslastung / x[0];

            for (int i = 1; i < x.Length; i++)
                if (auslastung < x[i])
                    return y[i - 1] + (y[i] - y[i - 1]) * (auslastung - x[i - 1]) / (x[i] - x[i - 1]);

            // Oberhalb der groessten Stuetzstelle: konstant - dahinter greift das Clipping.
            return y[y.Length - 1];
        }

        // =================================================================================
        // Die Gruppierung: EIN Geraet = (ID_Wechselrichter, Geraetenummer)
        // =================================================================================

        /// <summary>
        /// Ein PHYSISCHES Geraet einer Anlage mit den Straengen, die daran haengen —
        /// die Einheit, an der Kennlinie, Clipping und Nachtverbrauch rechnen
        /// (Konzept 3.4, Entwurfsentscheidung 1).
        /// </summary>
        public sealed class Geraetegruppe
        {
            /// <summary>FK auf die Projektkopie <c>Tab_Wechselrichter.ID</c>.</summary>
            public int ID_Wechselrichter;

            /// <summary>Welches physische Geraet dieses Typs (1…n).</summary>
            public int Geraetenummer;

            /// <summary>Geraetename aus dem Katalogsatz; leer, wenn er fehlt.</summary>
            public string Bezeichner = "";

            /// <summary>Der Katalogsatz; nie <c>null</c> (die Gruppierung nimmt nur Geraete auf, die es gibt).</summary>
            public WechselrichterModel Geraet;

            /// <summary>Die Straenge dieses Geraets in Rangfolge.</summary>
            public List<AnlageStrangModel> Straenge = new List<AnlageStrangModel>();

            /// <summary>AC-Nennwirkleistung [kW]; <c>null</c> = kein Clipping.</summary>
            public double? PAcNennKw;

            /// <summary>Die Stuetzstellen der Kennlinie (Auslastungen).</summary>
            public double[] KennlinieX;

            /// <summary>Die Stuetzstellen der Kennlinie (Wirkungsgrade).</summary>
            public double[] KennlinieY;

            /// <summary>Stammt die Kennlinie aus dem Katalog? Sonst ist die Vorgabe eingesprungen.</summary>
            public bool KennlinieGepflegt;

            /// <summary>Einschaltschwelle [kW] — umgerechnet aus <c>P_Standby</c> [W]; 0 = keine.</summary>
            public double PStandbyKw;

            /// <summary>Nachtverbrauch [kW] — umgerechnet aus <c>P_Nacht</c> [W]; 0 = keiner.</summary>
            public double PNachtKw;

            /// <summary>Nennleistung der angeschlossenen Module [kWp] — fuellt der Aufrufer.</summary>
            public double KwpDc;

            /// <summary>DC/AC-Verhaeltnis; 0 = ohne AC-Nennleistung nicht bestimmbar.</summary>
            public double DcAc => (PAcNennKw.HasValue && PAcNennKw.Value > 0.0) ? KwpDc / PAcNennKw.Value : 0.0;

            // --- Jahressummen (Kennzahlen, Konzept 4.4) --------------------------------

            /// <summary>Σ P_AC ueber das Jahr [kWh] — nach Clipping, einschliesslich Nachtverbrauch.</summary>
            public double ErtragKwh;

            /// <summary>Σ P_DC,sys ueber das Jahr [kWh] — der Nenner des Jahresnutzungsgrads.</summary>
            public double DcSysKwh;

            /// <summary>Σ max(0; P_AC,roh − P_AC,nenn) [kWh].</summary>
            public double ClippingKwh;

            /// <summary>Σ (P_DC,sys − P_AC,roh) [kWh] — der Kennlinienverlust ohne das Clipping.</summary>
            public double WrVerlustKwh;

            /// <summary>Σ Nachtverbrauch [kWh] — eine POSITIVE Zahl, obwohl sie den Ertrag mindert.</summary>
            public double NachtKwh;

            /// <summary>Zahl der Stunden, in denen das Geraet unter der Einschaltschwelle blieb.</summary>
            public int Nachtstunden;

            /// <summary>
            /// Der Jahresnutzungsgrad des Geraets: <c>Σ P_AC / Σ P_DC,sys</c> — die Zahl,
            /// mit der sich der <c>Eta_Euro</c> des Datenblatts gegen das tatsaechliche
            /// Betriebsverhalten DIESER Anlage vergleichen laesst (Konzept 4.4).
            /// 0, solange nichts erzeugt wurde.
            /// </summary>
            public double Jahresnutzungsgrad => DcSysKwh > 0.0 ? ErtragKwh / DcSysKwh : 0.0;

            /// <summary>
            /// Der Clipping-Verlust als Anteil des UNGEKLIPPTEN Wechselstromertrags [%]:
            /// <c>Clipping / (Ertrag + Clipping)</c>. Bezugsgroesse ist bewusst die
            /// Summe, nicht die Gleichstromseite — gefragt ist „wieviel der moeglichen
            /// EINSPEISUNG bleibt am Wechselrichter haengen", und das ist der Anteil an
            /// P_AC,roh.
            /// </summary>
            public double ClippingAnteilProzent
            {
                get
                {
                    double roh = ErtragKwh + ClippingKwh;
                    return roh > 0.0 ? ClippingKwh * 100.0 / roh : 0.0;
                }
            }

            /// <summary>Volllaststunden bezogen auf die AC-Nennleistung [h/a]; 0 ohne Nennleistung.</summary>
            public double VolllaststundenAc =>
                (PAcNennKw.HasValue && PAcNennKw.Value > 0.0) ? ErtragKwh / PAcNennKw.Value : 0.0;

            /// <summary>Ein sprechender Name fuer Protokoll und Karte: „Geraet ‹Bezeichner› Nr. n".</summary>
            public string Anzeigename =>
                (string.IsNullOrEmpty(Bezeichner) ? ("ID " + ID_Wechselrichter) : Bezeichner) +
                " Nr. " + Geraetenummer.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Gruppiert die Straenge EINER Anlage nach (<c>ID_Wechselrichter</c>,
        /// <c>Geraetenummer</c>) und haengt an jede Gruppe ihren Katalogsatz.
        ///
        /// <para><b>Straenge OHNE Geraet fallen heraus</b> — sie stehen in der Tabelle,
        /// rechnen aber nicht mit (so steht es seit S2 im Kopf von
        /// <see cref="AnlageStrangModel.ID_Wechselrichter"/>, und die Ampel meldet es).
        /// Ebenso ein Strang, dessen Geraet die Projektkopie nicht (mehr) kennt: Er
        /// wird ueber <paramref name="ohneGeraet"/> gezaehlt, damit der Aufrufer eine
        /// Protokollmeldung schreiben kann statt still zu rechnen.</para>
        ///
        /// <para><b>Die Reihenfolge ist festgelegt</b> — nach <c>ID_Wechselrichter</c>,
        /// dann <c>Geraetenummer</c>, die Straenge in Eingangsreihenfolge (also nach
        /// <c>Rang</c>, so liefert sie <c>AnlageStrangCtrl</c>). Ohne feste Reihenfolge
        /// haengt die letzte Stelle der Jahressumme an der Reihenfolge der
        /// Gleitkommaadditionen.</para>
        /// </summary>
        public static List<Geraetegruppe> Gruppieren(IEnumerable<AnlageStrangModel> straenge,
                                                     IReadOnlyDictionary<int, WechselrichterModel> geraete,
                                                     out int ohneGeraet)
        {
            var liste = new List<Geraetegruppe>();
            var gefunden = new Dictionary<long, Geraetegruppe>();
            ohneGeraet = 0;

            if (straenge == null) return liste;

            foreach (AnlageStrangModel s in straenge)
            {
                if (s == null) continue;

                int idWr = s.ID_Wechselrichter ?? 0;
                WechselrichterModel g = null;
                if (idWr > 0 && geraete != null) geraete.TryGetValue(idWr, out g);

                if (g == null) { ohneGeraet++; continue; }

                int nummer = s.GeraetenummerOderEins;
                long schluessel = (long)idWr * 1000000L + nummer;

                Geraetegruppe gruppe;
                if (!gefunden.TryGetValue(schluessel, out gruppe))
                {
                    gruppe = Anlegen(idWr, nummer, g);
                    gefunden[schluessel] = gruppe;
                    liste.Add(gruppe);
                }

                gruppe.Straenge.Add(s);
            }

            liste.Sort((a, b) => a.ID_Wechselrichter != b.ID_Wechselrichter
                                     ? a.ID_Wechselrichter.CompareTo(b.ID_Wechselrichter)
                                     : a.Geraetenummer.CompareTo(b.Geraetenummer));
            return liste;
        }

        /// <summary>Eine Geraetegruppe aus ihrem Katalogsatz — die Umrechnung W → kW steht hier.</summary>
        public static Geraetegruppe Anlegen(int idWechselrichter, int geraetenummer, WechselrichterModel g)
        {
            var gruppe = new Geraetegruppe
            {
                ID_Wechselrichter = idWechselrichter,
                Geraetenummer = geraetenummer,
                Geraet = g,
                Bezeichner = (g != null && !string.IsNullOrEmpty(g.m_szName)) ? g.m_szName : "",
                // Eine AC-Nennleistung <= 0 ist keine: dann gibt es kein Clipping, und
                // die Auslastung bezieht sich ersatzweise auf die DC-Nennleistung -
                // dieselbe Regel wie im Anlagenweg (SimulationPV, Zweig ERWEITERT).
                PAcNennKw = (g != null && g.m_P_AC_Nenn.HasValue && g.m_P_AC_Nenn.Value > 0.0)
                                ? g.m_P_AC_Nenn : null,
                PStandbyKw = (g != null && g.m_P_Standby.HasValue && g.m_P_Standby.Value > 0.0)
                                ? g.m_P_Standby.Value / 1000.0 : 0.0,
                PNachtKw = (g != null && g.m_P_Nacht.HasValue && g.m_P_Nacht.Value > 0.0)
                                ? g.m_P_Nacht.Value / 1000.0 : 0.0
            };

            double[] x, y;
            gruppe.KennlinieGepflegt = Kennlinie(Etas(g), out x, out y);
            gruppe.KennlinieX = x;
            gruppe.KennlinieY = y;
            return gruppe;
        }

        /// <summary>Die sechs Stuetzstellen eines Katalogsatzes in der Reihenfolge von <see cref="Stuetzstellen"/>.</summary>
        public static double?[] Etas(WechselrichterModel g)
        {
            if (g == null) return new double?[Stuetzstellen.Length];
            return new[] { g.m_Eta05, g.m_Eta10, g.m_Eta20, g.m_Eta30, g.m_Eta50, g.m_Eta100 };
        }

        // =================================================================================
        // Der Stundenschritt eines Geraets (Konzept 4.1, Schritt 4)
        // =================================================================================

        /// <summary>
        /// Ein Stundenschritt EINES Geraets: Kennlinie, Clipping, Nachtverbrauch. Die
        /// Jahressummen der Gruppe werden dabei fortgeschrieben.
        ///
        /// <code>
        /// x        = P_DC,sys / P_AC,nenn        (ohne Nennleistung: / P_STC des Geraets)
        /// eta(x)   = Interpolation ueber die vorhandenen Stuetzstellen
        /// P_AC,roh = P_DC,sys · eta(x)
        /// P_AC     = min(P_AC,roh; P_AC,nenn)                       Clipping
        /// P_DC,sys &lt; P_Standby  ->  P_AC = −P_Nacht               Nachtverbrauch
        /// </code>
        ///
        /// <para><b>Zur Definition von x</b> (Konzept 4.1, letzter Absatz): Sie ist
        /// bewusst dieselbe wie im Anlagenweg — das Verhaeltnis der EINGANGSleistung zur
        /// AC-Nennleistung. Eine Datenblattkennlinie bezieht sich streng genommen auf die
        /// ABGEGEBENE Leistung; der Unterschied liegt bei 2–4 % der Auslastung und damit
        /// im Zehntelprozentbereich des Wirkungsgrads. Die bestehende Definition bleibt,
        /// damit der Dreipunkt-Pfad ohne Zuordnung Zeichen fuer Zeichen unveraendert
        /// rechnet.</para>
        ///
        /// <para><b>Der Nachtverbrauch ist eine NEGATIVE Erzeugung.</b> Unterhalb der
        /// Einschaltschwelle speist das Geraet nicht ein, sondern verbraucht — genau das
        /// meint <c>P_Nacht</c> (CEC <c>Pnt</c>). Fuehrt der Katalog keinen (NULL oder 0),
        /// ist das Ergebnis 0 und nicht etwa ein erfundener Verbrauch. Ohne gepflegte
        /// Einschaltschwelle ist die Schwelle 0 — dann sind es genau die Stunden ohne
        /// Einstrahlung.</para>
        /// </summary>
        /// <param name="gruppe">Das Geraet samt seiner Kennlinie und seiner Jahressummen.</param>
        /// <param name="pDcSysKw">Gleichstromleistung des Geraets NACH Systemverlusten [kW].</param>
        /// <returns>Die Wechselstromleistung dieser Stunde [kW]; negativ in Nachtstunden.</returns>
        public static double Stunde(Geraetegruppe gruppe, double pDcSysKw)
        {
            if (gruppe == null) return 0.0;

            gruppe.DcSysKwh += pDcSysKw;

            // Nachtfall zuerst: unterhalb der Einschaltschwelle rechnet keine Kennlinie.
            if (pDcSysKw <= gruppe.PStandbyKw)
            {
                gruppe.Nachtstunden++;
                if (gruppe.PNachtKw <= 0.0) return 0.0;

                gruppe.NachtKwh += gruppe.PNachtKw;
                gruppe.ErtragKwh -= gruppe.PNachtKw;
                return -gruppe.PNachtKw;
            }

            double bezugKw = gruppe.PAcNennKw ?? gruppe.KwpDc;
            double auslastung = bezugKw > 0.0
                ? pDcSysKw / bezugKw : PvErweitertesModell.AUSLASTUNG_OBEN;

            double eta = EtaAusPunkten(auslastung, gruppe.KennlinieX, gruppe.KennlinieY);
            double pAcRoh = pDcSysKw * eta;
            gruppe.WrVerlustKwh += pDcSysKw - pAcRoh;

            double pAc = pAcRoh;
            if (gruppe.PAcNennKw.HasValue && pAc > gruppe.PAcNennKw.Value)
            {
                gruppe.ClippingKwh += pAc - gruppe.PAcNennKw.Value;
                pAc = gruppe.PAcNennKw.Value;
            }

            gruppe.ErtragKwh += pAc;
            return pAc;
        }
    }
}

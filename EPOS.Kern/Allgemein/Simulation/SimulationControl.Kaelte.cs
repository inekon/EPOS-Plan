using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Kälteseite des Erzeugerlaufs</b> (Stufe KU2 der Kühlung; Kühlkonzept 5.1, 5.2, 5.5,
    /// 6.1; Entscheide E15, E21, E33).
    ///
    /// <para>Drei Schritte, an drei Stellen von <see cref="Kaskade_Zweikanalig"/>:</para>
    /// <list type="number">
    /// <item><see cref="KaelteerzeugerVorbereiten"/> — VOR der Stundenschleife der Speicherstufe:
    /// welche Wärmepumpen im Kühlbetrieb rechnen (Sperrgründe benannt), mit welcher Kühlkennlinie,
    /// und die Tagesbetriebsart, die ihren Heizkanal am Kühltag sperrt (K8a).</item>
    /// <item><see cref="KaeltekaskadeRechnen"/> — gleich NACH der Speicherstufe, an der Stelle des
    /// Wärmepumpenstroms: die <see cref="Kaeltekaskade"/> und ihr Kältestrom in der Strombilanz
    /// (6.1) — eine eigene Reihe neben <c>WP_Strombedarf_stuendlich</c>, addiert im Rest.</item>
    /// <item><see cref="KaelteseiteAbschliessen"/> — NACH der ganzen Wärmekaskade: die Meldungen zu
    /// Deckung und Unterdeckung (F-K12, 5.5) und die Deckungsprobe Kälte (4.3 #31).</item>
    /// </list>
    ///
    /// <para><b>Ohne Kälte kein Takt.</b> Rechnet das Projekt keine Kälte oder steht keine
    /// Wärmepumpe auf Kühlbetrieb — jedes Referenzprojekt —, bleibt die Liste der Kälteerzeuger
    /// leer, das Wärmepumpenmodul erfährt nichts, und kein Wert der Wärme- oder Stromseite ändert
    /// sich.</para>
    ///
    /// <para><b>Der Kältestrom eines abweichenden Kühlträgers (E34, Kühlkonzept 6.1):</b> Trägt
    /// eine Anlage einen anderen Stromträger für die Kühlung als das Projekt, läuft ihr Kältestrom
    /// entweder <b>anteilig am Netzbezug</b> durch die Stufenrechnung (Vorgabe — Eigenverbrauch aus
    /// Photovoltaik und Stromspeicher gemeinsam) oder über einen <b>eigenen Zähler</b> NEBEN ihr (nicht
    /// in den Rest, nicht in die Lastreihe des Speichers). Am Laufende teilt
    /// <see cref="KaeltestromNetzbezugAufteilen"/> den Netzbezug jeder Viertelstunde nach dem Anteil
    /// des Kältestroms am Stromverbrauch; bepreist und bewertet wird er einmal, im
    /// <c>KostenEmissionRechner</c>.</para>
    /// </summary>
    partial class SimulationControl
    {
        /// <summary>Die Kälteerzeuger dieses Laufs in Kaskadenreihenfolge; <c>null</c> = keiner vorbereitet.</summary>
        private List<Kaelteerzeuger> _kaelteerzeuger;

        /// <summary>Die Kältespeicher dieses Laufs (KU3-5), die die Kältekaskade gerechnet hat; leer = keiner.</summary>
        private List<SimulationPufferspeicher> _kaeltespeicher = new List<SimulationPufferspeicher>();

        /// <summary>
        /// <b>Die gerechneten Kältespeicher des Laufs</b> (KU3-5, E68) — getrennt von <see cref="AlleSpeicher"/>:
        /// Sie stehen in keiner Wärmeordnung und dürfen in der Deckungsprobe nicht als Wärme im Kühlkanal zählen.
        /// Ergebniszeile, Anzeige und Bericht nehmen sie neben den Wärmespeichern.
        /// </summary>
        public List<SimulationPufferspeicher> Kaeltespeicher()
        {
            return _kaeltespeicher ?? new List<SimulationPufferspeicher>();
        }

        /// <summary>
        /// <b>Alle Speicher mit Füllstandsganglinie</b> (KU3-4d): <see cref="AlleSpeicher"/>, dahinter
        /// <see cref="Kaeltespeicher"/> — die Liste, aus der Navigator, Präsenz und Füllstandsexport ihre Reihen
        /// bilden. Die Schlüssel (<c>PUFFER_&lt;ID&gt;</c>) bleiben eindeutig, die Wärmeseite ändert sich nicht.
        /// </summary>
        public List<SimulationPufferspeicher> SpeicherSamtKaelte()
        {
            var liste = new List<SimulationPufferspeicher>(AlleSpeicher() ?? new List<SimulationPufferspeicher>());
            liste.AddRange(Kaeltespeicher());
            return liste;
        }

        /// <summary>Die Tagesbetriebsart dieses Laufs; <c>null</c> ohne Kälteerzeuger.</summary>
        private bool[] _kuehltage;

        /// <summary>Stunden, in denen ein Wärmekanal sich während der Kältekaskade verändert hat — für die Deckungsprobe.</summary>
        private int _waermekanalAbweichungen;

        /// <summary>
        /// <b>Der Kältestrom, der durch die Stufenrechnung läuft</b> [kWh je Stunde] — die Summe der
        /// Kälteerzeuger OHNE eigenen Zähler (E34). Ihn liest die Lastreihe des Stromspeichers
        /// (<c>StromspeicherSimCtrl.BaueLastreihe</c>), damit Speicher und Flotte dieselbe Last sehen
        /// wie der Rest; und er ist Teil des Stromverbrauchs, nach dem der Netzbezug geteilt wird.
        /// <c>null</c> ohne gerechnete Kältekaskade — dann ändert sich an der Lastreihe nichts.
        /// </summary>
        public double[] Kaeltestrom_Stufenrechnung_stuendlich;

        /// <summary>Den Kältezustand des Vorlaufs verwerfen — am Beginn von <see cref="Kaskade_Zweikanalig"/>.</summary>
        private void KaelteseiteZuruecksetzen()
        {
            _kaelteerzeuger = null;
            _kaeltespeicher = new List<SimulationPufferspeicher>();
            _kuehltage = null;
            _waermekanalAbweichungen = 0;
            Kaeltestrom_Stufenrechnung_stuendlich = null;
        }

        private static string Anzeigename(WErzeugerModel m)
        {
            return string.IsNullOrEmpty(m.Bezeichner)
                ? m.ID.ToString(CultureInfo.CurrentCulture) : m.Bezeichner;
        }

        // =====================================================================
        //  1. Vor der Stundenschleife: Kälteerzeuger und Tagesbetriebsart
        // =====================================================================

        /// <summary>
        /// Stellt die Kälteerzeuger des Laufs zusammen (Stufe KU2): die Wärmepumpen-Module, deren
        /// Gerät auf Kühlbetrieb steht (<c>Tab_WP.Kuehlbetrieb</c>), in der Reihenfolge ihrer
        /// Anlagen — die Kaskadenplätze der Wärmeseite, gefiltert auf die Kälteerzeuger (5.5).
        /// Gerufen in <see cref="Speicherstufe_Rechnen"/>, nach dem Modulaufbau und vor der
        /// Stundenschleife.
        ///
        /// <para><b>Benannt gesperrt</b> (5.0.1, 5.1 Festlegung 6, 8.5) — die Maschine heizt dann
        /// nur, und die Meldung sagt warum: das Projekt rechnet keine Kälte; ein Quellspeicher als
        /// Wärmequelle; keine Kühlkennlinie im Projekt; die Kennlinie des gewählten Vorlaufs in
        /// Heizlage oder mit vertauschten Achsen (K22).</para>
        /// </summary>
        private void KaelteerzeugerVorbereiten()
        {
            _kaelteerzeuger = new List<Kaelteerzeuger>();
            _kuehltage = null;
            if (_wpInSchleife) simulation_wp.KuehlbetriebSetzen(null, null);

            SimulationKaeltebedarf kaelte = simulation_Waermebedarf != null ? simulation_Waermebedarf.Kaelteseite : null;
            bool erhoben = kaelte != null && kaelte.Gerechnet;

            // Der Stromträger des Projekts - einmal je Lauf und erst, wenn ein Kälteerzeuger ihn braucht (E34).
            int projekttraeger = -1;

            for (int i = 0; _wpInSchleife && i < simulation_wp.wp_model.Count && i < SimulationWaermepumpe.MAX_WP; i++)
            {
                WErzeugerModel m = simulation_wp.wp_model[i];
                if (m == null) continue;

                DataTable dt = StilleDb.Tabelle(
                    "SELECT Kuehlbetrieb, Kuehl_Vorlauf, Kuehl_Hilfsstromanteil, Kuehlleistung, ID_Stamm " +
                    "FROM Tab_WP WHERE ID = ?",
                    StilleDb.Par("@id", DbParamTyp.Integer, m.ID_WP));
                if (dt == null || dt.Rows.Count == 0) continue;
                DataRow r = dt.Rows[0];

                bool kuehlbetrieb = StilleDb.Zahl(StilleDb.Feld(r, "Kuehlbetrieb")) != 0;
                object vorlauf = StilleDb.Feld(r, "Kuehl_Vorlauf");
                int? kuehlVorlauf = vorlauf != null ? (int?)StilleDb.Zahl(vorlauf) : null;
                object hilfs = StilleDb.Feld(r, "Kuehl_Hilfsstromanteil");
                double kuehlleistung = StilleDb.Kommazahl(StilleDb.Feld(r, "Kuehlleistung"));
                int idStamm = StilleDb.Zahl(StilleDb.Feld(r, "ID_Stamm"));
                string name = Anzeigename(m);

                if (!kuehlbetrieb)
                {
                    // 8.5: Nennkühlleistung ohne Kühlkennlinie - diese Maschine rechnet nur Wärme.
                    if (erhoben && kuehlleistung > 0 && !KenndatenKuehlungCtrl.HatKenndatenProjekt(m.ID_WP))
                        Protokoll.WarnungEinmal("kuehl-nennleistung-ohne-kennlinie-" + m.ID,
                            string.Format(CultureInfo.CurrentCulture,
                                MyResource.Resource.SIMENG_KAELTE_WP_NENNLEISTUNG_OHNE_KENNLINIE, name));
                    continue;
                }

                if (!erhoben)
                {
                    Protokoll.HinweisEinmal("kuehl-wp-projekt-aus-" + m.ID,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_PROJEKT_AUS, name));
                    continue;
                }

                // 5.1, Festlegung 6: Quellspeicher - die Kondensatorwärme müsste in den Speicher.
                string wpTyp = i < simulation_wp.WPTypen.Count ? simulation_wp.WPTypen[i] : "";
                string wqTyp = WaermequelleClass.WertLesenStill(m.ID, "WQ_Typ") as string;
                bool quellspeicher = (i < simulation_wp.Quellspeicher.Count && simulation_wp.Quellspeicher[i] != null) ||
                                     (!string.IsNullOrEmpty(wpTyp) && wpTyp != DbWerte.WP_BAUART_LUFT_WASSER &&
                                      wqTyp == WaermequelleClass.TYP_PUFFER);
                if (quellspeicher)
                {
                    Protokoll.WarnungEinmal("kuehl-wp-quellspeicher-" + m.ID,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_QUELLSPEICHER, name));
                    continue;
                }

                // 5.0.1: Der Kühlbetrieb hängt an der Kennlinie IM PROJEKT.
                // KK2: die Zeilen einmal gelesen — die Kennlinie am festen Vorlauf (wie KennlinieProjekt) und die Schar über alle Vorläufe.
                List<KuehlkennlinienZeile> kuehlZeilen = KenndatenKuehlungCtrl.ZeilenProjekt(m.ID_WP);
                Kuehlkennlinie k = Kuehlkennlinie.Bilden(kuehlZeilen, kuehlVorlauf, true);
                if (k.Leer)
                {
                    string text = string.Format(CultureInfo.CurrentCulture,
                        MyResource.Resource.SIMENG_KAELTE_WP_OHNE_KENNLINIE, name);
                    if (idStamm > 0 && KenndatenKuehlungCtrl.HatKenndatenStamm(idStamm))
                        text += " " + MyResource.Resource.SIMENG_KAELTE_WP_KENNLINIE_IM_KATALOG;
                    Protokoll.WarnungEinmal("kuehl-wp-ohne-kennlinie-" + m.ID, text);
                    continue;
                }

                // K22: Heizlage und vertauschte Achsen werden benannt abgelehnt, nie umgedeutet.
                if (k.Befund == KuehlblockBefund.Heizlage || k.Befund == KuehlblockBefund.AchsenVertauscht)
                {
                    string vorlage = k.Befund == KuehlblockBefund.Heizlage
                        ? MyResource.Resource.SIMENG_KAELTE_WP_HEIZLAGE
                        : MyResource.Resource.SIMENG_KAELTE_WP_ACHSEN;
                    Protokoll.WarnungEinmal("kuehl-wp-befund-" + m.ID + "-" + k.Vorlauf.ToString(CultureInfo.InvariantCulture),
                        string.Format(CultureInfo.CurrentCulture, vorlage, name, k.Vorlauf));
                    continue;
                }
                if (!k.Rechenbar) continue;

                // K21: außerhalb der Stützstellen - die nächste, einmal je Gerät und Vorlauf benannt.
                if (k.VorlaufAusgewichen)
                    Protokoll.HinweisEinmal("kuehl-wp-vorlauf-" + m.ID_WP + "-" + k.VorlaufGewuenscht.Value.ToString(CultureInfo.InvariantCulture),
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_VORLAUF_AUSGEWICHEN,
                                      name, k.VorlaufGewuenscht.Value,
                                      string.Join(", ", k.Stuetzstellen.Select(v => v.ToString(CultureInfo.CurrentCulture))),
                                      k.Vorlauf));

                // AK3-I (I-3): zwischen zwei Stützstellen interpoliert - einmal je Gerät und Vorlauf benannt.
                if (k.Interpoliert)
                    Protokoll.HinweisEinmal("kuehl-wp-vorlauf-interpoliert-" + m.ID_WP + "-" + k.VorlaufGewuenscht.Value.ToString(CultureInfo.InvariantCulture),
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_VORLAUF_INTERPOLIERT,
                                      name, k.VorlaufGewuenscht.Value, k.Vorlauf, k.Oben.Vorlauf));

                // Dubletten deterministisch zusammengefasst - und benannt.
                if (k.Dubletten > 0)
                    Protokoll.HinweisEinmal("kuehl-wp-dubletten-" + m.ID_WP,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_DUBLETTEN, name, k.Dubletten));
                if (k.DublettenAbweichend > 0)
                    Protokoll.WarnungEinmal("kuehl-wp-dubletten-abweichend-" + m.ID_WP,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_DUBLETTEN_ABWEICHEND,
                                      name, k.DublettenAbweichend));

                // K23: NULL = kein Zuschlag; ein Wert außerhalb 0 <= x < 1 kommt nicht aus dem
                // Schreibweg und wird benannt verworfen.
                double hilfsstromanteil = 0.0;
                if (hilfs != null)
                {
                    double h = StilleDb.Kommazahl(hilfs);
                    if (h >= 0 && h < 1) hilfsstromanteil = h;
                    else
                        Protokoll.WarnungEinmal("kuehl-wp-hilfsstrom-" + m.ID_WP,
                            string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_HILFSSTROM_UNGUELTIG,
                                          name, h));
                }

                // E34 (Kühlkonzept 6.1): Nur ein ABWEICHENDER Kühlträger wirkt - dann gilt die
                // Abrechnungsart der Anlage (NULL = anteilig am Netzbezug, 1 = eigener Zähler).
                // Ohne Kühlträger oder mit dem Stromträger des Projekts läuft der Kältestrom wie der
                // Wärmepumpenstrom und trägt Tarif und Faktor des Projekts.
                if (projekttraeger < 0) projekttraeger = Kaeltestromabrechnung.Projekttraeger(m_ID_Projekt);
                int kuehltraeger = Kaeltestromabrechnung.Abweichend(m.Kuehl_ID_Carrier, projekttraeger)
                    ? m.Kuehl_ID_Carrier.Value : 0;
                bool eigenerZaehler = kuehltraeger > 0 && m.Kuehl_EigenerZaehler == true;
                if (kuehltraeger > 0)
                    Protokoll.HinweisEinmal("kuehl-wp-kuehltraeger-" + m.ID,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_KUEHLTRAEGER,
                                      name, Emissionsquelle.TraegerName(kuehltraeger),
                                      eigenerZaehler ? MyResource.Resource.SIMENG_KAELTE_ABRECHNUNG_ZAEHLER
                                                     : MyResource.Resource.SIMENG_KAELTE_ABRECHNUNG_ANTEILIG));

                // KU3-6 (F2): freie Kühlung über die Wärmequelle nur mit einer Quelle, die sie trägt -
                // sonst benannt abgelehnt, der Schalter bleibt ohne Wirkung.
                bool freieKuehlung = false;
                if (m.Kuehl_Frei)
                {
                    freieKuehlung = FreieKuehlungSoleMoeglich(wpTyp, wqTyp);
                    if (!freieKuehlung)
                        Protokoll.WarnungEinmal("kuehl-wp-frei-ohne-quelle-" + m.ID,
                            string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_FREI_OHNE_QUELLE, name));
                }

                _kaelteerzeuger.Add(new Kaelteerzeuger
                {
                    AnlagenID = m.ID,
                    IdWp = m.ID_WP,
                    Bezeichner = name,
                    Modulindex = i,
                    Kennlinie = k,
                    Schar = new KuehlkennlinienSchar(kuehlZeilen),
                    Hilfsstromanteil = hilfsstromanteil,
                    Quelltemperatur = i < simulation_wp.Quelltemperaturen.Count ? simulation_wp.Quelltemperaturen[i] : null,
                    FreieKuehlungSole = freieKuehlung,
                    FreieKuehlungGraedigkeitK = m.Kuehl_Frei_Graedigkeit_K ?? KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K,
                    FreieKuehlungLeistungKw = m.Kuehl_Frei_Leistung_kW,
                    KuehlVorlaufC = kuehlVorlauf ?? k.Vorlauf,
                    Kuehltraeger = kuehltraeger,
                    EigenerZaehler = eigenerZaehler,
                    // Welle M4, WP1: der Taktverlust gilt auch im Kühlbetrieb - die Mindestleistung als
                    // Anteil der Heiz-Nennleistung (Tab_WP.Nennleistung); ohne beide keine Taktrechnung.
                    Mindestanteil = Kaeltekaskade.Mindestanteil(simulation_wp.TaktMindestleistung(i), m.Grenzleistung),
                    Cd = simulation_wp.TaktCd(i)
                });
            }

            // KU3-4: die Kältemaschinen nach den Wärmepumpen - der Typ Kältemaschine folgt den Wärmepumpen.
            bool mitWaermepumpe = _kaelteerzeuger.Count > 0;
            KaeltemaschinenVorbereiten(erhoben, ref projekttraeger);

            // KB-A: die Folge der Kälteerzeuger aus der einen Quelle (Kaeltefolge) - dieselbe Regel liest der
            // Bereich „Kälte“ der Simulationskonfiguration. KB-D: gepflegte Ränge vorn, ohne Rang die Vorgabefolge.
            _kaelteerzeuger = Kaeltefolge.ErzeugerOrdnen(_kaelteerzeuger, Kaeltefolge.RaengeLesen(m_ID_Projekt));

            if (_kaelteerzeuger.Count == 0) return;

            // K8a (5.2): die Tagesbetriebsart aus den Tagessummen des PROJEKTbedarfs - Heizkanal
            // gegen Kühlkanal, vor jedem Erzeuger.
            Kanalsatz bedarf = simulation_Waermebedarf.KanaeleDrei();
            _kuehltage = Kaeltekaskade.TagesbetriebsartBestimmen(bedarf.Heizung, bedarf.Kuehlung);

            if (!mitWaermepumpe) return;
            bool[] module = new bool[simulation_wp.wp_model.Count];
            foreach (Kaelteerzeuger e in _kaelteerzeuger)
                if (e.Modulindex >= 0) module[e.Modulindex] = true;
            simulation_wp.KuehlbetriebSetzen(module, _kuehltage);
        }

        /// <summary>
        /// <b>Vorgabe des zweiten Feldlaufs</b> (Konzept Simulationsablauf 23.4, 23.5) aus dem eben
        /// beendeten Lauf — null, wenn kein Sondenfeld gerechnet hat, der Lauf gescheitert ist oder er
        /// selbst schon der zweite war. Je Sondenfeld: die gemeldete Entzugsreihe und die Kühlwärme,
        /// die Wärmepumpen dieses Feldes im Kühlbetrieb abgegeben haben, Q_ab = Kälte + Verdichterarbeit
        /// (Kältestrom ohne Hilfsstromzuschlag und ohne Mehrstrom aus Taktverlust — der geht wie im
        /// Heizbetrieb nicht als Wärme über die Sonde; bei freier Kühlung die Kälte samt Pumpenarbeit).
        /// Kältemaschinen speisen nicht ins Erdreich — ihre Rückkühlung arbeitet gegen die Luft oder
        /// ein Kühlwerk (<see cref="Kaelteerzeuger.Maschine"/> gesetzt, kein Wärmepumpenmodul).
        /// </summary>
        /// <summary>
        /// Regeneration des Sondenfeldes durch Kühlwärme (Konzept 23.5); nur Tests schalten sie ab, um
        /// ihre Wirkung zu messen.
        /// </summary>
        internal bool RegenerationRechnen = true;

        /// <summary>
        /// Zweiter Feldlauf der Erdsonde (Konzept 23.4); nur Tests schalten ihn ab, um Jahr 1 der
        /// Startschätzung und die Rechenzeit daneben zu legen.
        /// </summary>
        internal bool ZweitenFeldlaufRechnen = true;

        private Dictionary<int, SimulationWaermepumpe.Feldvorgabe> FeldvorgabeAusLauf()
        {
            if (!ZweitenFeldlaufRechnen || m_bError || !string.IsNullOrEmpty(Sperrgrund) || !_wpInSchleife) return null;
            if (simulation_wp.ZweiterFeldlauf || simulation_wp.Sondenfelder.Count == 0) return null;

            var vorgabe = new Dictionary<int, SimulationWaermepumpe.Feldvorgabe>();
            foreach (KeyValuePair<int, Erdsondenfeld> paar in simulation_wp.Sondenfelder)
            {
                double[] rueck = new double[Kanalsatz.STUNDEN_JAHR];
                if (_kaelteerzeuger != null && RegenerationRechnen)
                    foreach (Kaelteerzeuger e in _kaelteerzeuger)
                    {
                        if (e.Maschine != null || e.Modulindex < 0) continue;
                        if (!ReferenceEquals(simulation_wp.Sondenfeld(e.Modulindex), paar.Value)) continue;
                        double zuschlag = 1.0 + e.Hilfsstromanteil;
                        for (int h = 0; h < rueck.Length; h++)
                        {
                            double kaelte = e.Kaelte_stuendlich[h];
                            if (!(kaelte > 0)) continue;
                            rueck[h] += kaelte + e.Strom_stuendlich[h] / zuschlag - e.Taktstrom_stuendlich[h];
                        }
                    }

                double[] entzug = paar.Value.LastKw();
                double[] netto = new double[entzug.Length];
                for (int h = 0; h < netto.Length; h++) netto[h] = entzug[h] - rueck[h];
                vorgabe[paar.Key] = new SimulationWaermepumpe.Feldvorgabe { VorjahrLastKw = netto, RueckspeisungKw = rueck };
            }
            return vorgabe;
        }

        /// <summary>
        /// <b>Trägt die Wärmequelle die freie Kühlung?</b> (KU3-6, F2): Bauart Sole-Wasser oder Wasser-Wasser
        /// und eine gepflegte Quelle — <c>WQ_Typ</c> Erdreich, Konstant, Profil oder CSV. Luft-Wasser, die
        /// leere Quelle (Außenluft-Rückfall), Außenluft und der Pufferspeicher scheiden aus.
        /// </summary>
        internal static bool FreieKuehlungSoleMoeglich(string wpTyp, string wqTyp)
        {
            if (wpTyp != DbWerte.WP_BAUART_SOLE_WASSER && wpTyp != DbWerte.WP_BAUART_WASSER_WASSER) return false;
            return wqTyp == WaermequelleClass.TYP_ERDREICH || wqTyp == WaermequelleClass.TYP_KONSTANT ||
                   wqTyp == WaermequelleClass.TYP_PROFIL || wqTyp == WaermequelleClass.TYP_CSV;
        }

        /// <summary>
        /// <b>Die Kältemaschinen des Projekts</b> (KU3-4; Kühlkonzept 5.3, 5.5, 6.1): jede Anlagenzeile mit Typ
        /// <see cref="KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE"/> und Verweis auf ihre Projektkopie ist eine
        /// Maschine — mit Anzahl, Kaltwasservorlauf (<c>Kuehl_Vorlauf</c>), Hilfsstromanteil, Kühlträger und
        /// Abrechnungsart. Die Plätze <c>Tool_1</c> bis <c>Tool_4</c> ordnen die Erzeugertypen der Wärmeseite;
        /// gefiltert auf die kühlfähigen Erzeuger folgt die Kältemaschine als Typ den Wärmepumpen im Kühlbetrieb,
        /// innerhalb des Typs in der Reihenfolge ihrer Anlagenzeilen; in Stunden freier Kühlung deckt sie vor allen
        /// anderen (<see cref="Kaeltekaskade.Rechnen"/>). Eine Projektkopie ohne Anlagenzeile rechnet nicht und
        /// wird benannt.
        /// </summary>
        private void KaeltemaschinenVorbereiten(bool erhoben, ref int projekttraeger)
        {
            IReadOnlyList<KaeltemaschineAnlageModel> anlagen = Kaeltefolge.KaeltemaschinenOrdnen(KaeltemaschineAnlageCtrl.ListeStill(m_ID_Projekt));
            var gefuehrt = new HashSet<int>(anlagen.Where(a => a.IdKaeltemaschine.HasValue).Select(a => a.IdKaeltemaschine.Value));
            foreach (int id in KaeltemaschineCtrl.IdsImProjektStill(m_ID_Projekt))
                if (!gefuehrt.Contains(id))
                {
                    KaeltemaschineModel ohne = KaeltemaschineCtrl.LadenStill(id);
                    Protokoll.HinweisEinmal("kuehl-km-ohne-anlage-" + id,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_OHNE_ANLAGE,
                                      ohne != null && !string.IsNullOrEmpty(ohne.Bezeichner) ? ohne.Bezeichner : id.ToString(CultureInfo.CurrentCulture)));
                }
            if (anlagen.Count == 0) return;

            double[] feuchte = simulation_Waermebedarf != null ? simulation_Waermebedarf.Luftfeuchte_stuendlich() : null;
            int angelegt = 0;
            foreach (KaeltemaschineAnlageModel a in anlagen)
            {
                if (!a.IdKaeltemaschine.HasValue)
                {
                    Protokoll.WarnungEinmal("kuehl-km-anlage-ohne-geraet-" + a.AnlagenId,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_ANLAGE_OHNE_GERAET, a.Bezeichner));
                    continue;
                }
                int id = a.IdKaeltemaschine.Value;
                KaeltemaschineModel m = KaeltemaschineCtrl.LadenStill(id);
                if (m == null) continue;
                Kaeltemaschine k = Kaeltemaschine.AusModell(m, out bool angehoben);
                if (!string.IsNullOrWhiteSpace(a.Bezeichner)) k.Bezeichner = a.Bezeichner;
                k.Anzahl = Math.Max(1, a.Anzahl);
                // KM3: eine gewählte, aber verworfene Teillastkurve rechnet linear mit Taktverlust - einmal je Maschine gemeldet.
                if (k.Teillast != null && k.Teillast.Herkunft == KaeltemaschinenKurvenherkunft.Verworfen)
                    Protokoll.HinweisEinmal("kuehl-km-kurve-verworfen-" + id,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_KURVE_VERWORFEN, k.Bezeichner));
                if (!erhoben)
                {
                    Protokoll.HinweisEinmal("kuehl-km-projekt-aus-" + id,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_PROJEKT_AUS, k.Bezeichner));
                    continue;
                }
                if (k.Kennlinie.Leer)
                {
                    Protokoll.WarnungEinmal("kuehl-km-ohne-kennlinie-" + id,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_OHNE_KENNLINIE, k.Bezeichner));
                    continue;
                }
                if (angehoben)
                    Protokoll.HinweisEinmal("kuehl-km-kaltwasser-" + id,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_KALTWASSER_ANGEHOBEN,
                                      k.Bezeichner, k.Kaltwassertemperatur.ToString("F1", CultureInfo.CurrentCulture)));

                // K-F1: das gewählte Rückkühlwerk der Anlagenzeile - seine Bauart gilt statt der Rückkühlart, Rückkühltemperatur
                // und freie Kühlung nehmen denselben Weg; ohne Rückkühlwerk unverändert die Rückkühlart mit den Festwerten.
                RueckkuehlwerkAnsetzen(a, k);
                k.Rueckkuehltemperatur_stuendlich = k.RueckkuehltemperaturenBilden(Stundentemperatur, feuchte, out int ohneFeuchte);
                if (ohneFeuchte > 0)
                    Protokoll.HinweisEinmal("kuehl-km-feuchte-" + id,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_NASSKUEHLER_OHNE_FEUCHTE,
                                      k.Bezeichner, ohneFeuchte));

                // K23 wie bei der Wärmepumpe: NULL = kein Zuschlag; ein Wert außerhalb 0 <= x < 1 wird benannt verworfen.
                double hilfsstromanteil = 0.0;
                if (m.Kuehl_Hilfsstromanteil.HasValue)
                {
                    double h = m.Kuehl_Hilfsstromanteil.Value;
                    if (h >= 0 && h < 1) hilfsstromanteil = h;
                    else
                        Protokoll.WarnungEinmal("kuehl-km-hilfsstrom-" + id,
                            string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_HILFSSTROM_UNGUELTIG,
                                          k.Bezeichner, h));
                }

                // E34 (6.1): nur ein ABWEICHENDER Kühlträger wirkt - dann gilt die Abrechnungsart der Anlage.
                if (projekttraeger < 0) projekttraeger = Kaeltestromabrechnung.Projekttraeger(m_ID_Projekt);
                int kuehltraeger = Kaeltestromabrechnung.Abweichend(a.KuehlIdCarrier, projekttraeger)
                    ? a.KuehlIdCarrier.Value : 0;
                bool eigenerZaehler = kuehltraeger > 0 && a.KuehlEigenerZaehler == true;
                if (kuehltraeger > 0)
                    Protokoll.HinweisEinmal("kuehl-km-kuehltraeger-" + a.AnlagenId,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_KUEHLTRAEGER,
                                      k.Bezeichner, Emissionsquelle.TraegerName(kuehltraeger),
                                      eigenerZaehler ? MyResource.Resource.SIMENG_KAELTE_ABRECHNUNG_ZAEHLER
                                                     : MyResource.Resource.SIMENG_KAELTE_ABRECHNUNG_ANTEILIG));

                _kaelteerzeuger.Add(new Kaelteerzeuger
                {
                    AnlagenID = a.AnlagenId,
                    IdWp = 0,
                    Bezeichner = k.Bezeichner,
                    Modulindex = -1,
                    Maschine = k,
                    Hilfsstromanteil = hilfsstromanteil,
                    Zeitanteil = null,
                    Quelltemperatur = k.Rueckkuehltemperatur_stuendlich,
                    Kuehltraeger = kuehltraeger,
                    EigenerZaehler = eigenerZaehler,
                });
                angelegt++;
            }
            if (angelegt > 0)
                Protokoll.HinweisEinmal("kuehl-km-reihenfolge", MyResource.Resource.SIMENG_KAELTE_KM_REIHENFOLGE);
        }

        /// <summary>
        /// <b>Das Rückkühlwerk einer Kältemaschinen-Anlage</b> (K-F1; Entwurf Split/VRF/Rückkühlwerk 5.1–5.3): liest die
        /// Projektkopie aus <c>ID_Rueckkuehlwerk</c> über <see cref="RueckkuehlwerkCtrl.LadenStill"/> und setzt sie an die
        /// Maschine. Eine luftgekühlte Maschine kennt kein Rückkühlwerk — es wird dann mit Hinweis übergangen. Was K-F1 am
        /// Rückkühlwerk noch nicht rechnet, wird einmal je Anlage benannt; gerechnet wird der Weg <c>FEST</c>.
        /// </summary>
        private void RueckkuehlwerkAnsetzen(KaeltemaschineAnlageModel a, Kaeltemaschine k)
        {
            if (!a.IdRueckkuehlwerk.HasValue) return;
            Rueckkuehlwerk r = Rueckkuehlwerk.AusModell(RueckkuehlwerkCtrl.LadenStill(a.IdRueckkuehlwerk.Value));
            if (r == null) return;
            if (string.Equals(k.Rueckkuehlart, KaeltemaschineSchema.RUECKKUEHLART_LUFT, StringComparison.Ordinal))
            {
                Protokoll.HinweisEinmal("kuehl-rkw-luft-" + a.AnlagenId,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_RKW_LUFT_IGNORIERT,
                                  k.Bezeichner, r.Bezeichner));
                return;
            }
            k.RueckkuehlwerkSetzen(r);
            if (r.NichtGerechnet.Count > 0)
                Protokoll.HinweisEinmal("kuehl-rkw-nicht-gerechnet-" + a.AnlagenId,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_RKW_NICHT_GERECHNET,
                                  k.Bezeichner, r.Bezeichner, RueckkuehlwerkMerkmale(r.NichtGerechnet)));
        }

        /// <summary>Die nicht gerechneten Merkmale eines Rückkühlwerks als Aufzählung in der Sprache des Laufs.</summary>
        internal static string RueckkuehlwerkMerkmale(IReadOnlyList<string> merkmale)
        {
            var namen = new List<string>();
            foreach (string m in merkmale)
                switch (m)
                {
                    case Rueckkuehlwerk.MERKMAL_LASTABHAENGIG: namen.Add(MyResource.Resource.SIMENG_KAELTE_RKW_MERKMAL_LASTABHAENGIG); break;
                    case Rueckkuehlwerk.MERKMAL_VENTILATOR: namen.Add(MyResource.Resource.SIMENG_KAELTE_RKW_MERKMAL_VENTILATOR); break;
                    case Rueckkuehlwerk.MERKMAL_BEFEUCHTUNG: namen.Add(MyResource.Resource.SIMENG_KAELTE_RKW_MERKMAL_BEFEUCHTUNG); break;
                    case Rueckkuehlwerk.MERKMAL_WASSERBILANZ: namen.Add(MyResource.Resource.SIMENG_KAELTE_RKW_MERKMAL_WASSERBILANZ); break;
                    case Rueckkuehlwerk.MERKMAL_REIHE: namen.Add(MyResource.Resource.SIMENG_KAELTE_RKW_MERKMAL_REIHE); break;
                    default: namen.Add(m); break;
                }
            return string.Join("; ", namen);
        }

        // =====================================================================
        //  2. Nach der Speicherstufe: die Kältekaskade und ihr Strom
        // =====================================================================

        /// <summary>
        /// Rechnet die <see cref="Kaeltekaskade"/> — nach der Speicherstufe, in der die reversiblen
        /// Maschinen ihren Heizzeitanteil festgelegt haben — und führt ihren Kältestrom an der
        /// Stelle des Wärmepumpenstroms in die Stufenrechnung (6.1): Jahressumme in
        /// <see cref="ReststromMwh"/>, Viertelstundenverlauf in den Rest, aus dem Eigenverbrauch,
        /// Speicher und Netzbezug gerechnet werden.
        /// </summary>
        /// <param name="kanaele">Der Kanalsatz der Kaskade — nur für die Deckungsprobe gelesen.</param>
        private void KaeltekaskadeRechnen(Kanalsatz kanaele)
        {
            _kaeltespeicher = new List<SimulationPufferspeicher>();
            if (m_bError) return;
            SimulationKaeltebedarf kaelte = simulation_Waermebedarf != null ? simulation_Waermebedarf.Kaelteseite : null;
            bool gerechnet = kaelte != null && kaelte.Gerechnet;
            bool mitErzeuger = _kaelteerzeuger != null && _kaelteerzeuger.Count > 0;
            if (!gerechnet || !mitErzeuger)
            {
                KaeltespeicherOhneRechnungMelden(gerechnet);
                return;
            }

            // AK3-K (Festlegung 16): Im AK3-Weg hat der Kreis die Kältestunden schon gerechnet.
            Kaeltekaskade stuendlich = _kaeltestunde;
            _kaeltestunde = null;
            foreach (Kaelteerzeuger e in stuendlich != null ? new List<Kaelteerzeuger>() : _kaelteerzeuger)
            {
                if (e.Maschine != null) continue;   // KU3-2: die Kältemaschine kennt keinen Heizzeitanteil
                WErzeugerModel m = simulation_wp.wp_model[e.Modulindex];
                double[] heiz = simulation_wp.Heizzeitanteil_stuendlich != null &&
                                e.Modulindex < simulation_wp.Heizzeitanteil_stuendlich.Length
                    ? simulation_wp.Heizzeitanteil_stuendlich[e.Modulindex] : null;
                // V14: das Sperrprofil des Moduls (ohne Tab_Sperrfenster genau das Altfenster).
                Sperrprofil sperre = simulation_wp.SperrprofilDesModuls(e.Modulindex);
                e.Zeitanteil = sperre != null
                    ? Kaeltekaskade.ZeitanteilBilden(_kuehltage, heiz, sperre.Verdichter)
                    : Kaeltekaskade.ZeitanteilBilden(_kuehltage, heiz, m.Sperrung, m.Sperrzeit_von, m.Sperrzeit_bis);
            }

            // Deckungsprobe, dritte Aussage: die Wärmekanäle vor der Kältekaskade festhalten.
            double[][] vorher = null;
            if (kanaele != null)
            {
                vorher = new double[Kanal.ANZAHL][];
                foreach (int k in Kanal.KANAELE_WAERME) vorher[k] = (double[])kanaele.Bedarf[k].Clone();
            }

            Kaeltekaskade kaskade;
            if (stuendlich != null)
            {
                kaskade = stuendlich;
                kaskade.Abschliessen();
            }
            else
            {
                kaskade = new Kaeltekaskade { Erzeuger = _kaelteerzeuger, Kuehltage = _kuehltage,
                                              Speicher = KaeltespeicherLesen() };
                kaskade.Rechnen(kaelte.Kaeltebedarf, simulation_wp != null && simulation_wp.Extrapolation_Erlaubt);
            }
            kaelte.DeckungUebernehmen(kaskade);

            _waermekanalAbweichungen = 0;
            if (vorher != null)
                for (int h = 0; h < Kanalsatz.STUNDEN_JAHR; h++)
                    foreach (int k in Kanal.KANAELE_WAERME)
                        if (kanaele.Bedarf[k][h] != vorher[k][h]) { _waermekanalAbweichungen++; break; }

            // 6.1: der Kältestrom als eigene Reihe in die Stufenrechnung - addiert im Rest, nicht in
            // der Reihe der Wärmepumpe (5.1, Festlegung 5). E34: OHNE den Kältestrom der Anlagen mit
            // eigenem Zähler - der läuft neben der Stufenrechnung, wird also weder aus PV-Eigenstrom
            // noch aus dem Stromspeicher gedeckt. Ohne eigenen Zähler ist die Reihe die der Kaskade.
            double stufeKwh;
            Kaeltestrom_Stufenrechnung_stuendlich = KaeltestromDerStufenrechnung(kaskade, out stufeKwh);
            ReststromMwh += stufeKwh / 1000.0;
            double[] temp = Stundenwerte_zu_viertelstunden(Kaeltestrom_Stufenrechnung_stuendlich);
            Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);

            KennlinienlageMelden(kaskade);

            // KU3-5: Die Kältespeicher stehen erst nach der Kaskade als Ergebnis bereit.
            _kaeltespeicher = kaskade.Speicher;
            foreach (SimulationPufferspeicher sp in _kaeltespeicher)
                Protokoll.Hinweis(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTESPEICHER_BETRIEB,
                    sp.BezeichnerAnzeige(), sp.Q_max.ToString("N1", CultureInfo.CurrentCulture),
                    (sp.Entladung_gesamt / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                    (sp.Ladung_gesamt / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                    (sp.Verluste_gesamt / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                    sp.Vollzyklen.ToString("N1", CultureInfo.CurrentCulture)));
        }

        /// <summary>Die Kältekaskade, die der Kreis je Stunde rechnet (AK3-K); <c>null</c> = Jahreslauf nach der Wärme.</summary>
        private Kaeltekaskade _kaeltestunde;

        /// <summary>Die Sperrmasken der Wärmepumpen im Kühlbetrieb (AK3-K) — dieselben wie in der Kältestunde.</summary>
        private Dictionary<Kaelteerzeuger, bool[]> _ak3Kaeltemasken;

        /// <summary>
        /// <b>Kälte-Restbedarf im Kreis</b> (AK3-K, Festlegung 14): Stunden, in denen die Kältestunde einen Rest lässt —
        /// die Abweichung der Vorrangschätzung der Kälteschranke zur echten Kältestunde; gezählt, nicht nachiteriert.
        /// </summary>
        internal int Ak3KaelteRestStunden { get; private set; }

        /// <summary>Der Kälte-Restbedarf dieser Stunden [kWh] (AK3-K, Festlegung 14).</summary>
        internal double Ak3KaelteRestKwh { get; private set; }

        /// <summary>
        /// Prüfauftrag K3 (Entwurf 4.5): true, solange die Kälteschranke nach der Wärmestunde mit dem wirklichen
        /// Heizzeitanteil nachgerechnet wird — nur zum Vergleich mit der Vorrangschätzung, ohne Wirkung auf den Lauf.
        /// </summary>
        private bool _ak3HeizzeitanteilEcht;

        /// <summary>Der wirkliche Heizzeitanteil des Moduls <paramref name="modul"/> in der eben gerechneten Wärmestunde.</summary>
        private double EchterHeizzeitanteil(int modul, int stunde)
        {
            double[] heiz = simulation_wp?.Heizzeitanteil_stuendlich != null && modul < simulation_wp.Heizzeitanteil_stuendlich.Length
                ? simulation_wp.Heizzeitanteil_stuendlich[modul] : null;
            return heiz != null && stunde < heiz.Length ? heiz[stunde] : 0.0;
        }

        /// <summary>
        /// <b>Die Kälteschranke des Kreises</b> (AK3-K 4.2, 4.3, Festlegung 11): die Kälteerzeuger der Kältestunde als
        /// Kapazitäten — Wärmepumpen im Kühlbetrieb mit Erzeugertagesart, Sperrmaske und der Vorrangschätzung aus ihrer
        /// Heizkapazität im Kreis, Kältemaschinen aus ihrer Kennlinie —, die Kältespeicher der Kältestunde über den
        /// Kältespeicherleser und der Kühlvorlauf der Anlage. <c>null</c> ohne Kältestunde im Kreis.
        /// </summary>
        private Kaelteschranke Ak3KaelteschrankeBauen(IReadOnlyList<IErzeugerkapazitaet> waerme)
        {
            if (_kaeltestunde == null || _kaelteerzeuger == null) return null;
            bool extrapolation = simulation_wp != null && simulation_wp.Extrapolation_Erlaubt;
            var erzeuger = new List<IKaelteerzeugerkapazitaet>();
            Kaelteschranke schranke = null;
            foreach (Kaelteerzeuger e in _kaelteerzeuger)
            {
                if (e.Maschine != null)
                {
                    erzeuger.Add(new KaeltemaschineKapazitaet(e));
                    continue;
                }
                bool[] maske = _ak3Kaeltemasken != null && _ak3Kaeltemasken.TryGetValue(e, out bool[] m) ? m : null;
                var wp = new WaermepumpeKaeltekapazitaet(e, _kuehltage, maske, extrapolation);
                // Die Heizkapazität derselben Maschine im Kreis (Modul der Anlagenzeile); ohne sie keine Schätzung.
                WaermepumpeKapazitaet heiz = null;
                // Die Erzeuger vor der Wärmepumpe in der Kaskade: Sie tragen den Vorrang der Stunde zuerst (K5a).
                var vorgelagert = new List<IErzeugerkapazitaet>();
                if (simulation_wp != null && e.Modulindex >= 0 && e.Modulindex < simulation_wp.wp_list.Count)
                {
                    int id = simulation_wp.wp_list[e.Modulindex];
                    int stelle = 0;
                    foreach (IErzeugerkapazitaet k in waerme)
                    {
                        if (k is WaermepumpeKapazitaet w && _ak3WaermeAnlagen != null && stelle < _ak3WaermeAnlagen.Count
                            && _ak3WaermeAnlagen[stelle] == id) { heiz = w; break; }
                        vorgelagert.Add(k);
                        stelle++;
                    }
                }
                if (heiz != null)
                {
                    WaermepumpeKapazitaet h = heiz;
                    int modul = e.Modulindex;
                    wp.Heizzeitanteil = stunde =>
                    {
                        if (_ak3HeizzeitanteilEcht) return EchterHeizzeitanteil(modul, stunde);
                        // Gibt der Fahrplan die Heizseite nicht frei (Sperrzeit, Zeitprogramm, Umschaltung am Kühltag),
                        // heizt die Wärmepumpe in der Stunde nicht - sie trägt keinen Vorrang (K5a).
                        Erzeugerangebot eigen = h.Abfragen(stunde, double.NaN);
                        if (!(eigen.VerfuegbarKw > 0.0)) return 0.0;
                        double vor = 0.0;
                        foreach (IErzeugerkapazitaet k in vorgelagert) vor += k.Abfragen(stunde, double.NaN).VerfuegbarKw;
                        return Kaelteschranke.Heizzeitanteil(schranke.VorrangDerStunde, eigen.KapazitaetKw, vor);
                    };
                }
                erzeuger.Add(wp);
            }
            schranke = new Kaelteschranke(erzeuger, new Kaeltespeicherleser(_kaeltestunde.Speicher),
                                          simulation_Waermebedarf.KuehlVorlaufAnlageC);
            return schranke;
        }

        /// <summary>
        /// <b>Die Kältestunde im Kreis</b> (AK3-K, Entwurf 4.1 Schritt 5, Festlegung 16), nur im AK3-Weg: Kälteerzeuger (ohne Wärmepumpe in der Schleife hier statt nach der Wärme),
        /// Kältespeicher und die Kältekaskade werden vor der Stundenschleife angelegt; die zurückgegebene Aktion rechnet je
        /// Stunde nach der Wärmestunde den Kühlzeitanteil der Wärmepumpen aus ihrem eben gerechneten Heizzeitanteil und
        /// die Kältestunde mit dem Kältebedarf des Kreises (Pass 1 plus Abweichung der Stunde). Danach schließt
        /// <see cref="KaeltekaskadeRechnen"/> das Jahr ab, statt es zu rechnen. <c>null</c> = der Jahreslauf.
        /// </summary>
        private Action<int> Ak3KaeltestundeEinrichten()
        {
            _kaeltestunde = null;
            Ak3Weg weg = Stundenbedarf is Ak3Stundenbedarf ? simulation_Waermebedarf?.Ak3 : null;
            if (weg == null || m_bError) return null;
            SimulationKaeltebedarf kaelte = simulation_Waermebedarf.Kaelteseite;
            if (kaelte == null || !kaelte.Gerechnet) return null;
            if (!_wpInSchleife) KaelteerzeugerVorbereiten();
            if (_kaelteerzeuger == null || _kaelteerzeuger.Count == 0) return null;

            var masken = new Dictionary<Kaelteerzeuger, bool[]>();
            _ak3Kaeltemasken = masken;
            Ak3KaelteRestStunden = 0;
            Ak3KaelteRestKwh = 0.0;
            foreach (Kaelteerzeuger e in _kaelteerzeuger)
            {
                if (e.Maschine != null) continue;
                WErzeugerModel m = simulation_wp.wp_model[e.Modulindex];
                Sperrprofil sperre = simulation_wp.SperrprofilDesModuls(e.Modulindex);
                masken[e] = sperre != null
                    ? sperre.Verdichter
                    : Sperrprofil.Bilden(m.Sperrung, m.Sperrzeit_von, m.Sperrzeit_bis, null, 0).Verdichter;
                e.Zeitanteil = new double[Kanalsatz.STUNDEN_JAHR];
            }
            var kaskade = new Kaeltekaskade { Erzeuger = _kaelteerzeuger, Kuehltage = _kuehltage,
                                              Speicher = KaeltespeicherLesen(), ImKreis = true };
            kaskade.Beginnen(simulation_wp != null && simulation_wp.Extrapolation_Erlaubt);
            _kaeltestunde = kaskade;
            return h =>
            {
                foreach (KeyValuePair<Kaelteerzeuger, bool[]> z in masken)
                {
                    double[] heiz = simulation_wp.Heizzeitanteil_stuendlich != null &&
                                    z.Key.Modulindex < simulation_wp.Heizzeitanteil_stuendlich.Length
                        ? simulation_wp.Heizzeitanteil_stuendlich[z.Key.Modulindex] : null;
                    z.Key.Zeitanteil[h] = Kaeltekaskade.ZeitanteilDerStunde(_kuehltage, heiz, z.Value, h);
                }
                // Prüfauftrag K3 (4.5): die Kälteschranke mit dem wirklichen Heizzeitanteil gegen die Vorrangschätzung —
                // am Zustand des Stundenbeginns (Kältespeicher erst in der Kältestunde geschrieben), ohne Wirkung.
                Anlagenkopplung kreis = weg.Kreis;
                Kopplungsstunde ks = kreis?.LetzteStunde;
                // KK3 (Entwurf KK 2.5): der Erzeugervorlauf der konvergierten Stunde; NaN = der feste Vorlauf (bitgleich).
                double vK = ks != null && ks.Jahresstunde == h ? ks.KuehlVorlaufC : double.NaN;
                if (ks != null && ks.Jahresstunde == h && ks.KaelteschrankeGreift && kreis.Kaelteschranke != null)
                {
                    _ak3HeizzeitanteilEcht = true;
                    try
                    {
                        Kaelteschranke ksr = kreis.Kaelteschranke;
                        kreis.VorrangschaetzungPruefen(ks, ksr.Angebot(h, ksr.VorrangDerStunde, vK).LeistungKw);
                    }
                    finally { _ak3HeizzeitanteilEcht = false; }
                }
                double d = weg.KaelteDeltaKwh[h];
                kaskade.StundeRechnen(h, d != 0.0 ? kaelte.Kaeltebedarf[h] + d : kaelte.Kaeltebedarf[h], vK);
                // Festlegung 14: der Rest der echten Kältestunde ist die Abweichung zur Schätzung der Kälteschranke.
                double rest = kaskade.Rest_stuendlich[h];
                kreis?.KaelteRestZaehlen(rest);
                if (rest > 0.0)
                {
                    Ak3KaelteRestStunden++;
                    Ak3KaelteRestKwh += rest;
                }
            };
        }

        /// <summary>
        /// <b>Die Kältespeicher des Projekts</b> (KU3-5, E68; Kühlkonzept 4.6, 5.5): jeder Projektpuffer mit
        /// der Verwendung <see cref="SimulationPufferspeicher.VERWENDUNG_KAELTE"/> — ohne Senkenzeile, denn
        /// die Kälteseite hat nur einen Kanal und alle Kälteerzeuger laden ihn. Temperaturpaar aus der
        /// Projektkopie (Vorgabe 6/12 °C, wenn es leer oder vertauscht ist), Schwellen und Leistungsgrenzen
        /// wie beim Wärmepuffer; Reihenfolge nach Entladepriorität (0 = automatisch, hinten), sonst nach
        /// Bezeichner.
        /// </summary>
        private List<SimulationPufferspeicher> KaeltespeicherLesen()
        {
            var liste = new List<SimulationPufferspeicher>();
            foreach (WaermesenkeClass.PufferInfo p in WaermesenkeClass.ProjektPufferListe(m_ID_Projekt, WaermesenkeClass.VERWENDUNG_KAELTE))
            {
                var sp = new SimulationPufferspeicher
                {
                    Bezeichner = p.Bezeichner,
                    Erzeuger = DbWerte.PSP_VERWENDUNG_KAELTE,
                    ID_Pufferspeicher = p.ID,
                    ID_Projekt = p.ID_Projekt,
                };
                sp.InitKaelte(p.Gesamtvolumen, p.Vorlauf, p.Ruecklauf, p.Bereitschaftsverluste);
                sp.SchwelleEin = p.SchwelleEin / 100.0;
                sp.SchwelleAus = p.SchwelleAus / 100.0;
                sp.SchwelleAusNachrang = p.SchwelleAusNachrang / 100.0;
                sp.Entladeprio = p.Entladeprio;
                LeistungsgrenzenUebernehmen(sp);
                sp.ImRechenpfad = true;
                if (sp.KaeltepaarVorgabe)
                    Protokoll.HinweisEinmal("kaeltespeicher-paar-" + p.ID,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTESPEICHER_PAAR_VORGABE,
                                      sp.BezeichnerAnzeige(), sp.KaltVorlauf, sp.KaltRuecklauf));
                liste.Add(sp);
            }
            // Stabil sortieren: gepflegte Entladepriorität vorn, 0 (automatisch) hinten (KB-A: die Regel der Kaeltefolge).
            return Kaeltefolge.KaeltespeicherOrdnen(liste, sp => sp.Entladeprio);
        }

        /// <summary>Lade- und Entladeleistungsgrenze [kW] eines Kältespeichers aus seiner Projektzeile (0 = unbegrenzt).</summary>
        private void LeistungsgrenzenUebernehmen(SimulationPufferspeicher sp)
        {
            DataRow r = Schichtzeile(sp.ID_Pufferspeicher);
            if (r == null) return;
            sp.LadeleistungMax = StilleDb.Kommazahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_LADELEISTUNG_MAX), 0);
            if (sp.LadeleistungMax < 0) sp.LadeleistungMax = 0;
            sp.EntladeleistungMax = StilleDb.Kommazahl(StilleDb.Feld(r, SchemaKatalog.SPALTE_PSP_ENTLADELEISTUNG_MAX), 0);
            if (sp.EntladeleistungMax < 0) sp.EntladeleistungMax = 0;
        }

        /// <summary>
        /// Benannt statt still (KU3-5): Ein Kältespeicher im Projekt rechnet nicht, wenn das Projekt keine
        /// Kälte rechnet oder kein Kälteerzeuger angelegt ist. Die Projektpufferliste wird nur gelesen,
        /// wenn überhaupt ein Projekt läuft.
        /// </summary>
        private void KaeltespeicherOhneRechnungMelden(bool kaelteGerechnet)
        {
            if (m_ID_Projekt <= 0) return;
            foreach (WaermesenkeClass.PufferInfo p in WaermesenkeClass.ProjektPufferListe(m_ID_Projekt, WaermesenkeClass.VERWENDUNG_KAELTE))
            {
                string name = string.IsNullOrEmpty(p.Bezeichner) ? p.ID.ToString(CultureInfo.CurrentCulture) : p.Bezeichner;
                Protokoll.WarnungEinmal("kaeltespeicher-ohne-rechnung-" + p.ID,
                    string.Format(CultureInfo.CurrentCulture,
                        kaelteGerechnet ? MyResource.Resource.SIMENG_KAELTESPEICHER_OHNE_ERZEUGER
                                        : MyResource.Resource.SIMENG_KAELTESPEICHER_OHNE_KUEHLUNG, name));
            }
        }

        /// <summary>
        /// Der Kältestrom, der durch die Stufenrechnung läuft [kWh je Stunde] — die Reihe der Kaskade,
        /// wenn keine Anlage einen eigenen Zähler führt (dann Zeichen für Zeichen dieselbe Reihe und
        /// dieselbe Jahressumme wie ohne Abrechnungsart), sonst die Summe der übrigen Anlagen (E34).
        /// </summary>
        private static double[] KaeltestromDerStufenrechnung(Kaeltekaskade kaskade, out double summeKwh)
        {
            if (!kaskade.Erzeuger.Any(e => e.NebenDerStufenrechnung))
            {
                summeKwh = kaskade.StromGesamtKwh;
                return (double[])kaskade.Stromverbrauch_Kuehlung_stuendlich.Clone();
            }

            var reihe = new double[Kaeltekaskade.STUNDEN];
            summeKwh = 0.0;
            for (int h = 0; h < Kaeltekaskade.STUNDEN; h++)
                foreach (Kaelteerzeuger e in kaskade.Erzeuger)
                {
                    if (e.NebenDerStufenrechnung) continue;
                    reihe[h] += e.Strom_stuendlich[h];
                    summeKwh += e.Strom_stuendlich[h];
                }
            return reihe;
        }

        // =====================================================================
        //  2a. Am Laufende: der Netzbezug des Kältestroms (E34, Kühlkonzept 6.1)
        // =====================================================================

        /// <summary>
        /// Teilt am Laufende — nach Photovoltaik und Stromspeicher, wenn der Viertelstundenrest der
        /// Netzbezug ist — den Netzbezug auf den Kältestrom jeder Anlage auf
        /// (<see cref="Kaelteerzeuger.NetzbezugKwh"/>): über die Stufenrechnung
        /// Σ Netzbezug(t) · Kältestrom(t) / Stromverbrauch(t) je Viertelstunde, mit dem
        /// Stromverbrauch aus der Lastreihe des Laufs (Gebäude, Wärmepumpe, Heizstab, Kessel,
        /// Kältestrom der Stufenrechnung — dieselbe Reihe, die der Stromspeicher sieht); mit eigenem
        /// Zähler der ganze Kältestrom. Eigenverbrauch aus Photovoltaik und Stromspeicher bleibt so
        /// gemeinsam, ohne Vorrang (E34, Wahl 1).
        ///
        /// <para><b>Nur eine Aufteilung, kein zweiter Rechenweg:</b> Rest, Netzbezug und
        /// <see cref="ReststromMwh"/> bleiben, wie sie sind. Ohne Kältekaskade ein sofortiger
        /// Rücksprung. Bepreist und bewertet wird die Menge einmal, im <c>KostenEmissionRechner</c>
        /// — mit dem Kühlträger, wo er abweicht, sonst mit dem Stromträger des Projekts.</para>
        /// </summary>
        private void KaeltestromNetzbezugAufteilen()
        {
            if (m_bError || _kaelteerzeuger == null || _kaelteerzeuger.Count == 0) return;
            SimulationKaeltebedarf kaelte = simulation_Waermebedarf != null ? simulation_Waermebedarf.Kaelteseite : null;
            if (kaelte == null || kaelte.Kaskade == null) return;

            double[] last = null;
            foreach (Kaelteerzeuger e in kaelte.Kaskade.Erzeuger)
            {
                if (e.NebenDerStufenrechnung) { e.NetzbezugKwh = e.StromGesamtKwh; }
                else if (!(e.StromGesamtKwh > 0)) { e.NetzbezugKwh = 0.0; }
                else
                {
                    if (last == null) last = new StromspeicherSimCtrl().BaueLastreihe(this);
                    e.NetzbezugKwh = Kaeltekaskade.NetzbezugAnteilKwh(Rest_Strombedarf_viertelstuendlich,
                                                                      last, e.Strom_stuendlich);
                }

                if (e.Kuehltraeger > 0)
                    Protokoll.Hinweis(KuehltraegerMengeHinweis(e, Emissionsquelle.TraegerName(e.Kuehltraeger)));
            }
        }

        /// <summary>
        /// Der Hinweis zum Netzbezug des Kältestroms mit abweichendem Kühlträger — für die
        /// Wärmepumpe und die Kältemaschine (<see cref="Kaelteerzeuger.Maschine"/>) je mit eigenem Text.
        /// </summary>
        internal static string KuehltraegerMengeHinweis(Kaelteerzeuger e, string traeger)
        {
            string vorlage = e.Maschine != null
                ? MyResource.Resource.SIMENG_KAELTE_KM_KUEHLTRAEGER_MENGE
                : MyResource.Resource.SIMENG_KAELTE_KUEHLTRAEGER_MENGE;
            return string.Format(CultureInfo.CurrentCulture, vorlage, e.Bezeichner,
                (e.NetzbezugKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                (e.StromGesamtKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                traeger);
        }

        /// <summary>
        /// Die Kennlinienlage je Gerät und Vorlauf — EIN Hinweis (Kühlkonzept 8.5, 10.2), wie die
        /// Extrapolationsmeldung der Heizseite: Stunden unter der untersten Stützstelle (gekappt),
        /// Stunden über der obersten (verlängert oder gekappt), und eine Kennlinie mit nur einer
        /// Stützstelle.
        /// </summary>
        private void KennlinienlageMelden(Kaeltekaskade kaskade)
        {
            foreach (Kaelteerzeuger e in kaskade.Erzeuger)
            {
                if (e.Maschine != null) { KaeltemaschineMelden(e); continue; }
                Kuehlkennlinie k = e.Kennlinie;
                if (k == null) continue;
                string schluessel = "kuehl-wp-kennlinie-" + e.IdWp + "-" + k.Vorlauf.ToString(CultureInfo.InvariantCulture);

                if (e.StundenEinzelpunkt > 0)
                    Protokoll.HinweisEinmal(schluessel,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_EINZELPUNKT,
                                      e.Bezeichner, k.Vorlauf, k.TemperaturMin.ToString("F1", CultureInfo.CurrentCulture)));
                else if (e.StundenUnterKennlinie > 0 || e.StundenUeberKennlinie > 0)
                    Protokoll.HinweisEinmal(schluessel,
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_KENNLINIE_AUSSERHALB,
                                      e.Bezeichner, k.Vorlauf,
                                      e.StundenUnterKennlinie, k.TemperaturMin.ToString("F1", CultureInfo.CurrentCulture),
                                      e.StundenUeberKennlinie, k.TemperaturMax.ToString("F1", CultureInfo.CurrentCulture),
                                      e.Verlaengert ? MyResource.Resource.SIMENG_KAELTE_KENNLINIE_VERLAENGERT
                                                    : MyResource.Resource.SIMENG_KAELTE_KENNLINIE_GEKAPPT));
            }
        }

        /// <summary>
        /// Die Meldungen einer Kältemaschine (KU3-2): Betrieb mit freier Kühlung und Takt, die Stunden
        /// am Rand der Kennlinie und die Unterdeckung an der Leistungsgrenze — je einmal.
        /// </summary>
        private void KaeltemaschineMelden(Kaelteerzeuger e)
        {
            Kaeltemaschine k = e.Maschine;
            string kw = k.Kaltwassertemperatur.ToString("F1", CultureInfo.CurrentCulture);
            Protokoll.HinweisEinmal("kuehl-km-betrieb-" + e.AnlagenID,
                string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_BETRIEB,
                    e.Bezeichner, kw,
                    (e.KaelteGesamtKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                    (e.KaelteFreiKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                    e.StundenFreieKuehlung,
                    (e.StromGesamtKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                    e.Taktstunden));
            if (e.StundenRandwert > 0)
                Protokoll.HinweisEinmal("kuehl-km-randwert-" + e.AnlagenID,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_RANDWERT,
                        e.Bezeichner, e.StundenRandwert,
                        k.Kennlinie.RueckkuehlMin.ToString("F1", CultureInfo.CurrentCulture),
                        k.Kennlinie.RueckkuehlMax.ToString("F1", CultureInfo.CurrentCulture), kw));
            // KM3: die Stunden mit Gütegrad-Extrapolation - nur mit Kennfeld_Randweg GUETEGRAD.
            if (k.GuetegradWirksam && e.StundenExtrapoliert > 0)
                Protokoll.HinweisEinmal("kuehl-km-extrapoliert-" + e.AnlagenID,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_EXTRAPOLIERT,
                        e.Bezeichner, e.StundenExtrapoliert));
            if (e.StundenLeistungsgrenze > 0)
                Protokoll.WarnungEinmal("kuehl-km-unterdeckung-" + e.AnlagenID,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_KM_UNTERDECKUNG,
                        e.Bezeichner, e.StundenLeistungsgrenze,
                        (e.OffenAnLeistungsgrenzeKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture)));
        }

        // =====================================================================
        //  3. Nach der Wärmekaskade: Meldungen und Deckungsprobe Kälte
        // =====================================================================

        /// <summary>
        /// Schließt die Kälteseite des Erzeugerlaufs ab: die Meldungen zu Deckung und Unterdeckung
        /// (F-K12; 5.5: mit Menge UND Grund), die Auskunft über den gesperrten Heizkanal an Kühltagen
        /// (5.2) und die <b>Deckungsprobe Kälte</b> (4.3 #31) — sie setzt den Lauf bei einer
        /// Verletzung auf fehlgeschlagen.
        /// </summary>
        /// <param name="kanaele">Der Kanalsatz nach der ganzen Wärmekaskade (die Restbedarfe).</param>
        private void KaelteseiteAbschliessen(Kanalsatz kanaele)
        {
            // Ein abgebrochener Erzeugerlauf speichert ohnehin kein Ergebnis - keine Kältemeldung dazu.
            if (m_bError) return;

            SimulationKaeltebedarf kaelte = simulation_Waermebedarf != null ? simulation_Waermebedarf.Kaelteseite : null;
            if (kaelte == null || !kaelte.Gerechnet) return;

            Kaeltekaskade k = kaelte.Kaskade;
            if (k == null)
            {
                // Angelegt, aber keiner rechnet (gesperrt, ohne Kaskadenplatz): die Warnung des
                // Bedarfslaufs, jetzt an ihrem Ort - die Sperrgründe stehen schon im Protokoll.
                if (kaelte.KaelteerzeugerAngelegt > 0) kaelte.OhneErzeugerMelden();
            }
            else
            {
                Protokoll.Hinweis(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_DECKUNG,
                    (k.DeckungGesamtKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                    kaelte.Kaeltebedarf_Gesamt.ToString("N2", CultureInfo.CurrentCulture),
                    SimulationRunner.DeckungProzent(k.DeckungGesamtKwh / 1000.0, kaelte.Kaeltebedarf_Gesamt)
                                    .ToString("N1", CultureInfo.CurrentCulture),
                    (k.StromGesamtKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                    k.EerJahreswert.ToString("N2", CultureInfo.CurrentCulture),
                    k.AnzahlKuehltage));

                if (k.RestGesamtKwh > 0)
                    Protokoll.Warnung(string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_UNTERDECKUNG,
                        (k.RestGesamtKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                        (k.RestAnHeiztagenKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                        (k.RestAnKuehltagenKwh / 1000.0).ToString("N2", CultureInfo.CurrentCulture)));

                HeizkanalAnKuehltagenMelden(k, kanaele);
            }

            DeckungsprobeKaelteAusfuehren(kaelte, kanaele);
        }

        /// <summary>
        /// 5.2, „Kühltag mit Heizbedarf — Heizung ungedeckt, benannt": Wie viel Heizbedarf lag auf
        /// den Kühltagen, und wie viel davon blieb nach ALLEN Erzeugern offen?
        /// </summary>
        private void HeizkanalAnKuehltagenMelden(Kaeltekaskade k, Kanalsatz kanaele)
        {
            if (k.AnzahlKuehltage <= 0 || kanaele == null) return;

            double[] heizbedarf = simulation_Waermebedarf.KanaeleDrei().Heizung;
            double[] heizrest = kanaele.Heizung;
            double bedarf = 0, offen = 0;
            for (int h = 0; h < Kanalsatz.STUNDEN_JAHR; h++)
            {
                if (!k.Kuehltage[h / 24]) continue;
                bedarf += heizbedarf[h];
                offen += heizrest[h];
            }
            if (!(bedarf > 0)) return;

            string text = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_HEIZKANAL_GESPERRT,
                k.AnzahlKuehltage,
                (bedarf / 1000.0).ToString("N2", CultureInfo.CurrentCulture),
                (offen / 1000.0).ToString("N2", CultureInfo.CurrentCulture));
            if (offen > 0) Protokoll.Warnung(text);
            else Protokoll.Hinweis(text);
        }

        /// <summary>
        /// Die Deckungsprobe Kälte im Lauf (4.3 #31) — Aussage und Schärfe bei
        /// <see cref="Kaeltekaskade.Deckungsprobe"/>. Gesammelt werden die Buchungen JEDES
        /// Wärmeerzeugers im Kühlkanal, als Jahressumme und als Ganglinie, dazu die
        /// Pufferentladungen.
        /// </summary>
        private void DeckungsprobeKaelteAusfuehren(SimulationKaeltebedarf kaelte, Kanalsatz kanaele)
        {
            DeckungsprobeKaelte p = Kaeltekaskade.Deckungsprobe(kaelte.Kaskade, WaermebuchungenImKuehlkanal(),
                                                                kaelte.Kaeltebedarf,
                                                                kanaele != null ? kanaele.Kuehlung : null,
                                                                _waermekanalAbweichungen);
            if (p.Ok) return;

            string text = string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_DECKUNGSPROBE,
                p.WaermeImKuehlkanal, p.KuehlkanalVeraendert, p.WaermekanalVeraendert, p.BilanzVerletzt,
                p.MaxAbweichung.ToString("G4", CultureInfo.CurrentCulture));
            Protokoll.Fehlermeldung(text);
            FehlertextAufnehmen(text);
            m_bError = true;
        }

        /// <summary>
        /// Die Buchungen JEDES Wärmeerzeugers im Kühlkanal [kWh] — je Größe die Jahressumme und die
        /// Summe ihrer Ganglinie, dazu die Pufferentladungen. Erwartungswert: lauter Nullen.
        /// </summary>
        private List<double> WaermebuchungenImKuehlkanal()
        {
            var buchungen = new List<double>();
            int kk = Kanal.KUEHLUNG;
            Action<double[], Kanalganglinie> nehmen = delegate (double[] jahr, Kanalganglinie g)
            {
                if (jahr != null && kk < jahr.Length) buchungen.Add(jahr[kk]);
                if (g != null) buchungen.Add(g.Jahressumme(kk));
            };

            if (simulation_wp != null)
            {
                nehmen(simulation_wp.Direktdeckung_Kanal, simulation_wp.Direktdeckung_KanalStuendlich);
                nehmen(simulation_wp.Speicherentladung_Kanal, simulation_wp.Speicherentladung_KanalStuendlich);
                nehmen(simulation_wp.Heizstab_Kanal, simulation_wp.Heizstab_KanalStuendlich);
            }
            if (simulation_spk != null)
            {
                nehmen(simulation_spk.Direktdeckung_Kanal, simulation_spk.Direktdeckung_KanalStuendlich);
                nehmen(simulation_spk.Speicherentladung_Kanal, simulation_spk.Speicherentladung_KanalStuendlich);
            }
            if (simulation_solarthermie != null)
            {
                nehmen(simulation_solarthermie.Direktdeckung_Kanal, simulation_solarthermie.Direktdeckung_KanalStuendlich);
                nehmen(simulation_solarthermie.Speicherentladung_Kanal, simulation_solarthermie.Speicherentladung_KanalStuendlich);
            }
            if (simulation_bhkw != null)
            {
                nehmen(simulation_bhkw.Direktdeckung_Kanal, simulation_bhkw.Direktdeckung_KanalStuendlich);
                nehmen(simulation_bhkw.Speicherentladung_Kanal, simulation_bhkw.Speicherentladung_KanalStuendlich);
            }
            foreach (SimulationPufferspeicher sp in AlleSpeicher())
                if (sp != null) nehmen(sp.Entladung_Kanal, sp.Entladung_KanalStuendlich);

            return buchungen;
        }

#if DEBUG
        /// <summary>
        /// Die Zeile der Deckungsprobe Kälte für <see cref="KanalganglinienProbe"/> — die Kälteregel
        /// tritt neben die Ganglinienprobe des Bestands (Kühlkonzept 4.3 #31). Ohne erhobene Kälte leer.
        /// </summary>
        private string DeckungsprobeKaelteZeile()
        {
            SimulationKaeltebedarf kaelte = simulation_Waermebedarf != null ? simulation_Waermebedarf.Kaelteseite : null;
            if (kaelte == null || !kaelte.Gerechnet) return "";

            DeckungsprobeKaelte p = Kaeltekaskade.Deckungsprobe(kaelte.Kaskade, WaermebuchungenImKuehlkanal(),
                                                                kaelte.Kaeltebedarf, null, _waermekanalAbweichungen);
            return string.Format(CultureInfo.InvariantCulture,
                "Deckungsprobe Kaelte (Kuehlkonzept 4.3 #31): Waerme im Kuehlkanal {0}, Waermekanal veraendert {1}, " +
                "Bilanz verletzt {2} -> {3}",
                p.WaermeImKuehlkanal, p.WaermekanalVeraendert, p.BilanzVerletzt, p.Ok ? "OK" : "FEHLER");
        }
#endif
    }
}

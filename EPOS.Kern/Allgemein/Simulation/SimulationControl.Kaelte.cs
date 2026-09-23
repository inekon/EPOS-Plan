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
    /// <para><b>Übergang bis zur dritten Welle (E34, benannt):</b> Der Kältestrom geht in die
    /// Stufenrechnung wie der Wärmepumpenstrom und trägt Tarif und Faktor des Projekts; ein
    /// gewählter Kühlträger (<c>Kuehl_ID_Carrier</c>) steht als Hinweis im Protokoll.</para>
    /// </summary>
    partial class SimulationControl
    {
        /// <summary>Die Kälteerzeuger dieses Laufs in Kaskadenreihenfolge; <c>null</c> = keiner vorbereitet.</summary>
        private List<Kaelteerzeuger> _kaelteerzeuger;

        /// <summary>Die Tagesbetriebsart dieses Laufs; <c>null</c> ohne Kälteerzeuger.</summary>
        private bool[] _kuehltage;

        /// <summary>Stunden, in denen ein Wärmekanal sich während der Kältekaskade verändert hat — für die Deckungsprobe.</summary>
        private int _waermekanalAbweichungen;

        /// <summary>Den Kältezustand des Vorlaufs verwerfen — am Beginn von <see cref="Kaskade_Zweikanalig"/>.</summary>
        private void KaelteseiteZuruecksetzen()
        {
            _kaelteerzeuger = null;
            _kuehltage = null;
            _waermekanalAbweichungen = 0;
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
            simulation_wp.KuehlbetriebSetzen(null, null);

            SimulationKaeltebedarf kaelte = simulation_Waermebedarf != null ? simulation_Waermebedarf.Kaelteseite : null;
            bool erhoben = kaelte != null && kaelte.Gerechnet;

            for (int i = 0; i < simulation_wp.wp_model.Count && i < SimulationWaermepumpe.MAX_WP; i++)
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
                Kuehlkennlinie k = KenndatenKuehlungCtrl.KennlinieProjekt(m.ID_WP, kuehlVorlauf);
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

                // K21: keine Stützstelle - die nächste, einmal je Gerät und Vorlauf benannt.
                if (k.VorlaufAusgewichen)
                    Protokoll.HinweisEinmal("kuehl-wp-vorlauf-" + m.ID_WP + "-" + k.VorlaufGewuenscht.Value.ToString(CultureInfo.InvariantCulture),
                        string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_VORLAUF_AUSGEWICHEN,
                                      name, k.VorlaufGewuenscht.Value,
                                      string.Join(", ", k.Stuetzstellen.Select(v => v.ToString(CultureInfo.CurrentCulture))),
                                      k.Vorlauf));

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

                // E34, Übergang bis Welle 3: ein gewählter Kühlträger wirkt noch nicht - benannt.
                if (m.Kuehl_ID_Carrier.HasValue && m.Kuehl_ID_Carrier.Value > 0)
                {
                    int heiztraeger = m.ID_Carrier > 0 ? m.ID_Carrier : Emissionsquelle.StromTraeger(m_ID_Projekt);
                    if (m.Kuehl_ID_Carrier.Value != heiztraeger)
                        Protokoll.HinweisEinmal("kuehl-wp-kuehltraeger-" + m.ID,
                            string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_KAELTE_WP_KUEHLTRAEGER, name));
                }

                _kaelteerzeuger.Add(new Kaelteerzeuger
                {
                    AnlagenID = m.ID,
                    IdWp = m.ID_WP,
                    Bezeichner = name,
                    Modulindex = i,
                    Kennlinie = k,
                    Hilfsstromanteil = hilfsstromanteil,
                    Quelltemperatur = i < simulation_wp.Quelltemperaturen.Count ? simulation_wp.Quelltemperaturen[i] : null
                });
            }

            if (_kaelteerzeuger.Count == 0) return;

            // K8a (5.2): die Tagesbetriebsart aus den Tagessummen des PROJEKTbedarfs - Heizkanal
            // gegen Kühlkanal, vor jedem Erzeuger.
            Kanalsatz bedarf = simulation_Waermebedarf.KanaeleDrei();
            _kuehltage = Kaeltekaskade.TagesbetriebsartBestimmen(bedarf.Heizung, bedarf.Kuehlung);

            bool[] module = new bool[simulation_wp.wp_model.Count];
            foreach (Kaelteerzeuger e in _kaelteerzeuger) module[e.Modulindex] = true;
            simulation_wp.KuehlbetriebSetzen(module, _kuehltage);
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
            if (m_bError || _kaelteerzeuger == null || _kaelteerzeuger.Count == 0) return;
            SimulationKaeltebedarf kaelte = simulation_Waermebedarf != null ? simulation_Waermebedarf.Kaelteseite : null;
            if (kaelte == null || !kaelte.Gerechnet) return;

            foreach (Kaelteerzeuger e in _kaelteerzeuger)
            {
                WErzeugerModel m = simulation_wp.wp_model[e.Modulindex];
                double[] heiz = simulation_wp.Heizzeitanteil_stuendlich != null &&
                                e.Modulindex < simulation_wp.Heizzeitanteil_stuendlich.Length
                    ? simulation_wp.Heizzeitanteil_stuendlich[e.Modulindex] : null;
                e.Zeitanteil = Kaeltekaskade.ZeitanteilBilden(_kuehltage, heiz, m.Sperrung, m.Sperrzeit_von, m.Sperrzeit_bis);
            }

            // Deckungsprobe, dritte Aussage: die Wärmekanäle vor der Kältekaskade festhalten.
            double[][] vorher = null;
            if (kanaele != null)
            {
                vorher = new double[Kanal.ANZAHL][];
                foreach (int k in Kanal.KANAELE_WAERME) vorher[k] = (double[])kanaele.Bedarf[k].Clone();
            }

            var kaskade = new Kaeltekaskade { Erzeuger = _kaelteerzeuger, Kuehltage = _kuehltage };
            kaskade.Rechnen(kaelte.Kaeltebedarf, simulation_wp.Extrapolation_Erlaubt);
            kaelte.DeckungUebernehmen(kaskade);

            _waermekanalAbweichungen = 0;
            if (vorher != null)
                for (int h = 0; h < Kanalsatz.STUNDEN_JAHR; h++)
                    foreach (int k in Kanal.KANAELE_WAERME)
                        if (kanaele.Bedarf[k][h] != vorher[k][h]) { _waermekanalAbweichungen++; break; }

            // 6.1: der Kältestrom als eigene Reihe in die Stufenrechnung - addiert im Rest, nicht in
            // der Reihe der Wärmepumpe (5.1, Festlegung 5).
            ReststromMwh += kaskade.StromGesamtKwh / 1000.0;
            double[] temp = Stundenwerte_zu_viertelstunden(kaskade.Stromverbrauch_Kuehlung_stuendlich);
            Rest_Strombedarf_viertelstuendlich = AddVectors(Rest_Strombedarf_viertelstuendlich, temp);

            KennlinienlageMelden(kaskade);
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

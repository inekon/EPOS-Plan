using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // ETAPPE E9b — DER AUSWEIS „n VON m PARAMETERN SZENARIERT" (Konzept § 2.11.5,
    // Regel „Pflege"; § 2.11.7: der Hinweistext entfällt mit der Etappe, die ihn
    // überflüssig macht). Entscheide E9b‑Q2 (Zählregel, Lesart a) und E9b‑Q3 (Ort:
    // unter der Annahmentafel, an der Stelle des Hinweistexts, Lesart a).
    //
    // ALLES IN DIESER DATEI IST AUSGABE. Keine Zahl der Rechnung ändert sich; gezählt
    // wird, was gepflegt ist — nach derselben Regel, nach der der Kern es liest.
    // ---------------------------------------------------------------------------

    /// <summary>
    /// ETAPPE E9b — ein Energieträger MIT VERBRAUCH in einem Stand, wie ihn die Zählung
    /// braucht: die drei wirksamen Erwartet-Preise und die Preise je Szenario.
    /// </summary>
    public sealed class SzenarioAbdeckungTraeger
    {
        /// <summary>Anzeigename („Erdgas E"); bei mehreren Ständen mit dem Stand dahinter.</summary>
        public string Name = "";

        /// <summary>Ist es ein STROMträger? Dann zählt der Leistungspreis immer mit.</summary>
        public bool IstStrom;

        /// <summary>Wirksamer Erwartet-Arbeitspreis (Rückfallkette); <c>null</c> = keiner.</summary>
        public double? Arbeitspreis;

        /// <summary>Wirksamer Erwartet-Grundpreis [€/a]; <c>null</c> = keiner.</summary>
        public double? Grundpreis;

        /// <summary>Wirksamer Erwartet-Leistungspreis; <c>null</c> = keiner.</summary>
        public double? Leistungspreis;

        /// <summary>Die Preise je Szenario (Schemaschritt 117); nie <c>null</c>.</summary>
        public TraegerpreisSzenario Szenario = new TraegerpreisSzenario();
    }

    /// <summary>
    /// ETAPPE E9b — eine Vergütungszeile der Photovoltaik, die für einen Stand MIT
    /// PV-Anlage gilt (Konzept § 2.16: eigene Werte oder vom Stamm übernommen — die
    /// übernommene zählt einmal, beim Stamm).
    /// </summary>
    public sealed class SzenarioAbdeckungPv
    {
        /// <summary>Der Stand, bei mehreren Zeilen; leer = kein Zusatz.</summary>
        public string Name = "";

        /// <summary>Das wirksame Vergütungsmodell; <c>null</c> = keine Zeile (Flat-Pfad).</summary>
        public ProjektPhotovoltaikModel Modell;
    }

    /// <summary>
    /// ETAPPE E9b (Konzept § 2.11.5, „Pflege") — <b>wie viele Parameter die Szenarien
    /// tragen</b>: der Ausweis „n von m Parametern szenariert" samt der gepflegten
    /// Größen, unter der Annahmentafel der Seite, im Wort- und im Excelbericht und in
    /// Punkt 9 der Anhang-E-Checkliste — an der Stelle des Hinweistexts (§ 2.11.7), den
    /// die Pflege in den Dialogen überflüssig macht.
    ///
    /// <para><b>Die Zählregel</b> (E9b‑Q2, Lesart a) — <c>m</c>, die Parameter:</para>
    /// <list type="bullet">
    ///   <item><description>die SIEBEN Größen des W5‑B‑9-Satzes (Zins, Preissteigerung
    ///     Energie, Betrieb, Investition, Investitionsänderung, Ertragsänderung,
    ///     Nutzungsdaueränderung);</description></item>
    ///   <item><description>Betrachtungszeitraum, Mengenänderung, Einspeisevergütung PV und
    ///     Einspeisevergütung KWK;</description></item>
    ///   <item><description>DV-Entgelt und PPA-Preis je Vergütungszeile, die für einen
    ///     Stand MIT PV-Anlage gilt;</description></item>
    ///   <item><description>je Träger MIT VERBRAUCH und Stand der Arbeits- und der
    ///     Grundpreis, der Leistungspreis nur beim Stromträger oder dort, wo ein
    ///     Leistungspreis (Erwartet oder je Szenario) gepflegt ist.</description></item>
    /// </list>
    /// <para><c>n</c> — die SZENARIERTEN: Ein Parameter zählt, wenn sein Best- oder sein
    /// Worst-Wert gepflegt ist und sich um mehr als 1e−9 vom Erwartet-Wert unterscheidet
    /// (VALERI-Vorrang, <see cref="SzenarioSatz.GEPFLEGT_SCHWELLE"/>). Für die sieben
    /// Größen des W5‑B‑9-Satzes heißt „gepflegt" ein eingetragenes Feld — die Vorgabe
    /// (leeres Feld) ist keine Pflege, und eine gepflegte 0 zählt dort, wenn sie vom
    /// Erwartet-Wert abweicht (0 %/a Preissteigerung ist eine Aussage). Für die neuen
    /// Größen gilt die Nullregel des Kerns: leer oder 0 heißt „wie Erwartet"
    /// (<see cref="SzenarioSatz.Gepflegt"/>, <see cref="TraegerpreisSzenario.Wirksam"/>).
    /// Ein gezählter Wert trägt damit immer eine Abweichung — eine Pflege, die gleich dem
    /// Erwartet-Wert ist, rechnet nicht anders und zählt nicht.</para>
    /// </summary>
    public sealed class SzenarioAbdeckung
    {
        private readonly List<string> _gepflegte = new List<string>();

        /// <summary><c>n</c> — die Parameter mit gepflegter Abweichung.</summary>
        public int Szenariert { get; private set; }

        /// <summary><c>m</c> — alle Parameter nach der Zählregel.</summary>
        public int Parameter { get; private set; }

        /// <summary>Die Namen der szenarierten Parameter, in der Reihenfolge der Zählung.</summary>
        public IReadOnlyList<string> Gepflegte { get { return _gepflegte; } }

        /// <summary>Keine Zählung — ohne Parametersatz gibt es keinen Ausweis.</summary>
        public bool Leer { get { return Parameter == 0; } }

        /// <summary>
        /// Der Satz des Ausweises: „3 von 16 Parametern szenariert: Betrachtungszeitraum,
        /// Arbeitspreis Erdgas E, …" — ohne gepflegten Parameter nur die Zählung; ohne
        /// Zählung leer.
        /// </summary>
        public string Satz(CultureInfo kultur)
        {
            if (Leer) return "";
            if (kultur == null) kultur = CultureInfo.CurrentCulture;
            return _gepflegte.Count == 0
                ? string.Format(kultur, MyResource.Resource.WIRT_SZ_ABDECKUNG, Szenariert, Parameter)
                : string.Format(kultur, MyResource.Resource.WIRT_SZ_ABDECKUNG_LISTE, Szenariert, Parameter,
                                string.Join(", ", _gepflegte.ToArray()));
        }

        /// <summary>
        /// DIE ZÄHLUNG — ohne Datenbank. Der Aufrufer legt die Träger mit Verbrauch und die
        /// Vergütungszeilen der PV-Stände vor (<see cref="Lesen"/> tut es aus der
        /// Datenbank); <paramref name="p"/> trägt die projektweiten Größen.
        /// </summary>
        /// <param name="p">Der Parametersatz der Gruppe; <c>null</c> = kein Ausweis.</param>
        /// <param name="traeger">Die Träger mit Verbrauch je Stand; <c>null</c> = keine.</param>
        /// <param name="pv">Die Vergütungszeilen der Stände mit PV-Anlage; <c>null</c> = keine.</param>
        public static SzenarioAbdeckung Zaehle(WirtschaftlichkeitParameter p,
                                              IEnumerable<SzenarioAbdeckungTraeger> traeger,
                                              IEnumerable<SzenarioAbdeckungPv> pv)
        {
            var a = new SzenarioAbdeckung();
            if (p == null) return a;

            SzenarioSatz b = p.SatzFuer(WirtschaftlichkeitSzenario.BEST)
                             ?? SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.BEST);
            SzenarioSatz w = p.SatzFuer(WirtschaftlichkeitSzenario.WORST)
                             ?? SzenarioSatz.Vorgabe(WirtschaftlichkeitSzenario.WORST);

            // ---- die sieben Größen des W5‑B‑9-Satzes: gepflegt = eingetragen und abweichend
            a.Zaehlen(MyResource.Resource.WPAR_SZ_ZINS,
                      W5(b.Zinssatz, p.Zinssatz) || W5(w.Zinssatz, p.Zinssatz));
            a.Zaehlen(MyResource.Resource.WPAR_SZ_PREIS_E,
                      W5(b.PreissteigerungEnergie, p.PreissteigerungEnergie) ||
                      W5(w.PreissteigerungEnergie, p.PreissteigerungEnergie));
            a.Zaehlen(MyResource.Resource.WPAR_SZ_PREIS_B,
                      W5(b.PreissteigerungBetrieb, p.PreissteigerungBetrieb) ||
                      W5(w.PreissteigerungBetrieb, p.PreissteigerungBetrieb));
            a.Zaehlen(MyResource.Resource.WPAR_SZ_PREIS_I,
                      W5(b.PreissteigerungInvestition, p.PreisInvestWirksam) ||
                      W5(w.PreissteigerungInvestition, p.PreisInvestWirksam));
            a.Zaehlen(MyResource.Resource.WPAR_SZ_INVEST,
                      W5(b.InvestitionAenderung, 0.0) || W5(w.InvestitionAenderung, 0.0));
            a.Zaehlen(MyResource.Resource.WPAR_SZ_ERTRAG,
                      W5(b.ErtragAenderung, 0.0) || W5(w.ErtragAenderung, 0.0));
            a.Zaehlen(MyResource.Resource.WPAR_SZ_DAUER,
                      W5(b.NutzungsdauerAenderung, 0.0) || W5(w.NutzungsdauerAenderung, 0.0));

            // ---- Rahmen, Mengen und die zwei Einspeisevergütungen (E9a: ohne Vorgabe)
            int t = p.Betrachtungszeitraum;
            a.Zaehlen(MyResource.Resource.WIRT_ANN_ZEITRAUM, b.ZeitraumGepflegt(t) || w.ZeitraumGepflegt(t));
            a.Zaehlen(MyResource.Resource.WIRT_ANN_MENGE, b.MengeGepflegt || w.MengeGepflegt);
            a.Zaehlen(MyResource.Resource.WIRT_ANN_VERGUETUNG,
                      SzenarioSatz.Gepflegt(b.Einspeiseverguetung, p.Einspeiseverguetung) ||
                      SzenarioSatz.Gepflegt(w.Einspeiseverguetung, p.Einspeiseverguetung));
            double kwk = p.EinspeiseverguetungKWK ?? 0.0;
            a.Zaehlen(MyResource.Resource.WIRT_ANN_VERGUETUNG_KWK,
                      SzenarioSatz.Gepflegt(b.EinspeiseverguetungKwk, kwk) ||
                      SzenarioSatz.Gepflegt(w.EinspeiseverguetungKwk, kwk));

            // ---- DV-Entgelt und PPA-Preis je Vergütungszeile eines PV-Standes
            if (pv != null)
                foreach (SzenarioAbdeckungPv z in pv)
                {
                    if (z == null) continue;
                    ProjektPhotovoltaikModel m = z.Modell;
                    string zusatz = string.IsNullOrEmpty(z.Name) ? "" : " (" + z.Name + ")";
                    a.Zaehlen(MyResource.Resource.WIRT_ANN_DV_ENTGELT + zusatz,
                              m != null && (SzenarioSatz.Gepflegt(m.DvEntgeltBest, m.DvEntgelt ?? 0.0) ||
                                            SzenarioSatz.Gepflegt(m.DvEntgeltWorst, m.DvEntgelt ?? 0.0)));
                    a.Zaehlen(MyResource.Resource.WIRT_ANN_PPA_PREIS + zusatz,
                              m != null && (SzenarioSatz.Gepflegt(m.PpaPreisBest, m.PpaPreis ?? 0.0) ||
                                            SzenarioSatz.Gepflegt(m.PpaPreisWorst, m.PpaPreis ?? 0.0)));
                }

            // ---- die Trägerpreise je Träger mit Verbrauch
            if (traeger != null)
                foreach (SzenarioAbdeckungTraeger tr in traeger)
                {
                    if (tr == null) continue;
                    TraegerpreisSzenario sz = tr.Szenario ?? new TraegerpreisSzenario();
                    a.Zaehlen(MyResource.Resource.WIRT_SZ_TP_ARBEIT + " " + tr.Name,
                              Paar(tr.Arbeitspreis, sz.ArbeitspreisBest, sz.ArbeitspreisWorst));
                    a.Zaehlen(MyResource.Resource.WIRT_SZ_TP_GRUND + " " + tr.Name,
                              Paar(tr.Grundpreis, sz.GrundpreisBest, sz.GrundpreisWorst));

                    // Der Leistungspreis zählt beim Stromträger immer, sonst nur, wo ein
                    // Leistungspreis gepflegt ist — Erwartet oder je Szenario. So bleibt
                    // n ≤ m: Ein gepflegter Szenario-Leistungspreis bringt seinen
                    // Parameter mit.
                    bool leistungSzenario = (sz.LeistungspreisBest ?? 0) != 0 || (sz.LeistungspreisWorst ?? 0) != 0;
                    if (tr.IstStrom || (tr.Leistungspreis ?? 0) != 0 || leistungSzenario)
                        a.Zaehlen(MyResource.Resource.WIRT_SZ_TP_LEISTUNG + " " + tr.Name,
                                  Paar(tr.Leistungspreis, sz.LeistungspreisBest, sz.LeistungspreisWorst));
                }

            return a;
        }

        /// <summary>
        /// Die Zählung einer Vergleichsgruppe aus der DATENBANK: je Stand die Träger mit
        /// Verbrauch (die Verwendungsliste der Kostenseite, dazu der Stromträger, wenn der
        /// Stand Strom bezieht) mit ihren wirksamen Erwartet-Preisen und den Preisen je
        /// Szenario, und die Vergütungszeilen der Stände mit PV-Anlage — eine übernommene
        /// Zeile zählt einmal, bei dem Stand, dem sie gehört. Ein Lesefehler kostet den
        /// betroffenen Teil, nie den Ausweis.
        /// </summary>
        /// <param name="p">Der Parametersatz der Gruppe; <c>null</c> = kein Ausweis.</param>
        /// <param name="staende">Die Stände der Gruppe (Id, Anzeigename) in Listenreihenfolge.</param>
        public static SzenarioAbdeckung Lesen(WirtschaftlichkeitParameter p,
                                             IEnumerable<KeyValuePair<int, string>> staende)
        {
            if (p == null) return new SzenarioAbdeckung();

            var liste = new List<KeyValuePair<int, string>>();
            var gesehen = new HashSet<int>();
            if (staende != null)
                foreach (KeyValuePair<int, string> s in staende)
                    if (s.Key > 0 && gesehen.Add(s.Key)) liste.Add(s);
            bool mehrere = liste.Count > 1;

            // GRUPPENREGEL „Strombedarf ohne Verwendung": Verwendet ein Stand der Gruppe
            // Strom, bepreist im Vergleich JEDER Stand seinen Netzbezug — dann zählt auch der
            // Stromträger eines Standes ohne eigene Stromverwendung mit (dieselbe Regel wie
            // WirtschaftlichkeitCtrl.StromGruppenregel).
            bool gruppeStrom = false;
            if (mehrere)
            {
                var ids = new List<int>();
                foreach (KeyValuePair<int, string> st in liste) ids.Add(st.Key);
                List<int> verwender;
                try { gruppeStrom = ProjektEnergietraegerCtrl.GruppeVerwendetStrom(ids, out verwender); }
                catch { gruppeStrom = false; }
            }

            var traeger = new List<SzenarioAbdeckungTraeger>();
            var pv = new List<SzenarioAbdeckungPv>();
            var pvZeilen = new HashSet<int>();
            var pvCtrl = new ProjektPhotovoltaikCtrl();

            foreach (KeyValuePair<int, string> s in liste)
            {
                int id = s.Key;
                string stand = string.IsNullOrWhiteSpace(s.Value) ? id.ToString(CultureInfo.InvariantCulture) : s.Value;

                foreach (KeyValuePair<int, string> c in TraegerMitVerbrauch(id, gruppeStrom))
                {
                    double? arbeit = null, grund = null, leistung = null;
                    try { KostenEmissionRechner.PreisSatz(id, c.Key, null, out arbeit, out grund, out leistung); }
                    catch { }
                    TraegerpreisSzenario sz;
                    try { sz = EnergietraegerPreisCtrl.SzenarioLesen(id, c.Key); }
                    catch { sz = new TraegerpreisSzenario(); }

                    traeger.Add(new SzenarioAbdeckungTraeger
                    {
                        Name = c.Value + (mehrere ? " (" + stand + ")" : ""),
                        IstStrom = IstStromtraeger(id, c.Key),
                        Arbeitspreis = arbeit,
                        Grundpreis = grund,
                        Leistungspreis = leistung,
                        Szenario = sz
                    });
                }

                // DV-Entgelt und PPA-Preis nur bei PV-Anlage; die wirksame Zeile einmal.
                bool mitPv;
                try { mitPv = PhotovoltaikCtrl.KwpDesProjekts(id) > 0; }
                catch { mitPv = false; }
                if (!mitPv) continue;

                PvVerguetungStand v = null;
                try { v = pvCtrl.LiesAufgeloest(id); }
                catch { }
                int zeile = v != null && v.Modell != null && v.IdQuelle > 0 ? v.IdQuelle : id;
                if (!pvZeilen.Add(zeile)) continue;
                pv.Add(new SzenarioAbdeckungPv
                {
                    Name = mehrere ? stand : "",
                    Modell = v != null ? v.Modell : null
                });
            }

            return Zaehle(p, traeger, pv);
        }

        /// <summary>
        /// Die Träger MIT VERBRAUCH eines Standes (Id, Name): was seine Anlagen beziehen
        /// (<see cref="ProjektEnergietraegerCtrl.Verwendete"/>), dazu der Stromträger, wenn
        /// der Stand Strom bezieht (<see cref="ProjektEnergietraegerCtrl.BrauchtStromTraeger"/>
        /// — auch ein BHKW-Projekt mit Reststrom). Jeder Träger einmal, nach Id.
        ///
        /// <para><paramref name="gruppeStrom"/>: Die Gruppe verwendet Strom (Gruppenregel) —
        /// dann gehört der Stromträger auch zu einem Stand ohne eigene Stromverwendung,
        /// ohne Zuordnung der Auslieferungsträger, mit dem ihn der Vergleich bepreist.</para>
        /// </summary>
        private static List<KeyValuePair<int, string>> TraegerMitVerbrauch(int idProjekt, bool gruppeStrom)
        {
            var liste = new List<KeyValuePair<int, string>>();
            var ids = new HashSet<int>();
            try
            {
                foreach (ProjektEnergietraegerCtrl.Verwendung v in ProjektEnergietraegerCtrl.Verwendete(idProjekt))
                    if (v != null && v.CarrierId > 0 && ids.Add(v.CarrierId))
                        liste.Add(new KeyValuePair<int, string>(v.CarrierId, v.Name ?? ""));
            }
            catch { }

            try
            {
                bool eigen = ProjektEnergietraegerCtrl.BrauchtStromTraeger(idProjekt);
                if (eigen || gruppeStrom)
                {
                    int strom = Emissionsquelle.StromTraeger(idProjekt);
                    if (strom <= 0 && !eigen)
                        strom = ProjektEnergietraegerCtrl.StromTraegerImVergleich(idProjekt);
                    if (strom > 0 && ids.Add(strom))
                        liste.Add(new KeyValuePair<int, string>(strom, Emissionsquelle.TraegerName(strom)));
                }
            }
            catch { }

            liste.Sort(delegate (KeyValuePair<int, string> x, KeyValuePair<int, string> y)
                       { return x.Key.CompareTo(y.Key); });
            return liste;
        }

        /// <summary>Ist der Träger ein Stromträger dieses Standes — der des Projekts oder
        /// der eigene einer Anlage?</summary>
        private static bool IstStromtraeger(int idProjekt, int carrierId)
        {
            try
            {
                if (Emissionsquelle.StromTraeger(idProjekt) == carrierId) return true;
                foreach (int c in ProjektEnergietraegerCtrl.EigeneStromTraeger(idProjekt).Values)
                    if (c == carrierId) return true;
            }
            catch { }
            return false;
        }

        /// <summary>Ein W5‑B‑9-Feld ist szenariert, wenn es eingetragen ist und vom
        /// Erwartet-Wert um mehr als die Schwelle abweicht (die Vorgabe zählt nicht).</summary>
        private static bool W5(double? wert, double erwartet)
        {
            return wert.HasValue && Math.Abs(wert.Value - erwartet) > SzenarioSatz.GEPFLEGT_SCHWELLE;
        }

        /// <summary>Ein Trägerpreis-Paar ist szenariert, wenn Best oder Worst den Erwartet-Preis
        /// nach der EINEN Regel ersetzt (<see cref="TraegerpreisSzenario.Wirksam"/>).</summary>
        private static bool Paar(double? erwartet, double? best, double? worst)
        {
            bool b, w;
            TraegerpreisSzenario.Wirksam(erwartet, best, out b);
            TraegerpreisSzenario.Wirksam(erwartet, worst, out w);
            return b || w;
        }

        private void Zaehlen(string name, bool szenariert)
        {
            Parameter++;
            if (!szenariert) return;
            Szenariert++;
            _gepflegte.Add((name ?? "").Trim());
        }
    }
}

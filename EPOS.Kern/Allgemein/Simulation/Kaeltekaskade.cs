using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Kälteerzeuger der Kältekaskade</b> — in Stufe KU2 die reversible Wärmepumpe im
    /// Kühlbetrieb (Kühlkonzept 5.1, 5.5; E15, E33). Eingang und Ergebnis EINER Anlage; gerechnet
    /// wird in <see cref="Kaeltekaskade.Rechnen"/>.
    ///
    /// <para><b>Eigene Reihen, nicht die der Wärmepumpe</b> (Festlegung 5): Kälteerzeugung und
    /// Kältestrom stehen hier, nicht in <c>WP_Waermeproduktion_stuendlich</c> und
    /// <c>WP_Strombedarf_stuendlich</c> — sonst verschöbe der Kühlbetrieb Entzugsarbeit und
    /// Entzugsspitze der Sonde (<c>ErdreichAuswertung</c>) und die Jahresarbeitszahl der Wärme,
    /// ohne dass es jemand sähe.</para>
    /// </summary>
    public sealed class Kaelteerzeuger
    {
        /// <summary>Die Anlagenzeile (<c>Tab_Energieanlagen.ID</c>).</summary>
        public int AnlagenID;

        /// <summary>Das Projektgerät (<c>Tab_WP.ID</c>), dessen Kühlkennlinie gilt.</summary>
        public int IdWp;

        /// <summary>Bezeichner der Anlage — für Meldungen.</summary>
        public string Bezeichner = "";

        /// <summary>Platz der Anlage im Wärmepumpenmodul (<c>SimulationWaermepumpe.wp_list</c>); -1 = keiner.</summary>
        public int Modulindex = -1;

        /// <summary>Die Kühlkennlinie für den Kühl-Vorlauf der Anlage (K21); nur eine rechenbare.</summary>
        public Kuehlkennlinie Kennlinie;

        /// <summary>
        /// KK2 (Entwurf KK 2.4): die Kühlkennlinie des Geräts über ALLE Kühl-Vorläufe — ausgewertet nur am Stundenvorlauf
        /// (<see cref="Kaeltekaskade.StundeRechnen(int, double, double)"/>, <c>AbfragenAmVorlauf</c>); <c>null</c> = nur
        /// <see cref="Kennlinie"/>.
        /// </summary>
        public KuehlkennlinienSchar Schar;

        /// <summary>
        /// KK2 (Entwurf KK 2.3): der Vorlauf, den die Wärmepumpe am verlangten Vorlauf <paramref name="vorlaufC"/> fährt — nie
        /// kälter als die kleinste Stützstelle ihrer Kühlkennlinie; ohne <see cref="Schar"/> unverändert.
        /// </summary>
        public double VorlaufAmErzeuger(double vorlaufC)
        {
            double min = Schar != null ? Schar.VorlaufMinC : double.NaN;
            return !double.IsNaN(min) && vorlaufC < min ? min : vorlaufC;
        }

        /// <summary>
        /// Hilfsstromanteil des Kältekreises an der Verdichterarbeit [—] (<c>Kuehl_Hilfsstromanteil</c>,
        /// K23): 0 = kein Zuschlag — so wird NULL gelesen, damit keine geratene Zahl entsteht.
        /// </summary>
        public double Hilfsstromanteil;

        /// <summary>
        /// Außen- bzw. Quellentemperatur je Stunde [°C] — dieselbe Reihe, über der die Heizkennlinie
        /// gelesen wird: Die Rückkühlung der reversiblen Maschine ist ihre Quelle, rückwärts gelesen
        /// (Festlegung 5, K8/E33).
        /// </summary>
        public double[] Quelltemperatur;

        /// <summary>
        /// Der Anteil jeder Stunde, der dem Kühlbetrieb offensteht [0…1] — gebildet von
        /// <see cref="Kaeltekaskade.ZeitanteilBilden"/> aus Tagesbetriebsart, Brauchwasser und
        /// Sperrzeit (5.2).
        /// </summary>
        public double[] Zeitanteil;

        /// <summary>Senke der Kälteseite (Kühlkonzept 4.6): Erzeuger → Kältekreis → Kühlkanal.</summary>
        public Senke Ziel { get { return Senke.Kaeltekreis; } }

        /// <summary>
        /// <b>Die Kältemaschine dieses Erzeugers</b> (KU3-2) — <c>null</c> für eine Wärmepumpe im
        /// Kühlbetrieb. Ist sie gesetzt, rechnet die Stunde über
        /// <see cref="Kaeltemaschine.Stunde"/>: ohne <see cref="Kennlinie"/>, <see cref="Zeitanteil"/>
        /// und <see cref="Modulindex"/> (-1, kein Wärmepumpenmodul).
        /// </summary>
        public Kaeltemaschine Maschine;

        /// <summary>
        /// <b>Freie Kühlung über die Wärmequelle</b> (KU3-6, F2/F3) — nur für eine Wärmepumpe: Der Schalter
        /// <c>Kuehl_Frei</c> der Anlagenzeile ist gesetzt UND die Quelle trägt ihn (Sole-Wasser oder
        /// Wasser-Wasser mit <c>WQ_Typ</c> Erdreich, Konstant, Profil oder CSV). Sonst false — die Stunde
        /// rechnet Zeichen für Zeichen wie ohne Schalter.
        /// </summary>
        public bool FreieKuehlungSole;

        /// <summary>
        /// Grädigkeit des Wärmetauschers der freien Kühlung [K] (<c>Kuehl_Frei_Graedigkeit_K</c>; NULL =
        /// <see cref="KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K"/>).
        /// </summary>
        public double FreieKuehlungGraedigkeitK = KaelteFestwerte.FREIE_KUEHLUNG_SOLE_GRAEDIGKEIT_K;

        /// <summary>
        /// Leistungsgrenze der freien Kühlung [kW] (<c>Kuehl_Frei_Leistung_kW</c>); <c>null</c> = die
        /// Kälteleistung der Kühlkennlinie in der Stunde.
        /// </summary>
        public double? FreieKuehlungLeistungKw;

        /// <summary>
        /// Kaltwasser-Vorlauf [°C], gegen den die freie Kühlung der Wärmepumpe geprüft wird — der gepflegte
        /// <c>Tab_WP.Kuehl_Vorlauf</c>, ohne ihn der Vorlauf der Kennlinie.
        /// </summary>
        public double KuehlVorlaufC;

        /// <summary>Stunden in freier Kühlung (Kältemaschine über den Rückkühler, Wärmepumpe über die Quelle).</summary>
        public int StundenFreieKuehlung;

        /// <summary>Kälte aus freier Kühlung [kWh] (Kältemaschine über den Rückkühler, Wärmepumpe über die Quelle).</summary>
        public double KaelteFreiKwh;

        /// <summary>Stunden mit Kennlinie am Rand (nur Kältemaschine).</summary>
        public int StundenRandwert;

        /// <summary>Stunden unter der Mindestteillast — die Maschine taktet (nur Kältemaschine).</summary>
        public int StundenTakt;

        /// <summary>Stunden, in denen die Kennlinienleistung die Last nicht trug (nur Kältemaschine).</summary>
        public int StundenLeistungsgrenze;

        /// <summary>Last, die an der Leistungsgrenze offen blieb [kWh] (nur Kältemaschine).</summary>
        public double OffenAnLeistungsgrenzeKwh;

        // ---- Ergebnis ------------------------------------------------------------

        /// <summary>Gedeckte Kälte je Stunde [kWh].</summary>
        public double[] Kaelte_stuendlich = new double[Kaeltekaskade.STUNDEN];

        /// <summary>Kältestrom je Stunde [kWh] = Kälte / EER · (1 + Hilfsstromanteil) (6.1).</summary>
        public double[] Strom_stuendlich = new double[Kaeltekaskade.STUNDEN];

        /// <summary>
        /// Davon Mehrstrom aus Taktverlust je Stunde [kWh] (ohne Hilfsstromzuschlag) — die
        /// Rückspeisung ins Sondenfeld rechnet ohne ihn (Konzept Simulationsablauf 23.5).
        /// </summary>
        public double[] Taktstrom_stuendlich = new double[Kaeltekaskade.STUNDEN];

        /// <summary>Gedeckte Kälte im Jahr [kWh].</summary>
        public double KaelteGesamtKwh;

        /// <summary>Kältestrom im Jahr [kWh], einschließlich Hilfsstrom.</summary>
        public double StromGesamtKwh;

        /// <summary>davon Hilfsstrom [kWh].</summary>
        public double HilfsstromGesamtKwh;

        /// <summary>Stunden mit gedeckter Kälte.</summary>
        public int StundenMitKaelte;

        /// <summary>Stunden mit Kältebedarf und offenem Zeitanteil, deren Temperatur unter der untersten Stützstelle lag (gekappt).</summary>
        public int StundenUnterKennlinie;

        /// <summary>Dieselben Stunden über der obersten Stützstelle (verlängert bzw. gekappt).</summary>
        public int StundenUeberKennlinie;

        /// <summary>true, wenn eine Stunde über der obersten Stützstelle linear verlängert wurde.</summary>
        public bool Verlaengert;

        /// <summary>Stunden, in denen eine Kennlinie mit einer einzigen Stützstelle konstant galt.</summary>
        public int StundenEinzelpunkt;

        /// <summary>Jahresarbeitszahl Kälte (EER-Jahreswert) = Kälte / Kältestrom; 0 ohne Strom.</summary>
        public double EerJahreswert
        {
            get { return StromGesamtKwh > 0 ? KaelteGesamtKwh / StromGesamtKwh : 0.0; }
        }

        // ---- Abrechnung des Kältestroms (Entscheid E34; Kühlkonzept 6.1) -------------

        /// <summary>
        /// Der Kühlträger dieser Anlage, WENN er vom Stromträger des Projekts abweicht
        /// (<c>Tab_Energieanlagen.Kuehl_ID_Carrier</c>, <c>energy_carrier.id</c>); <b>0 = der
        /// Kältestrom trägt den Stromträger des Projekts</b> — kein oder derselbe Kühlträger, dann
        /// ist die Abrechnungsart wirkungslos (E34).
        /// </summary>
        public int Kuehltraeger;

        /// <summary>
        /// <b>Eigener Zähler</b> (E34, Wahl 2; <c>Kuehl_EigenerZaehler</c> = 1): Der Kältestrom läuft
        /// NEBEN der Stufenrechnung — er geht nicht in den Viertelstundenrest, wird also weder aus
        /// Photovoltaik noch aus dem Stromspeicher gedeckt und erhöht deren Eigenverbrauch nicht.
        /// Nur mit abweichendem <see cref="Kuehltraeger"/> gesetzt.
        /// </summary>
        public bool EigenerZaehler;

        /// <summary>Läuft der Kältestrom dieser Anlage außerhalb der Stufenrechnung (eigener Zähler)?</summary>
        public bool NebenDerStufenrechnung
        {
            get { return Kuehltraeger > 0 && EigenerZaehler; }
        }

        /// <summary>
        /// <b>Der Netzbezug, der dem Kältestrom dieser Anlage zukommt</b> [kWh]: über die
        /// Stufenrechnung (anteilig am Netzbezug, E34 Wahl 1 — ebenso ohne abweichenden Kühlträger)
        /// Σ Netzbezug(t) · Kältestrom(t) / Stromverbrauch(t) über die Viertelstunden, mit dem
        /// eigenen Zähler (Wahl 2) der ganze Kältestrom. Gesetzt am Laufende
        /// (<c>SimulationControl.KaeltestromNetzbezugAufteilen</c>).
        /// </summary>
        public double NetzbezugKwh;

        /// <summary>
        /// <b>Taktverlust im Kühlbetrieb</b> (Welle M4, WP1): die kleinste Modulationsstufe als Anteil
        /// der Kühlleistung der Stunde — Mindestleistung der Wärmepumpe durch ihre Heiz-Nennleistung,
        /// höchstens 1. 0 = keine Taktrechnung (Mindestleistung leer oder keine Nennleistung).
        /// </summary>
        public double Mindestanteil;

        /// <summary>Teillastkoeffizient C_d der Taktrechnung (EN 14825); Vorgabe 0,9.</summary>
        public double Cd = Waermepumpentakt.VORGABE_CD;

        /// <summary>Starts im Kühlbetrieb [1/a] — nur mit <see cref="Mindestanteil"/> &gt; 0.</summary>
        public int Starts;

        /// <summary>Taktstunden im Kühlbetrieb [h/a]: Kühlstunden unter der Mindestleistung.</summary>
        public int Taktstunden;

        /// <summary>Mehrstrom aus Taktverlust im Kühlbetrieb [kWh/a] — Teil von <see cref="StromGesamtKwh"/>.</summary>
        public double TaktstromKwh;

        /// <summary>Die Stunde des letzten Kühllaufs (für die Zählung der Laufphasen); −2 = keiner.</summary>
        internal int LetzteKuehlstunde = -2;

        // ---- KM3: Teillast und Takten der Kältemaschine (Fachkonzept 5.3) ------------
        // Nur mit Teillast_Weg (bzw. Kennfeld_Randweg GUETEGRAD für die extrapolierten Stunden) belegt; Starts und
        // TaktstromKwh/Taktstrom_stuendlich tragen dann den Mehrstrom und die Starts der Maschine.

        /// <summary>KM3: Verdichterstunden mit PLR_min ≤ PLR &lt; <see cref="KaelteFestwerte.TEILLASTSTUNDEN_LASTGRAD_GRENZE"/>.</summary>
        public int StundenTeillast;

        /// <summary>KM3: Stunden mit Gütegrad-Extrapolation über den Kennfeldrand.</summary>
        public int StundenExtrapoliert;

        /// <summary>KM3: Σ Kälte · Lastgrad der Verdichterstunden [kWh] — Zähler des kältegewichteten Lastgrads.</summary>
        public double LastgradGewichtKwh;

        /// <summary>KM3: Σ Kälte der Verdichterstunden mit Weg [kWh] — Nenner des kältegewichteten Lastgrads.</summary>
        public double LastgradKaelteKwh;

        /// <summary>KM3: kältegewichteter mittlerer Lastgrad der Verdichterstunden; 0 ohne solche Stunde.</summary>
        public double LastgradMittel => LastgradKaelteKwh > 0 ? LastgradGewichtKwh / LastgradKaelteKwh : 0.0;

        /// <summary>KM3: laufende Maschinen der Vorstunde <see cref="LetzteKuehlstunde"/> (Starts der Folgeschaltung).</summary>
        internal int LaufendVorstunde;

        /// <summary>Setzt das Ergebnis auf den Laufanfang.</summary>
        internal void Nullen()
        {
            Starts = 0;
            Taktstunden = 0;
            TaktstromKwh = 0;
            LetzteKuehlstunde = -2;
            StundenTeillast = 0;
            StundenExtrapoliert = 0;
            LastgradGewichtKwh = 0;
            LastgradKaelteKwh = 0;
            LaufendVorstunde = 0;
            Array.Clear(Kaelte_stuendlich, 0, Kaelte_stuendlich.Length);
            Array.Clear(Strom_stuendlich, 0, Strom_stuendlich.Length);
            Array.Clear(Taktstrom_stuendlich, 0, Taktstrom_stuendlich.Length);
            KaelteGesamtKwh = 0;
            StromGesamtKwh = 0;
            HilfsstromGesamtKwh = 0;
            NetzbezugKwh = 0;
            StundenMitKaelte = 0;
            StundenUnterKennlinie = 0;
            StundenUeberKennlinie = 0;
            Verlaengert = false;
            StundenEinzelpunkt = 0;
            StundenFreieKuehlung = 0;
            KaelteFreiKwh = 0;
            StundenRandwert = 0;
            StundenTakt = 0;
            StundenLeistungsgrenze = 0;
            OffenAnLeistungsgrenzeKwh = 0;
        }
    }

    /// <summary>
    /// <b>Die Kältekaskade</b> — die eigene Stundenschleife der Kälteseite (Kühlkonzept 5.5,
    /// E21). Sie läuft NACH der Wärmekaskade: Die reversible Maschine hat ihre Tagesbetriebsart
    /// dann festgelegt und ihr Brauchwasser bedient (5.2); was ihr von der Stunde bleibt, geht an
    /// die Kälte.
    ///
    /// <para><b>Reihenfolge</b> = die Kaskadenplätze der Wärmeseite, gefiltert auf die
    /// Kälteerzeuger — in KU2 die Wärmepumpen mit Kühlbetrieb, in der Reihenfolge ihrer
    /// Anlagen (<see cref="Erzeuger"/>). Es gibt keine zweite Belegung und keine
    /// Knappheitsreihenfolge: Die Kälteseite hat einen Kanal (4.5, benannte Abweichung).</para>
    ///
    /// <para><b>Je Stunde und Erzeuger</b>: Kapazität = Zeitanteil · Pkuehl(Stundentemperatur) aus
    /// der Kühlkennlinie (Laststufe <c>MAX(Last)</c>); gedeckt wird bis zum offenen Kältebedarf,
    /// Teillast linear mit konstantem EER (K8b). Kältestrom = Kälte / EER · (1 + Hilfsstromanteil)
    /// (6.1, K23). Der Rest ist die ungedeckte Kälte (<c>Kaelterestbedarf</c>, F-K12).</para>
    ///
    /// <para><b>Ohne Datenbank und ohne Kanalsatz</b>: Die Kaskade liest den Kältebedarf als
    /// Reihe und schreibt nur in ihre eigenen Felder — sie KANN keinen Wärmekanal berühren; die
    /// Deckungsprobe Kälte hält das trotzdem fest (4.3 #31).</para>
    /// </summary>
    public sealed class Kaeltekaskade
    {
        /// <summary>Stunden des Rechenjahres.</summary>
        public const int STUNDEN = Kanalsatz.STUNDEN_JAHR;

        /// <summary>Tage des Rechenjahres (kein Schaltjahr).</summary>
        public const int TAGE = STUNDEN / 24;

        /// <summary>Die Kälteerzeuger in Kaskadenreihenfolge.</summary>
        public List<Kaelteerzeuger> Erzeuger = new List<Kaelteerzeuger>();

        /// <summary>Die Tagesbetriebsart des Laufs (<see cref="TagesbetriebsartBestimmen"/>): true = Kühltag.</summary>
        public bool[] Kuehltage = new bool[TAGE];

        /// <summary>Der Kältebedarf, gegen den gedeckt wurde [kWh je Stunde] — eigene Kopie.</summary>
        public double[] Bedarf_stuendlich = new double[STUNDEN];

        /// <summary>Gedeckte Kälte aller Erzeuger je Stunde [kWh].</summary>
        public double[] Deckung_stuendlich = new double[STUNDEN];

        /// <summary>Kältestrom aller Erzeuger je Stunde [kWh] — <c>Stromverbrauch_Kuehlung_stuendlich</c> aus 6.1.</summary>
        public double[] Stromverbrauch_Kuehlung_stuendlich = new double[STUNDEN];

        /// <summary>Ungedeckte Kälte je Stunde [kWh].</summary>
        public double[] Rest_stuendlich = new double[STUNDEN];

        /// <summary>
        /// Kälte der Wärmepumpen aus freier Kühlung über die Wärmequelle je Stunde [kWh] (KU3-6) — Teil
        /// der Erzeugerkälte; eine Stunde mit Wert &gt; 0 zählt einmal, gleich wie viele Wärmepumpen frei kühlen.
        /// </summary>
        public double[] FreieKuehlungWp_stuendlich = new double[STUNDEN];

        /// <summary>Stunden, in denen irgendeine Wärmepumpe frei über ihre Wärmequelle kühlte (KU3-6).</summary>
        public int StundenFreieKuehlungWp
        {
            get
            {
                int n = 0;
                foreach (double v in FreieKuehlungWp_stuendlich) if (v > 0) n++;
                return n;
            }
        }

        /// <summary>
        /// <b>Die Kältespeicher der Kaskade</b> (KU3-5, E68; Kühlkonzept 5.5 Schritt 4) — Puffer mit der
        /// Verwendung <see cref="SimulationPufferspeicher.VERWENDUNG_KAELTE"/>, gerechnet in Listenreihenfolge.
        /// Leer: Die Stundenschleife rechnet Zeichen für Zeichen wie ohne Speicher.
        /// </summary>
        public List<SimulationPufferspeicher> Speicher = new List<SimulationPufferspeicher>();

        /// <summary>Kälte aus den Kältespeichern in den Kühlkanal je Stunde [kWh] — Teil von <see cref="Deckung_stuendlich"/>.</summary>
        public double[] Speicherentladung_stuendlich = new double[STUNDEN];

        /// <summary>Kälte der Erzeuger in die Kältespeicher je Stunde [kWh] — Teil der Erzeugerkälte, nicht der Deckung.</summary>
        public double[] Speicherladung_stuendlich = new double[STUNDEN];

        /// <summary>Kälte aus den Kältespeichern im Jahr [kWh].</summary>
        public double SpeicherentladungKwh;

        /// <summary>Kälte in die Kältespeicher im Jahr [kWh].</summary>
        public double SpeicherladungKwh;

        /// <summary>Kältebedarf im Jahr [kWh].</summary>
        public double BedarfGesamtKwh;

        /// <summary>Gedeckte Kälte im Jahr [kWh].</summary>
        public double DeckungGesamtKwh;

        /// <summary>Kältestrom im Jahr [kWh] einschließlich Hilfsstrom.</summary>
        public double StromGesamtKwh;

        /// <summary>davon Hilfsstrom [kWh].</summary>
        public double HilfsstromGesamtKwh;

        /// <summary>Ungedeckte Kälte im Jahr [kWh].</summary>
        public double RestGesamtKwh;

        /// <summary>davon an Heiztagen [kWh] — die Betriebsart des Tages war belegt (5.2, 5.5).</summary>
        public double RestAnHeiztagenKwh;

        /// <summary>davon an Kühltagen [kWh] — die Leistung reichte nicht (oder Brauchwasser/Sperrzeit belegten die Stunde).</summary>
        public double RestAnKuehltagenKwh;

        /// <summary>Zahl der Kühltage.</summary>
        public int AnzahlKuehltage
        {
            get
            {
                int n = 0;
                if (Kuehltage != null) foreach (bool k in Kuehltage) if (k) n++;
                return n;
            }
        }

        /// <summary>Jahresarbeitszahl Kälte (EER-Jahreswert) über alle Erzeuger = Kälte / Kältestrom (6.4).</summary>
        public double EerJahreswert
        {
            get { return StromGesamtKwh > 0 ? DeckungGesamtKwh / StromGesamtKwh : 0.0; }
        }

        // =====================================================================
        //  Die Umschaltung Heizen ↔ Kühlen (K8a, 5.2)
        // =====================================================================

        /// <summary>
        /// <b>Die Tagesbetriebsart</b> (K8a, Kühlkonzept 5.2): Ein Tag ist KÜHLTAG, wenn seine Summe
        /// im Kühlkanal die Summe im Heizkanal übersteigt — bestimmt am Tagesanfang aus den
        /// Tagessummen, gültig 24 Stunden (Mindestverweildauer ein Tag). Gleichstand und ein Tag
        /// ohne jeden Bedarf sind Heiztage.
        ///
        /// <para>Die Tagesbetriebsart gilt für den HEIZKANAL der reversiblen Maschine, nicht für die
        /// Maschine als Ganzes: Am Kühltag ist ihr Heizkanal gesperrt, Brauchwasser (und
        /// Prozesswärme) bleiben bedienbar; am Heiztag kühlt sie nicht.</para>
        /// </summary>
        /// <param name="heizkanal">Bedarf des Heizkanals je Stunde [kWh] (<see cref="Kanal.HEIZUNG"/>).</param>
        /// <param name="kuehlkanal">Bedarf des Kühlkanals je Stunde [kWh] (<see cref="Kanal.KUEHLUNG"/>).</param>
        public static bool[] TagesbetriebsartBestimmen(double[] heizkanal, double[] kuehlkanal)
        {
            var kuehltag = new bool[TAGE];
            if (kuehlkanal == null) return kuehltag;

            for (int d = 0; d < TAGE; d++)
            {
                double heiz = 0, kuehl = 0;
                for (int h = d * 24; h < d * 24 + 24; h++)
                {
                    if (heizkanal != null && h < heizkanal.Length) heiz += heizkanal[h];
                    if (h < kuehlkanal.Length) kuehl += kuehlkanal[h];
                }
                kuehltag[d] = kuehl > heiz;
            }
            return kuehltag;
        }

        /// <summary>
        /// Der Anteil jeder Stunde, der einer reversiblen Maschine für die Kälte bleibt (5.2):
        /// am Heiztag 0; am Kühltag 1 minus dem Zeitanteil, den der Verdichter für Wärme
        /// (Brauchwasser, Prozess, Speicherladung) gelaufen ist — „je Stunde zuerst Brauchwasser, der
        /// Rest an die Kälte"; in der Sperrzeit des Energieversorgers 0 (sie sperrt den Verdichter,
        /// gleich in welcher Betriebsart).
        /// </summary>
        /// <param name="kuehltage">Tagesbetriebsart (<see cref="TagesbetriebsartBestimmen"/>).</param>
        /// <param name="heizzeitanteil">Zeitanteil des Heizbetriebs je Stunde [0…1]; <c>null</c> = 0.</param>
        /// <param name="sperrung">Sperrzeit gepflegt (<c>Tab_Energieanlagen.Sperrung</c>)?</param>
        /// <param name="sperrVon">Beginn der Sperrzeit [h des Tages].</param>
        /// <param name="sperrBis">Ende der Sperrzeit [h des Tages], ausschließlich.</param>
        public static double[] ZeitanteilBilden(bool[] kuehltage, double[] heizzeitanteil,
                                                bool sperrung, int sperrVon, int sperrBis)
            => ZeitanteilBilden(kuehltage, heizzeitanteil,
                                Sperrprofil.Bilden(sperrung, sperrVon, sperrBis, null, 0).Verdichter);

        /// <summary>
        /// Wie <see cref="ZeitanteilBilden(bool[], double[], bool, int, int)"/>, mit der Stundenmaske des
        /// Sperrprofils (<see cref="Sperrprofil.Verdichter"/>, Welle V14); <c>null</c> = keine Sperre.
        /// </summary>
        public static double[] ZeitanteilBilden(bool[] kuehltage, double[] heizzeitanteil, bool[] sperrmaske)
        {
            var anteil = new double[STUNDEN];
            if (kuehltage == null) return anteil;

            for (int h = 0; h < STUNDEN; h++)
                anteil[h] = ZeitanteilDerStunde(kuehltage, heizzeitanteil, sperrmaske, h);
            return anteil;
        }

        /// <summary>
        /// Der Kühlzeitanteil einer Stunde <paramref name="h"/> nach <see cref="ZeitanteilBilden(bool[], double[], bool[])"/>
        /// (AK3-K: im Kreis nach der Wärmestunde, wenn der Heizzeitanteil der Stunde feststeht).
        /// </summary>
        public static double ZeitanteilDerStunde(bool[] kuehltage, double[] heizzeitanteil, bool[] sperrmaske, int h)
        {
            if (kuehltage == null) return 0.0;
            int tag = h / 24;
            if (tag >= kuehltage.Length || !kuehltage[tag]) return 0.0;

            if (sperrmaske != null && h < sperrmaske.Length && sperrmaske[h]) return 0.0;

            double heiz = (heizzeitanteil != null && h < heizzeitanteil.Length) ? heizzeitanteil[h] : 0.0;
            if (heiz < 0) heiz = 0;
            return heiz >= 1.0 ? 0.0 : 1.0 - heiz;
        }

        // =====================================================================
        //  Die Stundenschleife
        // =====================================================================

        /// <summary>
        /// Deckt den Kältebedarf Stunde für Stunde über die <see cref="Erzeuger"/> in Reihenfolge.
        /// </summary>
        /// <param name="bedarf">Kältebedarf je Stunde [kWh] (<c>SimulationKaeltebedarf.Kaeltebedarf</c>).</param>
        /// <param name="extrapolationErlaubt">Projekteinstellung für die ungünstige Seite der Kennlinie.</param>
        public void Rechnen(double[] bedarf, bool extrapolationErlaubt)
        {
            Beginnen(extrapolationErlaubt);
            for (int h = 0; h < STUNDEN; h++)
                StundeRechnen(h, (bedarf != null && h < bedarf.Length) ? bedarf[h] : 0.0);
            Abschliessen();
        }

        // AK3-K (Festlegung 16): der Zustand des Jahreslaufs zwischen den Stunden.
        private bool _extrapolation;
        private bool _mitSpeicher;
        private bool _mitFreierKuehlung;
        private int _naechsteStunde = -1;

        /// <summary>true: Der Kreis hat die Stunden je Stunde nach der Wärmestunde gerechnet (AK3-K); false = Jahreslauf.</summary>
        internal bool ImKreis { get; set; }

        /// <summary>Die nächste zu rechnende Stunde (8760 = das Jahr ist gerechnet; −1 = nicht begonnen).</summary>
        public int NaechsteStunde => _naechsteStunde;

        /// <summary>
        /// <b>Beginn des Jahres</b> (AK3-K, Festlegung 16): alle Reihen, Summen, Erzeugerzähler und Kältespeicher auf
        /// null. Danach je Stunde <see cref="StundeRechnen"/> in Jahresfolge, am Ende <see cref="Abschliessen"/> —
        /// der Jahreslauf <see cref="Rechnen"/> ist genau diese Folge.
        /// </summary>
        /// <param name="extrapolationErlaubt">Projekteinstellung für die ungünstige Seite der Kennlinie.</param>
        public void Beginnen(bool extrapolationErlaubt)
        {
            _extrapolation = extrapolationErlaubt;
            _naechsteStunde = 0;
            Array.Clear(Bedarf_stuendlich, 0, STUNDEN);
            Array.Clear(Deckung_stuendlich, 0, STUNDEN);
            Array.Clear(Stromverbrauch_Kuehlung_stuendlich, 0, STUNDEN);
            Array.Clear(Rest_stuendlich, 0, STUNDEN);
            Array.Clear(FreieKuehlungWp_stuendlich, 0, STUNDEN);
            BedarfGesamtKwh = 0;
            DeckungGesamtKwh = 0;
            StromGesamtKwh = 0;
            HilfsstromGesamtKwh = 0;
            RestGesamtKwh = 0;
            RestAnHeiztagenKwh = 0;
            RestAnKuehltagenKwh = 0;
            foreach (Kaelteerzeuger e in Erzeuger) e.Nullen();
            Array.Clear(Speicherentladung_stuendlich, 0, STUNDEN);
            Array.Clear(Speicherladung_stuendlich, 0, STUNDEN);
            SpeicherentladungKwh = 0;
            SpeicherladungKwh = 0;
            _mitSpeicher = Speicher != null && Speicher.Count > 0;
            if (_mitSpeicher) foreach (SimulationPufferspeicher sp in Speicher) sp.Reset();

            // KU3-2 (Kühlkonzept 5.5): Kältemaschinen mit Trocken- oder Nasskühler kühlen in einer
            // Stunde mit kaltem Rückkühler frei - dann decken sie VOR allen anderen. Ohne eine solche
            // Maschine bleibt die Reihenfolge Zeichen für Zeichen die der Liste.
            _mitFreierKuehlung = false;
            foreach (Kaelteerzeuger e in Erzeuger)
                if (e.Maschine != null && e.Maschine.FreieKuehlungMoeglich) _mitFreierKuehlung = true;
        }

        /// <summary>
        /// <b>Eine Kältestunde</b> (AK3-K, Festlegung 16): Ladewunsch der Kältespeicher, freie Kühlung, Entladung, Erzeuger
        /// in Listenfolge mit Taktverlust, Bereitschaftsverlust — in Jahresfolge nach <see cref="Beginnen"/> zu rufen.
        /// Der Jahreslauf ruft dieselbe Stunde; im AK3-Weg ruft sie der Kreis nach der Wärmestunde.
        /// </summary>
        /// <param name="h">Die Stunde; muss <see cref="NaechsteStunde"/> sein.</param>
        /// <param name="bedarfKwh">Kältebedarf der Stunde [kWh] (<c>SimulationKaeltebedarf.Kaeltebedarf[h]</c>).</param>
        public void StundeRechnen(int h, double bedarfKwh) => StundeRechnen(h, bedarfKwh, double.NaN);

        /// <summary>
        /// KK2 (Entwurf KK 2.5, 2.6): die Kältestunde am Stundenvorlauf <paramref name="kuehlVorlaufC"/> [°C] — dem kältesten
        /// verlangten Vorlauf der konvergierten Stunde. Mit Kältespeicher nie wärmer als dessen Vorlauf (Festlegung 10); jede
        /// Wärmepumpe wertet ihre <see cref="Kaelteerzeuger.Schar"/> am Vorlauf (nie kälter als ihre kleinste Stützstelle) und
        /// prüft die freie Kühlung gegen ihn, jede Kältemaschine rechnet bei <see cref="Kaeltemaschine.KaltwasserAmVorlauf"/>.
        /// NaN = der feste Vorlauf, Zeichen für Zeichen <see cref="StundeRechnen(int, double)"/>.
        /// </summary>
        public void StundeRechnen(int h, double bedarfKwh, double kuehlVorlaufC)
        {
            if (h != _naechsteStunde)
                throw new InvalidOperationException("Die Kältekaskade erwartet die Stunde " + _naechsteStunde + ", nicht " + h + ".");
            _naechsteStunde++;
            bool mitSpeicher = _mitSpeicher;
            bool mitFreierKuehlung = _mitFreierKuehlung;
            bool extrapolationErlaubt = _extrapolation;
            // Der Kühlkanal führt positive Mengen (K2); ein negativer Wert wäre ein Fehler der
            // Bedarfsseite und wird hier nicht zu einer „Kältelieferung".
            double b = bedarfKwh > 0 ? bedarfKwh : 0.0;
            Bedarf_stuendlich[h] = b;
            BedarfGesamtKwh += b;

            double rest = b;
            bool gleitend = !double.IsNaN(kuehlVorlaufC);
            double vorlauf = gleitend && mitSpeicher
                ? Kaeltevorlauf.MitSpeicherregel(kuehlVorlaufC, Kaeltevorlauf.SpeicherVorlauf(Speicher))
                : kuehlVorlaufC;

            // KU3-5 (5.5 Schritt 4): Ladewunsch der Kältespeicher in der Ladephase - bis zur
            // Abschaltschwelle, begrenzt durch die Ladeleistung. Die Erzeuger bekommen ihn als
            // Zusatzlast HINTER dem Raum: Was eine Stunde über den Bedarf hinaus erzeugt, lädt.
            // Geladen wird nur an Kühltagen (5.2) - sonst hielte die Kältemaschine den Vorrat den
            // Winter über gegen den Wärmeeintrag. Ohne Tagesbetriebsart (Kuehltage null) an jedem Tag.
            bool ladetag = Kuehltage == null || (h / 24 < Kuehltage.Length && Kuehltage[h / 24]);
            double lade = mitSpeicher && ladetag ? Ladewunsch() : 0.0;

            if (mitFreierKuehlung)
                foreach (Kaelteerzeuger e in Erzeuger)
                {
                    if (rest + lade <= 0) break;
                    if (e.Maschine == null) continue;
                    double kw = gleitend ? e.Maschine.KaltwasserAmVorlauf(vorlauf) : e.Maschine.Kaltwassertemperatur;
                    if (e.Maschine.FreieKuehlung(h, kw)) MaschineRechnen(e, h, ref rest, ref lade, kw);
                }

            // KU3-5: Die Kältespeicher entladen NACH der freien Kühlung und VOR den verdichtenden
            // Erzeugern.
            if (mitSpeicher && rest > 0) rest = SpeicherEntladen(h, rest);

            foreach (Kaelteerzeuger e in Erzeuger)
            {
                if (rest + lade <= 0) break;
                if (e.Maschine != null)
                {
                    double kw = gleitend ? e.Maschine.KaltwasserAmVorlauf(vorlauf) : e.Maschine.Kaltwassertemperatur;
                    if (!(mitFreierKuehlung && e.Maschine.FreieKuehlung(h, kw))) MaschineRechnen(e, h, ref rest, ref lade, kw);
                    continue;
                }
                double anteil = (e.Zeitanteil != null && h < e.Zeitanteil.Length) ? e.Zeitanteil[h] : 0.0;
                if (anteil <= 0 || e.Kennlinie == null) continue;

                // KK2: am Stundenvorlauf die Schar am Vorlauf der Maschine, die freie Kühlung gegen ihn.
                bool amVorlauf = gleitend && e.Schar != null;
                double vorlaufWp = amVorlauf ? e.VorlaufAmErzeuger(vorlauf) : e.KuehlVorlaufC;
                Kuehlkennlinie kennlinie = amVorlauf ? e.Schar.Kennlinie(vorlaufWp) : e.Kennlinie;
                double t = (e.Quelltemperatur != null && h < e.Quelltemperatur.Length) ? e.Quelltemperatur[h] : 0.0;
                KennlinienPunkt p = kennlinie.Auswerten(t, extrapolationErlaubt);
                switch (p.Lage)
                {
                    case KennlinienLage.KappungUnten: e.StundenUnterKennlinie++; break;
                    case KennlinienLage.ExtrapolationOben: e.StundenUeberKennlinie++; e.Verlaengert = true; break;
                    case KennlinienLage.KappungOben: e.StundenUeberKennlinie++; break;
                    case KennlinienLage.EinzelneStuetzstelle: e.StundenEinzelpunkt++; break;
                }

                double last = rest + lade;

                // KU3-6 (F3): freie Kühlung über die Wärmequelle VOR dem Verdichter - solange
                // Quellentemperatur plus Grädigkeit den Kaltwasser-Vorlauf nicht übersteigen, bis zur
                // Leistungsgrenze (ohne sie die Kälteleistung der Kennlinie) im offenen Zeitanteil.
                double frei = 0.0;
                if (e.FreieKuehlungSole && t + e.FreieKuehlungGraedigkeitK <= vorlaufWp)
                {
                    double grenze = e.FreieKuehlungLeistungKw ?? p.Pkuehl;
                    double moeglich = anteil * grenze;
                    if (moeglich > 0) frei = last < moeglich ? last : moeglich;
                }

                // Der Rest der Stunde über den Verdichter nach Kennlinie. Ohne freie Kühlung ist
                // restLast == last, und die Stunde rechnet Zeichen für Zeichen wie zuvor.
                double kapazitaet = anteil * p.Pkuehl;
                double restLast = frei > 0 ? last - frei : last;
                double verdichterKaelte = 0.0;
                double verdichter = 0.0;
                if (restLast > 0 && kapazitaet > 0 && p.Eer > 0)
                {
                    verdichterKaelte = restLast < kapazitaet ? restLast : kapazitaet;
                    verdichter = verdichterKaelte / p.Eer;

                    // Welle M4, WP1: Taktverlust nach EN 14825 auch im Kühlbetrieb - die
                    // Mindestleistung als Anteil der Kühlleistung der Stunde. Ohne Mindestanteil
                    // rechnet die Stunde wie zuvor.
                    if (e.Mindestanteil > 0)
                        verdichter += Taktverlust(e, h, verdichterKaelte, verdichter, e.Mindestanteil * p.Pkuehl);
                }
                if (!(frei > 0) && !(verdichterKaelte > 0)) continue;

                double deckung = frei > 0 ? frei + verdichterKaelte : verdichterKaelte;
                double raum = deckung < rest ? deckung : rest;
                double ladung = deckung - raum;

                double strom = verdichter * (1.0 + e.Hilfsstromanteil);
                if (frei > 0)
                {
                    // Pumpenstrom der freien Kühlung: EER-Ersatz wie am Rückkühler, mit Hilfsstrom.
                    double freiStrom = frei / KaelteFestwerte.FREIE_KUEHLUNG_EER;
                    verdichter += freiStrom;
                    strom += freiStrom * (1.0 + e.Hilfsstromanteil);
                    e.StundenFreieKuehlung++;
                    e.KaelteFreiKwh += frei;
                    FreieKuehlungWp_stuendlich[h] += frei;
                }

                e.Kaelte_stuendlich[h] = deckung;
                e.Strom_stuendlich[h] = strom;
                e.KaelteGesamtKwh += deckung;
                e.StromGesamtKwh += strom;
                e.HilfsstromGesamtKwh += strom - verdichter;
                e.StundenMitKaelte++;

                Deckung_stuendlich[h] += raum;
                Stromverbrauch_Kuehlung_stuendlich[h] += strom;
                DeckungGesamtKwh += raum;
                StromGesamtKwh += strom;
                HilfsstromGesamtKwh += strom - verdichter;

                rest -= raum;
                if (rest < 0) rest = 0;
                if (ladung > 0) lade -= SpeicherLaden(h, ladung);
            }

            // KU3-5: Bereitschaftsverlust der Kältespeicher - der Wärmeeintrag der Stunde.
            if (mitSpeicher) foreach (SimulationPufferspeicher sp in Speicher) sp.StundeAbschliessen(h);

            Rest_stuendlich[h] = rest;
            RestGesamtKwh += rest;
            if (Kuehltage != null && h / 24 < Kuehltage.Length && Kuehltage[h / 24]) RestAnKuehltagenKwh += rest;
            else RestAnHeiztagenKwh += rest;
        }

        /// <summary>Abschluss des Jahres (AK3-K, Festlegung 16): die Kennzahlen der Kältespeicher.</summary>
        public void Abschliessen()
        {
            if (_naechsteStunde != STUNDEN)
                throw new InvalidOperationException("Die Kältekaskade ist bei Stunde " + _naechsteStunde + ", nicht am Jahresende.");
            if (_mitSpeicher) foreach (SimulationPufferspeicher sp in Speicher) sp.KennzahlenBerechnen();
        }

        // =====================================================================
        //  Die Kältespeicher (KU3-5, E68; Kühlkonzept 5.5 Schritt 4)
        // =====================================================================

        /// <summary>
        /// Der Ladewunsch aller Kältespeicher der Stunde [kWh]. Die Ladephase folgt der Hysterese des
        /// Wärmepuffers: Sie beginnt, wenn der Vorrat auf die Einschaltschwelle fällt (der Lauf beginnt
        /// leer, also ladend), und endet an der Abschaltschwelle. In der Ladephase wünscht der Speicher
        /// den Abstand bis zur Abschaltschwelle, begrenzt durch seine Ladeleistung.
        /// </summary>
        private double Ladewunsch()
        {
            double summe = 0.0;
            foreach (SimulationPufferspeicher sp in Speicher)
            {
                if (!LadephaseFortschreiben(sp)) continue;
                summe += Ladebedarf(sp);
            }
            return summe;
        }

        /// <summary>Hysterese eines Kältespeichers fortschreiben; true = Ladephase.</summary>
        internal static bool LadephaseFortschreiben(SimulationPufferspeicher sp)
        {
            if (sp == null || !(sp.Q_max > 0)) return false;
            if (!sp.LaedtGerade && sp.SOC <= sp.SchwelleEin * sp.Q_max) sp.LaedtGerade = true;
            if (sp.LaedtGerade && sp.SOC >= sp.SchwelleAus * sp.Q_max - Rechenrand.ABSOLUT) sp.LaedtGerade = false;
            return sp.LaedtGerade;
        }

        /// <summary>Ladebedarf eines Kältespeichers in der Ladephase [kWh] — bis zur Abschaltschwelle.</summary>
        private static double Ladebedarf(SimulationPufferspeicher sp)
        {
            double bedarf = sp.SchwelleAus * sp.Q_max - sp.SOC;
            if (!(bedarf > 0)) return 0.0;
            if (sp.LadeleistungMax > 0 && bedarf > sp.LadeleistungMax) bedarf = sp.LadeleistungMax;
            return bedarf;
        }

        /// <summary>
        /// Entlädt die Kältespeicher in Listenreihenfolge in den Kühlkanal und liefert den Rest der Stunde —
        /// wie der Wärmepuffer in jeder Stunde mit Vorrat, auch in der Ladephase: Die Erzeuger decken den
        /// Raum vor dem Ladewunsch, eine Spitze bleibt damit auch beim Nachladen gekappt.
        /// </summary>
        private double SpeicherEntladen(int h, double rest)
        {
            foreach (SimulationPufferspeicher sp in Speicher)
            {
                if (rest <= 0) break;
                double e = sp.Entladen(rest, h, Kanal.KUEHLUNG);
                if (!(e > 0)) continue;
                Speicherentladung_stuendlich[h] += e;
                SpeicherentladungKwh += e;
                Deckung_stuendlich[h] += e;
                DeckungGesamtKwh += e;
                rest -= e;
                if (rest < Rechenrand.ABSOLUT) rest = 0;
            }
            return rest;
        }

        /// <summary>
        /// Verteilt Erzeugerkälte über den Raumbedarf hinaus auf die Kältespeicher in der Ladephase und
        /// liefert die aufgenommene Menge [kWh].
        /// </summary>
        private double SpeicherLaden(int h, double menge)
        {
            double aufgenommen = 0.0;
            foreach (SimulationPufferspeicher sp in Speicher)
            {
                if (menge - aufgenommen <= 0) break;
                if (!sp.LaedtGerade) continue;
                double wunsch = Ladebedarf(sp);
                if (!(wunsch > 0)) continue;
                double teil = Math.Min(wunsch, menge - aufgenommen);
                aufgenommen += sp.Laden(teil, h);
                // An der Abschaltschwelle endet die Ladephase in derselben Stunde - sonst holte der
                // Wärmeeintrag danach den Speicher jede Stunde zurück in die Ladung.
                LadephaseFortschreiben(sp);
            }
            Speicherladung_stuendlich[h] += aufgenommen;
            SpeicherladungKwh += aufgenommen;
            return aufgenommen;
        }

        /// <summary>
        /// Eine Stunde einer Kältemaschine (KU3-2): deckt höchstens <paramref name="rest"/> und den
        /// Ladewunsch <paramref name="lade"/> der Kältespeicher (KU3-5), bucht Kälte und Strom (Verdichter ·
        /// (1 + Hilfsstromanteil) + Hilfsstrom der Rückkühlung) und schreibt Rest und Ladewunsch fort.
        /// </summary>
        private void MaschineRechnen(Kaelteerzeuger e, int h, ref double rest, ref double lade, double kaltwasserC)
        {
            // KU3-5: Die Maschine sieht Raum und Ladewunsch als EINE Last; der Raum hat Vorrang.
            double last = rest + lade;
            KaeltemaschinenStunde s = e.Maschine.Stunde(h, last, kaltwasserC);
            if (s.Randwert) e.StundenRandwert++;
            if (s.Extrapoliert) e.StundenExtrapoliert++;
            if (!(s.KaelteKwh > 0))
            {
                if (rest > 0)
                {
                    e.StundenLeistungsgrenze++;
                    e.OffenAnLeistungsgrenzeKwh += rest;
                }
                return;
            }
            double raum = s.KaelteKwh < rest ? s.KaelteKwh : rest;
            double ladung = s.KaelteKwh - raum;

            double verdichter = s.VerdichterKwh * (1.0 + e.Hilfsstromanteil);
            double strom = verdichter + s.HilfsstromKwh;
            double hilfs = strom - s.VerdichterKwh;

            e.Kaelte_stuendlich[h] = s.KaelteKwh;
            e.Strom_stuendlich[h] = strom;
            e.KaelteGesamtKwh += s.KaelteKwh;
            e.StromGesamtKwh += strom;
            e.HilfsstromGesamtKwh += hilfs;
            e.StundenMitKaelte++;
            if (s.FreieKuehlung) { e.StundenFreieKuehlung++; e.KaelteFreiKwh += s.KaelteKwh; }
            if (s.Takt) e.StundenTakt++;
            if (e.Maschine.TeillastWirksam) TeillastBuchen(e, h, s);

            Deckung_stuendlich[h] += raum;
            Stromverbrauch_Kuehlung_stuendlich[h] += strom;
            DeckungGesamtKwh += raum;
            StromGesamtKwh += strom;
            HilfsstromGesamtKwh += hilfs;

            rest -= raum;
            if (rest < Rechenrand.ABSOLUT) rest = 0;
            if (rest > 0)
            {
                e.StundenLeistungsgrenze++;
                e.OffenAnLeistungsgrenzeKwh += rest;
            }
            if (ladung > 0) lade -= SpeicherLaden(h, ladung);
            if (lade < 0) lade = 0;
        }

        /// <summary>
        /// KM3 (Fachkonzept Teillast und Takten 3.3, 5.1 Schritt 7): bucht Mehrstrom, Starts, Teillaststunden und Lastgrad
        /// einer Verdichterstunde der Kältemaschine mit <c>Teillast_Weg</c>. Der Mehrstrom steckt schon im Verdichterstrom
        /// der Stunde (vor dem Hilfsstromzuschlag, wie an der Wärmepumpe); hier steht er zusätzlich in
        /// <see cref="Kaelteerzeuger.Taktstrom_stuendlich"/>. Starts: in einer Taktstunde die Starts im Takt, sonst je
        /// Maschine, die gegenüber der Vorstunde neu läuft (Übergang aus → an, Muster <see cref="Taktverlust"/>).
        /// </summary>
        private static void TeillastBuchen(Kaelteerzeuger e, int h, KaeltemaschinenStunde s)
        {
            if (s.FreieKuehlung || !(s.KaelteKwh > 0)) return;
            if (s.MehrstromKwh > 0)
            {
                e.TaktstromKwh += s.MehrstromKwh;
                if (h >= 0 && h < e.Taktstrom_stuendlich.Length) e.Taktstrom_stuendlich[h] += s.MehrstromKwh;
            }
            int vorher = e.LetzteKuehlstunde == h - 1 ? e.LaufendVorstunde : 0;
            if (s.Takt) e.Starts += s.Starts;
            else if (s.Laufend > vorher) e.Starts += s.Laufend - vorher;
            e.LetzteKuehlstunde = h;
            e.LaufendVorstunde = s.Laufend;
            if (!s.Takt && s.Lastgrad < KaelteFestwerte.TEILLASTSTUNDEN_LASTGRAD_GRENZE) e.StundenTeillast++;
            e.LastgradGewichtKwh += s.KaelteKwh * s.Lastgrad;
            e.LastgradKaelteKwh += s.KaelteKwh;
        }

        /// <summary>
        /// Die kleinste Modulationsstufe als Anteil (Welle M4, WP1): Mindestleistung durch
        /// Heiz-Nennleistung, höchstens 1; 0 ohne Mindestleistung oder ohne Nennleistung.
        /// </summary>
        public static double Mindestanteil(double mindestleistungKw, double nennleistungKw)
        {
            if (!(mindestleistungKw > 0) || !(nennleistungKw > 0)) return 0.0;
            double a = mindestleistungKw / nennleistungKw;
            return a > 1.0 ? 1.0 : a;
        }

        /// <summary>
        /// Taktverlust und Starts einer Kühlstunde (Welle M4, WP1): der Mehrstrom des Verdichters
        /// (<see cref="Waermepumpentakt.Mehrstrom"/>) bei Kälte unter der Mindestleistung, die Starts
        /// wie im Heizbetrieb. Liefert den Mehrstrom [kWh].
        /// </summary>
        private static double Taktverlust(Kaelteerzeuger e, int h, double kaelteKwh, double verdichterKwh,
                                          double mindestleistungKw)
        {
            if (!(kaelteKwh >= Rechenrand.ABSOLUT)) return 0.0;
            double mehr = 0.0;
            if (Waermepumpentakt.Taktet(kaelteKwh, mindestleistungKw))
            {
                e.Starts += Waermepumpentakt.StartsImTakt(kaelteKwh, mindestleistungKw);
                e.Taktstunden++;
                mehr = Waermepumpentakt.Mehrstrom(verdichterKwh, kaelteKwh, mindestleistungKw, e.Cd);
                e.TaktstromKwh += mehr;
                if (h >= 0 && h < e.Taktstrom_stuendlich.Length) e.Taktstrom_stuendlich[h] += mehr;
            }
            else if (e.LetzteKuehlstunde != h - 1)
            {
                e.Starts++;
            }
            e.LetzteKuehlstunde = h;
            return mehr;
        }

        // =====================================================================
        //  Der Netzbezug des Kältestroms (Entscheid E34; Kühlkonzept 6.1)
        // =====================================================================

        /// <summary>
        /// <b>Der Anteil des Kältestroms am Netzbezug</b> [kWh] — die Rechenregel „anteilig am
        /// Netzbezug" aus E34: je Viertelstunde t trägt der Kältestrom
        /// <c>Netzbezug(t) · Kältestrom(t) / Stromverbrauch(t)</c>; Eigenverbrauch aus Photovoltaik
        /// und Stromspeicher deckt Kältestrom und übrigen Strom damit im selben Verhältnis, ohne
        /// Vorrang.
        ///
        /// <para><b>Raster:</b> Netzbezug und Stromverbrauch als Leistung [kW] je Viertelstunde (die
        /// Reihen der Stufenrechnung), der Kältestrom als Stundenwert [kWh] — in jeder Viertelstunde
        /// derselben Stunde dieselbe Leistung, wie <c>Stundenwerte_zu_viertelstunden</c> ihn in den
        /// Rest gibt. Die Energie einer Viertelstunde ist Leistung / 4.</para>
        ///
        /// <para><b>Ränder:</b> Ein Viertel ohne Netzbezug, ohne Stromverbrauch oder ohne Kältestrom
        /// trägt nichts; der Anteil ist höchstens 1 (der Kältestrom ist Teil des Stromverbrauchs).
        /// Nicht endliche Werte tragen nichts.</para>
        /// </summary>
        /// <param name="netzbezugKw">Netzbezug je Viertelstunde [kW] (der Rest nach PV und Speicher).</param>
        /// <param name="stromverbrauchKw">Stromverbrauch je Viertelstunde [kW], einschließlich des Kältestroms.</param>
        /// <param name="kaeltestromKwh">Kältestrom der Anlage je Stunde [kWh].</param>
        public static double NetzbezugAnteilKwh(double[] netzbezugKw, double[] stromverbrauchKw,
                                                double[] kaeltestromKwh)
        {
            if (netzbezugKw == null || stromverbrauchKw == null || kaeltestromKwh == null) return 0.0;
            int viertel = Math.Min(netzbezugKw.Length, stromverbrauchKw.Length);
            double summe = 0.0;
            for (int q = 0; q < viertel; q++)
            {
                int h = q / 4;
                if (h >= kaeltestromKwh.Length) break;
                double k = kaeltestromKwh[h];
                double n = netzbezugKw[q];
                double v = stromverbrauchKw[q];
                if (!(k > 0) || !(n > 0) || !(v > 0) || double.IsInfinity(k) || double.IsInfinity(n) ||
                    double.IsInfinity(v)) continue;
                double anteil = k / v;
                if (anteil > 1.0) anteil = 1.0;
                summe += n * anteil / 4.0;
            }
            return summe;
        }

        // =====================================================================
        //  Die Deckungsprobe Kälte (Kühlkonzept 4.3 #31, 4.4; F-K6)
        // =====================================================================

        /// <summary>
        /// <b>Die Deckungsprobe Kälte</b> — das Gegenstück zur Bedarfsprobe Kälte, an der Stelle, an
        /// der die Deckung entsteht (Kühlkonzept 4.4). Sie hält vier Aussagen fest:
        /// <list type="number">
        /// <item><b>Kein Wärmeerzeuger hat in den Kühlkanal gebucht</b> — jede Deckungsbuchung
        /// (Direktdeckung, Speicherentladung, Heizstab, Pufferentladung) im Kühlkanal ist 0.</item>
        /// <item><b>Der Kühlkanal hat die Wärmekaskade unverändert verlassen</b> — er trägt danach
        /// genau den Kältebedarf.</item>
        /// <item><b>Kein Kälteerzeuger hat in einen Wärmekanal gebucht</b> — die Wärmekanäle sind vor
        /// und nach der Kältekaskade gleich.</item>
        /// <item><b>Die Kältebilanz schließt</b> — je Stunde Bedarf = Deckung + Rest, und die Deckung
        /// ist die Summe der Erzeuger.</item>
        /// </list>
        /// <para><b>Schärfer als die Energieprobe der Wärmeseite, ausdrücklich so festgelegt:</b> Eine
        /// Verletzung meldet der Lauf einmal mit der Stufe FEHLER, und das Gesamtergebnis ist
        /// fehlgeschlagen — dann hält die Trennung der Deckungswelten nicht, und keine Zahl des Laufs
        /// ist brauchbar.</para>
        /// </summary>
        /// <param name="kaskade">Die Kältekaskade des Laufs; <c>null</c> = keine gerechnet (Punkt 4 entfällt).</param>
        /// <param name="waermeBuchungenKuehlkanal">Die Buchungen der Wärmeerzeuger im Kühlkanal [kWh].</param>
        /// <param name="kaeltebedarf">Der Kältebedarf der Fassade je Stunde [kWh].</param>
        /// <param name="kuehlkanalNachWaermekaskade">Der Kühlkanal des Kanalsatzes nach der Wärmekaskade [kWh].</param>
        /// <param name="waermekanalAbweichungen">Stunden, in denen ein Wärmekanal sich während der Kältekaskade verändert hat.</param>
        public static DeckungsprobeKaelte Deckungsprobe(Kaeltekaskade kaskade,
                                                        IEnumerable<double> waermeBuchungenKuehlkanal,
                                                        double[] kaeltebedarf,
                                                        double[] kuehlkanalNachWaermekaskade,
                                                        int waermekanalAbweichungen)
        {
            var p = new DeckungsprobeKaelte();

            if (waermeBuchungenKuehlkanal != null)
                foreach (double b in waermeBuchungenKuehlkanal)
                    if (b != 0.0)
                    {
                        p.WaermeImKuehlkanal++;
                        if (Math.Abs(b) > p.MaxAbweichung) p.MaxAbweichung = Math.Abs(b);
                    }

            if (kaeltebedarf != null && kuehlkanalNachWaermekaskade != null)
                for (int h = 0; h < STUNDEN && h < kaeltebedarf.Length && h < kuehlkanalNachWaermekaskade.Length; h++)
                {
                    double d = Math.Abs(kuehlkanalNachWaermekaskade[h] - kaeltebedarf[h]);
                    if (d > p.MaxAbweichung) p.MaxAbweichung = d;
                    if (!Kanalsatz.ErhaltungOk(kaeltebedarf[h], kuehlkanalNachWaermekaskade[h],
                                               Kanalsatz.ERHALTUNG_SCHRITTE_SUMME_KAELTE))
                        p.KuehlkanalVeraendert++;
                }

            p.WaermekanalVeraendert = waermekanalAbweichungen > 0 ? waermekanalAbweichungen : 0;

            if (kaskade != null)
            {
                int schritte = 2 * kaskade.Erzeuger.Count + 2 * (kaskade.Speicher != null ? kaskade.Speicher.Count : 0) + 1;
                for (int h = 0; h < STUNDEN; h++)
                {
                    double summeErzeuger = 0;
                    foreach (Kaelteerzeuger e in kaskade.Erzeuger) summeErzeuger += e.Kaelte_stuendlich[h];
                    // KU3-5: Die Deckung ist Erzeugerkälte plus Speicherentladung minus Speicherladung.
                    summeErzeuger += kaskade.Speicherentladung_stuendlich[h] - kaskade.Speicherladung_stuendlich[h];

                    double bilanz = kaskade.Deckung_stuendlich[h] + kaskade.Rest_stuendlich[h];
                    double d1 = Math.Abs(bilanz - kaskade.Bedarf_stuendlich[h]);
                    double d2 = Math.Abs(summeErzeuger - kaskade.Deckung_stuendlich[h]);
                    if (d1 > p.MaxAbweichung) p.MaxAbweichung = d1;
                    if (d2 > p.MaxAbweichung) p.MaxAbweichung = d2;

                    bool ok = Kanalsatz.ErhaltungOk(kaskade.Bedarf_stuendlich[h], bilanz, schritte)
                              && Kanalsatz.ErhaltungOk(kaskade.Deckung_stuendlich[h], summeErzeuger, schritte)
                              && kaskade.Deckung_stuendlich[h] >= 0 && kaskade.Rest_stuendlich[h] >= 0;
                    if (!ok) p.BilanzVerletzt++;
                }
            }

            return p;
        }
    }

    /// <summary>Ergebnis der <see cref="Kaeltekaskade.Deckungsprobe"/> — Zählungen je Aussage.</summary>
    public sealed class DeckungsprobeKaelte
    {
        /// <summary>Buchungen von Wärmeerzeugern im Kühlkanal, die nicht 0 sind.</summary>
        public int WaermeImKuehlkanal;

        /// <summary>Stunden, in denen der Kühlkanal die Wärmekaskade verändert verlassen hat.</summary>
        public int KuehlkanalVeraendert;

        /// <summary>Stunden, in denen ein Wärmekanal sich während der Kältekaskade verändert hat.</summary>
        public int WaermekanalVeraendert;

        /// <summary>Stunden mit offener Kältebilanz.</summary>
        public int BilanzVerletzt;

        /// <summary>Größte gefundene Abweichung [kWh].</summary>
        public double MaxAbweichung;

        /// <summary>true, wenn alle vier Aussagen halten.</summary>
        public bool Ok
        {
            get { return WaermeImKuehlkanal == 0 && KuehlkanalVeraendert == 0 && WaermekanalVeraendert == 0 && BilanzVerletzt == 0; }
        }
    }
}

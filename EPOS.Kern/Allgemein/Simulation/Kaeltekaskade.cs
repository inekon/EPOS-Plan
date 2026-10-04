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

        /// <summary>Stunden in freier Kühlung (nur Kältemaschine).</summary>
        public int StundenFreieKuehlung;

        /// <summary>Kälte aus freier Kühlung [kWh] (nur Kältemaschine).</summary>
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

        /// <summary>Setzt das Ergebnis auf den Laufanfang.</summary>
        internal void Nullen()
        {
            Starts = 0;
            Taktstunden = 0;
            TaktstromKwh = 0;
            LetzteKuehlstunde = -2;
            Array.Clear(Kaelte_stuendlich, 0, Kaelte_stuendlich.Length);
            Array.Clear(Strom_stuendlich, 0, Strom_stuendlich.Length);
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
            {
                int tag = h / 24;
                if (tag >= kuehltage.Length || !kuehltage[tag]) continue;

                if (sperrmaske != null && h < sperrmaske.Length && sperrmaske[h]) continue;

                double heiz = (heizzeitanteil != null && h < heizzeitanteil.Length) ? heizzeitanteil[h] : 0.0;
                if (heiz < 0) heiz = 0;
                anteil[h] = heiz >= 1.0 ? 0.0 : 1.0 - heiz;
            }
            return anteil;
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
            Array.Clear(Bedarf_stuendlich, 0, STUNDEN);
            Array.Clear(Deckung_stuendlich, 0, STUNDEN);
            Array.Clear(Stromverbrauch_Kuehlung_stuendlich, 0, STUNDEN);
            Array.Clear(Rest_stuendlich, 0, STUNDEN);
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
            bool mitSpeicher = Speicher != null && Speicher.Count > 0;
            if (mitSpeicher) foreach (SimulationPufferspeicher sp in Speicher) sp.Reset();

            // KU3-2 (Kühlkonzept 5.5): Kältemaschinen mit Trocken- oder Nasskühler kühlen in einer
            // Stunde mit kaltem Rückkühler frei - dann decken sie VOR allen anderen. Ohne eine solche
            // Maschine bleibt die Reihenfolge Zeichen für Zeichen die der Liste.
            bool mitFreierKuehlung = false;
            foreach (Kaelteerzeuger e in Erzeuger)
                if (e.Maschine != null && e.Maschine.FreieKuehlungMoeglich) mitFreierKuehlung = true;

            for (int h = 0; h < STUNDEN; h++)
            {
                // Der Kühlkanal führt positive Mengen (K2); ein negativer Wert wäre ein Fehler der
                // Bedarfsseite und wird hier nicht zu einer „Kältelieferung".
                double b = (bedarf != null && h < bedarf.Length && bedarf[h] > 0) ? bedarf[h] : 0.0;
                Bedarf_stuendlich[h] = b;
                BedarfGesamtKwh += b;

                double rest = b;

                // KU3-5 (5.5 Schritt 4): Ladewunsch der Kältespeicher in der Ladephase - bis zur
                // Abschaltschwelle, begrenzt durch die Ladeleistung. Die Erzeuger bekommen ihn als
                // Zusatzlast HINTER dem Raum: Was eine Stunde über den Bedarf hinaus erzeugt, lädt.
                // Geladen wird nur an Kühltagen (5.2) - sonst hielte die Kältemaschine den Vorrat den
                // Winter über gegen den Wärmeeintrag. Ohne Tagesbetriebsart (Rechenprobe) an jedem Tag.
                bool ladetag = Kuehltage == null || (h / 24 < Kuehltage.Length && Kuehltage[h / 24]);
                double lade = mitSpeicher && ladetag ? Ladewunsch() : 0.0;

                if (mitFreierKuehlung)
                    foreach (Kaelteerzeuger e in Erzeuger)
                    {
                        if (rest + lade <= 0) break;
                        if (e.Maschine != null && e.Maschine.FreieKuehlung(h)) MaschineRechnen(e, h, ref rest, ref lade);
                    }

                // KU3-5: Die Kältespeicher außerhalb der Ladephase entladen NACH der freien Kühlung
                // und VOR den verdichtenden Erzeugern.
                if (mitSpeicher && rest > 0) rest = SpeicherEntladen(h, rest);

                foreach (Kaelteerzeuger e in Erzeuger)
                {
                    if (rest + lade <= 0) break;
                    if (e.Maschine != null)
                    {
                        if (!(mitFreierKuehlung && e.Maschine.FreieKuehlung(h))) MaschineRechnen(e, h, ref rest, ref lade);
                        continue;
                    }
                    double anteil = (e.Zeitanteil != null && h < e.Zeitanteil.Length) ? e.Zeitanteil[h] : 0.0;
                    if (anteil <= 0 || e.Kennlinie == null) continue;

                    double t = (e.Quelltemperatur != null && h < e.Quelltemperatur.Length) ? e.Quelltemperatur[h] : 0.0;
                    KennlinienPunkt p = e.Kennlinie.Auswerten(t, extrapolationErlaubt);
                    switch (p.Lage)
                    {
                        case KennlinienLage.KappungUnten: e.StundenUnterKennlinie++; break;
                        case KennlinienLage.ExtrapolationOben: e.StundenUeberKennlinie++; e.Verlaengert = true; break;
                        case KennlinienLage.KappungOben: e.StundenUeberKennlinie++; break;
                        case KennlinienLage.EinzelneStuetzstelle: e.StundenEinzelpunkt++; break;
                    }

                    double kapazitaet = anteil * p.Pkuehl;
                    if (!(kapazitaet > 0) || !(p.Eer > 0)) continue;

                    double last = rest + lade;
                    double deckung = last < kapazitaet ? last : kapazitaet;
                    double raum = deckung < rest ? deckung : rest;
                    double ladung = deckung - raum;
                    double verdichter = deckung / p.Eer;

                    // Welle M4, WP1: Taktverlust nach EN 14825 auch im Kühlbetrieb - die
                    // Mindestleistung als Anteil der Kühlleistung der Stunde. Ohne Mindestanteil
                    // rechnet die Stunde wie zuvor.
                    if (e.Mindestanteil > 0)
                        verdichter += Taktverlust(e, h, deckung, verdichter, e.Mindestanteil * p.Pkuehl);

                    double strom = verdichter * (1.0 + e.Hilfsstromanteil);

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

            if (mitSpeicher) foreach (SimulationPufferspeicher sp in Speicher) sp.KennzahlenBerechnen();
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
        /// Entlädt die Kältespeicher außerhalb ihrer Ladephase in Listenreihenfolge in den Kühlkanal und
        /// liefert den Rest der Stunde.
        /// </summary>
        private double SpeicherEntladen(int h, double rest)
        {
            foreach (SimulationPufferspeicher sp in Speicher)
            {
                if (rest <= 0) break;
                if (sp.LaedtGerade) continue;
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
        private void MaschineRechnen(Kaelteerzeuger e, int h, ref double rest, ref double lade)
        {
            // KU3-5: Die Maschine sieht Raum und Ladewunsch als EINE Last; der Raum hat Vorrang.
            double last = rest + lade;
            KaeltemaschinenStunde s = e.Maschine.Stunde(h, last);
            if (s.Randwert) e.StundenRandwert++;
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

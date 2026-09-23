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

        /// <summary>Setzt das Ergebnis auf den Laufanfang.</summary>
        internal void Nullen()
        {
            Array.Clear(Kaelte_stuendlich, 0, Kaelte_stuendlich.Length);
            Array.Clear(Strom_stuendlich, 0, Strom_stuendlich.Length);
            KaelteGesamtKwh = 0;
            StromGesamtKwh = 0;
            HilfsstromGesamtKwh = 0;
            StundenMitKaelte = 0;
            StundenUnterKennlinie = 0;
            StundenUeberKennlinie = 0;
            Verlaengert = false;
            StundenEinzelpunkt = 0;
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
        {
            var anteil = new double[STUNDEN];
            if (kuehltage == null) return anteil;

            for (int h = 0; h < STUNDEN; h++)
            {
                int tag = h / 24;
                if (tag >= kuehltage.Length || !kuehltage[tag]) continue;

                int std = h % 24;
                if (sperrung && std >= sperrVon && std < sperrBis) continue;

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

            for (int h = 0; h < STUNDEN; h++)
            {
                // Der Kühlkanal führt positive Mengen (K2); ein negativer Wert wäre ein Fehler der
                // Bedarfsseite und wird hier nicht zu einer „Kältelieferung".
                double b = (bedarf != null && h < bedarf.Length && bedarf[h] > 0) ? bedarf[h] : 0.0;
                Bedarf_stuendlich[h] = b;
                BedarfGesamtKwh += b;

                double rest = b;
                foreach (Kaelteerzeuger e in Erzeuger)
                {
                    if (rest <= 0) break;
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

                    double deckung = rest < kapazitaet ? rest : kapazitaet;
                    double verdichter = deckung / p.Eer;
                    double strom = verdichter * (1.0 + e.Hilfsstromanteil);

                    e.Kaelte_stuendlich[h] = deckung;
                    e.Strom_stuendlich[h] = strom;
                    e.KaelteGesamtKwh += deckung;
                    e.StromGesamtKwh += strom;
                    e.HilfsstromGesamtKwh += strom - verdichter;
                    e.StundenMitKaelte++;

                    Deckung_stuendlich[h] += deckung;
                    Stromverbrauch_Kuehlung_stuendlich[h] += strom;
                    DeckungGesamtKwh += deckung;
                    StromGesamtKwh += strom;
                    HilfsstromGesamtKwh += strom - verdichter;

                    rest -= deckung;
                    if (rest < 0) rest = 0;
                }

                Rest_stuendlich[h] = rest;
                RestGesamtKwh += rest;
                if (Kuehltage != null && h / 24 < Kuehltage.Length && Kuehltage[h / 24]) RestAnKuehltagenKwh += rest;
                else RestAnHeiztagenKwh += rest;
            }
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
                int schritte = 2 * kaskade.Erzeuger.Count + 1;
                for (int h = 0; h < STUNDEN; h++)
                {
                    double summeErzeuger = 0;
                    foreach (Kaelteerzeuger e in kaskade.Erzeuger) summeErzeuger += e.Kaelte_stuendlich[h];

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

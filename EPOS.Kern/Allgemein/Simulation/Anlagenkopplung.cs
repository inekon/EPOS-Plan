using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein gekoppeltes Gebäude im Kreis</b> (AK3-W3b): sein Gebäude-Stepper und der Faktor vom
    /// Rechenmaßstab des Modells auf das wirkliche Gebäude (Verhältnisrechnung E8; mit Zone 1).
    /// </summary>
    internal sealed class Kopplungsgebaeude
    {
        internal Kopplungsgebaeude(int index, long id, string bezeichnung, GebaeudeStepper stepper, double faktor)
        {
            Index = index;
            Id = id;
            Bezeichnung = bezeichnung ?? "";
            Stepper = stepper ?? throw new ArgumentNullException(nameof(stepper));
            Faktor = faktor > 0.0 && !double.IsInfinity(faktor) ? faktor : 1.0;
        }

        /// <summary>Zeilenindex des Gebäudes im Lauf (Merkplatz der Ergebnisse).</summary>
        internal int Index { get; }

        /// <summary>Gebäude-Id — Ordnung des Rundungsrests der Verteilung.</summary>
        internal long Id { get; }

        /// <summary>Bezeichnung für Meldungen.</summary>
        internal string Bezeichnung { get; }

        /// <summary>Der Stepper (unskaliert, Rechenmaßstab des Modells).</summary>
        internal GebaeudeStepper Stepper { get; }

        /// <summary>Faktor Modell → wirkliches Gebäude (E8), &gt; 0.</summary>
        internal double Faktor { get; set; }
    }

    /// <summary>Das Ergebnis einer gekoppelten Stunde (AK3-W3b).</summary>
    internal sealed class Kopplungsstunde
    {
        /// <summary>
        /// Begrenzte Durchläufe der Stunde (≥ 1). Der unbegrenzte Probeschritt, der bei mehreren Gebäuden oder Zonen den
        /// Verteilschlüssel liefert, zählt nicht mit.
        /// </summary>
        internal int Durchlaeufe { get; set; }

        /// <summary>Heizleistung je Gebäude [W], Maßstab des wirklichen Gebäudes (Reihenfolge der Gebäude).</summary>
        internal double[] HeizlastW { get; set; }

        /// <summary>Bedarfsgewichteter Vorlauf der Stunde [°C]; NaN ohne gekoppelten Bedarf.</summary>
        internal double VorlaufC { get; set; }

        /// <summary>Bedarfsgewichteter Rücklauf der Stunde [°C]; NaN ohne gekoppelten Bedarf.</summary>
        internal double RuecklaufC { get; set; }

        /// <summary>Das Angebot des letzten Durchlaufs.</summary>
        internal Stundenangebot Angebot { get; set; }

        /// <summary>true: Die Schranke des Angebots (Leistung oder Vorlauf) begrenzt eine Zone der Lösung.</summary>
        internal bool SchrankeGreift { get; set; }

        /// <summary>
        /// Der Projektvorlauf V des Kreises [°C] aus der Lösung (bedarfsgewichtet, nach der H2-Naht); beim Festhalten der
        /// Vorlauf des ersten Durchlaufs; NaN, wenn er unbekannt blieb. Gegenstand des Orakels O1 (AK3-W3c).
        /// </summary>
        internal double VorlaufKreisC { get; set; }

        /// <summary>true: Die Stützstelle pendelte; gerechnet wurde mit der Stützstelle des ersten Durchlaufs (2.4).</summary>
        internal bool Festgehalten { get; set; }

        /// <summary>Die Stundenergebnisse der Lösung je Gebäude und Zone (unskaliert; der offene Schritt der Stepper).</summary>
        internal IReadOnlyList<Stundenergebnis>[] Loesung { get; set; }

        /// <summary>Die größte Anhebung des Raumeinflusses (H2), mit der die Lösung gerechnet wurde [K]; 0 ohne H2.</summary>
        internal double AnhebungK { get; set; }

        /// <summary>Das Kälteangebot der Stunde (AK3-K); <c>null</c> ohne Kälteschranke.</summary>
        internal Kaeltestundenangebot? Kaelteangebot { get; set; }

        /// <summary>true: Die Kälteschranke hat die Kühlleistung einer Zone der Lösung gekappt (AK3-K).</summary>
        internal bool KaelteschrankeGreift { get; set; }

        /// <summary>Die Jahresstunde (AK3-K: Zuordnung zur Kältestunde danach).</summary>
        internal int Jahresstunde { get; set; } = -1;

        /// <summary>
        /// KK3: der Erzeugervorlauf der Kälteseite der Stunde [°C] — der kälteste verlangte Vorlauf der Lösung (gleitender
        /// Erzeuger, Entwurf KK 2.3); NaN = der feste Vorlauf (ohne <see cref="Anlagenkopplung.KuehlRaumeinfluss"/>).
        /// </summary>
        internal double KuehlVorlaufC { get; set; } = double.NaN;

        /// <summary>KK3: die größte Absenkung des Raumeinflusses der Kühlkurve, mit der die Lösung gerechnet wurde [K].</summary>
        internal double KuehlAbsenkungK { get; set; }
    }

    /// <summary>
    /// <b>Die Naht H2</b> (Q-AK3-2 (a), Welle W4): der Projektvorlauf der Stunde aus dem Vorlauf
    /// <paramref name="vorlaufC"/> (Heizkurve bzw. bedarfsgewichtet) und der Lösung des letzten Durchlaufs
    /// <paramref name="loesung"/> (Raumtemperaturen derselben Stunde; <c>null</c> vor dem ersten Schritt).
    /// </summary>
    internal delegate double Vorlaufnaht(int stunde, double vorlaufC, IReadOnlyList<IReadOnlyList<Stundenergebnis>> loesung);

    /// <summary>
    /// <b>Die Naht der Kälteschranke</b> (AK3-K 4.1, 4.4): das Kälteangebot des nächsten Durchlaufs aus dem Angebot am
    /// Stundenbeginn <paramref name="angebot"/> und der Lösung des letzten Durchlaufs <paramref name="loesung"/>
    /// (<c>null</c> vor dem ersten Schritt) — die nachgezogene Kälteschranke. Im Lauf ohne Naht (die Schranke liest
    /// nur den Zustand am Stundenbeginn) bleibt sie über die Durchläufe gleich; das Orakel O1k setzt hier eine stetige
    /// Testkennlinie ein (Muster <see cref="Vorlaufnaht"/> für O1).
    /// </summary>
    internal delegate Kaeltestundenangebot Kaeltenaht(int stunde, Kaeltestundenangebot angebot,
                                                      IReadOnlyList<IReadOnlyList<Stundenergebnis>> loesung);

    /// <summary>
    /// <b>Benannter Fehler des Kreises</b> (Entwurf AK3 2.4, F-A15): Höchstzahl der Durchläufe oder
    /// Produktschranke der Mehrzonen gerissen — Gebäude, Stunde, Beteiligte, größte verbleibende Abweichung
    /// und letzter Stand; keine stille Näherung.
    /// </summary>
    internal sealed class AnlagenkopplungException : Exception
    {
        internal AnlagenkopplungException(string gebaeude, int stunde, string beteiligte, string abweichung,
                                          string letzterStand)
            // W4b: die Meldung aus den Ressourcen (AK3_MSG_KONVERGENZ, beide Sprachen) - was geschah, wo, und was der
            // Anwender prüfen kann; die Zahlen invariant wie im Laufprotokoll.
            : base(string.Format(CultureInfo.InvariantCulture, MyResource.Resource.AK3_MSG_KONVERGENZ,
                                 gebaeude, stunde, beteiligte, abweichung, letzterStand))
        {
            Gebaeude = gebaeude;
            Stunde = stunde;
            Beteiligte = beteiligte;
            Abweichung = abweichung;
            LetzterStand = letzterStand;
        }

        internal string Gebaeude { get; }
        internal int Stunde { get; }
        internal string Beteiligte { get; }
        internal string Abweichung { get; }
        internal string LetzterStand { get; }
    }

    /// <summary>
    /// <b>Der Iterationsrahmen der Anlagenkopplung AK3</b> (Entwurf AK3 2.3, 2.4; Festlegungen 5 bis 12; E102)
    /// — je Stunde der geschlossene Kreis zwischen den Gebäude-Steppern und der befragbaren Kaskade
    /// (<see cref="Angebotsfunktion"/>). Die echte Kaskadenstunde läuft danach einmal (Festlegung 4).
    /// <list type="number">
    /// <item><b>Verteilschlüssel</b> (Festlegung 9) — bei mehreren Gebäuden oder Zonen der unbegrenzte Probeschritt
    /// jedes Gebäudes; er zählt nicht als Durchlauf.</item>
    /// <item><b>Durchlauf k ≥ 1</b> — V₀ der Projektvorlauf (Pass 1 bzw. Heizkurve, mit der H2-Naht
    /// <see cref="Vorlaufkorrektur"/>), S_k = Angebot(h, V_k) verteilt mit <see cref="Stundenverteilung"/> auf
    /// Gebäude und Zonen, je Gebäude <see cref="GebaeudeStepper.Schritt"/> mit der Schranke (Rand
    /// <see cref="Stundenrand.MitVerfuegbarkeit"/>, Zeichen für Zeichen AK2 mit Schranke = S_k), V_k+1
    /// bedarfsgewichtet (Festlegung 12), S_k+1 neu. AK3-W3c: Auch der erste Durchlauf rechnet begrenzt — der
    /// unbegrenzte Schritt ist keine Lösung, selbst wenn sein Stundenmittel unter S liegt (innerhalb der Stunde
    /// kann die Schranke dennoch greifen; Orakel O2). Eine Schranke ohne Grenze (1 GW) rechnet Bit für Bit wie
    /// ohne Schranke (Gate „ohne Grenzen bitgleich zu AK1“).</item>
    /// <item><b>Abbruch</b>: |Δθ_i| ≤ 0,01 K, |ΔΦ| ≤ 0,1 W, |ΔV| ≤ 0,05 K, |ΔS| ≤ 0,1 W — oder das Angebot S
    /// Bit für Bit unverändert (der Vorlauf wirkt nur über S; der nächste Schritt wäre derselbe). Höchstzahl 20,
    /// Mehrzonen Zonenzahl × Durchläufe ≤ 120 (<see cref="GrenzeErreicht"/>); darüber
    /// <see cref="AnlagenkopplungException"/> mit der größten Abweichung der letzten beiden Durchläufe.</item>
    /// <item><b>Fallwechsel</b> (2.4): Wechsel der Stützstelle der Wärmepumpe am Vorlauf und des Betriebsfalls je
    /// Zone (<see cref="Stundenergebnis.Begrenzungsgrund"/>) zwischen zwei Durchläufen werden gezählt. Kehrt die
    /// Stützstelle zu einer schon besuchten zurück (Pendeln), wird die des ersten Durchlaufs festgehalten: ein letzter
    /// Durchlauf mit Vorlauf und Angebot des ersten (<see cref="StundenFestgehalten"/>). Den Betriebsfall der Zonen
    /// hält im Mehrzonenfall die Zonenschleife (Mustertreue).</item>
    /// <item><b>H2-Naht</b> <see cref="Vorlaufkorrektur"/> (W4): mit ihr wird der Vorlauf aus der Lösung jedes
    /// Durchlaufs neu gebildet, schon nach dem Probeschritt.</item>
    /// </list>
    /// <para>Übernahme mit <see cref="Festschreiben"/>; den Heizbedarf gibt der Aufrufer über die Naht
    /// <see cref="IStundenbedarf"/> an die Kaskadenstunde. Ohne Datenbank, ohne Protokoll, deterministisch.</para>
    /// </summary>
    internal sealed class Anlagenkopplung
    {
        /// <summary>Abbruchmaß Raumluft [K] (Konzept 6.3).</summary>
        internal const double ABBRUCH_THETA_K = 0.01;
        /// <summary>Abbruchmaß Heizleistung [W].</summary>
        internal const double ABBRUCH_PHI_W = 0.1;
        /// <summary>Abbruchmaß Vorlauf [K].</summary>
        internal const double ABBRUCH_VORLAUF_K = 0.05;
        /// <summary>Abbruchmaß Schranke [W].</summary>
        internal const double ABBRUCH_SCHRANKE_W = 0.1;
        /// <summary>Abbruchmaß Kälteschranke [W] (AK3-K 4.4, Festlegung 15; ADR-005 wie die Heizlast).</summary>
        internal const double ABBRUCH_KAELTESCHRANKE_W = 0.1;
        /// <summary>Höchstzahl der Anlagendurchläufe je Stunde (H-F9).</summary>
        internal const int HOECHSTZAHL = 20;
        /// <summary>Höchstes Produkt Zonen × Anlagendurchläufe im Mehrzonenfall.</summary>
        internal const int PRODUKT_MEHRZONEN = 120;

        private readonly Kopplungsgebaeude[] _gebaeude;
        private readonly IReadOnlyList<IErzeugerkapazitaet> _erzeuger;

        /// <summary>Die Erzeuger des Kreises in Kaskadenreihenfolge (Proben).</summary>
        internal IReadOnlyList<IErzeugerkapazitaet> Erzeuger => _erzeuger;
        private readonly ISpeicherangebot _speicher;
        private readonly long[] _ids;
        private readonly bool[] _alle;

        /// <param name="gebaeude">Die gekoppelten Gebäude (eingeschwungen, <see cref="GebaeudeStepper.Beginnen"/>).</param>
        /// <param name="erzeuger">Die Heizerzeuger als Kapazitäten (Festlegung 5).</param>
        /// <param name="speicher">Der Speicheranteil (Festlegung 6); <c>null</c> = keiner.</param>
        internal Anlagenkopplung(IReadOnlyList<Kopplungsgebaeude> gebaeude, IReadOnlyList<IErzeugerkapazitaet> erzeuger,
                                 ISpeicherangebot speicher)
        {
            if (gebaeude == null || gebaeude.Count == 0) throw new ArgumentException("Der Kreis braucht ein gekoppeltes Gebäude.", nameof(gebaeude));
            _gebaeude = gebaeude.ToArray();
            _erzeuger = erzeuger ?? Array.Empty<IErzeugerkapazitaet>();
            _speicher = speicher;
            _ids = _gebaeude.Select(g => g.Id).ToArray();
            _alle = Enumerable.Repeat(true, _gebaeude.Length).ToArray();
            DurchlaeufeVerteilung = new int[HOECHSTZAHL + 1];
        }

        /// <summary>Die Gebäude des Kreises.</summary>
        internal IReadOnlyList<Kopplungsgebaeude> Gebaeude => _gebaeude;

        /// <summary>
        /// <b>Naht H2</b> (Q-AK3-2, Welle W4): korrigiert den Projektvorlauf V der Stunde aus den Raumtemperaturen
        /// derselben Stunde. <c>null</c> = keine Korrektur (bis W4; die Orakel O1/O1p setzen ein Testdouble ein).
        /// </summary>
        internal Vorlaufnaht Vorlaufkorrektur { get; set; }

        /// <summary>
        /// <b>Die echte H2</b> (W4a; <see cref="WindowsFormsApplication1.Raumeinfluss"/>): hebt je Gebäude den Vorlauf der Heizkurve
        /// im Rand jeder Zone an und führt die Anhebung je Durchlauf aus der Lösung derselben Stunde nach; der
        /// Projektvorlauf des Angebots ist der der Lösung (angehoben, gekappt). <c>null</c> = ohne Raumeinfluss
        /// (Zeichen für Zeichen der Kreis ohne H2).
        /// </summary>
        internal Raumeinfluss Raumeinfluss { get; set; }

        /// <summary>
        /// <b>KK3 — Raumeinfluss der Kühlkurve und gleitender Erzeugervorlauf</b> (<see cref="WindowsFormsApplication1.KuehlRaumeinfluss"/>):
        /// senkt je Gebäude den Kühlvorlauf im Rand jeder Zone, führt die Absenkung je Durchlauf nach (ΔK2) und fragt die
        /// Kälteschranke am kältesten verlangten Vorlauf. Wirkt nur mit <see cref="Kaelteschranke"/>; <c>null</c> = fester
        /// Kühlvorlauf (Schalter aus bitgleich).
        /// </summary>
        internal KuehlRaumeinfluss KuehlRaumeinfluss { get; set; }

        /// <summary>Stunden, in denen am Kreis ein Heizungspuffer steht und nichts aus ihm entnehmbar ist (Festlegung 22).</summary>
        internal int StundenSpeicherLeer { get; private set; }

        /// <summary>Stunden, in denen die Stützstelle pendelte und die des ersten Durchlaufs festgehalten wurde.</summary>
        internal int StundenFestgehalten { get; private set; }

        /// <summary>Stützstelle der Wärmepumpe zum Vorlauf (Fallwechsel); <c>null</c> = nicht gezählt.</summary>
        internal Func<double, int> Stuetzstelle { get; set; }

        /// <summary>
        /// UB‑E2: der Kreisrücklauf je Stunde [°C], geschrieben je Durchlauf vor der Angebotsabfrage — gelesen von einer
        /// Wärmepumpe mit Bivalenzobjekt im Vorwärmbetrieb (B3). <c>null</c> = keine solche Wärmepumpe (Bestand).
        /// </summary>
        internal double[] Kreisruecklauf { get; set; }

        /// <summary>
        /// Hält die Stützstelle des ersten Durchlaufs fest, wenn sie pendelt (Vorgabe <c>true</c>). Mit der
        /// Interpolation über den Vorlauf (I-1) ist die Kapazität stetig; dann wird nur gezählt (Entwurf 3, O3 entfällt).
        /// </summary>
        internal bool StuetzstelleHalten { get; set; } = true;

        /// <summary>Zahl der Stunden je Durchlaufzahl (Index = Durchläufe).</summary>
        internal int[] DurchlaeufeVerteilung { get; }

        /// <summary>Gerechnete Stunden.</summary>
        internal int Stunden { get; private set; }

        /// <summary>Stunden, in denen die Schranke gegriffen hat.</summary>
        internal int StundenAnDerSchranke { get; private set; }

        /// <summary>
        /// <b>Die Kälteschranke</b> (AK3-K 4.2): das Kälteangebot am festen Kühlvorlauf, einmal am Stundenbeginn befragt,
        /// verteilt wie die Wärmeschranke (bei mehreren Gebäuden oder Zonen nach dem unbegrenzten Kühlbedarf des
        /// Probeschritts) und je Zone über <see cref="Stundenrand.MitKaelteverfuegbarkeit"/> in den Schritt gegeben.
        /// <c>null</c> = keine Kälteschranke (ohne Kälteseite) — Zeichen für Zeichen wie zuvor.
        /// </summary>
        internal Kaelteschranke Kaelteschranke { get; set; }

        /// <summary>Stunden, in denen die Kälteschranke gegriffen hat („Kälteschranke gegriffen“, AK3-K).</summary>
        internal int StundenAnDerKaelteschranke { get; private set; }

        /// <summary>
        /// Die nachgezogene Kälteschranke je Durchlauf (<see cref="Kaeltenaht"/>); <c>null</c> = das Angebot am
        /// Stundenbeginn gilt für alle Durchläufe (Vorgabe).
        /// </summary>
        internal Kaeltenaht Kaeltekorrektur { get; set; }

        /// <summary>
        /// <b>Umschaltstunden</b> (AK3-K 4.5, Festlegung 20): Stunden, in denen eine Schranke — Wärme oder Kälte — mit dem
        /// Anlagengrund <see cref="Verfuegbarkeitsgrund.Umschaltung"/> eine Zone begrenzt: gerade der Wegfall der reversiblen
        /// Wärmepumpe an die Gegenseite bindet.
        /// </summary>
        internal int StundenUmschaltung { get; private set; }

        /// <summary>
        /// <b>Kälte-Restbedarf</b> (AK3-K, Festlegung 14): Stunden, in denen die Kältestunde nach der Wärmestunde einen Rest
        /// lässt (<see cref="KaelteRestZaehlen"/>); gezählt, nicht nachiteriert.
        /// </summary>
        internal int KaelteRestStunden { get; private set; }

        /// <summary>Der Kälte-Restbedarf dieser Stunden [kWh].</summary>
        internal double KaelteRestKwh { get; private set; }

        /// <summary>
        /// Stunden an der Kälteschranke, in denen die echte Kältestunde (wirklicher Heizzeitanteil nach der Wärmestunde) eine
        /// höhere Kälteschranke ergeben hätte als die Vorrangschätzung am Stundenbeginn (Entwurf 4.5, Prüfauftrag K3).
        /// </summary>
        internal int KaelteschrankeZuKnappStunden { get; private set; }

        /// <summary>Σ (echte − geschätzte Kälteschranke) dieser Stunden [kWh].</summary>
        internal double KaelteschrankeZuKnappKwh { get; private set; }

        /// <summary>Stunden an der Kälteschranke, deren Vorrangschätzung gegen die echte Kältestunde geprüft wurde.</summary>
        internal int KaelteschrankeGeprueftStunden { get; private set; }

        /// <summary>Größte |echte − geschätzte Kälteschranke| der geprüften Stunden [kW].</summary>
        internal double KaelteschrankeAbweichungMaxKw { get; private set; }

        /// <summary>Stunden mit Vorrangschätzung über der echten Kältestunde (Schranke zu weit).</summary>
        internal int KaelteschrankeZuWeitStunden { get; private set; }

        /// <summary>Die zuletzt abgeschlossene Stunde des Kreises (für die Kältestunde danach).</summary>
        internal Kopplungsstunde LetzteStunde { get; private set; }

        /// <summary>Zählt den Rest der Kältestunde <paramref name="restKwh"/> (Festlegung 14).</summary>
        internal void KaelteRestZaehlen(double restKwh)
        {
            if (!(restKwh > 0.0)) return;
            KaelteRestStunden++;
            KaelteRestKwh += restKwh;
        }

        /// <summary>
        /// Vergleicht die Kälteschranke der Stunde mit der, die der wirkliche Heizzeitanteil ergeben hätte
        /// <paramref name="echtKw"/> (Prüfauftrag K3 zur Vorrangschätzung); zählt nur Stunden an der Kälteschranke.
        /// </summary>
        internal void VorrangschaetzungPruefen(Kopplungsstunde k, double echtKw)
        {
            if (k?.Kaelteangebot == null || !k.KaelteschrankeGreift) return;
            KaelteschrankeGeprueftStunden++;
            double d = echtKw - k.Kaelteangebot.Value.LeistungKw;
            if (Math.Abs(d) > KaelteschrankeAbweichungMaxKw) KaelteschrankeAbweichungMaxKw = Math.Abs(d);
            if (d * 1000.0 > ABBRUCH_KAELTESCHRANKE_W)
            {
                KaelteschrankeZuKnappStunden++;
                KaelteschrankeZuKnappKwh += d;
            }
            else if (-d * 1000.0 > ABBRUCH_KAELTESCHRANKE_W) KaelteschrankeZuWeitStunden++;
        }

        /// <summary>Gezählte Wechsel der Stützstelle zwischen zwei Durchläufen.</summary>
        internal int StuetzstellenWechsel { get; private set; }

        /// <summary>Gezählte Wechsel des Betriebsfalls einer Zone zwischen zwei begrenzten Durchläufen.</summary>
        internal int FallWechsel { get; private set; }

        /// <summary>Die größte Zahl der Durchläufe einer Stunde.</summary>
        internal int DurchlaeufeMax { get; private set; }

        /// <summary>Mittel der Durchläufe je Stunde.</summary>
        internal double DurchlaeufeMittel
        {
            get
            {
                long s = 0;
                for (int k = 0; k < DurchlaeufeVerteilung.Length; k++) s += (long)k * DurchlaeufeVerteilung[k];
                return Stunden == 0 ? 0.0 : (double)s / Stunden;
            }
        }

        /// <summary>
        /// <b>Die gekoppelte Stunde <paramref name="h"/></b>: iteriert bis zum Abbruch und lässt in jedem Stepper
        /// den Schritt der Lösung offen (<see cref="Festschreiben"/> übernimmt ihn).
        /// </summary>
        /// <param name="h">Die Jahresstunde.</param>
        /// <param name="vorlaufStartC">Der Projektvorlauf zum Start (Pass 1 bzw. Heizkurve); NaN = unbekannt.</param>
        /// <param name="vorrang">Der Vorrang der Stunde (Festlegung 7).</param>
        /// <exception cref="AnlagenkopplungException">Höchstzahl oder Produktschranke gerissen.</exception>
        internal Kopplungsstunde Stunde(int h, double vorlaufStartC, Stundenvorrang vorrang)
        {
            int n = _gebaeude.Length;
            double v = vorlaufStartC;
            if (Vorlaufkorrektur != null) v = Vorlaufkorrektur(h, v, null);
            Raumeinfluss h2 = Raumeinfluss;
            h2?.StundeBeginnen();
            // KK3: Raumeinfluss der Kühlkurve und gleitender Erzeugervorlauf — nur mit Kälteschranke; ohne ihn NaN = fest.
            KuehlRaumeinfluss k2 = Kaelteschranke != null ? KuehlRaumeinfluss : null;
            k2?.StundeBeginnen();
            double vK = k2 != null ? k2.StartC(h) : double.NaN;
            Stundenangebot s = Angebotsfunktion.Angebot(h, v, _erzeuger, _speicher, vorrang);
            // AK3-K (4.2): das Kälteangebot am festen Kühlvorlauf — es hängt nicht am Durchlauf; KK3: am gleitenden
            // Erzeugervorlauf vK (NaN = fest, Zeichen für Zeichen wie ohne Vorlauf).
            Kaeltestundenangebot? kaelte = Kaelteschranke?.Angebot(h, vorrang, vK);
            // K3 (4.1): die nachgezogene Kälteschranke — ohne Naht gilt das Angebot am Stundenbeginn in jedem Durchlauf.
            Kaeltestundenangebot kaelteBasis = kaelte ?? default;
            if (kaelte != null && Kaeltekorrektur != null) kaelte = Kaeltekorrektur(h, kaelteBasis, null);

            // Der Verteilschlüssel (Festlegung 9): der unbegrenzte Probeschritt — nur, wenn es etwas zu verteilen gibt
            // (mehrere Gebäude oder Zonen). Er zählt nicht als Durchlauf.
            int zonenMax = 1;
            foreach (Kopplungsgebaeude g in _gebaeude) zonenMax = Math.Max(zonenMax, g.Stepper.Zonenzahl);
            double[] schluessel = null;
            IReadOnlyList<double>[] probeZonen = null;
            double[] kaelteSchluessel = null;
            IReadOnlyList<double>[] kaelteZonen = null;
            if (n > 1 || zonenMax > 1)
            {
                schluessel = new double[n];
                probeZonen = new IReadOnlyList<double>[n];
                if (kaelte != null)
                {
                    kaelteSchluessel = new double[n];
                    kaelteZonen = new IReadOnlyList<double>[n];
                }
                for (int i = 0; i < n; i++)
                {
                    Kopplungsgebaeude g = _gebaeude[i];
                    IReadOnlyList<Stundenergebnis> e = g.Stepper.Schritt(h);
                    schluessel[i] = HeizlastW(e, g.Faktor);
                    if (e.Count > 1) probeZonen[i] = e.Select(z => Math.Max(z.HeizleistungW, 0.0)).ToArray();
                    if (kaelte != null)
                    {
                        kaelteSchluessel[i] = KuehllastW(e, g.Faktor);
                        if (e.Count > 1) kaelteZonen[i] = e.Select(z => Math.Max(z.KuehlleistungW, 0.0)).ToArray();
                    }
                }
                if (n == 1)
                {
                    schluessel = null;
                    kaelteSchluessel = null;
                }
            }
            // Die Kälteschranke je Gebäude und Zone (Regel der Wärmeschranke, Schlüssel der unbegrenzte Kühlbedarf).
            Anlagenverfuegbarkeit[][] kaelteVerteilt = kaelte != null
                ? Stundenverteilung.Verteilen(kaelte.Value.AlsVerfuegbarkeit(), _ids, _alle, kaelteSchluessel, kaelteZonen)
                : null;

            // Fallwechsel (2.4): die Stützstelle des ersten Durchlaufs wird festgehalten, sobald sie pendelt — also zu
            // einer schon besuchten Stützstelle zurückkehrt; jeder Wechsel wird gezählt.
            IReadOnlyList<Stundenergebnis>[] vorher = null;
            int stelleEins = Stuetzstelle != null && !double.IsNaN(v) ? Stuetzstelle(v) : int.MinValue;
            int stelleVorher = stelleEins;
            var besucht = new HashSet<int>();
            if (stelleEins != int.MinValue) besucht.Add(stelleEins);
            double vEins = v;
            Stundenangebot sEins = s;
            bool festgehalten = false;
            double dTheta = double.NaN, dPhi = double.NaN, dV = double.NaN, dS = double.NaN;
            // K3 (4.4): die Kälteseite im selben Rahmen — Pendeln der nachgezogenen Kälteschranke, ihr Abbruchmaß.
            Kaeltestundenangebot? kaelteEins = kaelte;
            HashSet<long> kaelteBesucht = null;
            if (kaelte != null && Kaeltekorrektur != null)
                kaelteBesucht = new HashSet<long> { BitConverter.DoubleToInt64Bits(kaelte.Value.LeistungKw) };
            double dPhiK = double.NaN, dSK = double.NaN;
            double[] heizW = new double[n];
            // KK3: Pendelregel der Absenkung (Festlegung 8) — besuchte Absenkungen, Durchlauf 1 mit 0.
            double vKEins = vK;
            HashSet<long> k2Besucht = k2 != null ? new HashSet<long> { BitConverter.DoubleToInt64Bits(0.0) } : null;
            for (int k = 1; ; k++)
            {
                if (GrenzeErreicht(zonenMax, k))
                    throw Fehler(h, k - 1, v, s, dTheta, dPhi, dV, dS, zonenMax > 1 && k <= HOECHSTZAHL, kaelte, dPhiK, dSK);
                k2?.DurchlaufBeginnen();
                if ((kaelteBesucht != null || k2 != null) && k > 1)
                    kaelteVerteilt = Stundenverteilung.Verteilen(kaelte.Value.AlsVerfuegbarkeit(), _ids, _alle, kaelteSchluessel, kaelteZonen);

                // Durchlauf k: S_k verteilt, je Gebäude der Schritt mit der Schranke (wie AK2 mit Schranke = S_k).
                Anlagenverfuegbarkeit[][] verteilt = Stundenverteilung.Verteilen(s.AlsVerfuegbarkeit(), _ids, _alle, schluessel, probeZonen);
                var jetzt = new IReadOnlyList<Stundenergebnis>[n];
                for (int i = 0; i < n; i++)
                {
                    Kopplungsgebaeude g = _gebaeude[i];
                    Anlagenverfuegbarkeit[] anteil = verteilt[i];
                    Anlagenverfuegbarkeit[] kaelteAnteil = kaelteVerteilt?[i];
                    double faktor = g.Faktor;
                    int gi = i;
                    jetzt[i] = Kopie(g.Stepper.Schritt(h, (int zone, int stunde, in Stundenrand r0) =>
                    {
                        Anlagenverfuegbarkeit a = anteil[zone < anteil.Length ? zone : anteil.Length - 1];
                        // KK3: der Raumeinfluss der Kühlkurve senkt den Kühlvorlauf vor allen Schranken; ohne ihn derselbe Rand.
                        Stundenrand r = k2 == null ? r0 : k2.Absenken(gi, zone, r0);
                        Stundenrand mit = h2 == null
                            ? r.MitVerfuegbarkeit(a.LeistungKw * 1000.0 / faktor, a.Grund, a.VorlaufC)
                            : h2.Anheben(gi, zone, r, a.VorlaufC).MitVerfuegbarkeit(a.LeistungKw * 1000.0 / faktor, a.Grund, a.VorlaufC);
                        if (kaelteAnteil == null) return mit;
                        Anlagenverfuegbarkeit k = kaelteAnteil[zone < kaelteAnteil.Length ? zone : kaelteAnteil.Length - 1];
                        // KZ2: Am gleitenden Erzeuger fährt die Anlage den verlangten Vorlauf DIESES Durchlaufs — das Angebot
                        // des vorigen Durchlaufs (vor der neuen Absenkung) kappt die Zone im Kühlkreis nicht zurück; ohne
                        // Raumeinfluss oder außerhalb des Kühlkreises derselbe Rand.
                        double vAngebot = k.VorlaufC;
                        if (k2 != null && mit.KuehlVorlaufAmAngebot && mit.KuehlVorlaufC < vAngebot) vAngebot = mit.KuehlVorlaufC;
                        return mit.MitKaelteverfuegbarkeit(k.LeistungKw * 1000.0 / faktor, k.Grund, vAngebot);
                    }));
                    heizW[i] = HeizlastW(jetzt[i], faktor);
                }
                _anhebungLoesung = h2 != null ? h2.AnhebungMax() : 0.0;
                _absenkungLoesung = k2 != null ? k2.AbsenkungMax() : 0.0;

                int wechselnd = 0;
                if (vorher != null)
                {
                    FaelleZaehlen(vorher, jetzt);
                    wechselnd = kaelte != null
                        ? AbweichungMitKaelte(vorher, jetzt, out dTheta, out dPhi, out dPhiK)
                        : Abweichung(vorher, jetzt, out dTheta, out dPhi);
                }
                if (festgehalten)
                {
                    StundenFestgehalten++;
                    return KuehlAbschliessen(Abschliessen(h, k, jetzt, heizW, s, v, true, kaelte), k2, vK);
                }

                // K3 (4.1): die Kälteschranke des nächsten Durchlaufs aus der Lösung dieses Durchlaufs.
                Kaeltestundenangebot? kaelteNeu = kaelte;
                bool kaelteGleich = true;
                if (kaelteBesucht != null)
                {
                    kaelteNeu = Kaeltekorrektur(h, kaelteBasis, jetzt);
                    dSK = Math.Abs(kaelteNeu.Value.LeistungKw - kaelte.Value.LeistungKw) * 1000.0;
                    kaelteGleich = kaelteNeu.Value.LeistungKw.Equals(kaelte.Value.LeistungKw) && kaelteNeu.Value.Grund == kaelte.Value.Grund;
                    long bits = BitConverter.DoubleToInt64Bits(kaelteNeu.Value.LeistungKw);
                    if (!kaelteGleich && dSK > ABBRUCH_KAELTESCHRANKE_W && StuetzstelleHalten && kaelteBesucht.Contains(bits))
                    {
                        // Pendeln der Kälteschranke (Pendelregel 2.4 für beide Seiten): ein letzter Durchlauf am ersten.
                        FallWechsel++;
                        festgehalten = true;
                        vorher = jetzt;
                        v = vEins;
                        s = sEins;
                        kaelte = kaelteEins;
                        stelleVorher = stelleEins;
                        continue;
                    }
                    kaelteBesucht.Add(bits);
                }
                else if (kaelte != null) dSK = 0.0;

                // KK3 (Entwurf KK 2.2, 2.5): die Absenkung aus der Lösung DIESER Stunde (ΔK2) und die Kälteschranke am
                // kältesten verlangten Vorlauf des Durchlaufs; ohne Raumeinfluss 0 und unverändert.
                double dK2 = 0.0;
                double vKNeu = vK;
                if (k2 != null)
                {
                    dK2 = k2.Nachfuehren(jetzt);
                    if (!double.IsNaN(k2.VerlangterC)) vKNeu = k2.VerlangterC;
                    if (kaelteBesucht == null && !Gleich(vKNeu, vK))
                    {
                        kaelteNeu = Kaelteschranke.Angebot(h, vorrang, vKNeu);
                        dSK = Math.Abs(kaelteNeu.Value.LeistungKw - kaelte.Value.LeistungKw) * 1000.0;
                        kaelteGleich = kaelteNeu.Value.LeistungKw.Equals(kaelte.Value.LeistungKw) && kaelteNeu.Value.Grund == kaelte.Value.Grund;
                    }
                    long bits = BitConverter.DoubleToInt64Bits(k2.AbsenkungMax());
                    if (dK2 > ABBRUCH_VORLAUF_K && StuetzstelleHalten && k2Besucht.Contains(bits))
                    {
                        // Pendeln der Absenkung (Pendelregel 2.4 wie AK3): ein letzter Durchlauf am ersten.
                        k2.Festhalten();
                        FallWechsel++;
                        festgehalten = true;
                        vorher = jetzt;
                        v = vEins;
                        s = sEins;
                        kaelte = kaelteEins;
                        vK = vKEins;
                        stelleVorher = stelleEins;
                        continue;
                    }
                    k2Besucht.Add(bits);
                }

                (double vNeu, double rNeu) = Kreis(jetzt, Faktoren());
                // UB-E2: der Kreisrücklauf dieses Durchlaufs für das Angebot einer Wärmepumpe im Vorwärmbetrieb (B3) —
                // die Abhängigkeit von θ_R wirkt über die Schranke und läuft mit den Abbruchschwellen; ohne Bivalenz null.
                if (Kreisruecklauf != null && !double.IsNaN(rNeu)) Kreisruecklauf[h] = rNeu;
                if (double.IsNaN(vNeu)) vNeu = v;
                if (Vorlaufkorrektur != null) vNeu = Vorlaufkorrektur(h, vNeu, jetzt);
                // H2 (Q-AK3-2): die Anhebung aus der Lösung DIESER Stunde. Der Projektvorlauf des Angebots ist der der
                // Lösung (angehoben und gekappt, wie er gerechnet wurde); am Fixpunkt stimmen beide überein.
                double dH2 = h2 != null ? h2.Nachfuehren(jetzt) : 0.0;
                Stundenangebot sNeu = Angebotsfunktion.Angebot(h, vNeu, _erzeuger, _speicher, vorrang);
                if (Stuetzstelle != null && !double.IsNaN(vNeu))
                {
                    int stelle = Stuetzstelle(vNeu);
                    if (stelleVorher != int.MinValue && stelle != stelleVorher)
                    {
                        StuetzstellenWechsel++;
                        if (StuetzstelleHalten && besucht.Contains(stelle))
                        {
                            // Pendeln: ein letzter Durchlauf an Vorlauf und Angebot des ersten (deterministisch).
                            festgehalten = true;
                            vorher = jetzt;
                            v = vEins;
                            s = sEins;
                            stelleVorher = stelleEins;
                            continue;
                        }
                    }
                    besucht.Add(stelle);
                    stelleVorher = stelle;
                }

                // K3: in einer reinen Kühlstunde bleibt der Vorlauf unbekannt (NaN) — gleich unbekannt heißt „kein Wechsel“.
                dV = kaelte != null && Gleich(vNeu, v) ? 0.0 : Math.Abs(vNeu - v);
                dS = Math.Abs(sNeu.LeistungKw - s.LeistungKw) * 1000.0;
                // Gleiches Angebot ⇒ der nächste Schritt wäre derselbe (der Vorlauf wirkt nur über das Angebot).
                bool gleich = sNeu.LeistungKw.Equals(s.LeistungKw) && Gleich(sNeu.VorlaufC, s.VorlaufC) && sNeu.Grund == s.Grund
                              && dH2 == 0.0 && kaelteGleich && dK2 == 0.0;
                bool klein = dV <= ABBRUCH_VORLAUF_K && dS <= ABBRUCH_SCHRANKE_W && vorher != null && wechselnd == 0
                             && dH2 <= ABBRUCH_VORLAUF_K && (kaelteGleich || dSK <= ABBRUCH_KAELTESCHRANKE_W)
                             && dK2 <= ABBRUCH_VORLAUF_K;
                if (gleich || klein)
                    return KuehlAbschliessen(Abschliessen(h, k, jetzt, heizW, s, vNeu, false, kaelte), k2, vK);

                vorher = jetzt;
                v = vNeu;
                s = sNeu;
                kaelte = kaelteNeu;
                vK = vKNeu;
            }
        }

        /// <summary>Übernimmt die Stunde in allen Steppern (Reihen, Zähler, Kreise).</summary>
        internal void Festschreiben(int h)
        {
            foreach (Kopplungsgebaeude g in _gebaeude) g.Stepper.Festschreiben(h);
            Raumeinfluss?.Festschreiben(_letzteLoesung);
            if (Kaelteschranke != null) KuehlRaumeinfluss?.Festschreiben(_letzteLoesung);
        }

        /// <summary>
        /// KK3: trägt Erzeugervorlauf und Absenkung der Kälteseite in die Stunde ein — der kälteste verlangte Vorlauf der
        /// Lösung, ohne kühlendes Gebäude der Vorlauf der Schranke <paramref name="vK"/>. Ohne Raumeinfluss unverändert.
        /// </summary>
        private Kopplungsstunde KuehlAbschliessen(Kopplungsstunde stunde, KuehlRaumeinfluss k2, double vK)
        {
            if (k2 == null) return stunde;
            stunde.KuehlVorlaufC = double.IsNaN(k2.VerlangterC) ? vK : k2.VerlangterC;
            stunde.KuehlAbsenkungK = _absenkungLoesung;
            return stunde;
        }

        /// <summary>Die größte Absenkung (KK3), mit der die Lösung des letzten Durchlaufs gerechnet wurde [K].</summary>
        private double _absenkungLoesung;

        private IReadOnlyList<Stundenergebnis>[] _letzteLoesung;

        /// <summary>Die größte Anhebung, mit der die Lösung des letzten Durchlaufs gerechnet wurde [K].</summary>
        private double _anhebungLoesung;

        // ----------------------------------------------------------------------------------------------

        private double[] Faktoren() => _gebaeude.Select(g => g.Faktor).ToArray();

        /// <summary>
        /// Die Schutzgrenze der Durchläufe (2.4): Höchstzahl <see cref="HOECHSTZAHL"/>, im Mehrzonenfall dazu
        /// Zonen × Durchläufe ≤ <see cref="PRODUKT_MEHRZONEN"/>. true = Durchlauf <paramref name="durchlauf"/> ist
        /// nicht mehr erlaubt.
        /// </summary>
        internal static bool GrenzeErreicht(int zonen, int durchlauf)
            => durchlauf > HOECHSTZAHL || (zonen > 1 && zonen * durchlauf > PRODUKT_MEHRZONEN);

        /// <summary>Greift die Schranke in der Lösung — begrenzt sie eine Zone an der Leistung oder am Vorlauf?</summary>
        private static bool Begrenzt(IReadOnlyList<Stundenergebnis>[] e)
        {
            foreach (IReadOnlyList<Stundenergebnis> g in e)
                foreach (Stundenergebnis z in g)
                    if (z.Begrenzungsgrund == Begrenzungsgrund.Verfuegbarkeit || z.Begrenzungsgrund == Begrenzungsgrund.VorlaufAnlage)
                        return true;
            return false;
        }

        /// <summary>Greift die Kälteschranke in der Lösung — kappt sie die Kühlleistung einer Zone (AK3-K)?</summary>
        private static bool KaelteBegrenzt(IReadOnlyList<Stundenergebnis>[] e)
        {
            foreach (IReadOnlyList<Stundenergebnis> g in e)
                foreach (Stundenergebnis z in g)
                    if (z.KaelteverfuegbarkeitBegrenzt) return true;
            return false;
        }

        private Kopplungsstunde Abschliessen(int h, int durchlaeufe, IReadOnlyList<Stundenergebnis>[] loesung, double[] heizW,
                                             Stundenangebot s, double vorlaufKreisC, bool festgehalten,
                                             Kaeltestundenangebot? kaelte)
        {
            bool schranke = Begrenzt(loesung);
            bool kaelteSchranke = kaelte != null && KaelteBegrenzt(loesung);
            if (kaelteSchranke) StundenAnDerKaelteschranke++;
            if (kaelte != null && Umschaltung(loesung)) StundenUmschaltung++;
            Stunden++;
            DurchlaeufeVerteilung[durchlaeufe]++;
            if (durchlaeufe > DurchlaeufeMax) DurchlaeufeMax = durchlaeufe;
            if (schranke) StundenAnDerSchranke++;
            if (_speicher != null && _speicher.Vorhanden && !(s.SpeicherKw > 0.0)) StundenSpeicherLeer++;
            _letzteLoesung = loesung;
            // Die offenen Schritte der Stepper sind die Lösung; Vorlauf und Rücklauf aus ihnen (Festlegung 12).
            (double vor, double rueck) = Kreis(loesung, Faktoren());
            var stunde = new Kopplungsstunde
            {
                Durchlaeufe = durchlaeufe,
                HeizlastW = (double[])heizW.Clone(),
                Angebot = s,
                SchrankeGreift = schranke,
                VorlaufC = vor,
                RuecklaufC = rueck,
                VorlaufKreisC = vorlaufKreisC,
                Festgehalten = festgehalten,
                Loesung = loesung,
                AnhebungK = _anhebungLoesung,
                Kaelteangebot = kaelte,
                KaelteschrankeGreift = kaelteSchranke,
                Jahresstunde = h,
            };
            if (kaelte != null) LetzteStunde = stunde;
            return stunde;
        }

        /// <summary>Begrenzt eine Schranke eine Zone mit dem Anlagengrund Umschaltung (AK3-K 4.5)?</summary>
        private static bool Umschaltung(IReadOnlyList<Stundenergebnis>[] e)
        {
            foreach (IReadOnlyList<Stundenergebnis> g in e)
                foreach (Stundenergebnis z in g)
                {
                    if (z.Begrenzungsgrund == Begrenzungsgrund.Verfuegbarkeit && z.Verfuegbarkeitsgrund == Verfuegbarkeitsgrund.Umschaltung)
                        return true;
                    if (z.KaelteverfuegbarkeitBegrenzt && z.Kaelteverfuegbarkeitsgrund == Verfuegbarkeitsgrund.Umschaltung)
                        return true;
                }
            return false;
        }

        /// <summary>Heizleistung eines Gebäudes [W] im Maßstab des wirklichen Gebäudes: Σ max(Φ_h,z, 0) × Faktor.</summary>
        internal static double HeizlastW(IReadOnlyList<Stundenergebnis> e, double faktor)
        {
            if (e.Count == 1) return e[0].HeizleistungW * faktor;
            double s = 0.0;
            foreach (Stundenergebnis z in e) if (z.HeizleistungW > 0.0) s += z.HeizleistungW;
            return s * faktor;
        }

        /// <summary>Kühlleistung eines Gebäudes [W] im Maßstab des wirklichen Gebäudes: Σ max(Φ_c,z, 0) × Faktor (AK3-K).</summary>
        internal static double KuehllastW(IReadOnlyList<Stundenergebnis> e, double faktor)
        {
            if (e.Count == 1) return e[0].KuehlleistungW * faktor;
            double s = 0.0;
            foreach (Stundenergebnis z in e) if (z.KuehlleistungW > 0.0) s += z.KuehlleistungW;
            return s * faktor;
        }

        /// <summary>
        /// Bedarfsgewichteter Vorlauf und Rücklauf über alle Zonen der Gebäude, Gewicht die Heizleistung im Maßstab des
        /// wirklichen Gebäudes (Regel <see cref="Kreisprojekt"/>; ein Beitrag: seine Zahlen ohne Rundung).
        /// </summary>
        internal static (double VorlaufC, double RuecklaufC) Kreis(IReadOnlyList<IReadOnlyList<Stundenergebnis>> e, IReadOnlyList<double> faktor)
        {
            double gewicht = 0.0, gv = 0.0, gr = 0.0;
            int beitraege = 0;
            double einV = double.NaN, einR = double.NaN;
            for (int i = 0; i < e.Count; i++)
                foreach (Stundenergebnis z in e[i])
                {
                    double q = z.HeizleistungW * faktor[i];
                    if (!(q > 0.0) || double.IsNaN(z.VorlaufC)) continue;
                    beitraege++;
                    einV = z.VorlaufC;
                    einR = z.RuecklaufC;
                    gewicht += q;
                    gv += q * z.VorlaufC;
                    gr += q * z.RuecklaufC;
                }
            if (beitraege == 0) return (double.NaN, double.NaN);
            return beitraege == 1 ? (einV, einR) : (gv / gewicht, gr / gewicht);
        }

        private static IReadOnlyList<Stundenergebnis> Kopie(IReadOnlyList<Stundenergebnis> e) => e.ToArray();

        private static bool Gleich(double a, double b) => a.Equals(b);

        /// <summary>Zahl der Zonen, deren θ oder Φ über dem Abbruchmaß wechselt; dazu die größten Abweichungen.</summary>
        private static int Abweichung(IReadOnlyList<Stundenergebnis>[] a, IReadOnlyList<Stundenergebnis>[] b,
                                      out double dTheta, out double dPhi)
        {
            int zahl = 0;
            dTheta = 0.0;
            dPhi = 0.0;
            for (int i = 0; i < a.Length; i++)
                for (int z = 0; z < a[i].Count && z < b[i].Count; z++)
                {
                    double t = Math.Abs(a[i][z].ThetaAirMittel - b[i][z].ThetaAirMittel);
                    double p = Math.Abs(a[i][z].HeizleistungW - b[i][z].HeizleistungW);
                    if (t > dTheta) dTheta = t;
                    if (p > dPhi) dPhi = p;
                    if (t > ABBRUCH_THETA_K || p > ABBRUCH_PHI_W) zahl++;
                }
            return zahl;
        }

        /// <summary>
        /// <see cref="Abweichung"/> mit der Kälteseite (AK3-K 4.4, Festlegung 15): je Zone dazu die Kühlleistung über dem
        /// Abbruchmaß und der Wechsel des Kühlgrunds.
        /// </summary>
        private static int AbweichungMitKaelte(IReadOnlyList<Stundenergebnis>[] a, IReadOnlyList<Stundenergebnis>[] b,
                                               out double dTheta, out double dPhi, out double dPhiK)
        {
            int zahl = 0;
            dTheta = 0.0;
            dPhi = 0.0;
            dPhiK = 0.0;
            for (int i = 0; i < a.Length; i++)
                for (int z = 0; z < a[i].Count && z < b[i].Count; z++)
                {
                    double t = Math.Abs(a[i][z].ThetaAirMittel - b[i][z].ThetaAirMittel);
                    double p = Math.Abs(a[i][z].HeizleistungW - b[i][z].HeizleistungW);
                    double c = Math.Abs(a[i][z].KuehlleistungW - b[i][z].KuehlleistungW);
                    if (t > dTheta) dTheta = t;
                    if (p > dPhi) dPhi = p;
                    if (c > dPhiK) dPhiK = c;
                    if (t > ABBRUCH_THETA_K || p > ABBRUCH_PHI_W || c > ABBRUCH_PHI_W
                        || a[i][z].KuehlBegrenzungsgrund != b[i][z].KuehlBegrenzungsgrund) zahl++;
                }
            return zahl;
        }

        private void FaelleZaehlen(IReadOnlyList<Stundenergebnis>[] vorher, IReadOnlyList<Stundenergebnis>[] jetzt)
        {
            for (int i = 0; i < vorher.Length; i++)
                for (int z = 0; z < vorher[i].Count && z < jetzt[i].Count; z++)
                {
                    if (vorher[i][z].Begrenzungsgrund != jetzt[i][z].Begrenzungsgrund) FallWechsel++;
                    // AK3-K: mit Kälteschranke zählt der Wechsel des Kühlgrunds in dieselbe Statistik.
                    else if (Kaelteschranke != null && vorher[i][z].KuehlBegrenzungsgrund != jetzt[i][z].KuehlBegrenzungsgrund) FallWechsel++;
                }
        }

        /// <summary>Der benannte Fehler mit der größten Abweichung zwischen den letzten beiden Durchläufen.</summary>
        private AnlagenkopplungException Fehler(int h, int durchlaeufe, double v, Stundenangebot s,
                                                double dTheta, double dPhi, double dV, double dS, bool produkt,
                                                Kaeltestundenangebot? kaelte = null, double dPhiK = double.NaN,
                                                double dSK = double.NaN)
        {
            string gebaeude = string.Join(", ", _gebaeude.Select(g => g.Bezeichnung + " (" + g.Id.ToString(CultureInfo.InvariantCulture) + ")"));
            string beteiligte = string.Join(", ", _erzeuger.Select(e => e.Bezeichner)) + (_speicher != null && _speicher.Vorhanden ? ", Speicher" : "");
            string abw = string.Format(CultureInfo.InvariantCulture, "θ {0:0.###} K, Φ {1:0.###} W, Vorlauf {2:0.###} K, Schranke {3:0.###} W{4}",
                                       dTheta, dPhi, dV, dS,
                                       produkt ? " (Produkt Zonen × Durchläufe > " + PRODUKT_MEHRZONEN + ")" : "");
            string stand = string.Format(CultureInfo.InvariantCulture, "{0} Durchläufe, Vorlauf {1:0.##} °C, Schranke {2:0.###} kW",
                                         durchlaeufe, v, s.LeistungKw);
            if (kaelte != null)
            {
                // K3 (4.4): die Kälteseite im benannten Fehler — Kühlleistung und Kälteschranke.
                abw += string.Format(CultureInfo.InvariantCulture, ", Kühlleistung {0:0.###} W, Kälteschranke {1:0.###} W", dPhiK, dSK);
                stand += string.Format(CultureInfo.InvariantCulture, ", Kälteschranke {0:0.###} kW", kaelte.Value.LeistungKw);
            }
            return new AnlagenkopplungException(gebaeude, h, beteiligte, abw, stand);
        }
    }
}

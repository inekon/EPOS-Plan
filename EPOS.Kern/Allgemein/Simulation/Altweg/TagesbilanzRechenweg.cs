using System;

namespace WindowsFormsApplication1.Altweg
{
    /// <summary>
    /// <b>Der Tagesbilanz-Weg eines Gebäudes</b> — die Ausprägung des Bestandswegs hinter der
    /// Weiche (<see cref="IGebaeudeRechenweg"/>, Vertrag V16 des Systementwurfs).
    ///
    /// <para><b>Zeichen für Zeichen verschoben</b> (Stufe G1.0, Entscheid E20, ADR-006): Die
    /// Methoden <see cref="Berechnung_Gebaeude_Tageswerte"/>, <see cref="DBTagesVeteilung"/>
    /// und <see cref="FerienmaskeBilden"/> stehen hier Anweisung für Anweisung wie zuvor in
    /// <c>SimulationWaermebedarf</c>; der Rumpf von <see cref="Rechnen"/> ist der Rest des
    /// früheren Schleifenrumpfs (Tageswerte, Tagesverteilung, Abbruch bei fehlender
    /// Verteilung, Puffer nullen, Stundenverteilung). Die Physik steht in
    /// <see cref="TagesbilanzPhysik"/>, die Vortemperatur in <see cref="Tagesbilanzzustand"/>.
    /// Abgenommen ist die Verschiebung an einem byte-gleichen Referenzlauf gegen die Basis
    /// <c>2026-09-22_R11_Bestandsbefunde</c>.</para>
    ///
    /// <para><b>Eingefroren.</b> Der Weg bekommt keine neue Funktion, kennt das Modul
    /// <c>Gebaeude/</c> nicht und liefert keinen Kältebedarf; er fällt mit der Stufe GA
    /// (Löschliste im Umsetzungskonzept 6.1). Wächter: <c>ModultrennungswacheTests</c>.</para>
    ///
    /// <para><b>Die Skalierung steckt in der Tagesrechnung</b> (E8): Die Tagesheizlast wird
    /// mit <c>Z_AuswahlWohnflaeche / Nutzflaeche</c> gestreckt. Die Verbrauchs-Rückrechnung
    /// ruft diesen Weg deshalb — wie im Bestand — zweimal: einmal mit der Katalogfläche für
    /// den unskalierten Jahreswert, einmal mit der zurückgerechneten Fläche. Die Schleife
    /// führt die Fassade.</para>
    /// </summary>
    internal sealed class TagesbilanzRechenweg : IGebaeudeRechenweg
    {
        // Klimakalender, Altweg-Teil (F-Ü3): die isotropen Tagesmittel der Einstrahlung, die
        // Tagesmitteltemperatur und die Tagestypen. Sie werden beim Aufbau je Lauf übergeben
        // und dort in place gefüllt; nur dieser Weg liest sie.
        private readonly double[] Sol_N;
        private readonly double[] Sol_w;
        private readonly double[] Sol_O;
        private readonly double[] Sol_S;
        private readonly double[] A_Temp;
        private readonly int[] TagTyp_W;
        private readonly int[] TagTyp_NW;

        // Klimakalender, gemeinsamer Teil: die Wochenendtage - je Aufruf aus
        // KlimakalenderGemeinsam.
        private bool[] WE = new bool[365];

        internal double[] Solare_Gewinne = new double[365];
        private double[] SpezWaermeverluste = new double[365];
        private double[] Heizlast = new double[365];
        private double[] TagesVerteilung = new double[240];
        private bool[] F_Absenkung = new bool[365];

        // Die Vortemperatur des Tagesbilanz-Wegs — je Instanz, je Aufruf zurückgesetzt.
        private readonly Tagesbilanzzustand _tagesbilanz = new Tagesbilanzzustand();

        /// <summary>
        /// Merkplatz je Gebäude: die Jahresheizwärme der letzten Tagesrechnung (Summe der
        /// Tagesheizlasten). Er wächst mit dem Index - keine feste Obergrenze der Gebäudezahl.
        /// </summary>
        internal double[] HeizwaermebedarfGeb = new double[1];

        /// <param name="altweg">Der Altweg-Teil des Klimakalenders des Laufs; seine Felder
        /// werden über die Referenz gelesen.</param>
        internal TagesbilanzRechenweg(KlimakalenderAltweg altweg)
        {
            if (altweg == null) throw new ArgumentNullException(nameof(altweg));
            Sol_N = altweg.Sol_N;
            Sol_w = altweg.Sol_w;
            Sol_O = altweg.Sol_O;
            Sol_S = altweg.Sol_S;
            A_Temp = altweg.A_Temp;
            TagTyp_W = altweg.TagTyp_W;
            TagTyp_NW = altweg.TagTyp_NW;
        }

        /// <summary>
        /// Ein Lauf des Tagesbilanz-Wegs für ein Gebäude: Tageswerte, Tagesverteilung,
        /// Stundenverteilung in <paramref name="ziel"/> (Watt).
        ///
        /// <para><b>Zustand.</b> Jeder Aufruf beginnt mit frischer Vortemperatur. Im Bestand
        /// trug die Vortemperatur vom ersten in den zweiten Lauf der Verbrauchs-Rückrechnung
        /// hinüber; das erreichte nie ein Ergebnis, weil Tag 1 des Jahreslaufs sie auf den
        /// Nachtsollwert setzt und der Jahreslauf die Heizlasten des Vorlaufs überschreibt
        /// (Stufe GB, <see cref="Tagesbilanzzustand"/>).</para>
        /// </summary>
        /// <returns><c>false</c>, wenn zum Gebäudetyp keine Tagesverteilung hinterlegt ist
        /// (Warnung im Protokollkanal) — der Lauf bricht dann ab, wie im Bestand.</returns>
        public bool Rechnen(ProjektGebaeudeModel item, int index, double[] ziel,
                            KlimakalenderGemeinsam gemeinsam, out double verbrauchAltKwh)
        {
            if (gemeinsam == null) throw new ArgumentNullException(nameof(gemeinsam));
            WE = gemeinsam.WE;

            // Keine feste Obergrenze der Gebäudezahl: Der Merkplatz wächst mit dem Index.
            if (index >= HeizwaermebedarfGeb.Length)
                Array.Resize(ref HeizwaermebedarfGeb, index + 1);

            // Jeder Lauf beginnt mit frischem Zustand der Vortemperatur - das Ergebnis
            // hängt damit nicht an der Zeilenreihenfolge und nicht an einem früheren Lauf
            // im selben Prozess.
            _tagesbilanz.ResetState();

            // Tagesverteilung berechnen
            Berechnung_Gebaeude_Tageswerte(item, index);
            //                double VerbrauchAlt = (BrauchwasserGeb[index] + HeizwaermebedarfGeb[index]) / 1000;
            verbrauchAltKwh = HeizwaermebedarfGeb[index] / 1000;

            bool tagv_found = false;
            TagesVerteilung = DBTagesVeteilung(item.Typ, item.ID_Gebaeude, ref tagv_found);
            // PAKET 8 (Konzept 13.4): Warnung im Protokollkanal statt MessageBox. Der
            // ABBRUCH der Bedarfsrechnung bleibt unverändert (return an derselben
            // Stelle) — die im Konzept genannte Ersatzlösung „Standardprofil
            // verwenden“ wäre eine Rechenänderung und gehört nicht in ein
            // Infrastrukturpaket (siehe Paket-8-Protokoll, offene Punkte).
            if (!tagv_found)
            {
                SimulationProtokoll.Aktuell.Warnung(string.Format(
                    MyResource.Resource.SIMENG_TAGESVERTEILUNG_FEHLT, item.Typ));
                return false;
            }

            // V0-1: Einzelpuffer je Durchlauf nullen - StdWerte addiert auf.
            WPPlan.Core.BhkwPlan.VectorInit(ziel);

            // Stundenwerte Wärmebedarf je nach Gebäudetyp und Tagtyp aus Klimaregion
            if (item.Typ == "Wohngebaeude  VDI 2067")
            {
                //com.I_StdWerte(ref Waermebedarf_Gebaeude, TagTyp_W, TagesVerteilung, Heizlast);
                TagesbilanzPhysik.StdWerte(ziel, TagTyp_W, TagesVerteilung, Heizlast);
            }
            else
                //com.I_StdWerte(ref Waermebedarf_Gebaeude, TagTyp_NW, TagesVerteilung, Heizlast);
                TagesbilanzPhysik.StdWerte(ziel, TagTyp_NW, TagesVerteilung, Heizlast);

            return true;
        }

        private double[] DBTagesVeteilung(string TagV_Type, int ID_Gebaeude, ref bool tagv_found)
        {
            double[] tagv = new double[192];
            RecordSet rs = new RecordSet();

            try
            {
                // BEFUND B1 (S7): "Tab_DBTagV.ID" war der Tabellen-, nicht der Sichtname -
                // in SQLite "no such column". Der Ausfall war STILL (nur die Warnung
                // "keine Daten hinterlegt"), die Tagesverteilung blieb leer. Die Sortierung
                // steht im Rumpf der Sicht (ORDER BY Tab_DBTagVDaten.ID) und traegt auch
                // durch dieses aeussere WHERE - an der migrierten Datenbank nachgemessen.
                rs.Open("select * from Abfrage_Tagverteilung where Bezeichner='" + TagV_Type + "' and ID=" + ID_Gebaeude);
                int n = 0;
                while (rs.Next())
                {
                    double val = (double)rs.Read("Verteilung");
                    tagv[n] = (double)val;
                    n++;
                }
                if (n > 0) tagv_found = true;
                return tagv;
            }
            finally { rs.Close(); }
        }

        /// <summary>
        /// Bildet die Ferienmaske eines Gebäudes (<c>true</c> = der Tag ist abgesenkt) —
        /// für jeden gültigen Zeitraum Tag für Tag dieselbe Maske wie bisher; wo die Eingabe
        /// bisher still danebengriff, meldet sie eine benannte Warnung.
        ///
        /// <para><b>Die Lesart bleibt, wie sie ist</b> (eingefrorener Bestandsweg):
        /// Zeitraum 1 ist der Jahreswechselblock — Tag <c>Ferienbeginn_1</c> (ohne
        /// <c>−1</c>) bis Jahresende und Jahresanfang bis Tag <c>Ferienende_1</c>; er wirkt
        /// nur bei <c>0 &lt; Ferienbeginn_1 ≤ 365</c>, 0 und 366 heißen „aus". Die Zeiträume
        /// 2–4 laufen <c>Beginn − 1 … Ende</c> und wirken bei <c>Beginn &gt; 0</c> und
        /// <c>Ende &gt; 0</c>. Gerechnet wird nur, wenn der Fahrplan aktiv ist
        /// (<c>Ferien &gt; 0,9</c>).</para>
        ///
        /// <para><b>Was gemeldet wird.</b> (1) Ein Zeitraum, der über die Tage 1–365
        /// hinausreicht, griff bisher über das Feld <c>bool[365]</c> hinaus und brach den
        /// Lauf mit <c>IndexOutOfRangeException</c> ab — jetzt wird der Teil innerhalb des
        /// Jahres abgesenkt und gewarnt; ein Beginn des Zeitraums 1 außerhalb von 0…366
        /// wirkt wie bisher nicht, wird aber gemeldet. (2) Zeitraum 1 mit einem Beginn, der
        /// nicht nach dem Ende liegt, senkt als Jahreswechsel gelesen das GANZE Jahr ab —
        /// gerechnet wie bisher, aber gemeldet. (3) Ein Zeitraum 2–4, der keinen einzigen Tag
        /// ergibt (Beginn nach Ende, nur eine der beiden Angaben), blieb still wirkungslos —
        /// jetzt benannt. Ein Zeitraum, dessen beide Angaben 0 oder 366 sind, ist nicht
        /// belegt und bleibt still.</para>
        /// </summary>
        /// <param name="item">Das Gebäude; nur gelesen.</param>
        /// <param name="maske">Die 365 Tage; wird vollständig überschrieben.</param>
        /// <param name="warnen">Nimmt (Schlüssel, Text) einer Warnung; der Schlüssel ist je
        /// Gebäude, Zeitraum und Art eindeutig.</param>
        internal static void FerienmaskeBilden(ProjektGebaeudeModel item, bool[] maske, Action<string, string> warnen)
        {
            int tage = maske.Length;
            for (int Tag = 0; Tag < tage; Tag++)
            {
                maske[Tag] = false;
            }

            if (!(item.Ferien > 0.9)) return;

            string name = item.Gebaeudename ?? "";

            void Melden(int zeitraum, string art, string text) =>
                warnen("Ferienmaske|" + item.ID_Gebaeude + "|" + zeitraum + "|" + art, text);

            string Ausserhalb(int zeitraum, double beginn, double ende) =>
                string.Format(MyResource.Resource.SIMENG_FERIEN_AUSSERHALB_JAHR, name, zeitraum, beginn, ende);

            // --- Zeitraum 1: der Jahreswechselblock ---
            double b1 = item.Ferienbeginn_1;
            double e1 = item.Ferienende_1;
            if (b1 > 0 && b1 <= tage)
            {
                for (int Tag = (int)b1; Tag < tage; Tag++)
                {
                    maske[Tag] = true;
                }
                if (e1 > tage) Melden(1, "ausserhalb", Ausserhalb(1, b1, e1));
                for (int Tag = 0; Tag < (int)e1 && Tag < tage; Tag++)
                {
                    maske[Tag] = true;
                }
                if (e1 > 0 && b1 <= e1)
                {
                    Melden(1, "jahreswechsel", string.Format(
                        MyResource.Resource.SIMENG_FERIEN_JAHRESWECHSEL, name, b1, e1));
                }
            }
            else if (!(b1 == 0 || b1 == tage + 1))
            {
                // Negativ, jenseits von 366 oder keine Zahl: wirkt nicht - wie bisher -,
                // wird aber nicht mehr verschwiegen.
                Melden(1, "ausserhalb", Ausserhalb(1, b1, e1));
            }

            // --- Zeiträume 2 bis 4: Beginn - 1 ... Ende ---
            double[] beginne = { item.Ferienbeginn_2, item.Ferienbeginn_3, item.Ferienbeginn_4 };
            double[] enden = { item.Ferienende_2, item.Ferienende_3, item.Ferienende_4 };
            for (int k = 0; k < 3; k++)
            {
                int zeitraum = k + 2;
                double beginn = beginne[k];
                double ende = enden[k];

                if (beginn > 0 && ende > 0)
                {
                    int von = (int)beginn - 1;
                    bool ausserhalb = von < 0 || von >= tage || ende > tage;
                    if (ausserhalb) Melden(zeitraum, "ausserhalb", Ausserhalb(zeitraum, beginn, ende));
                    if (!(von < ende))
                    {
                        if (!ausserhalb)
                        {
                            Melden(zeitraum, "ohnewirkung", string.Format(
                                MyResource.Resource.SIMENG_FERIEN_OHNE_WIRKUNG, name, zeitraum, beginn, ende));
                        }
                        continue;
                    }
                    for (int Tag = Math.Max(von, 0); Tag < ende && Tag < tage; Tag++)
                    {
                        maske[Tag] = true;
                    }
                }
                else if (!IstUnbelegt(beginn) || !IstUnbelegt(ende))
                {
                    Melden(zeitraum, "ohnewirkung", string.Format(
                        MyResource.Resource.SIMENG_FERIEN_OHNE_WIRKUNG, name, zeitraum, beginn, ende));
                }
            }

            bool IstUnbelegt(double tag) => tag == 0 || tag == tage + 1;
        }

        private void Berechnung_Gebaeude_Tageswerte(ProjektGebaeudeModel item, int GebaeudeNr)
        {
            int WE_Absenkung = 0;
            int Ferien_Absenkung = 0;

            if (item.Raumsolltemperatur_Ferien < 1)
            {
                item.Ferien = 0; // Ferienabsenkung
            }

            // Die Ferienmaske mit benannten Warnungen statt stiller Fehlgriffe; je Gebäude,
            // Zeitraum und Art nur EINMAL je Lauf (die Verbrauchsrückrechnung ruft diese
            // Methode je Gebäude zweimal).
            FerienmaskeBilden(item, F_Absenkung,
                (schluessel, text) => SimulationProtokoll.Aktuell.WarnungEinmal(schluessel, text));

            // ANWENDERENTSCHEID W8-O-5d-Q2 (07.09.2026): "keine Treue zur alten DLL".
            // Die drei Physik-Funktionen des BHKW-Plan-Ports gaben bis hierher int zurueck
            // (Borland _ftol, Abschneiden Richtung Null); seither geben sie double zurueck.
            // Zwei Stellen ziehen damit mit:
            //
            //   * Die Division "/ 100" hinter SpezWaermeverlusteC war GANZZAHLIG, solange
            //     die Funktion int lieferte - also ein zweites Abschneiden hinter dem
            //     ersten. Sie steht jetzt als "/ 100.0" da und ist eine double-Division.
            //   * Die Division hinter SolareGewinneC war schon immer eine double-Division;
            //     "(double)100" ist nur noch "100.0" geschrieben - derselbe Wert.
            //
            // Der Faktor 100 selbst BLEIBT: Er gehoert zur Schnittstelle der DLL-Funktion
            // (sie liefert das Hundertfache), nicht zur Physik.
            for (int Tag = 350; Tag < 365; Tag++)
            {
                /*
                Solare_Gewinne[Tag] = com.I_SolareGewinneC(Sol_N[Tag], (double)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (double)item.Fensterflaeche_OstWest, Sol_S[Tag], (double)item.Fensterflaeche_Sued,
                        (double)item.Fensterdurchlassgrad) / (double)100;
                */
                Solare_Gewinne[Tag] = TagesbilanzPhysik.SolareGewinneC(Sol_N[Tag], (double)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (double)item.Fensterflaeche_OstWest, Sol_S[Tag], (double)item.Fensterflaeche_Sued,
                        (double)item.Fensterdurchlassgrad) / 100.0;

                /*
                SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC((double)item.k_Wert_Außenwand, (double)item.Flaeche_Außenwand,
                        (double)item.k_Wert_Fenster, (double)item.gesamte_Fensterflaeche, (double)item.k_Wert_Dachflaeche,
                        (double)item.Dachflaeche, (double)item.k_Wert_Grundflaeche, (double)item.Grundflaeche,
                        (double)item.k_Wert_Sonstiges, (double)item.Sonstige_Flaechen, (double)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                        (double)item.Abmessung_Anschluß_Fenster_Wand, (double)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (double)item.Abmessung_Anschluß_Wand_Dach,
                        (double)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (double)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (double)item.Nutzflaeche,
                        (double)item.Raumhoehe, (double)item.Luftwechselrate) / 100;
                */
                SpezWaermeverluste[Tag] = TagesbilanzPhysik.SpezWaermeverlusteC((double)item.k_Wert_Außenwand, (double)item.Flaeche_Außenwand,
                       (double)item.k_Wert_Fenster, (double)item.gesamte_Fensterflaeche, (double)item.k_Wert_Dachflaeche,
                       (double)item.Dachflaeche, (double)item.k_Wert_Grundflaeche, (double)item.Grundflaeche,
                       (double)item.k_Wert_Sonstiges, (double)item.Sonstige_Flaechen, (double)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                       (double)item.Abmessung_Anschluß_Fenster_Wand, (double)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (double)item.Abmessung_Anschluß_Wand_Dach,
                       (double)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (double)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (double)item.Nutzflaeche,
                       (double)item.Raumhoehe, (double)item.Luftwechselrate) / 100.0;

                WE_Absenkung = 0;
                if ((double)item.Raumsolltemperatur_Wochenende > 5)
                {
                    if (WE[Tag]) WE_Absenkung = 1; else WE_Absenkung = 0;
                }
                if (F_Absenkung[Tag]) Ferien_Absenkung = 1; else Ferien_Absenkung = 0;
                /*
                Heizlast[Tag] = com.I_TaeglHeizlastWG(Tag + 1,
                        WE_Absenkung,
                        (double)item.Raumsolltemperatur_Wochenende,
                        Ferien_Absenkung,
                        (double)item.Raumsolltemperatur_Ferien,
                        (double)item.Raumsolltemperatur_Tag,
                        (double)item.Raumsolltemperatur_Nachtabsenkung,
                        (double)item.Interne_Waermegewinne,
                        (double)Solare_Gewinne[Tag],
                        (double)SpezWaermeverluste[Tag],
                        (double)item.Bauweise,
                        (double)A_Temp[Tag],
                        (double)item.Maximaleraumtemperatur,
                        (double)item.Z_AuswahlWohnflaeche,
                        (double)item.Nutzflaeche);
                */
                Heizlast[Tag] = TagesbilanzPhysik.TaeglHeizlastWG(_tagesbilanz, Tag + 1,
                        WE_Absenkung,
                        (double)item.Raumsolltemperatur_Wochenende,
                        Ferien_Absenkung,
                        (double)item.Raumsolltemperatur_Ferien,
                        (double)item.Raumsolltemperatur_Tag,
                        (double)item.Raumsolltemperatur_Nachtabsenkung,
                        (double)item.Interne_Waermegewinne,
                        (double)Solare_Gewinne[Tag],
                        (double)SpezWaermeverluste[Tag],
                        (double)item.Bauweise,
                        (double)A_Temp[Tag],
                        (double)item.Maximaleraumtemperatur,
                        (double)item.Z_AuswahlWohnflaeche,
                        (double)item.Nutzflaeche);
            }

            HeizwaermebedarfGeb[GebaeudeNr] = 0;

            for (int Tag = 0; Tag < 365; Tag++)
            {
                /*
                Solare_Gewinne[Tag] = com.I_SolareGewinneC(Sol_N[Tag], (double)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                        (double)item.Fensterflaeche_OstWest, Sol_S[Tag], (double)item.Fensterflaeche_Sued,
                        (double)item.Fensterdurchlassgrad) / 100;
                */
                Solare_Gewinne[Tag] = TagesbilanzPhysik.SolareGewinneC(Sol_N[Tag], (double)item.Fensterflaeche_Nord, Sol_w[Tag], Sol_O[Tag],
                    (double)item.Fensterflaeche_OstWest, Sol_S[Tag], (double)item.Fensterflaeche_Sued,
                    (double)item.Fensterdurchlassgrad) / 100.0;
                /*
                SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC((double)item.k_Wert_Außenwand, (double)item.Flaeche_Außenwand,
                        (double)item.k_Wert_Fenster, (double)item.gesamte_Fensterflaeche, (double)item.k_Wert_Dachflaeche,
                        (double)item.Dachflaeche, (double)item.k_Wert_Grundflaeche, (double)item.Grundflaeche,
                        (double)item.k_Wert_Sonstiges, (double)item.Sonstige_Flaechen, (double)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                        (double)item.Abmessung_Anschluß_Fenster_Wand, (double)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (double)item.Abmessung_Anschluß_Wand_Dach,
                        (double)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (double)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (double)item.Nutzflaeche,
                        (double)item.Raumhoehe, (double)item.Luftwechselrate) / 100;
                */
                SpezWaermeverluste[Tag] = TagesbilanzPhysik.SpezWaermeverlusteC((double)item.k_Wert_Außenwand, (double)item.Flaeche_Außenwand,
                     (double)item.k_Wert_Fenster, (double)item.gesamte_Fensterflaeche, (double)item.k_Wert_Dachflaeche,
                     (double)item.Dachflaeche, (double)item.k_Wert_Grundflaeche, (double)item.Grundflaeche,
                     (double)item.k_Wert_Sonstiges, (double)item.Sonstige_Flaechen, (double)item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand,
                     (double)item.Abmessung_Anschluß_Fenster_Wand, (double)item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach, (double)item.Abmessung_Anschluß_Wand_Dach,
                     (double)item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke, (double)item.Abmessung_Anschluß_Außenwand_Kellerdecke, A_Temp[Tag], (double)item.Nutzflaeche,
                     (double)item.Raumhoehe, (double)item.Luftwechselrate) / 100.0;

                WE_Absenkung = 0;
                if ((double)item.Raumsolltemperatur_Wochenende > 5)
                {
                    if (WE[Tag]) WE_Absenkung = 1; else WE_Absenkung = 0;
                }
                // Die Ferienabsenkung je Tag nachführen wie im Vorlauf. Bis hierher fehlte
                // diese Zeile: Ferien_Absenkung behielt den Wert des letzten Vorlauftags
                // (Tag 365) und galt damit für alle 365 Tage des Jahres oder für keinen
                // (Befund X 3.4 Punkt 8).
                if (F_Absenkung[Tag]) Ferien_Absenkung = 1; else Ferien_Absenkung = 0;

                /*
                Heizlast[Tag] = (double)com.I_TaeglHeizlastWG(
                    Tag+1,
                    WE_Absenkung,
                    (double)item.Raumsolltemperatur_Wochenende,
                    Ferien_Absenkung,
                    (double)item.Raumsolltemperatur_Ferien,
                    (double)item.Raumsolltemperatur_Tag,
                    (double)item.Raumsolltemperatur_Nachtabsenkung,
                    (double)item.Interne_Waermegewinne,
                    (double)Solare_Gewinne[Tag],
                    (double)SpezWaermeverluste[Tag],
                    (double)item.Bauweise,
                    (double)A_Temp[Tag],
                    (double)item.Maximaleraumtemperatur,
                    (double)item.Z_AuswahlWohnflaeche,
                    (double)item.Nutzflaeche);
                */
                Heizlast[Tag] = TagesbilanzPhysik.TaeglHeizlastWG(_tagesbilanz, Tag + 1,
                      WE_Absenkung,
                      (double)item.Raumsolltemperatur_Wochenende,
                      Ferien_Absenkung,
                      (double)item.Raumsolltemperatur_Ferien,
                      (double)item.Raumsolltemperatur_Tag,
                      (double)item.Raumsolltemperatur_Nachtabsenkung,
                      (double)item.Interne_Waermegewinne,
                      (double)Solare_Gewinne[Tag],
                      (double)SpezWaermeverluste[Tag],
                      (double)item.Bauweise,
                      (double)A_Temp[Tag],
                      (double)item.Maximaleraumtemperatur,
                      (double)item.Z_AuswahlWohnflaeche,
                      (double)item.Nutzflaeche);

                HeizwaermebedarfGeb[GebaeudeNr] = HeizwaermebedarfGeb[GebaeudeNr] + Heizlast[Tag];
            }

        }
    }
}

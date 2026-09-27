using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Bestandsfelder, aus denen die Vorgabe-Matrix ihre Zellen nimmt</b> (Konzept
    /// Konditionierungsprofile 3.3). Ein reiner Werteträger <b>ohne Datenbank</b>: Der Lauf füllt ihn
    /// aus dem aufgelösten Gebäude- bzw. Zonenstand, der Controller aus der Zeile — so liest die
    /// Matrix im Lauf und im Dialog dieselben Felder, und beide sind ohne Datei prüfbar.
    ///
    /// <para><b>Die drei Einzelangaben neben der Matrix</b> (Konzept 3.3) stehen hier mit: die
    /// Ferienzeiträume samt Merker (sie gelten für alle Spalten), die Maximalraumtemperatur als
    /// Grenze der Überhitzungsstunden (F21) und das Sollwert-Zeitprogramm der Anlagenkopplung.</para>
    /// </summary>
    public sealed class Matrixeingang
    {
        /// <summary>Die Zahl der Ferienzeiträume am Gebäude (<c>Ferienbeginn/-ende_1…4</c>).</summary>
        public const int FERIENZEITRAEUME = 4;

        /// <summary>Der Nennwert eines Menschen [W], sensibel, je zur Hälfte konvektiv und radiativ (EPOS-Vorgabe, Konzept 3.1).</summary>
        public const double PERSON_W = 70.0;

        /// <summary><c>Raumsolltemperatur_Tag</c> [°C].</summary>
        public double? SollTag { get; set; }

        /// <summary><c>Raumsolltemperatur_Nachtabsenkung</c> [°C].</summary>
        public double? SollNacht { get; set; }

        /// <summary><c>Raumsolltemperatur_Wochenende</c> [°C] — <b>absolut</b>, wirksam nur über 5 °C (Konzept 3.3).</summary>
        public double? SollWochenende { get; set; }

        /// <summary><c>Raumsolltemperatur_Ferien</c> [°C] — <b>absolut</b>, wirksam ab 1 °C und nur mit <see cref="Ferienmerker"/>.</summary>
        public double? SollFerien { get; set; }

        /// <summary><c>Nachtabsenkung_Beginn</c> [Uhr 0 … 23]; leer heißt zusammen mit <see cref="NachtEnde"/> 22–6 Uhr.</summary>
        public int? NachtBeginn { get; set; }

        /// <summary><c>Nachtabsenkung_Ende</c> [Uhr 0 … 23], ausschließlich.</summary>
        public int? NachtEnde { get; set; }

        /// <summary>Der Merker <c>Ferien</c> — der VDI-Weg verlangt ihn über 0,9 (Konzept 3.3).</summary>
        public double Ferienmerker { get; set; }

        /// <summary>Der Merker <c>Wochenende</c>; die Rechnung liest ihn nicht, nur den Wert (Konzept 3.3).</summary>
        public double Wochenendmerker { get; set; }

        /// <summary><c>Ferienbeginn_1…4</c> als Jahrestag; 0 oder 366 an einer Grenze heißt „aus".</summary>
        public double[] Ferienbeginn { get; } = new double[FERIENZEITRAEUME];

        /// <summary><c>Ferienende_1…4</c> als Jahrestag; Beginn nach Ende heißt über den Jahreswechsel.</summary>
        public double[] Ferienende { get; } = new double[FERIENZEITRAEUME];

        /// <summary><c>Sollwertprofil</c> — das 168-Werte-Zeitprogramm der Anlagenkopplung (AK1), sonst <c>null</c>.</summary>
        public string Sollwertprofil { get; set; }

        /// <summary>Wirkt die Anlagenkopplung, so dass <see cref="Sollwertprofil"/> Tag, Nacht und Wochenende ersetzt?</summary>
        public bool KopplungWirksam { get; set; }

        /// <summary><c>Kuehl_Sollwert</c> [°C].</summary>
        public double? KuehlSollwert { get; set; }

        /// <summary><c>Kuehl_Sollwert_Nacht</c> [°C] — die Zelle Kühlen/Nacht (P13 (a)).</summary>
        public double? KuehlSollwertNacht { get; set; }

        /// <summary>Wirkt die Kühlung (Projektschalter, <c>Kuehlung_Aktiv</c> und gesetzter Sollwert, E32)?</summary>
        public bool KuehlungWirksam { get; set; }

        /// <summary><c>Luftwechsel_Infiltration</c> [1/h] — der Nennwert der Lüftungsspalte, konstant darunter (F15).</summary>
        public double? LuftwechselInfiltration { get; set; }

        /// <summary><c>Luftwechsel_Nutzer</c> [1/h] — der Tagwert der Lüftungsspalte, absolut.</summary>
        public double? LuftwechselNutzer { get; set; }

        /// <summary>
        /// <c>Luftwechselrate</c> [1/h] — die <b>Gesamtangabe</b>. Stammt der Luftwechsel aus ihr,
        /// verlangt jede Lüftungsvorgabe die getrennte Angabe (F15): Der Generator lehnt dann
        /// benannt ab, statt eine Infiltration zu erfinden.
        /// </summary>
        public double? Luftwechselrate { get; set; }

        /// <summary>Woher der wirksame Luftwechsel heute kommt — ist es die Gesamtangabe, greift die Regel aus F15.</summary>
        public bool LuftwechselAusGesamtangabe { get; set; }

        /// <summary><c>Interne_Waermegewinne</c> [W] — der Nennwert der Gerätespalte (nach P1 Gesamtwert minus Personenmittel).</summary>
        public double? InterneWaermegewinne { get; set; }

        /// <summary><c>Bewohner</c> — schlägt die Personenzahl vor (Nennwert = Zahl × <see cref="PERSON_W"/>).</summary>
        public double? Bewohner { get; set; }

        /// <summary><c>Maximaleraumtemperatur</c> [°C] — die Grenze der Überhitzungsstunden, kein Kühlsollwert (F21).</summary>
        public double? Maximaleraumtemperatur { get; set; }

        /// <summary>
        /// <b>Die Nachtzeit dieses Eingangs</b> — <see cref="Nachtzeit.Vorgabe"/> (22–6 Uhr), wenn
        /// beide Spalten leer sind; sonst die gesetzten Stunden. Die Prüfung ist dieselbe wie im
        /// Lauf (<see cref="Nachtzeit.Pruefen"/>); der Generator zieht sie <b>streng</b>, statt wie
        /// <c>Bestandswoche</c> auf die Vorgabe zurückzufallen (Konzept 3.3).
        /// </summary>
        public Nachtzeit Nachtzeit() => WindowsFormsApplication1.Nachtzeit.Aus(NachtBeginn, NachtEnde);

        /// <summary>Der Befund der Nachtzeit, ohne Ausnahme — für die benannte Ablehnung.</summary>
        public NachtzeitBefund Nachtzeitbefund() => WindowsFormsApplication1.Nachtzeit.Pruefen(NachtBeginn, NachtEnde);

        /// <summary>
        /// <b>Die Kaskade der Einzelangaben</b>: Was dieser Eingang (der der Zone) nicht führt, kommt
        /// vom Gebäude. Die <b>Ferienzeiträume und die Merker kommen immer vom Gebäude</b> — sie
        /// gelten für alle Spalten (Konzept 3.4), und <c>Tab_Zone</c> führt sie nicht.
        /// </summary>
        public Matrixeingang Erben(Matrixeingang gebaeude)
        {
            if (gebaeude == null) return this;
            var e = new Matrixeingang
            {
                SollTag = SollTag ?? gebaeude.SollTag,
                SollNacht = SollNacht ?? gebaeude.SollNacht,
                SollWochenende = SollWochenende ?? gebaeude.SollWochenende,
                SollFerien = SollFerien ?? gebaeude.SollFerien,
                NachtBeginn = NachtBeginn ?? gebaeude.NachtBeginn,
                NachtEnde = NachtEnde ?? gebaeude.NachtEnde,
                Ferienmerker = gebaeude.Ferienmerker,
                Wochenendmerker = gebaeude.Wochenendmerker,
                Sollwertprofil = Sollwertprofil ?? gebaeude.Sollwertprofil,
                KopplungWirksam = gebaeude.KopplungWirksam,
                KuehlSollwert = KuehlSollwert ?? gebaeude.KuehlSollwert,
                KuehlSollwertNacht = KuehlSollwertNacht ?? gebaeude.KuehlSollwertNacht,
                KuehlungWirksam = KuehlungWirksam || gebaeude.KuehlungWirksam,
                LuftwechselInfiltration = LuftwechselInfiltration ?? gebaeude.LuftwechselInfiltration,
                LuftwechselNutzer = LuftwechselNutzer ?? gebaeude.LuftwechselNutzer,
                Luftwechselrate = Luftwechselrate ?? gebaeude.Luftwechselrate,
                LuftwechselAusGesamtangabe = LuftwechselAusGesamtangabe || gebaeude.LuftwechselAusGesamtangabe,
                InterneWaermegewinne = InterneWaermegewinne ?? gebaeude.InterneWaermegewinne,
                Bewohner = Bewohner ?? gebaeude.Bewohner,
                Maximaleraumtemperatur = Maximaleraumtemperatur ?? gebaeude.Maximaleraumtemperatur,
            };
            Array.Copy(gebaeude.Ferienbeginn, e.Ferienbeginn, FERIENZEITRAEUME);
            Array.Copy(gebaeude.Ferienende, e.Ferienende, FERIENZEITRAEUME);
            return e;
        }
    }
}

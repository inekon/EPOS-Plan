namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Bedarfsnaht der Kaskadenstunde</b> (AK3-W2; Entwurf AK3 2.1 Schritt 4): liefert am Kopf
    /// von <see cref="Kaskadenschleife.StundeRechnen"/> den Bedarf der Stunde je Wärmekanal.
    ///
    /// <para>Im heutigen Lauf (AK1, AK2 und ohne Kopplung) liefert die Vorgabe
    /// <see cref="VektorStundenbedarf"/> den Wert des Jahresvektors — bitgleich zur früheren Zeile
    /// der Stundenschleife. Mit AK3 (W3) rechnet die Naht die gekoppelte Stunde (Gebäude-Stepper,
    /// Angebot der Kaskade) und gibt den Heizbedarf dieser Stunde zurück; Brauchwasser und Prozess
    /// bleiben Vorab-Vektoren (feste Last).</para>
    ///
    /// <para><b>Vertrag:</b> Die Naht beschreibt in <c>rest</c> GENAU die Wärmekanäle
    /// (<see cref="Kanal.KANAELE_WAERME"/>); der Kühlkanal bleibt unberührt (Kühlkonzept 4.2). Sie wird
    /// je Stunde genau einmal gerufen, vor jeder Phase der Stunde. Was die Naht liefert, schreibt die
    /// Stunde nach Phase G als Restbedarf in den Kanalsatz zurück — der Kanalsatz ist danach die
    /// Eingangsgröße der Stufen hinter der Speicherstufe.</para>
    ///
    /// <para><b>Leser des Jahresvektors</b> (Festlegung 14, Inventar W2). Je Leser steht, wann er den
    /// Bedarfsvektor liest und welche Reihe er im AK3-Weg bekommt — „Pass 1" (die Summen des
    /// unbegrenzten Bedarfs vor der Schleife) oder „gekoppelt" (die Reihe nach der Schleife).</para>
    /// <list type="table">
    /// <listheader><term>Leser (Ort)</term><description>Zeitpunkt, Größe — AK3-Weg</description></listheader>
    /// <item><term>Netzverluste in „%" (<c>SimulationWaermebedarf.Waermebedarf_berechnen</c>)</term>
    ///   <description>vor der Schleife, Jahressumme <c>Waermebedarf_Gesamt</c> → stündlicher Verlust je Kanal — Pass 1</description></item>
    /// <item><term>Desinfektion (<c>SimulationControl.DesinfektionVorbereiten</c>)</term>
    ///   <description>vor der Schleife, Brauchwasserkanal und Desinfektionsreihe — Vorab-Vektor, unverändert (Brauchwasser ist feste Last)</description></item>
    /// <item><term>Warnung Kanal ohne Versorger (<c>BedarfskanalOhneVersorgerMelden</c>)</term>
    ///   <description>vor der Schleife, Jahressumme je Kanal — Pass 1 (nur Protokoll)</description></item>
    /// <item><term>Vektorstufen vor der Speicherstufe (<c>Simulation_SPK/Solarthermie/BHKW_Ctrl_Zweikanalig</c>)</term>
    ///   <description>vor der Schleife, ganzer Kanalsatz — im AK3-Weg Schleifenmitglieder (Festlegung 13, <see cref="SimulationControl.Ak3VektorstufenInSchleife"/>)</description></item>
    /// <item><term>Stufeneingang der Wärmepumpe (<c>SimulationWaermepumpe.Zweikanalig_Start</c>: <c>Waermebedarf_stuendlich = kanaele.Summe()</c>)</term>
    ///   <description>am Schleifenkopf, Summenvektor → <c>WaermebedarfGesamtKwh</c>, Ergebnis der WP — gekoppelt (in W3 aus den Werten der Naht nachführen)</description></item>
    /// <item><term>Stufeneingang Solar, Kessel, BHKW (<c>Stunde_Start(stunde, rest)</c>)</term>
    ///   <description>je Stunde aus <c>rest</c> — liest bereits die Naht</description></item>
    /// <item><term>Vektorstufen hinter der Speicherstufe</term>
    ///   <description>nach der Schleife, Restkanäle — gekoppelt (Rest der Stunden)</description></item>
    /// <item><term>Tagesbetriebsart der Kälte (<c>Kaeltekaskade.TagesbetriebsartBestimmen</c> über <c>KanaeleDrei().Heizung</c>)</term>
    ///   <description>nach der Wärmekaskade, Tagessummen des Projektbedarfs — Pass 1 (Festlegung 18, Kälte vorwärts)</description></item>
    /// <item><term>Heizkanal an Kühltagen (<c>HeizkanalAnKuehltagenMelden</c>)</term>
    ///   <description>nach der Kälte, <c>KanaeleDrei().Heizung</c> gegen den Rest — gekoppelt (nur Protokoll)</description></item>
    /// <item><term>Rest der Wärme (<c>Rest_Waermebedarf_stuendlich</c>, <c>WaermeUnterdeckungMelden</c>)</term>
    ///   <description>Startwert Klon von <c>Waermebedarf</c>, Unterdeckung gegen <c>Waermebedarf_Gesamt</c> — gekoppelt</description></item>
    /// <item><term>Kennzahlen und Ergebnis (<c>SimulationRunner</c>: <c>Waermebedarf_Gesamt</c>, <c>Waermebedarf_Max</c>, <c>BedarfJeKanal</c>, Deckungsanteile je Erzeuger, <c>DeckungJeKanal</c>)</term>
    ///   <description>nach dem Lauf, Jahres- und Kanalsummen — gekoppelt</description></item>
    /// <item><term>Monats- und Dauerlinienreihen (<c>Waermebedarf_sortiert</c>, <c>*_Monat</c>, Bericht)</term>
    ///   <description>in der Bedarfsrechnung gebildet — gekoppelt (in W3 nach der Schleife neu bilden)</description></item>
    /// <item><term>Heizkreis (<c>Heizkreis.VorlaufC</c>/<c>RuecklaufC</c> an WP, Solar, Kessel)</term>
    ///   <description>Vorab-Vektor je Stunde — im AK3-Weg je Stunde aus dem Stepper (W3)</description></item>
    /// </list>
    /// </summary>
    internal interface IStundenbedarf
    {
        /// <summary>
        /// Schreibt den Bedarf der Stunde <paramref name="stunde"/> je Wärmekanal [kWh] in
        /// <paramref name="rest"/> (indiziert nach <see cref="Kanal"/>).
        /// </summary>
        void BedarfDerStunde(int stunde, Kanalsatz kanaele, double[] rest);
    }

    /// <summary>
    /// Vorgabe der Bedarfsnaht: der Jahresvektor des Kanalsatzes, Stunde für Stunde — der heutige
    /// Lauf, Zeichen für Zeichen die frühere Zeile der Stundenschleife.
    /// </summary>
    internal sealed class VektorStundenbedarf : IStundenbedarf
    {
        /// <summary>Die eine, zustandsfreie Instanz.</summary>
        internal static readonly VektorStundenbedarf Instanz = new VektorStundenbedarf();

        private VektorStundenbedarf() { }

        /// <inheritdoc/>
        public void BedarfDerStunde(int stunde, Kanalsatz kanaele, double[] rest)
        {
            foreach (int k in Kanal.KANAELE_WAERME) rest[k] = kanaele.Bedarf[k][stunde];
        }
    }
}

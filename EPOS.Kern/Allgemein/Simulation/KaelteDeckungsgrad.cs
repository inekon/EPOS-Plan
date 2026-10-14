namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Deckungsgrad der Kälte mit dezentraler (Split-)Deckung</b> (K6b; Konzept Kältebedarf 4.3, Frage 1 des Plans K6) —
    /// die EINE Stelle, an der die zwei Zahlen gebildet werden; Reiter, Ring, Bericht und Kennzahlenkatalog rufen sie.
    ///
    /// <para><b>Hauptzahl</b> „Deckungsgrad Kälte“ = (Kältefolge + dezentral) / Gesamtbedarf; <b>darunter</b> „davon
    /// Kältefolge“ = Kältefolge / (Gesamtbedarf − dezentral). Die gespeicherten Größen (<c>Kaelte.DeckungsgradProzent</c>,
    /// <c>Deckung_Kanal</c> der Wärmepumpe) behalten ihre Bedeutung (Kältefolge / Gesamtbedarf); die zwei Zahlen hier
    /// entstehen nur mit Split-Zeile — ohne sie ist das Ergebnis <c>null</c>, und jede Anzeige bleibt, wie sie war.</para>
    ///
    /// <para>Der Entscheid kann noch fallen: Eine andere Regel wird allein in <see cref="Hauptzahl"/> und
    /// <see cref="DavonKaeltefolge"/> umgestellt.</para>
    /// </summary>
    public static class KaelteDeckungsgrad
    {
        /// <summary>
        /// Hauptzahl [%] = (Kältefolge + dezentral) / Gesamtbedarf; <c>null</c> ohne dezentrale Deckung oder ohne Bedarf.
        /// </summary>
        /// <param name="kaeltefolgeMwh">Die Deckung der Kältefolge [MWh/a]; 0 ohne Kälteerzeuger.</param>
        /// <param name="dezentralMwh">Die dezentral (Split) gedeckte Kälte [MWh/a].</param>
        /// <param name="gesamtMwh">Der Kältebedarf gesamt [MWh/a] — die Summe des Kühlkanals samt dezentraler Zeilen.</param>
        public static double? Hauptzahl(double kaeltefolgeMwh, double dezentralMwh, double gesamtMwh)
        {
            if (!(dezentralMwh > 0) || !(gesamtMwh > 0)) return null;
            return SimulationRunner.DeckungProzent(kaeltefolgeMwh + dezentralMwh, gesamtMwh);
        }

        /// <summary>
        /// „davon Kältefolge“ [%] = Kältefolge / (Gesamtbedarf − dezentral); <c>null</c> ohne dezentrale Deckung oder wenn
        /// kein zentral zu deckender Bedarf bleibt.
        /// </summary>
        public static double? DavonKaeltefolge(double kaeltefolgeMwh, double dezentralMwh, double gesamtMwh)
        {
            if (!(dezentralMwh > 0)) return null;
            double zentral = gesamtMwh - dezentralMwh;
            if (!(zentral > 0)) return null;
            return SimulationRunner.DeckungProzent(kaeltefolgeMwh, zentral);
        }
    }
}

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Herkunft des U-Werts eines Bauteils, wie das OK des Gebäudeeditors sie schriebe</b> (MA-6b) — der Lesewert des
    /// Hilfe-Assistenten im Bauteildialog. Dieselben Regeln wie der Schreibweg (<c>GebaeudeZonenCtrl.Schreiben</c>): ein neues
    /// Bauteil nach <c>GebaeudeZonenCtrl.UHerkunftNeu</c>, ein gespeichertes nach <c>GebaeudeZonenCtrl.UHerkunftNachPflege</c>.
    /// Rechnet nichts am Gebäude und schreibt nichts.
    /// </summary>
    public static class BauteilUHerkunftVorschau
    {
        /// <summary>
        /// Die Herkunft (Persistenzwert, <c>DbWerte.HERKUNFT_*</c>) oder <c>null</c>. Ein im Dialog übernommener Katalogaufbau
        /// (<paramref name="katalogaufbau"/>) wird beim OK zu einem neuen Projektaufbau und zählt deshalb als anderer Aufbau.
        /// </summary>
        /// <param name="gespeichert">Die gespeicherte Herkunft; bei einem neuen Bauteil die mitgebrachte (sonst <c>null</c>).</param>
        /// <param name="neu">Das Bauteil hat noch keine Zeile in der Datenbank.</param>
        /// <param name="gespeicherterAufbau">Der Aufbau beim Öffnen.</param>
        /// <param name="gespeicherterU">Der U-Wert beim Öffnen.</param>
        /// <param name="neuerAufbau">Der Aufbau des Arbeitsstands.</param>
        /// <param name="neuerU">Der U-Wert des Arbeitsstands.</param>
        /// <param name="katalogaufbau">Ein Aufbau aus dem Katalog ist übernommen.</param>
        public static string Herkunft(string gespeichert, bool neu, int? gespeicherterAufbau, double? gespeicherterU,
                                      int? neuerAufbau, double? neuerU, bool katalogaufbau)
        {
            if (neu)
                return GebaeudeZonenCtrl.UHerkunftNeu(new BauteilModel
                {
                    U_Herkunft = gespeichert,
                    ID_Aufbau = neuerAufbau ?? (katalogaufbau ? -1 : (int?)null),
                    U_Wert = neuerU
                });
            if (katalogaufbau) return DbWerte.HERKUNFT_KATALOG;
            return GebaeudeZonenCtrl.UHerkunftNachPflege(gespeichert, gespeicherterAufbau, gespeicherterU, neuerAufbau, neuerU);
        }
    }
}

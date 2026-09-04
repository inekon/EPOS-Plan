using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Gemeinsame Darstellungsregeln der Ergebnis-Ganglinien.
    ///
    /// <b>Warum es diese Klasse gibt.</b> Vier Ansichten zeigen dieselben Stundenvektoren
    /// als Jahresganglinie: die Wärmepumpen- und die Heizkessel-Seite der Detailansicht
    /// (<see cref="Form_Simulation_Detail"/>) sowie <c>NavigatorWaerme</c> und
    /// <c>NavigatorStrom</c>. Zwei Regeln müssen dort überall gleich gelten — die
    /// Dauerlinien-Sortierung des Umschalters „sortiert" und der Serientyp der
    /// Stapelserien. Beide lagen bis hierher als Kopie in jeder Ansicht; ein Fix an
    /// einer Stelle wirkte in den übrigen nicht (Muster N6 aus Paket 5: keine
    /// Logik-Kopien).
    ///
    /// <b>Seit iU9‑W11a stehen <c>Dauerlinie</c> und <c>Anzeigewerte</c> im Kern</b>
    /// (<see cref="Ganglinie"/>) — sie rechnen nur auf <c>float[]</c> und werden dort auch
    /// vom Renderer und der Razor-Ergebnisseite gebraucht. Was hier bleibt, ist die
    /// WinForms-Hälfte: <see cref="Stapeltyp"/> und <see cref="StapelEinstellen"/> arbeiten
    /// auf einer <c>Series</c>. Die beiden Weiterleitungen unten bleiben stehen, damit die
    /// 24 Aufrufstellen der Masken unverändert bleiben, bis W11b sie löscht.
    ///
    /// Reine Darstellung: hier wird nichts gerechnet, was in ein Ergebnis einginge, und
    /// keine Quellganglinie verändert — <see cref="Dauerlinie"/> arbeitet auf einer Kopie.
    /// </summary>
    internal static class GanglinienDarstellung
    {
        /// <summary>
        /// Jahresdauerlinie: eine Kopie des Vektors, absteigend sortiert.
        ///
        /// Sortiert wird JEDE SERIE FÜR SICH — die Stunde i der einen Serie hat danach
        /// mit der Stunde i der anderen nichts mehr zu tun. Die Kopie schützt den
        /// Originalvektor, mit dem CSV-Export, Skalierung und das Zurückschalten in die
        /// chronologische Darstellung weiterarbeiten.
        /// </summary>
        public static float[] Dauerlinie(float[] werte)
        {
            return Ganglinie.Dauerlinie(werte);
        }

        /// <summary>
        /// Werte in der aktuellen Darstellungsform. Ohne <paramref name="sortiert"/>
        /// kommt der ORIGINALVEKTOR zurück (keine Kopie) — das Zurückschalten stellt
        /// damit bitgleich denselben Kurvenverlauf her.
        /// </summary>
        public static float[] Anzeigewerte(float[] werte, bool sortiert)
        {
            return Ganglinie.Anzeigewerte(werte, sortiert);
        }

        /// <summary>
        /// Serientyp der Erzeuger-/Stapelserien.
        ///
        /// <b>Chronologisch: <c>StackedColumn</c>, nicht <c>StackedArea</c>.</b> Eine
        /// Fläche verbindet ihre Stützstellen mit einer Geraden. Läuft eine Anlage im
        /// ALTERNATIVbetrieb — je Stunde entweder Wärmepumpe oder Kessel —, hat der
        /// Kessel in den WP-Stunden den Wert 0; die Kesselfläche wird dann zwischen der
        /// kumulierten Oberkante der WP-Stunden und den Nullstunden aufgespannt und
        /// übermalt den WP-Anteil mit Dreiecken. Genau das war im Sichttest zu sehen
        /// („die Wärmepumpe wird in blau dargestellt"). Die Säulendarstellung zeichnet
        /// je Stunde einen eigenen Balken und interpoliert nicht — die Stundenwerte
        /// bleiben, was sie sind. Präzedenz im Bestand: <c>DashboardForm</c> stapelt
        /// seine Tagesbilanzen ebenfalls als <c>StackedColumn</c>.
        ///
        /// <b>Sortiert: <c>FastLine</c>.</b> In der Dauerlinie ist jede Serie für sich
        /// sortiert; eine Summe daraus wäre frei erfunden, deshalb wird dort nicht
        /// gestapelt.
        /// </summary>
        public static SeriesChartType Stapeltyp(bool sortiert)
        {
            return sortiert ? SeriesChartType.FastLine : SeriesChartType.StackedColumn;
        }

        /// <summary>
        /// Setzt Serientyp, Stapelgruppe und Balkenbreite einer Serie.
        ///
        /// <paramref name="stapelgruppe"/> trennt mehrere Stapel in EINEM Diagramm (z. B.
        /// „Bedarf" und „Produktion"); ohne sie wirft MS-Chart alle gestapelten Serien in
        /// einen gemeinsamen Stapel.
        ///
        /// <c>PointWidth = 1</c> nimmt den Standardabstand zwischen den Säulen heraus.
        /// Bei 8760 Säulen ist eine Säule weit schmaler als ein Bildschirmpunkt — mit
        /// Abstand sähe die Fläche ausgedünnt aus statt geschlossen.
        /// </summary>
        public static void StapelEinstellen(Series s, SeriesChartType typ, string stapelgruppe)
        {
            if (s == null) return;

            s.ChartType = typ;
            if (!string.IsNullOrEmpty(stapelgruppe)) s["StackedGroupName"] = stapelgruppe;

            if (typ == SeriesChartType.StackedColumn || typ == SeriesChartType.StackedColumn100)
                s["PointWidth"] = "1";
        }
    }
}

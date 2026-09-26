namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Regeln des Gebäudeexports</b> (Gebäudesimulation Stufe G7a; Datenaustauschkonzept
    /// Kapitel 5) — die EINE Stelle, an der Oberfläche und Hüllen lesen, ob der Export angeboten wird.
    /// Öffentlich nach dem Muster von <see cref="GebaeudeZonenregeln"/>, damit die Oberfläche sie
    /// ohne Freigabe der Interna liest.
    ///
    /// <para><b>Der Freigabeschalter</b> (<see cref="GbxmlExportFreigegeben"/>; Anwenderentscheid F1 vom
    /// 26.09.2026): G7a wird vor G7b gebaut, aber erst mit G7b ausgeliefert (D2). Im
    /// Entwicklungsstand steht der Schalter auf „an"; <b>vor jeder Auslieferung wird er
    /// ausgeschaltet</b> — dann gibt es keinen Exportknopf. Er ist bewusst kein Profilfeld und keine
    /// Einstellung: Er ändert sich nur mit dem Quelltext.</para>
    /// </summary>
    public static class GebaeudeExportRegeln
    {
        /// <summary>Die Stellung des Freigabeschalters in diesem Stand (F1: an im Entwicklungsstand, aus vor jeder Auslieferung).</summary>
        private const bool FREIGABE_GBXML_EXPORT = true;

        /// <summary>Wird der gbXML-Export angeboten? Der Freigabeschalter (F1) — aus, gibt es keinen Exportknopf.</summary>
        public static bool GbxmlExportFreigegeben => FREIGABE_GBXML_EXPORT;
    }
}

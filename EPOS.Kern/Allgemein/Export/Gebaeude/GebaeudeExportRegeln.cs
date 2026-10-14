namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Regeln des Gebäudeexports</b> (Gebäudesimulation Stufe G7a; Datenaustauschkonzept
    /// Kapitel 5) — die EINE Stelle, an der Oberfläche und Hüllen lesen, ob der Export angeboten wird.
    /// Öffentlich nach dem Muster von <see cref="GebaeudeZonenregeln"/>, damit die Oberfläche sie
    /// ohne Freigabe der Interna liest.
    ///
    /// <para><b>Der Freigabeschalter</b> (<see cref="GbxmlExportFreigegeben"/>; Anwenderentscheid F1, D2):
    /// Der gbXML-Export wird mit Stufe G7b (Stufe 2: Raumgeometrie aus dem Zonengeometrie-Modell)
    /// ausgeliefert und ist <b>dauerhaft an</b>; vor einer Auslieferung wird er nicht mehr
    /// ausgeschaltet. Er bleibt öffentlich, weil die Oberfläche ihn liest, und ist bewusst kein
    /// Profilfeld und keine Einstellung: Er ändert sich nur mit dem Quelltext.</para>
    /// </summary>
    public static class GebaeudeExportRegeln
    {
        /// <summary>Die Stellung des Freigabeschalters: mit G7b ausgeliefert, dauerhaft an (F1, D2).</summary>
        private const bool FREIGABE_GBXML_EXPORT = true;

        /// <summary>Wird der gbXML-Export angeboten? Der Freigabeschalter (F1) — mit G7b dauerhaft an.</summary>
        public static bool GbxmlExportFreigegeben => FREIGABE_GBXML_EXPORT;
    }
}

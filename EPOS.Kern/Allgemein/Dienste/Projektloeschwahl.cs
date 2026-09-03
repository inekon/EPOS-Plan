using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Rückgabefach des Löschdialogs — die MEHRFACHauswahl (Nutzerauftrag 02.09.2026).
    ///
    /// <para><b>Warum ein zweites Fach neben <see cref="Projektwahl"/>.</b>
    /// <c>Projektwahl</c> trägt GENAU EIN Projekt heraus; das reichte, solange der
    /// Löschdialog eines auf einmal löschte. Seit dem Umbau auf Häkchenauswahl liefert
    /// er eine LISTE (Varianten vor ihren Stämmen) und dazu den Wunsch nach einer
    /// Sicherungskopie. Beides in <c>Projektwahl</c> zu quetschen hieße, der
    /// Projektauswahl Felder anzuhängen, die sie nie füllt — deshalb ein eigenes Fach
    /// nach derselben Bauform (iU5: der Aufrufer reicht es hinein und liest es danach
    /// aus, ohne die Maske zu kennen).</para>
    /// </summary>
    public sealed class Projektloeschwahl
    {
        /// <summary>
        /// Die vom Anwender bestätigten Projekte — VARIANTEN VOR IHREN STÄMMEN, damit
        /// das Löschen keine Variante hinter einem schon entfernten Stamm stehen lässt.
        /// Leer = der Anwender hat nichts gewählt.
        /// </summary>
        public List<ProjektModel> ZuLoeschen = new List<ProjektModel>();

        /// <summary>
        /// true, wenn vor dem Löschen eine Sicherungskopie der Datenbank angelegt werden
        /// soll (Vorgabe im Dialog).
        /// </summary>
        public bool SicherungGewuenscht = true;
    }
}

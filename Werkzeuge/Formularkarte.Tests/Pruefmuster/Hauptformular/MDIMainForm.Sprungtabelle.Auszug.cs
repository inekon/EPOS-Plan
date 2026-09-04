// Prüfmuster für Formularkarte — die WURZEL des eingefrorenen Maskenschlüssel-Wegs
// (iU9-W16c.5, offener Punkt W16b-O-1).
//
// Sie gehört zu `WinFormsNavigation.Auszug.cs` im selben Ordner: Dort steht die
// Sprungtabelle, hier der Anfang des Wegs. Zusammen bilden sie die Kette, die der Bestand
// nach iU9-W16b nicht mehr hergibt:
//
//     MDIMainForm  →  Masken.PufferSpAdmin  →  WinFormsNavigation.OeffneMaske
//                  →  Form_PufferSp_Admin   („ja", Pfadlänge 4)
//
// Der Erreichbarkeitsgraph kennt genau eine Wurzelmaske (`Erreichbarkeit.Wurzelmasken` =
// { "MDIMainForm" }); deshalb muss der Weg an DIESER Klasse beginnen. Sie ist im
// Prüfmusterbaum bereits als partielle Klasse angelegt — Pruefmuster/Pufferspeicher/
// MDIMainForm.Auszug.cs trägt den UNMITTELBAREN Weg, der für den „unklar"-Fall gebraucht
// wird. Dieser Teil trägt den Weg ÜBER DEN SCHLÜSSEL; beide zeigen auf dieselbe Maske und
// stören einander nicht.
//
// Die Methode heißt bewusst NICHT <Steuerelement>_Click: Ein Ereignishandler, den kein
// Designer anmeldet, gilt dem Graphen als gesperrt.

namespace WindowsFormsApplication1
{
    public partial class MDIMainForm : Form
    {
        public void PufferspeicherUeberDieSprungtabelle()
        {
            Dienste.Navigation.OeffneMaske(Masken.PufferSpAdmin);
        }
    }
}

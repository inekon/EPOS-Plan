// Prüfmuster für Formularkarte — die WURZEL des eingefrorenen „unklar"-Musters.
//
// Der Erreichbarkeitsgraph kennt genau zwei Wurzeln, MDIMainForm und Form_Start
// (Erreichbarkeit.Wurzelmasken). Ohne eine davon hätte jede Maske im Prüfmusterbaum den
// Zustand „nein" — und der Unterschied zwischen „nein" (kein Weg) und „unklar" (nur ein
// zweifelhafter Weg) wäre nicht mehr prüfbar.
//
// Dieser Auszug ist deshalb der KÜRZESTE Weg von der Wurzel bis zum gesperrten Knopf:
// Menüpunkt → Form_PufferSp_Admin (Zustand „ja") → btn_Bearbeiten / btn_Neu, die der
// Load-Handler in einem Zweig dauerhaft sperrt → Form_PufferSp_Bearbeiten („unklar").
//
// Der Rumpf ist der Menüpunkt aus WindowsFormsApplication1/MDIMainForm.cs
// (MenuItem_PufferSpBearbeiten_Click, Stand vor iU9-W14a.1) — dort geht er über
// MenueCtrl und die Maskentabelle, hier direkt, weil das Prüfmuster weder MenueCtrl noch
// die Navigation mitbringt und die KANTE dieselbe ist.
//
// Die Methode heißt bewusst NICHT <Steuerelement>_Click: Ein Ereignishandler, den kein
// Designer anmeldet, gilt dem Graphen als gesperrt ("Handler ... ist nirgends
// angemeldet") — und der Weg wäre schon hier zu Ende, bevor die Regel greift, um die es
// geht.

namespace WindowsFormsApplication1
{
    public partial class MDIMainForm : Form
    {
        public void PufferspeicherVerwaltungOeffnen()
        {
            using (Form_PufferSp_Admin frm = new Form_PufferSp_Admin())
            {
                frm.ShowDialog();
            }
        }
    }
}

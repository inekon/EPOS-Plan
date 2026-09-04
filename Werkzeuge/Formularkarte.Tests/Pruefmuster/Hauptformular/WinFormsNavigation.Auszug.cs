// Prüfmuster für Formularkarte — die SPRUNGTABELLE (iU9-W16c.5, offener Punkt W16b-O-1).
//
// Der Erreichbarkeitsgraph löst einen Maskenschlüssel NICHT über einen Aufruf auf,
// sondern über eine besondere Klasse: Er erkennt `WinFormsNavigation` am Namen, liest den
// `switch` in ihrer Methode `OeffneMaske` und ordnet jedem `case Masken.X:` die Masken zu,
// die dieser Zweig anfasst (Erreichbarkeit.cs:651-694). Wer irgendwo `Masken.X` schreibt,
// bekommt damit eine Kante auf genau diesen Zweig — und nur auf ihn.
//
// DIESE MECHANIK WAR NACH iU9-W16b NICHT MEHR PRÜFBAR: Der Bestand führte danach keinen
// einzigen Maskenschlüssel mehr, hinter dem eine WinForms-Maske stand (`Masken.ProjektDetail`
// ist mit `FormMain` gefallen, `Masken.Assistent` führt seit W16a.5 in eine Razor-Hülle).
// Der Test `DieSprungtabelleLoestDieMaskenschluesselAuf` wurde deshalb mit W16b.1 gestrichen
// und der Rückweg als offener Punkt W16b-O-1 notiert: „Wer im Prüfmuster einen Auszug der
// Sprungtabelle mit einfriert, bekommt diesen Zeugen zurück."
//
// Genau das steht hier. Der Auszug führt EINEN Zweig — den zum eingefrorenen
// `Form_PufferSp_Admin` unter Pruefmuster/Pufferspeicher/ —, und der Weg dorthin beginnt in
// `Hauptfensterrahmen.Sprungtabelle.Auszug.cs`, der zweiten Wurzeldatei desselben Ordners.
//
// Der Rumpf ist der Zweig aus WindowsFormsApplication1/Dienste/WinFormsNavigation.cs, Stand
// vor iU9-W14a.1 — dort ging er noch auf die Maske, heute auf `PufferSpAdminHuelle`. Die
// KANTE ist dieselbe, und nur um sie geht es.

namespace WindowsFormsApplication1
{
    public sealed class WinFormsNavigation
    {
        public bool OeffneMaske(string maske, params object[] argumente)
        {
            switch (maske)
            {
                case Masken.PufferSpAdmin:
                    using (Form_PufferSp_Admin frm = new Form_PufferSp_Admin())
                    {
                        return frm.ShowDialog() == DialogResult.OK;
                    }
            }

            return false;
        }
    }
}

using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Spalte „Kalender" der Gebäudekataloge</b> (Entwurf KP2, Welle K4, Festlegung 15): die
    /// Zeilen aus <see cref="GebaeudeStammCtrl.Katalogfilterzeilen"/>, ergänzt um die Zahl der
    /// angelegten Konditionierungskalender je Katalogbau (<see cref="Katalogfilterprofil.SpKonditionierungskalender"/>,
    /// 0 … 5). Die Zahl kommt aus <see cref="Konditionierungdatenweg.KalenderJeKatalogbau"/> — einer
    /// Abfrage für den ganzen Katalog über <c>ID_Gebaeude_Stamm</c>.
    ///
    /// <para>Ein Satz ohne Kalender zeigt 0, nicht „—": „kein Kalender" ist eine Aussage, keine
    /// Datenlücke, und die Spalte sortiert und filtert als Zahl.</para>
    /// </summary>
    public static class GebaeudeKatalogkalender
    {
        /// <summary>Die Zeilen der Gebäudekataloge samt Spalte „Kalender" — die Quelle der Katalogauswahl.</summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
            => Ergaenzen(GebaeudeStammCtrl.Katalogfilterzeilen());

        /// <summary>
        /// Setzt in jeder Zeile die Spalte „Kalender" (ganze Zahl) und gibt die Zeilen zurück; die Id
        /// der Zeile ist die des Katalogbaus.
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Ergaenzen(IReadOnlyList<Katalogfilterzeile> zeilen)
        {
            if (zeilen == null || zeilen.Count == 0) return zeilen;
            Dictionary<long, int> zahl = Konditionierungdatenweg.KalenderJeKatalogbau();
            foreach (Katalogfilterzeile z in zeilen)
                z.MitZahl(Katalogfilterprofil.SpKonditionierungskalender,
                          zahl.TryGetValue(z.Id, out int n) ? n : 0, 0);
            return zeilen;
        }
    }
}

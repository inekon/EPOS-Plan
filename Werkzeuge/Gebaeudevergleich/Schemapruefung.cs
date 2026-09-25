using System.Globalization;
using WindowsFormsApplication1;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b>Der Schemastand</b> der gebundenen Datenbank gegen <see cref="SchemaStand.Zielversion"/>
    /// dieses Stands. <c>vergleich</c> und <c>variante</c> rechnen nur auf dem Zielstand: Auf
    /// einem älteren Stand fehlen Spalten, die der Rechenweg liest (Zonen, Übergabe, Kühlung),
    /// und der Kern fiele still auf Vorgaben zurück — der Vergleich zeigte dann nicht den Weg,
    /// den der Anwender nach dem nächsten Programmstart rechnet.
    /// </summary>
    internal static class Schemapruefung
    {
        /// <summary>Der Schemastand der Datenbank, auf die <c>DataRepository</c> zeigt (0 = keiner).</summary>
        internal static int Stand() => ApplikationCtrl.GetSchemaVersion();

        /// <summary>Rückgabe <c>null</c> = Zielstand, sonst der Abbruchgrund samt Migrationsweg.</summary>
        internal static string Pruefen(int stand)
        {
            int ziel = SchemaStand.Zielversion;
            if (stand == ziel) return null;

            string s = stand.ToString(CultureInfo.InvariantCulture);
            string z = ziel.ToString(CultureInfo.InvariantCulture);
            if (stand > ziel)
                return "Schemastand der Datenbank " + s + ", Zielstand dieses Werkzeugs " + z + ": Die Datenbank ist " +
                       "neuer als das Werkzeug. Das Werkzeug auf den Stand des Programms bringen, das die Datenbank " +
                       "migriert hat.";

            return "Schemastand der Datenbank " + s + ", Zielstand dieses Werkzeugs " + z + ". Migrationsweg: " +
                   "EPOS-Plan in der Fassung dieses Stands starten (es migriert die Arbeitsdatenbank mit eigener " +
                   "Sicherung) und die Aufnahme wiederholen. Ersatzweg, nur wenn die Programm-Migration scheitert: " +
                   "die Momentaufnahme über den Windows-Referenzlauf migrieren (Werkzeuge/Gebaeudevergleich/LIESMICH.md).";
        }
    }
}

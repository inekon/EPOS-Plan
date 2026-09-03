using System;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Textlieferant des Kerns (Fachkonzept 3.7, Paket B5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum es diese Klasse gibt.</b> <c>KiKern</c> darf <c>MyResource</c> nicht
    /// referenzieren - er ist UI- und ressourcenfrei und hat ueberhaupt keine
    /// Projektreferenzen. Er kennt deshalb nur SCHLUESSEL
    /// (<see cref="KiTexte.Vorsatz"/> + Name) und fragt sie ueber
    /// <see cref="KiTexte.Lieferant"/> ab. Die Anwendung beantwortet sie hier aus
    /// <c>MyResource.Resource</c>, in der Sprache, die der Anwender eingestellt hat.
    /// </para>
    /// <para>
    /// <b>Der Lieferant darf NICHT werfen.</b> Fehlt ein Schluessel oder laesst sich der
    /// Ressourcensatz nicht laden, kommt <c>null</c> zurueck und der Kern nimmt seine
    /// deutschsprachige Vorgabe. Ein fehlender Text ist ein Schoenheitsfehler und kein
    /// Grund, eine Aktion scheitern zu lassen.
    /// </para>
    /// <para>
    /// Eingerichtet wird einmalig beim Programmstart (<c>Program.Main</c>), NACH dem
    /// Setzen der Oberflaechensprache. Der Aktionsharnisch und die Kerntests setzen
    /// keinen Lieferanten - dort greifen die Vorgaben des Kerns.
    /// </para>
    /// </remarks>
    public static class KiTextlieferant
    {
        private static bool _eingerichtet;

        /// <summary>
        /// Haengt den Lieferanten ein. Mehrfachaufruf ist unschaedlich.
        /// </summary>
        /// <remarks>
        /// Oeffentlich, damit der Aktionsharnisch dieselbe Verdrahtung pruefen kann, die
        /// auch das Programm benutzt - und nicht eine nachgebaute zweite.
        /// </remarks>
        public static void Einrichten()
        {
            if (_eingerichtet) return;
            _eingerichtet = true;
            KiTexte.Lieferant = Hole;
        }

        /// <summary>Ein Schluessel aus <c>MyResource</c>; <c>null</c>, wenn unbekannt.</summary>
        private static string Hole(string schluessel)
        {
            if (string.IsNullOrEmpty(schluessel)) return null;

            try
            {
                // Kultur bewusst null - dann gilt CurrentUICulture, genau wie bei den
                // erzeugten Eigenschaften der Ressourcenklasse.
                return MyResource.Resource.ResourceManager.GetString(schluessel, null);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

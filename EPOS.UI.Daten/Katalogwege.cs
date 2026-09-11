using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die zweite NAHT der Datenseite (Auftrag #208): Wege, die HEUTE noch in einer
    /// Windows-Hülle stecken und deshalb nicht auf jeder Plattform zu haben sind.
    ///
    /// <para><b>Warum es sie gibt.</b> Der Auslieferungskatalog erscheint über
    /// <c>KatalogBrowserHuelle</c>, und die öffnet je nach Weg ein eigenes
    /// WinForms-Fenster; ihre Datenhälfte ist plattformfrei, ihre Fensterhälfte nicht.
    /// Sie mit umzuziehen hätte die Kette Katalogbrowser → Editoren → Kataloghüllen
    /// aufgemacht — ein eigener Schritt (iU11). Bis dahin ist der eine Weg, den die
    /// Simulationskonfiguration braucht, ein HAKEN: Windows hängt ihn in
    /// <c>Program.Main</c> ein, iOS lässt ihn leer.</para>
    ///
    /// <para><b>Kein Delegat ist kein Knopf.</b> Ohne eingehängten Haken zeigt
    /// <c>PufferSpProjektDialog</c> den Knopf „Katalog ansehen" gar nicht erst
    /// (<c>@if (VerwaltungGaben is not null)</c>) — die Pufferverwaltung selbst
    /// funktioniert vollständig.</para>
    /// </summary>
    internal static class Katalogwege
    {
        /// <summary>
        /// Der Parametersatz des Auslieferungskatalogs der Pufferspeicher, NUR LESEN —
        /// unter Windows <c>PufferSpAdminHuelle.Gaben(true)</c>. <c>null</c> = diese
        /// Schale zeigt den Katalog nicht an.
        /// </summary>
        internal static Func<IReadOnlyDictionary<string, object>> PufferKatalogGaben;
    }
}

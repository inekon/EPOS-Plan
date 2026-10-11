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

        /// <summary>
        /// Die Dateiwege des Ganglinienimports der Solarthermie (Katalogseite des Dialogs
        /// „Solarthermie Ganglinie"): Dateiwahl mit dem Ganglinienordner als Start,
        /// verlustfreie Ablage, Anzeigen mit der Systemanwendung. Unter Windows
        /// <c>SolarganglinieHuelle.Dateiwege()</c>; <c>null</c> = diese Schale importiert
        /// nicht, und der Knopf „Import…" nennt den Grund
        /// (<c>SGL_IMP_NICHT_VERFUEGBAR</c>).
        /// </summary>
        internal static Func<GanglinienDateiwege> SolarganglinienDatei;

        /// <summary>
        /// Die Dateiwege des Imports der PV-Ganglinien (Dialog „Photovoltaik Ganglinie", PVG): dieselbe Form wie
        /// <see cref="SolarganglinienDatei"/>. Unter Windows <c>PvGanglinieHuelle.Dateiwege()</c>; <c>null</c> = diese
        /// Schale importiert nicht (iOS), und der Knopf „Import…" nennt den Grund (<c>PVG_IMP_NICHT_VERFUEGBAR</c>).
        /// </summary>
        internal static Func<GanglinienDateiwege> PvGanglinienDatei;
    }

    /// <summary>
    /// Was eine Schale zum Import einer Ganglinien-DATEI beisteuert — die Kette selbst
    /// (Lesen, Prüfen, Schreiben) liegt im Kern.
    /// </summary>
    internal sealed class GanglinienDateiwege
    {
        /// <summary>Der Dateiwähler (Filter → Pfad, <c>null</c> = abgebrochen).</summary>
        internal Func<string, System.Threading.Tasks.Task<string>> DateiWaehlen;

        /// <summary>Die verlustfreie Originalablage im Ganglinienordner (optional).</summary>
        internal Func<string, System.Threading.Tasks.Task<EPOS.UI.Dialoge.Bedarf.AblageErgebnis>> Ablegen;

        /// <summary>Öffnet eine Datei mit der Systemanwendung (optional).</summary>
        internal Func<string, System.Threading.Tasks.Task<bool>> MitSystemOeffnen;

        /// <summary>Der Ganglinienordner als Anzeigetext.</summary>
        internal string Ordner = "";
    }
}

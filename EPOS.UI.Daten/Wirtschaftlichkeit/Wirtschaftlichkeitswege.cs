using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die NAHT der Datenseite von Kosten und Wirtschaftlichkeit (Etappe E3,
    /// Schritt 1): die zwei Wege, die HEUTE noch in einer Windows-Hülle stecken
    /// und deshalb nicht auf jeder Plattform zu haben sind.
    ///
    /// <para><b>Warum es sie gibt.</b> Die vier nahtlosen Hüllen dieses Feldes
    /// führen selbst keine einzige WinForms-Anweisung; sie RUFEN aber zwei
    /// Hüllen, die noch ein eigenes Fenster zeigen und deshalb erst mit
    /// E3 Schritt 6 wandern: <c>GesetzeskatalogHuelle</c>
    /// (<c>Views/Admin/</c>) und <c>PhotovoltaikVerguetungHuelle</c>
    /// (<c>Views/Wirtschaftlichkeit/</c>). Statt der zwei Aufrufe wegen die
    /// ganze Datenseite in der Windows-Anwendung zu lassen, kommen sie als
    /// benannte Naht herein — dasselbe Muster wie <see cref="Katalogwege"/>:
    /// Windows hängt sie in <c>Program.Main</c> ein, iOS lässt sie leer.</para>
    ///
    /// <para><b>Kein Delegat ist kein Knopf.</b> Ohne eingehängten Haken setzt
    /// die Hülle den betroffenen Schlüssel gar nicht erst in ihren
    /// Parametersatz; <c>WirtschaftlichkeitParameterDialog</c> zeigt den
    /// Gesetzeskatalog dann nicht an (<c>@if (GesetzeGaben is not null)</c>),
    /// der Abschnitt „Ertrag/Bonus" keinen Weg in die PV-Vergütung. Alles
    /// Übrige der beiden Masken arbeitet vollständig.</para>
    ///
    /// <para><b>Diese Naht ist auf Abruf.</b> Mit E3 Schritt 6 wandern beide
    /// gerufenen Hüllen selbst nach <c>EPOS.UI.Daten</c> (Fenster-Adapter im
    /// Muster <c>EnergietraegerFenster</c>); dann rufen die Hüllen einander
    /// unmittelbar, und diese Datei entfällt.</para>
    /// </summary>
    internal static class Wirtschaftlichkeitswege
    {
        /// <summary>
        /// Der Parametersatz des Gesetzeskatalogs zu EINER Gesetzesklasse
        /// (<c>DbWerte.GESETZ_KLASSE_*</c>) — unter Windows
        /// <c>GesetzeskatalogHuelle.Gaben(klasse)</c>. <c>null</c> = diese Schale
        /// zeigt den Katalog nicht als Überlagerung an.
        /// </summary>
        internal static Func<string, IReadOnlyDictionary<string, object>> GesetzeskatalogGaben;

        /// <summary>
        /// Öffnet den PV-Vergütungsdialog für ein Projekt
        /// (<c>Tab_Projekt.ID</c>) — unter Windows
        /// <c>PhotovoltaikVerguetungHuelle.Oeffnen(null, idProjekt)</c>, also ein
        /// eigenes Fenster über dem Kostendialog (Risiko R2, deshalb
        /// nachgelagert). <c>null</c> = diese Schale kennt den Weg nicht.
        /// </summary>
        internal static Action<int> PvVerguetungOeffnen;
    }
}

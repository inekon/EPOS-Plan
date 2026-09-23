using System;
using System.Collections.Generic;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die NAHT der Gebäudehüllen (Stufe G1; Umsetzungskonzept Gebäudesimulation 2.8,
    /// Entscheid E27/A10): die zwei Wege, die HEUTE noch in einer Windows-Hülle stecken.
    ///
    /// <para><b>Warum es sie gibt.</b> Die Gebäudehüllen liegen seit G1 in
    /// <c>EPOS.UI.Daten</c>; zwei ihrer Unterdialoge — die Brauchwasser-Profilliste
    /// (<c>BedarfsProfileHuelle</c>) und die Gebäudetypen-Verwaltung
    /// (<c>GebaeudetypHuelle</c>) — haben ihre Datenhälfte noch in der Windows-Schale.
    /// Windows hängt beide Haken in <c>Program.Main</c> ein, iOS lässt sie leer. Bauform wie
    /// <see cref="Katalogwege"/>.</para>
    ///
    /// <para><b>Kein Delegat ist kein Knopf.</b> Ohne eingehängten Haken zeigt der
    /// Katalogeditor keinen Knopf „Brauchwasser…" und der Gebäudedialog keinen Knopf
    /// „Gebäudetyp in DB ändern…".</para>
    /// </summary>
    internal static class Gebaeudewege
    {
        /// <summary>
        /// Der Parametersatz der Brauchwasser-Profilliste des Projekts: Projekt-Id, die
        /// Zeilen (die Liste gehört dem Aufrufer), der Änderungsrückruf und der
        /// Zapfprofil-Behälter dieses Öffnens (Umsetzungskonzept Zapfprofilgenerator 5.2).
        /// <c>null</c> = diese Schale zeigt die Liste nicht an.
        /// </summary>
        internal static Func<int, List<BedarfsProfilZeile>, Action, ZapfprofilBehaelter, IReadOnlyDictionary<string, object>> BrauchwasserGaben;

        /// <summary>Der Parametersatz der Gebäudetypen-Verwaltung; <c>null</c> = kein Knopf.</summary>
        internal static Func<IReadOnlyDictionary<string, object>> GebaeudetypGaben;
    }
}

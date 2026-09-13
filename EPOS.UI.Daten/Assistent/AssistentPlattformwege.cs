using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die NAHT zwischen dem plattformfreien Projektassistenten und der Schale, in
    /// der er läuft (Befund <b>W16a-O-4</b>) — dieselbe Bauform wie
    /// <see cref="SimulationPlattformwege"/> aus Auftrag #208.
    ///
    /// <para><b>Wozu sie da ist.</b> Der Ablauf des Assistenten steht im Kern
    /// (<see cref="AssistentCtrl"/>), der Rahmen als Razor-Seite
    /// (<c>EPOS.UI/Seiten/Assistent/AssistentSeite</c>) und sein Parametersatz seit
    /// diesem Befund in <see cref="AssistentAnsichtQuelle"/> — plattformfrei. Was
    /// dort NICHT stehen kann, ist zweierlei: die elf Seitenhüllen, die bis heute in
    /// <c>WindowsFormsApplication1/Views</c> liegen und dort einen Fensterbesitzer
    /// führen (sie wandern mit iU11), und die zwei Kurzhinweise der Startseite, die
    /// es nur unter Windows gibt.
    ///
    /// <para><b>Kein Delegat ist kein Knopf</b> — mit derselben Ausnahme wie bei der
    /// Simulation: Ein Weg, den die Plattform NICHT anbietet, wird BENANNT abgelehnt
    /// und fällt nicht still aus. Dafür trägt die Naht ihren
    /// <see cref="SeitenSperrgrund"/>; die Assistentenseite zeigt ihn anstelle des
    /// leeren Schritts.</para>
    /// </summary>
    internal sealed class AssistentPlattformwege
    {
        /// <summary>
        /// Der Parametersatz EINER Assistentenseite jenseits der zwei
        /// plattformfreien (Komponentenauswahl und Projektkopf).
        ///
        /// <para>Argumente: der laufende <see cref="AssistentCtrl"/>, die
        /// Seitennummer aus <see cref="WizardItemClass"/> und der Projektname des
        /// Bearbeiten-Zweigs (im Neu-Zweig leer). <c>null</c> = diese Plattform
        /// kennt die elf Seiten nicht; dann gilt
        /// <see cref="SeitenSperrgrund"/>.</para>
        /// </summary>
        /// <remarks>
        /// Unter Windows ist das <c>AssistentHuelle.Seitengaben</c> — elf Aufrufe von
        /// <c>GebaeudeHuelle.Gaben(IWin32Window, …)</c> und seinen Geschwistern. Sie
        /// reichen den Fensterbesitzer an die Katalogdialoge weiter, die sie aus sich
        /// heraus öffnen, und liegen deshalb weiter in der Windows-Anwendung.
        /// </remarks>
        internal Func<AssistentCtrl, int, string, IReadOnlyDictionary<string, object>> SeitenGaben;

        /// <summary>
        /// Der Grund, aus dem die elf Seiten auf DIESER Plattform nicht gehen — die
        /// benannte Ablehnung. Leer = sie gehen.
        /// </summary>
        internal string SeitenSperrgrund = "";

        /// <summary>
        /// Ein Kurzhinweis über der Startansicht („Daten gespeichert"). <c>null</c> =
        /// diese Schale führt keine Startseite; dann gibt es keinen Hinweis, und das
        /// ist kein Fehler.
        /// </summary>
        internal Action<string> Kurzhinweis;

        /// <summary>
        /// Der Kurzhinweis „Projekt &lt;Name&gt; geöffnet!" nach dem Rückweg aus dem
        /// linken Band. <c>null</c> = wie oben.
        /// </summary>
        internal Action HinweisProjektGeoeffnet;

        /// <summary>Die Naht einer Schale, die die elf Seiten nicht anbietet.</summary>
        internal static AssistentPlattformwege Ohne(string sperrgrund)
        {
            return new AssistentPlattformwege { SeitenSperrgrund = sperrgrund ?? "" };
        }
    }
}

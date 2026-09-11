using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Das EINE Programmsymbol jedes Fensters (Auftrag #229, Anwenderwunsch
    /// 11.09.2026: "nehme das EPOS-ICON als Programm-Symbol (fuer taskleiste
    /// etc.)"). Bis dahin trug <c>WindowsFormsApplication1.csproj</c> kein
    /// <c>&lt;ApplicationIcon&gt;</c> und kein Fenster ein eigenes
    /// <see cref="Form.Icon"/> — Taskleiste, Alt-Tab, Explorer und jeder
    /// Fensterkopf zeigten das .NET-Standardsymbol.
    ///
    /// <para><b>EINE Quelle, kein zweites Bild.</b> Das Symbol ist die
    /// EPOS-Plan-Marke (drei Farbfelder, weisser Kern, Blitz — dieselbe wie in
    /// <c>EPOS.UI/Bausteine/InfoKnopf.razor</c>/<c>KiKnopf.razor</c>) als
    /// mehrstufiges ICO (16/24/32/48/64/128/256, PNG-komprimiert) unter
    /// <c>Resources/EPOS-Plan.ico</c>. Über <c>&lt;ApplicationIcon&gt;</c> im
    /// csproj steckt es schon in der gebauten EXE, und <c>Setup/EPOS-Plan.iss</c>
    /// nimmt für den Installer dieselbe Datei. Diese Klasse liest die Datei
    /// deshalb NICHT ein zweites Mal ein, sondern holt das Symbol dort ab, wo es
    /// bereits liegt: <see cref="Icon.ExtractAssociatedIcon"/> auf dem Pfad der
    /// laufenden EXE.</para>
    ///
    /// <para><b>WinForms setzt es nicht von selbst.</b> Ein
    /// <c>&lt;ApplicationIcon&gt;</c> allein färbt nur die EXE-Datei (Explorer,
    /// Taskleistenkachel ohne eigenes Fenstericon) — jedes <see cref="Form"/>
    /// braucht sein <see cref="Form.Icon"/> ausdrücklich gesetzt, sonst zeigt sein
    /// Fensterkopf weiter das eingebaute .NET-Symbol. Jedes Fenster, das eine
    /// eigene Taskleisten- oder Alt-Tab-Kachel tragen kann, ruft deshalb
    /// <see cref="Anwenden"/> in seinem Konstruktor — heute drei Stellen:
    /// <c>Hauptfensterrahmen</c>, <c>BlazorDialogForm&lt;T&gt;</c> (die EINE
    /// <see cref="Form"/>-Unterklasse aller Blazor-Dialoge; <c>KiChatHuelle</c>
    /// baut intern eine davon auf und bekommt das Symbol damit geschenkt, ohne
    /// einen zweiten Aufruf) und <c>Form_HelpPopup</c>.</para>
    ///
    /// <para><b>Kein Kern.</b> <c>EPOS.Kern</c> kennt kein WinForms (Wächter in
    /// <c>EPOS.Kern/CLAUDE.md</c>) — diese Klasse gehört deshalb in die
    /// WinForms-Hülle, nicht in einen Controller.</para>
    /// </summary>
    internal static class Programmsymbol
    {
        /// <summary>
        /// Einmal geladen, für den Rest des Prozesses wiederverwendet —
        /// <see cref="Icon.ExtractAssociatedIcon"/> liest bei jedem Aufruf die
        /// EXE-Datei neu; das braucht kein Fenster zweimal zu bezahlen.
        /// </summary>
        private static readonly Lazy<Icon> _symbol = new Lazy<Icon>(Laden);

        private static Icon Laden()
        {
            try
            {
                string pfad = Application.ExecutablePath;
                return string.IsNullOrEmpty(pfad) ? null : Icon.ExtractAssociatedIcon(pfad);
            }
            catch (Exception ex)
            {
                // Kein Absturz wegen eines fehlenden oder nicht lesbaren Symbols -
                // das Fenster bleibt dann beim .NET-Standardsymbol, wie vor
                // Auftrag #229.
                System.Diagnostics.Debug.WriteLine("Programmsymbol: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Setzt das Programmsymbol auf <paramref name="fenster"/> — folgenlos,
        /// wenn es nicht geladen werden konnte.
        /// </summary>
        public static void Anwenden(Form fenster)
        {
            if (fenster == null) return;

            Icon symbol = _symbol.Value;
            if (symbol == null) return;

            fenster.Icon = symbol;
        }
    }
}

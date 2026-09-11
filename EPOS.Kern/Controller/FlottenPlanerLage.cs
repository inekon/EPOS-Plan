using System;
using SpeicherEngine;

namespace WindowsFormsApplication1;

/// <summary>
/// Die EINE Wahrheit zur Frage „Kann diese Plattform einen Fahrplan rechnen?" (Auftrag #170c).
///
/// <para>Drei der fünf Betriebsziele einer Speicherflotte PLANEN: <see cref="FlottenBetriebsziel.PvPlanung"/>,
/// <see cref="FlottenBetriebsziel.Arbitrage"/> und <see cref="FlottenBetriebsziel.MultiUse"/>. Sie brauchen
/// einen <see cref="IFlottenPlaner"/>. Den einzigen Planer des Hauses —
/// <c>SpeicherPlanung.OrToolsFlottenPlaner</c> auf Google OR-Tools/SCIP — kennt allein die
/// Windows-Anwendung; <c>WindowsFormsApplication1.Program.Main</c> legt ihn als
/// <see cref="SpeicherFlottenProjektCtrl.PlanerFactory"/> ein.</para>
///
/// <para><b>Auf iOS wird bewusst keiner registriert:</b> <c>EPOS.iOS/MauiProgram</c> setzt die Fabrik
/// nicht, weil OR-Tools dort keine native Hälfte hat (Hausregel in <c>CLAUDE.md</c>: OR-Tools nie an
/// Kern, UI, SpeicherEngine oder iOS). Dieselbe Lage haben die Linux-Prüfstände. Die Auskunft braucht
/// dafür KEINEN Plattformadapter — sie liest die Fabrik, sonst nichts.</para>
///
/// <para>Die zwei Ausnahmen in <see cref="SpeicherFlottenProjektCtrl"/> und <c>FlottenSimulator</c>
/// bleiben als letzte Schranke stehen. Diese Auskunft ist der VORDERE Riegel: Die Oberfläche fragt sie,
/// sperrt die drei planenden Ziele mit sichtbarem Grund und erklärt ein gespeichertes planendes Profil
/// mit einem Banner, statt den Anwender erst im Lauf gegen eine Ausnahme laufen zu lassen.</para>
/// </summary>
public static class FlottenPlanerLage
{
    /// <summary>Sperre um den Zwischenspeicher; die Auskunft wird aus Zeichenfäden gelesen.</summary>
    private static readonly object Riegel = new();

    /// <summary>Die Fabrik, für die <see cref="_letztesErgebnis"/> gilt. Wechselt sie, wird neu geprüft.</summary>
    private static Func<IFlottenPlaner> _geprueft;
    private static bool _gilt;
    private static bool _letztesErgebnis;

    /// <summary>
    /// Plant dieses Betriebsziel, braucht also einen <see cref="IFlottenPlaner"/>?
    /// Die zwei reaktiven Ziele <see cref="FlottenBetriebsziel.PvGreedy"/> und
    /// <see cref="FlottenBetriebsziel.PeakShaving"/> rechnen ohne Planer.
    /// </summary>
    public static bool IstPlanend(FlottenBetriebsziel ziel) =>
        ziel is FlottenBetriebsziel.PvPlanung or FlottenBetriebsziel.Arbitrage
            or FlottenBetriebsziel.MultiUse;

    /// <summary>
    /// Ist ein Fahrplan-Löser da? <c>true</c>, wenn eine Fabrik registriert ist UND sie einen Planer
    /// liefert. Eine Fabrik, die <c>null</c> gibt oder wirft, zählt wie keine — genau das, woran
    /// <see cref="SpeicherFlottenProjektCtrl"/> sonst erst im Lauf scheitern würde.
    /// </summary>
    /// <remarks>
    /// Das Ergebnis wird je Fabrikinstanz gemerkt: Die Oberfläche fragt bei jedem Zeichnen, und ein
    /// Probelauf der Fabrik legt bei Windows einen OR-Tools-Planer an. Setzt jemand eine ANDERE Fabrik
    /// ein (der Betrieb einmal beim Start, ein Prüfstand öfter), wird neu geprüft.
    /// </remarks>
    public static bool Verfuegbar
    {
        get
        {
            Func<IFlottenPlaner> fabrik = SpeicherFlottenProjektCtrl.PlanerFactory;
            lock (Riegel)
            {
                if (_gilt && ReferenceEquals(_geprueft, fabrik)) return _letztesErgebnis;
                _geprueft = fabrik;
                _gilt = true;
                _letztesErgebnis = Probe(fabrik);
                return _letztesErgebnis;
            }
        }
    }

    /// <summary>
    /// Der Grund, warum planende Ziele gesperrt sind — leer, solange ein Planer da ist.
    /// Der Text steht in <c>MyResource.Resource</c> und wird mitübersetzt.
    /// </summary>
    public static string Grund => Verfuegbar ? "" : MyResource.Resource.FLOTTE_PLANER_FEHLT;

    /// <summary>
    /// Lässt sich dieses Betriebsziel hier rechnen? Reaktive Ziele immer, planende nur mit Planer.
    /// </summary>
    public static bool ZielMoeglich(FlottenBetriebsziel ziel) => !IstPlanend(ziel) || Verfuegbar;

    /// <summary>
    /// Vergisst das gemerkte Ergebnis. Für Prüfstände, die dieselbe Fabrikinstanz behalten und
    /// trotzdem eine neue Antwort erwarten.
    /// </summary>
    public static void Vergessen()
    {
        lock (Riegel)
        {
            _geprueft = null;
            _gilt = false;
            _letztesErgebnis = false;
        }
    }

    private static bool Probe(Func<IFlottenPlaner> fabrik)
    {
        if (fabrik == null) return false;
        try
        {
            IFlottenPlaner planer = fabrik();
            (planer as IDisposable)?.Dispose();
            return planer != null;
        }
        catch (Exception)
        {
            // Eine Fabrik, die beim Anlegen scheitert (fehlende native Hälfte, gesperrte
            // Lizenz), ist für die Oberfläche dasselbe wie keine Fabrik.
            return false;
        }
    }
}

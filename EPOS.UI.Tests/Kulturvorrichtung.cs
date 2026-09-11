using System;
using System.Globalization;
using System.Threading;
using Bunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Die GEMEINSAME Vorrichtung für die Oberflächenkultur aller bunit-Fälle, seit
/// Auftrag #168 (11.09.2026, Anwenderentscheid „Empfehlung umsetzen" auf Befund
/// #167: 65 Dateien pinnten die Kultur mit je einer eigenen Methode, kaum eine
/// stellte sie zurück). Statt dessen EIN Werkzeug: Merkt beim Anlegen die VIER
/// Werte EINZELN (nicht die UI-Kultur aus dem Kultur-Merkwert — die Lehre aus
/// #167) und stellt in <c>Dispose</c> jeden aus seinem eigenen Merkwert zurück.
/// Regel seit diesem Auftrag: Kein Test pinnt die Prozesskultur ohne
/// Rückstellung — über diese Vorrichtung oder in eigenem <c>Dispose</c>/<c>finally</c>;
/// der Kulturwächter (<c>EPOS.Kern.Tests/KulturwaechterTests.cs</c>) prüft das seither
/// auch für <c>EPOS.UI.Tests</c>.
/// </summary>
public sealed class Kulturvorrichtung : IDisposable
{
    private readonly CultureInfo? _defaultThreadCurrentCultureVorher;
    private readonly CultureInfo? _defaultThreadCurrentUICultureVorher;
    private readonly CultureInfo _threadCurrentCultureVorher;
    private readonly CultureInfo _threadCurrentUICultureVorher;

    /// <summary>
    /// Merkt die vier Werte und setzt <paramref name="kultur"/> (Standard
    /// <c>de-DE</c>) auf <c>CultureInfo.DefaultThreadCurrentCulture</c>,
    /// <c>…DefaultThreadCurrentUICulture</c>, <c>Thread.CurrentThread.CurrentCulture</c>
    /// und <c>…CurrentUICulture</c>.
    /// </summary>
    public Kulturvorrichtung(string kultur = "de-DE")
    {
        _defaultThreadCurrentCultureVorher = CultureInfo.DefaultThreadCurrentCulture;
        _defaultThreadCurrentUICultureVorher = CultureInfo.DefaultThreadCurrentUICulture;
        _threadCurrentCultureVorher = Thread.CurrentThread.CurrentCulture;
        _threadCurrentUICultureVorher = Thread.CurrentThread.CurrentUICulture;

        CultureInfo neu = CultureInfo.GetCultureInfo(kultur);
        CultureInfo.DefaultThreadCurrentCulture = neu;
        CultureInfo.DefaultThreadCurrentUICulture = neu;
        Thread.CurrentThread.CurrentCulture = neu;
        Thread.CurrentThread.CurrentUICulture = neu;
    }

    /// <summary>Stellt jeden der vier Werte aus seinem eigenen Merkwert zurück.</summary>
    public void Dispose()
    {
        CultureInfo.DefaultThreadCurrentCulture = _defaultThreadCurrentCultureVorher;
        CultureInfo.DefaultThreadCurrentUICulture = _defaultThreadCurrentUICultureVorher;
        Thread.CurrentThread.CurrentCulture = _threadCurrentCultureVorher;
        Thread.CurrentThread.CurrentUICulture = _threadCurrentUICultureVorher;
    }
}

/// <summary>
/// Basisklasse für bunit-Testklassen, die die Oberflächenkultur pinnen: Legt im
/// Konstruktor eine <see cref="Kulturvorrichtung"/> an (Standard <c>de-DE</c>,
/// optional eine andere Kultur für Sprachwechsel-Fälle) und gibt sie beim
/// Verwerfen frei. Ersetzt die früheren privaten Pinn-Methoden
/// (<c>DeutscheOberflaeche()</c> u. ä.) — <c>class XyzTests : BunitContext</c>
/// wird zu <c>class XyzTests : EposBunitContext</c>, der Aufruf im Konstruktor
/// entfällt.
/// </summary>
public abstract class EposBunitContext : BunitContext
{
    private readonly Kulturvorrichtung _kulturvorrichtung;

    protected EposBunitContext(string kultur = "de-DE")
    {
        _kulturvorrichtung = new Kulturvorrichtung(kultur);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _kulturvorrichtung.Dispose();
        }

        base.Dispose(disposing);
    }
}

using KiKern;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Strom;

/// <summary>
/// Das FLACHE Abbild des <see cref="GanglinieImportOptionenDialog"/> für den Hilfe-Assistenten
/// (Freigabe der Importdialoge, Teil A): acht Klapplisten, die Kopfzeile, die erste Datenzeile und
/// die Zahl der zu lesenden Zeilen.
/// </summary>
/// <remarks>
/// <para><b>Warum eine Sichtklasse:</b> Der Dialog führt nur private PLÄTZE seiner Klapplisten.
/// Die Sicht hält keinen Zustand: Gelesen wird der Platz, gesetzt über den Delegaten
/// <c>setzen</c> — derselbe Weg wie ein Griff in die Klappliste (der Platz wechselt, die Vorschau
/// bleibt bis „Vorschau aktualisieren" stehen, wie von Hand).</para>
/// <para><b>Sperrgrund:</b> Ein Tabellenblatt gibt es nur bei einer Excel-Mappe. Die
/// Formatbeschreibung (IM-4, <see cref="GanglinienImportFormat"/>) sperrt, was ihr Format nicht
/// zeigt: Einheit, Raster und Konvention bei einem Sonderformat, das Jahr ohne Jahresfeld.</para>
/// </remarks>
public sealed class GanglinieImportOptionenKiSicht
{
    private readonly Func<string, int> _platz;
    private readonly Func<bool> _kopfzeile;
    private readonly Action<string, object?> _setzen;
    private readonly Func<string, IReadOnlyList<KiWahleintrag>> _wahl;
    private readonly Func<bool> _istExcel;
    private readonly Func<GanglinienImportFormat> _format;

    /// <summary>Legt die Sicht über die Plätze des Dialogs.</summary>
    public GanglinieImportOptionenKiSicht(Func<string, int> platz, Func<bool> kopfzeile,
                                          Action<string, object?> setzen,
                                          Func<string, IReadOnlyList<KiWahleintrag>> wahl,
                                          Func<bool> istExcel,
                                          Func<GanglinienImportFormat>? format = null)
    {
        _platz = platz ?? throw new ArgumentNullException(nameof(platz));
        _kopfzeile = kopfzeile ?? throw new ArgumentNullException(nameof(kopfzeile));
        _setzen = setzen ?? throw new ArgumentNullException(nameof(setzen));
        _wahl = wahl ?? throw new ArgumentNullException(nameof(wahl));
        _istExcel = istExcel ?? throw new ArgumentNullException(nameof(istExcel));
        _format = format ?? (() => GanglinienImportFormat.Ganglinie);
    }

    /// <summary>Der Sperrgrund je Katalogfeld (<c>KiMaskenhaken.Sperrgrund</c>); <c>null</c> = frei.</summary>
    public string? Sperrgrund(string feld)
    {
        GanglinienImportFormat f = _format() ?? GanglinienImportFormat.Ganglinie;
        return feld switch
        {
            "blatt" when !_istExcel() => Resource.KI_GIO_NUR_EXCEL,
            "einheit" when !f.Einheitswahl => Resource.KI_GIO_FORMAT_FEST,
            "raster" or "konvention" when !f.Rasterwahl => Resource.KI_GIO_FORMAT_FEST,
            "jahr" when !f.Jahresfeld => Resource.KI_GIO_KEIN_JAHR,
            _ => null
        };
    }

    /// <summary>Die Kennung des Formats, das der Dialog liest (<c>GANGLINIE</c>, <c>QUELLPROFIL</c> …).</summary>
    public string Format => (_format() ?? GanglinienImportFormat.Ganglinie).Kennung;

    /// <summary>
    /// Wie die Vorschau die Reihe zeigt: <c>Jahr</c> (Jahresverlauf) oder <c>Tag</c> (Tagesansicht der
    /// TWW-Messreihe).
    /// </summary>
    public string Bildart => (_format() ?? GanglinienImportFormat.Ganglinie).Bildart.ToString();

    /// <summary>Feste Werteanzahl des Formats; 0 = frei.</summary>
    public int Werteanzahl => (_format() ?? GanglinienImportFormat.Ganglinie).Werteanzahl;

    /// <summary>Das Jahr der Reihe (nur bei Formaten mit Jahresfeld); 0 = keines.</summary>
    public int Jahr
    {
        get => _platz(nameof(Jahr));
        set => _setzen(nameof(Jahr), value);
    }

    /// <summary>Ist die erste Zeile eine Kopfzeile?</summary>
    public bool Kopfzeile
    {
        get => _kopfzeile();
        set => _setzen(nameof(Kopfzeile), value);
    }

    /// <summary>Die 1-basierte Nummer der ersten Datenzeile (Zählung der Vorschautabelle).</summary>
    public int ErsteDatenzeile
    {
        get => _platz(nameof(ErsteDatenzeile));
        set => _setzen(nameof(ErsteDatenzeile), value);
    }

    /// <summary>Wie viele Zeilen ab der ersten Datenzeile gelesen werden.</summary>
    public int Zeilenzahl
    {
        get => _platz(nameof(Zeilenzahl));
        set => _setzen(nameof(Zeilenzahl), value);
    }

    /// <summary>Die Wahl des Feldes <c>Trennzeichen</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> TrennzeichenWahl => _wahl(nameof(Trennzeichen));

    /// <summary>Der Platz der Klappliste <c>Trennzeichen</c>.</summary>
    public int Trennzeichen
    {
        get => _platz(nameof(Trennzeichen));
        set => _setzen(nameof(Trennzeichen), value);
    }

    /// <summary>Die Wahl des Feldes <c>Dezimaltrenner</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> DezimaltrennerWahl => _wahl(nameof(Dezimaltrenner));

    /// <summary>Der Platz der Klappliste <c>Dezimaltrenner</c>.</summary>
    public int Dezimaltrenner
    {
        get => _platz(nameof(Dezimaltrenner));
        set => _setzen(nameof(Dezimaltrenner), value);
    }

    /// <summary>Die Wahl des Feldes <c>Wertspalte</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> WertspalteWahl => _wahl(nameof(Wertspalte));

    /// <summary>Der Platz der Klappliste <c>Wertspalte</c>.</summary>
    public int Wertspalte
    {
        get => _platz(nameof(Wertspalte));
        set => _setzen(nameof(Wertspalte), value);
    }

    /// <summary>Die Wahl des Feldes <c>Zeitspalte</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> ZeitspalteWahl => _wahl(nameof(Zeitspalte));

    /// <summary>Der Platz der Klappliste <c>Zeitspalte</c>.</summary>
    public int Zeitspalte
    {
        get => _platz(nameof(Zeitspalte));
        set => _setzen(nameof(Zeitspalte), value);
    }

    /// <summary>Die Wahl des Feldes <c>Einheit</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> EinheitWahl => _wahl(nameof(Einheit));

    /// <summary>Der Platz der Klappliste <c>Einheit</c>.</summary>
    public int Einheit
    {
        get => _platz(nameof(Einheit));
        set => _setzen(nameof(Einheit), value);
    }

    /// <summary>Die Wahl des Feldes <c>Raster</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> RasterWahl => _wahl(nameof(Raster));

    /// <summary>Der Platz der Klappliste <c>Raster</c>.</summary>
    public int Raster
    {
        get => _platz(nameof(Raster));
        set => _setzen(nameof(Raster), value);
    }

    /// <summary>Die Wahl des Feldes <c>Konvention</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> KonventionWahl => _wahl(nameof(Konvention));

    /// <summary>Der Platz der Klappliste <c>Konvention</c>.</summary>
    public int Konvention
    {
        get => _platz(nameof(Konvention));
        set => _setzen(nameof(Konvention), value);
    }

    /// <summary>Die Wahl des Feldes <c>Blatt</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> BlattWahl => _wahl(nameof(Blatt));

    /// <summary>Der Platz der Klappliste <c>Blatt</c>.</summary>
    public int Blatt
    {
        get => _platz(nameof(Blatt));
        set => _setzen(nameof(Blatt), value);
    }
}

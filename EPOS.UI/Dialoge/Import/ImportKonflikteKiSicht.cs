using System.Globalization;
using KiKern;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Import;

/// <summary>
/// Das FLACHE Abbild des <see cref="ImportKonflikteDialog"/> für den Hilfe-Assistenten
/// (Freigabe der Importdialoge, Teil A): das Raster der Kandidaten mit Aktion und Name.
/// </summary>
/// <remarks>
/// <para><b>Warum eine Sichtklasse:</b> Die Zeilen des Dialogs tragen öffentliche FELDER, die
/// Anmeldung löst aber Eigenschaften auf. Die Liste entsteht bei jedem Zugriff neu über den
/// lebenden Zeilen — eine gehaltene zeigte den Stand von vorhin.</para>
/// <para><b>Der Setzer geht den Handweg:</b> die Aktion über <c>AktionGeaendert</c> (Namensvorschlag
/// beim Umbenennen, Name zurück sonst, Beanstandung gelöscht), der Name über <c>NameGeaendert</c>.
/// Eine Aktion, die der Befund der Zeile nicht zulässt, und ein Name ohne „Umbenennen" lehnt der
/// Setzer benannt ab — die Absage hängt an der Zeile und kann deshalb nicht im feldweisen
/// Sperrgrund stehen.</para>
/// </remarks>
public sealed class ImportKonflikteKiSicht
{
    private readonly Func<IReadOnlyList<ImportKonflikteDialog.Zeile>> _zeilen;
    private readonly Action<ImportKonflikteDialog.Zeile, int?> _aktionGeaendert;
    private readonly Action<ImportKonflikteDialog.Zeile, string> _nameGeaendert;

    /// <summary>Legt die Sicht über die Zeilen des Dialogs und seine zwei Handwege.</summary>
    public ImportKonflikteKiSicht(Func<IReadOnlyList<ImportKonflikteDialog.Zeile>> zeilen,
                                  Action<ImportKonflikteDialog.Zeile, int?> aktionGeaendert,
                                  Action<ImportKonflikteDialog.Zeile, string> nameGeaendert)
    {
        _zeilen = zeilen ?? throw new ArgumentNullException(nameof(zeilen));
        _aktionGeaendert = aktionGeaendert ?? throw new ArgumentNullException(nameof(aktionGeaendert));
        _nameGeaendert = nameGeaendert ?? throw new ArgumentNullException(nameof(nameGeaendert));
    }

    /// <summary>Die Zeilen des Rasters, Kennzeichen die Nummer ab 1.</summary>
    public IReadOnlyList<ImportKonfliktKiZeile> Zeilen
    {
        get
        {
            IReadOnlyList<ImportKonflikteDialog.Zeile> zeilen = _zeilen();
            var liste = new List<ImportKonfliktKiZeile>(zeilen.Count);
            for (int i = 0; i < zeilen.Count; i++)
                liste.Add(new ImportKonfliktKiZeile(i, zeilen[i], _aktionGeaendert, _nameGeaendert));
            return liste;
        }
    }

    /// <summary>
    /// Die Wahl der Spalte „Aktion": alle vier Aktionen; welche eine Zeile zulässt, prüft ihr Setzer.
    /// </summary>
    public IReadOnlyList<KiWahleintrag> AktionWahl => Enum.GetValues<KonfliktAktion>()
        .Select(a => new KiWahleintrag(a.ToString(), ImportKonfliktModell.AktionText(a)))
        .ToList();
}

/// <summary>Eine Zeile der Konfliktliste für den Assistenten.</summary>
public sealed class ImportKonfliktKiZeile
{
    private readonly int _index;
    private readonly ImportKonflikteDialog.Zeile _z;
    private readonly Action<ImportKonflikteDialog.Zeile, int?> _aktionGeaendert;
    private readonly Action<ImportKonflikteDialog.Zeile, string> _nameGeaendert;

    internal ImportKonfliktKiZeile(int index, ImportKonflikteDialog.Zeile z,
                                   Action<ImportKonflikteDialog.Zeile, int?> aktionGeaendert,
                                   Action<ImportKonflikteDialog.Zeile, string> nameGeaendert)
    {
        _index = index;
        _z = z;
        _aktionGeaendert = aktionGeaendert;
        _nameGeaendert = nameGeaendert;
    }

    /// <summary>Die Nummer der Zeile ab 1 — das Kennzeichen der Spalten.</summary>
    public string Nummer => (_index + 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>Die gewählte Aktion; nur eine, die der Befund der Zeile zulässt.</summary>
    public KonfliktAktion Aktion
    {
        get => _z.Aktion;
        set
        {
            int platz = _z.Erlaubt.IndexOf(value);
            if (platz < 0)
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture, Resource.KI_IKF_AKTION_NICHT_ERLAUBT,
                    string.Join(", ", _z.Erlaubt.Select(ImportKonfliktModell.AktionText))));
            _aktionGeaendert(_z, platz);
        }
    }

    /// <summary>Der Name, unter dem der Eintrag angelegt wird; setzbar nur bei „Umbenennen".</summary>
    public string Name
    {
        get => _z.Name ?? "";
        set
        {
            if (_z.Aktion != KonfliktAktion.Umbenennen)
                throw new InvalidOperationException(Resource.KI_IKF_NAME_NUR_UMBENENNEN);
            _nameGeaendert(_z, value ?? "");
        }
    }
}

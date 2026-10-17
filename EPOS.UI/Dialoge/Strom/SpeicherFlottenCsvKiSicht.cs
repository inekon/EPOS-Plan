using KiKern;
using SpeicherEngine;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Strom;

/// <summary>
/// Das FLACHE Abbild des <see cref="SpeicherFlottenCsvDialog"/> für den Hilfe-Assistenten
/// (Freigabe der Importdialoge, Teil A): die sechsundzwanzig Eingaben des CSV-Formats.
/// </summary>
/// <remarks>
/// <para><b>Warum eine Sichtklasse:</b> Der Optionssatz des Dialogs ist privat und liegt drei
/// Stufen tief (<c>Zeitbasis.Trennzeichen</c>); ein Katalogpfad trägt zwei.</para>
/// <para><b>Sie hält keinen Zustand und schreibt nicht selbst.</b> Gelesen wird der lebende
/// Optionssatz, gesetzt wird über den Delegaten <c>setzen</c> — der Dialog ruft damit DIESELBE
/// Methode wie die Klappliste (Vorschau neu, Meldung gelöscht, Zeitangabe samt Spaltenumstellung).
/// So geht ein Wert des Assistenten nie einen zweiten, stillen Weg.</para>
/// <para><b>Sperrgrund:</b> Was die Maske gerade nicht zeigt — das Spaltenpaar der anderen
/// Zeitangabe, die Prognosezuordnung beim Einlesen von Projektjahren —, lehnt
/// <see cref="Sperrgrund"/> vor der Bestätigung ab.</para>
/// </remarks>
public sealed class SpeicherFlottenCsvKiSicht
{
    private readonly Func<SpeicherFlottenCsvOptionen> _optionen;
    private readonly Func<bool> _getrennteZeit;
    private readonly Func<bool> _prognosen;
    private readonly Action<string, object?> _setzen;
    private readonly Func<string, IReadOnlyList<KiWahleintrag>> _wahl;

    /// <summary>Legt die Sicht über den lebenden Stand des Dialogs.</summary>
    /// <param name="optionen">Der Optionssatz des Dialogs.</param>
    /// <param name="getrennteZeit">Steht die Zeit in getrennten Spalten?</param>
    /// <param name="prognosen">Liest der Dialog Prognosen (sonst Projektjahre)?</param>
    /// <param name="setzen">Der Handweg je Eigenschaftsname.</param>
    /// <param name="wahl">Die Einträge der Klappliste je Eigenschaftsname.</param>
    public SpeicherFlottenCsvKiSicht(Func<SpeicherFlottenCsvOptionen> optionen, Func<bool> getrennteZeit,
                                     Func<bool> prognosen, Action<string, object?> setzen,
                                     Func<string, IReadOnlyList<KiWahleintrag>> wahl)
    {
        _optionen = optionen ?? throw new ArgumentNullException(nameof(optionen));
        _getrennteZeit = getrennteZeit ?? throw new ArgumentNullException(nameof(getrennteZeit));
        _prognosen = prognosen ?? throw new ArgumentNullException(nameof(prognosen));
        _setzen = setzen ?? throw new ArgumentNullException(nameof(setzen));
        _wahl = wahl ?? throw new ArgumentNullException(nameof(wahl));
    }

    private SpeicherFlottenCsvOptionen O => _optionen();
    private SpeicherZeitreihenOptionen Z => _optionen().Zeitbasis;

    /// <summary>Der Sperrgrund je Katalogfeld (<c>KiMaskenhaken.Sperrgrund</c>); <c>null</c> = frei.</summary>
    public string? Sperrgrund(string feld) => feld switch
    {
        "zeitstempelspalte" or "zeitstempelformat" when _getrennteZeit() => Resource.KI_FCSV_NUR_EINE_SPALTE,
        "datumsspalte" or "uhrzeitspalte" or "datumsformat" or "uhrzeitformat" when !_getrennteZeit()
            => Resource.KI_FCSV_NUR_GETRENNT,
        "snapshot_id" or "bekannt_seit" or "entscheidung" when !_prognosen() => Resource.KI_FCSV_NUR_PROGNOSEN,
        _ => null
    };

    /// <summary>Steht in der ersten Datenzeile eine Kopfzeile?</summary>
    public bool Kopfzeile
    {
        get => Z.Kopfzeile;
        set => _setzen(nameof(Kopfzeile), value);
    }

    /// <summary>Wie viele Zeilen vor den Daten übergangen werden.</summary>
    public int ZeilenUeberspringen
    {
        get => Z.ZuUeberspringendeZeilen;
        set => _setzen(nameof(ZeilenUeberspringen), value);
    }

    /// <summary>Die Wahl des Feldes <c>Trennzeichen</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> TrennzeichenWahl => _wahl(nameof(Trennzeichen));

    /// <summary>Der Platz der Klappliste <c>Trennzeichen</c>.</summary>
    public int Trennzeichen
    {
        get => Z.Trennzeichen switch { ',' => 1, '\t' => 2, _ => 0 };
        set => _setzen(nameof(Trennzeichen), value);
    }

    /// <summary>Die Wahl des Feldes <c>Dezimaltrenner</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> DezimaltrennerWahl => _wahl(nameof(Dezimaltrenner));

    /// <summary>Der Platz der Klappliste <c>Dezimaltrenner</c>.</summary>
    public int Dezimaltrenner
    {
        get => Z.Dezimaltrenner == '.' ? 1 : 0;
        set => _setzen(nameof(Dezimaltrenner), value);
    }

    /// <summary>Die Wahl des Feldes <c>Kodierung</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> KodierungWahl => _wahl(nameof(Kodierung));

    /// <summary>Der Platz der Klappliste <c>Kodierung</c>.</summary>
    public int Kodierung
    {
        get => (int)Z.Encoding;
        set => _setzen(nameof(Kodierung), value);
    }

    /// <summary>Die Wahl des Feldes <c>Zeitangabe</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> ZeitangabeWahl => _wahl(nameof(Zeitangabe));

    /// <summary>Der Platz der Klappliste <c>Zeitangabe</c>.</summary>
    public int Zeitangabe
    {
        get => _getrennteZeit() ? 1 : 0;
        set => _setzen(nameof(Zeitangabe), value);
    }

    /// <summary>Die Wahl des Feldes <c>Zeitstempelspalte</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> ZeitstempelspalteWahl => _wahl(nameof(Zeitstempelspalte));

    /// <summary>Der Platz der Klappliste <c>Zeitstempelspalte</c>.</summary>
    public int Zeitstempelspalte
    {
        get => Z.ZeitstempelSpalte;
        set => _setzen(nameof(Zeitstempelspalte), value);
    }

    /// <summary>Die Wahl des Feldes <c>Datumsspalte</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> DatumsspalteWahl => _wahl(nameof(Datumsspalte));

    /// <summary>Der Platz der Klappliste <c>Datumsspalte</c>.</summary>
    public int Datumsspalte
    {
        get => Z.DatumSpalte;
        set => _setzen(nameof(Datumsspalte), value);
    }

    /// <summary>Die Wahl des Feldes <c>Uhrzeitspalte</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> UhrzeitspalteWahl => _wahl(nameof(Uhrzeitspalte));

    /// <summary>Der Platz der Klappliste <c>Uhrzeitspalte</c>.</summary>
    public int Uhrzeitspalte
    {
        get => Z.UhrzeitSpalte;
        set => _setzen(nameof(Uhrzeitspalte), value);
    }

    /// <summary>Die Wahl des Feldes <c>Intervall</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> IntervallWahl => _wahl(nameof(Intervall));

    /// <summary>Der Platz der Klappliste <c>Intervall</c>.</summary>
    public int Intervall
    {
        get => (int)Z.Konvention;
        set => _setzen(nameof(Intervall), value);
    }

    /// <summary>Die Wahl des Feldes <c>Last</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> LastWahl => _wahl(nameof(Last));

    /// <summary>Der Platz der Klappliste <c>Last</c>.</summary>
    public int Last
    {
        get => O.LastSpalte;
        set => _setzen(nameof(Last), value);
    }

    /// <summary>Die Wahl des Feldes <c>Pv</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> PvWahl => _wahl(nameof(Pv));

    /// <summary>Der Platz der Klappliste <c>Pv</c>.</summary>
    public int Pv
    {
        get => O.PvSpalte;
        set => _setzen(nameof(Pv), value);
    }

    /// <summary>Die Wahl des Feldes <c>Bhkw</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> BhkwWahl => _wahl(nameof(Bhkw));

    /// <summary>Der Platz der Klappliste <c>Bhkw</c>.</summary>
    public int Bhkw
    {
        get => O.BhkwSpalte;
        set => _setzen(nameof(Bhkw), value);
    }

    /// <summary>Die Wahl des Feldes <c>Bezug</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> BezugWahl => _wahl(nameof(Bezug));

    /// <summary>Der Platz der Klappliste <c>Bezug</c>.</summary>
    public int Bezug
    {
        get => O.BezugSpalte;
        set => _setzen(nameof(Bezug), value);
    }

    /// <summary>Die Wahl des Feldes <c>PvPreis</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> PvPreisWahl => _wahl(nameof(PvPreis));

    /// <summary>Der Platz der Klappliste <c>PvPreis</c>.</summary>
    public int PvPreis
    {
        get => O.PvPreisSpalte;
        set => _setzen(nameof(PvPreis), value);
    }

    /// <summary>Die Wahl des Feldes <c>BhkwPreis</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> BhkwPreisWahl => _wahl(nameof(BhkwPreis));

    /// <summary>Der Platz der Klappliste <c>BhkwPreis</c>.</summary>
    public int BhkwPreis
    {
        get => O.BhkwPreisSpalte;
        set => _setzen(nameof(BhkwPreis), value);
    }

    /// <summary>Die Wahl des Feldes <c>BatteriePreis</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> BatteriePreisWahl => _wahl(nameof(BatteriePreis));

    /// <summary>Der Platz der Klappliste <c>BatteriePreis</c>.</summary>
    public int BatteriePreis
    {
        get => O.BatteriePreisSpalte;
        set => _setzen(nameof(BatteriePreis), value);
    }

    /// <summary>Die Wahl des Feldes <c>LeistungEinheit</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> LeistungEinheitWahl => _wahl(nameof(LeistungEinheit));

    /// <summary>Der Platz der Klappliste <c>LeistungEinheit</c>.</summary>
    public int LeistungEinheit
    {
        get => (int)O.LeistungEinheit;
        set => _setzen(nameof(LeistungEinheit), value);
    }

    /// <summary>Die Wahl des Feldes <c>PreisEinheit</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> PreisEinheitWahl => _wahl(nameof(PreisEinheit));

    /// <summary>Der Platz der Klappliste <c>PreisEinheit</c>.</summary>
    public int PreisEinheit
    {
        get => (int)O.PreisEinheit;
        set => _setzen(nameof(PreisEinheit), value);
    }

    /// <summary>Die Wahl des Feldes <c>SnapshotId</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> SnapshotIdWahl => _wahl(nameof(SnapshotId));

    /// <summary>Der Platz der Klappliste <c>SnapshotId</c>.</summary>
    public int SnapshotId
    {
        get => O.SnapshotIdSpalte;
        set => _setzen(nameof(SnapshotId), value);
    }

    /// <summary>Die Wahl des Feldes <c>BekanntSeit</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> BekanntSeitWahl => _wahl(nameof(BekanntSeit));

    /// <summary>Der Platz der Klappliste <c>BekanntSeit</c>.</summary>
    public int BekanntSeit
    {
        get => O.BekanntSeitSpalte;
        set => _setzen(nameof(BekanntSeit), value);
    }

    /// <summary>Die Wahl des Feldes <c>Entscheidung</c> — dieselbe Liste wie die Klappliste der Maske.</summary>
    public IReadOnlyList<KiWahleintrag> EntscheidungWahl => _wahl(nameof(Entscheidung));

    /// <summary>Der Platz der Klappliste <c>Entscheidung</c>.</summary>
    public int Entscheidung
    {
        get => O.EntscheidungSpalte;
        set => _setzen(nameof(Entscheidung), value);
    }

    /// <summary>Das Textfeld <c>Zeitstempelformat</c>.</summary>
    public string Zeitstempelformat
    {
        get => Z.ZeitstempelFormat ?? "";
        set => _setzen(nameof(Zeitstempelformat), value ?? "");
    }

    /// <summary>Das Textfeld <c>Datumsformat</c>.</summary>
    public string Datumsformat
    {
        get => Z.DatumFormat ?? "";
        set => _setzen(nameof(Datumsformat), value ?? "");
    }

    /// <summary>Das Textfeld <c>Uhrzeitformat</c>.</summary>
    public string Uhrzeitformat
    {
        get => Z.UhrzeitFormat ?? "";
        set => _setzen(nameof(Uhrzeitformat), value ?? "");
    }

    /// <summary>Das Textfeld <c>Zeitzone</c>.</summary>
    public string Zeitzone
    {
        get => Z.ZeitzoneId ?? "";
        set => _setzen(nameof(Zeitzone), value ?? "");
    }
}

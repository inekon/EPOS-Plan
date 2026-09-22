using KiKern;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Dialoge.Strom;

/// <summary>
/// Das FLACHE Abbild der Maske „Zeitreihe einlesen" für den Hilfe-Assistenten
/// (Welle KI‑F6).
///
/// <para><b>Die Maske stellt LESEREGELN ein, sie lädt nicht.</b> Der Ladevorgang
/// selbst — die Datei zu wählen und sie zu übernehmen — bleibt nach KI‑D‑Q6
/// draußen; was hier steht, sind die sechzehn Einstellwerte, mit denen die
/// Vorschau gelesen wird. Ändert der Assistent einen davon, rechnet die Maske
/// ihre Vorschau neu, genau wie nach einem Griff in die Klappliste.</para>
///
/// <para><b>Warum eine Sichtklasse und nicht <c>SpeicherZeitreihenOptionen</c>
/// selbst.</b> Drei Gründe, alle an der Maske ablesbar: Trennzeichen und
/// Dezimaltrenner sind dort <c>char</c>, die Maske zeigt sie als KLAPPLISTE mit
/// festem Platz; die Zeitangabe („eine Zeitstempelspalte" gegen „getrennte
/// Datums- und Uhrzeitspalten") ist überhaupt kein Feld der Optionen, sondern
/// ein privater Schalter der Komponente, der drei Spaltennummern gleichzieht;
/// und jedes Setzen muss die VORSCHAU nachziehen — ein unmittelbar
/// angemeldetes Optionsobjekt zeigte die Tabelle von vorhin.</para>
///
/// <para><b>Sie hält keinen Zustand</b>: Jede Eigenschaft greift bei jedem
/// Zugriff auf den lebenden Optionssatz der offenen Maske.</para>
/// </summary>
public sealed class SpeicherZeitreihenKiSicht
{
    private readonly Func<SpeicherZeitreihenOptionen> _optionen;
    private readonly Func<bool> _getrennteZeit;
    private readonly Action<bool> _zeitartSetzen;
    private readonly Action _geaendert;
    private readonly Func<IReadOnlyList<KiWahleintrag>> _spalten;
    private readonly Func<IReadOnlyList<KiWahleintrag>> _einheiten;
    private readonly Func<string> _datei;

    /// <summary>Legt die Sicht über die lebenden Stände der Maske.</summary>
    /// <param name="optionen">Der Leseregelsatz der offenen Maske; nie <c>null</c>.</param>
    /// <param name="getrennteZeit">Steht die Zeitangabe auf getrennten Spalten?</param>
    /// <param name="zeitartSetzen">Stellt die Zeitangabe um — samt der drei Spaltennummern.</param>
    /// <param name="geaendert">Rechnet die Vorschau neu; derselbe Weg wie nach einer Eingabe.</param>
    /// <param name="spalten">Die Spalten der Vorschau, wie die Klapplisten sie führen.</param>
    /// <param name="einheiten">Die Einheiten — sie hängen an der ROLLE der Reihe.</param>
    /// <param name="datei">Der Dateiname aus der Herleitungszeile.</param>
    public SpeicherZeitreihenKiSicht(Func<SpeicherZeitreihenOptionen> optionen,
                                     Func<bool> getrennteZeit,
                                     Action<bool> zeitartSetzen,
                                     Action geaendert,
                                     Func<IReadOnlyList<KiWahleintrag>> spalten,
                                     Func<IReadOnlyList<KiWahleintrag>> einheiten,
                                     Func<string> datei)
    {
        _optionen = optionen ?? throw new ArgumentNullException(nameof(optionen));
        _getrennteZeit = getrennteZeit ?? throw new ArgumentNullException(nameof(getrennteZeit));
        _zeitartSetzen = zeitartSetzen ?? throw new ArgumentNullException(nameof(zeitartSetzen));
        _geaendert = geaendert ?? throw new ArgumentNullException(nameof(geaendert));
        _spalten = spalten ?? throw new ArgumentNullException(nameof(spalten));
        _einheiten = einheiten ?? throw new ArgumentNullException(nameof(einheiten));
        _datei = datei ?? throw new ArgumentNullException(nameof(datei));
    }

    // =====================================================================
    //  Die festen Klapplisten — je einmal, damit Maske und Assistent nicht
    //  auseinanderlaufen können.
    // =====================================================================

    /// <summary>Die drei Trennzeichen, in der Reihenfolge der Klappliste.</summary>
    public static readonly IReadOnlyList<KiWahleintrag> TrennzeichenEintraege = new[]
    {
        new KiWahleintrag("0", "Semikolon (;)"),
        new KiWahleintrag("1", "Komma (,)"),
        new KiWahleintrag("2", "Tabulator")
    };

    /// <summary>Die zwei Dezimaltrenner.</summary>
    public static readonly IReadOnlyList<KiWahleintrag> DezimaltrennerEintraege = new[]
    {
        new KiWahleintrag("0", "Komma (,)"),
        new KiWahleintrag("1", "Punkt (.)")
    };

    /// <summary>Die zwei Kodierungen — Schlüssel sind die Namen der Aufzählung.</summary>
    public static readonly IReadOnlyList<KiWahleintrag> KodierungEintraege = new[]
    {
        new KiWahleintrag(nameof(SpeicherZeitreihenEncoding.Utf8), "UTF-8"),
        new KiWahleintrag(nameof(SpeicherZeitreihenEncoding.Windows1252), "Windows-1252")
    };

    /// <summary>Die zwei Formen der Zeitangabe.</summary>
    public static readonly IReadOnlyList<KiWahleintrag> ZeitartEintraege = new[]
    {
        new KiWahleintrag("0", "Eine Zeitstempelspalte"),
        new KiWahleintrag("1", "Getrennte Datums- und Uhrzeitspalten")
    };

    /// <summary>
    /// Was der Zeitstempel bezeichnet — in der Reihenfolge der Aufzählung, also
    /// „automatisch erkennen" zuerst.
    /// </summary>
    /// <remarks>
    /// <b>Kein <c>static readonly</c>:</b> Die Anzeigetexte kommen aus den Ressourcen,
    /// und die Sprache wechselt zur Laufzeit; ein Feld hielte den Text des ersten
    /// Zugriffs fest. Die Schlüssel bleiben die Namen der Aufzählung — sie sind
    /// sprachneutral und gehen so auch in einen englischen Chat.
    /// </remarks>
    public static IReadOnlyList<KiWahleintrag> KonventionEintraege => new[]
    {
        new KiWahleintrag(nameof(SpeicherEngine.IntervallKonvention.Automatisch),
                          Resource.IMPORT_KONV_AUTO),
        new KiWahleintrag(nameof(SpeicherEngine.IntervallKonvention.Anfang),
                          Resource.IMPORT_KONV_ANFANG),
        new KiWahleintrag(nameof(SpeicherEngine.IntervallKonvention.Ende),
                          Resource.IMPORT_KONV_ENDE)
    };

    // =====================================================================
    //  Was der Katalog sieht
    // =====================================================================

    /// <summary>Die eingelesene Datei — Anzeige.</summary>
    public string Datei => _datei() ?? "";

    /// <summary>Die Rolle der Reihe (Last, PV, Bezugspreis) — Anzeige.</summary>
    public string Rolle => _optionen().Rolle.ToString();

    /// <summary>Die Trennzeichen der Klappliste.</summary>
    public IReadOnlyList<KiWahleintrag> TrennzeichenWahl => TrennzeichenEintraege;

    /// <summary>Das Feldtrennzeichen der Datei, als Platz der Klappliste.</summary>
    public int Trennzeichen
    {
        get => _optionen().Trennzeichen switch { ',' => 1, '\t' => 2, _ => 0 };
        set
        {
            _optionen().Trennzeichen = value switch { 1 => ',', 2 => '\t', _ => ';' };
            _geaendert();
        }
    }

    /// <summary>Die Dezimaltrenner der Klappliste.</summary>
    public IReadOnlyList<KiWahleintrag> DezimaltrennerWahl => DezimaltrennerEintraege;

    /// <summary>Das Dezimalzeichen der Zahlen, als Platz der Klappliste.</summary>
    public int Dezimaltrenner
    {
        get => _optionen().Dezimaltrenner == '.' ? 1 : 0;
        set
        {
            _optionen().Dezimaltrenner = value == 1 ? '.' : ',';
            _geaendert();
        }
    }

    /// <summary>Die Kodierungen der Klappliste.</summary>
    public IReadOnlyList<KiWahleintrag> KodierungWahl => KodierungEintraege;

    /// <summary>Die Zeichenkodierung der Datei.</summary>
    public SpeicherZeitreihenEncoding Kodierung
    {
        get => _optionen().Encoding;
        set { _optionen().Encoding = value; _geaendert(); }
    }

    /// <summary>Trägt die erste Datenzeile die Spaltennamen?</summary>
    public bool Kopfzeile
    {
        get => _optionen().Kopfzeile;
        set { _optionen().Kopfzeile = value; _geaendert(); }
    }

    /// <summary>Zeilen, die vor der Kopfzeile übersprungen werden.</summary>
    public int ZeilenUeberspringen
    {
        get => _optionen().ZuUeberspringendeZeilen;
        set { _optionen().ZuUeberspringendeZeilen = value; _geaendert(); }
    }

    /// <summary>Die zwei Formen der Zeitangabe.</summary>
    public IReadOnlyList<KiWahleintrag> ZeitangabeWahl => ZeitartEintraege;

    /// <summary>
    /// Steht die Zeit in EINER Spalte (0) oder in Datum und Uhrzeit getrennt (1)?
    /// </summary>
    /// <remarks>
    /// Sie umzustellen zieht drei Spaltennummern gleich — genau das tut auch der
    /// Griff in die Klappliste; ein Setzer, der nur den Schalter legte, ließe die
    /// Maske mit widersprüchlichen Spalten stehen.
    /// </remarks>
    public int Zeitangabe
    {
        get => _getrennteZeit() ? 1 : 0;
        set => _zeitartSetzen(value == 1);
    }

    /// <summary>Die Spalten der Vorschau.</summary>
    public IReadOnlyList<KiWahleintrag> ZeitstempelspalteWahl => _spalten();

    /// <summary>Die Spalte mit dem vollständigen Zeitstempel.</summary>
    public int Zeitstempelspalte
    {
        get => _optionen().ZeitstempelSpalte;
        set { _optionen().ZeitstempelSpalte = value; _geaendert(); }
    }

    /// <summary>Das Format des Zeitstempels; leer = Standard.</summary>
    public string Zeitstempelformat
    {
        get => _optionen().ZeitstempelFormat ?? "";
        set { _optionen().ZeitstempelFormat = value ?? ""; _geaendert(); }
    }

    /// <summary>Die Spalten der Vorschau.</summary>
    public IReadOnlyList<KiWahleintrag> DatumsspalteWahl => _spalten();

    /// <summary>Die Spalte mit dem Datum.</summary>
    public int Datumsspalte
    {
        get => _optionen().DatumSpalte;
        set { _optionen().DatumSpalte = value; _geaendert(); }
    }

    /// <summary>Das Format des Datums; leer = Standard.</summary>
    public string Datumsformat
    {
        get => _optionen().DatumFormat ?? "";
        set { _optionen().DatumFormat = value ?? ""; _geaendert(); }
    }

    /// <summary>Die Spalten der Vorschau.</summary>
    public IReadOnlyList<KiWahleintrag> UhrzeitspalteWahl => _spalten();

    /// <summary>Die Spalte mit der Uhrzeit.</summary>
    public int Uhrzeitspalte
    {
        get => _optionen().UhrzeitSpalte;
        set { _optionen().UhrzeitSpalte = value; _geaendert(); }
    }

    /// <summary>Das Format der Uhrzeit; leer = Standard.</summary>
    public string Uhrzeitformat
    {
        get => _optionen().UhrzeitFormat ?? "";
        set { _optionen().UhrzeitFormat = value ?? ""; _geaendert(); }
    }

    /// <summary>Die Spalten der Vorschau.</summary>
    public IReadOnlyList<KiWahleintrag> WertspalteWahl => _spalten();

    /// <summary>Die Spalte mit dem Messwert.</summary>
    public int Wertspalte
    {
        get => _optionen().WertSpalte;
        set { _optionen().WertSpalte = value; _geaendert(); }
    }

    /// <summary>Die Zeitzone, in der die Zeitangaben der Datei stehen.</summary>
    public string Zeitzone
    {
        get => _optionen().ZeitzoneId ?? "";
        set { _optionen().ZeitzoneId = value ?? ""; _geaendert(); }
    }

    /// <summary>Anfang oder Ende des Intervalls.</summary>
    public IReadOnlyList<KiWahleintrag> IntervallWahl => KonventionEintraege;

    /// <summary>Was der Zeitstempel einer Zeile bezeichnet.</summary>
    public SpeicherEngine.IntervallKonvention Intervall
    {
        get => _optionen().Konvention;
        set { _optionen().Konvention = value; _geaendert(); }
    }

    /// <summary>Die Einheiten — sie wechseln mit der Rolle der Reihe.</summary>
    public IReadOnlyList<KiWahleintrag> EinheitWahl => _einheiten();

    /// <summary>Die Einheit der Werte in der Datei.</summary>
    public SpeicherZeitreihenEinheit Einheit
    {
        get => _optionen().Einheit;
        set { _optionen().Einheit = value; _geaendert(); }
    }
}

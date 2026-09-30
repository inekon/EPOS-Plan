using EPOS.UI.Dienste;
using KiKern;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Vorgabe-Matrix als FELDTAFEL des Assistenten</b> (Stufe KP2, Wellen U1 und U4) — EIN Weg für
/// die Sichtklassen, die die Felder des Profils <c>KiKonditionierungsfelder</c> beantworten: der
/// Gebäude-Katalogeditor und die Gebäudeverwaltung (<see cref="GebaeudeKatalogKiSicht"/>, Karte
/// <c>Alle</c>) und der Zonendialog (<see cref="ZonenKiSicht"/>, Karte <c>Zonenfelder</c>). Gelesen und
/// gesetzt wird über die <see cref="KonditionierungBearbeitung"/> des Wirts — derselbe Weg wie die
/// Zellen der Matrix, mit Zellenort, Folgeregel und Rückfrage; die Vorlage einer Größe
/// (<c>kond_&lt;größe&gt;_vorlage</c>, Welle U2) mit der Aktion des Knopfs „Übernehmen".
/// </summary>
/// <remarks>
/// Ein Zeitfenster hat nur beide Grenzen zusammen (E53, E43): Die erste gesetzte Grenze wartet in der
/// Tafel auf die zweite — erst beide oder keine gehen an den Weg, wie im Reiter. Das ist der einzige
/// Zustand der Tafel.
/// </remarks>
public sealed class KonditionierungKiTafel
{
    private readonly Func<string, KiKonditionierungsfelder.Feld?> _finde;

    /// <summary>Die halbe Angabe eines Zeitfensters (Nachtfenster, Saison), bis beide Grenzen stehen.</summary>
    private readonly Dictionary<string, (int? Von, int? Bis)> _halb = new(StringComparer.Ordinal);

    /// <param name="finde">Die Karte: <c>KiKonditionierungsfelder.Finde</c> bzw. <c>FindeZone</c>.</param>
    public KonditionierungKiTafel(Func<string, KiKonditionierungsfelder.Feld?> finde) => _finde = finde;

    /// <summary>
    /// <b>Die Wahlquellen der Vorlagefelder</b> (<c>kond_&lt;größe&gt;_vorlage</c>, Welle U2): je Größe die
    /// Liste der Karte, Schlüssel und Text der Name — derselbe, den das Feld liest; gefragt bei jedem
    /// Zugriff, denn „Als Vorlage speichern…" und die Vorlagenverwaltung ändern die Listen, solange der
    /// Wirt offen steht. Die Tafel kennt keine Begleiteigenschaft, deshalb meldet der Wirt die Listen mit
    /// an — der Katalogeditor und die Gebäudeverwaltung (Welle U4) mit derselben Liste.
    /// </summary>
    public static (string Feld, Func<IReadOnlyList<KiWahleintrag>> Eintraege)[] Vorlagenlisten(
        Func<KonditionierungBearbeitung> bearbeitung)
        => KiKonditionierungsfelder.Alle
            .Where(f => f.Teil == KiKonditionierungsfelder.Teil.Vorlage)
            .Select(f => (f.Schluessel, (Func<IReadOnlyList<KiWahleintrag>>)(() => KiMaskenanmeldung.Eintraege(
                bearbeitung().Vorlagen((KonditionierungGroesse)f.Groessenplatz), v => v.Name, v => v.Name))))
            .ToArray();

    /// <summary>Die Ablehnung einer Handlung — mit dem Grund, den der Reiter nennt.</summary>
    public static InvalidOperationException Ablehnung(KonditionierungBearbeitung b)
        => new(b.OffeneFrage?.Text is { Length: > 0 } frage ? frage
               : b.LetzteMeldung is { Length: > 0 } m ? m
               : b.Sperrgrund ?? b.Texte.GrundOhneTabellen);

    /// <summary>
    /// Liest ein Feld der Karte: der Wert der Zelle (leer = <c>null</c>; an einer Zone „wie Gebäude"),
    /// „aus", eine Grenze eines Zeitfensters oder ΔT; <c>null</c> ohne Bearbeitung oder ohne Feld.
    /// </summary>
    public object? Lesen(KonditionierungBearbeitung? b, string schluessel)
    {
        if (b is null || _finde(schluessel) is not KiKonditionierungsfelder.Feld f) return null;
        var g = (KonditionierungGroesse)f.Groessenplatz;
        var z = (KonditionierungZeile)f.Zeilenplatz;
        switch (f.Teil)
        {
            case KiKonditionierungsfelder.Teil.Aus:
                return b.Wert(g, z) is double a && double.IsNaN(a);
            case KiKonditionierungsfelder.Teil.Von:
                return _halb.TryGetValue(Fenster(schluessel), out var hv) ? hv.Von : b.Zeiten(g, z).Von;
            case KiKonditionierungsfelder.Teil.Bis:
                return _halb.TryGetValue(Fenster(schluessel), out var hb) ? hb.Bis : b.Zeiten(g, z).Bis;
            case KiKonditionierungsfelder.Teil.DeltaT:
                return b.DeltaT;
            case KiKonditionierungsfelder.Teil.Vorlage:
                return b.Herkunft(g);
            case KiKonditionierungsfelder.Teil.Woche:
                return Wochentext(b.Kalender(g));
            default:
                return b.Wert(g, z) is double w && double.IsFinite(w) ? w : null;
        }
    }

    /// <summary>
    /// Setzt ein Feld der Karte über die Bearbeitung; eine abgelehnte Handlung wirft mit dem Grund des
    /// Reiters (<see cref="Ablehnung"/>), ohne Bearbeitung mit dem Grund „ohne Tabellen".
    /// </summary>
    public void Setzen(KonditionierungBearbeitung? b, string schluessel, object? wert)
    {
        if (_finde(schluessel) is not KiKonditionierungsfelder.Feld f)
            throw new InvalidOperationException(schluessel);
        if (b is null)
            throw new InvalidOperationException(new KonditionierungTexte().GrundOhneTabellen);
        var g = (KonditionierungGroesse)f.Groessenplatz;
        var z = (KonditionierungZeile)f.Zeilenplatz;
        bool ok;
        switch (f.Teil)
        {
            case KiKonditionierungsfelder.Teil.Aus:
                bool aus = wert is bool an && an;
                bool istAus = b.Wert(g, z) is double a && double.IsNaN(a);
                if (aus == istAus) return;
                ok = b.WertSetzen(g, z, aus ? double.NaN : null);
                break;
            case KiKonditionierungsfelder.Teil.Von:
            case KiKonditionierungsfelder.Teil.Bis:
                string fenster = Fenster(schluessel);
                (int? von, int? bis) = _halb.TryGetValue(fenster, out var halb) ? halb : b.Zeiten(g, z);
                int? neu = wert is int n ? n : null;
                if (f.Teil == KiKonditionierungsfelder.Teil.Von) von = neu; else bis = neu;
                if (von.HasValue != bis.HasValue)
                {
                    _halb[fenster] = (von, bis);
                    return;
                }
                _halb.Remove(fenster);
                ok = b.ZeitenSetzen(g, z, von, bis);
                break;
            case KiKonditionierungsfelder.Teil.DeltaT:
                ok = b.DeltaTSetzen(wert as double?);
                break;
            case KiKonditionierungsfelder.Teil.Vorlage:
                VorlageUebernehmen(b, g, wert as string);
                return;
            case KiKonditionierungsfelder.Teil.Woche:
                ok = WocheSetzen(b, g, wert as string);
                break;
            default:
                ok = b.WertSetzen(g, z, wert as double?);
                break;
        }
        if (!ok) throw Ablehnung(b);
    }

    /// <summary>
    /// <b>Die Vorlage einer Größe</b> (Welle U2; Entwurf KP2 D9) — mit der Aktion des Knopfs
    /// „Übernehmen": wählen und übernehmen, in den Arbeitsstand; geschrieben wird mit dem OK bzw. dem
    /// „Speichern" des Wirts. Ein Name, den die Liste der Karte nicht führt, wird benannt abgelehnt. Eine
    /// Rückfrage (P12 am angelegten Kalender, „aufteilen" an der Gesamtangabe der Lüftung) beantwortet der
    /// Assistent nicht selbst: Sie steht danach im Reiter, und die Ablehnung nennt sie.
    /// </summary>
    private static void VorlageUebernehmen(KonditionierungBearbeitung b, KonditionierungGroesse g, string? name)
    {
        System.Globalization.CultureInfo c = System.Globalization.CultureInfo.CurrentCulture;
        string feld = KonditionierungBearbeitung.Groessenname(b.Texte, g) + " · "
                      + WindowsFormsApplication1.MyResource.Resource.KOND_LBL_ZEILE_VORLAGE;
        IReadOnlyList<KonditionierungVorlageDaten> liste = b.Vorlagen(g);
        if (!b.MitVorlagen || liste.Count == 0)
            throw new InvalidOperationException(
                b.Sperrgrund ?? string.Format(c, WindowsFormsApplication1.MyResource.Resource.KI_FELD_WAHL_LEER, feld));
        string gesucht = (name ?? "").Trim();
        KonditionierungVorlageDaten? v = liste.FirstOrDefault(x => string.Equals(x.Name, gesucht, StringComparison.Ordinal))
                                         ?? liste.FirstOrDefault(x => string.Equals(x.Name, gesucht, StringComparison.OrdinalIgnoreCase));
        if (v is null)
            throw new InvalidOperationException(string.Format(
                c, WindowsFormsApplication1.MyResource.Resource.KI_FELD_WAHL_UNBEKANNT, feld, gesucht,
                string.Join(", ", liste.Select(x => x.Name))));
        if (!b.VorlageWaehlen(g, v.Id) || !b.VorlageUebernehmen(g) || b.OffeneFrage is not null
            || !string.Equals(b.Herkunft(g), v.Name, StringComparison.Ordinal))
            throw Ablehnung(b);
    }

    private static string Fenster(string schluessel) => schluessel[..schluessel.LastIndexOf('_')];

    // ------------------------------------------------------------------ Die Woche als Text (Welle U3)

    /// <summary>Das Kennwort „aus" in der Woche als Text — invariant, wie in der gespeicherten Woche.</summary>
    private const string AUS = "aus";

    /// <summary>
    /// <b>Die Woche des angelegten Kalenders als Text</b> (<c>kond_&lt;größe&gt;_woche</c>): 168 Werte in der
    /// Einheit der Spalte, durch „;" getrennt, „aus" für abgeschaltet; ohne Woche die Grundangabe als ein Wert;
    /// <c>null</c> ohne angelegten Kalender.
    /// </summary>
    public static string? Wochentext(KonditionierungKalender? k)
    {
        if (k is null || k.Zustand != KonditionierungZustand.Angelegt) return null;
        return k.Angabe switch
        {
            KonditionierungAngabe.Woche when k.Woche is { Length: 168 } w => string.Join(";", w.Select(Zahltext)),
            KonditionierungAngabe.Aus => AUS,
            KonditionierungAngabe.Wert when k.Wert is double v => Zahltext(v),
            _ => null
        };
    }

    private static string Zahltext(double v)
        => double.IsNaN(v) ? AUS : v.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>
    /// Setzt die Woche aus Text über die Bearbeitung: 168 Werte → Standardwoche, ein Wert (oder „aus") →
    /// Grundangabe, leer → die Woche fällt zugunsten der Grundangabe. Jede andere Zahl von Werten und jeder
    /// unlesbare Wert wird benannt abgelehnt.
    /// </summary>
    private static bool WocheSetzen(KonditionierungBearbeitung b, KonditionierungGroesse g, string? text)
    {
        string[] teile = (text ?? "").Split(new[] { ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (teile.Length == 0)
            return b.Kalender(g)?.Angabe != KonditionierungAngabe.Woche || b.StandardwocheSetzen(g, null);
        var werte = new double[teile.Length];
        for (int i = 0; i < teile.Length; i++)
        {
            if (string.Equals(teile[i], AUS, StringComparison.OrdinalIgnoreCase)
                || string.Equals(teile[i], b.Texte.ZelleAus, StringComparison.OrdinalIgnoreCase))
                werte[i] = double.NaN;
            else if (EPOS.UI.Standards.Zahlen.ZahlParsen(teile[i], out double v))
                werte[i] = v;
            else
                throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                    WindowsFormsApplication1.MyResource.Resource.KOND_TXT_KI_WOCHE_FORMAT, teile.Length));
        }
        if (werte.Length == 1) return b.GrundangabeSetzen(g, double.IsNaN(werte[0]) ? null : werte[0]);
        if (werte.Length != 168)
            throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture,
                WindowsFormsApplication1.MyResource.Resource.KOND_TXT_KI_WOCHE_FORMAT, werte.Length));
        return b.StandardwocheSetzen(g, werte);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// Was das Umschalten des Auslieferungskennzeichens ergeben hat — das Abbild von
/// <c>Auslieferungskennzeichen.Ergebnis</c> auf der Oberflächenseite.
/// </summary>
/// <param name="Ok">Wurde geschrieben? Sonst ist nichts geändert.</param>
/// <param name="Meldung">Der Grund im Klartext, bereits lokalisiert (nur ohne <paramref name="Ok"/>).</param>
public sealed record SchlossErgebnis(bool Ok, string Meldung)
{
    /// <summary>Die Sätze, deren Schloss umgeschaltet wurde.</summary>
    public IReadOnlyList<int> Geaendert { get; init; } = Array.Empty<int>();

    /// <summary>Die Sätze, die schon im Zielzustand standen.</summary>
    public IReadOnlyList<int> Unveraendert { get; init; } = Array.Empty<int>();
}

/// <summary>
/// <b>Der Weg, den die Hülle für „Schloss setzen…" / „Schloss aufheben…" hereinreicht</b>
/// (Entscheid AD-Q15) — der Einzeiler des Stamm-Controllers und die Frage nach dem
/// Lesemodus der Lizenz. Kein Weg, keine Handlung (Hausregel „kein Delegat, kein Knopf").
/// </summary>
/// <param name="Setzen">Schaltet die Sätze (IDs) auf gesperrt (<c>true</c>) oder eigen (<c>false</c>).</param>
public sealed record Schlossweg(Func<IReadOnlyList<int>, bool, SchlossErgebnis> Setzen)
{
    /// <summary>
    /// Steht die Datenbank im Lesemodus der Lizenz? Dann ist die Handlung HART gesperrt.
    /// Die Frage stellt die Hülle (<c>Schreibnaht.DarfSchreiben</c>), nicht die Ansicht —
    /// eine Razor-Komponente stellt keine Lizenzfragen. <c>null</c> = keine Sperre.
    /// </summary>
    public Func<bool>? Lesemodus { get; init; }
}

/// <summary>
/// Der Ausgang einer beantworteten Rückfrage: ob geschrieben wurde, was die Statuszeile
/// sagt und was das Warnband meldet.
/// </summary>
public sealed record Schlossausgang(bool Geschrieben, string Status, string Fehler)
{
    /// <summary>Nein, Esc oder nichts zu tun — nichts geschrieben, nichts zu melden.</summary>
    public static readonly Schlossausgang Nichts = new(false, "", "");
}

/// <summary>
/// <b>„Schloss setzen…" / „Schloss aufheben…" — die eine Handlung der zehn Verwaltungen</b>
/// (Konzept Administrationsdialoge, Entscheid <b>AD-Q15</b>): das Auslieferungskennzeichen
/// der gewählten Sätze umschalten, nach Rückfrage, über die Auswahlleiste.
///
/// <para><b>Warum eine Klasse und nicht zehn Fassungen.</b> Dieselben Regeln gelten in jeder
/// Verwaltung: Die Beschriftung folgt der Auswahl (enthält sie gesperrte Sätze, heißt die
/// Handlung „Schloss aufheben…" und wirkt nur auf diese, sonst „Schloss setzen…"); im
/// Lesemodus der Lizenz und bei <c>NurLesen</c> ist sie hart gesperrt; die Rückfrage kommt
/// in beiden Richtungen mit „Nein" als Vorgabe; nach dem Schalten nennt die Statuszeile, was
/// geschah. Und der Wirt merkt sich, welche Sätze er in dieser Sitzung entsperrt hat — ihr
/// Stammblatt trägt dann das Band <c>ADM_SB_ENTSPERRT</c> (kein Schemaschritt: die Datenbank
/// kennt nur das Kennzeichen, nicht seine Geschichte).</para>
///
/// <para><b>Was der Wirt noch tut:</b> die Handlung zwischen „Duplizieren…" und „Löschen"
/// einreihen, die <c>Rueckfrage</c> zeichnen (<see cref="FrageOffen"/>, <see cref="Titel"/>,
/// <see cref="Frage"/>, <c>VorgabeNein</c>), nach einem Ausgang mit
/// <see cref="Schlossausgang.Geschrieben"/> seine Liste neu laden — der Lesemodus des
/// Stammblatts folgt dann von selbst aus der Fokuszeile — und Esc nicht als Beenden deuten,
/// solange die Frage steht.</para>
///
/// <para><b>Kein Weg für den Hilfe-Assistenten</b> (AD-Q15): Die Handlung ist, wie das
/// Löschen, keiner Maske als Knopf angemeldet.</para>
/// </summary>
public sealed class Schlossumschaltung
{
    /// <summary>Die IDs, deren Schloss DIESER Wirt in dieser Sitzung aufgehoben hat.</summary>
    private readonly HashSet<int> _entsperrt = new();

    /// <summary>Die Sätze der offenen Rückfrage — genau die, die das „Ja" schaltet.</summary>
    private List<Katalogfilterzeile> _ziele = new();

    /// <summary>Steht die Rückfrage?</summary>
    public bool FrageOffen { get; private set; }

    /// <summary>Die Richtung der Rückfrage: <c>true</c> = aufheben, <c>false</c> = setzen.</summary>
    public bool Aufheben { get; private set; }

    /// <summary>Der Titel der Rückfrage.</summary>
    public string Titel => Aufheben ? Resource.ADM_SCHLOSS_AUFHEBEN_TITEL : Resource.ADM_SCHLOSS_SETZEN_TITEL;

    /// <summary>Der Fragetext der offenen Rückfrage.</summary>
    public string Frage { get; private set; } = "";

    /// <summary>Die in dieser Sitzung entsperrten IDs (Prüfhilfe für Tests).</summary>
    public IReadOnlyCollection<int> Entsperrte => _entsperrt;

    /// <summary>
    /// Trägt die Fokuszeile das Band „Schloss aufgehoben"? Ja, wenn sie kein Schloss (mehr)
    /// trägt und dieser Wirt es in dieser Sitzung aufgehoben hat.
    /// </summary>
    public bool Entsperrt(Katalogfilterzeile? zeile)
        => zeile is not null && !zeile.Geschuetzt && _entsperrt.Contains(zeile.Id);

    /// <summary>
    /// Die Zeilen zu den Zielen der Auswahlleiste (<c>Zeilenauswahl.Ziele</c>) — über den
    /// <see cref="Katalogfilterzeile.Schluessel"/>, in der Reihenfolge der Liste.
    /// </summary>
    public static IReadOnlyList<Katalogfilterzeile> Zeilen(IReadOnlyList<string> ziele,
                                                           IEnumerable<Katalogfilterzeile> alle)
    {
        var menge = new HashSet<string>(ziele ?? Array.Empty<string>(), StringComparer.Ordinal);
        return (alle ?? Array.Empty<Katalogfilterzeile>()).Where(z => menge.Contains(z.Schluessel)).ToList();
    }

    /// <summary>
    /// <b>Die Handlung für die Auswahlleiste</b> — ohne Weg keine. Die Beschriftung folgt den
    /// Zielzeilen: Ist eine gesperrt, „Schloss aufheben…", sonst „Schloss setzen…"; die
    /// Breitenvorlage hält den Knopf so breit wie die längere Beschriftung.
    /// </summary>
    /// <param name="weg">Der Weg der Hülle; <c>null</c> = keine Handlung.</param>
    /// <param name="ausfuehren">Der Rückruf des Wirts (er ruft <see cref="Fragen"/>).</param>
    /// <param name="ziele">Die Zeilen, auf die die Leiste gerade wirkt.</param>
    /// <param name="nurLesen">Lesemodus des Wirts — hart gesperrt wie „Neu…".</param>
    public Auswahlhandlung? Handlung(Schlossweg? weg, EventCallback ausfuehren,
                                     IReadOnlyList<Katalogfilterzeile> ziele, bool nurLesen)
    {
        if (weg is null) return null;
        bool aufheben = (ziele ?? Array.Empty<Katalogfilterzeile>()).Any(z => z.Geschuetzt);
        return new Auswahlhandlung(aufheben ? Resource.ADM_AW_SCHLOSS_AUFHEBEN : Resource.ADM_AW_SCHLOSS_SETZEN,
                                   ausfuehren)
        {
            Kurztext = aufheben ? Resource.ADM_AW_SCHLOSS_AUFHEBEN_KURZ : Resource.ADM_AW_SCHLOSS_SETZEN_KURZ,
            Breitenvorlage = aufheben ? Resource.ADM_AW_SCHLOSS_SETZEN : Resource.ADM_AW_SCHLOSS_AUFHEBEN,
            Aus = nurLesen || Lesemodus(weg)
        };
    }

    /// <summary>
    /// <b>Der Klick: die Rückfrage vorbereiten.</b> Aufgehoben wird nur das Schloss der
    /// gesperrten Zielzeilen; sind keine gesperrt, wird das Schloss aller gesetzt.
    /// </summary>
    /// <param name="weg">Der Weg der Hülle.</param>
    /// <param name="ziele">Die Zielzeilen der Auswahlleiste.</param>
    /// <param name="ungespeichert">Trägt das Stammblatt geänderte Felder? Dann hält es an.</param>
    /// <param name="zusatzBeimAufheben">
    /// Ein Satz, den die Frage beim Aufheben anhängt — etwa, dass das Wochenprofil beim Typ
    /// gesperrt bleibt (<c>ADM_SCHLOSS_TYP_BLEIBT</c>); er bekommt die Sätze, die die Frage
    /// schaltet. <c>null</c> = keiner.
    /// </param>
    /// <returns>Ein Grund für das Warnband, wenn nicht gefragt wird; sonst <c>null</c>.</returns>
    public string? Fragen(Schlossweg? weg, IReadOnlyList<Katalogfilterzeile> ziele, bool ungespeichert,
                          Func<IReadOnlyList<Katalogfilterzeile>, string>? zusatzBeimAufheben = null)
    {
        if (weg is null || ziele is null || ziele.Count == 0) return null;
        if (Lesemodus(weg)) return Resource.LIZ_LESEMODUS_SPERRE;
        if (ungespeichert) return Resource.ADM_MSG_UNGESPEICHERT;

        Aufheben = ziele.Any(z => z.Geschuetzt);
        _ziele = Aufheben ? ziele.Where(z => z.Geschuetzt).ToList() : ziele.ToList();

        string frage = _ziele.Count == 1
            ? string.Format(CultureInfo.CurrentCulture,
                            Aufheben ? Resource.ADM_SCHLOSS_AUFHEBEN_FRAGE : Resource.ADM_SCHLOSS_SETZEN_FRAGE,
                            _ziele[0].Bezeichner)
            : string.Format(CultureInfo.CurrentCulture,
                            Aufheben ? Resource.ADM_SCHLOSS_AUFHEBEN_FRAGE_MEHR : Resource.ADM_SCHLOSS_SETZEN_FRAGE_MEHR,
                            _ziele.Count, string.Join(", ", _ziele.Select(z => z.Bezeichner)));

        if (Aufheben && zusatzBeimAufheben is not null)
        {
            string zusatz = zusatzBeimAufheben(_ziele) ?? "";
            if (zusatz.Length > 0) frage += " " + zusatz;
        }

        Frage = frage;
        FrageOffen = true;
        return null;
    }

    /// <summary>
    /// <b>Die Antwort.</b> Nur ein „Ja" schreibt — über den Weg der Hülle, in einem Vorgang.
    /// Aufgehobene IDs merkt sich der Wirt (Band im Stammblatt), gesetzte vergisst er.
    /// </summary>
    public Schlossausgang Beantwortet(bool? antwort, Schlossweg? weg)
    {
        FrageOffen = false;
        List<Katalogfilterzeile> ziele = _ziele;
        _ziele = new List<Katalogfilterzeile>();
        if (antwort != true || weg is null || ziele.Count == 0) return Schlossausgang.Nichts;

        bool gesperrt = !Aufheben;
        SchlossErgebnis e = weg.Setzen(ziele.Select(z => z.Id).ToList(), gesperrt);
        if (!e.Ok) return new Schlossausgang(false, "", e.Meldung ?? "");

        foreach (int id in e.Geaendert)
        {
            if (gesperrt) _entsperrt.Remove(id);
            else _entsperrt.Add(id);
        }

        return new Schlossausgang(true, Statustext(e, ziele, gesperrt), "");
    }

    /// <summary>
    /// Die Statuszeile: „Schloss von „X" aufgehoben." bzw. „… von 3 Sätzen …", dazu, wie
    /// viele Sätze schon so standen.
    /// </summary>
    private static string Statustext(SchlossErgebnis e, IReadOnlyList<Katalogfilterzeile> ziele, bool gesperrt)
    {
        var teile = new List<string>();
        int n = e.Geaendert.Count;
        if (n == 1)
        {
            string name = ziele.FirstOrDefault(z => z.Id == e.Geaendert[0])?.Bezeichner ?? "";
            teile.Add(string.Format(CultureInfo.CurrentCulture,
                                    gesperrt ? Resource.ADM_MSG_SCHLOSS_GESETZT : Resource.ADM_MSG_SCHLOSS_AUFGEHOBEN,
                                    name));
        }
        else if (n > 1)
        {
            teile.Add(string.Format(CultureInfo.CurrentCulture,
                                    gesperrt ? Resource.ADM_MSG_SCHLOSS_GESETZT_MEHR : Resource.ADM_MSG_SCHLOSS_AUFGEHOBEN_MEHR,
                                    n));
        }
        if (e.Unveraendert.Count > 0)
            teile.Add(string.Format(CultureInfo.CurrentCulture, Resource.ADM_SCHLOSS_UNVERAENDERT, e.Unveraendert.Count));
        return string.Join(" ", teile);
    }

    private static bool Lesemodus(Schlossweg weg) => weg.Lesemodus?.Invoke() == true;
}

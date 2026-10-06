using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Bausteine;
using EPOS.UI.Standards;
using WindowsFormsApplication1.Zeichnung;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Woher eine Größe eines Nutzungsprofils ihren Zeitverlauf nimmt</b> (Konzept Nutzungsprofile 6.1, 4.3, NP-F7,
/// NP-F9) — der Umschalter des Profileditors. Der Generator nimmt je Größe das Zeilenbild vor dem Stundenprofil vor den
/// Kennwerten; der Editor hält deshalb je Größe genau eines davon.
/// </summary>
public enum RaumnutzungBildweg
{
    /// <summary>Die Kennwerte oben gelten (Tabelle 4.3).</summary>
    Kennwerte = 0,

    /// <summary>Das Zeilenbild gilt wörtlich (NP-F7).</summary>
    Zeilenbild = 1,

    /// <summary>Das Stundenprofil ergibt eine Standardwoche (NP-F9).</summary>
    Stundenprofil = 2,
}

/// <summary>
/// <b>Einheit und Grenzen einer Größe</b> in der Anzeige des Editors — die Hülle liest sie aus den Grenzen des Kerns
/// (NP-F11); Anteile zeigt der Editor in Prozent (<paramref name="Faktor"/> 100).
/// </summary>
/// <param name="Groesse">Die Größe.</param>
/// <param name="Einheit">Die Einheit der Anzeige (°C, 1/h, %).</param>
/// <param name="Faktor">Vom Profilwert zur Anzeige (100 bei Anteilen, sonst 1).</param>
/// <param name="Min">Untergrenze in der Anzeige.</param>
/// <param name="Max">Obergrenze in der Anzeige.</param>
/// <param name="MitAus">Kennt die Größe den Zustand „aus" (Heizen, Kühlen)?</param>
public sealed record RaumnutzungBildgrenzen(KonditionierungGroesse Groesse, string Einheit, double Faktor, double Min, double Max,
                                            bool MitAus)
{
    /// <summary>Die Grenzen ohne Hülle — offen, ohne Umrechnung; der Kern prüft beim Speichern genau.</summary>
    public static RaumnutzungBildgrenzen Offen(KonditionierungGroesse g) => g switch
    {
        KonditionierungGroesse.Heizen => new(g, "°C", 1, 5, 40, true),
        KonditionierungGroesse.Kuehlen => new(g, "°C", 1, 10, 40, true),
        KonditionierungGroesse.Lueftung => new(g, "1/h", 1, 0, 20, false),
        _ => new(g, "%", 100, 0, 100, false),
    };
}

/// <summary>
/// <b>Die Vorschau einer Größe</b> im Editor (6.1 Punkt 2): eine typische Woche und das Teppichbild des Kalenders, den
/// ein leeres Ziel bekäme — an einer neutralen Ferienlage, ohne Datenbank.
/// </summary>
/// <param name="Belegt"><c>false</c> = die Größe ist nicht belegt, das Ziel behielte seinen Kalender (NP-F6).</param>
/// <param name="Weg">Woher der Generator die Größe nimmt.</param>
/// <param name="Woche">Die 168 Werte der Woche in der Einheit der Anzeige („aus" = NaN) oder <c>null</c>.</param>
/// <param name="Teppich">Das Teppichbild oder <c>null</c>.</param>
/// <param name="Hinweis">Was der Generator benennt; leer = nichts.</param>
/// <param name="Bezug">Die Zeile zu Bezugsjahr und Ferienlage.</param>
public sealed record RaumnutzungBildvorschau(bool Belegt, RaumnutzungBildweg Weg, double[]? Woche, Zeichenmodell? Teppich,
                                             string Hinweis, string Bezug);

/// <summary>
/// <b>Die Texte des Editors „Zeitverlauf je Größe"</b> (<c>RNP_ED_*</c>). Die Vorgaben sind die deutschen Texte; die
/// Hülle füllt sie aus den Ressourcen.
/// </summary>
public sealed class RaumnutzungBildTexte
{
    /// <summary>Die Überschrift des Editors.</summary>
    public string Titel { get; set; } = "Zeitverlauf je Größe";

    /// <summary>Die Beschriftung des Umschalters.</summary>
    public string LabelWeg { get; set; } = "Quelle der Größe";

    /// <summary>„aus Kennwerten".</summary>
    public string WegKennwerte { get; set; } = "aus Kennwerten";

    /// <summary>„Zeilenbild".</summary>
    public string WegZeilenbild { get; set; } = "Zeilenbild";

    /// <summary>„Stundenprofil".</summary>
    public string WegStunden { get; set; } = "Stundenprofil";

    /// <summary>Was „aus Kennwerten" heißt.</summary>
    public string HinweisKennwerte { get; set; } = "Es gelten die Kennwerte oben; die Vorschau zeigt, was daraus entsteht.";

    /// <summary>Was das Zeilenbild heißt.</summary>
    public string HinweisZeilenbild { get; set; } = "Das Zeilenbild gilt wörtlich statt der Kennwerte dieser Größe; eine leere Zeile gilt nicht.";

    /// <summary>Was das Stundenprofil heißt.</summary>
    public string HinweisStunden { get; set; } = "Das Stundenprofil ergibt eine Standardwoche: der Werktag an den Nutzungstagen, sonst der nutzungsfreie Tag.";

    /// <summary>Der Zusatz der Lüftung mit Kennwert Außenluft.</summary>
    public string HinweisLuft { get; set; } = "Mit dem Kennwert Außenluft sind die Stundenwerte der Lüftung Anteile davon.";

    /// <summary>Ein Zeilenbild bzw. Stundenprofil ohne Inhalt.</summary>
    public string HinweisLeer { get; set; } = "Noch ohne Werte — bis dahin gilt der nächste Weg (Stundenprofil, sonst Kennwerte).";

    /// <summary>Titel der Rückfrage beim Umschalten.</summary>
    public string TitelFrage { get; set; } = "Quelle wechseln";

    /// <summary>Rückfrage „aus Kennwerten" ({0} = Größe).</summary>
    public string FrageKennwerte { get; set; } = "Zeilenbild und Stundenprofil der Größe „{0}“ löschen? Danach gelten die Kennwerte.";

    /// <summary>Rückfrage Zeilenbild → Stundenprofil ({0} = Größe).</summary>
    public string FrageStunden { get; set; } = "Das Zeilenbild der Größe „{0}“ löschen? Danach gilt das Stundenprofil.";

    /// <summary>Rückfrage Stundenprofil → Zeilenbild ({0} = Größe).</summary>
    public string FrageZeilenbild { get; set; } = "Das Stundenprofil der Größe „{0}“ löschen? Danach gilt das Zeilenbild.";

    /// <summary>Die Beschriftung einer Stundenzelle ({0} = Tagesart, {1} = Stunde).</summary>
    public string LabelStunde { get; set; } = "{0} · Stunde {1}";

    /// <summary>Werktag.</summary>
    public string TagesartWerktag { get; set; } = "Werktag";

    /// <summary>Nutzungsfreier Tag.</summary>
    public string TagesartFrei { get; set; } = "nutzungsfreier Tag";

    /// <summary>Beschriftung des Einfügefelds ({0} = Tagesart).</summary>
    public string LabelEinfuegen { get; set; } = "{0} · Zeile einfügen (24 Werte)";

    /// <summary>Platzhalter des Einfügefelds.</summary>
    public string PlatzhalterEinfuegen { get; set; } = "24 Werte, durch Leerzeichen oder Tab getrennt";

    /// <summary>Knopf „Einfügen".</summary>
    public string KnopfEinfuegen { get; set; } = "Einfügen";

    /// <summary>Grund der weichen Sperre von „Einfügen".</summary>
    public string GrundEinfuegen { get; set; } = "Zuerst eine Zeile mit 24 Werten eintragen.";

    /// <summary>Falsche Zahl der Werte ({0} = gefundene Zahl).</summary>
    public string MeldungAnzahl { get; set; } = "Erwartet 24 Werte, gefunden {0}.";

    /// <summary>Ein Wert ist keine Zahl in den Grenzen ({0} Stunde, {1} Text, {2} Min, {3} Max, {4} Einheit).</summary>
    public string MeldungWert { get; set; } = "Stunde {0}: „{1}“ ist keine Zahl im Bereich {2} … {3} {4}.";

    /// <summary>Überschrift der Vorschau.</summary>
    public string LabelVorschau { get; set; } = "Vorschau der Größe";

    /// <summary>Überschrift der Woche.</summary>
    public string LabelWoche { get; set; } = "Woche";

    /// <summary>Überschrift des Teppichbilds.</summary>
    public string LabelTeppich { get; set; } = "Teppichbild";

    /// <summary>Die Zeile zu Bezugsjahr und Ferienlage ({0} = Jahr).</summary>
    public string TextBezug { get; set; } = "Bezugsjahr {0}, neutrale Ferienlage 1. bis 14. August; die Woche liegt im Januar ohne Feiertag.";

    /// <summary>Die Größe ist nicht belegt.</summary>
    public string TextNichtBelegt { get; set; } = "Die Größe ist nicht belegt; das Ziel behält seinen Kalender.";

    /// <summary>Der Grund des Lesemodus (ausgeliefertes Profil).</summary>
    public string GrundNurLesen { get; set; } = "Ausgeliefert — nur duplizierbar.";

    /// <summary>Die Beschriftungen der Wochenvorschau.</summary>
    public WochenrasterTexte Raster { get; set; } = new();
}

/// <summary>
/// <b>Der Weg des Editors „Zeitverlauf je Größe"</b> — was er vom Kern braucht, als EIN Bündel (Muster
/// <see cref="RaumnutzungWeg"/>). Kein Delegat, keine Vorschau bzw. kein Vorschlag; geschrieben wird über „Speichern" des
/// Blatts (<see cref="RaumnutzungWeg.ProfilAendern"/>), nicht hier.
/// </summary>
public sealed class RaumnutzungBildWeg
{
    /// <summary>Die Texte.</summary>
    public RaumnutzungBildTexte Texte { get; init; } = new();

    /// <summary>Einheit und Grenzen je Größe; ohne Delegat die offenen Vorgaben.</summary>
    public Func<KonditionierungGroesse, RaumnutzungBildgrenzen>? Grenzen { get; init; }

    /// <summary>Die Vorschau der Größe über dem Entwurf — ohne Datenbank, ohne zu schreiben.</summary>
    public Func<RaumnutzungProfilDaten, KonditionierungGroesse, RaumnutzungBildvorschau?>? Vorschau { get; init; }

    /// <summary>Das Zeilenbild, das die Kennwerte der Größe ergäben — der Vorschlag beim Umschalten.</summary>
    public Func<RaumnutzungProfilDaten, KonditionierungGroesse, IReadOnlyList<RaumnutzungZeilenbildDaten>>? Zeilenbildvorschlag { get; init; }

    /// <summary>Die Stundenprofile, die der bisherige Weg der Größe ergäbe — der Vorschlag beim Umschalten.</summary>
    public Func<RaumnutzungProfilDaten, KonditionierungGroesse, IReadOnlyList<RaumnutzungStundenDaten>>? Stundenvorschlag { get; init; }

    /// <summary>Die Grenzen einer Größe (Delegat oder offene Vorgabe).</summary>
    public RaumnutzungBildgrenzen GrenzenVon(KonditionierungGroesse g) => Grenzen?.Invoke(g) ?? RaumnutzungBildgrenzen.Offen(g);
}

/// <summary>
/// <b>Die reine Arbeit des Editors</b> am Entwurf (<see cref="RaumnutzungProfilDaten"/>): Weg je Größe, Zeilen und
/// Stunden setzen, Einfügen aus der Zwischenablage lesen. Ohne Kern, ohne Datenbank — prüfbar ohne Oberfläche.
/// </summary>
public static class RaumnutzungBild
{
    /// <summary>Die Zeilen des Zeilenbilds in Matrixordnung (ohne Nennwert und Saison, 4.1).</summary>
    public static IReadOnlyList<KonditionierungZeile> Zeilen { get; } = new[]
    {
        KonditionierungZeile.Tag, KonditionierungZeile.Nacht, KonditionierungZeile.Wochenende, KonditionierungZeile.Ferien,
    };

    /// <summary>Die Tagesarten des Stundenprofils in Anzeigeordnung.</summary>
    public static IReadOnlyList<RaumnutzungTagesart> Tagesarten { get; } = new[] { RaumnutzungTagesart.Werktag, RaumnutzungTagesart.Frei };

    /// <summary>Die fünf Größen in Matrixordnung.</summary>
    public static IReadOnlyList<KonditionierungGroesse> Groessen { get; } = new[]
    {
        KonditionierungGroesse.Heizen, KonditionierungGroesse.Kuehlen, KonditionierungGroesse.Lueftung,
        KonditionierungGroesse.Geraete, KonditionierungGroesse.Personen,
    };

    /// <summary>Die Stunden eines Tages.</summary>
    public const int STUNDEN = 24;

    /// <summary>Die Kennung einer Zeile, wie <c>Tab_Raumnutzungszeile.Zeile</c> sie trägt (<c>DbWerte.KOND_ZEILE_*</c>).</summary>
    public static string Kennung(KonditionierungZeile z) => z switch
    {
        KonditionierungZeile.Tag => "TAG",
        KonditionierungZeile.Nacht => "NACHT",
        KonditionierungZeile.Wochenende => "WOCHENENDE",
        KonditionierungZeile.Ferien => "FERIEN",
        KonditionierungZeile.Nennwert => "NENNWERT",
        _ => "SAISON",
    };

    /// <summary>Die Zeile zu einer Kennung; <c>null</c> = keine Zeile des Zeilenbilds.</summary>
    public static KonditionierungZeile? Zeile(string? kennung)
        => Zeilen.Cast<KonditionierungZeile?>().FirstOrDefault(z => string.Equals(Kennung(z!.Value), kennung, StringComparison.Ordinal));

    /// <summary>Führt die Größe ΔT der Nachtauskühlung (nur Lüftung, Zeile Nacht)?</summary>
    public static bool MitDeltaT(KonditionierungGroesse g, KonditionierungZeile z)
        => g == KonditionierungGroesse.Lueftung && z == KonditionierungZeile.Nacht;

    /// <summary>
    /// Der Weg einer Größe im Entwurf — dieselbe Reihenfolge wie der Generator: ein Zeilenbild vor einem Stundenprofil vor
    /// den Kennwerten.
    /// </summary>
    public static RaumnutzungBildweg Weg(RaumnutzungProfilDaten p, KonditionierungGroesse g)
        => p.Zeilenbild.Any(z => z.Groesse == g) ? RaumnutzungBildweg.Zeilenbild
           : p.Stunden.Any(s => s.Groesse == g) ? RaumnutzungBildweg.Stundenprofil
           : RaumnutzungBildweg.Kennwerte;

    /// <summary>Die Zeile einer Größe im Zeilenbild; <c>null</c> = keine.</summary>
    public static RaumnutzungZeilenbildDaten? Zeile(RaumnutzungProfilDaten p, KonditionierungGroesse g, KonditionierungZeile z)
        => p.Zeilenbild.FirstOrDefault(x => x.Groesse == g && x.Zeile == Kennung(z));

    /// <summary>
    /// Setzt eine Zeile des Zeilenbilds (Wert in der Einheit des PROFILS, „aus" vor dem Wert). Eine Zeile ohne Wert, „aus",
    /// Fenster und ΔT entfällt; ΔT bleibt nur an Lüftung/Nacht, das Fenster nur an der Nacht.
    /// </summary>
    public static void ZeileSetzen(RaumnutzungProfilDaten p, KonditionierungGroesse g, KonditionierungZeile z, double? wert, bool aus,
                                   int? von, int? bis, double? deltaT)
    {
        if (!Zeilen.Contains(z)) throw new ArgumentOutOfRangeException(nameof(z));
        bool nacht = z == KonditionierungZeile.Nacht;
        var neu = new RaumnutzungZeilenbildDaten(g, Kennung(z), aus ? null : wert, aus, nacht ? von : null, nacht ? bis : null,
                                                 MitDeltaT(g, z) ? deltaT : null);
        var liste = p.Zeilenbild.Where(x => !(x.Groesse == g && x.Zeile == neu.Zeile)).ToList();
        if (neu.Wert.HasValue || neu.Aus || neu.Von.HasValue || neu.Bis.HasValue || neu.DeltaT.HasValue)
            liste.Add(neu);
        p.Zeilenbild = Geordnet(liste);
    }

    /// <summary>Ersetzt das Zeilenbild einer Größe im Ganzen (Vorschlag beim Umschalten).</summary>
    public static void ZeilenbildSetzen(RaumnutzungProfilDaten p, KonditionierungGroesse g, IEnumerable<RaumnutzungZeilenbildDaten> zeilen)
        => p.Zeilenbild = Geordnet(p.Zeilenbild.Where(x => x.Groesse != g)
                                   .Concat((zeilen ?? Enumerable.Empty<RaumnutzungZeilenbildDaten>())
                                           .Where(x => x.Groesse == g && Zeile(x.Zeile) is not null))
                                   .ToList());

    /// <summary>Die 24 Werte einer Tagesart (Einheit des Profils, NaN = „aus"); <c>null</c> = keine.</summary>
    public static double[]? Stunden(RaumnutzungProfilDaten p, KonditionierungGroesse g, RaumnutzungTagesart t)
        => p.Stunden.FirstOrDefault(s => s.Groesse == g && s.Tagesart == t) is { Werte.Count: STUNDEN } s ? s.Werte.ToArray() : null;

    /// <summary>
    /// Setzt die 24 Werte einer Tagesart (Einheit des Profils). Fehlt die andere Tagesart der Größe, bekommt sie dieselben
    /// Werte — ein Stundenprofil ist erst mit beiden vollständig, und so bleibt der Entwurf rechenbar.
    /// </summary>
    public static void StundenSetzen(RaumnutzungProfilDaten p, KonditionierungGroesse g, RaumnutzungTagesart t, IReadOnlyList<double> werte)
    {
        if (werte is null || werte.Count != STUNDEN) throw new ArgumentException("Erwartet 24 Werte.", nameof(werte));
        var liste = p.Stunden.Where(s => !(s.Groesse == g && s.Tagesart == t)).ToList();
        liste.Add(new RaumnutzungStundenDaten(g, t, werte.ToArray()));
        foreach (RaumnutzungTagesart andere in Tagesarten)
            if (!liste.Any(s => s.Groesse == g && s.Tagesart == andere))
                liste.Add(new RaumnutzungStundenDaten(g, andere, werte.ToArray()));
        p.Stunden = GeordnetStunden(liste);
    }

    /// <summary>
    /// Setzt einen Stundenwert (Einheit des Profils). Hat die Tagesart noch keine Werte, nimmt der erste Wert den ganzen
    /// Tag — so bleibt der Entwurf in den Grenzen der Größe; die Tagesart entsteht wie bei <see cref="StundenSetzen"/>.
    /// </summary>
    public static void StundeSetzen(RaumnutzungProfilDaten p, KonditionierungGroesse g, RaumnutzungTagesart t, int stunde, double wert)
    {
        if (stunde < 0 || stunde >= STUNDEN) throw new ArgumentOutOfRangeException(nameof(stunde));
        double[] werte = Stunden(p, g, t) ?? Enumerable.Repeat(wert, STUNDEN).ToArray();
        werte[stunde] = wert;
        StundenSetzen(p, g, t, werte);
    }

    /// <summary>Ersetzt die Stundenprofile einer Größe im Ganzen (Vorschlag beim Umschalten).</summary>
    public static void StundenprofilSetzen(RaumnutzungProfilDaten p, KonditionierungGroesse g, IEnumerable<RaumnutzungStundenDaten> stunden)
        => p.Stunden = GeordnetStunden(p.Stunden.Where(x => x.Groesse != g)
                                       .Concat((stunden ?? Enumerable.Empty<RaumnutzungStundenDaten>())
                                               .Where(x => x.Groesse == g && x.Werte.Count == STUNDEN))
                                       .ToList());

    /// <summary>Entfernt Zeilenbild und/oder Stundenprofil einer Größe.</summary>
    public static void Entfernen(RaumnutzungProfilDaten p, KonditionierungGroesse g, bool zeilenbild, bool stunden)
    {
        if (zeilenbild) p.Zeilenbild = p.Zeilenbild.Where(x => x.Groesse != g).ToList();
        if (stunden) p.Stunden = p.Stunden.Where(x => x.Groesse != g).ToList();
    }

    /// <summary>
    /// <b>Eine Zeile aus der Zwischenablage</b>: 24 Zahlen, durch Leerzeichen oder Tab getrennt, in der Einheit der ANZEIGE
    /// (Prozent bei Anteilen), komma- und punkttolerant (<see cref="Zahlen.ZahlParsen"/>); „aus" nur, wo die Größe es kennt. Ergebnis in
    /// der Einheit des PROFILS; sonst <c>null</c> und die benannte Meldung.
    /// </summary>
    public static double[]? ZeileLesen(string? text, RaumnutzungBildgrenzen grenzen, string ausText, RaumnutzungBildTexte texte,
                                       out string? meldung)
    {
        meldung = null;
        string[] teile = (text ?? "").Split(new[] { ' ', '\t', '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (teile.Length != STUNDEN)
        {
            meldung = string.Format(CultureInfo.CurrentCulture, texte.MeldungAnzahl, teile.Length);
            return null;
        }
        var werte = new double[STUNDEN];
        for (int h = 0; h < STUNDEN; h++)
        {
            string t = teile[h].Trim();
            if (grenzen.MitAus && (string.Equals(t, ausText, StringComparison.CurrentCultureIgnoreCase) ||
                                   string.Equals(t, "aus", StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(t, "off", StringComparison.OrdinalIgnoreCase)))
            {
                werte[h] = double.NaN;
                continue;
            }
            if (!Zahlen.ZahlParsen(t, out double v) || !double.IsFinite(v) ||
                v < grenzen.Min || v > grenzen.Max)
            {
                meldung = string.Format(CultureInfo.CurrentCulture, texte.MeldungWert, h, t,
                                        grenzen.Min.ToString("0.##", CultureInfo.CurrentCulture),
                                        grenzen.Max.ToString("0.##", CultureInfo.CurrentCulture), grenzen.Einheit);
                return null;
            }
            werte[h] = Math.Round(v / grenzen.Faktor, 6, MidpointRounding.AwayFromZero);
        }
        return werte;
    }

    /// <summary>Ein Profilwert in der Anzeige („aus" = NaN bleibt).</summary>
    public static double? Anzeige(double? wert, RaumnutzungBildgrenzen g)
        => wert is double w ? (double.IsNaN(w) ? w : Math.Round(w * g.Faktor, 6, MidpointRounding.AwayFromZero)) : null;

    /// <summary>Ein Anzeigewert als Profilwert („aus" = NaN bleibt).</summary>
    public static double? Profilwert(double? anzeige, RaumnutzungBildgrenzen g)
        => anzeige is double w ? (double.IsNaN(w) ? w : Math.Round(w / g.Faktor, 6, MidpointRounding.AwayFromZero)) : null;

    private static List<RaumnutzungZeilenbildDaten> Geordnet(IEnumerable<RaumnutzungZeilenbildDaten> zeilen)
        => zeilen.OrderBy(z => (int)z.Groesse)
                 .ThenBy(z => Zeile(z.Zeile) is KonditionierungZeile k ? (int)k : int.MaxValue)
                 .ToList();

    private static List<RaumnutzungStundenDaten> GeordnetStunden(IEnumerable<RaumnutzungStundenDaten> stunden)
        => stunden.OrderBy(s => (int)s.Groesse).ThenBy(s => (int)s.Tagesart).ToList();
}

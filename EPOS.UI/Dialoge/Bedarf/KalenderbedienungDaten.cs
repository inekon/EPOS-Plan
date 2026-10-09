namespace EPOS.UI.Dialoge.Bedarf;

// Die Daten der Kalenderbedienung (Konzept Konditionierungsprofile 7.8, E110; Welle K1b) — die Oberfläche sieht die
// Kern-Schicht `Kalenderbedienung` nur über diese Sätze; übersetzt wird allein in der Hülle (`KonditionierungHuelle`).
// Werte stehen in der Einheit der Anzeige (Geräte und Personen in %), Tage 1 … 365 im Bezugsjahr ohne Schalttag.

/// <summary>
/// <b>Der Schlüssel einer Zuordnungszeile</b>: Name, erster und letzter Tag — bei einer Feiertagsregel Name und Regel
/// (Beginn = Ende = 0). Über ihn sind die Kopien einer Zeile in den Kalendern der Größen gekoppelt.
/// </summary>
public sealed record KalenderZeilenschluessel(string Name, int Beginn, int Ende, string? Feiertagsregel = null)
{
    /// <summary>Eine Feiertagsregel?</summary>
    public bool IstFeiertag => Feiertagsregel is not null;

    /// <summary>Ein Einzeltag (Beginn = Ende oder Feiertagsregel)?</summary>
    public bool IstEinzeltag => IstFeiertag || Beginn == Ende;
}

/// <summary>Was eine Zuordnungszeile in jeder Größe bewirkt.</summary>
public enum KalenderWirkung
{
    /// <summary>Die Woche eines Wochenprofils derselben Größe (leerer Name = Standardwoche) — kopiert.</summary>
    Wochenprofil = 0,

    /// <summary>„aus" über den ganzen Tag.</summary>
    Aus = 1,

    /// <summary>„wie Wochentag X" der Standardwoche.</summary>
    WieWochentag = 2,

    /// <summary>Ein Wert über den ganzen Tag.</summary>
    Wert = 3,
}

/// <summary>Die Wirkung einer Zeile: Art, Profilname, Wochentag 1 = Montag … 7 = Sonntag, Wert in der Anzeigeeinheit.</summary>
public sealed record KalenderWirkungsangabe(KalenderWirkung Wirkung, string? Profil = null, int Wochentag = 7, double? Wert = null)
{
    /// <summary>„wie Sonntag" — die Wirkung eines Feiertags.</summary>
    public static KalenderWirkungsangabe WieSonntag { get; } = new(KalenderWirkung.WieWochentag);

    /// <summary>„aus".</summary>
    public static KalenderWirkungsangabe Abgeschaltet { get; } = new(KalenderWirkung.Aus);
}

/// <summary>Wo ein Wochenprofil steht: Ort (Größe, Zone) und Rang der Periode; <c>null</c> = Standardwoche.</summary>
public sealed record KalenderProfilort(KonditionierungOrt Ort, int? Rang = null, long? IdWoche = null);

/// <summary>Ein Wochenprofil: die Standardwoche (<see cref="Rang"/> <c>null</c>) oder die Woche einer eigenen Zeile; 168 Werte, NaN = „aus".</summary>
public sealed record KalenderWochenprofil(int? Rang, string Name, double[] Werte, long? IdWoche = null)
{
    /// <summary>Ist es die Standardwoche?</summary>
    public bool IstStandardwoche => !Rang.HasValue && !IdWoche.HasValue;

    /// <summary>Eine benannte Woche (Stufe 2)?</summary>
    public bool IstBenannt => IdWoche.HasValue;
}

/// <summary>Ein Pinselstrich: Tage <paramref name="TagVon"/> … <paramref name="TagBis"/> (0 = Montag), Stunden von (eingeschlossen) bis (ausgeschlossen), Wert oder „aus" (<c>null</c>).</summary>
public sealed record KalenderPinselstrich(int TagVon, int TagBis, int StundeVon, int StundeBis, double? Wert);

/// <summary>
/// <b>Eine Zuordnungszeile</b> „von–bis → Wirkung, gilt für": die gekoppelten Kopien mit Rang je Größe; die
/// <see cref="Wirkung"/> ist die der ersten Größe, in der die Zeile steht.
/// </summary>
public sealed record KalenderZuordnungszeile(KalenderZeilenschluessel Schluessel, int ErsterTag,
                                             IReadOnlyList<KonditionierungGroesse> GiltFuer,
                                             IReadOnlyDictionary<KonditionierungGroesse, int> Raenge,
                                             KalenderWirkungsangabe Wirkung, bool IstFerien, int Maske = 31, bool IstGemeinsam = false, long? IdWoche = null)
{
    /// <summary>Ein Einzeltag?</summary>
    public bool IstEinzeltag => Schluessel.IstEinzeltag;
}

/// <summary>Was einen Tag des Jahresrasters bestimmt — die Farbe in der Anzeige.</summary>
public enum KalenderTagart
{
    /// <summary>Die Standardwoche, Werktag.</summary>
    Grundwoche = 0,

    /// <summary>Die Standardwoche an Samstag oder Sonntag.</summary>
    Wochenende = 1,

    /// <summary>Eine Zuordnungszeile über mehrere Tage.</summary>
    Zeitraum = 2,

    /// <summary>Ein Einzeltag mit festem Datum.</summary>
    Einzeltag = 3,

    /// <summary>Ferien.</summary>
    Ferien = 4,

    /// <summary>Eine Feiertagsregel.</summary>
    Feiertag = 5,

    /// <summary>Außerhalb der Saison („aus").</summary>
    Saison = 6,
}

/// <summary>Ein Tag des Jahresrasters (Tag 1 … 365, Monat 1 … 12, Wochentag 0 = Montag) mit Quelle und Schlüssel der eigenen Zeile.</summary>
public sealed record KalenderRastertag(int Tag, int Monat, int TagImMonat, int Wochentag, KalenderTagart Art, string? Quelle,
                                       KalenderZeilenschluessel? Schluessel);

/// <summary>Ein Abschnitt des Jahresbands: aufeinanderfolgende Tage derselben Quelle.</summary>
public sealed record KalenderBandabschnitt(int Beginn, int Ende, KalenderTagart Art, string? Quelle, KalenderZeilenschluessel? Schluessel);

/// <summary>Ein Ferienzeitraum der Schnellfelder: Name „Ferien n", erster und letzter Tag (Beginn nach Ende = über den Jahreswechsel).</summary>
public sealed record KalenderFerienzeile(string Name, int Beginn, int Ende);

/// <summary>
/// Eine Periode des gemeinsamen Kalenders (Stufe 2): die Periode in Kern-Einheiten und die Maske „gilt für"
/// (Bit k = k-te Größe, Heizen = 1 … Personen = 16, 31 = alle).
/// </summary>
public sealed record KalenderGemeinschaftsperiode(KonditionierungPeriode Periode, int Maske);

/// <summary>Eine benannte Woche (Stufe 2): Id (≤ 0 = noch ohne Zeile), Größe, Name und 168 Werte in Kern-Einheiten.</summary>
public sealed record KalenderBenannteWoche(long Id, KonditionierungGroesse Groesse, string Name, double[] Werte);

/// <summary>Zwei gekoppelte Zeilen, die in zwei Größen in umgekehrter Folge stehen (Warnzeile der Zuordnung).</summary>
public sealed record KalenderRangwarnung(string Erste, string Zweite, KonditionierungGroesse Oben, KonditionierungGroesse Unten);

/// <summary>
/// <b>Die Ansicht der Kalenderbedienung</b> einer Größe am Ort — alles, was der Baustein zeichnet, aus EINEM Aufruf
/// der Hülle: Wochenprofile, Zuordnungszeilen und Einzeltage, Ferien, Jahresraster, Jahresband, Warnungen.
/// </summary>
public sealed class KalenderAnsicht
{
    /// <summary>Die Wochenprofile der Größe: die Standardwoche zuerst, dann die Wochen der eigenen Zeilen.</summary>
    public IReadOnlyList<KalenderWochenprofil> Profile { get; init; } = Array.Empty<KalenderWochenprofil>();

    /// <summary>Die Zuordnungszeilen am Ort (alle Größen), nach erstem Tag und Name.</summary>
    public IReadOnlyList<KalenderZuordnungszeile> Zuordnungen { get; init; } = Array.Empty<KalenderZuordnungszeile>();

    /// <summary>Die Ferienzeiträume des Gebäudes (die ersten vier aus den Gebäudespalten, dann „Ferien 5" …).</summary>
    public IReadOnlyList<KalenderFerienzeile> Ferien { get; init; } = Array.Empty<KalenderFerienzeile>();

    /// <summary>Die 365 Tage des Jahresrasters der Größe.</summary>
    public IReadOnlyList<KalenderRastertag> Raster { get; init; } = Array.Empty<KalenderRastertag>();

    /// <summary>Das Jahresband der Größe.</summary>
    public IReadOnlyList<KalenderBandabschnitt> Band { get; init; } = Array.Empty<KalenderBandabschnitt>();

    /// <summary>Die gekoppelten Zeilen mit abweichender Rangfolge zwischen den Größen.</summary>
    public IReadOnlyList<KalenderRangwarnung> Rangwarnungen { get; init; } = Array.Empty<KalenderRangwarnung>();

    /// <summary>Die Wochenendtage des Gebäudes (0 = Montag); Vorgabe Samstag und Sonntag.</summary>
    public IReadOnlyList<int> Wochenendtage { get; init; } = new[] { 5, 6 };

    /// <summary>Das Feiertagsland des Gebäudes (ISO-Kürzel); <c>null</c> = nur bundeseinheitlich.</summary>
    public string? Feiertagsland { get; init; }

    /// <summary>Die wählbaren Länder (ISO-Kürzel).</summary>
    public IReadOnlyList<string> Feiertagslaender { get; init; } = Array.Empty<string>();
}

/// <summary>Die Tage des Bezugsjahrs (Gemeinjahr, kein Schalttag) — Umrechnung zwischen Jahrestag und Datum für die Anzeige.</summary>
public static class Kalendertage
{
    /// <summary>Die Tage je Monat eines Gemeinjahrs.</summary>
    public static IReadOnlyList<int> TageJeMonat { get; } = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    /// <summary>Der Jahrestag 1 … 365 zu Monat und Tag; der 29. Februar zählt als 28.</summary>
    public static int Jahrestag(int monat, int tag)
    {
        int d = 0;
        for (int m = 1; m < monat; m++) d += TageJeMonat[m - 1];
        return d + Math.Min(tag, TageJeMonat[monat - 1]);
    }

    /// <summary>Der Jahrestag eines Datums; <c>null</c> bleibt <c>null</c>.</summary>
    public static int? Jahrestag(DateOnly? datum) => datum is DateOnly d ? Jahrestag(d.Month, d.Day) : null;

    /// <summary>Das Datum eines Jahrestags im Bezugsjahr <paramref name="jahr"/>; außerhalb 1 … 365 <c>null</c>.</summary>
    public static DateOnly? Datum(int? tag, int jahr)
    {
        if (tag is not int t || t < 1 || t > 365) return null;
        int m = 1;
        while (t > TageJeMonat[m - 1]) { t -= TageJeMonat[m - 1]; m++; }
        return new DateOnly(jahr, m, t);
    }

    /// <summary>„TT.MM." eines Jahrestags; leer außerhalb.</summary>
    public static string Kurz(int tag, int jahr)
        => Datum(tag, jahr) is DateOnly d ? d.Day.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + "."
                                             + d.Month.ToString("00", System.Globalization.CultureInfo.InvariantCulture) + "." : "";
}

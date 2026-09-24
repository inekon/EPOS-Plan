using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge.Hilfe;

/// <summary>
/// Der Weg einer ZAHLENREIHE durch die Maskenbrücke, wie ihn <c>reihe_setzen</c> geht
/// (Welle #458 Stufe 3b) — für die bunit-Fälle der sieben Masken: Zugang holen, Liste
/// umsetzen (<see cref="KiFeldwandler.WandleReihe"/>), setzen.
/// </summary>
/// <remarks>
/// Die Aktion selbst (Vorbedingung, Bestätigung, Riegel) prüft
/// <c>EPOS.Kern.Tests/KiReiheSetzenTests</c> an Prüfdialogen; hier geht es um das, was
/// nur der echte Dialog zeigt: dass die Reihe an seinem lebenden Stand hängt.
/// </remarks>
internal static class KiReihenhilfe
{
    /// <summary>Der Zugang der Reihe; die Maske muss angemeldet und das Feld eine Zahlenreihe sein.</summary>
    internal static KiFeldzugang Zugang(string maske, string feld)
    {
        KiFeldzugang? zugang = KiMaskenbruecke.Feldzugang(maske, feld);
        Assert.NotNull(zugang);
        Assert.True(zugang!.Feld.IstReihe, feld + " ist keine Zahlenreihe");
        Assert.True(zugang.Setzbar, feld + " ist nicht setzbar");
        return zugang;
    }

    /// <summary>Die Werte der Reihe, wie der Assistent sie liest.</summary>
    internal static double?[] Werte(string maske, string feld)
        => KiFeldwandler.Reihenwerte(Zugang(maske, feld)).ToArray();

    /// <summary>
    /// Setzt <paramref name="werte"/> ab Stelle <paramref name="ab"/> (0 = die ganze Reihe)
    /// und verlangt, dass es gelingt.
    /// </summary>
    internal static void Setze(string maske, string feld, IReadOnlyList<double> werte, int ab = 0)
    {
        KiFeldzugang zugang = Zugang(maske, feld);
        KiFeldumsetzung umsetzung = KiFeldwandler.WandleReihe(zugang, werte, ab);
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
    }

    /// <summary>Warum die Umsetzung scheitert; <c>null</c>, wenn sie gelänge.</summary>
    internal static string? Grund(string maske, string feld, IReadOnlyList<double> werte, int ab = 0)
    {
        KiFeldumsetzung umsetzung = KiFeldwandler.WandleReihe(Zugang(maske, feld), werte, ab);
        return umsetzung.Ok ? null : umsetzung.Grund;
    }

    /// <summary>Eine Liste gleicher Werte.</summary>
    internal static double[] Gleich(int anzahl, double wert) => Enumerable.Repeat(wert, anzahl).ToArray();

    /// <summary>1, 2, 3, … mal <paramref name="faktor"/>.</summary>
    internal static double[] Folge(int anzahl, double faktor = 1.0)
        => Enumerable.Range(1, anzahl).Select(i => i * faktor).ToArray();
}

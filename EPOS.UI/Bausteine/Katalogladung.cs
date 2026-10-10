using System.Globalization;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Die Fangstelle der Katalogliste</b> — EIN Ort, an dem ein Katalogdialog seine Liste
/// liest. Wirft die Quelle (Hülle, Controller, Datenbank), bleibt der Dialog nicht still:
/// Die Liste ist leer, der Grund steht als Text bereit (<see cref="Resource.WURZEL_LISTE_FEHLER"/>,
/// beide Sprachen, die Meldung der Ausnahme dahinter), und ein Vermerk steht im
/// <see cref="Ausnahmeprotokoll"/>. Der Dialog zeigt den Text in seinem Warnbanner; Schließen,
/// Hilfe und die übrigen Knöpfe bleiben erreichbar, weil der Aufbau nicht abbricht.
/// </summary>
/// <remarks>
/// Der Fang sitzt am Rand der Oberfläche, nicht in der Hülle: Die Hülle reicht die Ausnahme
/// weiter, der Dialog benennt sie (Hausblatt „benannt ablehnen, nie still übergehen“).
/// </remarks>
public static class Katalogladung
{
    /// <summary>Liest die Liste aus <paramref name="quelle"/>; ohne Quelle eine leere Liste.</summary>
    /// <param name="quelle">Die Lesestelle der Liste; <c>null</c> = keine Liste.</param>
    /// <param name="liste">Der Name der Liste im Grund (Titel des Dialogs).</param>
    /// <param name="fehler">Leer, wenn die Liste gelesen ist; sonst der Grund für den Warnbanner.</param>
    public static IReadOnlyList<T> Lesen<T>(Func<IReadOnlyList<T>>? quelle, string? liste, out string fehler)
    {
        fehler = "";
        if (quelle is null) return Array.Empty<T>();
        try
        {
            return quelle() ?? Array.Empty<T>();
        }
        catch (Exception ex)
        {
            string name = liste ?? "";
            fehler = string.Format(CultureInfo.CurrentCulture, Resource.WURZEL_LISTE_FEHLER, name, ex.Message);
            Ausnahmeprotokoll.Vermerken("Katalogliste \u201e" + name + "\u201c nicht gelesen: "
                + ex.GetType().FullName + ": " + ex.Message);
            return Array.Empty<T>();
        }
    }
}

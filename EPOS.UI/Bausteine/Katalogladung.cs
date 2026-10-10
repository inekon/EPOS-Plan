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
/// Dieselbe Stelle fängt das Lesen des gewählten Satzes (<see cref="Satz{T}"/>), das Speichern
/// (<see cref="Schreiben{T}"/>) und das Löschen (<see cref="Loeschen{T}"/>): Wirft der Weg, hat
/// der Dialog einen Grund in beiden Sprachen und das Protokoll einen Vermerk; die Eingaben
/// bleiben stehen, der Dialog bleibt offen.
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

    /// <summary>
    /// Liest den gewählten Satz. Wirft <paramref name="weg"/>, ist das Ergebnis <c>null</c> (kein
    /// halber Satz), <paramref name="fehler"/> nennt den Grund (<see cref="Resource.WURZEL_SATZ_FEHLER"/>),
    /// und das Protokoll hat einen Vermerk.
    /// </summary>
    /// <param name="weg">Die Lesestelle des Satzes.</param>
    /// <param name="satz">Der Name des Satzes im Grund.</param>
    /// <param name="fehler">Leer, wenn der Satz gelesen ist; sonst der Grund für den Warnbanner.</param>
    public static T? Satz<T>(Func<T?> weg, string? satz, out string fehler) where T : class
    {
        fehler = "";
        try
        {
            return weg();
        }
        catch (Exception ex)
        {
            fehler = Benennen(Resource.WURZEL_SATZ_FEHLER, "Katalogsatz", "nicht gelesen", satz, ex);
            return null;
        }
    }

    /// <summary>
    /// Wie <see cref="Satz{T}"/>, aber für einen Satz, der zu einem schon gelesenen hinzukommt
    /// (Vergleich, Aufklapper): <paramref name="fehler"/> wird nur bei einer Ausnahme gesetzt und
    /// sonst nicht geleert, damit ein früherer Grund stehen bleibt.
    /// </summary>
    public static T? SatzDazu<T>(Func<T?> weg, string? satz, ref string fehler) where T : class
    {
        T? ergebnis = Satz(weg, satz, out string grund);
        if (grund.Length > 0) fehler = grund;
        return ergebnis;
    }

    /// <summary>
    /// Speichert über <paramref name="weg"/>. Wirft der Weg, liefert <paramref name="abgelehnt"/>
    /// das abgelehnte Ergebnis mit dem Grund (<see cref="Resource.WURZEL_SCHREIBEN_FEHLER"/>) — der
    /// Dialog meldet es wie jede andere Ablehnung —, und das Protokoll hat einen Vermerk.
    /// </summary>
    public static T Schreiben<T>(Func<T> weg, string? satz, Func<string, T> abgelehnt)
    {
        try
        {
            return weg();
        }
        catch (Exception ex)
        {
            return abgelehnt(Benennen(Resource.WURZEL_SCHREIBEN_FEHLER, "Katalogsatz", "nicht gespeichert", satz, ex));
        }
    }

    /// <summary>
    /// Löscht über <paramref name="weg"/>. Wirft der Weg, liefert <paramref name="abgelehnt"/> das
    /// abgelehnte Ergebnis mit dem Grund (<see cref="Resource.WURZEL_LOESCHEN_FEHLER"/>), und das
    /// Protokoll hat einen Vermerk.
    /// </summary>
    public static T Loeschen<T>(Func<T> weg, string? satz, Func<string, T> abgelehnt)
    {
        try
        {
            return weg();
        }
        catch (Exception ex)
        {
            return abgelehnt(Benennen(Resource.WURZEL_LOESCHEN_FEHLER, "Katalogsatz", "nicht gelöscht", satz, ex));
        }
    }

    /// <summary>
    /// Löscht über einen Weg mit Ja/Nein-Antwort. <paramref name="fehler"/> ist leer, solange der
    /// Weg nicht wirft; wirft er, ist die Antwort <c>false</c> und <paramref name="fehler"/> nennt
    /// den Grund.
    /// </summary>
    public static bool Loeschen(Func<bool> weg, string? satz, out string fehler)
    {
        string grund = "";
        bool ok = Loeschen(weg, satz, g => { grund = g; return false; });
        fehler = grund;
        return ok;
    }

    /// <summary>Der Grund in der Sprache der Oberfläche und der Vermerk im Ausnahmeprotokoll.</summary>
    private static string Benennen(string vorlage, string was, string wie, string? satz, Exception ex)
    {
        string name = satz ?? "";
        Ausnahmeprotokoll.Vermerken(was + " \u201e" + name + "\u201c " + wie + ": "
            + ex.GetType().FullName + ": " + ex.Message);
        return string.Format(CultureInfo.CurrentCulture, vorlage, name, ex.Message);
    }
}

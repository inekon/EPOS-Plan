using Xunit;

namespace Formularkarte.Tests;

/// <summary>
/// Prueft Fundstellen ("datei:zeile") gegen den Quelltext, statt eine feste
/// Zeilennummer festzuschreiben.
///
/// <para>
/// Die Tests laufen gegen die echten Masken des Bestands, und der bewegt sich:
/// Ein einziges entferntes <c>using</c> verschiebt jede Zeile darunter. Ein
/// Test, der "Zeile 42" fordert, prueft dann nicht mehr das Werkzeug, sondern
/// den Stand des Bestands. Hier wird stattdessen geprueft, dass die genannte
/// Zeile auch wirklich das enthaelt, was dort stehen soll.
/// </para>
/// </summary>
public static class Fundstelle
{
    /// <summary>Die genannte Zeile der Datei; 1-basiert wie in der Karte.</summary>
    public static string Zeile(string datei, int nummer)
    {
        var zeilen = File.ReadAllLines(datei);
        Assert.InRange(nummer, 1, zeilen.Length);
        return zeilen[nummer - 1];
    }

    /// <summary>
    /// Zerlegt "WindowsFormsApplication1/Views/Kosten/Form_Kosten.cs:2092" und
    /// prueft, dass die genannte Zeile <paramref name="erwartet"/> enthaelt.
    /// </summary>
    public static void Enthaelt(string fundstelle, string erwartet) =>
        Enthaelt(Repowurzel.Pfad, fundstelle, erwartet);

    /// <summary>
    /// Dieselbe Pruefung fuer eine Fundstelle, die nicht auf die Repowurzel
    /// bezogen ist: Das Werkzeug meldet sie relativ zum Elternordner seiner
    /// Suchwurzel, bei den Pruefmustern also gegen
    /// <c>Werkzeuge/Formularkarte.Tests</c> statt gegen das Repo.
    /// </summary>
    public static void Enthaelt(string bezug, string fundstelle, string erwartet)
    {
        var doppelpunkt = fundstelle.LastIndexOf(':');
        Assert.True(doppelpunkt > 0, "Keine Fundstelle der Form 'datei:zeile': " + fundstelle);

        var datei = Path.Combine(bezug, fundstelle.Substring(0, doppelpunkt)
                                                  .Replace('/', Path.DirectorySeparatorChar));
        var nummer = int.Parse(fundstelle.Substring(doppelpunkt + 1));

        Assert.True(File.Exists(datei), "Datei aus der Fundstelle fehlt: " + datei);
        Assert.Contains(erwartet, Zeile(datei, nummer), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prueft, dass die Maske den Handler kennt und seine Zeilenangabe auf die
    /// Methodendeklaration in der Form_X.cs zeigt.
    /// </summary>
    public static void HandlerStimmt(Maske maske, string handler)
    {
        Assert.True(maske.Handler.TryGetValue(handler, out var stelle),
                    "Handler " + handler + " nicht gefunden.");

        var quelltext = QuelltextLeser.Quelltextpfad(maske.Datei);
        Assert.NotNull(quelltext);
        Assert.Contains(handler, Zeile(quelltext!, stelle.Zeile), StringComparison.Ordinal);
        Assert.True(stelle.Zeilen > 0, "Umfang von " + handler + " ist " + stelle.Zeilen + " Zeilen.");
    }
}

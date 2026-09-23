using System.Collections.Generic;
using WindowsFormsApplication1;

namespace EPOS.UI.Bausteine;

/// <summary>
/// <b>Eine Zeile der Vergleichstabelle</b> (Konzept Administrationsdialoge, Stufe 3,
/// Vorschlag V12): ein Parameter über alle gewählten Sätze, je Satz ein Wert, und ob die
/// Werte voneinander abweichen.
/// </summary>
public sealed record Vergleichszeile(string Name, IReadOnlyList<string> Werte, bool Abweichend);

/// <summary>
/// <b>Ein Feld eines Satzes für den Vergleich</b> — Schlüssel (woran die Zeilen der
/// Sätze einander zugeordnet werden), Beschriftung und Anzeigewert.
/// </summary>
public sealed record Vergleichsfeld(string Schluessel, string Name, string Wert);

/// <summary>
/// <b>Baut die Zeilen der Vergleichstabelle</b> (V12) — aus den Feldern der Sätze, aus
/// den Spalten eines Katalogprofils oder aus der Parameterübersicht.
///
/// <para><b>Die Reihenfolge ist die des ERSTEN Satzes</b>; ein Feld, das ein späterer
/// Satz nicht führt, steht dort als Leerwert („—"). Verglichen wird ZEICHENGENAU, wie
/// die Werte dastehen — dieselbe Regel wie in der Vergleichsüberlagerung der
/// <see cref="Katalogliste"/>. Gerechnet wird EINMAL, wenn der Wirt den Vergleich
/// anstellt, nicht je Zeichenlauf: Hinter den Feldern eines Satzes steht ein
/// Datenbankzugriff.</para>
/// </summary>
public static class Vergleichsbau
{
    /// <summary>Aus den Feldern der Sätze, je Satz in Anzeigereihenfolge.</summary>
    public static IReadOnlyList<Vergleichszeile> AusFeldern(IReadOnlyList<IReadOnlyList<Vergleichsfeld>> saetze)
    {
        var zeilen = new List<Vergleichszeile>();
        if (saetze is null || saetze.Count == 0) return zeilen;

        foreach (Vergleichsfeld feld in saetze[0])
        {
            var werte = new List<string>(saetze.Count);
            foreach (IReadOnlyList<Vergleichsfeld> satz in saetze)
                werte.Add(Wert(satz, feld.Schluessel));

            zeilen.Add(new Vergleichszeile(feld.Name, werte, Abweichend(werte)));
        }
        return zeilen;
    }

    /// <summary>Aus den Spalten eines Katalogprofils — der Rückfall ohne Felder.</summary>
    public static IReadOnlyList<Vergleichszeile> AusSpalten(Katalogfilterprofil profil,
                                                           IReadOnlyList<Katalogfilterzeile> zeilen)
    {
        var liste = new List<Vergleichszeile>();
        if (profil is null || zeilen is null || zeilen.Count == 0) return liste;

        foreach (Katalogspalte spalte in profil.Spalten)
        {
            var werte = new List<string>(zeilen.Count);
            foreach (Katalogfilterzeile z in zeilen) werte.Add(z.Text(spalte.Schluessel));
            liste.Add(new Vergleichszeile(spalte.Kopftext, werte, Abweichend(werte)));
        }
        return liste;
    }

    /// <summary>Weichen die Werte voneinander ab? Zeichengenau, wie sie dastehen.</summary>
    public static bool Abweichend(IReadOnlyList<string> werte)
    {
        for (int i = 1; i < werte.Count; i++)
            if (!string.Equals(werte[0], werte[i], System.StringComparison.Ordinal)) return true;
        return false;
    }

    private static string Wert(IReadOnlyList<Vergleichsfeld> satz, string schluessel)
    {
        foreach (Vergleichsfeld f in satz)
            if (f.Schluessel == schluessel) return f.Wert;
        return ParameterVerwendung.LEER;
    }
}

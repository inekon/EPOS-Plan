using System;
using System.Collections.Generic;

namespace EPOS.UI.Bausteine;

/// <summary>
/// Ein Knoten der <c>Baumansicht</c> — rein anzeigend, ohne Fachbezug
/// (iU9-W14c.4, Bausteinlücke „Baumansicht" des Wellenplans).
///
/// <para><b>Warum ein <see cref="Schluessel"/> und kein Index.</b> Der Wirt bekommt
/// die Auswahl als Zeichenkette zurück und schlägt sie in seinem eigenen Verzeichnis
/// nach — genauso, wie <c>Reiter</c> es mit <c>Reiterblatt.Schluessel</c> macht. Ein
/// Index bräche, sobald ein Neuaufbau die Reihenfolge ändert; und genau das tut der
/// einzige Nutzer nach jeder Aktion.</para>
///
/// <para><b>Warum <see cref="Kennzeichen"/> getrennt vom <see cref="Text"/>.</b> Der
/// Vorläufer hängte <c>" [Auslieferung]"</c> an den Blatttext. Getrennt lässt es sich
/// als Abzeichen zeichnen (und unter <c>forced-colors</c> als Rahmen); zusammen wäre
/// es nur Text.</para>
/// </summary>
/// <param name="Schluessel">Eindeutig im ganzen Baum, sprachneutral.</param>
/// <param name="Text">Die fertige Zeile.</param>
/// <param name="Kinder">Leer = Blatt.</param>
/// <param name="Kennzeichen">Optionales Abzeichen, z. B. „[Auslieferung]".</param>
/// <param name="VonVornOffen">Vorgabe des Aufklappzustands.</param>
public sealed record Baumknoten(
    string Schluessel,
    string Text,
    IReadOnlyList<Baumknoten> Kinder,
    string Kennzeichen = "",
    bool VonVornOffen = false)
{
    /// <summary>Ein Blatt — ohne Kinder, in einer Zeile geschrieben.</summary>
    public static Baumknoten Blatt(string schluessel, string text, string kennzeichen = "")
        => new(schluessel, text, Array.Empty<Baumknoten>(), kennzeichen);
}

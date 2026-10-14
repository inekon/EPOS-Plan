using System.Globalization;

namespace EPOS.UI.Dienste;

/// <summary>
/// <b>Die Position des gezeigten Stands</b> (Konzept Berichtsvorlagen 4.5, 9.4; Katalog v8): wo der Stand, dessen
/// Werte eine Seite zeigt, in der Folge des Berichts steht — das Stammprojekt ist Stand 1, dann folgen die Varianten
/// in der Reihenfolge der Gruppe. Die Seite reicht sie als kaskadierenden Wert an ihre Platzhaltermarken; die Marke
/// eines Standwerts nennt damit zusätzlich die Positionsform <c>stand.&lt;n&gt;.&lt;rest&gt;</c> (bei einer Variante
/// auch <c>variante.&lt;n&gt;.&lt;rest&gt;</c>), die überall im Bericht gilt. Die Hülle bildet sie; die Bausteine
/// kennen keinen Katalog.
///
/// <para><b>Mehrere Stände an einer Marke:</b> Zeigt ein Element die Werte mehrerer Stände in einer Tafel (die
/// Sensitivität der Wirtschaftlichkeitsseite), reicht der Wirt der Marke die Positionen als Liste
/// (<c>Vorlagenfeldknopf.Positionen</c>); jede trägt dann den <see cref="Name"/> ihres Stands, damit die Aufklappung
/// die Formen zuordnet.</para>
/// </summary>
/// <param name="Stand">Die Position des Stands, ab 1 (Stammprojekt = 1); 0 = unbekannt.</param>
/// <param name="Variante">Die Position unter den Varianten, ab 1; 0 = der Stand ist das Stammprojekt.</param>
/// <param name="Name">Der Anzeigename des Stands, wenn eine Marke mehrere Stände nennt; leer = der gezeigte Stand.</param>
public sealed record Vorlagenfeldposition(int Stand, int Variante, string Name = "")
{
    /// <summary>Dieselbe Position mit dem Anzeigenamen ihres Stands (für eine Marke über mehrere Stände).</summary>
    public Vorlagenfeldposition MitName(string? name) => this with { Name = name?.Trim() ?? "" };

    /// <summary>
    /// Die Positionsformen eines Platzhalters an diesem Stand — <c>stand.&lt;n&gt;.&lt;rest&gt;</c> und, bei einer
    /// Variante, <c>variante.&lt;n&gt;.&lt;rest&gt;</c>; leer ohne <see cref="Vorlagenfeldanzeige.Positionsrest"/>
    /// oder ohne bekannte Position.
    /// </summary>
    public IReadOnlyList<string> Formen(Vorlagenfeldanzeige? feld)
    {
        string rest = feld?.Positionsrest?.Trim() ?? "";
        if (rest.Length == 0 || Stand <= 0) return Array.Empty<string>();
        var formen = new List<string>(2) { "stand." + Stand.ToString(CultureInfo.InvariantCulture) + "." + rest };
        if (Variante > 0) formen.Add("variante." + Variante.ToString(CultureInfo.InvariantCulture) + "." + rest);
        return formen;
    }
}

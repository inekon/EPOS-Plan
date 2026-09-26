using EPOS.UI.Dienste;

namespace EPOS.UI.Bausteine;

/// <summary>Welcher Hinweis zum kopierten Text gehört (Konzept Berichtsvorlagen 9.4 „Kopieren je Art").</summary>
public enum Kopierhinweis
{
    /// <summary>Text, Zahl, Datum: an jeder Stelle einfügen.</summary>
    Einzeln,

    /// <summary>Tabelle, Liste, Kapitel, Blatt: in einen eigenen Absatz.</summary>
    EigenerAbsatz,

    /// <summary>Bild: ein Bild einfügen, Alternativtext = Schlüssel.</summary>
    Bild,

    /// <summary>Schalter: nur als Bedingung eines Blocks.</summary>
    Schalter,
}

/// <summary>
/// Der Text, den „Kopieren" in die Zwischenablage legt, und sein Hinweis.
/// </summary>
/// <param name="Text">Der Text für die Vorlage.</param>
/// <param name="Hinweis">Wie er einzufügen ist.</param>
/// <param name="Block">Der Wiederholblock, der ihn umrahmt (<c>stand</c>, <c>gebaeude</c>); leer = keiner.</param>
public sealed record Vorlagenfeldkopie(string Text, Kopierhinweis Hinweis, string Block = "")
{
    /// <summary>
    /// <b>Kopieren je Art</b> (Konzept 9.4): Text, Zahl, Datum <c>{{schlüssel}}</c>; Tabelle, Liste,
    /// Kapitel (und Blatt) dasselbe für einen eigenen Absatz; ein Bild nur den Schlüssel — er gehört in
    /// den Alternativtext; ein Schalter als Bedingung <c>{{#wenn …}}</c> … <c>{{/wenn}}</c>. Ein Schlüssel
    /// des Kontexts Stand kommt samt Blockrahmen <c>{{#je stand}}</c> … <c>{{/je}}</c> (Gebäude ebenso
    /// mit <c>{{#je gebaeude}}</c>); das Bild bleibt dabei der nackte Schlüssel, der Hinweis nennt den
    /// Block. Die Absätze trennt ein Zeilenumbruch — Word macht daraus beim Einfügen Absätze.
    /// </summary>
    public static Vorlagenfeldkopie Fuer(Vorlagenfeldanzeige? feld)
    {
        if (feld is null || string.IsNullOrWhiteSpace(feld.Schluessel)) return new Vorlagenfeldkopie("", Kopierhinweis.Einzeln);

        string s = feld.Schluessel.Trim();
        string art = feld.ArtKennung ?? "";
        string block = feld.KontextKennung switch
        {
            "Stand" => "stand",
            "Gebaeude" => "gebaeude",
            _ => "",
        };

        if (art == "Bild") return new Vorlagenfeldkopie(s, Kopierhinweis.Bild, block);

        string text;
        Kopierhinweis hinweis;
        switch (art)
        {
            case "Schalter":
                text = "{{#wenn " + s + "}}\n…\n{{/wenn}}";
                hinweis = Kopierhinweis.Schalter;
                break;
            case "Tabelle":
            case "Liste":
            case "Kapitel":
            case "Blatt":
                text = "{{" + s + "}}";
                hinweis = Kopierhinweis.EigenerAbsatz;
                break;
            default:
                text = "{{" + s + "}}";
                hinweis = Kopierhinweis.Einzeln;
                break;
        }

        if (block.Length > 0) text = "{{#je " + block + "}}\n" + text + "\n{{/je}}";
        return new Vorlagenfeldkopie(text, hinweis, block);
    }
}

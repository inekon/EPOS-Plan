using System.Xml.Linq;

namespace Formularkarte;

/// <summary>
/// Liest die .resx-Dateien einer lokalisierten Maske.
///
/// <para>
/// 21 der 74 Designer unter <c>Views/</c> versorgen ihre Steuerelemente ueber
/// <c>resources.ApplyResources(ctrl, "name")</c>. Koordinaten, Groessen,
/// TabIndex und Texte stehen dann nicht im Quelltext, sondern als
/// <c>&lt;data name="ctrl.Location"&gt;</c> in <c>Form_X.resx</c>. Daneben
/// liegen <c>Form_X.de-DE.resx</c> und <c>Form_X.en-US.resx</c>, die nur die
/// Texte ueberschreiben.
/// </para>
/// <para>
/// Die Werte werden in derselben Form abgelegt wie beim Designer-Leser
/// ("159, 26" fuer Punkte), damit die Karte nicht zwei Faelle kennen muss.
/// </para>
/// </summary>
public static class ResxLeser
{
    /// <summary>
    /// Legt die Werte aus den .resx-Dateien ueber die bereits gelesene Maske.
    /// <paramref name="resxPfad"/> ist die neutrale Datei; die Kulturdateien
    /// daneben werden selbst gesucht.
    /// </summary>
    public static void Anwenden(Maske maske, string? resxPfad)
    {
        resxPfad ??= Standardpfad(maske.Datei);
        if (resxPfad is null || !File.Exists(resxPfad)) return;

        var neutral = Lesen(resxPfad);
        maske.Ressourcendateien.Add(resxPfad.Replace('\\', '/'));
        Uebernehmen(maske, neutral, nurTexte: false);

        var stamm = resxPfad.Substring(0, resxPfad.Length - ".resx".Length);

        var deutsch = stamm + ".de-DE.resx";
        if (File.Exists(deutsch))
        {
            maske.Ressourcendateien.Add(deutsch.Replace('\\', '/'));
            Uebernehmen(maske, Lesen(deutsch), nurTexte: true);
        }

        var englisch = stamm + ".en-US.resx";
        if (File.Exists(englisch))
        {
            maske.Ressourcendateien.Add(englisch.Replace('\\', '/'));
            Englisch(maske, Lesen(englisch));
        }
    }

    /// <summary>Die .resx neben der Designer-Datei, gleicher Dateistamm.</summary>
    public static string? Standardpfad(string designerPfad)
    {
        var ordner = Path.GetDirectoryName(Path.GetFullPath(designerPfad));
        if (ordner is null) return null;
        return Path.Combine(ordner, DesignerLeser.Dateibezeichner(designerPfad) + ".resx");
    }

    /// <summary>Alle <c>&lt;data&gt;</c>-Eintraege einer .resx als Name-Wert-Paare.</summary>
    public static Dictionary<string, string> Lesen(string pfad)
    {
        var werte = new Dictionary<string, string>(StringComparer.Ordinal);
        XDocument dokument;
        try
        {
            dokument = XDocument.Load(pfad);
        }
        catch (Exception fehler) when (fehler is System.Xml.XmlException or IOException)
        {
            return werte;
        }

        foreach (var eintrag in dokument.Root?.Elements("data") ?? Enumerable.Empty<XElement>())
        {
            var name = (string?)eintrag.Attribute("name");
            if (string.IsNullOrEmpty(name)) continue;
            // ">>ctrl.Name", ">>ctrl.Type", ">>ctrl.Parent", ">>ctrl.ZOrder" sind
            // Buchhaltung des Designers, keine Eigenschaften.
            if (name.StartsWith(">>", StringComparison.Ordinal)) continue;
            // Eingebettete Bilder und Symbole interessieren die Karte nicht.
            if (eintrag.Attribute("mimetype") is not null) continue;

            werte[name] = eintrag.Element("value")?.Value ?? "";
        }
        return werte;
    }

    private static void Uebernehmen(Maske maske, Dictionary<string, string> werte, bool nurTexte)
    {
        foreach (var (schluessel, wert) in werte)
        {
            var punkt = schluessel.LastIndexOf('.');
            if (punkt <= 0) continue;

            var ziel = schluessel.Substring(0, punkt);
            var eigenschaft = schluessel.Substring(punkt + 1);
            if (nurTexte && eigenschaft != "Text") continue;

            if (ziel == "$this")
            {
                maske.Formular[eigenschaft] = wert;
                continue;
            }

            var element = maske.Finden(ziel);
            if (element is null) continue;
            element.Eigenschaften[eigenschaft] = wert;
        }
    }

    private static void Englisch(Maske maske, Dictionary<string, string> werte)
    {
        foreach (var (schluessel, wert) in werte)
        {
            if (!schluessel.EndsWith(".Text", StringComparison.Ordinal)) continue;
            var ziel = schluessel.Substring(0, schluessel.Length - ".Text".Length);

            if (ziel == "$this")
            {
                maske.TitelEn = wert;
                continue;
            }
            var element = maske.Finden(ziel);
            if (element is not null) element.TextEn = wert;
        }
    }
}

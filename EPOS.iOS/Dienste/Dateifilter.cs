namespace EPOS.iOS;

/// <summary>
/// Uebersetzt die Windows-Dateifilter des Bestands in die Typkennungen (UTI),
/// die der iOS-Dokumentenwaehler verlangt.
///
/// <para><b>Warum das ueberhaupt noetig ist.</b> Die drei Fundstellen des Kerns
/// (<c>FileDlgClass</c>, <c>CsvExportClass</c>, <c>ToolsClass</c>) reichen ihre
/// Filter in der Windows-Schreibweise durch -
/// <c>"xls files (*.xls)|*.xls|All files (*.*)|*.*"</c>. Die Zeichenkette dort
/// zu aendern hiesse, an einem Dutzend Aufrufstellen des Bestands zu arbeiten,
/// ohne dass sich unter Windows etwas verbessert. Uebersetzt wird deshalb hier,
/// in der Huelle, die die andere Schreibweise braucht.</para>
///
/// <para><b>Bewusst konservativ.</b> Was nicht in der Tabelle steht, wird zu
/// <c>public.data</c> - „irgendeine Datei". Lieber ein Waehler, der zu viel
/// anbietet, als einer, der die gesuchte Datei ausgraut.</para>
/// </summary>
internal static class Dateifilter
{
    /// <summary>Die Typkennung fuer „alles".</summary>
    internal const string ALLES = "public.data";

    /// <summary>Endung (mit Punkt, klein) -&gt; Typkennung.</summary>
    private static readonly Dictionary<string, string> Kennungen = new(StringComparer.OrdinalIgnoreCase)
    {
        [".csv"] = "public.comma-separated-values-text",
        [".txt"] = "public.plain-text",
        [".xml"] = "public.xml",
        [".json"] = "public.json",
        [".pdf"] = "com.adobe.pdf",
        [".zip"] = "public.zip-archive",
        [".xls"] = "com.microsoft.excel.xls",
        [".xlsx"] = "org.openxmlformats.spreadsheetml.sheet",
        [".doc"] = "com.microsoft.word.doc",
        [".docx"] = "org.openxmlformats.wordprocessingml.document",
        // Berichtsvorlagen (Konzept Berichtsvorlagen 8.5 und 10.3, Etappe BV-E1):
        // Die Vorlagenliste nimmt auch Word-Vorlagen (.dotx) auf; ohne diese Zeile
        // fiele die Endung auf public.data zurueck.
        [".dotx"] = "org.openxmlformats.wordprocessingml.template",
        // Excel-Vorlagen (Konzept Berichtsvorlagen 8.5 und 10.3, Etappe BV-E7): Die
        // Vorlagenliste nimmt auch Excel-Vorlagen (.xltx) auf; .xlsx steht oben.
        [".xltx"] = "org.openxmlformats.spreadsheetml.template",
        // iU9-W15c (Auflage O-3): Die Lizenzvereinbarung kann als .rtf vorliegen -
        // der Dateiwaehler des Lizenzdialogs bietet sie an. Fuer ".lic" gibt es
        // keine registrierte Typkennung; dort bleibt public.data die richtige
        // Antwort (Befund W15c-B14).
        [".rtf"] = "public.rtf",
        [".png"] = "public.png",
        [".jpg"] = "public.jpeg",
        [".jpeg"] = "public.jpeg",
        // Gebaeudeimport (Umsetzungskonzept Gebaeudesimulation 3.6,
        // Datenaustauschkonzept 2.5): ifcXML ist XML, ".ifczip" ein ZIP-Behaelter,
        // ".gbxml" gbXML unter eigener Endung. Fuer ".ifc" (STEP-Text) gibt es
        // keine registrierte Typkennung; dort bleibt public.data die richtige
        // Antwort.
        [".ifcxml"] = "public.xml",
        [".ifczip"] = "public.zip-archive",
        [".gbxml"] = "public.xml",
    };

    /// <summary>
    /// Die Typkennungen zu einem Windows-Filter. Leere oder unbekannte Filter
    /// ergeben <see cref="ALLES"/>; Doppelte fallen weg, die Reihenfolge bleibt.
    /// </summary>
    internal static IReadOnlyList<string> Kennungen_Zu(string? filter)
    {
        var treffer = new List<string>();

        foreach (string endung in Endungen(filter))
        {
            string kennung = Kennungen.TryGetValue(endung, out string? k) ? k : ALLES;
            if (!treffer.Contains(kennung)) treffer.Add(kennung);
        }

        if (treffer.Count == 0) treffer.Add(ALLES);
        return treffer;
    }

    /// <summary>
    /// Die Endungen eines Windows-Filters, klein und mit fuehrendem Punkt.
    ///
    /// <para>Der Filter ist paarweise aufgebaut: Beschreibung, senkrechter
    /// Strich, Mustergruppe, senkrechter Strich, naechste Beschreibung … Nur
    /// die Mustergruppen zaehlen; in ihnen trennt ein Semikolon.
    /// <c>*.*</c> steht fuer „alles" und liefert deshalb nichts, was eine
    /// Einschraenkung waere.</para>
    /// </summary>
    internal static IReadOnlyList<string> Endungen(string? filter)
    {
        var endungen = new List<string>();
        if (string.IsNullOrWhiteSpace(filter)) return endungen;

        string[] teile = filter.Split('|');
        for (int i = 1; i < teile.Length; i += 2)
        {
            foreach (string muster in teile[i].Split(';'))
            {
                string m = muster.Trim();
                if (m.Length == 0 || m == "*.*" || m == "*") continue;

                int punkt = m.LastIndexOf('.');
                if (punkt < 0) continue;

                string endung = m.Substring(punkt).ToLowerInvariant();
                if (endung.Length > 1 && !endungen.Contains(endung)) endungen.Add(endung);
            }
        }
        return endungen;
    }
}

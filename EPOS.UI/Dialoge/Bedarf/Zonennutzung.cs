using System.Globalization;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// <b>Die Angabe „Nutzung/Profil“ einer Zeile der Zonenliste</b> (Entwurf Gebäudedialog Mehrzonen 2.5, GD-5): das Profil der
/// Zone (<see cref="ZoneDaten.Nutzungsprofil"/>, sonst die Nutzung ihrer Kalender), dazu die Größen, die sie vom Gebäude erbt —
/// auch die, für die ihr Profil keinen Kennwert trägt (Lücke 3, NP-F18) — und ohne Profil die Vorgabe der Gebäudeart. Eine
/// unbeheizte Zone hat keinen Heiz- und Kühlkalender; das steht da. Rein, ohne Datenbank.
/// </summary>
public static class Zonennutzung
{
    /// <summary>
    /// Die Größen, die die Zone vom Gebäude erbt: „vom Gebäude“ oder ohne Kalender mit leerer Matrix. Heizen und Kühlen einer
    /// unbeheizten Zone zählen nicht (sie hat keine). Ohne Konditionierung im Arbeitsstand erbt sie alles.
    /// </summary>
    public static IReadOnlyList<KonditionierungGroesse> Geerbt(ZoneDaten zone)
    {
        var liste = new List<KonditionierungGroesse>();
        foreach (KonditionierungGroesse g in KonditionierungDaten.Alle)
        {
            if (!zone.IstBeheizt && (g == KonditionierungGroesse.Heizen || g == KonditionierungGroesse.Kuehlen)) continue;
            KonditionierungSpalte? s = zone.Konditionierung?.Spalte(g);
            if (s is null || s.Kalender.Zustand == KonditionierungZustand.VomGebaeude
                || (s.Kalender.Zustand == KonditionierungZustand.Abgeleitet && Enum.GetValues<KonditionierungZeile>().All(z => s.Zelle(z).Leer)))
                liste.Add(g);
        }
        return liste;
    }

    /// <summary>Erbt die Zone jede ihrer Größen vom Gebäude?</summary>
    public static bool GanzGeerbt(ZoneDaten zone)
        => Geerbt(zone).Count == KonditionierungDaten.Alle.Count(g => zone.IstBeheizt || (g != KonditionierungGroesse.Heizen && g != KonditionierungGroesse.Kuehlen));

    /// <summary>
    /// Der Text der Zelle: „Wohnen · geerbt: Lüftung“, „geerbt vom Gebäude · Vorgabe (Profil der Gebäudeart): Wohnen“,
    /// „Büro · unbeheizt — ohne Heiz- und Kühlkalender“.
    /// </summary>
    /// <param name="zone">Die Zone im Arbeitsstand.</param>
    /// <param name="vorgabe">Das Profil der Gebäudeart (<see cref="GebaeudeZonenweg.Vorgabeprofil"/>); <c>null</c> = keins.</param>
    /// <param name="t">Die Texte der Zonenliste.</param>
    /// <param name="k">Die Texte der Konditionierung (Größennamen, alte Kennungen).</param>
    public static string Text(ZoneDaten zone, string? vorgabe, GebaeudeZonenTexte t, KonditionierungTexte k)
    {
        string? name = !string.IsNullOrWhiteSpace(zone.Nutzungsprofil) ? zone.Nutzungsprofil!.Trim()
                     : !string.IsNullOrWhiteSpace(zone.Kalendernutzung) ? k.Nutzungsanzeige(zone.Kalendernutzung) : null;
        IReadOnlyList<KonditionierungGroesse> geerbt = Geerbt(zone);
        var teile = new List<string>();
        if (name is null)
        {
            teile.Add(t.NutzungGeerbt);
            if (!string.IsNullOrWhiteSpace(vorgabe)) teile.Add(string.Format(CultureInfo.CurrentCulture, t.NutzungVorgabe, vorgabe.Trim()));
        }
        else
        {
            teile.Add(name);
            if (GanzGeerbt(zone)) teile.Add(t.NutzungGeerbt);
            else if (geerbt.Count > 0)
                teile.Add(string.Format(CultureInfo.CurrentCulture, t.NutzungTeilgeerbt, string.Join(", ", geerbt.Select(g => Groessenname(g, k)))));
        }
        if (!zone.IstBeheizt) teile.Add(t.NutzungUnbeheizt);
        return string.Join(" · ", teile);
    }

    /// <summary>Der Name einer Größe aus den Texten der Konditionierung.</summary>
    public static string Groessenname(KonditionierungGroesse g, KonditionierungTexte k) => g switch
    {
        KonditionierungGroesse.Heizen => k.GroesseHeizen,
        KonditionierungGroesse.Kuehlen => k.GroesseKuehlen,
        KonditionierungGroesse.Lueftung => k.GroesseLueftung,
        KonditionierungGroesse.Geraete => k.GroesseGeraete,
        _ => k.GroessePersonen,
    };
}

using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Standards;
using WindowsFormsApplication1;

namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>Die Beschriftungen des Steckbriefs „Gewählter Lastgang" (WP1b, KA‑E‑25); füllen sich selbst aus den Ressourcen.</summary>
public sealed class WaermebedarfExternSteckbriefTexte : SatzsteckbriefTexte
{
    /// <summary>SST_GRP_LASTGANG — der Gruppenkopf über dem Steckbrief.</summary>
    public string GruppeLastgang { get; set; } = T("SST_GRP_LASTGANG", "Gewählter Lastgang");

    /// <summary>WBX_LBL_BEZEICHNER</summary>
    public string LabelBezeichner { get; set; } = T("WBX_LBL_BEZEICHNER", "Bezeichner:");

    /// <summary>STROMGL_LBL_SPITZE</summary>
    public string LabelSpitze { get; set; } = T("STROMGL_LBL_SPITZE", "Spitzenlast:");

    /// <summary>DECK_GRUPPE</summary>
    public string LabelDeckung { get; set; } = T("DECK_GRUPPE", "Deckung");

    /// <summary>SST_LBL_HERKUNFT</summary>
    public string LabelHerkunft { get; set; } = T("SST_LBL_HERKUNFT", "Herkunft");

    /// <summary>AUSWAHL_MARKE_PROJEKTSATZ / AUSWAHL_MARKE_KATALOGSATZ — die Herkunft des Satzes.</summary>
    public string HerkunftProjekt { get; set; } = T("AUSWAHL_MARKE_PROJEKTSATZ", "Projektsatz");
    public string HerkunftKatalog { get; set; } = T("AUSWAHL_MARKE_KATALOGSATZ", "Katalogsatz");

    /// <summary>Einheit der Spitzenlast; in beiden Sprachen gleich.</summary>
    public string EinheitKw { get; set; } = "kW";
}

/// <summary>
/// <b>Die Zeilen des Steckbriefs „Gewählter Lastgang"</b> der Überlagerung „Wärme-/Kältebedarf extern" (WP1b, KA‑E‑25) —
/// lesend aus der Zuordnung, die die Felder links bearbeiten, und den Kennzahlen der Ganglinie (Jahresarbeit, Spitze,
/// Vollbenutzungsstunden), die die Grafik rechts zeigt. Kanal, Art und Deckung nur am Projektsatz; fehlend „—".
/// </summary>
public static class WaermebedarfExternSteckbrief
{
    /// <summary>Die Zeilen in fester Reihenfolge.</summary>
    /// <param name="bezeichner">Der Name des Satzes.</param>
    /// <param name="istProjektsatz">Projektsatz (Zuordnung) oder Katalogsatz.</param>
    /// <param name="kanal">Der Kanal (Beschriftung, Anzeigetext); <c>null</c> = keine Zeile.</param>
    /// <param name="art">Die Art einer Kältezeile (Beschriftung, Anzeigetext); <c>null</c> = keine Zeile.</param>
    /// <param name="jahresarbeit">Beschriftung und fertiger Text der Jahresarbeit samt Einheit; Text <c>null</c> = „—".</param>
    /// <param name="kennzahlen">Die Kennzahlen der Ganglinie; <c>null</c> = „—".</param>
    /// <param name="vollbenutzung">Beschriftung und Einheit der Vollbenutzungsstunden.</param>
    /// <param name="deckung">Die Deckung einer Kühlzeile in einem Satz; <c>null</c> = keine Zeile.</param>
    public static IReadOnlyList<Steckbriefzeile> Zeilen(string bezeichner, bool istProjektsatz,
        (string Label, string Text)? kanal, (string Label, string Text)? art, (string Label, string? Text) jahresarbeit,
        GanglinienKennzahlen? kennzahlen, (string Label, string Einheit) vollbenutzung, string? deckung,
        WaermebedarfExternSteckbriefTexte texte)
    {
        var z = new List<Steckbriefzeile>
        {
            new(Satzsteckbrief.Beschriftung(texte.LabelBezeichner), bezeichner, Kennung: "name"),
        };
        if (kanal is { } k) z.Add(new(Satzsteckbrief.Beschriftung(k.Label), k.Text, Kennung: "kanal"));
        if (art is { } a) z.Add(new(Satzsteckbrief.Beschriftung(a.Label), a.Text, Kennung: "art"));
        z.Add(new(Satzsteckbrief.Beschriftung(jahresarbeit.Label), kennzahlen is null ? null : jahresarbeit.Text,
                  Kennung: "jahresarbeit"));
        z.Add(new(Satzsteckbrief.Beschriftung(texte.LabelSpitze),
                  kennzahlen is { } s ? Zahlen.Anzeigetext(s.SpitzeKw, 1) : null, texte.EinheitKw, Kennung: "spitze"));
        z.Add(new(Satzsteckbrief.Beschriftung(vollbenutzung.Label),
                  kennzahlen?.VollbenutzungsstundenH is double h ? Zahlen.Anzeigetext(h, 0) : null, vollbenutzung.Einheit,
                  Kennung: "vollbenutzung"));
        if (deckung is not null) z.Add(new(texte.LabelDeckung, deckung, Kennung: "deckung"));
        z.Add(new(texte.LabelHerkunft, istProjektsatz ? texte.HerkunftProjekt : texte.HerkunftKatalog, Kennung: "herkunft"));
        return z;
    }
}

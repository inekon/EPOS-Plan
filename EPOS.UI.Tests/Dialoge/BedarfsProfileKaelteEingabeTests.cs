using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.Extensions.DependencyInjection;
using EPOS.UI.Standards;
using WindowsFormsApplication1;
using Resource = WindowsFormsApplication1.MyResource.Resource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>KBP-3a: die Eingabearten der Kältezeile in der Bearbeiten-Überlagerung des <see cref="BedarfsProfileDialog"/></b>
/// (Konzept Kältebedarfsprofile 3.2, 3.3, 3.5): Wahl der Eingabeart (Typtage gesperrt), Editor der Monatswerte in MWh und
/// % samt Summenprüfung, Editor des Wochenprofils in beiden Ansichten, Übernahme mit OK in den Arbeitsstand der Zeile
/// (geschrieben erst vom OK des Dialogs über die Vormerkung der Hülle), Abbrechen verwirft, der Art-Vorschlag einer neu
/// übernommenen Zeile bleibt stehen.
/// </summary>
public class BedarfsProfileKaelteEingabeTests : EposBunitContext
{
    private static readonly double[] ANTEILE = { 2, 3, 5, 8, 10, 14, 16, 15, 12, 8, 4, 3 };

    private static List<BedarfsProfilZeile> Zeilen() => new()
    {
        new()
        {
            IdZ = 1, IdStamm = 3, Name = "Büro", Summe = 20, Deckung = new DeckungsartDaten(),
            KaelteArt = DbWerte.KAELTE_ART_RAUM, Eingabeart = DbWerte.KAELTE_EINGABE_JAHR,
            Monatsanteile = (double[])ANTEILE.Clone(),
            Wochenprofil = Kaelteprofileingabe.WocheAusTagen(Tag(1), Tag(0.5), Tag(0.2))
        },
    };

    private static double[] Tag(double w) => Enumerable.Repeat(w, 24).ToArray();

    public BedarfsProfileKaelteEingabeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static IReadOnlyList<Katalogfilterzeile> Katalog() => new[]
    {
        new Katalogfilterzeile(9, "Tiefkühlraum")
            .MitText(Katalogfilterprofil.SpBezeichner, "Tiefkühlraum")
            .MitText(Katalogfilterprofil.SpTyp, "Typ 1")
            .MitZahl(Katalogfilterprofil.SpJahressummeMwh, 5, 3)
            .MitText(Katalogfilterprofil.SpBeschreibung, "")
            .MitKennzeichen(Katalogfilterprofil.SpAuslieferung, false)
    };

    private IRenderedComponent<BedarfsProfileDialog> Aufbauen(List<BedarfsProfilZeile> zeilen, Action? geaendert = null,
                                                             Func<string, BedarfsProfilZeile?>? aufnehmen = null)
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Kaelte)
            .Add(x => x.TitelText, "Kältebedarf")
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => Katalog())
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Kaelte, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "", "Typ 1"))
            .Add(x => x.Jahressumme, _ => 1.0)
            .Add(x => x.Aufnehmen, aufnehmen)
            .Add(x => x.KaelteArten, new[] { (DbWerte.KAELTE_ART_RAUM, "Raum"), (DbWerte.KAELTE_ART_PROZESS, "Prozess") })
            .Add(x => x.Geaendert, geaendert)
            .Add(x => x.Einheit, Energieeinheit.MWh));

    private static IElement Ueberlagerung(IRenderedComponent<BedarfsProfileDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt").Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    private static IRenderedComponent<Auswahlfeld> Wahl(IRenderedComponent<BedarfsProfileDialog> cut, string bezeichnung)
        => cut.FindComponents<Auswahlfeld>().Single(a => a.Instance.Bezeichnung == bezeichnung);

    private static void Eingabeart(IRenderedComponent<BedarfsProfileDialog> cut, int index)
        => Wahl(cut, Resource.KBP_LBL_EINGABEART).Find("select").Change(index.ToString());

    private static void Ok(IRenderedComponent<BedarfsProfileDialog> cut) => cut.Find(".epos-satzueberlagerung-ok").Click();

    [Fact]
    public void Die_Eingabeart_steht_ueber_den_Feldern_Typtage_sind_gesperrt()
    {
        var cut = Aufbauen(Zeilen());
        IElement satz = Ueberlagerung(cut);
        Assert.Contains(Resource.KBP_GRUPPE_EINGABEART, satz.TextContent);
        var wahl = Wahl(cut, Resource.KBP_LBL_EINGABEART);
        Assert.Equal(0, wahl.Instance.Auswahl);
        Assert.Contains(3, wahl.Instance.GesperrteEintraege);
        Assert.NotNull(wahl.Find("option[value='3'][disabled]"));
        Assert.Empty(cut.FindComponents<KaelteMonatswerteEditor>());
        Assert.Empty(cut.FindComponents<KaelteWochenprofilEditor>());

        // Typtage lassen sich nicht wählen; die Wahl bleibt bei der Jahressumme.
        Eingabeart(cut, 3);
        Assert.Equal(0, Wahl(cut, Resource.KBP_LBL_EINGABEART).Instance.Auswahl);

        // Prozesswärme kennt die Wahl nicht.
        var prozess = Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Prozesswaerme)
            .Add(x => x.Zeilen, new List<BedarfsProfilZeile> { new() { IdZ = 1, IdStamm = 3, Name = "P", Summe = 1 } })
            .Add(x => x.Katalogzeilen, () => new List<Katalogfilterzeile>())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand()));
        Assert.DoesNotContain(Resource.KBP_GRUPPE_EINGABEART, Ueberlagerung(prozess).TextContent);
    }

    [Fact]
    public void Monatswerte_in_MWh_setzen_Jahressumme_und_Anteile_mit_OK()
    {
        int gemeldet = 0;
        var zeilen = Zeilen();
        var cut = Aufbauen(zeilen, () => gemeldet++);
        Ueberlagerung(cut);
        Eingabeart(cut, 1);

        var editor = cut.FindComponent<KaelteMonatswerteEditor>();
        Assert.False(editor.Instance.Prozent);
        // Vorbelegung: Vorlage · Summe; die Jahressumme ist hier keine Eingabe.
        Assert.Equal(20.0 * 16 / 100, editor.Instance.Werte[6]!.Value, 9);
        Assert.Empty(cut.FindComponents<Zahlenfeld>().Where(z => z.Instance.Bezeichnung == "neuer Wert"));
        editor.FindComponents<Zahlenfeld>()[6].Find("input").Input("10");
        Assert.Contains("26", editor.Markup);                                                   // Summe aus den Monaten

        Ok(cut);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(DbWerte.KAELTE_EINGABE_MONAT, zeilen[0].Eingabeart);
        Assert.Equal(26.8, zeilen[0].Summe, 9);
        Assert.Equal(100.0 * 10 / 26.8, zeilen[0].Monatsanteile![6], 9);
        Assert.True(zeilen[0].ProfilGeaendert);
        Assert.True(gemeldet > 0);
    }

    [Fact]
    public void Monatswerte_in_Prozent_muessen_100_ergeben()
    {
        var zeilen = Zeilen();
        var cut = Aufbauen(zeilen);
        Ueberlagerung(cut);
        Eingabeart(cut, 1);
        cut.FindComponent<KaelteMonatswerteEditor>().FindComponents<Auswahlfeld>()[0].Find("select").Change("1");
        var editor = cut.FindComponent<KaelteMonatswerteEditor>();
        Assert.True(editor.Instance.Prozent);
        Assert.Equal(16.0, editor.Instance.Werte[6]!.Value, 9);                                 // umgerechnet in %

        editor.FindComponents<Zahlenfeld>()[0].Find("input").Input("3");                        // 101 %
        Assert.NotNull(cut.Find(".epos-kbp-summenfehler"));
        Ok(cut);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.Contains(string.Format(Resource.KBP_MSG_SUMME_PROZENT, "101"), cut.Markup);
        Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, zeilen[0].Eingabeart);
        Assert.False(zeilen[0].ProfilGeaendert);

        cut.FindComponent<KaelteMonatswerteEditor>().FindComponents<Zahlenfeld>()[1].Find("input").Input("2");
        Ok(cut);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(20.0, zeilen[0].Summe, 12);                                                 // % behält die Jahressumme
        Assert.Equal(3.0, zeilen[0].Monatsanteile![0], 12);
        Assert.Equal(2.0, zeilen[0].Monatsanteile![1], 12);
    }

    [Fact]
    public void Wochenprofil_Werktag_schreibt_Montag_bis_Freitag_168h_mit_Tag_kopieren()
    {
        var zeilen = Zeilen();
        var cut = Aufbauen(zeilen);
        Ueberlagerung(cut);
        Eingabeart(cut, 2);
        var editor = cut.FindComponent<KaelteWochenprofilEditor>();
        Assert.True(editor.Instance.DreiTage);
        editor.FindComponents<Zahlenfeld>()[8].Find("input").Input("4");                        // Werktag 8 Uhr

        // 168 h: Dienstag wählen, Montag kopieren und am Samstag einfügen.
        editor.FindComponents<Auswahlfeld>()[0].Find("select").Change("1");
        editor = cut.FindComponent<KaelteWochenprofilEditor>();
        Assert.False(editor.Instance.DreiTage);
        editor.Find(".epos-leiste .epos-knopf").Click();                                         // Tag kopieren (Montag)
        editor.FindComponents<Auswahlfeld>()[1].Find("select").Change("5");                       // Samstag
        editor.FindAll(".epos-leiste .epos-knopf")[1].Click();                                   // Tag einfügen

        Ok(cut);
        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        double[] w = zeilen[0].Wochenprofil!;
        Assert.Equal(DbWerte.KAELTE_EINGABE_WOCHE, zeilen[0].Eingabeart);
        for (int d = 0; d < 5; d++) Assert.Equal(4.0, w[d * 24 + 8]);
        Assert.Equal(4.0, w[5 * 24 + 8]);
        Assert.Equal(1.0, w[5 * 24 + 7]);
        Assert.Equal(0.2, w[6 * 24 + 8]);
        Assert.True(zeilen[0].ProfilGeaendert);
    }

    [Fact]
    public void Ungleiche_Werktage_sperren_die_Dreitagesansicht()
    {
        var zeilen = Zeilen();
        zeilen[0].Wochenprofil![24 + 3] = 9;
        var cut = Aufbauen(zeilen);
        Ueberlagerung(cut);
        Eingabeart(cut, 2);
        var editor = cut.FindComponent<KaelteWochenprofilEditor>();
        Assert.False(editor.Instance.DreiTage);
        Assert.Contains(Resource.KBP_HINW_WERKTAGE_UNGLEICH, editor.Markup);
        Assert.NotNull(editor.Find("option[value='0'][disabled]"));
    }

    [Fact]
    public void Abbrechen_verwirft_Eingabeart_und_Werte()
    {
        int gemeldet = 0;
        var zeilen = Zeilen();
        var cut = Aufbauen(zeilen, () => gemeldet++);
        Ueberlagerung(cut);
        Eingabeart(cut, 1);
        cut.FindComponent<KaelteMonatswerteEditor>().FindComponents<Zahlenfeld>()[0].Find("input").Input("50");
        cut.Find(".epos-satzueberlagerung-abbrechen").Click();

        Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, zeilen[0].Eingabeart);
        Assert.Equal(20.0, zeilen[0].Summe);
        Assert.Equal(ANTEILE, zeilen[0].Monatsanteile);
        Assert.False(zeilen[0].ProfilGeaendert);
        Assert.Equal(0, gemeldet);

        // Wieder geöffnet: der Stand der Zeile, nicht der verworfene.
        Ueberlagerung(cut);
        Assert.Equal(0, Wahl(cut, Resource.KBP_LBL_EINGABEART).Instance.Auswahl);
    }

    [Fact]
    public void Der_Art_Vorschlag_einer_neu_uebernommenen_Zeile_bleibt_stehen()
    {
        var zeilen = new List<BedarfsProfilZeile>();
        var cut = Aufbauen(zeilen, aufnehmen: n => new BedarfsProfilZeile
        {
            IdZ = 100000, IdStamm = 9, Name = n, Summe = 5, Deckung = new DeckungsartDaten(),
            KaelteArt = DbWerte.KAELTE_ART_PROZESS, Eingabeart = DbWerte.KAELTE_EINGABE_JAHR
        });
        cut.FindAll(".epos-katalogliste .epos-zeilenzelle--name")[0].Click();
        cut.FindAll(".epos-zweispalten-knopf--uebernehmen")[0].Click();
        Assert.Single(zeilen);
        Assert.Equal(DbWerte.KAELTE_ART_PROZESS, zeilen[0].KaelteArt);
        Ueberlagerung(cut);
        Assert.Equal(1, Wahl(cut, Resource.KAELTE_ART_LABEL).Instance.Auswahl);
    }
}

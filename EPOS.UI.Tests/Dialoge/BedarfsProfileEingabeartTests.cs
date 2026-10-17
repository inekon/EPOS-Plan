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
/// <b>KBP-5a: die Eingabearten für Prozesswärme und Brauchwasser im <see cref="BedarfsProfileDialog"/></b> (Entscheid
/// OE-5, „derselbe Dialog“): dieselbe Wahl wie bei der Kälte (Vorgabe Jahressumme, Typtage gesperrt), Monatswerte und
/// Wochenprofil mit OK in den Arbeitsstand der Zeile; beim Brauchwasser mit Zapfprofilgenerator steht statt der Wahl der
/// Grund, und eine Wahl über den Delegat greift nicht.
/// </summary>
public class BedarfsProfileEingabeartTests : EposBunitContext
{
    private static readonly double[] ANTEILE = { 10, 9, 9, 8, 8, 7, 7, 7, 8, 9, 9, 9 };

    private static double[] Tag(double w) => Enumerable.Repeat(w, 24).ToArray();

    private static List<BedarfsProfilZeile> Zeilen() => new()
    {
        new()
        {
            IdZ = 1, IdStamm = 13, Name = "Profil A", Summe = 30, Eingabeart = DbWerte.KAELTE_EINGABE_JAHR,
            Monatsanteile = (double[])ANTEILE.Clone(),
            Wochenprofil = Kaelteprofileingabe.WocheAusTagen(Tag(1), Tag(0.5), Tag(0.2))
        },
    };

    public BedarfsProfileEingabeartTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<BedarfsProfileDialog> Aufbauen(BedarfsArt art, List<BedarfsProfilZeile> zeilen,
                                                             Action? geaendert = null, ZapfprofilWeg? weg = null)
        => Render<BedarfsProfileDialog>(p =>
        {
            p.Add(x => x.Art, art)
             .Add(x => x.TitelText, "Bedarf")
             .Add(x => x.Zeilen, zeilen)
             .Add(x => x.Katalogzeilen, () => new List<Katalogfilterzeile>())
             .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
             .Add(x => x.Info, n => new BedarfsProfilInfo(n, "", "Typ 1"))
             .Add(x => x.Jahressumme, _ => 1.0)
             .Add(x => x.Geaendert, geaendert)
             .Add(x => x.Einheit, Energieeinheit.MWh);
            if (weg is ZapfprofilWeg w)
                p.Add(x => x.RechenwegBrauchwasser, w).Add(x => x.RechenwegGesetzt, _ => { });
        });

    private static IElement Ueberlagerung(IRenderedComponent<BedarfsProfileDialog> cut)
    {
        if (cut.FindAll(".epos-satzueberlagerung-koerper").Count == 0)
            cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--bearbeiten-projekt").Click();
        return cut.Find(".epos-satzueberlagerung-koerper");
    }

    private static IRenderedComponent<Auswahlfeld> Wahl(IRenderedComponent<BedarfsProfileDialog> cut)
        => cut.FindComponents<Auswahlfeld>().Single(a => a.Instance.Bezeichnung == Resource.KBP_LBL_EINGABEART);

    private static void Ok(IRenderedComponent<BedarfsProfileDialog> cut) => cut.Find(".epos-satzueberlagerung-ok").Click();

    [Theory]
    [InlineData(BedarfsArt.Prozesswaerme)]
    [InlineData(BedarfsArt.Brauchwasser)]
    public void Die_Wahl_steht_mit_Vorgabe_Jahressumme_Typtage_gesperrt(BedarfsArt art)
    {
        var cut = Aufbauen(art, Zeilen());
        Assert.Contains(Resource.KBP_GRUPPE_EINGABEART, Ueberlagerung(cut).TextContent);
        var wahl = Wahl(cut);
        Assert.Equal(0, wahl.Instance.Auswahl);
        Assert.Contains(3, wahl.Instance.GesperrteEintraege);
        Assert.Empty(cut.FindComponents<KaelteMonatswerteEditor>());
        Assert.Empty(cut.FindComponents<KaelteWochenprofilEditor>());
    }

    [Theory]
    [InlineData(BedarfsArt.Prozesswaerme)]
    [InlineData(BedarfsArt.Brauchwasser)]
    public void Monatswerte_in_MWh_setzen_Jahressumme_und_Anteile_mit_OK(BedarfsArt art)
    {
        int gemeldet = 0;
        var zeilen = Zeilen();
        var cut = Aufbauen(art, zeilen, () => gemeldet++);
        Ueberlagerung(cut);
        Wahl(cut).Find("select").Change("1");

        var editor = cut.FindComponent<KaelteMonatswerteEditor>();
        Assert.Equal(30.0 * 7 / 100, editor.Instance.Werte[6]!.Value, 9);                       // Vorlage · Summe
        editor.FindComponents<Zahlenfeld>()[6].Find("input").Input("4.9");                       // Juli +2,8 MWh
        Ok(cut);

        Assert.False(cut.Instance.SatzUeberlagerungOffen);
        Assert.Equal(DbWerte.KAELTE_EINGABE_MONAT, zeilen[0].Eingabeart);
        Assert.Equal(32.8, zeilen[0].Summe, 9);
        Assert.Equal(100.0, zeilen[0].Monatsanteile!.Sum(), 9);
        Assert.Equal(100.0 * 4.9 / 32.8, zeilen[0].Monatsanteile![6], 9);
        Assert.True(zeilen[0].ProfilGeaendert);
        Assert.True(gemeldet > 0);
    }

    [Theory]
    [InlineData(BedarfsArt.Prozesswaerme)]
    [InlineData(BedarfsArt.Brauchwasser)]
    public void Wochenprofil_Werktag_schreibt_Montag_bis_Freitag_und_behaelt_die_Summe(BedarfsArt art)
    {
        var zeilen = Zeilen();
        var cut = Aufbauen(art, zeilen);
        Ueberlagerung(cut);
        Wahl(cut).Find("select").Change("2");
        var editor = cut.FindComponent<KaelteWochenprofilEditor>();
        Assert.True(editor.Instance.DreiTage);
        editor.FindComponents<Zahlenfeld>()[8].Find("input").Input("4");                        // Werktag 8 Uhr
        Ok(cut);

        Assert.Equal(DbWerte.KAELTE_EINGABE_WOCHE, zeilen[0].Eingabeart);
        Assert.Equal(30.0, zeilen[0].Summe, 12);
        for (int d = 0; d < Kaelteprofileingabe.SAMSTAG; d++) Assert.Equal(4.0, zeilen[0].Wochenprofil![d * 24 + 8]);
        Assert.Equal(0.5, zeilen[0].Wochenprofil![Kaelteprofileingabe.SAMSTAG * 24 + 8]);
        Assert.True(zeilen[0].ProfilGeaendert);
    }

    [Fact]
    public void Abbrechen_verwirft_die_Wahl()
    {
        var zeilen = Zeilen();
        var cut = Aufbauen(BedarfsArt.Prozesswaerme, zeilen);
        Ueberlagerung(cut);
        Wahl(cut).Find("select").Change("1");
        cut.Find(".epos-satzueberlagerung-abbrechen").Click();
        Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, zeilen[0].Eingabeart);
        Assert.False(zeilen[0].ProfilGeaendert);
    }

    [Fact]
    public void Brauchwasser_mit_Zapfprofilgenerator_bietet_keine_Eingabeart_an()
    {
        var zeilen = Zeilen();
        var cut = Aufbauen(BedarfsArt.Brauchwasser, zeilen, weg: ZapfprofilWeg.Generator);
        IElement satz = Ueberlagerung(cut);
        Assert.Contains(Resource.KBP_HINW_EINGABEART_ZAPFPROFIL, satz.TextContent);
        Assert.Empty(cut.FindComponents<Auswahlfeld>().Where(a => a.Instance.Bezeichnung == Resource.KBP_LBL_EINGABEART));
        Ok(cut);
        Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, zeilen[0].Eingabeart);
        Assert.False(zeilen[0].ProfilGeaendert);

        // Auf dem Profilweg steht die Wahl wieder.
        var bestand = Aufbauen(BedarfsArt.Brauchwasser, Zeilen(), weg: ZapfprofilWeg.Bestand);
        Assert.Contains(Resource.KBP_GRUPPE_EINGABEART, Ueberlagerung(bestand).TextContent);
    }

    [Fact]
    public void Ohne_Projektkopie_bleibt_die_Wahl_aus()
    {
        var cut = Aufbauen(BedarfsArt.Brauchwasser,
                           new List<BedarfsProfilZeile> { new() { IdZ = 1, IdStamm = 13, Name = "Profil A", Summe = 3 } });
        Assert.DoesNotContain(Resource.KBP_GRUPPE_EINGABEART, Ueberlagerung(cut).TextContent);
    }
}

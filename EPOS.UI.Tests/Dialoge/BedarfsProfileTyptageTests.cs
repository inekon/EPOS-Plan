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
/// <b>KBP-4b: der Typtage-Editor der Kältezeile im <see cref="BedarfsProfileDialog"/></b> (Konzept Kältebedarfsprofile
/// 3.4, Entscheid OE-1): Wechsel auf Typtage mit Vorbelegung aus dem Wochenprofil, Wahl einer Matrixzelle, Hinweis bei
/// leerer Zelle (dieselbe Aussage wie im Lauf), Übernahme mit OK in den Arbeitsstand der Zeile (geschrieben erst vom OK
/// des Dialogs über die Hülle), Abbrechen verwirft, die Grafik folgt dem Arbeitsstand, ohne Typtag-Tabellen bleibt die
/// Wahl benannt gesperrt.
/// </summary>
public class BedarfsProfileTyptageTests : EposBunitContext
{
    private static List<BedarfsProfilZeile> Zeilen() => new()
    {
        new()
        {
            IdZ = 1, IdStamm = 3, Name = "Büro", Summe = 20, Deckung = new DeckungsartDaten(),
            KaelteArt = DbWerte.KAELTE_ART_RAUM, Eingabeart = DbWerte.KAELTE_EINGABE_JAHR,
            Monatsanteile = Enumerable.Repeat(100.0 / 12, 12).ToArray(),
            Wochenprofil = Kaelteprofileingabe.WocheAusTagen(Tag(1), Tag(0.5), Tag(0.2))
        },
    };

    private static double[] Tag(double w) => Enumerable.Repeat(w, 24).ToArray();

    public BedarfsProfileTyptageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private BedarfsProfilZeile? _grafikMarkiert;

    private IRenderedComponent<BedarfsProfileDialog> Aufbauen(List<BedarfsProfilZeile> zeilen, Action? geaendert = null,
                                                             bool verfuegbar = true)
        => Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Kaelte)
            .Add(x => x.TitelText, "Kältebedarf")
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Katalogzeilen, () => new List<Katalogfilterzeile>())
            .Add(x => x.Katalogprofil, Katalogfilterprofil.FuerBedarf(BedarfsArt.Kaelte, s => s))
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.Info, n => new BedarfsProfilInfo(n, "", "Typ 1"))
            .Add(x => x.Jahressumme, _ => 1.0)
            .Add(x => x.KaelteArten, new[] { (DbWerte.KAELTE_ART_RAUM, "Raum"), (DbWerte.KAELTE_ART_PROZESS, "Prozess") })
            .Add(x => x.KaelteGrafik, (m, _) => { _grafikMarkiert = m; return null; })
            .Add(x => x.TyptageVerfuegbar, verfuegbar)
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

    private static string Zelle(int monat, int art)
        => Resource.KBP_MONATE.Split(';')[monat] + " – " + Resource.KBP_TYPTAG_TAGESARTEN.Split(';')[art];

    private static void ZelleWaehlen(IRenderedComponent<BedarfsProfileDialog> cut, int monat, int art, int nr)
        => cut.FindComponent<KaelteTyptagEditor>().FindComponents<Auswahlfeld>()
              .Single(a => a.Instance.Titel == Zelle(monat, art)).Find("select").Change(nr.ToString());

    private static void Ok(IRenderedComponent<BedarfsProfileDialog> cut) => cut.Find(".epos-satzueberlagerung-ok").Click();

    [Fact]
    public void Der_Wechsel_auf_Typtage_belegt_drei_Typtage_aus_dem_Wochenprofil_vor()
    {
        var cut = Aufbauen(Zeilen());
        Ueberlagerung(cut);
        var wahl = Wahl(cut, Resource.KBP_LBL_EINGABEART);
        Assert.DoesNotContain(3, wahl.Instance.GesperrteEintraege);
        Eingabeart(cut, 3);

        var editor = cut.FindComponent<KaelteTyptagEditor>();
        Assert.Contains(Resource.KBP_HINW_TYPTAG_VORBELEGT, editor.Markup);
        KaeltetyptagEingabe e = cut.Instance.TyptageArbeitsstand!;
        Assert.Equal(new[] { 1, 2, 3 }, e.Typtage.Select(t => t.Nr));
        Assert.Equal(Resource.KBP_TYPTAG_TAGESARTEN.Split(';'), e.Typtage.Select(t => t.Bezeichner));
        // Stunden in % der Tagesspitze, Tagesmenge relativ zum Werktag: Samstag 0,5, Sonntag 0,2.
        Assert.All(e.Typtage, t => Assert.All(t.Stunden, h => Assert.Equal(100.0, h, 6)));
        Assert.Equal(new[] { 1.0, 0.5, 0.2 }, e.Typtage.Select(t => Math.Round(t.Tagesmenge, 6)));
        for (int m = 0; m < 12; m++)
            for (int a = 0; a < 3; a++) Assert.Equal(a + 1, e.Matrix[m, a]);
        // Vollständig vorbelegt: kein Hinweis.
        Assert.Empty(editor.FindAll(".epos-kbp-typtag-hinweis"));
        // 36 Matrixzellen als Auswahlfeld, kein datalist.
        Assert.Equal(36, editor.FindAll(".epos-kbp-matrix select").Count);
        Assert.Empty(editor.FindAll("datalist"));
    }

    [Fact]
    public void Eine_Matrixzelle_waehlt_ihren_Typtag_und_eine_leere_Zelle_bringt_den_Hinweis()
    {
        var cut = Aufbauen(Zeilen());
        Ueberlagerung(cut);
        Eingabeart(cut, 3);
        ZelleWaehlen(cut, 6, 0, 2);
        Assert.Equal(2, cut.Instance.TyptageArbeitsstand!.Matrix[6, 0]);

        ZelleWaehlen(cut, 0, 1, 0);
        Assert.Equal(0, cut.Instance.TyptageArbeitsstand!.Matrix[0, 1]);
        string erwartet = string.Format(System.Globalization.CultureInfo.CurrentCulture, Resource.SIMENG_TYPTAG_ZELLEN_LEER,
                                        "", "Büro", 1, "1/" + KaeltetyptagSchema.TAGESARTEN[1]);
        var hinweise = cut.FindComponent<KaelteTyptagEditor>().FindAll(".epos-kbp-typtag-hinweis");
        Assert.Contains(hinweise, h => h.TextContent == erwartet);

        // Ein Typtag ohne Stunden: derselbe Hinweis wie im Lauf.
        for (int h = 1; h <= 24; h++)
            cut.FindComponent<KaelteTyptagEditor>().FindComponents<Zahlenfeld>()
               .Single(z => z.Instance.Feldname == "kbp_typtag_stunde_" + h).Find("input").Input("0");
        Assert.Contains(cut.FindComponent<KaelteTyptagEditor>().FindAll(".epos-kbp-typtag-hinweis"),
                        h => h.TextContent == string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                                            Resource.SIMENG_TYPTAG_OHNE_STUNDEN, "", "Büro", 1));
    }

    [Fact]
    public void OK_uebernimmt_Typtage_und_Matrix_in_die_Zeile()
    {
        int gemeldet = 0;
        var zeilen = Zeilen();
        var cut = Aufbauen(zeilen, () => gemeldet++);
        Ueberlagerung(cut);
        Eingabeart(cut, 3);
        cut.Find(".epos-kbp-typtag-neu").Click();
        ZelleWaehlen(cut, 7, 0, 4);
        Ok(cut);

        BedarfsProfilZeile z = zeilen[0];
        Assert.Equal(DbWerte.KAELTE_EINGABE_TYPTAG, z.Eingabeart);
        Assert.True(z.TyptageGeaendert);
        Assert.Equal(4, z.Typtage!.Typtage.Count);
        Assert.Equal(string.Format(Resource.KBP_TYPTAG_NEU_NAME, 4), z.Typtage.Typtag(4)!.Bezeichner);
        Assert.Equal(4, z.Typtage.Matrix[7, 0]);
        Assert.Equal(1, z.Typtage.Matrix[6, 0]);
        Assert.Null(z.Typtage.Pruefen());
        Assert.False(z.ProfilGeaendert);           // Monate und Woche bleiben unangetastet
        Assert.True(gemeldet > 0);

        // Die Zeile hält eine Kopie: eine spätere Bearbeitung ändert sie erst mit dem nächsten OK.
        Ueberlagerung(cut);
        Assert.Equal(3, Wahl(cut, Resource.KBP_LBL_EINGABEART).Instance.Auswahl);
        Assert.NotSame(z.Typtage, cut.Instance.TyptageArbeitsstand);
        Assert.Empty(cut.FindComponent<KaelteTyptagEditor>().FindAll(".epos-herleitung")
                        .Where(h => h.TextContent.Contains(Resource.KBP_HINW_TYPTAG_VORBELEGT)));
    }

    [Fact]
    public void Ein_Typtag_ohne_Bezeichner_haelt_die_Ueberlagerung_offen()
    {
        var zeilen = Zeilen();
        var cut = Aufbauen(zeilen);
        Ueberlagerung(cut);
        Eingabeart(cut, 3);
        cut.FindComponent<KaelteTyptagEditor>().FindComponent<Textfeld>().Find("input").Input("  ");
        Ok(cut);
        Assert.True(cut.Instance.SatzUeberlagerungOffen);
        Assert.False(zeilen[0].TyptageGeaendert);
        Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, zeilen[0].Eingabeart);
    }

    [Fact]
    public void Abbrechen_schreibt_keine_Typtage()
    {
        int gemeldet = 0;
        var zeilen = Zeilen();
        var cut = Aufbauen(zeilen, () => gemeldet++);
        Ueberlagerung(cut);
        Eingabeart(cut, 3);
        ZelleWaehlen(cut, 0, 0, 2);
        cut.Find(".epos-satzueberlagerung-abbrechen").Click();

        Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, zeilen[0].Eingabeart);
        Assert.Null(zeilen[0].Typtage);
        Assert.False(zeilen[0].TyptageGeaendert);
        Assert.Equal(0, gemeldet);
        Assert.Null(cut.Instance.TyptageArbeitsstand);
    }

    [Fact]
    public void Die_Grafik_rechnet_die_ungespeicherten_Typtage()
    {
        var cut = Aufbauen(Zeilen());
        Ueberlagerung(cut);
        Assert.NotEqual(DbWerte.KAELTE_EINGABE_TYPTAG, _grafikMarkiert!.Eingabeart);
        Eingabeart(cut, 3);
        Assert.Equal(DbWerte.KAELTE_EINGABE_TYPTAG, _grafikMarkiert!.Eingabeart);
        Assert.True(_grafikMarkiert.TyptageGeaendert);
        Assert.Equal(1, _grafikMarkiert.Typtage!.Matrix[4, 0]);

        ZelleWaehlen(cut, 4, 0, 3);
        Assert.Equal(3, _grafikMarkiert!.Typtage!.Matrix[4, 0]);
    }

    [Fact]
    public void Ohne_Typtag_Tabellen_und_ausserhalb_der_Kaelte_bleiben_Typtage_benannt_gesperrt()
    {
        var cut = Aufbauen(Zeilen(), verfuegbar: false);
        Ueberlagerung(cut);
        var wahl = Wahl(cut, Resource.KBP_LBL_EINGABEART);
        Assert.Contains(3, wahl.Instance.GesperrteEintraege);
        Assert.Equal(Resource.KBP_HINW_TYPTAG_FOLGT, wahl.Instance.GesperrtHinweis);
        Eingabeart(cut, 3);
        Assert.Empty(cut.FindComponents<KaelteTyptagEditor>());

        var prozess = Render<BedarfsProfileDialog>(p => p
            .Add(x => x.Art, BedarfsArt.Prozesswaerme)
            .Add(x => x.Zeilen, new List<BedarfsProfilZeile>
            {
                new() { IdZ = 1, IdStamm = 3, Name = "P", Summe = 1, Eingabeart = DbWerte.KAELTE_EINGABE_JAHR,
                        Monatsanteile = new double[12], Wochenprofil = new double[168] }
            })
            .Add(x => x.Katalogzeilen, () => new List<Katalogfilterzeile>())
            .Add(x => x.Filterstandvorgabe, new Katalogfilterstand())
            .Add(x => x.TyptageVerfuegbar, true));
        Ueberlagerung(prozess);
        var pw = Wahl(prozess, Resource.KBP_LBL_EINGABEART);
        Assert.Contains(3, pw.Instance.GesperrteEintraege);
        Assert.Equal(Resource.KBP_HINW_TYPTAG_NUR_KAELTE, pw.Instance.GesperrtHinweis);
    }
}

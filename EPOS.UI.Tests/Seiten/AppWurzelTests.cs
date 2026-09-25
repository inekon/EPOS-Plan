using System.Globalization;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using EPOS.UI.Seiten;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Seiten;

/// <summary>
/// AppWurzel - die Zustandsmaschine der iOS-Huelle (iU10-2).
///
/// Geprueft wird das, was die Huelle von ihr erwartet: Sie zeigt beim Start die
/// Liste, schaltet auf Knopfdruck in einen Dialog, kommt von dort zurueck und
/// laedt dabei nach. Der Kern kommt in diesen Tests nicht vor - die Daten
/// liefert <see cref="TestProjektquelle"/>.
///
/// UI-Kultur auf de-DE gepinnt wie in SpeichernLeisteTests: Die Beschriftungen
/// der Dialoge stammen aus dem Ressourcenkatalog des Kerns, und die CI-Laeufer
/// auf macOS und Windows laufen englisch.
/// </summary>
public class AppWurzelTests : EposBunitContext
{
    private static readonly ProjektZeile[] ZweiProjekte =
    {
        new ProjektZeile(1030, "B3-Kaskade", "Region 12", "WP+BHKW"),
        new ProjektZeile(1007, "Speichervariante A", "Region 12", "WP+Speicher")
    };

    public AppWurzelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    protected override void Dispose(bool disposing)
    {
        Navigationsziel.Aktuell = null;
        base.Dispose(disposing);
    }

    private IRenderedComponent<AppWurzel> Aufbauen(TestProjektquelle quelle)
    {
        Services.AddSingleton<IProjektQuelle>(quelle);
        return Render<AppWurzel>();
    }

    [Fact]
    public void Beim_Start_steht_die_Projektliste()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Empty(cut.FindAll(".epos-dialog"));
        Assert.Equal(2, cut.Instance.ProjektAnzahl);
    }

    [Fact]
    public void Der_Klick_schaltet_von_der_Liste_in_den_Dialog()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        cut.FindAll(".epos-projekt-energie")[0].Click();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Equal("Energieträger Variante", cut.Find(".epos-dialog-titel").TextContent);
    }

    [Fact]
    public void Abbrechen_fuehrt_zurueck_zur_Liste_und_laedt_neu()
    {
        var quelle = new TestProjektquelle(ZweiProjekte);
        var cut = Aufbauen(quelle);
        int vorher = quelle.Geladen;

        cut.FindAll(".epos-projekt-energie")[0].Click();
        // Im Dialog ist Abbrechen der erste Knopf der Speichernleiste.
        cut.FindAll(".epos-dialog button").Last(k => !k.ClassList.Contains("epos-knopf--primaer")).Click();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Equal(vorher + 1, quelle.Geladen);
        Assert.Null(quelle.Uebernommen);
    }

    [Fact]
    public void Ein_OK_reicht_das_Ergebnis_an_die_Huelle_weiter()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { Antwort = "Erdgas Sondertarif" };
        var cut = Aufbauen(quelle);

        cut.FindAll(".epos-projekt-energie")[1].Click();
        cut.Find(".epos-dialog input[type=text]").Input("Erdgas Sondertarif");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(quelle.Uebernommen);
        Assert.Equal("Erdgas Sondertarif", quelle.Uebernommen!.VariantenName);
        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains("Erdgas Sondertarif", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ohne_BHKW_Daten_bleibt_die_Liste_stehen_und_sagt_warum()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        cut.FindAll(".epos-projekt-bhkw")[0].Click();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains("keine BHKW-Vergleichsgruppe", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Mit_BHKW_Daten_geht_der_zweite_Dialog_auf()
    {
        var daten = new BhkwDialogDaten(
            IdStamm: 1030,
            StammName: "B3-Kaskade",
            Anlagen: new List<KwkgAnlagenAngabe>(),
            Parameter: new WirtschaftlichkeitParameter(),
            HatHeizkessel: false,
            Doppelpflege: Array.Empty<KohaerenzHinweis>(),
            Katalog: null,
            ErgebnisseLaden: null,
            SpeichereAnlage: null,
            SpeichereVorgaben: null);

        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte, bhkw: daten));

        cut.FindAll(".epos-projekt-bhkw")[0].Click();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Contains("B3-Kaskade", cut.Find(".epos-dialog-titel").TextContent);
    }

    /// <summary>
    /// Auftrag #286, Einbettungsstelle 1: Der Wirt bekommt nach OK, was er braucht
    /// (die Speichermeldung — der Dialog hat geschrieben), und nach Abbrechen nichts.
    /// Gemessen werden die Schreibzugriffe, nicht die Anzeige.
    ///
    /// <para><b>ETAPPE E2 (Befund 04/B30).</b> Der OK-Weg schreibt nur noch den
    /// WERTLICH geaenderten Stand. Geaendert ist hier allein die Anlagenzeile — die
    /// Projektvorgaben bleiben, wie sie geladen wurden, und werden deshalb nicht
    /// mitgeschrieben. Aus den zwei Zugriffen wird EINER; die Aussage des Falls
    /// bleibt dieselbe: geschrieben wird im OK-Weg und sonst nirgends.</para>
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Der_BHKW_Dialog_schreibt_in_der_Wurzel_nur_im_OK_Weg(bool ok)
    {
        var anlage = new KwkgAnlagenAngabe
        { IdAnlage = 14920, IdProjekt = 1030, Bezeichner = "BHKW 50", PelKW = 50 };
        var parameter = new WirtschaftlichkeitParameter();
        int zugriffe = 0;

        var daten = new BhkwDialogDaten(
            IdStamm: 1030,
            StammName: "B3-Kaskade",
            Anlagen: new List<KwkgAnlagenAngabe> { anlage },
            Parameter: parameter,
            HatHeizkessel: false,
            Doppelpflege: Array.Empty<KohaerenzHinweis>(),
            Katalog: null,
            ErgebnisseLaden: null,
            SpeichereAnlage: _ => { zugriffe++; return true; },
            SpeichereVorgaben: _ => { zugriffe++; return true; });

        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte, bhkw: daten));
        cut.FindAll(".epos-projekt-bhkw")[0].Click();

        // Eine aendernde Bedienung: der erste Satz der gewaehlten Anlage.
        cut.FindAll("input[inputmode=decimal]")[0].Input("5,57");
        Assert.Equal(0, zugriffe);

        // Die Leiste traegt Abbrechen (0) und Speichern (1).
        var knoepfe = cut.FindAll(".epos-leiste button");
        knoepfe[ok ? 1 : 0].Click();

        Assert.Equal(ok ? 1 : 0, zugriffe);          // nur die geaenderte Anlagenzeile
        Assert.Equal(ok ? 5.57 : (double?)null, anlage.SatzEinspCt);
        Assert.Single(cut.FindAll(".epos-seite"));   // zurueck in der Liste
    }

    [Fact]
    public void Die_Wurzel_meldet_sich_als_Navigationsziel_an()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.Same(cut.Instance, Navigationsziel.Aktuell);
        Assert.True(Navigationsziel.Aktuell!.OeffneMaske(Seitenschluessel.Energietraeger));
        Assert.False(Navigationsziel.Aktuell.OeffneMaske("GIBT_ES_NICHT"));
    }

    // =====================================================================
    //  K7 (iU9-W16c.2) - die drei Ansichten des Rahmens
    // =====================================================================

    [Fact]
    public void Die_Wurzel_traegt_die_Schale_ueber_jeder_Ansicht()
    {
        // Entscheid E-1: eine Wurzel, zwei Schalen. Unter Windows ist die
        // Kopfleiste das Menueband; auf iOS gibt es keine.
        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle(ZweiProjekte));
        var cut = Render<AppWurzel>(p => p
            .Add(x => x.Kopfleiste,
                 (RenderFragment)(b => b.AddMarkupContent(0, "<div id=\"schale\">Menue</div>"))));

        Assert.NotNull(cut.Find("#schale"));
        Assert.Single(cut.FindAll(".epos-seite"));

        cut.Instance.OeffneMaske(Seitenschluessel.Energietraeger);
        cut.Render();

        // Auch ueber dem Dialog steht die Schale - ein Menue verschwindet nicht.
        Assert.NotNull(cut.Find("#schale"));
    }

    [Fact]
    public void Ohne_Startseitengaben_bleibt_die_Liste_stehen_und_sagt_warum()
    {
        // Der Zustand der iOS-Huelle vor iU11 - derselbe Umgang wie beim
        // Assistenten und beim KI-Chat.
        var quelle = new TestProjektquelle(ZweiProjekte);
        var cut = Aufbauen(quelle);

        cut.Instance.OeffneMaske(Seitenschluessel.Startseite);
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-startseite"));
        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains("Startseite", cut.Find(".epos-warnbanner").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void Mit_Startseitengaben_loest_die_Startseite_die_Liste_ab()
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Startseite = new Dictionary<string, object>
            {
                ["ProjektId"] = new Func<int>(() => 1030)
            }
        };
        var cut = Aufbauen(quelle);

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.Startseite));
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Single(cut.FindAll(".epos-startseite"));
    }

    [Fact]
    public void Die_Startseite_zeigt_ihre_Gattungszeile_nur_ohne_Kopfleiste()
    {
        // ANWENDERWUNSCH 05.09.2026 (W16b-E-4): Unter Windows nennt das Kopfband
        // des Hauptfensters die Produktgattung schon - die Startseite laesst
        // ihre eigene Zeile dann weg. Auf iOS ist die Kopfleiste leer, und die
        // Zeile bleibt die einzige Nennung des Produkts.
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Startseite = new Dictionary<string, object>
            {
                ["ProjektId"] = new Func<int>(() => 1030)
            }
        };
        Services.AddSingleton<IProjektQuelle>(quelle);

        // (a) OHNE Kopfleiste - der iOS-Weg.
        var ohne = Render<AppWurzel>(p => p.Add(x => x.Startansicht, Seitenschluessel.Startseite));
        Assert.Single(ohne.FindAll(".epos-startseite-gattung"));

        // (b) MIT Kopfleiste - der Windows-Weg.
        var mit = Render<AppWurzel>(p => p
            .Add(x => x.Startansicht, Seitenschluessel.Startseite)
            .Add(x => x.Kopfleiste,
                 (RenderFragment)(b => b.AddMarkupContent(0, "<div id=\"schale\">Menue</div>"))));

        Assert.NotNull(mit.Find("#schale"));
        Assert.Single(mit.FindAll(".epos-startseite"));
        Assert.Empty(mit.FindAll(".epos-startseite-gattung"));
    }

    [Fact]
    public void Die_Startansicht_ist_zugleich_das_Ziel_des_Rueckwegs()
    {
        // Unter Windows fuehrt "Schliessen" eines Dialogs zurueck auf die
        // STARTSEITE, auf iOS auf die Projektliste. Es ist derselbe Weg.
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Startseite = new Dictionary<string, object>
            {
                ["ProjektId"] = new Func<int>(() => 1030)
            }
        };
        Services.AddSingleton<IProjektQuelle>(quelle);
        var cut = Render<AppWurzel>(p => p.Add(x => x.Startansicht, Seitenschluessel.Startseite));

        Assert.Single(cut.FindAll(".epos-startseite"));

        cut.Instance.OeffneMaske(Seitenschluessel.Energietraeger);
        cut.Render();
        Assert.Empty(cut.FindAll(".epos-startseite"));

        // Der Dialog meldet sein Ende - danach steht wieder die Startseite,
        // nicht die Liste. (Abbrechen ist der letzte nicht-primaere Knopf der
        // Speichernleiste - derselbe Griff wie in den Faellen oben.)
        cut.FindAll(".epos-dialog button").Last(k => !k.ClassList.Contains("epos-knopf--primaer")).Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
    }

    [Fact]
    public void Berichte_und_Kosten_ist_eine_Ansicht_mit_Rueckweg()
    {
        // ANWENDERENTSCHEID W16c-E-3 (04.09.2026): BERICHTE_KOSTEN ist der Weg
        // BEIDER Plattformen. Die Ansicht traegt deshalb einen Rueckwegknopf -
        // sie loest die Startansicht ab, und ohne ihn gaebe es keinen Weg
        // zurueck; als sechstes Reiterblatt der Startseite hat dieselbe
        // Komponente ihn NICHT (dort fehlt der Rueckruf).
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Startseite = new Dictionary<string, object>
            {
                ["ProjektId"] = new Func<int>(() => 1030)
            },
            BerichteKosten = new Dictionary<string, object>
            {
                ["ZurueckText"] = "◀ Zurück"
            }
        };
        Services.AddSingleton<IProjektQuelle>(quelle);
        var cut = Render<AppWurzel>(p => p.Add(x => x.Startansicht, Seitenschluessel.Startseite));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.BerichteKosten));
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-startseite"));
        Assert.Single(cut.FindAll(".epos-navigation"));

        cut.Find(".epos-navigation-zurueck").Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
        Assert.Empty(cut.FindAll(".epos-navigation"));
    }

    [Fact]
    public void Ohne_Parametersatz_bleibt_Berichte_und_Kosten_stehen_und_sagt_warum()
    {
        // Die Standardumsetzung von IProjektQuelle.BerichteKostenGaben liefert
        // null. Dann wechselt die Wurzel NICHT, sondern nennt den Grund - der
        // Zustand einer Huelle, die diese Seite nicht fuehrt.
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.BerichteKosten));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Empty(cut.FindAll(".epos-navigation"));
    }

    // =====================================================================
    //  Der PLATZ als Argument (Anwenderentscheid KI-D-Q8, 21.09.2026)
    // =====================================================================

    private static TestProjektquelle MitStartseite()
        => new TestProjektquelle(ZweiProjekte)
        {
            Startseite = new Dictionary<string, object>
            {
                ["ProjektId"] = new Func<int>(() => 1030)
            }
        };

    /// <summary>
    /// <b>Der Reiter kommt als Argument mit.</b> Die sechs Erzeugermasken des
    /// Projekts haben die Startseite als Ziel; ohne den Reiter „Energieerzeuger"
    /// landete der Anwender auf „Projekt" und müsste selbst weitersuchen. Der Kern
    /// nennt ihn als Zeichenkette (<c>KiMaskenziele.REITER_ERZEUGER</c>), die Wurzel
    /// verbraucht ihn als Reiterwunsch.
    /// </summary>
    [Fact]
    public void Das_Argument_der_Startseite_holt_den_Erzeugerreiter_nach_vorn()
    {
        var cut = Aufbauen(MitStartseite());

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.Startseite,
                                             EPOS.UI.Seiten.Start.Reiterschluessel.Erzeuger));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
        Assert.Equal("true", cut.FindAll("[role='tab']")[3].GetAttribute("aria-selected"));
    }

    /// <summary>
    /// <b>Der Wunsch bleibt nicht kleben.</b> Er gilt für DIESEN Aufbau; der nächste
    /// Aufruf ohne Argument macht wieder auf „Projekt" auf — derselbe Verbrauch wie
    /// beim Rückweg „Projekt angelegt".
    /// </summary>
    [Fact]
    public void Ohne_Argument_steht_die_Startseite_wieder_auf_dem_ersten_Reiter()
    {
        var cut = Aufbauen(MitStartseite());

        cut.Instance.OeffneMaske(Seitenschluessel.Startseite,
                                 EPOS.UI.Seiten.Start.Reiterschluessel.Erzeuger);
        cut.Render();
        Assert.Equal("true", cut.FindAll("[role='tab']")[3].GetAttribute("aria-selected"));

        cut.Instance.OeffneMaske(Seitenschluessel.Projektliste);
        cut.Render();
        cut.Instance.OeffneMaske(Seitenschluessel.Startseite);
        cut.Render();

        Assert.Equal("true", cut.FindAll("[role='tab']")[0].GetAttribute("aria-selected"));
    }

    /// <summary>
    /// <b>Dasselbe für das BLATT der Ansicht „Berichte und Kosten".</b> Sie führt
    /// vier Blätter, und vier Masken des Dialogkatalogs zeigen darauf; ohne das
    /// Argument stünde immer die Übersicht vorn.
    /// </summary>
    [Fact]
    public void Das_Argument_der_Berichtsansicht_waehlt_das_Blatt()
    {
        var quelle = MitStartseite();
        quelle.BerichteKosten = new Dictionary<string, object>
        {
            ["ZurueckText"] = "◀ Zurück"
        };
        Services.AddSingleton<IProjektQuelle>(quelle);
        var cut = Render<AppWurzel>(p => p.Add(x => x.Startansicht, Seitenschluessel.Startseite));

        Assert.True(cut.Instance.OeffneMaske(
            Seitenschluessel.BerichteKosten,
            EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_KOSTEN));
        cut.Render();

        Assert.Equal(EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_KOSTEN,
                     cut.FindComponent<EPOS.UI.Seiten.Berichte.BerichteKostenSeite>()
                        .Instance.AktiveSeite);
    }

    // =====================================================================
    //  Die FÜNF Masken, die die Wurzel seit KI-D-Q8 selbst zeigt
    // =====================================================================

    /// <summary>
    /// <b>Mit Parametersatz löst der Dialog die Liste ab.</b> Geprüft wird jede der
    /// fünf Masken mit einem LEEREN Gabensatz — genau das ist der Hausfall „zeichnet
    /// AUCH OHNE GABEN": Jeder Delegat <c>null</c>, jede Liste leer, jeder Text der
    /// Rückfall.
    /// </summary>
    [Theory]
    [InlineData(Seitenschluessel.Klimadaten)]
    [InlineData(Seitenschluessel.ProjektAlsVariante)]
    [InlineData(Seitenschluessel.ProjektSpeichernUnter)]
    [InlineData(Seitenschluessel.PeakShaving)]
    [InlineData(Seitenschluessel.StromganglinieAdmin)]
    public void Mit_Parametersatz_zeichnet_die_Wurzel_den_Dialog(string schluessel)
    {
        var quelle = MitAllenFuenf();
        var cut = Aufbauen(quelle);

        Assert.True(cut.Instance.OeffneMaske(schluessel));
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Single(cut.FindAll(".epos-dialog"));
    }

    /// <summary>
    /// <b>Ohne Parametersatz bleibt die Liste stehen und sagt warum</b> — benannt
    /// abgelehnt, nicht still übergangen. Das ist der Zustand einer Hülle, die die
    /// Maske nicht führt; unter Windows heißt derselbe Zustand „das Fenster geht
    /// nicht auf".
    /// </summary>
    /// <remarks>
    /// <c>OeffneMaske</c> antwortet dabei <c>true</c>: Die Frage, die es beantwortet,
    /// ist „führt die Wurzel diesen Schlüssel?", und die Ansicht wechselt eine
    /// Nachricht später auf dem Renderfaden. Die ABLEHNUNG steht deshalb im Banner —
    /// dieselbe Aufteilung wie bei „Berichte und Kosten" und beim Projektassistenten.
    /// </remarks>
    [Theory]
    [InlineData(Seitenschluessel.Klimadaten)]
    [InlineData(Seitenschluessel.ProjektAlsVariante)]
    [InlineData(Seitenschluessel.ProjektSpeichernUnter)]
    [InlineData(Seitenschluessel.PeakShaving)]
    [InlineData(Seitenschluessel.StromganglinieAdmin)]
    public void Ohne_Parametersatz_bleibt_die_Liste_stehen_und_sagt_warum(string schluessel)
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.True(cut.Instance.OeffneMaske(schluessel));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Empty(cut.FindAll(".epos-dialog"));
        Assert.NotEmpty(cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// <b>Jede der fünf nennt ihren EIGENEN Grund.</b> „Geht hier nicht" ist keine
    /// Auskunft, wenn fünf Masken denselben Satz bekämen.
    /// </summary>
    [Fact]
    public void Die_fuenf_Ablehnungen_sind_voneinander_verschieden()
    {
        AppWurzel wurzel = Aufbauen(new TestProjektquelle(ZweiProjekte)).Instance;

        string[] gruende =
        {
            wurzel.KeineKlimadatenText,
            wurzel.KeineProjektvarianteText,
            wurzel.KeineProjektkopieText,
            wurzel.KeinPeakShavingText,
            wurzel.KeineStromganglinienText
        };

        Assert.DoesNotContain(gruende, g => string.IsNullOrWhiteSpace(g));
        Assert.Equal(gruende.Length, gruende.Distinct(StringComparer.Ordinal).Count());
    }

    /// <summary>
    /// <b>Der Schreibweg liegt in der Hülle, nicht in der Wurzel</b> (Muster
    /// <c>BeiEnergietraegerFertig</c>): Das OK des Variantendialogs geht an
    /// <c>IProjektQuelle.ProjektVarianteUebernehmen</c>, und die Wurzel meldet den
    /// Namen.
    /// </summary>
    [Fact]
    public void Das_OK_des_Variantendialogs_geht_an_die_Huelle()
    {
        var quelle = MitAllenFuenf();
        quelle.VariantenAntwort = "B3-Kaskade V2";
        var cut = Aufbauen(quelle);

        cut.Instance.OeffneMaske(Seitenschluessel.ProjektAlsVariante);
        cut.Render();

        cut.Find(".epos-dialog input[type=text]").Input("B3-Kaskade V2");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.NotNull(quelle.VarianteUebernommen);
        Assert.Equal("B3-Kaskade V2", quelle.VarianteUebernommen!.Value.Bezeichner);
        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains("B3-Kaskade V2", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// <b>Abbrechen schreibt nicht und führt zurück.</b> Der Rückweg räumt dabei
    /// auf: Die Wurzel trägt den Parametersatz nicht weiter, sie holt ihn beim
    /// nächsten Betreten neu.
    /// </summary>
    [Fact]
    public void Abbrechen_im_Variantendialog_fuehrt_ohne_Schreibweg_zurueck()
    {
        var quelle = MitAllenFuenf();
        var cut = Aufbauen(quelle);

        cut.Instance.OeffneMaske(Seitenschluessel.ProjektAlsVariante);
        cut.Render();
        Assert.Single(cut.FindAll(".epos-dialog"));

        // Abbrechen ist der letzte nicht-primaere Knopf der Speichernleiste -
        // derselbe Griff wie in den Faellen der Energietraeger-Variante.
        cut.FindAll(".epos-dialog button").Last(k => !k.ClassList.Contains("epos-knopf--primaer")).Click();
        cut.Render();

        Assert.Null(quelle.VarianteUebernommen);
        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Empty(cut.FindAll(".epos-dialog"));
    }

    /// <summary>Eine Quelle, die alle fünf Masken führt — mit leeren Gabensätzen.</summary>
    private static TestProjektquelle MitAllenFuenf()
    {
        var leer = new Dictionary<string, object>();
        return new TestProjektquelle(ZweiProjekte)
        {
            Klimadaten = new Dictionary<string, object>(leer),
            Projektvariante = new Dictionary<string, object>(leer),
            Projektkopie = new Dictionary<string, object>(leer),
            PeakShaving = new Dictionary<string, object>(leer),
            Stromganglinien = new Dictionary<string, object>(leer)
        };
    }

    // =====================================================================
    //  Die DREI Masken der Wirtschaftlichkeit (E3/8, Anwenderentscheid A19)
    // =====================================================================

    /// <summary>
    /// <b>Die Wurzel FÜHRT die drei Schlüssel</b> — bis E3/8 fielen sie durch, und
    /// der Kern lehnte sie unter iOS benannt ab („kein Weg"), obwohl ihre Hüllen
    /// seither plattformfrei sind. Plattformfrei heißt nicht erreichbar; dieser
    /// Fall ist der Unterschied.
    /// </summary>
    [Theory]
    [InlineData(Seitenschluessel.Kostenverwaltung)]
    [InlineData(Seitenschluessel.NutzungsdauerVerwaltung)]
    [InlineData(Seitenschluessel.Gesetzeskatalog)]
    public void Mit_Parametersatz_zeichnet_die_Wurzel_die_drei_Wirtschaftsmasken(string schluessel)
    {
        var cut = Aufbauen(MitAllenDreien());

        Assert.True(cut.Instance.OeffneMaske(schluessel));
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Single(cut.FindAll(".epos-dialog"));
    }

    /// <summary>Ohne Parametersatz bleibt die Liste stehen und sagt warum (E3/8).</summary>
    [Theory]
    [InlineData(Seitenschluessel.Kostenverwaltung)]
    [InlineData(Seitenschluessel.NutzungsdauerVerwaltung)]
    [InlineData(Seitenschluessel.Gesetzeskatalog)]
    public void Ohne_Parametersatz_bleiben_die_drei_Wirtschaftsmasken_zu(string schluessel)
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.True(cut.Instance.OeffneMaske(schluessel));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Empty(cut.FindAll(".epos-dialog"));
        Assert.NotEmpty(cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// <b>Die zwei Kataloge der Gebäudesimulation</b> (G3) sind freie Ansichten auf BEIDEN
    /// Plattformen: Mit dem Parametersatz der Quelle (iOS) zeichnet die Wurzel die Verwaltung,
    /// der Rückweg führt zur Startansicht.
    /// </summary>
    [Theory]
    [InlineData(Seitenschluessel.BaustoffKatalog, "epos-baustoff-admin")]
    [InlineData(Seitenschluessel.BauteilaufbauKatalog, "epos-bauteilaufbau-admin")]
    public void Mit_Parametersatz_zeichnet_die_Wurzel_die_Kataloge_der_Gebaeudehuelle(string schluessel, string klasse)
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            BaustoffKatalog = new Dictionary<string, object>(),
            BauteilaufbauKatalog = new Dictionary<string, object>()
        };
        var cut = Aufbauen(quelle);

        Assert.True(cut.Instance.OeffneMaske(schluessel));
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-seite"));
        Assert.Single(cut.FindAll("." + klasse));

        cut.Find(".epos-dialog-kopf .epos-dialog-zu").Click();       // Kreuz = Beenden
        Assert.Single(cut.FindAll(".epos-seite"));
    }

    /// <summary>
    /// Unter Windows reicht die Hülle den Parametersatz als DELEGAT herein (über das
    /// Hauptfenster); er hat Vorrang vor der Quelle.
    /// </summary>
    [Fact]
    public void Der_Delegat_der_Huelle_hat_Vorrang_vor_der_Quelle()
    {
        int gerufen = 0;
        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle(ZweiProjekte));
        var cut = Render<AppWurzel>(ps => ps
            .Add(p => p.BaustoffKatalogGaben, () => { gerufen++; return new Dictionary<string, object>(); }));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.BaustoffKatalog));
        cut.Render();

        Assert.Equal(1, gerufen);
        Assert.Single(cut.FindAll(".epos-baustoff-admin"));
    }

    /// <summary>Ohne Parametersatz bleiben die zwei Kataloge zu und sagen warum.</summary>
    [Theory]
    [InlineData(Seitenschluessel.BaustoffKatalog)]
    [InlineData(Seitenschluessel.BauteilaufbauKatalog)]
    public void Ohne_Parametersatz_bleiben_die_Kataloge_der_Gebaeudehuelle_zu(string schluessel)
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.True(cut.Instance.OeffneMaske(schluessel));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Empty(cut.FindAll(".epos-dialog"));
        Assert.NotEmpty(cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>
    /// <b>Alle ELF Ablehnungen sind voneinander verschieden</b> — die fünf aus
    /// KI‑D‑Q8, die drei aus E3/8, die zwei der Gebäudehülle (G3) und der Brauchwasserkatalog (ZU26).
    /// </summary>
    [Fact]
    public void Die_elf_Ablehnungen_sind_voneinander_verschieden()
    {
        AppWurzel wurzel = Aufbauen(new TestProjektquelle(ZweiProjekte)).Instance;

        string[] gruende =
        {
            wurzel.KeineKlimadatenText,
            wurzel.KeineProjektvarianteText,
            wurzel.KeineProjektkopieText,
            wurzel.KeinPeakShavingText,
            wurzel.KeineStromganglinienText,
            wurzel.KeineKostenverwaltungText,
            wurzel.KeineNutzungsdauernText,
            wurzel.KeinGesetzeskatalogText,
            wurzel.KeinBaustoffKatalogText,
            wurzel.KeinBauteilaufbauKatalogText,
            wurzel.KeinNutzungsartKatalogText
        };

        Assert.DoesNotContain(gruende, g => string.IsNullOrWhiteSpace(g));
        Assert.Equal(gruende.Length, gruende.Distinct(StringComparer.Ordinal).Count());
    }

    // =====================================================================
    //  Der Katalog der BRAUCHWASSER-NUTZUNGSARTEN auf iOS (ZU26)
    // =====================================================================

    /// <summary>
    /// Ohne Delegat der Hülle (iOS) öffnet die Wurzel den Katalog über die QUELLE — der einzige
    /// Katalog, der dort aufgeht. „Beenden" führt zur Startansicht zurück (wie die zwei Kataloge der Gebäudesimulation, ohne Rückwegstapel). Geprüft über
    /// Klassen und Zustand, nicht über Klartext (kulturunabhängig).
    /// </summary>
    [Fact]
    public void Der_Brauchwasserkatalog_oeffnet_ueber_die_Quelle_und_schliesst_zurueck()
    {
        var quelle = new TestProjektquelle(ZweiProjekte) { NutzungsartKatalog = new Dictionary<string, object>() };
        var cut = Aufbauen(quelle);
        Assert.Single(cut.FindAll(".epos-seite"));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.BrauchwasserNutzungsarten));
        cut.Render();

        Assert.Equal(1, quelle.NutzungsartKatalogGefragt);
        Assert.Single(cut.FindAll(".epos-tww-katalog"));
        Assert.Empty(cut.FindAll(".epos-seite"));

        cut.Find(".epos-tww-katalog > .epos-leiste button.epos-knopf--primaer").Click();

        Assert.Empty(cut.FindAll(".epos-tww-katalog"));
        Assert.Single(cut.FindAll(".epos-seite"));
    }

    /// <summary>Ohne Parametersatz bleibt der Katalog zu und sagt warum — benannt, nicht still.</summary>
    [Fact]
    public void Ohne_Parametersatz_bleibt_der_Brauchwasserkatalog_zu()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.BrauchwasserNutzungsarten));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Empty(cut.FindAll(".epos-tww-katalog"));
        Assert.Contains(cut.Instance.KeinNutzungsartKatalogText, cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>Eine Quelle, die die drei Wirtschaftsmasken führt.</summary>
    private static TestProjektquelle MitAllenDreien()
        => new TestProjektquelle(ZweiProjekte)
        {
            Kostenverwaltung = new Dictionary<string, object>(),
            Nutzungsdauern = new Dictionary<string, object>(),
            Gesetzeskatalog = new Dictionary<string, object>()
        };

    /// <summary>
    /// Ohne Argument bleibt es bei der Übersicht — der Zustand vor KI‑D‑Q8.
    /// </summary>
    [Fact]
    public void Ohne_Argument_macht_die_Berichtsansicht_auf_der_Uebersicht_auf()
    {
        var quelle = MitStartseite();
        quelle.BerichteKosten = new Dictionary<string, object>
        {
            ["ZurueckText"] = "◀ Zurück"
        };
        Services.AddSingleton<IProjektQuelle>(quelle);
        var cut = Render<AppWurzel>(p => p.Add(x => x.Startansicht, Seitenschluessel.Startseite));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.BerichteKosten));
        cut.Render();

        Assert.Equal(EPOS.UI.Seiten.Berichte.BerichteKostenSeite.SEITE_UEBERSICHT,
                     cut.FindComponent<EPOS.UI.Seiten.Berichte.BerichteKostenSeite>()
                        .Instance.AktiveSeite);
    }

    // =====================================================================
    //  Der PROJEKTASSISTENT als freie Ansicht (W16a-E-1 / W16b-O-5, #62b)
    // =====================================================================

    /// <summary>
    /// Der Parametersatz eines Assistentenlaufs, wie ihn unter Windows
    /// <c>AssistentHuelle.AnsichtGaben</c> baut — hier ohne Datenbank: nur die
    /// Betriebsart, ein Gabendelegat fuer den Komponentenschritt und die Frage
    /// nach ungespeicherten Eingaben.
    /// </summary>
    private static IReadOnlyDictionary<string, object> Assistentengaben(
        int betriebsart, List<int> betriebsarten, Func<bool>? geaendert = null)
    {
        betriebsarten.Add(betriebsart);
        return new Dictionary<string, object>
        {
            ["Betriebsart"] = betriebsart,
            ["SeiteAktiv"] = new Func<int, bool>(nr => nr <= 1),
            ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>?>(
                nr => new Dictionary<string, object>()),
            ["HatAenderungen"] = geaendert ?? (() => false)
        };
    }

    private IRenderedComponent<AppWurzel> MitAssistent(
        List<int> betriebsarten, Func<bool>? geaendert = null)
    {
        Services.AddSingleton<IProjektQuelle>(new TestProjektquelle(ZweiProjekte));
        return Render<AppWurzel>(p => p
            .Add(x => x.AssistentGaben,
                 new Func<int, IReadOnlyDictionary<string, object>?>(
                     b => Assistentengaben(b, betriebsarten, geaendert))));
    }

    /// <summary>
    /// <b>Die SCHALTLOGIK des Entscheids:</b> Die zwei Menuewege PROJEKT_NEU und
    /// PROJEKT_BEARBEITEN setzen die ANSICHT — sie erzeugen kein Fenster (das ist
    /// auf Linux nicht messbar, wohl aber, dass die Wurzel den Schluessel kennt und
    /// die Ansicht wechselt). Die Betriebsart kommt aus dem Schluessel.
    /// </summary>
    [Theory]
    [InlineData("PROJEKT_NEU", 0)]
    [InlineData("PROJEKT_BEARBEITEN", 1)]
    public void Die_zwei_Menuewege_schalten_die_Assistentenansicht(string schluessel, int erwartet)
    {
        var betriebsarten = new List<int>();
        var cut = MitAssistent(betriebsarten);

        Assert.True(cut.Instance.OeffneMaske(schluessel));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-assistentseite"));
        Assert.Equal(new[] { erwartet }, betriebsarten);

        // Das linke Band steht nur beim BEARBEITEN - der sichtbare Unterschied der
        // zwei Wege, und der Beleg, dass die Betriebsart wirklich ankommt.
        Assert.Equal(erwartet == 1, cut.FindAll(".epos-assistent-band").Count == 1);
    }

    /// <summary>
    /// Der Kern ruft den Assistenten ueber <c>Masken.Assistent</c> und reicht die
    /// Betriebsart als ARGUMENT herein — so tut es <c>MenueCtrl.AssistentZeigen</c>
    /// seit jeher. Die Wurzel nimmt sie an.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Die_Betriebsart_kommt_auch_als_Argument_an(int betriebsart)
    {
        var betriebsarten = new List<int>();
        var cut = MitAssistent(betriebsarten);

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.Assistent, betriebsart));
        cut.Render();

        Assert.Single(cut.FindAll(".epos-assistentseite"));
        Assert.Equal(new[] { betriebsart }, betriebsarten);
    }

    /// <summary>
    /// Ohne Delegat bleibt es beim iOS-Zustand: Die Liste steht und sagt warum
    /// (<c>IProjektQuelle.AssistentGaben</c> liefert dort <c>null</c>).
    /// </summary>
    [Fact]
    public void Ohne_Assistentengaben_bleibt_die_Liste_stehen_und_sagt_warum()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.ProjektNeu));
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-assistentseite"));
        Assert.Single(cut.FindAll(".epos-seite"));
        Assert.Contains("Projektassistent", cut.Find(".epos-warnbanner").TextContent);
    }

    // =====================================================================
    //  Der PROJEKTASSISTENT auf iOS (Befund W16a-O-4)
    // =====================================================================

    /// <summary>
    /// <b>Der iOS-Weg.</b> Ohne Delegat der Huelle fragt die Wurzel
    /// <c>IProjektQuelle.AssistentGaben</c> — und seit W16a-O-4 antwortet
    /// <c>IosProjektQuelle</c> darauf. Geprueft wird, dass die Ansicht damit
    /// aufgeht und dass Betriebsart UND Projekt an der Quelle ankommen.
    /// </summary>
    [Fact]
    public void Der_Assistent_geht_auch_ueber_die_Projektquelle_auf()
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Assistent = (b, id) => new Dictionary<string, object>
            {
                ["Betriebsart"] = b,
                ["SeiteAktiv"] = new Func<int, bool>(nr => nr <= 1),
                ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>?>(
                    nr => new Dictionary<string, object>())
            }
        };

        var cut = Aufbauen(quelle);

        // Wie der Knopf „Bearbeiten…" der Projektliste: erst das Projekt melden,
        // dann den Schluessel.
        cut.Find(".epos-projekt-assistent").Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-assistentseite"));
        Assert.Equal((1, ZweiProjekte[0].Id), quelle.AssistentRuf);
    }

    /// <summary>
    /// Der Kopfknopf „Neues Projekt…" der Projektliste fuehrt in denselben
    /// Assistenten, aber in Betriebsart NEU — der iOS-Ersatz fuer die Startkachel
    /// „Projekt neu" (Befund W16a-O-4).
    /// </summary>
    [Fact]
    public void Der_Kopfknopf_neues_Projekt_oeffnet_den_Assistenten_im_Neu_Zweig()
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Assistent = (b, id) => new Dictionary<string, object>
            {
                ["Betriebsart"] = b,
                ["SeiteAktiv"] = new Func<int, bool>(nr => nr <= 1),
                ["SeiteGaben"] = new Func<int, IReadOnlyDictionary<string, object>?>(
                    nr => new Dictionary<string, object>())
            }
        };

        var cut = Aufbauen(quelle);

        cut.Find(".epos-projekt-neu").Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-assistentseite"));
        Assert.Equal(0, quelle.AssistentRuf!.Value.Betriebsart);

        // Der Neu-Zweig fuehrt kein linkes Band - der sichtbare Unterschied.
        Assert.Empty(cut.FindAll(".epos-assistent-band"));
    }

    /// <summary>
    /// <b>62b-E-1, Festlegung 1:</b> OHNE Aenderungen wechselt die Ansicht
    /// unmittelbar — keine Rueckfrage.
    /// </summary>
    [Fact]
    public void Ohne_Aenderungen_wechselt_die_Ansicht_unmittelbar()
    {
        var cut = MitAssistent(new List<int>());

        cut.Instance.OeffneMaske(Seitenschluessel.ProjektNeu);
        cut.Render();
        Assert.Single(cut.FindAll(".epos-assistentseite"));

        cut.Instance.OeffneMaske(Seitenschluessel.Projektliste);
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-assistentseite"));
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
    }

    /// <summary>
    /// <b>62b-E-1:</b> MIT Aenderungen kommt die Rueckfrage, und die Ansicht bleibt
    /// stehen, bis sie beantwortet ist. „Bleiben" laesst den Assistenten stehen,
    /// „Verwerfen" laesst den Wechsel zu.
    /// </summary>
    [Fact]
    public void Mit_Aenderungen_haelt_die_Rueckfrage_den_Ansichtswechsel_auf()
    {
        var cut = MitAssistent(new List<int>(), geaendert: () => true);

        cut.Instance.OeffneMaske(Seitenschluessel.ProjektNeu);
        cut.Render();

        cut.Instance.OeffneMaske(Seitenschluessel.Projektliste);
        cut.Render();

        // Die Frage steht, der Assistent auch.
        Assert.Single(cut.FindAll(".epos-rueckfrage"));
        Assert.Single(cut.FindAll(".epos-assistentseite"));

        // Bleiben.
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[2].Click();
        Assert.Empty(cut.FindAll(".epos-rueckfrage"));
        Assert.Single(cut.FindAll(".epos-assistentseite"));

        // Zweiter Versuch, diesmal verwerfen.
        cut.Instance.OeffneMaske(Seitenschluessel.Projektliste);
        cut.Render();
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();

        Assert.Empty(cut.FindAll(".epos-assistentseite"));
        Assert.Single(cut.FindAll(".epos-seite"));
    }

    /// <summary>
    /// <b>62b-E-1, Festlegung 4:</b> <c>DarfVerlassen</c> ist der Weg des
    /// <c>Hauptfensterrahmens</c> beim Schliessen des Programms. Ohne stehenden
    /// Assistenten sagt er unmittelbar ja.
    /// </summary>
    [Fact]
    public async Task DarfVerlassen_sagt_ohne_Assistenten_unmittelbar_ja()
    {
        var cut = Aufbauen(new TestProjektquelle(ZweiProjekte));

        Assert.False(cut.Instance.VerlassenFraglich);
        Assert.True(await cut.Instance.DarfVerlassen());
    }

    /// <summary>
    /// Steht der Assistent MIT Aenderungen, fragt <c>DarfVerlassen</c> — und
    /// antwortet erst, wenn der Anwender geantwortet hat. „Bleiben" heisst nein.
    /// </summary>
    [Fact]
    public async Task DarfVerlassen_fragt_den_stehenden_Assistenten()
    {
        var cut = MitAssistent(new List<int>(), geaendert: () => true);
        cut.Instance.OeffneMaske(Seitenschluessel.ProjektNeu);
        cut.Render();

        // Die SYNCHRONE Vorfrage - an ihr entscheidet der Hauptfensterrahmen, ob er
        // sein FormClosing ueberhaupt abbricht.
        Assert.True(cut.Instance.VerlassenFraglich);

        Task<bool> antwort = cut.Instance.DarfVerlassen();
        cut.WaitForElement(".epos-rueckfrage");
        Assert.False(antwort.IsCompleted);

        cut.FindAll(".epos-rueckfrage .epos-leiste button")[2].Click();   // Bleiben
        Assert.False(await antwort);

        // Und mit „Verwerfen" darf geschlossen werden.
        Task<bool> zweite = cut.Instance.DarfVerlassen();
        cut.WaitForElement(".epos-rueckfrage");
        cut.FindAll(".epos-rueckfrage .epos-leiste button")[1].Click();
        Assert.True(await zweite);

        // Danach ist nichts mehr zu fragen: Ein zweites Schliessen - und der
        // Neustart des Sprachwechsels - laeuft ohne Abbruch durch.
        Assert.False(cut.Instance.VerlassenFraglich);
    }

    // =====================================================================
    //  DER RUECKWEGSTAPEL (Auftrag #207, Anwenderentscheid SIM-Q4)
    // =====================================================================

    /// <summary>
    /// Der Parametersatz der Ansicht SIMULATION in seiner schmalsten Form: zwei
    /// eingebettete Seiten, ein gerechneter Lauf und ein Blatt „Stromspeicher".
    /// </summary>
    /// <param name="auslegungOeffnen">
    /// Der Weg in die Stromspeicher-Auslegung (Auftrag <b>#274</b>): Er steht seither
    /// in Schritt ① neben der Pufferverwaltung. <c>null</c> = die Schale bietet ihn
    /// nicht an, dann zeichnet die Konfiguration den Knopf gar nicht erst.
    /// </param>
    private static IReadOnlyDictionary<string, object> Simulationsgaben(
        Action? auslegungOeffnen = null)
        => new Dictionary<string, object>
        {
            ["Dienste"] = new EPOS.UI.Seiten.Simulation.SimulationAnsichtDienste
            {
                Konfiguration = new Dictionary<string, object>
                {
                    ["Dienste"] = new EPOS.UI.Seiten.Simulation.SimulationKonfigDienste
                    {
                        Laden = _ => new EPOS.UI.Seiten.Simulation.SimulationKonfigDaten
                        {
                            IdProjekt = 1030,
                            Stromspeicherstand = new EPOS.UI.Seiten.Simulation.StromspeicherStand
                            {
                                Vorhanden = auslegungOeffnen is not null,
                                Standzeile = "Mehrspeicherbetrieb aktiviert"
                            }
                        },
                        AuslegungOeffnen = auslegungOeffnen
                    },
                    ["StartProjekt"] = 1030
                },
                Ergebnis = new Dictionary<string, object>
                {
                    ["Dienste"] = new EPOS.UI.Seiten.Simulation.SimulationErgebnisDienste
                    {
                        Laden = _ => new EPOS.UI.Seiten.Simulation.SimulationErgebnisDaten
                        {
                            IdProjekt = 1030,
                            ErgebnisGueltig = true,
                            ReiterStromspeicher = true
                        },
                        Modell = _ => null
                    },
                    ["StartProjekt"] = 1030
                },
                ErgebnisVorhanden = () => true
            },
            ["ProjektText"] = "Projekt „B3-Kaskade“"
        };

    private IRenderedComponent<AppWurzel> MitSimulation(TestProjektquelle quelle,
                                                       Action? auslegungOeffnen = null)
    {
        quelle.Startseite = new Dictionary<string, object>
        {
            ["ProjektId"] = new Func<int>(() => 1030)
        };
        quelle.Simulation = Simulationsgaben(auslegungOeffnen);

        Services.AddSingleton<IProjektQuelle>(quelle);
        return Render<AppWurzel>(p => p.Add(x => x.Startansicht, Seitenschluessel.Startseite));
    }

    /// <summary>
    /// <b>AUFTRAG #274</b> (Anwenderwunsch 14.09.2026): Der Einstieg in die
    /// Stromspeicher-Auslegung steht in SCHRITT ① neben „Pufferspeicher anlegen /
    /// verwalten…" — und der Rückweg führt über denselben Stapel dorthin zurück,
    /// nicht mehr in ③ auf das Blatt „Stromspeicher".
    /// </summary>
    [Fact]
    public void Der_Rueckweg_aus_der_Auslegung_landet_in_Schritt_1()
    {
        IRenderedComponent<AppWurzel>? wurzel = null;
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Auslegung = new Dictionary<string, object>
            {
                ["Dienste"] = new EPOS.UI.Seiten.Strom.StromspeicherAuslegungDienste(),
                ["PlanerVerfuegbar"] = true
            }
        };
        var cut = MitSimulation(quelle,
            () => wurzel!.Instance.OeffneMaske(Seitenschluessel.StromspeicherAuslegung));
        wurzel = cut;

        // ① Die Simulation, auf Schritt ① geoeffnet (der Knopf der Startseite).
        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.SimulationKonfiguration));
        cut.Render();
        Assert.Single(cut.FindAll("div.epos-simkonfig"));

        // ② „Stromspeicher auslegen…" wechselt die Ansicht.
        cut.Find("button.epos-simkonfig-auslegung").Click();
        cut.Render();
        Assert.Empty(cut.FindAll(".epos-simansicht"));
        Assert.Single(cut.FindAll(".epos-spauslegung"));

        // ③ „← zurueck" landet wieder in ① - nicht auf der Startseite und nicht in ③.
        cut.FindAll("button").First(k => k.TextContent.Trim() == "← zurück").Click();
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-spauslegung"));
        Assert.Empty(cut.FindAll(".epos-startseite"));
        Assert.Single(cut.FindAll(".epos-simansicht"));
        Assert.Single(cut.FindAll("div.epos-simkonfig"));
        Assert.Equal(EPOS.UI.Seiten.Simulation.SimulationMarke.Schreiben(1, ""),
                     MarkeDerSimulation(cut));
    }

    /// <summary>
    /// <b>Die Lücke, die #207 schließt</b> (Konzept „Simulationsablauf" 1.3): Der
    /// Rückweg der Stromspeicher-Auslegung war richtig gebaut, aber es gab kein
    /// Ziel, zu dem er zurückkonnte — das Ergebnis war eine Überlagerung IN der
    /// Startseite und keine Ansicht. Jetzt ist es eine, der Stapel merkt sie sich
    /// samt MARKE, und die Rückkehr landet in ③ auf demselben Reiterblatt.
    /// </summary>
    [Fact]
    public void Der_Rueckweg_aus_der_Auslegung_landet_in_der_Simulation_auf_ihrer_Marke()
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Auslegung = new Dictionary<string, object>
            {
                ["Dienste"] = new EPOS.UI.Seiten.Strom.StromspeicherAuslegungDienste(),
                ["PlanerVerfuegbar"] = true
            }
        };
        var cut = MitSimulation(quelle);

        // ① Die Simulation, auf Schritt ③ geoeffnet (die Kachel „Simulation").
        Assert.True(cut.Instance.OeffneMaske(
            Seitenschluessel.Simulation,
            EPOS.UI.Seiten.Simulation.SimulationMarke.SCHRITT_ERGEBNIS));
        cut.Render();
        Assert.Single(cut.FindAll(".epos-simansicht"));

        // ② Auf das Blatt „Stromspeicher" - das ist die Marke, die zurueckkommen soll.
        cut.FindAll("div.epos-simerg button[role='tab']")
           .Single(k => k.TextContent.Contains("Stromspeicher")).Click();

        // ③ Die Auslegung loest die Ansicht ab.
        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.StromspeicherAuslegung));
        cut.Render();
        Assert.Empty(cut.FindAll(".epos-simansicht"));
        Assert.Single(cut.FindAll(".epos-spauslegung"));

        // ④ „← zurueck" fuehrt DORTHIN ZURUECK, WOHER MAN KAM - und nicht auf die
        //    Startseite, wie es #192 unter Windows tat.
        cut.FindAll("button").First(k => k.TextContent.Trim() == "← zurück").Click();
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-spauslegung"));
        Assert.Single(cut.FindAll(".epos-simansicht"));
        Assert.Empty(cut.FindAll(".epos-startseite"));

        // Und zwar auf Schritt ③, Blatt „Stromspeicher".
        Assert.NotEmpty(cut.FindAll("div.epos-simerg"));
        Assert.Contains("blatt=" + EPOS.UI.Seiten.Simulation.SimulationErgebnisSeite.Blatt.Stromspeicher,
                        MarkeDerSimulation(cut));
    }

    /// <summary>Die Marke der stehenden Simulationsansicht — über ihre Prüfhilfe.</summary>
    private static string MarkeDerSimulation(IRenderedComponent<AppWurzel> cut)
        => cut.FindComponent<EPOS.UI.Seiten.Simulation.SimulationSeite>().Instance.AktuelleMarke;

    /// <summary>
    /// Der KI-Hilfe-Assistent ist ein ABSTECHER: Er kehrt dorthin zurück, woher er
    /// kam (#199) — seit #207 über denselben Stapel wie die Auslegung, nicht mehr
    /// über ein eigenes Feld.
    /// </summary>
    [Fact]
    public void Der_KI_Assistent_kehrt_ueber_den_Stapel_in_die_Simulation_zurueck()
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            KiAssistent = new Dictionary<string, object>()
        };
        var cut = MitSimulation(quelle);

        Assert.True(cut.Instance.OeffneMaske(
            Seitenschluessel.Simulation,
            EPOS.UI.Seiten.Simulation.SimulationMarke.SCHRITT_ERGEBNIS));
        cut.Render();
        Assert.Single(cut.FindAll(".epos-simansicht"));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.KiAssistent));
        cut.Render();
        Assert.Single(cut.FindAll(".epos-kichat"));
        Assert.Empty(cut.FindAll(".epos-simansicht"));

        // Der letzte Knopf der Chatleiste ist „Schliessen".
        var knoepfe = cut.FindAll(".epos-kichat-knoepfe button.epos-knopf");
        knoepfe[knoepfe.Count - 1].Click();
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-kichat"));
        Assert.Single(cut.FindAll(".epos-simansicht"));
        Assert.Empty(cut.FindAll(".epos-startseite"));
    }

    /// <summary>
    /// Der Stapel ist FLACH und wird beim Wechsel auf die Startansicht geleert
    /// (Konzept 2.1) — wer von dort aus neu beginnt, hat keinen Weg mehr hinter
    /// sich. Danach landet der Rückweg der Auslegung wieder auf der Startseite.
    /// </summary>
    [Fact]
    public void Ein_Wechsel_auf_die_Startansicht_leert_den_Stapel()
    {
        var quelle = new TestProjektquelle(ZweiProjekte)
        {
            Auslegung = new Dictionary<string, object>
            {
                ["Dienste"] = new EPOS.UI.Seiten.Strom.StromspeicherAuslegungDienste(),
                ["PlanerVerfuegbar"] = true
            }
        };
        var cut = MitSimulation(quelle);

        cut.Instance.OeffneMaske(Seitenschluessel.Simulation);
        cut.Render();
        cut.Instance.OeffneMaske(Seitenschluessel.Startseite);
        cut.Render();
        Assert.Single(cut.FindAll(".epos-startseite"));

        cut.Instance.OeffneMaske(Seitenschluessel.StromspeicherAuslegung);
        cut.Render();
        cut.FindAll("button").First(k => k.TextContent.Trim() == "← zurück").Click();
        cut.Render();

        Assert.Single(cut.FindAll(".epos-startseite"));
        Assert.Empty(cut.FindAll(".epos-simansicht"));
    }

    /// <summary>
    /// <b>Die zwei alten Schlüssel bleiben gültig</b> (Auftrag #207): Sie öffnen
    /// dieselbe Ansicht, nur auf verschiedenen Schritten — als EINSTIEGSMARKEN.
    /// </summary>
    [Fact]
    public void Die_zwei_alten_Simulationsschluessel_sind_Einstiegsmarken_derselben_Ansicht()
    {
        var cut = MitSimulation(new TestProjektquelle(ZweiProjekte));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.SimulationKonfiguration));
        cut.Render();
        Assert.Single(cut.FindAll(".epos-simansicht"));
        Assert.NotEmpty(cut.FindAll("div.epos-simkonfig"));
        Assert.Empty(cut.FindAll("div.epos-simerg"));

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.SimulationErgebnis));
        cut.Render();
        Assert.Single(cut.FindAll(".epos-simansicht"));
        Assert.NotEmpty(cut.FindAll("div.epos-simerg"));
    }

    /// <summary>
    /// Ohne Parametersatz bleibt die Liste stehen und sagt warum — der
    /// Windows-Zustand „Seite geht nicht auf", auf iOS der Stand vor Stufe S2.
    /// </summary>
    [Fact]
    public void Ohne_Parametersatz_geht_die_Simulation_nicht_auf()
    {
        var quelle = new TestProjektquelle(ZweiProjekte);
        Services.AddSingleton<IProjektQuelle>(quelle);
        var cut = Render<AppWurzel>();

        Assert.True(cut.Instance.OeffneMaske(Seitenschluessel.Simulation));
        cut.Render();

        Assert.Empty(cut.FindAll(".epos-simansicht"));
        Assert.Contains("Simulation nicht öffnen", cut.Find(".epos-warnbanner").TextContent);
    }
}

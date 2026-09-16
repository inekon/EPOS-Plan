using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Waermepumpe;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Baustein <c>WaermepumpeKonfiguration</c> (Anwenderentscheid 16.09.2026) — der
/// Block, der bis dahin im Anlagendialog unter der Überschrift „Wärmeerzeuger
/// Spitzenlast:" stand.
///
/// <para><b>Zwei Wirte, ein Block.</b> Die Detailansicht zeigt ihn in einer
/// <c>Ueberlagerung</c> hinter dem Knopf „Konfiguration…", Simulation › Konfiguration
/// in ihrem eigenen Abschnitt. Hier stehen deshalb die Regeln des BAUSTEINS —
/// Feldbestand, Bindung, Sichtbarkeit —, in
/// <c>WaermepumpeAnlageDialogTests</c> nur, dass der Dialog sie durchreicht.</para>
/// </summary>
public class WaermepumpeKonfigurationTests : EposBunitContext
{
    public WaermepumpeKonfigurationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static WaermepumpeAnlageDaten Voll() => new()
    {
        Bezeichner = "WP Alpha",
        Heizstab = true,
        Sperrung = false,
        SperrzeitVon = 0,
        SperrzeitBis = 0,
        BivalenterBetrieb = false,
        Betriebsart = "",
        Abschaltpunkt = -5,
        CarrierId = 60
    };

    private IRenderedComponent<WaermepumpeKonfiguration> Aufbauen(
        WaermepumpeAnlageDaten? daten = null,
        IReadOnlyList<EnergietraegerWahl.Eintrag>? traegerkatalog = null,
        Action? geaendert = null,
        bool aktiv = true)
        => Render<WaermepumpeKonfiguration>(p => p
            .Add(x => x.Daten, daten ?? Voll())
            .Add(x => x.Traegerkatalog, traegerkatalog ?? Array.Empty<EnergietraegerWahl.Eintrag>())
            .Add(x => x.Aktiv, aktiv)
            .Add(x => x.Geaendert, () => geaendert?.Invoke()));

    // =================================================================================
    // Feldbestand
    // =================================================================================

    /// <summary>
    /// Der Block trägt genau das, was bis zum 16.09.2026 in der Gruppe „Wärmeerzeuger
    /// Spitzenlast:" stand: drei Häkchen, die beiden Sperrzeitfelder, die zwei
    /// Formulargruppen und die drei farbigen Erklärkästen.
    /// </summary>
    [Fact]
    public void Der_Feldbestand_des_Blocks_steht()
    {
        var cut = Aufbauen();

        var schalter = cut.FindAll(".epos-schalter").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Heizstab mitrechnen", schalter);
        Assert.Contains("Sperrzeit durch Energieversorger", schalter);
        Assert.Contains("Bivalenter Betrieb", schalter);

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Sperrzeit von", texte);
        Assert.Contains("Sperrzeit bis", texte);

        var gruppen = cut.FindAll(".epos-formulargruppe-titel").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Wärmepumpenleistung / maximale Betriebszeit:", gruppen);
        Assert.Contains("Außentemperaturgesteuerter Betrieb:", gruppen);

        Assert.Equal(3, cut.FindAll(".epos-wp-erklaerung").Count);

        // Der Gruppenkopf gehoert dem WIRT (Ueberlagerungstitel bzw. Abschnitt).
        Assert.Empty(cut.FindAll(".epos-gruppenkopf-titel"));
        Assert.Contains("Der Heizstab dieser Wärmepumpe wird bei Unterdeckung zugeschaltet.",
                        cut.FindAll(".epos-herleitung").Select(e => e.TextContent));
    }

    // =================================================================================
    // Der Heizstab je Anlage (Schemaschritt 79, 16.09.2026)
    // =================================================================================

    /// <summary>
    /// <b>Der Schalter heißt „Heizstab mitrechnen"</b> und sagt darunter, was das
    /// bedeutet. Bis zum 16.09.2026 stand da „Elektrische Nachheizung aktivieren (falls
    /// vorhanden)" — ein Text aus der Zeit, als dieser Schalter gar nicht rechnete: Er
    /// entschied allein über die Energieträgerwahl, gerechnet wurde der projektweite
    /// <c>Tab_Einstellungen.WP_Heizstab</c>. Den gibt es seit Schemaschritt 79 nicht
    /// mehr; der Lauf liest DIESEN, je Wärmepumpe.
    /// </summary>
    [Fact]
    public void Der_Heizstabschalter_heisst_mitrechnen_und_erklaert_sich()
    {
        var daten = Voll();
        daten.HeizstabLeistung = 9;
        var cut = Aufbauen(daten);

        Assert.Contains("Heizstab mitrechnen",
                        cut.FindAll(".epos-schalter").Select(e => e.TextContent.Trim()));
        Assert.DoesNotContain("Elektrische Nachheizung", cut.Markup);

        var herleitungen = cut.FindAll(".epos-herleitung").Select(e => e.TextContent).ToList();
        Assert.Contains("Der Heizstab dieser Wärmepumpe wird bei Unterdeckung zugeschaltet.",
                        herleitungen);

        // Mit hinterlegter Leistung KEIN zweiter Hinweis.
        Assert.DoesNotContain(herleitungen, h => h.Contains("keine Heizstableistung hinterlegt"));
    }

    /// <summary>
    /// <b>Ohne hinterlegte Heizstableistung sagt der Block es — und sperrt nicht.</b>
    /// Ob ein Heizstab mitgerechnet werden soll, ist eine Entscheidung; ob eine
    /// Leistung dafür gepflegt ist, eine Tatsache. Ein gesperrter Schalter verschwiege,
    /// welche der beiden fehlt.
    /// </summary>
    /// <remarks>
    /// <b>0 und leer sind dasselbe</b> — <c>Tab_WP.Heizung</c> ist eine INTEGER-Spalte,
    /// und ein Heizstab mit 0 kW ist keiner.
    /// </remarks>
    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public void Ohne_Heizstableistung_steht_der_Hinweis_und_der_Schalter_bleibt_bedienbar(int? kw)
    {
        var daten = Voll();
        daten.HeizstabLeistung = kw;
        var cut = Aufbauen(daten);

        Assert.Contains(cut.FindAll(".epos-herleitung").Select(e => e.TextContent),
                        h => h.Contains("keine Heizstableistung hinterlegt"));

        var haken = cut.FindAll(".epos-schalter input[type=checkbox]")[0];
        Assert.False(haken.HasAttribute("disabled"));

        haken.Change(false);
        Assert.False(daten.Heizstab);
    }

    /// <summary>
    /// Der Hinweis ist KEIN eigener Zustand: Er hängt am Feldsatz, den der Wirt hält —
    /// im Anlagendialog zieht die Eingabe im Stammfeld „Heizstab" ihn unmittelbar nach.
    /// </summary>
    [Fact]
    public void Der_Hinweis_verschwindet_mit_einer_hinterlegten_Leistung()
    {
        var daten = Voll();
        daten.HeizstabLeistung = 0;
        var cut = Aufbauen(daten);
        Assert.Contains("keine Heizstableistung hinterlegt", cut.Markup);

        daten.HeizstabLeistung = 6;
        cut.Render();

        Assert.DoesNotContain("keine Heizstableistung hinterlegt", cut.Markup);
    }

    /// <summary>
    /// <b>Ohne Gaben zeichnet er</b> (Hausregel EPOS.UI): leerer Satz, leerer
    /// Trägerkatalog, Texte aus dem selbstfüllenden Bündel.
    /// </summary>
    [Fact]
    public void Ohne_Gaben_zeichnet_der_Baustein()
    {
        var cut = Render<WaermepumpeKonfiguration>();

        Assert.NotEmpty(cut.FindAll(".epos-wp-konfiguration"));
        Assert.Equal(3, cut.FindAll(".epos-wp-erklaerung").Count);
        Assert.Contains("Heizstab mitrechnen",
                        cut.FindAll(".epos-schalter").Select(e => e.TextContent.Trim()));
        Assert.Empty(cut.FindAll(".epos-traegerwahl"));

        // Ohne Gaben traegt der leere Feldsatz auch keine Heizstableistung - der
        // Hinweis steht also, und zwar in der Sprache des Buendels.
        Assert.Contains("keine Heizstableistung hinterlegt", cut.Markup);
    }

    // =================================================================================
    // Bindung
    // =================================================================================

    [Fact]
    public void Jede_Eingabe_schreibt_in_die_Daten_und_meldet()
    {
        int gemeldet = 0;
        var daten = Voll();
        var cut = Aufbauen(daten, geaendert: () => gemeldet++);

        IElement Haken(int i) => cut.FindAll(".epos-schalter input[type=checkbox]")[i];
        IElement Feld(int i) => cut.FindAll("input[type=text]")[i];

        Haken(0).Change(false);                 // Heizstab
        Assert.False(daten.Heizstab);

        Haken(1).Change(true);                  // Sperrzeit
        Assert.True(daten.Sperrung);

        Feld(0).Input("3");
        Feld(1).Input("18");
        Assert.Equal(3, daten.SperrzeitVon);
        Assert.Equal(18, daten.SperrzeitBis);

        Assert.Equal(4, gemeldet);
    }

    [Fact]
    public void Die_Betriebsart_schreibt_den_Steuerwert_aus_DbWerte()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        var cut = Aufbauen(daten);

        cut.Find("select").Change("2");
        Assert.Equal(WindowsFormsApplication1.DbWerte.WP_BETRIEBSART_TEILPARALLEL, daten.Betriebsart);
    }

    /// <summary>
    /// W7‑B‑2: Ein Altwert wie „parallelbetrieb" wird TOLERANT gelesen und steht damit
    /// gewählt in der Klappliste, statt sie leer zu lassen.
    /// </summary>
    [Fact]
    public void Ein_alter_Betriebsart_Text_steht_gewaehlt_in_der_Klappliste()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        daten.Betriebsart = "parallelbetrieb";
        var cut = Aufbauen(daten);

        Assert.Equal("1", cut.Find("select").GetAttribute("value"));
    }

    // =================================================================================
    // Sichtbarkeitsregeln
    // =================================================================================

    [Fact]
    public void Die_Betriebsart_erscheint_erst_mit_bivalentem_Betrieb()
    {
        var cut = Aufbauen();
        Assert.DoesNotContain("Betriebsart", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));

        cut.FindAll(".epos-schalter input[type=checkbox]")[2].Change(true);

        Assert.Contains("Betriebsart", cut.FindAll(".epos-feld-text").Select(e => e.TextContent));
    }

    /// <summary>
    /// Die Bivalenztemperatur erscheint nur, wo sie RECHENWIRKSAM ist —
    /// Teilparallel- und Alternativbetrieb; im Parallelbetrieb bleibt der Wert
    /// wirkungslos und das Feld verborgen.
    /// </summary>
    [Fact]
    public void Die_Bivalenztemperatur_erscheint_nur_wo_sie_rechenwirksam_ist()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        var cut = Aufbauen(daten);

        IEnumerable<string> Texte() => cut.FindAll(".epos-feld-text").Select(e => e.TextContent);
        IElement Betriebsart() => cut.Find("select");

        Assert.DoesNotContain("Bivalenztemperatur", Texte());   // ohne Betriebsart

        Betriebsart().Change("1");                              // Parallelbetrieb
        Assert.DoesNotContain("Bivalenztemperatur", Texte());

        Betriebsart().Change("0");                              // Alternativbetrieb
        Assert.Contains("Bivalenztemperatur", Texte());

        Betriebsart().Change("2");                              // Teilparallelbetrieb
        Assert.Contains("Bivalenztemperatur", Texte());
    }

    /// <summary>
    /// Die drei Erklärkästen stehen IMMER (label21/22/23 kannten keine
    /// Sichtbarkeitsregel) — hervorgehoben ist der, dessen Betriebsart gewählt ist.
    /// </summary>
    [Fact]
    public void Der_Kasten_der_gewaehlten_Betriebsart_ist_hervorgehoben()
    {
        var daten = Voll();
        daten.BivalenterBetrieb = true;
        daten.Betriebsart = WindowsFormsApplication1.DbWerte.WP_BETRIEBSART_PARALLEL;
        var cut = Aufbauen(daten);

        var kaesten = cut.FindAll(".epos-wp-erklaerung");
        Assert.DoesNotContain("epos-wp-erklaerung--aktiv", kaesten[0].ClassName);
        Assert.Contains("epos-wp-erklaerung--aktiv", kaesten[1].ClassName);
        Assert.DoesNotContain("epos-wp-erklaerung--aktiv", kaesten[2].ClassName);
    }

    // =================================================================================
    // Energietraeger (ET-5) und Sperre
    // =================================================================================

    [Fact]
    public void Die_Traegerwahl_steht_nur_mit_Katalog_und_schreibt_in_die_Daten()
    {
        var daten = Voll();
        Assert.Empty(Aufbauen(daten).FindAll(".epos-traegerwahl"));

        var cut = Aufbauen(daten, traegerkatalog: new[]
        {
            new EnergietraegerWahl.Eintrag(11, "Gas", "Erdgas E"),
            new EnergietraegerWahl.Eintrag(60, "Strom", "Elektrische Energie"),
            new EnergietraegerWahl.Eintrag(58, "Strom", "Elektrische Energie 2")
        });

        var selects = cut.Find(".epos-traegerwahl").QuerySelectorAll("select");
        Assert.Equal(2, selects.Length);

        selects[1].Change("58");
        Assert.Equal(58, daten.CarrierId);
    }

    /// <summary>Nur ansehen: Jedes Feld ist gesperrt — kein Klick ändert etwas.</summary>
    [Fact]
    public void Ohne_Aktiv_ist_jedes_Feld_gesperrt()
    {
        var cut = Aufbauen(aktiv: false);

        Assert.All(cut.FindAll(".epos-schalter input[type=checkbox]"),
                   k => Assert.True(k.HasAttribute("disabled")));
        Assert.All(cut.FindAll("input[type=text]"),
                   f => Assert.True(f.HasAttribute("disabled") || f.HasAttribute("readonly")));
    }
}

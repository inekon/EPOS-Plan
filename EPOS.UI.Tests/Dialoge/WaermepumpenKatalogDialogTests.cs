using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bunit;
using EPOS.UI.Dialoge.Waermepumpe;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>Wärmepumpen-Katalog im SPALTENMODELL</b> — Anwenderentscheid
/// <b>W14a‑E‑10</b> vom 07.09.2026, Stufe <b>S2.2</b>: „Das Schema des Dialogs
/// sollte immer gleich aussehen (Wärmepumpe ähnlich wie PV-Module und
/// Heizkessel)."
///
/// <para><b>Was dieser Prüfstand hält, ist der KERN des Entscheids.</b> Bis
/// hierher war dieser Dialog der Gegenentwurf: elf Bedienelemente in einer
/// Filterleiste — sieben Klapplisten (Hersteller, Auslegung, Quelle, Regelung,
/// Bauart, Aufstellung, Zuheizung) und vier Zahlenfelder (VL min/max, P_N
/// min/max) —, dazu zwei Knöpfe „Daten filtern" und „Filter Reset" und eine
/// Herleitungszeile. Alle dreizehn sind weg; an ihrer Stelle stehen NEUN
/// Spalten mit Sortierpfeil und Trichter und EINE Suchzeile.</para>
///
/// <para>Die Fälle prüfen deshalb dreierlei: dass die elf Bedienelemente
/// wirklich fort sind, dass die neun Spalten dastehen — und dass die Auswahl,
/// das Übernehmen und der Tastaturweg unverändert arbeiten.</para>
/// </summary>
public class WaermepumpenKatalogDialogTests : BunitContext
{
    /// <summary>
    /// Das PROFIL des Katalogs — dieselben neun Spalten wie in der Verwaltung
    /// (S1.4), hier mit dem echten Übersetzer, damit im Kopf steht, was der
    /// Anwender sieht.
    /// </summary>
    private static readonly Katalogfilterprofil Profil =
        Katalogfilterprofil.Finde(Anlagenart.Waermepumpe,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>Dasselbe Profil MIT der Spalte „im Projekt verwendet" (Q12).</summary>
    private static readonly Katalogfilterprofil ProfilMitVerwendung =
        Katalogfilterprofil.MitVerwendung(Anlagenart.Waermepumpe,
            s => Resource.ResourceManager.GetString(s) ?? s);

    /// <summary>
    /// Der Filterstand DIESES Prüfstands. Ohne ihn nähme der Dialog den aus dem
    /// <c>Katalogfilterregister</c> — der lebt prozessweit, und xunit fährt
    /// Testklassen nebeneinander.
    /// </summary>
    private readonly Katalogfilterstand _filterstand = new();

    /// <summary>
    /// Vier Katalogzeilen mit echten Werten — dieselben vier wie im Vorläufer-
    /// Prüfstand, nur als <see cref="Katalogfilterzeile"/> statt als
    /// <c>WaermepumpenKatalogZeile</c>.
    /// </summary>
    private static IReadOnlyList<Katalogfilterzeile> Katalogzeilen() => new[]
    {
        Zeile(1, "Alpha", "CS-070", "Luft-Wasser",  7.0, 35, 55,  3, false, 3.4),
        Zeile(2, "Alpha", "CS-127", "Luft-Wasser", 12.7, 35, 60,  6, true,  3.2),
        Zeile(3, "Beta",  "BX-200", "Sole-Wasser", 20.0, 30, 45,  9, false, 4.1),
        Zeile(4, "Gamma", "cs-990", "Sole-Wasser", 99.0, 25, 35,  0, false, 4.4),
    };

    private static Katalogfilterzeile Zeile(int id, string hersteller, string modell,
                                            string quelle, double pN, double vlMin,
                                            double vlMax, double zuheizung, bool kuehlen,
                                            double cop)
        => new Katalogfilterzeile(id, modell)
            .MitText(Katalogfilterprofil.SpHersteller, hersteller)
            .MitText(Katalogfilterprofil.SpBezeichner, modell)
            .MitText(Katalogfilterprofil.SpQuelle, quelle)
            .MitZahl(Katalogfilterprofil.SpNennleistung, pN, 1)
            .MitZahl(Katalogfilterprofil.SpVlMin, vlMin, 0)
            .MitZahl(Katalogfilterprofil.SpVlMax, vlMax, 0)
            .MitZahl(Katalogfilterprofil.SpZuheizung, zuheizung, 1)
            .MitKennzeichen(Katalogfilterprofil.SpKuehlen, kuehlen)
            .MitZahl(Katalogfilterprofil.SpCop, cop, 2);

    public WaermepumpenKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;   // QuickGrid laedt ein JS-Modul
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private IRenderedComponent<WaermepumpenKatalogDialog> Aufbauen(
        Action<string?>? geschlossen = null,
        IReadOnlyList<Katalogfilterzeile>? zeilen = null,
        Katalogfilterprofil? profil = null,
        IReadOnlyCollection<string>? imProjekt = null)
        => Render<WaermepumpenKatalogDialog>(p => p
            .Add(x => x.Zeilen, zeilen ?? Katalogzeilen())
            .Add(x => x.Profil, profil ?? Profil)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.ImProjektVerwendet, imProjekt)
            .Add(x => x.Geschlossen, n => geschlossen?.Invoke(n)));

    /// <summary>Die Datenzeilen des Rasters (ohne Kopfzeile).</summary>
    private static int Trefferzahl(IRenderedComponent<WaermepumpenKatalogDialog> cut)
        => cut.FindAll(".epos-raster tbody tr").Count;

    // =================================================================================
    // 1 — Die elf Bedienelemente sind weg
    // =================================================================================

    /// <summary>
    /// <b>Der Kern des Entscheids.</b> Keine Klappliste, kein Zahlenfeldpaar, kein
    /// Knopf „Daten filtern", kein Knopf „Filter Reset", keine Herleitungszeile —
    /// und keine Filterleiste (<c>.epos-zahlenraster</c>), in der sie stünden.
    /// </summary>
    [Fact]
    public void Die_elf_Bedienelemente_der_Filterleiste_sind_gefallen()
    {
        var cut = Aufbauen();

        Assert.Empty(cut.FindAll(".epos-zahlenraster"));
        Assert.Empty(cut.FindAll("select"));
        Assert.Empty(cut.FindAll(".epos-herleitung"));

        var knopftexte = cut.FindAll(".epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.DoesNotContain("Daten filtern", knopftexte);
        Assert.DoesNotContain("Filter Reset", knopftexte);

        // Es bleiben ZWEI Knöpfe in der Schlussleiste.
        Assert.Equal(2, knopftexte.Count);
        Assert.Contains(knopftexte, k => k.Contains("übernehmen"));
        Assert.Contains(knopftexte, k => k.Contains("Abbrechen"));
    }

    /// <summary>
    /// <b>An ihrer Stelle stehen die NEUN Spalten</b> (Konzept 5.6.5, Reiter M2) —
    /// plus die Wahlspalte. Acht davon tragen einen Trichter; „Kühlen" ist ein
    /// Kennzeichen und trägt nur den Sortierpfeil (5.6.2).
    /// </summary>
    [Fact]
    public void An_ihrer_Stelle_stehen_neun_Spalten_mit_Trichter_und_Sortierpfeil()
    {
        var cut = Aufbauen();

        var kopf = cut.FindAll(".epos-raster th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(Profil.Spalten.Count + 1, kopf.Count);        // 9 + Wahl
        Assert.Equal(9, Profil.Spalten.Count);

        Assert.Contains(kopf, k => k.StartsWith("Hersteller"));
        Assert.Contains(kopf, k => k.StartsWith("Modell"));
        Assert.Contains(kopf, k => k.StartsWith("Quelle"));
        Assert.Contains(kopf, k => k.Contains("P_N"));
        Assert.Contains(kopf, k => k.Contains("VL min"));
        Assert.Contains(kopf, k => k.Contains("VL max"));
        Assert.Contains(kopf, k => k.Contains("Zuheizung"));
        Assert.Contains(kopf, k => k.StartsWith("Kühlen"));
        Assert.Contains(kopf, k => k.StartsWith("COP"));

        // Acht Trichter: alle ausser dem Kennzeichen „Kuehlen".
        Assert.Equal(8, cut.FindAll(".epos-trichter").Count);
        Assert.Equal(8, Profil.Spalten.Count(x => x.Filterbar));
        Assert.False(Profil.Spalte(Katalogfilterprofil.SpKuehlen)!.Filterbar);

        // Neun Sortierpfeile - jede Parameterspalte laesst sich sortieren.
        Assert.Equal(9, cut.FindAll(".epos-sortierpfeil").Count);
    }

    /// <summary>
    /// <b>Drei der elf werden KEINE Spalte</b> — und das ist gemessen, nicht
    /// vergessen: Die Bauart ist in 45 von 51 Sätzen leer, „Auslegung" ist
    /// dieselbe Aussage wie das Kennzeichen „Kühlen" (beides
    /// <c>Kuehlleistung &gt; 0</c>), und Regelung wie Aufstellung stehen im
    /// Kenndatenblock. Die Suche über alle Spalten findet sie weiterhin, sobald
    /// sie als Wert dastehen.
    /// </summary>
    [Fact]
    public void Bauart_Regelung_und_Aufstellung_stehen_nicht_als_Spalte()
    {
        var kopf = Aufbauen().FindAll(".epos-raster th").Select(e => e.TextContent.Trim()).ToList();

        Assert.DoesNotContain(kopf, k => k.StartsWith("Bauart"));
        Assert.DoesNotContain(kopf, k => k.StartsWith("Regelung"));
        Assert.DoesNotContain(kopf, k => k.StartsWith("Aufstellung"));
        Assert.DoesNotContain(kopf, k => k.StartsWith("Auslegung"));
    }

    /// <summary>
    /// Die Trefferzahl steht in der SUCHZEILE („4 von 4 Sätzen") statt als eigene
    /// Herleitungszeile; im Vorläufer stand sie in der Fensterüberschrift, die eine
    /// Überlagerung nicht hat (A‑7).
    /// </summary>
    [Fact]
    public void Die_Trefferzahl_steht_in_der_Suchzeile()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll(".epos-katalog-suchzeile"));
        Assert.Equal("4 von 4 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Equal(4, Trefferzahl(cut));
    }

    // =================================================================================
    // 2 — Der Filter sitzt im Spaltenkopf
    // =================================================================================

    /// <summary>
    /// <b>Die Suche über alle Spalten</b> ersetzt das Feld „Modell filtern
    /// (z. B. CS*7*)" — mit denselben Platzhaltern, aber über ALLE Spalten statt
    /// nur über den Bezeichner.
    /// </summary>
    [Fact]
    public void Das_Suchfeld_kennt_Platzhalter_und_Teilsuche()
    {
        var cut = Aufbauen();
        var suche = cut.Find(".epos-katalog-suchfeld input");

        suche.Input("CS*7*");
        Assert.Equal(2, Trefferzahl(cut));

        suche.Input("CS-0?0");
        Assert.Equal(1, Trefferzahl(cut));

        // Klartext = Teilsuche, gross/klein egal.
        suche.Input("cs");
        Assert.Equal(3, Trefferzahl(cut));

        // ... und sie greift auch auf eine ANDERE Spalte als den Bezeichner.
        suche.Input("Sole");
        Assert.Equal(2, Trefferzahl(cut));
    }

    /// <summary>
    /// <b>Aus den zwei Zahlenfeldpaaren wird EIN Feld je Zahlenspalte</b>
    /// (Frage Q1 = ja): „5..12" statt zweier Kästen. Der Ausdruck steht im
    /// <c>Katalogfilterstand</c> des Wirtes und wirkt vor dem Raster.
    /// </summary>
    [Fact]
    public void Eine_Zahlenspalte_versteht_einen_Bereichsausdruck()
    {
        var cut = Aufbauen();

        _filterstand.Setzen(Katalogfilterprofil.SpNennleistung, "5..15");
        cut.Render();
        Assert.Equal(2, Trefferzahl(cut));            // CS-070 (7,0) und CS-127 (12,7)

        _filterstand.Setzen(Katalogfilterprofil.SpNennleistung, ">=20");
        cut.Render();
        Assert.Equal(2, Trefferzahl(cut));            // BX-200 (20,0) und cs-990 (99,0)
    }

    /// <summary>
    /// <b>Das Beispiel des Mockups (Reiter M2)</b>, hier auf den vier Prüfzeilen:
    /// „Quelle enthält Luft" UND „P_N 5..12" — die Spaltenfilter wirken UND
    /// (5.6.3).
    /// </summary>
    [Fact]
    public void Zwei_Spaltenfilter_wirken_zusammen()
    {
        var cut = Aufbauen();

        _filterstand.Setzen(Katalogfilterprofil.SpQuelle, "Luft");
        cut.Render();
        Assert.Equal(2, Trefferzahl(cut));

        _filterstand.Setzen(Katalogfilterprofil.SpNennleistung, "5..12");
        cut.Render();
        Assert.Equal(1, Trefferzahl(cut));
        Assert.Contains("CS-070", cut.Find(".epos-raster tbody").TextContent);
    }

    /// <summary>
    /// Der Textknopf „Filter zurücksetzen" der Suchzeile ersetzt den Knopf
    /// „Filter Reset" — er erscheint NUR, solange ein Spaltenfilter gesetzt ist.
    /// </summary>
    [Fact]
    public void Der_Ruecksetzer_erscheint_nur_mit_gesetztem_Filter()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll(".epos-katalog-ruecksetzer"));

        _filterstand.Setzen(Katalogfilterprofil.SpQuelle, "Luft");
        cut.Render();
        Assert.Single(cut.FindAll(".epos-katalog-ruecksetzer"));

        cut.Find(".epos-katalog-ruecksetzer").Click();
        Assert.Equal(4, Trefferzahl(cut));
        Assert.Empty(cut.FindAll(".epos-katalog-ruecksetzer"));
    }

    // =================================================================================
    // 3 — „im Projekt verwendet" (Q12)
    // =================================================================================

    /// <summary>
    /// Die Spalte gibt es nur mit dem Profil des PROJEKTdialogs — und sie sagt „Ja"
    /// für die Sätze, die in der Projektliste des Wirtes stehen.
    /// </summary>
    [Fact]
    public void Die_Spalte_im_Projekt_verwendet_kommt_aus_der_Projektliste()
    {
        var cut = Aufbauen(profil: ProfilMitVerwendung, imProjekt: new[] { "CS-127" });

        var kopf = cut.FindAll(".epos-raster th").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains(kopf, k => k.StartsWith("im Projekt verwendet"));

        // Genau EINE Zeile traegt „Ja".
        var zeilen = cut.FindAll(".epos-raster tbody tr");
        int ja = zeilen.Count(z => z.QuerySelectorAll("td").Last().TextContent.Trim() == "Ja");
        Assert.Equal(1, ja);
    }

    /// <summary>Ohne die Spalte im Profil gibt es sie auch im Markup nicht.</summary>
    [Fact]
    public void Ohne_Projektliste_gibt_es_die_Spalte_nicht()
    {
        var kopf = Aufbauen().FindAll(".epos-raster th").Select(e => e.TextContent.Trim()).ToList();
        Assert.DoesNotContain(kopf, k => k.StartsWith("im Projekt verwendet"));
    }

    // =================================================================================
    // 4 — Übernehmen, Abbrechen, Tastatur (unverändert)
    // =================================================================================

    [Fact]
    public void Uebernehmen_ist_ohne_Zeile_gesperrt()
    {
        string? ergebnis = "nicht gerufen";
        var cut = Aufbauen(n => ergebnis = n);

        var uebernehmen = cut.FindAll(".epos-leiste button")[0];
        Assert.True(uebernehmen.HasAttribute("disabled"));

        uebernehmen.Click();
        Assert.Equal("nicht gerufen", ergebnis);
    }

    [Fact]
    public void Uebernehmen_liefert_den_Bezeichner_der_gewaehlten_Zeile()
    {
        string? ergebnis = null;
        var cut = Aufbauen(n => ergebnis = n);

        cut.FindAll(".epos-raster tbody tr")[1].QuerySelector("button")!.Click();
        cut.FindAll(".epos-leiste button")[0].Click();

        Assert.Equal("CS-127", ergebnis);
    }

    /// <summary>
    /// <b>Die Markierung hängt am BEZEICHNER</b>, nicht an der Zeilennummer: Sie
    /// bleibt stehen, auch wenn ein Filter die Zeile ausblendet (Hausregel aus dem
    /// <c>EnergietraegerDialog</c>, W4) — und „Übernehmen" liefert danach dieselbe
    /// Wärmepumpe.
    /// </summary>
    [Fact]
    public void Die_Markierung_ueberlebt_einen_Filterwechsel()
    {
        string? ergebnis = null;
        var cut = Aufbauen(n => ergebnis = n);

        cut.FindAll(".epos-raster tbody tr")[1].QuerySelector("button")!.Click();
        Assert.Equal("CS-127", cut.Instance.Gewaehlt);

        // Ein Filter, der GENAU diese Zeile ausblendet.
        _filterstand.Setzen(Katalogfilterprofil.SpQuelle, "Sole");
        cut.Render();
        Assert.Equal(2, Trefferzahl(cut));
        Assert.Equal("CS-127", cut.Instance.Gewaehlt);

        cut.FindAll(".epos-leiste button")[0].Click();
        Assert.Equal("CS-127", ergebnis);
    }

    [Fact]
    public void Die_Zeilenwahl_ist_ein_Beruehrungsziel_statt_eines_Doppelklicks()
    {
        // A-8: Der Vorlaeufer nahm die Zeile per CellDoubleClick an. Ein Doppelklick
        // ist kein Beruehrungsziel (M2/iL4) - wie in W5 A-3 wird daraus ein Knopf.
        var cut = Aufbauen();
        Assert.Equal(4, cut.FindAll(".epos-raster tbody tr button").Count);
    }

    [Fact]
    public void Abbrechen_liefert_null()
    {
        string? ergebnis = "nicht gerufen";
        var cut = Aufbauen(n => ergebnis = n);

        cut.FindAll(".epos-leiste button")[1].Click();
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_mit_null()
    {
        string? ergebnis = "nicht gerufen";
        var cut = Aufbauen(n => ergebnis = n);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Null(ergebnis);
    }

    /// <summary>
    /// Ein leerer Katalog zeigt „Kein Treffer." statt einer leeren Liste
    /// (<c>ETV_SUCHE_LEER</c>, wie im <c>EnergietraegerDialog</c>).
    /// </summary>
    [Fact]
    public void Ein_leerer_Katalog_zeigt_Kein_Treffer()
    {
        var cut = Aufbauen(zeilen: Array.Empty<Katalogfilterzeile>());

        Assert.Equal(0, Trefferzahl(cut));
        Assert.Equal("0 von 0 Sätzen", cut.Find(".epos-katalog-treffer").TextContent);
        Assert.Single(cut.FindAll(".epos-katalog-leer"));
    }

    /// <summary>Englische Beschriftungen: Die Maske war NICHT lokalisiert (W7.9).</summary>
    [Fact]
    public void Die_Texte_lassen_sich_von_aussen_setzen()
    {
        var cut = Render<WaermepumpenKatalogDialog>(p => p
            .Add(x => x.Zeilen, Katalogzeilen())
            .Add(x => x.Profil, Profil)
            .Add(x => x.Filterstandvorgabe, _filterstand)
            .Add(x => x.TitelText, "Heat pump catalogue")
            .Add(x => x.BtnUebernehmenText, "Apply")
            .Add(x => x.AbbrechenText, "Cancel"));

        Assert.Equal("Heat pump catalogue", cut.Find(".epos-dialog-titel").TextContent);
        var knoepfe = cut.FindAll(".epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Contains("Apply", knoepfe);
        Assert.Contains("Cancel", knoepfe);
    }
}

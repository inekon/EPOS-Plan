using System.Collections.Generic;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Zeilenmarkierung (iU9-W13.0l) — die Markierungsregel der vier Einlesemasken.
/// Vorbild ist <c>SelectionMode.MultiExtended</c> der <c>ListBox</c>: ein Klick
/// waehlt eine Zeile, <c>Strg</c> nimmt dazu oder weg, <c>Umschalt</c> waehlt
/// den Bereich ab dem Anker.
/// </summary>
public class ZeilenmarkierungTests
{
    [Fact]
    public void Ein_einfacher_Klick_waehlt_genau_eine_Zeile()
    {
        var w = new Zeilenmarkierung();

        w.Anklicken(3, false, false);
        Assert.Equal(new[] { 3 }, w.Gewaehlt);
        Assert.Equal(3, w.Anker);

        w.Anklicken(7, false, false);
        Assert.Equal(new[] { 7 }, w.Gewaehlt);
        Assert.Equal(7, w.Anker);
    }

    [Fact]
    public void Strg_nimmt_eine_Zeile_dazu_und_wieder_weg()
    {
        var w = new Zeilenmarkierung();

        w.Anklicken(2, false, false);
        w.Anklicken(5, true, false);
        w.Anklicken(9, true, false);
        Assert.Equal(new[] { 2, 5, 9 }, w.Gewaehlt);

        w.Anklicken(5, true, false);
        Assert.Equal(new[] { 2, 9 }, w.Gewaehlt);
        Assert.Equal(2, w.Anzahl);
        Assert.True(w.IstGewaehlt(9));
        Assert.False(w.IstGewaehlt(5));
    }

    [Fact]
    public void Umschalt_waehlt_den_Bereich_ab_dem_Anker_in_beide_Richtungen()
    {
        var w = new Zeilenmarkierung();

        w.Anklicken(4, false, false);
        w.Anklicken(7, false, true);
        Assert.Equal(new[] { 4, 5, 6, 7 }, w.Gewaehlt);

        // Der Anker bleibt stehen: ein zweiter Umschalt-Klick verkleinert.
        w.Anklicken(5, false, true);
        Assert.Equal(new[] { 4, 5 }, w.Gewaehlt);

        // ... und greift auch nach oben.
        w.Anklicken(1, false, true);
        Assert.Equal(new[] { 1, 2, 3, 4 }, w.Gewaehlt);
        Assert.Equal(4, w.Anker);
    }

    [Fact]
    public void Umschalt_ohne_Anker_verhaelt_sich_wie_ein_einfacher_Klick()
    {
        var w = new Zeilenmarkierung();

        w.Anklicken(6, false, true);

        Assert.Equal(new[] { 6 }, w.Gewaehlt);
        Assert.Equal(6, w.Anker);
    }

    [Fact]
    public void Alle_waehlen_und_leeren()
    {
        var w = new Zeilenmarkierung();

        w.AlleWaehlen(4);
        Assert.Equal(new[] { 0, 1, 2, 3 }, w.Gewaehlt);
        Assert.Equal(0, w.Anker);

        w.Leeren();
        Assert.Empty(w.Gewaehlt);
        Assert.Null(w.Anker);

        w.AlleWaehlen(0);
        Assert.Empty(w.Gewaehlt);
        Assert.Null(w.Anker);
    }

    /// <summary>
    /// Nach einem Filterwechsel wird die Anzeigeliste kuerzer: Was hinter ihr
    /// liegt, faellt aus der Markierung — sonst traefe die Uebernahme den
    /// falschen Satz. Dieselbe Zusage wie <c>VdiAuswahlFilter.QuellIndizes</c>.
    /// </summary>
    [Fact]
    public void Ein_Filterwechsel_wirft_Zeilen_hinter_der_neuen_Liste_hinaus()
    {
        var w = new Zeilenmarkierung();
        w.Anklicken(1, false, false);
        w.Anklicken(4, true, false);
        w.Anklicken(9, true, false);

        w.AufAnzahlBegrenzen(5);

        Assert.Equal(new[] { 1, 4 }, w.Gewaehlt);
        Assert.Null(w.Anker);          // der Anker 9 ist ungueltig geworden

        w.AufAnzahlBegrenzen(0);
        Assert.Empty(w.Gewaehlt);
    }

    /// <summary>
    /// Die Anzeigezeilen werden ueber die Zuordnung auf die Quellindizes
    /// abgebildet; eine veraltete Zeile bleibt ohne Wirkung.
    /// </summary>
    [Fact]
    public void QuellIndizes_bildet_nur_gueltige_Anzeigezeilen_ab()
    {
        var w = new Zeilenmarkierung();
        var anzeige = new List<int> { 7, 3, 11 };

        w.Anklicken(0, false, false);
        w.Anklicken(2, true, false);
        Assert.Equal(new[] { 7, 11 }, w.QuellIndizes(anzeige));

        w.Anklicken(99, true, false);
        Assert.Equal(new[] { 7, 11 }, w.QuellIndizes(anzeige));
        Assert.Empty(w.QuellIndizes(null));
    }

    [Fact]
    public void Ein_negativer_Index_wird_uebergangen()
    {
        var w = new Zeilenmarkierung();

        w.Anklicken(-1, false, false);

        Assert.Empty(w.Gewaehlt);
        Assert.Null(w.Anker);
    }
}

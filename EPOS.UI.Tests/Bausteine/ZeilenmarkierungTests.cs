using System.Collections.Generic;
using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Zeilenmarkierung (iU9-W13.0l) — die Markierungsregel aller sechs Importe.
///
/// <para><b>Seit W6‑E‑5 (07.09.2026) schaltet der einfache Klick um.</b> Vorbild
/// war bis dahin <c>SelectionMode.MultiExtended</c> der <c>ListBox</c>: ein Klick
/// ersetzte die Wahl, nur <c>Strg</c> nahm dazu. Die Wahlspalte zeigt aber ein
/// Kontrollkaestchen, und ein Kaestchen verspricht Umschalten — der Anwender
/// waehlte eine zweite Zeile und sah die erste verschwinden („die Mehrfachauswahl
/// funktioniert nicht"). Jetzt gilt: Klick schaltet um, <c>Strg</c> ebenso,
/// <c>Umschalt</c> nimmt den Bereich ab dem Anker DAZU.</para>
/// </summary>
public class ZeilenmarkierungTests
{
    /// <summary>
    /// <b>Der Kern von W6‑E‑5:</b> Zwei einfache Klicks wählen ZWEI Zeilen, ein
    /// dritter Klick auf dieselbe Zeile nimmt sie wieder weg.
    /// </summary>
    [Fact]
    public void Ein_einfacher_Klick_schaltet_die_Zeile_um()
    {
        var w = new Zeilenmarkierung();

        w.Anklicken(3, false, false);
        Assert.Equal(new[] { 3 }, w.Gewaehlt);
        Assert.Equal(3, w.Anker);

        w.Anklicken(7, false, false);
        Assert.Equal(new[] { 3, 7 }, w.Gewaehlt);
        Assert.Equal(7, w.Anker);

        w.Anklicken(3, false, false);
        Assert.Equal(new[] { 7 }, w.Gewaehlt);
        Assert.Equal(3, w.Anker);
    }

    /// <summary>
    /// <c>Hinzufuegen</c> nimmt die Zeile in die Wahl, ohne umzuschalten — der
    /// Doppelklick braucht das (der Browser schickt davor zwei Klicks, die sich
    /// gegenseitig aufheben), und der Filterwechsel stellt damit die gemerkte
    /// Wahl wieder her.
    /// </summary>
    [Fact]
    public void Hinzufuegen_schaltet_nicht_um()
    {
        var w = new Zeilenmarkierung();

        w.Hinzufuegen(2);
        w.Hinzufuegen(2);
        w.Hinzufuegen(5);

        Assert.Equal(new[] { 2, 5 }, w.Gewaehlt);
        Assert.Equal(5, w.Anker);

        w.Hinzufuegen(-1);
        Assert.Equal(new[] { 2, 5 }, w.Gewaehlt);
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

    /// <summary>
    /// Umschalt nimmt den Bereich ab dem Anker DAZU — es leert die Wahl nicht mehr
    /// (W6‑E‑5). Der Anker bleibt stehen, so dass sich der Bereich mit weiteren
    /// Umschalt-Klicks in beide Richtungen erweitern lässt.
    /// </summary>
    [Fact]
    public void Umschalt_nimmt_den_Bereich_ab_dem_Anker_dazu()
    {
        var w = new Zeilenmarkierung();

        w.Anklicken(4, false, false);
        w.Anklicken(7, false, true);
        Assert.Equal(new[] { 4, 5, 6, 7 }, w.Gewaehlt);

        // Der Anker bleibt stehen - und der Bereich waechst nach oben.
        w.Anklicken(1, false, true);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, w.Gewaehlt);
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
        Assert.Empty(w.QuellIndizes(null!));
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

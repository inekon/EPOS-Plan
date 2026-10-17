using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// <b>„Vergleichen…" der Ganglinien-Projektdialoge</b> (KA-E-19, Strom-, Solar- und PV-Ganglinie): Der Knopf steht
/// vorn in der Knopfleiste am Satz, ist unter zwei angekreuzten Sätzen im Bereich des gewählten Satzes gesperrt
/// (Ankreuzen wählt nicht, KA-E-17) und vergleicht beim Projektsatz die angekreuzten Projektzeilen mit ihren
/// Kennzahlen, beim Katalogsatz die angekreuzten Katalogsätze.
/// </summary>
internal static class GanglinienVergleichPruefung
{
    private const string PROJEKT = ".epos-zweispalten-bereich--projekt";
    private const string KATALOG = ".epos-zweispalten-bereich--katalog";

    internal static IElement Knopf<T>(IRenderedComponent<T> cut) where T : IComponent
        => cut.Find(".epos-zweispalten-satzknoepfe .epos-knopf--vergleichen");

    /// <summary>Vergleichen… steht als erster Knopf der Leiste am Satz.</summary>
    internal static void StehtVorn<T>(IRenderedComponent<T> cut) where T : IComponent
    {
        IElement erster = cut.Find(".epos-zweispalten-satzknoepfe").QuerySelectorAll("button")[0];
        Assert.Contains("epos-knopf--vergleichen", erster.ClassName ?? "");
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.AUSWAHL_BTN_VERGLEICHEN, erster.TextContent.Trim());
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.AUSWAHL_BTN_VERGLEICHEN_HINWEIS, erster.GetAttribute("title"));
    }

    /// <summary>
    /// Projektseite (<paramref name="liste"/>: die Katalogliste des Dialogs, sie zeigt den Vergleich): erste Projektzeile wählen, zwei Projektzeilen ankreuzen (gesperrt bis zur zweiten), vergleichen —
    /// die Zeile <paramref name="beschriftung"/> des Vergleichs trägt <paramref name="erwartet"/>; der Satz bleibt.
    /// </summary>
    internal static void Projektseite<T>(IRenderedComponent<T> cut, Func<Katalogliste> liste, string beschriftung,
                                         params string[] erwartet)
        where T : IComponent
    {
        KnopfleisteAmSatzPruefung.Projektzeilen(cut)[0].Click();
        Assert.True(Knopf(cut).HasAttribute("disabled"));

        cut.Find(PROJEKT).QuerySelectorAll("td .epos-wahlkaestchen")[0].Click();
        Assert.True(Knopf(cut).HasAttribute("disabled"));
        cut.Find(PROJEKT).QuerySelectorAll("td .epos-wahlkaestchen")[1].Click();
        Assert.False(Knopf(cut).HasAttribute("disabled"));
        // Ankreuzen waehlt nicht (KA-E-17): die erste Zeile bleibt der gewaehlte Satz.
        Assert.Equal("true", KnopfleisteAmSatzPruefung.Projektzeilen(cut)[0].GetAttribute("aria-current"));

        Knopf(cut).Click();
        cut.WaitForAssertion(() => Assert.True(liste().VergleichOffen));
        Katalogliste.Vergleichzeile zeile = liste().Vergleich.Single(z => z.Name == beschriftung);
        Assert.Equal(erwartet, zeile.Werte);
    }

    /// <summary>
    /// Katalogseite (<paramref name="liste"/>: die Katalogliste des Dialogs): ersten Katalogsatz wählen, zwei
    /// Katalogsätze ankreuzen (gesperrt bis zum zweiten), vergleichen.
    /// </summary>
    internal static void Katalogseite<T>(IRenderedComponent<T> cut, Func<Katalogliste> liste) where T : IComponent
    {
        cut.Find(KATALOG).QuerySelectorAll("tbody tr .epos-zeilenzelle--name")[0].Click();
        Assert.True(Knopf(cut).HasAttribute("disabled"));

        cut.Find(KATALOG).QuerySelectorAll("td .epos-kaestchenzelle input")[0].Change(true);
        Assert.True(Knopf(cut).HasAttribute("disabled"));
        cut.Find(KATALOG).QuerySelectorAll("td .epos-kaestchenzelle input")[1].Change(true);
        Assert.False(Knopf(cut).HasAttribute("disabled"));

        Knopf(cut).Click();
        cut.WaitForAssertion(() => Assert.True(liste().VergleichOffen));
        Assert.NotEmpty(liste().Vergleich);
    }
}

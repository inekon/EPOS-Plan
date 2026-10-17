using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Das Ablegen des erzeugten Profils im Dialog „Brauchwasser-Zapfprofil" (Konzept Zapfprofil
/// Bereiche und Methoden, ZC6): die zwei Knöpfe der Schlussleiste, ihre weiche Sperre ohne
/// Ergebnis, die Überlagerung mit Vorgaben und Schalter „mit Zirkulation", die Warnung vor dem
/// Projektweg, Erfolgs- und Ablehnungsmeldung. Die drei Wege der Hülle sind Prüfdelegaten.
/// Kultur de-DE (Hausvorrichtung).
/// </summary>
public partial class ZapfprofilDialogTests
{
    private const string WARNUNG = "Das Profil steht dann nur in diesem Projekt, nicht im Katalog.";
    private const string DOPPELT = "Das Projekt rechnet auf dem Bestandsweg bereits eine Zirkulation.";

    /// <summary>Die Prüfvorschau des Profilsatzes zum Schalter „mit Zirkulation" (erfunden).</summary>
    private static ZapfprofilProfilsatzVorschauDaten Profilsatz(bool mitZirkulation, bool bestandZirkulation = false) => new()
    {
        Gerechnet = true,
        MitZirkulation = mitZirkulation,
        JahressummeMwh = mitZirkulation ? 12.5 : 10.0,
        SpitzenstundeText = mitZirkulation ? "Mo 07–08 Uhr: 3,2 kW" : "Mo 07–08 Uhr: 2,8 kW",
        WarnungProjekt = WARNUNG,
        BestandswegHatZirkulation = bestandZirkulation,
        HinweisZirkulationDoppelt = bestandZirkulation ? DOPPELT : "",
        NameProjekt = "Zapfprofil Beispielprojekt",
        Beschreibung = mitZirkulation ? "Aus dem Zapfprofilgenerator (mit)" : "Aus dem Zapfprofilgenerator (ohne)"
    };

    /// <summary>Ein Aufruf eines Schreibwegs, wie ihn der Prüfdelegat festhält.</summary>
    private sealed record Ablageaufruf(string Name, string Typ, string Beschreibung, bool MitZirkulation);

    private sealed class Ablagefakes
    {
        public bool BestandZirkulation;
        public bool Ablehnen;
        public int Vorschauen;
        public readonly List<Ablageaufruf> Katalog = new();
        public readonly List<bool> Projekt = new();
        public readonly List<ZapfprofilProfilsatzErgebnis> Gemeldet = new();
    }

    private IRenderedComponent<ZapfprofilDialog> AufbauenAblage(Ablagefakes f, ZapfprofilDaten? daten = null)
        => Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, daten ?? Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, e => Vorschau(e))
            .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.ProfilsatzVorschau, (_, mz) => { f.Vorschauen++; return Profilsatz(mz, f.BestandZirkulation); })
            .Add(x => x.ProfilsatzKatalog, (_, name, typ, beschreibung, mz) =>
            {
                f.Katalog.Add(new Ablageaufruf(name, typ, beschreibung, mz));
                return f.Ablehnen
                    ? new ZapfprofilProfilsatzErgebnis { Erfolg = false, Kennung = "ZPG_PS_MSG_NAME_VORHANDEN", Text = $"Ein Katalogsatz „{name}“ ist schon vorhanden." }
                    : new ZapfprofilProfilsatzErgebnis { Erfolg = true, Id = 99, Name = name, Kennung = "ZPG_PS_MSG_KATALOG_GESPEICHERT", Text = $"Der Katalogsatz „{name}“ ist gespeichert." };
            })
            .Add(x => x.ProfilsatzProjekt, (_, mz) =>
            {
                f.Projekt.Add(mz);
                return new ZapfprofilProfilsatzErgebnis { Erfolg = true, Id = 501, Name = "Zapfprofil Beispielprojekt", Kennung = "ZPG_PS_MSG_PROJEKT_HINZUGEFUEGT", Text = "„Zapfprofil Beispielprojekt“ steht als Brauchwasserprofil im Projekt." };
            })
            .Add(x => x.ProjektzeileAngelegt, e => f.Gemeldet.Add(e))
            .Add(x => x.Geschlossen, _ => { }));

    private static IElement KatalogKnopf(IRenderedComponent<ZapfprofilDialog> cut) => cut.Find("button.epos-zapfprofil-ablage-katalog");
    private static IElement ProjektKnopf(IRenderedComponent<ZapfprofilDialog> cut) => cut.Find("button.epos-zapfprofil-ablage-projekt");
    private static IElement? Ablage(IRenderedComponent<ZapfprofilDialog> cut) => cut.FindAll(".epos-zapfprofil-ablage").FirstOrDefault();

    [Fact]
    public void Die_Knoepfe_stehen_vor_Abbrechen_und_OK_samt_leiser_Zeile_zum_Rechenweg()
    {
        var cut = AufbauenAblage(new Ablagefakes());

        IElement fuss = cut.FindAll(".epos-leiste").Last();
        string[] knoepfe = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToArray();
        Assert.Equal(new[] { "Als Katalogsatz speichern…", "Zum Projekt hinzufügen", "Abbrechen", "OK" }, knoepfe[^4..]);
        Assert.Null(KatalogKnopf(cut).GetAttribute("aria-disabled"));
        // Die Prüfdaten stehen auf dem Bestandsweg.
        Assert.Contains("gerade über die Bestandsprofile", cut.Find(".epos-zapfprofil-ablage-weg").TextContent);
    }

    [Fact]
    public void Ohne_Delegaten_gibt_es_keine_Knoepfe()
    {
        var cut = Aufbauen();
        Assert.Empty(cut.FindAll("button.epos-zapfprofil-ablage-katalog"));
        Assert.Empty(cut.FindAll("button.epos-zapfprofil-ablage-projekt"));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-ablage-weg"));
    }

    [Fact]
    public void Ohne_Ergebnis_sind_die_Knoepfe_weich_gesperrt_und_melden_den_Grund()
    {
        var f = new Ablagefakes();
        var cut = AufbauenAblage(f, Daten(vorschau: new ZapfprofilVorschauDaten { Zustand = ZapfprofilVorschauZustand.NichtGerechnet }));

        foreach (IElement knopf in new[] { KatalogKnopf(cut), ProjektKnopf(cut) })
        {
            Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
            Assert.False(knopf.HasAttribute("disabled"));
            Assert.Equal("Es ist kein Zapfprofil gerechnet — ohne Ergebnis kein Profilsatz.", knopf.GetAttribute("title"));
        }
        KatalogKnopf(cut).Click();
        Assert.Null(Ablage(cut));
        Assert.Equal(0, f.Vorschauen);
        Assert.Contains("ohne Ergebnis kein Profilsatz", cut.Find(".epos-zapfprofil-hinweis").TextContent);
    }

    [Fact]
    public void Eine_Fehleingabe_sperrt_die_Knoepfe()
    {
        var cut = AufbauenAblage(new Ablagefakes());
        Feld(cut, "Bezugsgröße, Bereich 1").Input("abc");

        cut.WaitForAssertion(() => Assert.Equal("true", KatalogKnopf(cut).GetAttribute("aria-disabled")));
        Assert.StartsWith("Erst die Fehleingaben beheben", ProjektKnopf(cut).GetAttribute("title"));
    }

    [Fact]
    public void Das_Katalogblatt_zeigt_Vorgaben_Schalter_und_Zusammenfassung()
    {
        var f = new Ablagefakes();
        var cut = AufbauenAblage(f);
        KatalogKnopf(cut).Click();

        Assert.NotNull(Ablage(cut));
        Assert.Contains("Als Katalogsatz speichern", cut.Find(".epos-ueberlagerung").TextContent);
        Assert.Equal("Zapfprofil Beispielprojekt", Feld(cut, "Name").GetAttribute("value"));
        Assert.Equal("Zapfprofil Beispielprojekt", Feld(cut, "Typ (Wochenprofil)").GetAttribute("value"));
        Assert.Equal("Aus dem Zapfprofilgenerator (mit)", Feld(cut, "Beschreibung").GetAttribute("value"));
        Assert.True(Feld(cut, "mit Zirkulation").HasAttribute("checked"));
        Assert.Empty(cut.FindAll(".epos-zapfprofil-ablage-zirkulation"));
        Assert.DoesNotContain(WARNUNG, Ablage(cut)!.TextContent);
        string summe = cut.Find(".epos-zapfprofil-ablage-summe").TextContent;
        Assert.Contains("12,5 MWh", summe);
        Assert.Contains("Mo 07–08 Uhr: 3,2 kW", summe);

        // Der Schalter rechnet die Zusammenfassung neu; die unberührte Beschreibung folgt.
        Feld(cut, "mit Zirkulation").Change(false);
        Assert.Contains("10,0 MWh", cut.Find(".epos-zapfprofil-ablage-summe").TextContent);
        Assert.Equal("Aus dem Zapfprofilgenerator (ohne)", Feld(cut, "Beschreibung").GetAttribute("value"));
    }

    [Fact]
    public void Bei_Bestandszirkulation_steht_der_Schalter_aus_mit_Hinweis()
    {
        var cut = AufbauenAblage(new Ablagefakes { BestandZirkulation = true });
        KatalogKnopf(cut).Click();

        Assert.False(Feld(cut, "mit Zirkulation").HasAttribute("checked"));
        Assert.Contains(DOPPELT, cut.Find(".epos-zapfprofil-ablage-zirkulation").TextContent);
        Assert.Contains("10,0 MWh", cut.Find(".epos-zapfprofil-ablage-summe").TextContent);
    }

    [Fact]
    public void Speichern_ruft_den_Weg_mit_den_Feldwerten_und_meldet_den_Erfolg()
    {
        var f = new Ablagefakes();
        var cut = AufbauenAblage(f);
        KatalogKnopf(cut).Click();

        Feld(cut, "Name").Input("Mein Profil");
        Assert.Equal("Mein Profil", Feld(cut, "Typ (Wochenprofil)").GetAttribute("value"));   // der Typ folgt dem Namen
        Feld(cut, "Beschreibung").Input("Eigene Beschreibung");
        Feld(cut, "mit Zirkulation").Change(false);
        cut.Find("button.epos-zapfprofil-ablage-speichern").Click();

        Assert.Equal(new Ablageaufruf("Mein Profil", "Mein Profil", "Eigene Beschreibung", false), Assert.Single(f.Katalog));
        Assert.Null(Ablage(cut));
        Assert.Contains("Der Katalogsatz „Mein Profil“ ist gespeichert.", cut.Markup);
        Assert.Empty(f.Projekt);
        Assert.Empty(f.Gemeldet);
    }

    [Fact]
    public void Eine_Ablehnung_bleibt_in_der_Ablage_stehen()
    {
        var f = new Ablagefakes { Ablehnen = true };
        var cut = AufbauenAblage(f);
        KatalogKnopf(cut).Click();
        cut.Find("button.epos-zapfprofil-ablage-speichern").Click();

        Assert.NotNull(Ablage(cut));
        Assert.Contains("Ein Katalogsatz „Zapfprofil Beispielprojekt“ ist schon vorhanden.",
                        Ablage(cut)!.QuerySelector(".epos-warnbanner")!.TextContent);

        // Abbrechen schließt ohne zweiten Aufruf.
        cut.Find("button.epos-zapfprofil-ablage-abbrechen").Click();
        Assert.Null(Ablage(cut));
        Assert.Single(f.Katalog);
    }

    [Fact]
    public void Der_Projektweg_zeigt_erst_die_Warnung_dann_den_Weg()
    {
        var f = new Ablagefakes { BestandZirkulation = true };
        var cut = AufbauenAblage(f);
        ProjektKnopf(cut).Click();

        // Erst die Warnung samt Schalter und Hinweis - geschrieben ist noch nichts.
        Assert.Empty(f.Projekt);
        IElement ablage = Ablage(cut)!;
        Assert.Contains(WARNUNG, ablage.QuerySelector(".epos-warnbanner")!.TextContent);
        Assert.Contains(DOPPELT, ablage.TextContent);
        Assert.False(Feld(cut, "mit Zirkulation").HasAttribute("checked"));
        Assert.Empty(ablage.QuerySelectorAll("input[type=text]"));   // kein Name: den vergibt die Hülle

        cut.Find("button.epos-zapfprofil-ablage-weiter").Click();

        Assert.False(Assert.Single(f.Projekt));
        Assert.Equal(501, Assert.Single(f.Gemeldet).Id);
        Assert.Null(Ablage(cut));
        Assert.Contains("steht als Brauchwasserprofil im Projekt.", cut.Markup);
        Assert.Empty(f.Katalog);
    }

    [Fact]
    public void Esc_schliesst_bei_offener_Ablage_nicht_den_Dialog()
    {
        bool geschlossen = false;
        var f = new Ablagefakes();
        var cut = Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, Daten())
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, e => Vorschau(e))
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.ProfilsatzVorschau, (_, mz) => Profilsatz(mz))
            .Add(x => x.ProfilsatzProjekt, (_, _) => new ZapfprofilProfilsatzErgebnis())
            .Add(x => x.Geschlossen, _ => geschlossen = true));
        ProjektKnopf(cut).Click();
        Assert.Empty(cut.FindAll("button.epos-zapfprofil-ablage-katalog"));   // kein Delegat, kein Knopf

        cut.Find(".epos-zapfprofil").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
        Assert.False(geschlossen);
    }
}

using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Block der Nutzungsbereiche (Konzept Zapfprofil — Bereiche und Methoden, ZC2): eine Tabelle, deren
/// Zeilen Nutzungsart, Bezugsgröße und Bedarfsniveau direkt tragen, darunter die Detailzeile der markierten
/// Zeile mit Name, Herleitung, nächstem Schritt und Meldungen; die Karte „Nutzung" entfällt.
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    private static IElement[] Bereichzeilen(IRenderedComponent<ZapfprofilDialog> cut)
        => cut.FindAll("table.epos-zapfprofil-zonen tbody tr.epos-zapfprofil-bereich").ToArray();

    private static string[] Beschriftungen(IElement zeile)
        => zeile.QuerySelectorAll(".epos-feld-text").Select(t => t.TextContent.Trim()).ToArray();

    [Fact]
    public void ZC2_Jede_Zeile_bearbeitet_Nutzungsart_Bezugsgroesse_und_Niveau_direkt()
    {
        var cut = AufbauenGefuehrt();

        // Die zweite Zeile ist nicht markiert und trägt trotzdem ihre drei Felder, benannt nach Spalte und Bereich.
        IElement zweite = Bereichzeilen(cut)[1];
        Assert.DoesNotContain("epos-zeile--markiert", zweite.ClassName ?? "");
        Assert.Equal(new[] { "Nutzungsart, Bereich 2", "Bezugsgröße, Bereich 2", "Bedarfsniveau, Bereich 2" }, Beschriftungen(zweite));

        Feld(cut, "Bezugsgröße, Bereich 2").Input("12,5");
        Assert.Equal(12.5, cut.Instance.Eingabe.Zonen[1].Bezugsmenge);
        Assert.Equal(20, cut.Instance.Eingabe.Zonen[0].Bezugsmenge);
        // Wer in einer Zeile arbeitet, markiert sie: Detailzeile und Karten folgen ihr.
        Assert.Contains("epos-zeile--markiert", Bereichzeilen(cut)[1].ClassName ?? "");

        Auswahl(cut, "Bedarfsniveau, Bereich 2").Change("3");
        Assert.Equal(ZapfprofilNiveau.Hoch, cut.Instance.Eingabe.Zonen[1].Niveau);

        Auswahl(cut, "Nutzungsart, Bereich 1").Change("2");
        Assert.Equal(2, cut.Instance.Eingabe.Zonen[0].IdNutzungsart);
        Assert.Contains("epos-zeile--markiert", Bereichzeilen(cut)[0].ClassName ?? "");

        // Die Klappliste nennt die Bezugsart im Eintrag; das Niveau ist eine Klappliste niedrig/mittel/hoch.
        Assert.Contains("Wohnen A · Personen", Auswahl(cut, "Nutzungsart, Bereich 1").TextContent);
        Assert.Equal(new[] { "niedrig", "mittel", "hoch" },
                     Auswahl(cut, "Bedarfsniveau, Bereich 1").QuerySelectorAll("option").Select(o => o.TextContent.Trim()).ToArray());
    }

    [Fact]
    public void ZC2_Die_Detailzeile_steht_nur_unter_der_markierten_Zeile()
    {
        var cut = AufbauenGefuehrt();

        IElement detail = cut.Find("table.epos-zapfprofil-zonen tbody tr.epos-zapfprofil-bereichdetail");
        Assert.Single(cut.FindAll("tr.epos-zapfprofil-bereichdetail"));
        Assert.Same(Bereichzeilen(cut)[0], detail.PreviousElementSibling);
        Assert.Equal("5", detail.QuerySelector("td")!.GetAttribute("colspan"));
        Assert.Equal("Wohnen", Feld(cut, "Zonenname").GetAttribute("value"));
        // Einfach: kein „Katalog…" in der Detailzeile, die Stufe Experte bringt ihn.
        Assert.Empty(detail.QuerySelectorAll(".epos-zapfprofil-katalogknopf"));

        // Die Wahl der zweiten Zeile nimmt die Detailzeile mit.
        Bereichzeilen(cut)[1].QuerySelector("button")!.Click();
        detail = cut.Find("tr.epos-zapfprofil-bereichdetail");
        Assert.Single(cut.FindAll("tr.epos-zapfprofil-bereichdetail"));
        Assert.Same(Bereichzeilen(cut)[1], detail.PreviousElementSibling);
        Assert.Equal("Büro", Feld(cut, "Zonenname").GetAttribute("value"));

        // Der Name ändert sich in der Detailzeile; die Zeile selbst trägt keinen Namen (ZC-Q3 B).
        Feld(cut, "Zonenname").Input("Büro Nord");
        Assert.Equal("Büro Nord", cut.Instance.Eingabe.Zonen[1].Name);
        Assert.DoesNotContain("Büro Nord", Bereichzeilen(cut)[1].TextContent);

        Stufe(cut, "Experte", aufklappen: false);
        Assert.NotNull(cut.Find("tr.epos-zapfprofil-bereichdetail .epos-zapfprofil-katalogknopf"));
    }

    [Fact]
    public void ZC2_Hinzufuegen_markiert_die_neue_unvollstaendige_Zeile_und_setzt_den_Fokus_in_ihre_Nutzungsart()
    {
        var sprung = JSInterop.SetupModule("./_content/EPOS.UI/epos-sprung.js");
        sprung.SetupVoid("feldFokus", _ => true);
        var cut = AufbauenGefuehrt();

        Knopf(cut, "Zone hinzufügen").Click();

        IElement neue = Bereichzeilen(cut)[2];
        Assert.Contains("epos-zeile--markiert", neue.ClassName ?? "");
        Assert.Equal("Bereich 3", cut.Instance.Eingabe.Zonen[2].Name);
        Assert.Equal("unvollständig", neue.QuerySelector("td.epos-zapfprofil-unvollstaendig")!.TextContent.Trim());
        // Die neue Zeile ist leer: keine Nutzungsart, keine Bezugsgröße.
        Assert.Equal("", Auswahl(cut, "Nutzungsart, Bereich 3").GetAttribute("value") ?? "");
        Assert.Equal("", Feld(cut, "Bezugsgröße, Bereich 3").GetAttribute("value") ?? "");
        Assert.Equal("Zone „Bereich 3“: Bitte eine Nutzungsart wählen.",
                     cut.Find("tr.epos-zapfprofil-bereichdetail .epos-zapfprofil-naechsterschritt").TextContent.Trim());

        // Der Fokus geht in die Klappliste Nutzungsart der MARKIERTEN Zeile — der neuen.
        JSRuntimeInvocation aufruf = sprung.VerifyInvoke("feldFokus");
        Assert.Equal("tr.epos-zeile--markiert select:not([disabled])", aufruf.Arguments[1]);
        Assert.Same(neue.QuerySelector("select"), neue.QuerySelectorAll("select:not([disabled])").First());
    }

    [Fact]
    public void ZC2_Eine_Fehleingabe_der_Bezugsgroesse_haelt_das_OK_und_nennt_ihre_Zeile()
    {
        var cut = AufbauenGefuehrt();

        Feld(cut, "Bezugsgröße, Bereich 2").Input("zwölf");
        Assert.Contains("epos-fehleingabe", Feld(cut, "Bezugsgröße, Bereich 2").ClassName ?? "");
        Knopf(cut, "OK").Click();
        Assert.Contains("Bezugsgröße, Bereich 2", cut.Instance.OkMeldung);

        // Die Zeile davor wird entfernt: Die Meldung wandert mit ihrer Zeile, die jetzt die erste ist.
        Bereichzeilen(cut)[0].QuerySelector("button")!.Click();
        Knopf(cut, "Entfernen").Click();
        Knopf(cut, "OK").Click();
        Assert.Contains("Bezugsgröße, Bereich 1", cut.Instance.OkMeldung);

        // Ein gültiger Wert hebt die Sperre auf.
        Feld(cut, "Bezugsgröße, Bereich 1").Input("12");
        Assert.DoesNotContain("epos-fehleingabe", Feld(cut, "Bezugsgröße, Bereich 1").ClassName ?? "");
        Knopf(cut, "OK").Click();
        Assert.DoesNotContain("Bezugsgröße", cut.Instance.OkMeldung);
    }

    [Fact]
    public void ZC2_Bei_Wohnungstabelle_steht_die_wirksame_Bezugsgroesse_grau_und_nicht_editierbar()
    {
        var cut = Aufbauen();
        Stufe(cut, "Experte");
        Knopf(cut, "Wohnungstyp hinzufügen").Click();
        Feld(cut, "Anzahl, Zeile 1").Input("4");
        Feld(cut, "Personen je Wohnung, Zeile 1").Input("2");

        IElement erste = Bereichzeilen(cut)[0];
        IElement wirksam = erste.QuerySelector(".epos-zapfprofil-wirksam")!;
        Assert.NotNull(wirksam);
        Assert.Empty(erste.QuerySelectorAll("input"));
        Assert.Contains("Wohnungstabelle", wirksam.GetAttribute("title") ?? "");
        // Die Herleitung steht zusätzlich in der Detailzeile.
        Assert.NotNull(cut.Find("tr.epos-zapfprofil-bereichdetail .epos-herleitung"));
    }
}

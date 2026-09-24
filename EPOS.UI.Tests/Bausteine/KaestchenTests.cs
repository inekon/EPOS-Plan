using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Die Kästchen der Mehrfachwahl</b> in der <see cref="Katalogliste"/> — Konzept
/// Administrationsdialoge, Stufe 3, Vorschlag V6 — und der Zustand der Auswahl im Wirt
/// (<see cref="Zeilenauswahl"/>).
///
/// <para>Geprüft: Die Kästchenspalte steht nur mit <c>ZeileIstWahl</c>; Kästchen,
/// Leertaste und Strg-Klick schalten dieselbe Wahl, die Fokuszeile bleibt davon
/// unberührt; das Kopfkästchen wählt alle SICHTBAREN; eine neue Gabe des Wirts zieht die
/// Liste nach, die eigene erkennt sie wieder; der Vergleichsknopf der Suchzeile entfällt.
/// Ohne den Schalter bleibt alles, wie es war.</para>
/// </summary>
public class KaestchenTests : EposBunitContext
{
    public KaestchenTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static Katalogfilterprofil Profil() =>
        Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => s);

    private static List<Katalogfilterzeile> Zeilen() => new()
    {
        new Katalogfilterzeile(1, "Alpha").MitText(Katalogfilterprofil.SpBezeichner, "Alpha")
            .MitText(Katalogfilterprofil.SpHersteller, "Vaillant").MitZahl(Katalogfilterprofil.SpPtherm, 15.0),
        new Katalogfilterzeile(2, "Beta").MitText(Katalogfilterprofil.SpBezeichner, "Beta")
            .MitText(Katalogfilterprofil.SpHersteller, "Buderus").MitZahl(Katalogfilterprofil.SpPtherm, 80.0),
        new Katalogfilterzeile(3, "Gamma").MitText(Katalogfilterprofil.SpBezeichner, "Gamma")
            .MitText(Katalogfilterprofil.SpHersteller, "Vaillant").MitZahl(Katalogfilterprofil.SpPtherm, 40.0)
    };

    /// <summary>
    /// Ein Wirt wie die Verwaltungen: Was die Liste meldet, hält er in einer
    /// <see cref="Zeilenauswahl"/> und reicht es beim nächsten Zeichnen zurück — ein
    /// Prüfschritt, der neu zeichnet, gibt deshalb <c>Gewaehlte</c> immer mit.
    /// </summary>
    private sealed class Wirt
    {
        public readonly Zeilenauswahl Auswahl = new();
        public string Fokus = "Alpha";
        public int Meldungen;
    }

    private IRenderedComponent<Katalogliste> Aufbauen(Wirt wirt, bool kaestchen = true,
                                                      bool zeileIstWahl = true,
                                                      Katalogfilterstand? stand = null)
    {
        IRenderedComponent<Katalogliste>? cut = null;
        cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, stand ?? new Katalogfilterstand())
            .Add(x => x.Gewaehlt, wirt.Fokus)
            .Add(x => x.ZeileIstWahl, zeileIstWahl)
            .Add(x => x.Kaestchen, kaestchen)
            .Add(x => x.Gewaehlte, wirt.Auswahl.Gewaehlte)
            .Add(x => x.GewaehltChanged, EventCallback.Factory.Create<string>(this, w => wirt.Fokus = w))
            .Add(x => x.GewaehlteChanged, EventCallback.Factory.Create<IReadOnlyList<string>>(this, g =>
            {
                wirt.Meldungen++;
                wirt.Auswahl.Setzen(g);
            })));
        return cut;
    }

    private static IReadOnlyList<AngleSharp.Dom.IElement> Kaestchen(IRenderedComponent<Katalogliste> cut)
        => cut.FindAll("tbody td.epos-spalte-kaestchen input");

    /// <summary>Die Kästchenspalte steht vorn, je Zeile ein Kästchen mit dem Namen der Zeile.</summary>
    [Fact]
    public void Die_Kaestchenspalte_steht_vorn()
    {
        var cut = Aufbauen(new Wirt());

        var koepfe = cut.FindAll("thead th");
        Assert.Contains("epos-spalte-kaestchen", koepfe[0].ClassName ?? "");
        Assert.Equal("Alle sichtbaren wählen", koepfe[0].QuerySelector("input")!.GetAttribute("aria-label"));
        Assert.Equal(3, Kaestchen(cut).Count);
        Assert.Equal("Alpha wählen", Kaestchen(cut)[0].GetAttribute("aria-label"));
        Assert.Contains("epos-katalogliste--kaestchen", cut.Find(".epos-katalogliste").ClassName ?? "");

        // Der Vergleichsknopf der Suchzeile entfaellt - er steht in der Auswahlleiste.
        Assert.Empty(cut.FindAll(".epos-katalog-vergleichknopf"));
        // Die Kaestchenzelle ist keine Klickflaeche der Zeile.
        Assert.Null(cut.Find("tbody td.epos-spalte-kaestchen").QuerySelector(".epos-zeilenzelle"));
    }

    /// <summary>
    /// <b>Das Kästchen wählt, die Fokuszeile bleibt</b>: Ein Klick auf das Kästchen meldet
    /// die neue Liste, die gewählte Zeile trägt <c>epos-zeile--markiert</c>, und die Wahl
    /// der Fokuszeile wird nicht gerufen.
    /// </summary>
    [Fact]
    public void Das_Kaestchen_waehlt_ohne_die_Fokuszeile_zu_wechseln()
    {
        var wirt = new Wirt();
        var cut = Aufbauen(wirt);

        Kaestchen(cut)[1].Change(true);
        Kaestchen(cut)[2].Change(true);

        Assert.Equal(new[] { "Beta", "Gamma" }, wirt.Auswahl.Gewaehlte);
        Assert.Equal("Alpha", wirt.Fokus);
        var zeilen = cut.FindAll("tbody tr");
        Assert.Contains("epos-zeile--markiert", zeilen[1].ClassName ?? "");
        Assert.DoesNotContain("epos-zeile--markiert", zeilen[0].ClassName ?? "");
        Assert.True(Kaestchen(cut)[1].HasAttribute("checked"));

        Kaestchen(cut)[1].Change(false);
        Assert.Equal(new[] { "Gamma" }, wirt.Auswahl.Gewaehlte);
    }

    /// <summary><b>Die Leertaste</b> setzt das Kästchen der Fokuszeile — und nimmt es wieder weg.</summary>
    [Fact]
    public void Die_Leertaste_schaltet_das_Kaestchen_der_Fokuszeile()
    {
        var wirt = new Wirt { Fokus = "Beta" };
        var cut = Aufbauen(wirt);

        cut.Find(".epos-raster-huelle").KeyDown(new KeyboardEventArgs { Key = " " });
        Assert.Equal(new[] { "Beta" }, wirt.Auswahl.Gewaehlte);

        cut.Find(".epos-raster-huelle").KeyDown(new KeyboardEventArgs { Key = " " });
        Assert.Empty(wirt.Auswahl.Gewaehlte);
        Assert.Equal("Beta", wirt.Fokus);
    }

    /// <summary>
    /// <b>Das Kästchen hält seine Tasten bei sich</b>: Ohne <c>stopPropagation</c>
    /// schaltete eine Leertaste auf dem fokussierten Kästchen es zweimal — einmal im
    /// Browser, einmal in der Tastenführung der Liste.
    /// </summary>
    [Fact]
    public void Das_Kaestchen_haelt_seine_Tasten_bei_sich()
    {
        var cut = Aufbauen(new Wirt());

        string markup = cut.Find("tbody td.epos-spalte-kaestchen").InnerHtml.ToLowerInvariant();
        Assert.Contains("onkeydown:stoppropagation", markup, StringComparison.Ordinal);
    }

    /// <summary><b>Strg-Klick</b> in die Zeile schaltet dasselbe Kästchen — ohne Grenze von drei.</summary>
    [Fact]
    public void Strg_Klick_schaltet_das_Kaestchen_ohne_Grenze()
    {
        var wirt = new Wirt();
        var cut = Aufbauen(wirt);

        for (int i = 0; i < 3; i++)
            cut.FindAll("tbody .epos-zeilenzelle--name")[i].Click(new MouseEventArgs { CtrlKey = true });
        Assert.Equal(3, wirt.Auswahl.Anzahl);

        // Ein blanker Klick waehlt weiter die Fokuszeile.
        cut.FindAll("tbody .epos-zeilenzelle--name")[2].Click();
        Assert.Equal("Gamma", wirt.Fokus);
        Assert.Equal(3, wirt.Auswahl.Anzahl);
    }

    /// <summary>
    /// <b>Das Kopfkästchen wählt alle SICHTBAREN</b> — gefiltert nur die gefilterten; ein
    /// zweiter Klick nimmt sie weg, Gewählte außerhalb des Filters bleiben.
    /// </summary>
    [Fact]
    public void Das_Kopfkaestchen_waehlt_alle_sichtbaren()
    {
        var wirt = new Wirt();
        var stand = new Katalogfilterstand();
        var cut = Aufbauen(wirt, stand: stand);

        Kaestchen(cut)[1].Change(true);                        // Beta (Buderus)
        stand.Setzen(Katalogfilterprofil.SpHersteller, "Vaillant");
        cut.Render(p => p.Add(x => x.Filterstand, stand).Add(x => x.Gewaehlte, wirt.Auswahl.Gewaehlte));
        Assert.Equal(2, cut.FindAll("tbody tr").Count);

        cut.Find("thead th.epos-spalte-kaestchen input").Change(true);
        Assert.Equal(new[] { "Beta", "Alpha", "Gamma" }, wirt.Auswahl.Gewaehlte);

        cut.Find("thead th.epos-spalte-kaestchen input").Change(false);
        Assert.Equal(new[] { "Beta" }, wirt.Auswahl.Gewaehlte);
    }

    /// <summary>
    /// <b>Eine neue Gabe des Wirts zieht die Liste nach</b> („Auswahl aufheben"): Die
    /// Kästchen fallen, ohne dass die Liste etwas meldet.
    /// </summary>
    [Fact]
    public void Eine_neue_Gabe_zieht_die_Liste_nach()
    {
        var wirt = new Wirt();
        var cut = Aufbauen(wirt);
        Kaestchen(cut)[0].Change(true);
        int meldungen = wirt.Meldungen;

        wirt.Auswahl.Aufheben();
        cut.Render(p => p.Add(x => x.Gewaehlte, wirt.Auswahl.Gewaehlte));

        Assert.All(Kaestchen(cut), k => Assert.False(k.HasAttribute("checked")));
        Assert.Empty(cut.Instance.Markiert);
        Assert.Equal(meldungen, wirt.Meldungen);
    }

    /// <summary>
    /// <b>Ohne den Schalter bleibt alles, wie es war</b> — und ohne <c>ZeileIstWahl</c>
    /// wirkt er nicht (Projektdialoge, Importe behalten ihre Wahlspalte).
    /// </summary>
    [Fact]
    public void Ohne_Schalter_oder_ohne_Zeilenwahl_keine_Kaestchen()
    {
        var ohne = Aufbauen(new Wirt(), kaestchen: false);
        Assert.Empty(ohne.FindAll(".epos-spalte-kaestchen"));
        Assert.Single(ohne.FindAll(".epos-katalog-vergleichknopf"));

        var projekt = Aufbauen(new Wirt(), kaestchen: true, zeileIstWahl: false);
        Assert.Empty(projekt.FindAll(".epos-spalte-kaestchen"));
        Assert.NotEmpty(projekt.FindAll("th.epos-spalte-wahl"));
    }

    // =====================================================================
    //  Der Zustand im Wirt
    // =====================================================================

    /// <summary>Unter zwei Kästchen endet der Vergleich; ohne zwei geht er gar nicht an.</summary>
    [Fact]
    public void Die_Zeilenauswahl_beendet_den_Vergleich_unter_zwei()
    {
        var a = new Zeilenauswahl();
        Assert.False(a.VergleichUmschalten());

        a.Setzen(new[] { "A", "B" });
        Assert.True(a.VergleichUmschalten());
        a.Setzen(new[] { "A" });
        Assert.False(a.Vergleich);

        a.Setzen(new[] { "A", "B" });
        a.VergleichUmschalten();
        a.Aufheben();
        Assert.False(a.Vergleich);
        Assert.Empty(a.Gewaehlte);
    }

    /// <summary>Worauf eine Handlung wirkt: die gewählten Zeilen, sonst die Fokuszeile.</summary>
    [Fact]
    public void Die_Ziele_sind_die_gewaehlten_sonst_die_Fokuszeile()
    {
        var a = new Zeilenauswahl();
        Assert.Equal(new[] { "F" }, a.Ziele("F"));
        Assert.Empty(a.Ziele(""));

        a.Setzen(new[] { "A", "B" });
        Assert.Equal(new[] { "A", "B" }, a.Ziele("F"));
    }

    /// <summary>
    /// <b>Abgleichen nach dem Neuaufbau</b>: Kästchen gelöschter Zeilen fallen (neue
    /// Instanz); stehen alle noch, bleibt die Instanz dieselbe — sonst zöge die Liste bei
    /// jedem Speichern grundlos nach.
    /// </summary>
    [Fact]
    public void Abgleichen_behaelt_die_Instanz_wenn_nichts_fehlt()
    {
        var a = new Zeilenauswahl();
        var liste = new List<string> { "A", "B" };
        a.Setzen(liste);

        a.Abgleichen(new[] { "A", "B", "C" });
        Assert.Same(liste, a.Gewaehlte);

        a.Abgleichen(new[] { "A", "C" });
        Assert.NotSame(liste, a.Gewaehlte);
        Assert.Equal(new[] { "A" }, a.Gewaehlte);
    }

    /// <summary>
    /// <b>Nach einer Übernahme sind die neuen Zeilen die Auswahl</b> (Konzept 7.1 d): ab
    /// zweien als Kästchen in ihrer Reihenfolge (doppelte und leere fallen), eine einzelne
    /// als Fokuszeile allein — Kästchen von vorher fallen in beiden Fällen, ein Vergleich
    /// endet, und die Instanz ist immer neu, damit die Liste nachzieht. Jede Übernahme
    /// zählt <see cref="Zeilenauswahl.Uebernahmen"/> weiter — der Anlass, an dem die Liste
    /// die Fokuszeile ins Bild rollt; die übrigen Handlungen zählen nicht.
    /// </summary>
    [Fact]
    public void Nach_einer_Uebernahme_sind_die_neuen_Zeilen_gewaehlt()
    {
        var a = new Zeilenauswahl();
        Assert.Equal(0, a.Uebernahmen);
        a.Setzen(new[] { "A", "B" });
        a.VergleichUmschalten();
        var vorher = a.Gewaehlte;

        a.Uebernommen(new[] { "N1", "N2", "N1", "", "N3" });
        Assert.Equal(new[] { "N1", "N2", "N3" }, a.Gewaehlte);
        Assert.NotSame(vorher, a.Gewaehlte);
        Assert.False(a.Vergleich);
        Assert.Equal(3, a.Anzahl);
        Assert.Equal(1, a.Uebernahmen);

        vorher = a.Gewaehlte;
        a.Uebernommen(new[] { "N4" });
        Assert.Empty(a.Gewaehlte);
        Assert.NotSame(vorher, a.Gewaehlte);
        Assert.Equal(new[] { "N4" }, a.Ziele("N4"));
        Assert.Equal(2, a.Uebernahmen);

        a.Setzen(new[] { "X", "Y" });
        a.Aufheben();
        a.Abgleichen(new[] { "Z" });
        Assert.Equal(2, a.Uebernahmen);
    }
}

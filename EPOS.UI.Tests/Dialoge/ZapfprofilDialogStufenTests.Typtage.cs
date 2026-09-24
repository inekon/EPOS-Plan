using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using KiKern;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Die WAHL DES TYPTAGWEGS im Zapfprofil-Dialog (Umsetzungskonzept Zapfprofilgenerator 4.2, 5.3;
/// Stufe Z4b, Gruppe 2): Sie steht in der Stufe Experte bei den Fachwerten neben dem
/// Tagesgangsatz, gilt für das ganze PROJEKT und ist ohne eingespielte Typtage gesperrt — mit
/// Grund, nie still. Der Knopf „VDI-4655-Typtage…" öffnet die Überlagerung des Importdialogs;
/// nimmt der Anwender dort den Stand weg, fällt die Wahl benannt auf den Bestandsweg zurück.
///
/// <para>Alle Delegaten sind Prüfdelegaten; Werte erfunden, kein Wert der Richtlinie.</para>
/// </summary>
public partial class ZapfprofilDialogStufenTests
{
    /// <summary>Ein erfundener Stand: zwei Klimazonen, eine Gebäudeart.</summary>
    private static TwwTyptagStandDaten Typtagstand(int zonen = 2) => new()
    {
        Vorhanden = true,
        Zeilen = 42,
        Quelle = "Probenrichtlinie",
        DatumImport = "2026-09-24",
        Klimazonen = zonen == 1 ? new List<int> { 3 } : new List<int> { 3, 7 },
        Gebaeudearten = new List<string> { "probehaus" },
        Typtage = new List<string> { "TT01", "TT02" }
    };

    private IRenderedComponent<ZapfprofilDialog> AufbauenMitTyptagen(
        TwwTyptagStandDaten? stand = null,
        Func<ZapfprofilKatalogstandDaten>? katalogstand = null,
        Action<ZapfprofilErgebnisDaten?>? geschlossen = null,
        bool mitGaben = true)
    {
        ZapfprofilDaten daten = Daten();
        daten.Typtagstand = stand ?? new TwwTyptagStandDaten();
        return Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, Vorschau)
            .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
            .Add(x => x.EntprellungMs, 0)
            .Add(x => x.Katalogstand, katalogstand)
            .Add(x => x.TyptagGaben, mitGaben
                ? () => new Dictionary<string, object> { ["Stand"] = new Func<TwwTyptagStandDaten>(() => stand ?? new TwwTyptagStandDaten()) }
                : null)
            .Add(x => x.Geschlossen, e => geschlossen?.Invoke(e)));
    }

    private static IElement Typtagschalter(IRenderedComponent<ZapfprofilDialog> cut)
        => cut.FindAll("label.epos-schalter").First(l => l.TextContent.Trim() == "Typtage nach VDI 4655 rechnen")
              .QuerySelector("input")!;

    // =================================================================================
    // Die Gruppe steht erst in Experte, und ohne Typtage ist sie gesperrt
    // =================================================================================

    [Fact]
    public void Die_Wahl_des_Typtagwegs_steht_erst_in_der_Stufe_Experte()
    {
        var cut = AufbauenMitTyptagen(Typtagstand());
        Assert.Empty(cut.FindAll("label.epos-schalter").Where(l => l.TextContent.Contains("Typtage")));

        Stufe(cut, "Erweitert");
        Assert.Empty(cut.FindAll("label.epos-schalter").Where(l => l.TextContent.Contains("Typtage")));

        Stufe(cut, "Experte");
        Assert.Contains("Typtage nach VDI 4655", cut.Find(".epos-zapfprofil-typtagleiste").ParentElement!.TextContent);
        Assert.NotNull(Typtagschalter(cut));
    }

    [Fact]
    public void Ohne_eingespielte_Typtage_ist_der_Schalter_gesperrt_und_nennt_den_Grund()
    {
        var cut = AufbauenMitTyptagen();
        Stufe(cut, "Experte");

        IElement schalter = Typtagschalter(cut);
        Assert.True(schalter.HasAttribute("disabled"));
        Assert.Contains("nicht verfügbar", cut.Find(".epos-zapfprofil-typtagleiste").ParentElement!.TextContent);

        // Der Versuch wirkt nicht - und die Wahlfelder erscheinen nicht.
        schalter.Change(true);
        Assert.Empty(cut.FindAll("label.epos-feld").Where(l => l.TextContent.StartsWith("Klimazone")));
    }

    // =================================================================================
    // Einschalten, vorbelegen, wählen
    // =================================================================================

    [Fact]
    public void Der_Schalter_zeigt_Klimazone_und_Gebaeudeart_und_belegt_eine_einzige_vor()
    {
        ZapfprofilErgebnisDaten? ergebnis = null;
        var cut = AufbauenMitTyptagen(Typtagstand(zonen: 1), geschlossen: e => ergebnis = e);
        Stufe(cut, "Experte");

        Typtagschalter(cut).Change(true);

        // Eine Zone und eine Gebäudeart gibt es nur einmal - sie werden vorbelegt.
        IElement zone = cut.FindAll("label.epos-feld").First(l => l.TextContent.StartsWith("Klimazone")).QuerySelector("select")!;
        IElement art = cut.FindAll("label.epos-feld").First(l => l.TextContent.StartsWith("Gebäudeart")).QuerySelector("select")!;
        Assert.Equal("3", zone.GetAttribute("value"));
        Assert.Equal("1", art.GetAttribute("value"));
        // Die Herleitungszeile sagt, dass die Wahl dem PROJEKT gilt, nicht der Zone.
        Assert.Contains("Angabe des Projekts", cut.Find(".epos-zapfprofil-typtagleiste").ParentElement!.TextContent);

        Knopf(cut, "OK").Click();
        Assert.True(ergebnis?.Eingabe.TyptageAktiv);
        Assert.Equal(3, ergebnis!.Eingabe.TyptageKlimazone);
        Assert.Equal("probehaus", ergebnis.Eingabe.TyptageGebaeudeart);
    }

    [Fact]
    public void Bei_mehreren_Zonen_bleibt_die_Wahl_leer_bis_der_Anwender_sie_trifft()
    {
        ZapfprofilErgebnisDaten? ergebnis = null;
        var cut = AufbauenMitTyptagen(Typtagstand(), geschlossen: e => ergebnis = e);
        Stufe(cut, "Experte");

        Typtagschalter(cut).Change(true);
        IElement zone = cut.FindAll("label.epos-feld").First(l => l.TextContent.StartsWith("Klimazone")).QuerySelector("select")!;
        Assert.Equal("", zone.GetAttribute("value"));
        Assert.Equal(3, zone.QuerySelectorAll("option").Length);         // Platzhalter, 3, 7

        zone.Change("7");
        Knopf(cut, "OK").Click();
        Assert.Equal(7, ergebnis?.Eingabe.TyptageKlimazone);
        Assert.Equal("probehaus", ergebnis?.Eingabe.TyptageGebaeudeart);  // eine Gebäudeart, vorbelegt
    }

    [Fact]
    public void Eine_gespeicherte_Wahl_die_das_Paket_nicht_fuehrt_bleibt_sichtbar()
    {
        ZapfprofilEingabeDaten e = Eingabe();
        e.TyptageAktiv = true;
        e.TyptageKlimazone = 11;
        e.TyptageGebaeudeart = "altbau";
        ZapfprofilDaten daten = Daten(e);
        daten.Typtagstand = Typtagstand();

        var cut = Render<ZapfprofilDialog>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Texte, new ZapfprofilTexte())
            .Add(x => x.Vorschau, Vorschau)
            .Add(x => x.Pruefen, _ => Array.Empty<ZapfprofilMeldung>())
            .Add(x => x.EntprellungMs, 0));
        Stufe(cut, "Experte");

        IElement zone = cut.FindAll("label.epos-feld").First(l => l.TextContent.StartsWith("Klimazone")).QuerySelector("select")!;
        IElement art = cut.FindAll("label.epos-feld").First(l => l.TextContent.StartsWith("Gebäudeart")).QuerySelector("select")!;
        Assert.Equal("11", zone.GetAttribute("value"));
        Assert.Equal(4, zone.QuerySelectorAll("option").Length);         // Platzhalter, 11, 3, 7
        Assert.Equal("1", art.GetAttribute("value"));
        Assert.Equal("altbau", art.QuerySelectorAll("option").First(o => o.GetAttribute("value") == "1").TextContent);
    }

    // =================================================================================
    // Der Importdialog als Überlagerung
    // =================================================================================

    [Fact]
    public void Ohne_Delegat_steht_der_Knopf_der_Typtage_nicht_da()
    {
        var cut = AufbauenMitTyptagen(Typtagstand(), mitGaben: false);
        Stufe(cut, "Experte");

        Assert.Empty(cut.FindAll("button").Where(b => b.TextContent.Trim() == "VDI-4655-Typtage…"));
        Assert.NotNull(Typtagschalter(cut));
    }

    [Fact]
    public void Nach_dem_Loeschen_des_Stands_faellt_die_Wahl_benannt_zurueck()
    {
        TwwTyptagStandDaten stand = Typtagstand(zonen: 1);
        var leer = new ZapfprofilKatalogstandDaten
        {
            Katalog = { Art(1, "Wohnen A", wohnen: true), Art(2, "Büro B", wohnen: false) },
            Typtagstand = new TwwTyptagStandDaten()
        };
        var cut = AufbauenMitTyptagen(stand, katalogstand: () => leer);
        Stufe(cut, "Experte");
        Typtagschalter(cut).Change(true);
        Assert.True(cut.Instance.Eingabe.TyptageAktiv);

        Knopf(cut, "VDI-4655-Typtage…").Click();
        Assert.NotEmpty(cut.FindAll(".epos-tww-typtage"));

        // Der Importdialog meldet „geändert": Der Katalogstand kommt neu, und ohne Typtage
        // ist der Weg nicht mehr verfügbar.
        cut.InvokeAsync(() => cut.FindComponent<TwwTyptagImportDialog>().Instance.Geschlossen.InvokeAsync(true));
        cut.WaitForAssertion(() => Assert.False(cut.Instance.Eingabe.TyptageAktiv));
        Assert.True(Typtagschalter(cut).HasAttribute("disabled"));
        Assert.Empty(cut.FindAll(".epos-tww-typtage"));
    }

    // =================================================================================
    // Der Hilfe-Assistent
    // =================================================================================

    [Fact]
    public void Der_Assistent_setzt_den_Typtagweg_nur_in_Experte_und_nur_mit_Daten()
    {
        var cut = AufbauenMitTyptagen(Typtagstand(zonen: 1));
        KiFeldzugang Zugang(string feld) => KiMaskenbruecke.Feldzugang(KiMaskennamen.ZAPFPROFIL, feld)!;

        // Stufe Einfach: benannt abgelehnt.
        Assert.Throws<InvalidOperationException>(() => Zugang("typtageweg").Setzen(true));

        Stufe(cut, "Experte");
        Zugang("typtageweg").Setzen(true);
        Assert.True(cut.Instance.Eingabe.TyptageAktiv);
        Assert.Equal(3, Zugang("typtagzone").Lesen());
        Assert.Equal(1, Zugang("typtagart").Lesen());

        // Eine Zone, die das Paket nicht führt, wird abgelehnt.
        Assert.Throws<InvalidOperationException>(() => Zugang("typtagzone").Setzen(99));
        Assert.Equal(3, cut.Instance.Eingabe.TyptageKlimazone);

        // Die Wahllisten stehen dem Assistenten zur Verfügung (KI-D-Q6).
        Assert.Single(Zugang("typtagzone").Eintraege!());
        Assert.Single(Zugang("typtagart").Eintraege!());
    }

    [Fact]
    public void Ohne_eingespielte_Typtage_lehnt_der_Assistent_den_Weg_benannt_ab()
    {
        var cut = AufbauenMitTyptagen();
        Stufe(cut, "Experte");
        KiFeldzugang Zugang(string feld) => KiMaskenbruecke.Feldzugang(KiMaskennamen.ZAPFPROFIL, feld)!;

        Assert.Throws<InvalidOperationException>(() => Zugang("typtageweg").Setzen(true));
        Assert.False(cut.Instance.Eingabe.TyptageAktiv);
    }
}

using Bunit;
using EPOS.UI.Bausteine;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Optionsgruppe (iU9-W1.0) - Ersatz der RadioButton-Gruppen des Bestands.
/// Geprueft wird, was die Masken von ihnen verlangen: alle Optionen sichtbar,
/// genau eine gewaehlt, einzelne sperrbar, jede Wahl gemeldet.
/// </summary>
public class OptionsgruppeTests : BunitContext
{
    private static readonly (int Id, string Text)[] Quellen =
    {
        (1, "Aus Vorlage/Variante:"),
        (2, "Aus Projekt/Anlage:")
    };

    [Fact]
    public void Zeigt_alle_Eintraege_mit_ihrer_Beschriftung()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.Equal(2, knoepfe.Count);

        var texte = cut.FindAll(".epos-feld-text");
        Assert.Equal("Aus Vorlage/Variante:", texte[0].TextContent);
        Assert.Equal("Aus Projekt/Anlage:", texte[1].TextContent);
    }

    [Fact]
    public void Der_Gruppentitel_steht_in_der_Legende()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Bezeichnung, "Quelle")
            .Add(x => x.Eintraege, Quellen));

        Assert.Equal("Quelle", cut.Find(".epos-optionsgruppe-titel").TextContent);
        Assert.Equal("Quelle", cut.Find("fieldset").GetAttribute("aria-label"));
    }

    [Fact]
    public void Ohne_Bezeichnung_gibt_es_keine_Legende()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        Assert.Empty(cut.FindAll(".epos-optionsgruppe-titel"));
    }

    [Fact]
    public void Die_Gruppe_meldet_sich_als_radiogroup()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        Assert.Equal("radiogroup", cut.Find("fieldset").GetAttribute("role"));
    }

    [Fact]
    public void Genau_die_gewaehlte_Option_ist_angehakt()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Auswahl, 2));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.False(knoepfe[0].HasAttribute("checked"));
        Assert.True(knoepfe[1].HasAttribute("checked"));
    }

    [Fact]
    public void Eine_Wahl_meldet_ihre_Id()
    {
        int? erhalten = null;
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Auswahl, 1)
            .Add(x => x.AuswahlChanged, (int? id) => erhalten = id));

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.Equal(2, erhalten);
    }

    [Fact]
    public void Alle_Eintraege_teilen_sich_einen_Namen()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.Equal(knoepfe[0].GetAttribute("name"), knoepfe[1].GetAttribute("name"));
        Assert.False(string.IsNullOrEmpty(knoepfe[0].GetAttribute("name")));
    }

    [Fact]
    public void Zwei_Gruppen_tragen_verschiedene_Namen()
    {
        var eine = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));
        var andere = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        Assert.NotEqual(eine.Find("input[type=radio]").GetAttribute("name"),
                        andere.Find("input[type=radio]").GetAttribute("name"));
    }

    [Fact]
    public void Aktiv_false_sperrt_die_ganze_Gruppe()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Aktiv, false));

        foreach (var knopf in cut.FindAll("input[type=radio]"))
        {
            Assert.True(knopf.HasAttribute("disabled"));
        }
    }

    [Fact]
    public void Eine_gesperrte_Option_bleibt_sichtbar_und_ist_nicht_waehlbar()
    {
        // Vorbild: rbQuelleVorlage.Enabled = _vorlagen.Count > 0.
        int? erhalten = null;
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Gesperrt, new[] { 1 })
            .Add(x => x.AuswahlChanged, (int? id) => erhalten = id));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.Equal(2, knoepfe.Count);
        Assert.True(knoepfe[0].HasAttribute("disabled"));
        Assert.False(knoepfe[1].HasAttribute("disabled"));

        knoepfe[0].Change(true);
        Assert.Null(erhalten);
    }

    /// <summary>
    /// Erlaeuterung unter einer Option (iU9-W10a.1). Vorbild sind die drei Labels
    /// unter den Wahlknoepfen von Form_Betriebsmodus; ohne sie waere die Wahl
    /// zwischen "laufzeitoptimiert" und "PV-optimiert" eine Ratefrage.
    /// </summary>
    [Fact]
    public void Eine_Erlaeuterung_steht_unter_ihrer_Option()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.Beschreibungen, new Dictionary<int, string>
            {
                [1] = "Erklärung zur zweiten Möglichkeit"
            }));

        var texte = cut.FindAll("p.epos-option-beschreibung");
        Assert.Single(texte);
        Assert.Equal("Erklärung zur zweiten Möglichkeit", texte[0].TextContent);
    }

    /// <summary>Ohne Beschreibungen steht kein einziger Erlaeuterungsabsatz da.</summary>
    [Fact]
    public void Ohne_Beschreibungen_steht_keine_Erlaeuterung()
    {
        var cut = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));

        Assert.Empty(cut.FindAll("p.epos-option-beschreibung"));
    }

    // =========================================================== Ein Eintrag je Aufruf

    /// <summary>
    /// NurEintrag zeichnet genau EINEN Eintrag - fuer Masken, die die Alternativen in
    /// getrennte Rubriken stellen (Waermequelle Erdreich). Die Liste bleibt dabei
    /// vollstaendig: Sie ist der Stand der Wahl, nicht die Anzeigeliste.
    /// </summary>
    [Fact]
    public void NurEintrag_zeichnet_genau_einen_Eintrag()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.NurEintrag, 2)
            .Add(x => x.Auswahl, 2));

        var knoepfe = cut.FindAll("input[type=radio]");
        Assert.Single(knoepfe);
        Assert.Equal("2", knoepfe[0].GetAttribute("value"));
        Assert.True(knoepfe[0].HasAttribute("checked"));
        Assert.Equal("Aus Projekt/Anlage:", cut.Find(".epos-feld-text").TextContent);
    }

    /// <summary>
    /// Ein Eintrag je Aufruf ist KEINE eigene Gruppe: Die Rolle radiogroup faellt weg -
    /// sonst meldete jede Rubrik eine Wahl fuer sich. Das aria-label bleibt und nennt in
    /// jeder Rubrik dieselbe Wahl; die Legende faellt weg (der Rubriktitel steht darueber).
    /// </summary>
    [Fact]
    public void Ein_einzelner_Eintrag_meldet_keine_eigene_Gruppe()
    {
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Bezeichnung, "Quelle")
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.NurEintrag, 1));

        var rahmen = cut.Find("fieldset");
        Assert.False(rahmen.HasAttribute("role"));
        Assert.Equal("Quelle", rahmen.GetAttribute("aria-label"));
        Assert.True(rahmen.ClassList.Contains("epos-optionsgruppe--einzeln"));
        Assert.Empty(cut.FindAll(".epos-optionsgruppe-titel"));
    }

    /// <summary>
    /// Der Gruppenname haelt zwei Aufrufe zu EINER Wahl zusammen: gleicher HTML-Name,
    /// also wandern die Pfeiltasten zwischen ihnen. Ohne Gruppenname traegt jede Instanz
    /// ihren eigenen Namen - zwei Gruppen auf derselben Seite schalten einander sonst um.
    /// </summary>
    [Fact]
    public void Der_Gruppenname_haelt_zwei_Aufrufe_zusammen()
    {
        var eine = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.NurEintrag, 1)
            .Add(x => x.Gruppenname, "quelle"));
        var andere = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.NurEintrag, 2)
            .Add(x => x.Gruppenname, "quelle"));

        Assert.Equal("quelle", eine.Find("input[type=radio]").GetAttribute("name"));
        Assert.Equal("quelle", andere.Find("input[type=radio]").GetAttribute("name"));

        var ohne = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));
        var nochOhne = Render<Optionsgruppe>(p => p.Add(x => x.Eintraege, Quellen));
        Assert.NotEqual(ohne.Find("input[type=radio]").GetAttribute("name"),
                        nochOhne.Find("input[type=radio]").GetAttribute("name"));
    }

    /// <summary>Auch als Einzelzeile meldet die Wahl ihre Id.</summary>
    [Fact]
    public void Ein_einzelner_Eintrag_meldet_seine_Wahl()
    {
        int? gewaehlt = null;
        var cut = Render<Optionsgruppe>(p => p
            .Add(x => x.Eintraege, Quellen)
            .Add(x => x.NurEintrag, 2)
            .Add(x => x.Auswahl, 1)
            .Add(x => x.AuswahlChanged, (int? id) => gewaehlt = id));

        cut.Find("input[type=radio]").Change("2");
        cut.WaitForAssertion(() => Assert.Equal(2, gewaehlt));
    }
}

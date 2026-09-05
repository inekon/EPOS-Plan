using System.Globalization;
using System.Threading;
using Bunit;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Katalogeditor Pufferspeicher (iU9-W14a.2) — der FEHLENDE VIERTE der Editorfamilie
/// aus W6/W7. Soll ist die Feldkarte von <c>Form_PufferSp_Bearbeiten</c>: drei Gruppen,
/// fünf Eingabefelder plus Auswahlliste, drei Speicherwege.
///
/// <para>Die Sprache wird im Konstruktor gepinnt (Regel seit iU9-W8, verschärft nach
/// dem Windows-Lauf 33839255709): Die Erwartungswerte sind deutsche Beschriftungen und
/// deutsche Zahlenschreibweise, der Windows-Läufer läuft mit englischer Oberfläche.</para>
/// </summary>
public class PufferSpKatalogDialogTests : BunitContext
{
    /// <summary>
    /// Die drei Speichertypen in der Reihenfolge der Auswahlliste — die Id ist der
    /// INDEX und damit der Steuerwert (Befund L0-1).
    /// </summary>
    private static readonly (int Id, string Text)[] Speichertypen =
    {
        (0, "Solarspeicher"), (1, "Pufferspeicher"), (2, "Kombispeicher")
    };

    public PufferSpKatalogDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        DeutscheOberflaeche();
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Kultur, UI-Kultur und die beiden <c>DefaultThread*</c>-Kulturen auf de-DE
    /// (Muster <c>GebaeudeKatalogDialogTests</c>).
    /// </summary>
    private static void DeutscheOberflaeche()
    {
        var de = new CultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        Thread.CurrentThread.CurrentCulture = de;
        Thread.CurrentThread.CurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;
    }

    private static PufferSpKatalogDaten Bestand() => new()
    {
        Name = "Puffer 3000Ltr",
        Firma = "Bosch",
        SpeichertypIndex = 1,
        Bereitschaftsverluste = 3.34,
        Gesamtvolumen = 3000,
        Investitionskosten = 1250.5
    };

    private IRenderedComponent<PufferSpKatalogDialog> Aufbauen(
        PufferSpKatalogDaten? daten = null,
        KatalogModus modus = KatalogModus.Bearbeiten,
        Func<PufferSpKatalogDaten, KatalogSpeicherErgebnis>? ueberschreiben = null,
        Func<PufferSpKatalogDaten, string, KatalogSpeicherErgebnis>? anlegen = null,
        Action<string?>? geschlossen = null,
        (int Id, string Text)[]? typen = null)
    {
        return Render<PufferSpKatalogDialog>(p => p
            .Add(x => x.Daten, daten ?? Bestand())
            .Add(x => x.Modus, modus)
            .Add(x => x.Speichertypen, typen ?? Speichertypen)
            .Add(x => x.Ueberschreiben,
                 ueberschreiben ?? (_ => new KatalogSpeicherErgebnis(true, "ok", "Puffer 3000Ltr")))
            .Add(x => x.Anlegen,
                 anlegen ?? ((_, n) => new KatalogSpeicherErgebnis(true, "ok", n)))
            .Add(x => x.Geschlossen, n => geschlossen?.Invoke(n)));
    }

    // =================================================================================
    // Feldbestand gegen die Karte
    // =================================================================================

    [Fact]
    public void Die_drei_Gruppen_der_Karte_stehen()
    {
        var cut = Aufbauen();

        var titel = cut.FindAll(".epos-gruppenkopf-titel");
        Assert.Equal(3, titel.Count);
        Assert.Equal("Bezeichnung", titel[0].TextContent);
        Assert.Equal("Technische Daten", titel[1].TextContent);
        Assert.Equal("Eingabedaten zur Berechnung der Kosten", titel[2].TextContent);
    }

    /// <summary>
    /// Fünf Eingabefelder und eine Auswahlliste — genau die dreizehn Kartenzeilen ohne
    /// die vier Knöpfe, die drei Gruppenrahmen und die reinen Einheitenlabels.
    /// </summary>
    [Fact]
    public void Der_Feldbestand_stimmt_nach_Zahl_und_Beschriftung()
    {
        var cut = Aufbauen();

        // Zwei Zahlenfelder (Verluste, Investitionskosten), ein Ganzzahlfeld (Volumen),
        // zwei reine Textfelder (Name, Hersteller), eine Auswahlliste.
        Assert.Equal(2, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Single(cut.FindAll("input[inputmode=numeric]"));
        Assert.Equal(2, cut.FindAll("input[type=text]:not([inputmode])").Count);
        Assert.Single(cut.FindAll("select"));
        Assert.Empty(cut.FindAll("textarea"));

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();
        Assert.Contains("Name:", texte);
        Assert.Contains("Hersteller:", texte);
        Assert.Contains("Speichertyp:", texte);
        Assert.Contains("Betriebsbereitschaftsverluste:", texte);
        Assert.Contains("Gesamtvolumen:", texte);
        Assert.Contains("Investitionskosten:", texte);
    }

    [Fact]
    public void Die_Einheiten_stehen_an_ihren_Feldern()
    {
        var cut = Aufbauen();

        var einheiten = cut.FindAll(".epos-einheit").Select(e => e.TextContent).ToList();
        Assert.Contains("kWh/d", einheiten);
        Assert.Contains("l", einheiten);
        Assert.Contains("€", einheiten);
    }

    [Fact]
    public void Die_englischen_Beschriftungen_lassen_sich_setzen()
    {
        var cut = Render<PufferSpKatalogDialog>(p => p
            .Add(x => x.Daten, Bestand())
            .Add(x => x.Speichertypen, Speichertypen)
            .Add(x => x.TitelText, "Buffer storage edit")
            .Add(x => x.LabelName, "Storage name:")
            .Add(x => x.GruppeTechnik, "Technical data")
            .Add(x => x.LabelVolumen, "Total volume:"));

        Assert.Equal("Buffer storage edit", cut.Find(".epos-dialog-titel").TextContent);
        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToList();

        // Befund W14-B24: der englische Text lautete "Boiler name:" - der Speichername,
        // beschriftet als Kessel.
        Assert.Contains("Storage name:", texte);
        Assert.DoesNotContain("Boiler name:", texte);
        Assert.Contains("Total volume:", texte);
        Assert.Equal("Technical data", cut.FindAll(".epos-gruppenkopf-titel")[1].TextContent);
    }

    // =================================================================================
    // Vorbelegung und Modus
    // =================================================================================

    [Fact]
    public void Die_Vorbelegung_kommt_aus_den_Daten()
    {
        var cut = Aufbauen();

        Assert.Equal("Puffer 3000Ltr", cut.FindAll("input[type=text]")[0].GetAttribute("value"));
        Assert.Equal("Bosch", cut.FindAll("input[type=text]")[1].GetAttribute("value"));
        Assert.Equal("3,34", cut.FindAll("input[inputmode=decimal]")[0].GetAttribute("value"));
        Assert.Equal("3000", cut.FindAll("input[inputmode=numeric]")[0].GetAttribute("value"));
        Assert.Equal("1", cut.Find("select").GetAttribute("value"));
    }

    [Fact]
    public void Im_Modus_Bearbeiten_ist_der_Name_nur_lesbar()
    {
        // Designer: textBox_Name gesperrt. Umbenannt wird ueber "Speichern unter".
        var cut = Aufbauen();

        Assert.True(cut.FindAll("input[type=text]")[0].HasAttribute("readonly"));
    }

    /// <summary>
    /// Konstruktor Z. 66-83: EDIT = „Überschreiben" + „Speichern unter",
    /// NEU = nur „Speichern". Die Enabled-Zustände bleiben bitgleich.
    /// </summary>
    [Fact]
    public void Der_Modus_entscheidet_ueber_die_drei_Speicherknoepfe()
    {
        var bearbeiten = Aufbauen(modus: KatalogModus.Bearbeiten);
        var knoepfeE = bearbeiten.FindAll(".epos-leiste .epos-knopf");
        Assert.False(knoepfeE[0].HasAttribute("disabled"));   // Überschreiben
        Assert.False(knoepfeE[1].HasAttribute("disabled"));   // Speichern unter
        Assert.False(knoepfeE[2].HasAttribute("disabled"));   // Abbrechen
        Assert.True(knoepfeE[3].HasAttribute("disabled"));    // Speichern

        var neu = Aufbauen(modus: KatalogModus.Neu);
        var knoepfeN = neu.FindAll(".epos-leiste .epos-knopf");
        Assert.True(knoepfeN[0].HasAttribute("disabled"));
        Assert.True(knoepfeN[1].HasAttribute("disabled"));
        Assert.False(knoepfeN[2].HasAttribute("disabled"));
        Assert.False(knoepfeN[3].HasAttribute("disabled"));
    }

    [Fact]
    public void Im_Modus_Neu_ist_der_Name_schreibbar()
    {
        var cut = Aufbauen(modus: KatalogModus.Neu);

        Assert.False(cut.FindAll("input[type=text]")[0].HasAttribute("readonly"));
    }

    // =================================================================================
    // Die drei Speicherwege
    // =================================================================================

    [Fact]
    public void Ueberschreiben_reicht_den_Feldsatz_weiter_und_schliesst()
    {
        PufferSpKatalogDaten? gesehen = null;
        string? ergebnis = null;

        var cut = Aufbauen(
            ueberschreiben: d => { gesehen = d; return new KatalogSpeicherErgebnis(true, "ok", d.Name); },
            geschlossen: n => ergebnis = n);

        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal("Puffer 3000Ltr", gesehen!.Name);
        Assert.Equal("Bosch", gesehen.Firma);
        Assert.Equal(1, gesehen.SpeichertypIndex);
        Assert.Equal(3000, gesehen.Gesamtvolumen);
        Assert.Equal("Puffer 3000Ltr", ergebnis);
    }

    [Fact]
    public void Speichern_legt_im_Modus_Neu_unter_dem_vorhandenen_Namen_an()
    {
        string? name = null;
        var daten = Bestand();
        daten.Name = "Neuer Speicher";

        var cut = Aufbauen(daten: daten, modus: KatalogModus.Neu,
                           anlegen: (_, n) => { name = n; return new KatalogSpeicherErgebnis(true, "ok", n); });

        cut.FindAll(".epos-leiste .epos-knopf")[3].Click();

        Assert.Equal("Neuer Speicher", name);
    }

    [Fact]
    public void Speichern_unter_fragt_den_Namen_in_einer_Ueberlagerung()
    {
        var cut = Aufbauen();

        Assert.False(cut.Instance.Namensfrage);
        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();

        Assert.True(cut.Instance.Namensfrage);
        Assert.Single(cut.FindAll(".epos-ueberlagerung"));
    }

    [Fact]
    public void Speichern_unter_legt_unter_dem_neuen_Namen_an()
    {
        string? name = null;
        string? ergebnis = null;
        var cut = Aufbauen(anlegen: (_, n) => { name = n; return new KatalogSpeicherErgebnis(true, "ok", n); },
                           geschlossen: n => ergebnis = n);

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]")[0].Input("Kopie 3000");
        cut.FindAll(".epos-ueberlagerung .epos-knopf--primaer")[0].Click();

        Assert.Equal("Kopie 3000", name);
        Assert.Equal("Kopie 3000", ergebnis);
    }

    /// <summary>
    /// <b>Befund W14-B22.</b> Der Vorläufer setzte bei einem fehlgeschlagenen
    /// „Überschreiben" <c>DialogResult.Cancel</c> und schloss OHNE Meldung. Jetzt
    /// bleibt der Dialog offen und zeigt den Grund.
    /// </summary>
    [Fact]
    public void Ein_Fehlschlag_laesst_den_Dialog_offen_und_nennt_den_Grund()
    {
        bool geschlossen = false;
        var cut = Aufbauen(
            ueberschreiben: _ => new KatalogSpeicherErgebnis(false, "Schreibgeschützt.", ""),
            geschlossen: _ => geschlossen = true);

        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.False(geschlossen);
        Assert.Equal("Schreibgeschützt.", cut.Instance.Meldung);
        Assert.Contains("Schreibgeschützt.", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Abbrechen_meldet_null()
    {
        string? ergebnis = "x";
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: n => { ergebnis = n; gerufen = true; });

        cut.FindAll(".epos-leiste .epos-knopf")[2].Click();

        Assert.True(gerufen);
        Assert.Null(ergebnis);
    }

    [Fact]
    public void Esc_schliesst_den_Dialog()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.True(gerufen);
    }

    /// <summary>
    /// Esc bei offener Namensabfrage schließt nur die Überlagerung, nicht den Dialog —
    /// die Regel „Esc schließt immer nur die oberste Ebene" (iU9-W7.5).
    /// </summary>
    [Fact]
    public void Esc_bei_offener_Namensabfrage_schliesst_den_Dialog_nicht()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        cut.FindAll(".epos-leiste .epos-knopf")[1].Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.False(gerufen);
    }

    // =================================================================================
    // Zahlenpruefung
    // =================================================================================

    /// <summary>
    /// Eine ungültige Zahl meldet beim Speichern IHREN NAMEN und hält den Dialog auf —
    /// dieselbe Regel wie <c>Program.GanzzahlPruefen</c>, nur ohne <c>MessageBox</c>.
    /// Der hartkodierte Feldname „Gesamtvolumen" des Vorläufers (Befund W14-B20) kommt
    /// jetzt aus dem Textkatalog.
    /// </summary>
    [Fact]
    public void Eine_ungueltige_Zahl_haelt_den_Speicherweg_auf()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        cut.FindAll("input[inputmode=numeric]")[0].Input("keine Zahl");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.False(gerufen);
        Assert.Contains("Gesamtvolumen", cut.Instance.Meldung);
    }

    /// <summary>
    /// Ein LEERES Feld ist gültig und zählt beim Schreiben als 0 — Bestandsregel
    /// <c>leerErlaubt: true</c> (Z. 261).
    /// </summary>
    [Fact]
    public void Ein_leeres_Zahlenfeld_haelt_nicht_auf()
    {
        bool gerufen = false;
        var cut = Aufbauen(geschlossen: _ => gerufen = true);

        cut.FindAll("input[inputmode=numeric]")[0].Input("");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.True(gerufen);
        Assert.Equal("", cut.Instance.Meldung);
    }

    /// <summary>
    /// Befund W14-B21: Verluste und Investitionskosten liefen im Vorläufer über
    /// <c>double.TryParse</c> OHNE Kultur, während das Volumen über
    /// <c>Program.GanzzahlPruefen</c> ging — dieselbe Maske, zwei Zahlregeln. Jetzt
    /// gilt für alle drei dieselbe: Komma und Punkt.
    /// </summary>
    [Fact]
    public void Komma_und_Punkt_gelten_in_allen_drei_Zahlenfeldern()
    {
        PufferSpKatalogDaten? gesehen = null;
        var cut = Aufbauen(ueberschreiben: d =>
        {
            gesehen = d;
            return new KatalogSpeicherErgebnis(true, "ok", d.Name);
        });

        cut.FindAll("input[inputmode=decimal]")[0].Input("4.5");
        cut.FindAll("input[inputmode=decimal]")[1].Input("1234,75");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(4.5, gesehen!.Bereitschaftsverluste);
        Assert.Equal(1234.75, gesehen.Investitionskosten);
    }

    // =================================================================================
    // Speichertyp
    // =================================================================================

    [Fact]
    public void Die_drei_Speichertypen_stehen_in_der_Auswahlliste()
    {
        var cut = Aufbauen();

        var eintraege = cut.FindAll("select option").Select(o => o.TextContent).ToList();
        Assert.Equal(new[] { "Solarspeicher", "Pufferspeicher", "Kombispeicher" }, eintraege);
    }

    /// <summary>
    /// Ein Katalogsatz mit unbekanntem Speichertyp verliert ihn NICHT: Die Hülle hängt
    /// den Rohwert als vierten Eintrag an, und die Auswahl zeigt ihn.
    /// </summary>
    [Fact]
    public void Ein_unbekannter_Speichertyp_bleibt_waehlbar()
    {
        var typen = new[]
        {
            (0, "Solarspeicher"), (1, "Pufferspeicher"), (2, "Kombispeicher"),
            (3, "Eiswürfelspeicher")
        };
        var daten = Bestand();
        daten.SpeichertypIndex = 3;

        var cut = Aufbauen(daten: daten, typen: typen);

        Assert.Equal("3", cut.Find("select").GetAttribute("value"));
        Assert.Contains("Eiswürfelspeicher",
                        cut.FindAll("select option").Select(o => o.TextContent));
    }

    [Fact]
    public void Die_Auswahl_geht_ueber_den_Index_in_den_Feldsatz()
    {
        PufferSpKatalogDaten? gesehen = null;
        var cut = Aufbauen(ueberschreiben: d =>
        {
            gesehen = d;
            return new KatalogSpeicherErgebnis(true, "ok", d.Name);
        });

        cut.Find("select").Change("2");
        cut.FindAll(".epos-leiste .epos-knopf")[0].Click();

        Assert.NotNull(gesehen);
        Assert.Equal(2, gesehen!.SpeichertypIndex);
    }
}

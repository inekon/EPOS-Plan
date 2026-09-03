using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Erzeuger;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Bedarfs-Stammkopf (iU9-W8.1). Soll sind die Feldkarten der DREI abgelösten Masken —
/// alle 31 Zeilen, alle 659 × 426: <c>Form_EingDBStromverbraucher</c>,
/// <c>Form_EingDBProzess</c>, <c>Form_EingDBBrauchwasser</c>. Geprüft wird je
/// AUSPRÄGUNG (Risiko R-W8-1).
/// </summary>
public class TypStammDialogTests : BunitContext
{
    private static readonly string[] TYPEN = { "Buero", "Gewerbe", "Wohnen" };

    public TypStammDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    private static TypStammDaten Daten(BedarfsArt art, bool mitWerten = true, string typ = "Gewerbe")
    {
        var d = new TypStammDaten { Art = art, Name = "Halle 1", Typ = typ, Beschreibung = "Probe" };
        if (mitWerten)
            for (int m = 0; m < 12; m++) d.Monat[m] = m + 1;
        return d;
    }

    /// <summary>Die zwölf Feldnamen der Prüfmeldung, je Ausprägung verschieden.</summary>
    private static string[] Feldnamen(BedarfsArt art)
    {
        var n = new string[12];
        string[] monate =
        {
            "Januar", "Februar", "März", "April", "Mai", "Juni",
            "Juli", "August", "September", "Oktober", "November", "Dezember"
        };
        for (int m = 0; m < 12; m++)
            n[m] = art == BedarfsArt.Stromverbraucher ? "Monatswert " + monate[m] : "Monat " + (m + 1);
        return n;
    }

    private IRenderedComponent<TypStammDialog> Aufbauen(
        TypStammDaten daten,
        KatalogModus modus = KatalogModus.Bearbeiten,
        Func<string, bool>? exists = null,
        Func<TypStammDaten, bool, string, KatalogSpeicherErgebnis>? speichern = null,
        Action<bool>? geschlossen = null,
        string labelTyp = "Verbrauchertyp:",
        string meldungTyp = "Verbrauchertyp auswählen!",
        string titel = "Eingabe Stromverbraucher")
        => Render<TypStammDialog>(p => p
            .Add(x => x.Daten, daten)
            .Add(x => x.Modus, modus)
            .Add(x => x.TitelText, titel)
            .Add(x => x.LabelTyp, labelTyp)
            .Add(x => x.MeldungTypFehlt, meldungTyp)
            .Add(x => x.Feldnamen, Feldnamen(daten.Art))
            .Add(x => x.Typen, () => TYPEN)
            .Add(x => x.Exists, exists ?? (_ => false))
            .Add(x => x.Speichern, speichern ?? ((_, _, n) => new KatalogSpeicherErgebnis(true, "", n)))
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<TypStammDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand je Ausprägung
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_beim_Stromverbraucher()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher));

        // 31 Kartenzeilen = Name, Typ, Beschreibung, 12 Monatswerte (mit je einer
        // Einheit) und vier Knoepfe.
        Assert.Equal(12, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Single(cut.FindAll("select"));
        Assert.Single(cut.FindAll("textarea"));

        Assert.Contains("Eingabe Stromverbraucher", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Verbrauchertyp:", cut.Markup);
        Assert.Contains("Name:", cut.Markup);
        Assert.Contains("Beschreibung:", cut.Markup);

        var knoepfe = cut.FindAll(".epos-leiste button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Überschreiben", "Speichern unter", "Speichern", "Beenden" }, knoepfe);
    }

    /// <summary>
    /// „Dezember" trägt im Designer aller drei Masken KEINEN Doppelpunkt; die zwölf
    /// Einheiten heißen „MWh".
    /// </summary>
    [Fact]
    public void Die_Monatsbeschriftungen_kommen_aus_dem_Designer()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher));

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent.Trim()).ToList();
        Assert.Contains("Januar:", texte);
        Assert.Contains("November:", texte);          // der Designer schrieb "Novmember:" (A-2)
        Assert.Contains("Dezember", texte);
        Assert.DoesNotContain("Dezember:", texte);
        Assert.Equal(12, cut.FindAll(".epos-einheit").Count(e => e.TextContent.Trim() == "MWh"));
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_bei_Prozess_und_Brauchwasser()
    {
        var prozess = Aufbauen(Daten(BedarfsArt.Prozesswaerme), labelTyp: "Prozesstyp:",
                               meldungTyp: "Prozesstyp auswählen!", titel: "Eingabe Prozess");
        Assert.Contains("Eingabe Prozess", prozess.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Prozesstyp:", prozess.Markup);
        Assert.Equal(12, prozess.FindAll("input[inputmode=decimal]").Count);

        var bw = Aufbauen(Daten(BedarfsArt.Brauchwasser), labelTyp: "Brauchwassertyp:",
                          meldungTyp: "Brauchwassertyp auswählen!",
                          titel: "Eingabe Brauchwasser Daten");
        Assert.Contains("Eingabe Brauchwasser Daten", bw.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Brauchwassertyp:", bw.Markup);
    }

    // =================================================================================
    // Modus
    // =================================================================================

    [Fact]
    public void Der_Modus_Bearbeiten_sperrt_Speichern()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher), KatalogModus.Bearbeiten);

        Assert.False(Knopf(cut, "Überschreiben").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Speichern unter").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Speichern").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Beenden").HasAttribute("disabled"));
    }

    [Fact]
    public void Der_Modus_Neu_sperrt_Ueberschreiben_und_Speichern_unter()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher, mitWerten: false), KatalogModus.Neu);

        Assert.True(Knopf(cut, "Überschreiben").HasAttribute("disabled"));
        Assert.True(Knopf(cut, "Speichern unter").HasAttribute("disabled"));
        Assert.False(Knopf(cut, "Speichern").HasAttribute("disabled"));
    }

    /// <summary>Im Modus Neu bleiben die zwölf Felder LEER — die Prüfung fordert sie ein.</summary>
    [Fact]
    public void Im_Modus_Neu_stehen_die_Monatsfelder_leer()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher, mitWerten: false), KatalogModus.Neu);

        foreach (IElement feld in cut.FindAll("input[inputmode=decimal]"))
            Assert.Equal("", feld.GetAttribute("value"));
    }

    // =================================================================================
    // Prüfregeln
    // =================================================================================

    [Fact]
    public void Ein_leerer_Monatswert_meldet_seinen_Namen_je_Auspraegung()
    {
        var strom = Daten(BedarfsArt.Stromverbraucher);
        strom.Monat[6] = null;
        var cut = Aufbauen(strom);
        Knopf(cut, "Überschreiben").Click();
        Assert.Contains("Monatswert Juli", cut.Find(".epos-warnbanner").TextContent);

        var prozess = Daten(BedarfsArt.Prozesswaerme);
        prozess.Monat[6] = null;
        var cut2 = Aufbauen(prozess, labelTyp: "Prozesstyp:");
        Knopf(cut2, "Überschreiben").Click();
        Assert.Contains("Monat 7", cut2.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Ein_leerer_Typ_meldet_beim_Speichern()
    {
        var daten = Daten(BedarfsArt.Stromverbraucher, typ: "");
        var cut = Aufbauen(daten, KatalogModus.Neu);

        Knopf(cut, "Speichern").Click();
        Assert.Contains("Verbrauchertyp auswählen!", cut.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>Erst der Typ, dann die Zahlen — die Reihenfolge von <c>btn_Speichern_Click</c>.</summary>
    [Fact]
    public void Der_Typ_wird_vor_den_Zahlen_geprueft()
    {
        var daten = Daten(BedarfsArt.Stromverbraucher, mitWerten: false, typ: "");
        var cut = Aufbauen(daten, KatalogModus.Neu);

        Knopf(cut, "Speichern").Click();
        Assert.Contains("Verbrauchertyp auswählen!", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Speicherwege
    // =================================================================================

    /// <summary>
    /// „Überschreiben" trifft den URSPRUNGSNAMEN, nicht den Feldinhalt — der Vorläufer
    /// nahm dafür sein Feld <c>m_szStromname</c>.
    /// </summary>
    [Fact]
    public void Ueberschreiben_trifft_den_Ursprungsnamen_und_meldet_ohne_zu_schliessen()
    {
        string? getroffen = null;
        bool istNeu = true;
        bool geschlossen = false;

        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher),
            speichern: (_, neu, bez) => { istNeu = neu; getroffen = bez; return new KatalogSpeicherErgebnis(true, "", bez); },
            geschlossen: _ => geschlossen = true);

        Knopf(cut, "Überschreiben").Click();

        Assert.Equal("Halle 1", getroffen);
        Assert.False(istNeu);
        Assert.False(geschlossen);                    // der Dialog bleibt offen (Bestand)
        Assert.Contains("Daten aktualisiert!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Speichern_legt_unter_dem_Namen_an_und_meldet()
    {
        bool istNeu = false;
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher), KatalogModus.Neu,
            speichern: (_, neu, bez) => { istNeu = neu; return new KatalogSpeicherErgebnis(true, "", bez); });

        Knopf(cut, "Speichern").Click();

        Assert.True(istNeu);
        Assert.Contains("Daten gespeichert!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Speichern_unter_fragt_den_Namen_in_einer_Ueberlagerung_mit_Vorbelegung()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher));
        Assert.False(cut.Instance.Namensfrage);

        Knopf(cut, "Speichern unter").Click();
        Assert.True(cut.Instance.Namensfrage);

        var feld = cut.FindAll(".epos-ueberlagerung input[type=text]").First();
        Assert.Equal("Halle 1", feld.GetAttribute("value"));
    }

    [Fact]
    public void Ein_belegter_Name_meldet_und_schreibt_nicht()
    {
        bool geschrieben = false;
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher),
            exists: _ => true,
            speichern: (_, _, bez) => { geschrieben = true; return new KatalogSpeicherErgebnis(true, "", bez); });

        Knopf(cut, "Speichern unter").Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]").First().Input("Halle 2");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(geschrieben);
        Assert.Contains("Name existiert bereits!", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Speichern_unter_uebernimmt_den_neuen_Namen()
    {
        string? getroffen = null;
        var daten = Daten(BedarfsArt.Stromverbraucher);
        var cut = Aufbauen(daten,
            speichern: (_, _, bez) => { getroffen = bez; return new KatalogSpeicherErgebnis(true, "", bez); });

        Knopf(cut, "Speichern unter").Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]").First().Input("Halle 2");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal("Halle 2", getroffen);
        Assert.Equal("Halle 2", daten.Name);
        Assert.Equal("Halle 1", cut.Instance.Ursprungsname);   // Ueberschreiben bleibt beim Alten
    }

    [Fact]
    public void Eine_Ablehnung_bleibt_als_Warnung_stehen()
    {
        bool geschlossen = false;
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher),
            speichern: (_, _, bez) => new KatalogSpeicherErgebnis(false, "Schreibgeschützt", bez),
            geschlossen: _ => geschlossen = true);

        Knopf(cut, "Überschreiben").Click();

        Assert.Contains("Schreibgeschützt", cut.Find(".epos-warnbanner").TextContent);
        Assert.False(geschlossen);
    }

    // =================================================================================
    // Tastatur
    // =================================================================================

    [Fact]
    public void Esc_schliesst_Enter_nicht()
    {
        int gemeldet = 0;
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher), geschlossen: _ => gemeldet++);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, gemeldet);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, gemeldet);
    }

    /// <summary>Esc bei offener Namensabfrage schließt nur die Überlagerung.</summary>
    [Fact]
    public void Esc_schliesst_bei_offener_Namensabfrage_nur_die_Ueberlagerung()
    {
        int gemeldet = 0;
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher), geschlossen: _ => gemeldet++);

        Knopf(cut, "Speichern unter").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(0, gemeldet);
    }
}

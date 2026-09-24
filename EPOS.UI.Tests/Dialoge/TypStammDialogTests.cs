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
public class TypStammDialogTests : EposBunitContext
{
    private static readonly string[] TYPEN = { "Buero", "Gewerbe", "Wohnen" };

    public TypStammDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
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

    /// <summary>Das Kreuz im Dialogkopf wirkt wie Esc/„Beenden": schließt mit <c>true</c>.</summary>
    [Fact]
    public void Kreuz_schliesst_wie_Esc()
    {
        bool? ergebnis = null;
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher), geschlossen: b => ergebnis = b);

        cut.Find(".epos-dialog-zu").Click();

        Assert.True(ergebnis);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher), titel: "");

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    // =====================================================================
    //  Formularraster (Anwenderwunsch iU8-E-2, Paket P3, 05.09.2026)
    // =====================================================================

    /// <summary>
    /// Der Kopf und die zwoelf Monatswerte stehen im Formularraster - im Vorbild (659 x 426) standen die Monate in zwei Spalten zu sechs.
    ///
    /// <para>Geprueft wird das MARKUP: Der Block traegt
    /// <c>epos-formularraster</c>, und darin stehen Felder. Was der Raster
    /// daraus MACHT (Beschriftungsspalte, kurzes Feld, zwei Spalten), steht
    /// als Stilblattprobe in <c>FormularrasterTests</c> - eine bunit-Probe
    /// rechnet kein CSS aus (Lehre W6-B-1).</para>
    /// </summary>
    [Fact]
    public void Kopf_und_Monatswerte_stehen_im_Formularraster()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher));

        Assert.Equal(2, cut.FindAll(".epos-formularraster").Count);

        // Zwoelf kurze Felder - je ein Monat.
        Assert.Equal(12, cut.FindAll(".epos-formularraster .epos-feld--kurz").Count);
    }

    // =================================================================================
    // Die Fussleiste nach der Hausregel (Konzept Knopfleisten, Abschnitt 5)
    // =================================================================================

    /// <summary>
    /// Das Katalogmuster: <b>Überschreiben · Speichern unter · Speichern · Füller ·
    /// Beenden</b>. Alle drei Speicherwege schreiben SOFORT und lassen die Maske
    /// stehen — es gibt keinen Arbeitsstand und damit kein Abbrechen. Sie stehen
    /// links vom Füller, und „Beenden" ist der eine primäre Schlussknopf.
    /// </summary>
    [Fact]
    public void Die_Fussleiste_traegt_das_Katalogmuster()
    {
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher));
        var leisten = cut.FindAll(".epos-dialog > .epos-leiste");
        IElement fuss = leisten[leisten.Count - 1];

        var knoepfe = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Überschreiben", "Speichern unter", "Speichern", "Beenden" }, knoepfe);

        var kinder = fuss.Children.Select(e => e.ClassName ?? "").ToList();
        Assert.Single(fuss.QuerySelectorAll(".epos-leiste-fueller"));
        Assert.Equal(3, kinder.FindIndex(k => k.Contains("epos-leiste-fueller")));

        var primaer = fuss.QuerySelectorAll("button.epos-knopf--primaer");
        Assert.Single(primaer);
        Assert.Equal("Beenden", primaer[0].TextContent.Trim());
    }

    /// <summary>
    /// Die Hervorhebung ist gewandert, die Wege sind geblieben: „Überschreiben" ruft
    /// weiter <c>Speichern</c>, ohne zu schließen, und der primäre Schlussknopf
    /// meldet <c>true</c>.
    /// </summary>
    [Fact]
    public void Die_gewanderten_Knoepfe_rufen_dieselben_Wege()
    {
        int laeufe = 0;
        bool? geschlossen = null;
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher),
                           speichern: (_, _, n) => { laeufe++; return new KatalogSpeicherErgebnis(true, "", n); },
                           geschlossen: b => geschlossen = b);

        Knopf(cut, "Überschreiben").Click();
        Assert.Equal(1, laeufe);
        Assert.Null(geschlossen);

        cut.Find(".epos-dialog > .epos-leiste button.epos-knopf--primaer").Click();
        Assert.True(geschlossen);
    }

    // =====================================================================
    //  Der Hilfe-Assistent (Welle KI-F3)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE dieser Maske an der Maskenbrücke.</b> Sie ist die einzige der
    /// Welle ohne Sichtklasse: Der Katalog hängt sich an <c>TypStammDaten</c> selbst,
    /// und ein Setzen der Beschreibung steht im selben Objekt, das die Maske zeigt.
    /// </summary>
    [Fact]
    public void Die_Maske_meldet_sich_beim_Assistenten_an_und_setzt_die_Beschreibung()
    {
        TypStammDaten daten = Daten(BedarfsArt.Stromverbraucher);
        var cut = Aufbauen(daten);

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.TYPSTAMM));

        WindowsFormsApplication1.KiFeldzugang name =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.TYPSTAMM, "name");
        Assert.NotNull(name);
        Assert.Equal("Halle 1", name.Lesen());
        Assert.False(name.Setzbar);

        WindowsFormsApplication1.KiFeldzugang beschreibung =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.TYPSTAMM, "beschreibung");
        Assert.True(beschreibung.Setzbar);

        beschreibung.Setzen("Neuer Text");
        cut.Render();

        Assert.Equal("Neuer Text", daten.Beschreibung);
    }

    /// <summary>
    /// <b>Der Verbrauchertyp ist ein WAHLFELD</b> (KI-D-Q6): Seine Einträge kennt nur
    /// der Dialog, er reicht sie beim Anmelden als Lieferant herein; gesetzt wird über
    /// den Anzeigetext, im Satz steht danach der Typname.
    /// </summary>
    [Fact]
    public void Der_Assistent_waehlt_den_Verbrauchertyp_ueber_seinen_Text()
    {
        TypStammDaten daten = Daten(BedarfsArt.Stromverbraucher);
        var cut = Aufbauen(daten);

        WindowsFormsApplication1.KiFeldzugang zugang =
            KiMaskenbruecke.Feldzugang(KiMaskennamen.TYPSTAMM, "typ");
        Assert.NotNull(zugang);
        Assert.Equal("Gewerbe", zugang.Lesen());

        KiFeldumsetzung umsetzung = KiFeldwandler.Wandle(zugang, "Wohnen");
        Assert.True(umsetzung.Ok, umsetzung.Grund);
        zugang.Setzen(umsetzung.Wert);
        cut.Render();

        Assert.Equal("Wohnen", daten.Typ);
    }

    // =====================================================================
    //  Die Zahlenreihe „monatswerte" (Welle #458 Stufe 3b)
    // =====================================================================

    /// <summary>
    /// <b>Die zwölf Monatswerte sind EINE Zahlenreihe</b> an <c>TypStammDaten.Monat</c>:
    /// gelesen als Liste, gesetzt ganz und je Monat — und die zwölf Felder zeigen danach
    /// genau das. Eine falsche Länge wird abgelehnt, der Satz bleibt unberührt.
    /// </summary>
    [Fact]
    public void Die_Monatswerte_liest_und_setzt_der_Assistent_als_Reihe()
    {
        TypStammDaten daten = Daten(BedarfsArt.Stromverbraucher);
        var cut = Aufbauen(daten);
        const string maske = KiMaskennamen.TYPSTAMM;

        Assert.Equal(Hilfe.KiReihenhilfe.Folge(12).Select(w => (double?)w),
                     Hilfe.KiReihenhilfe.Werte(maske, "monatswerte"));

        Hilfe.KiReihenhilfe.Setze(maske, "monatswerte", Hilfe.KiReihenhilfe.Folge(12, 10));
        cut.Render();
        Assert.Equal(120.0, daten.Monat[11]);
        Assert.Contains(cut.FindAll("input"), e => e.GetAttribute("value") == "120,0000");

        Hilfe.KiReihenhilfe.Setze(maske, "monatswerte", new[] { 7.5 }, ab: 3);
        Assert.Equal(new double?[] { 10, 20, 7.5, 40, 50, 60, 70, 80, 90, 100, 110, 120 }, daten.Monat);

        Assert.NotNull(Hilfe.KiReihenhilfe.Grund(maske, "monatswerte", new double[11]));
        Assert.NotNull(Hilfe.KiReihenhilfe.Grund(maske, "monatswerte", new double[2], ab: 12));
        Assert.Equal(7.5, daten.Monat[2]);
    }

    /// <summary>
    /// <b>Der Speicherweg ist der der Maske:</b> Die Pflichtprüfung meldet einen leeren
    /// Monat, und erst die vollständig gesetzte Reihe geht über „Überschreiben" in den
    /// Satz.
    /// </summary>
    [Fact]
    public async Task Die_gesetzte_Reihe_geht_ueber_den_Speicherweg_der_Maske_in_den_Satz()
    {
        double[]? geschrieben = null;
        var cut = Aufbauen(Daten(BedarfsArt.Stromverbraucher, mitWerten: false),
                           speichern: (d, _, n) =>
                           {
                               geschrieben = d.MonatWerte();
                               return new KatalogSpeicherErgebnis(true, "", n);
                           });
        KiMaskenhaken haken = KiMaskenbruecke.Haken(KiMaskennamen.TYPSTAMM);

        Assert.NotEqual("", haken.Befund());                       // zwölf leere Monate

        Hilfe.KiReihenhilfe.Setze(KiMaskennamen.TYPSTAMM, "monatswerte", Hilfe.KiReihenhilfe.Folge(12, 2.5));
        Assert.Equal("", haken.Befund());

        KiKern.KiErgebnis ergebnis = await cut.InvokeAsync(() => haken.Speichern());
        Assert.True(ergebnis.Erfolg, ergebnis.Text);
        Assert.Equal(Hilfe.KiReihenhilfe.Folge(12, 2.5), geschrieben);
    }
}

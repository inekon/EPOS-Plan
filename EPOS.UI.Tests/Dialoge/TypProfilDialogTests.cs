using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Wochen-Stundenprofil eines Bedarfstyps (iU9-W8.3). Soll sind die Feldkarten der DREI
/// abgelösten Masken — alle 607 × 544: <c>Form_EingStromTyp</c>,
/// <c>Form_EingProzTyp</c>, <c>Form_EingBrauchwasserTyp</c>. Geprüft wird je
/// AUSPRÄGUNG (Risiko R-W8-1).
/// </summary>
public class TypProfilDialogTests : BunitContext
{
    private static readonly string[] TYPEN = { "Buerogebaeude", "Wohnhaus" };

    public TypProfilDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        CultureInfo.CurrentUICulture = new CultureInfo("de-DE");
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>Ein Profil, dessen Wert die Stunde 1…168 IST — dann prüft sich alles selbst.</summary>
    private static double[,] Profil(double versatz = 0)
    {
        var w = new double[7, 24];
        for (int t = 0; t < 7; t++)
            for (int s = 0; s < 24; s++) w[t, s] = versatz + t * 24 + s + 1;
        return w;
    }

    private static string[] Feldnamen(BedarfsArt art)
    {
        var n = new string[24];
        string vorsatz = art == BedarfsArt.Stromverbraucher ? "Stundenwert" : "Stunde";
        for (int s = 0; s < 24; s++) n[s] = vorsatz + " " + (s + 1);
        return n;
    }

    private IRenderedComponent<TypProfilDialog> Aufbauen(
        BedarfsArt art = BedarfsArt.Stromverbraucher,
        Func<IReadOnlyList<string>>? typen = null,
        Func<string, (string, double[,])?>? lies = null,
        Func<string, double[,], string, bool>? speichern = null,
        Func<string, bool>? neu = null,
        Func<string, double[,], string, bool>? speichernUnter = null,
        Func<string, bool>? loeschen = null,
        Func<string, bool>? istReadOnly = null,
        Func<double[], byte[]>? bild = null,
        Action<bool>? geschlossen = null,
        string titel = "Stromverbrauchertyp Stundenverteilung",
        string labelListe = "Liste der Typen in der DB:")
        => Render<TypProfilDialog>(p => p
            .Add(x => x.Daten, new TypProfilDaten { Art = art })
            .Add(x => x.TitelText, titel)
            .Add(x => x.LabelTypliste, labelListe)
            .Add(x => x.Feldnamen, Feldnamen(art))
            .Add(x => x.Typen, typen ?? (() => TYPEN))
            .Add(x => x.Lies, lies ?? (t => ("Beschreibung " + t, Profil())))
            .Add(x => x.Speichern, speichern ?? ((_, _, _) => true))
            .Add(x => x.Neu, neu ?? (_ => true))
            .Add(x => x.SpeichernUnter, speichernUnter ?? ((_, _, _) => true))
            .Add(x => x.Loeschen, loeschen ?? (_ => true))
            .Add(x => x.IstReadOnly, istReadOnly ?? (_ => false))
            .Add(x => x.Bild, bild ?? (_ => new byte[] { 1, 2, 3 }))
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    private static IElement Knopf(IRenderedComponent<TypProfilDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim() == text);

    // =================================================================================
    // Feldbestand je Ausprägung
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_beim_Stromverbraucher()
    {
        var cut = Aufbauen();

        // Sieben Wochentage, 24 Stundenfelder, zwei Reiter, fuenf Knoepfe unten und
        // drei am Wochenblatt.
        Assert.Equal(7, cut.FindAll(".epos-option").Count);
        Assert.Equal(24, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Equal(2, cut.FindAll("[role=tab]").Count);

        Assert.Contains("Stromverbrauchertyp Stundenverteilung", cut.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Liste der Typen in der DB:", cut.Markup);
        Assert.Contains("Stundenwerte [KW, KWh oder %]", cut.Markup);
        Assert.Contains("Auswahl Wochentag", cut.Markup);

        var reiter = cut.FindAll("[role=tab]").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Wochenwerte", "Grafik" }, reiter);
    }

    /// <summary>Die sieben Wochentage sind Anzeigetexte und stehen als Ressourcen.</summary>
    [Fact]
    public void Die_sieben_Wochentage_stehen_in_der_richtigen_Reihenfolge()
    {
        var cut = Aufbauen();

        var tage = cut.FindAll(".epos-option .epos-feld-text").Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Montag", "Dienstag", "Mittwoch", "Donnerstag",
                             "Freitag", "Samstag", "Sonntag" }, tage);
    }

    /// <summary>Die 24 Beschriftungen sind die Stundenzahlen 1…24 aus dem Designer.</summary>
    [Fact]
    public void Die_Stundenbeschriftungen_laufen_von_eins_bis_vierundzwanzig()
    {
        var cut = Aufbauen();

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent.Trim()).ToList();
        for (int s = 1; s <= 24; s++) Assert.Contains(s.ToString(), texte);
    }

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_bei_Prozess_und_Brauchwasser()
    {
        var prozess = Aufbauen(BedarfsArt.Prozesswaerme,
            titel: "Prozesstypen Stundenverteilung",
            labelListe: "Liste der Prozesstypen in der DB:");
        Assert.Contains("Prozesstypen Stundenverteilung", prozess.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Liste der Prozesstypen in der DB:", prozess.Markup);
        Assert.Equal(24, prozess.FindAll("input[inputmode=decimal]").Count);

        var bw = Aufbauen(BedarfsArt.Brauchwasser,
            titel: "Brauchwassertypen Stundenverteilung",
            labelListe: "Brauchwassertypen in der DB:");
        Assert.Contains("Brauchwassertypen Stundenverteilung", bw.Find(".epos-dialog-titel").TextContent);
        Assert.Contains("Brauchwassertypen in der DB:", bw.Markup);
    }

    // =================================================================================
    // Vorbelegung, Typ- und Tagwahl
    // =================================================================================

    /// <summary>
    /// <c>SetControls</c>: Die erste Wahl steht vorn, sie lädt ihre Werte, und der Tag
    /// steht auf Montag.
    /// </summary>
    [Fact]
    public void Beim_Aufbau_steht_der_erste_Typ_und_Montag()
    {
        var cut = Aufbauen();

        Assert.Equal(TYPEN, cut.Instance.Typliste);
        Assert.Equal(0, cut.Instance.Wochentag);
        Assert.Equal(1.0, cut.Instance.Felder[0]);       // Montag, Stunde 1
        Assert.Equal(24.0, cut.Instance.Felder[23]);
    }

    [Fact]
    public void Die_Tagwahl_zeigt_die_vierundzwanzig_Werte_des_Tages()
    {
        var cut = Aufbauen();

        cut.FindAll(".epos-option input")[3].Change(true);   // Donnerstag
        Assert.Equal(3, cut.Instance.Wochentag);
        Assert.Equal(73.0, cut.Instance.Felder[0]);          // 3 * 24 + 1
        Assert.Equal(96.0, cut.Instance.Felder[23]);
    }

    /// <summary>
    /// Ein Tageswechsel verwirft nicht übernommene Eingaben — <c>Tagesdaten</c>
    /// überschrieb die 24 Felder aus <c>arr</c>, ohne zu fragen.
    /// </summary>
    [Fact]
    public void Ein_Tageswechsel_verwirft_nicht_uebernommene_Eingaben()
    {
        var cut = Aufbauen();

        cut.FindAll("input[inputmode=decimal]")[0].Input("999");
        Assert.Equal(999.0, cut.Instance.Felder[0]);

        cut.FindAll(".epos-option input")[1].Change(true);   // Dienstag
        cut.FindAll(".epos-option input")[0].Change(true);   // zurueck auf Montag
        Assert.Equal(1.0, cut.Instance.Felder[0]);
    }

    [Fact]
    public void Die_Typwahl_laedt_Werte_und_Beschreibung()
    {
        var cut = Aufbauen(lies: t => (t == "Wohnhaus" ? "Wohnen" : "Buero", Profil(t == "Wohnhaus" ? 1000 : 0)));

        cut.FindAll(".epos-auswahlspalte .epos-raster button")[1].Click();   // Wohnhaus
        Assert.Equal(1001.0, cut.Instance.Felder[0]);
        Assert.Contains("Wohnen", cut.Find("textarea").TextContent);
    }

    // =================================================================================
    // Übernehmen
    // =================================================================================

    [Fact]
    public void Uebernehmen_traegt_die_Felder_in_die_Werte_ein()
    {
        double[,] geschrieben = null!;
        var cut = Aufbauen(speichern: (_, w, _) => { geschrieben = w; return true; });

        cut.FindAll("input[inputmode=decimal]")[5].Input("42");
        Knopf(cut, "Änderungen Übernehmen").Click();
        Knopf(cut, "Speichern in DB").Click();

        Assert.NotNull(geschrieben);
        Assert.Equal(42.0, geschrieben[0, 5]);
    }

    [Fact]
    public void Ein_leerer_Stundenwert_meldet_seinen_Namen_je_Auspraegung()
    {
        var strom = Aufbauen();
        strom.FindAll("input[inputmode=decimal]")[6].Input("");
        Knopf(strom, "Änderungen Übernehmen").Click();
        Assert.Contains("Stundenwert 7", strom.Find(".epos-warnbanner").TextContent);

        var prozess = Aufbauen(BedarfsArt.Prozesswaerme);
        prozess.FindAll("input[inputmode=decimal]")[6].Input("");
        Knopf(prozess, "Änderungen Übernehmen").Click();
        Assert.Contains("Stunde 7", prozess.Find(".epos-warnbanner").TextContent);
    }

    /// <summary>Bei einem leeren Feld bleibt der Tag unverändert — es wird nichts übernommen.</summary>
    [Fact]
    public void Ein_leeres_Feld_laesst_den_Tag_unveraendert()
    {
        double[,] geschrieben = null!;
        var cut = Aufbauen(speichern: (_, w, _) => { geschrieben = w; return true; });

        cut.FindAll("input[inputmode=decimal]")[0].Input("77");
        cut.FindAll("input[inputmode=decimal]")[6].Input("");
        Knopf(cut, "Änderungen Übernehmen").Click();
        Knopf(cut, "Speichern in DB").Click();

        Assert.Equal(1.0, geschrieben[0, 0]);      // der alte Wert steht noch
    }

    // =================================================================================
    // Tag kopieren / einfügen (Befund W8-B1, A-6)
    // =================================================================================

    [Fact]
    public void Tag_einfuegen_ist_ohne_Kopie_gesperrt()
    {
        var cut = Aufbauen();
        Assert.False(cut.Instance.PufferGefuellt);
        Assert.True(Knopf(cut, "Tag einfügen").HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_kopierter_Tag_laesst_sich_in_einen_anderen_einfuegen()
    {
        var cut = Aufbauen();

        Knopf(cut, "Tag kopieren").Click();            // Montag: 1..24
        Assert.True(cut.Instance.PufferGefuellt);

        cut.FindAll(".epos-option input")[6].Change(true);   // Sonntag: 145..168
        Assert.Equal(145.0, cut.Instance.Felder[0]);

        Knopf(cut, "Tag einfügen").Click();
        Assert.Equal(1.0, cut.Instance.Felder[0]);
        Assert.Equal(24.0, cut.Instance.Felder[23]);
    }

    // =================================================================================
    // Speichern, Neu, Speichern unter, Löschen
    // =================================================================================

    [Fact]
    public void Speichern_auf_einem_Auslieferungstyp_meldet_und_schreibt_nicht()
    {
        bool geschrieben = false;
        var cut = Aufbauen(istReadOnly: _ => true,
                           speichern: (_, _, _) => { geschrieben = true; return true; });

        Knopf(cut, "Speichern in DB").Click();

        Assert.False(geschrieben);
        Assert.Contains("schreibgeschützt", cut.Find(".epos-warnbanner").TextContent);
    }

    [Fact]
    public void Speichern_meldet_den_Erfolg_und_laesst_den_Dialog_offen()
    {
        bool geschlossen = false;
        var cut = Aufbauen(geschlossen: _ => geschlossen = true);

        Knopf(cut, "Speichern in DB").Click();

        Assert.Contains("Datensatz gespeichert!", cut.Find(".epos-warnbanner").TextContent);
        Assert.False(geschlossen);
    }

    [Fact]
    public void Neu_fragt_den_Namen_und_waehlt_danach_den_neuen_Typ()
    {
        string angelegt = null!;
        var liste = new List<string>(TYPEN);

        var cut = Aufbauen(typen: () => liste,
                           neu: n => { angelegt = n; liste.Add(n); return true; });

        Knopf(cut, "Neu").Click();
        Assert.True(cut.Instance.Namensfrage);

        cut.FindAll(".epos-ueberlagerung input[type=text]").First().Input("Neuer Typ");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.Equal("Neuer Typ", angelegt);
        Assert.Contains("Neuer Typ", cut.Instance.Typliste);
    }

    [Fact]
    public void Speichern_unter_nimmt_die_aktuellen_Werte_mit()
    {
        double[,] mitgegeben = null!;
        var liste = new List<string>(TYPEN);

        var cut = Aufbauen(typen: () => liste,
                           speichernUnter: (n, w, _) => { mitgegeben = w; liste.Add(n); return true; });

        cut.FindAll("input[inputmode=decimal]")[2].Input("55");
        Knopf(cut, "Änderungen Übernehmen").Click();

        Knopf(cut, "Speichern unter").Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]").First().Input("Kopie");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.NotNull(mitgegeben);
        Assert.Equal(55.0, mitgegeben[0, 2]);
    }

    [Fact]
    public void Loeschen_fragt_erst_nach()
    {
        bool geloescht = false;
        var liste = new List<string>(TYPEN);
        var cut = Aufbauen(typen: () => liste,
                           loeschen: n => { geloescht = true; liste.Remove(n); return true; });

        Knopf(cut, "Löschen").Click();
        Assert.True(cut.Instance.Loeschfrage);
        Assert.False(geloescht);

        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "Nein").Click();
        Assert.False(geloescht);

        Knopf(cut, "Löschen").Click();
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "Ja").Click();
        Assert.True(geloescht);
        Assert.DoesNotContain("Buerogebaeude", cut.Instance.Typliste);
    }

    [Fact]
    public void Loeschen_eines_Auslieferungstyps_meldet_und_loescht_nicht()
    {
        bool geloescht = false;
        var cut = Aufbauen(istReadOnly: _ => true,
                           loeschen: _ => { geloescht = true; return true; });

        Knopf(cut, "Löschen").Click();
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "Ja").Click();

        Assert.False(geloescht);
        Assert.Contains("schreibgeschützt", cut.Find(".epos-warnbanner").TextContent);
    }

    // =================================================================================
    // Bild und Tastatur
    // =================================================================================

    [Fact]
    public void Das_Bild_bekommt_168_Werte()
    {
        int laenge = 0;
        var cut = Aufbauen(bild: w => { laenge = w.Length; return new byte[] { 9 }; });

        Assert.Equal(168, laenge);
        cut.FindAll("[role=tab]")[1].Click();
        Assert.Single(cut.FindAll("img.epos-chartbild"));
    }

    [Fact]
    public void Esc_schliesst_Enter_nicht()
    {
        int gemeldet = 0;
        var cut = Aufbauen(geschlossen: _ => gemeldet++);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Enter" });
        Assert.Equal(0, gemeldet);

        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, gemeldet);
    }

    [Fact]
    public void Esc_bei_offener_Rueckfrage_schliesst_den_Dialog_nicht()
    {
        int gemeldet = 0;
        var cut = Aufbauen(geschlossen: _ => gemeldet++);

        Knopf(cut, "Löschen").Click();
        cut.Find(".epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(0, gemeldet);
    }
}

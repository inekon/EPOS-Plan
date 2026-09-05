using System.Globalization;
using System.IO;
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
        Func<string, bool>? existiert = null,
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
            .Add(x => x.Existiert, existiert ?? (_ => false))
            .Add(x => x.Geschlossen, b => geschlossen?.Invoke(b)));

    /// <summary>
    /// Ein Knopf über seine Beschriftung. <c>EndsWith</c> und nicht <c>==</c>, weil
    /// „Änderungen Übernehmen" seit W8‑E‑1 wieder sein Diskettenzeichen vor dem Text
    /// trägt (<c>btn_WocheUebernehmen.Image</c> des Vorbilds); das Zeichen ist
    /// <c>aria-hidden</c> und steht in keiner Beschriftung.
    /// </summary>
    private static IElement Knopf(IRenderedComponent<TypProfilDialog> cut, string text)
        => cut.FindAll("button").First(b => b.TextContent.Trim().EndsWith(text));

    /// <summary>Die sieben Wochentagszeilen (Vorbild <c>listBox_Tag</c>).</summary>
    private static IReadOnlyList<IElement> Tageszeilen(IRenderedComponent<TypProfilDialog> cut)
        => cut.FindAll(".epos-typprofil-tage button.epos-zeilenwahl--breit");

    /// <summary>Die Typzeilen (Vorbild <c>listBox_Typname</c>).</summary>
    private static IReadOnlyList<IElement> Typzeilen(IRenderedComponent<TypProfilDialog> cut)
        => cut.FindAll(".epos-typprofil-typen button.epos-zeilenwahl--breit");

    // =================================================================================
    // Feldbestand je Ausprägung
    // =================================================================================

    [Fact]
    public void Der_Feldbestand_der_Karte_steht_beim_Stromverbraucher()
    {
        var cut = Aufbauen();

        // Sieben Wochentage, 24 Stundenfelder, zwei Reiter, fuenf Knoepfe unten und
        // drei am Wochenblatt.
        Assert.Equal(7, Tageszeilen(cut).Count);
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

        // Das Wahlzeichen der Zeile (●/○) steht als eigenes Zeichen davor; der NAME
        // steht in .epos-zeilenwahl-text.
        var tage = cut.FindAll(".epos-typprofil-tage .epos-zeilenwahl-text")
                      .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Montag", "Dienstag", "Mittwoch", "Donnerstag",
                             "Freitag", "Samstag", "Sonntag" }, tage);
    }

    // =================================================================================
    // Die Anordnung des Vorbilds (Befund W8-E-1, Windows-Abnahme 05.09.2026)
    // =================================================================================

    /// <summary>
    /// <b>Befund W8‑E‑1.</b> Der Anwender wollte den Dialog „so wie zuvor". Das Vorbild
    /// <c>Form_EingStromTyp</c> (607 × 544) stellt die Typliste (197 × 157 bei x = 17)
    /// und die Beschreibung (343 × 73 bei x = 248) NEBENEINANDER und darunter die zwei
    /// Reiter; die erste Razor-Fassung stapelte alles und war dreimal so hoch.
    /// </summary>
    [Fact]
    public void Typliste_und_Beschreibung_stehen_nebeneinander_ueber_den_Reitern()
    {
        var cut = Aufbauen();

        // Ein Paar mit genau zwei Spalten: links die Liste, rechts die Beschreibung.
        IElement paar = cut.Find(".epos-dialog > .epos-auswahlpaar");
        Assert.Equal(2, paar.QuerySelectorAll(".epos-auswahlspalte").Length);
        Assert.Single(paar.QuerySelectorAll(".epos-typprofil-typen .epos-raster-huelle"));
        Assert.Single(paar.QuerySelectorAll(".epos-typprofil-beschreibung textarea"));

        // Und die Reiterleiste steht DANACH, nicht in einer der Spalten.
        Assert.Empty(paar.QuerySelectorAll("[role=tab]"));
        Assert.Equal(2, cut.FindAll(".epos-dialog > .epos-reiter [role=tab]").Count);
    }

    /// <summary>
    /// Die 24 Stundenwerte stehen in DREI Spalten zu acht Zeilen (1–8, 9–16, 17–24) —
    /// im Markup aber weiter 1…24, damit der Tabulator die Reihenfolge der Maske nimmt
    /// (<c>st1</c>…<c>st24</c>, TabIndex 119…165). Die Dreiteilung macht das Stilblatt.
    /// </summary>
    [Fact]
    public void Die_Stundenwerte_stehen_in_einem_Raster_und_laufen_im_Markup_von_1_bis_24()
    {
        var cut = Aufbauen();

        IElement raster = cut.Find(".epos-typprofil-stunden .epos-stundenraster");
        Assert.Equal(24, raster.QuerySelectorAll("input[inputmode=decimal]").Length);

        var nummern = raster.QuerySelectorAll(".epos-feld-text")
                            .Select(e => e.TextContent.Trim()).ToList();
        Assert.Equal(Enumerable.Range(1, 24).Select(i => i.ToString()).ToList(), nummern);
    }

    /// <summary>
    /// <b>Eine bunit-Probe sieht eine Stilregel nicht</b> (Lehre W6‑B‑1) — die
    /// Dreiteilung steht deshalb hier gegen das Stilblatt selbst. Ohne
    /// <c>grid-auto-flow: column</c> stünden die 24 Felder wieder untereinander,
    /// und das Markup sähe genauso aus.
    /// </summary>
    [Fact]
    public void Das_Stundenraster_traegt_im_Stilblatt_acht_Zeilen_und_Spaltenfluss()
    {
        string raster = Stilblock(".epos-stundenraster {");

        Assert.Contains("display: grid", raster);
        Assert.Contains("grid-template-rows: repeat(8, auto)", raster);
        Assert.Contains("grid-auto-flow: column", raster);

        // Und die Stundennummer steht NEBEN dem Feld, nicht darueber - sonst waere
        // die Spalte doppelt so hoch, und genau das war der Befund.
        Assert.Contains("flex-direction: row", Stilblock(".epos-stundenraster .epos-feld {"));
    }

    /// <summary>
    /// Wochentagsliste und ihre zwei Knöpfe stehen RECHTS neben dem Stundenraster
    /// (<c>listBox_Tag</c> bei x = 345, <c>btn_Tagkopieren</c>/<c>btn_Tageinfuegen</c>
    /// darunter bei y = 185 und 217).
    /// </summary>
    [Fact]
    public void Die_Wochentage_stehen_neben_dem_Raster_mit_ihren_zwei_Knoepfen_darunter()
    {
        var cut = Aufbauen();

        IElement woche = cut.Find(".epos-typprofil-woche");
        Assert.Single(woche.QuerySelectorAll(".epos-typprofil-stunden"));
        Assert.Single(woche.QuerySelectorAll(".epos-typprofil-tage"));

        IElement tage = cut.Find(".epos-typprofil-tage");
        Assert.Single(tage.QuerySelectorAll(".epos-raster-huelle"));

        var knoepfe = tage.QuerySelectorAll(".epos-leiste button")
                          .Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Tag kopieren", "Tag einfügen" }, knoepfe);
    }

    /// <summary>
    /// Die Fußleiste in der Reihenfolge des Designers — x = 9 / 144 / 271 / 373 / 511.
    /// Die erste Razor-Fassung begann mit „Neu"; der Anwender liest sie von links.
    /// </summary>
    [Fact]
    public void Die_Fussleiste_steht_in_der_Reihenfolge_des_Vorbilds()
    {
        var cut = Aufbauen();

        var leisten = cut.FindAll(".epos-dialog > .epos-leiste");
        IElement fuss = leisten[leisten.Count - 1];

        var texte = fuss.QuerySelectorAll("button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "Speichern unter", "Speichern in DB", "Löschen", "Neu", "Schließen" },
                     texte);
    }

    /// <summary>
    /// Beide Listen stehen im Hausrahmen (Regel W9‑B‑2) — die Typliste rollt bei
    /// neun Zeilen, die Wochentage passen ganz hinein.
    /// </summary>
    [Fact]
    public void Beide_Listen_stehen_im_Rahmen_mit_Rollbalken()
    {
        var cut = Aufbauen();

        Assert.Single(cut.FindAll(".epos-typprofil-typen .epos-raster-huelle"));
        Assert.Single(cut.FindAll(".epos-typprofil-tage .epos-raster-huelle"));
        Assert.Empty(cut.FindAll(".epos-raster-huelle--frei"));

        // Und eine Listenzeile ist EIN Klickziel mit EINEM Tabulatorhalt (W4-E-1).
        Assert.Equal(TYPEN.Length, Typzeilen(cut).Count);
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

        Tageszeilen(cut)[3].Click();                        // Donnerstag
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

        Tageszeilen(cut)[1].Click();                        // Dienstag
        Tageszeilen(cut)[0].Click();                        // zurueck auf Montag
        Assert.Equal(1.0, cut.Instance.Felder[0]);
    }

    [Fact]
    public void Die_Typwahl_laedt_Werte_und_Beschreibung()
    {
        var cut = Aufbauen(lies: t => (t == "Wohnhaus" ? "Wohnen" : "Buero", Profil(t == "Wohnhaus" ? 1000 : 0)));

        Typzeilen(cut)[1].Click();                          // Wohnhaus
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

        Tageszeilen(cut)[6].Click();                        // Sonntag: 145..168
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

    // =================================================================================
    // Ein belegter Name (Befund W8-B-2, Windows-Abnahme 05.09.2026)
    // =================================================================================

    /// <summary>
    /// <b>Befund W8‑B‑2.</b> „Neu" mit dem vorhandenen Namen „test" lief bis in das
    /// <c>INSERT</c> und endete in einer MessageBox „Datenbankfehler: SQLite Error 19:
    /// 'UNIQUE constraint failed: Tab_Stromverbrauchertyp_STAMM.Typname'". Jetzt prüft
    /// der Dialog VORHER: Die Namensabfrage bleibt OFFEN, der Grund steht als
    /// Warnbanner über dem Feld, und geschrieben wird nichts.
    /// </summary>
    [Fact]
    public void Neu_mit_einem_belegten_Namen_meldet_und_laesst_die_Abfrage_offen()
    {
        string? angelegt = null;
        var cut = Aufbauen(neu: n => { angelegt = n; return true; },
                           existiert: n => n == "test");

        Knopf(cut, "Neu").Click();
        Assert.True(cut.Instance.Namensfrage);

        cut.FindAll(".epos-ueberlagerung input[type=text]").First().Input("test");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        // Die Abfrage steht noch, der Grund steht darin - und nichts ist geschrieben.
        Assert.True(cut.Instance.Namensfrage);
        Assert.Contains("Ein Typ mit diesem Namen ist schon vorhanden",
                        cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent);
        Assert.Null(angelegt);
    }

    /// <summary>Dasselbe für „Speichern unter" — derselbe Weg, derselbe Schutz.</summary>
    [Fact]
    public void Speichern_unter_mit_einem_belegten_Namen_meldet_und_schreibt_nicht()
    {
        string? angelegt = null;
        var cut = Aufbauen(speichernUnter: (n, _, _) => { angelegt = n; return true; },
                           existiert: n => n == "Wohnhaus");

        Knopf(cut, "Speichern unter").Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]").First().Input("Wohnhaus");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.True(cut.Instance.Namensfrage);
        Assert.Contains("Ein Typ mit diesem Namen ist schon vorhanden",
                        cut.Find(".epos-ueberlagerung .epos-warnbanner").TextContent);
        Assert.Null(angelegt);
    }

    /// <summary>
    /// Die Gegenprobe: Ein FREIER Name geht weiterhin durch, und die Abfrage schließt
    /// sich — sonst wäre die Vorprüfung eine, die alles sperrt.
    /// </summary>
    [Fact]
    public void Ein_freier_Name_geht_durch_und_die_Abfrage_schliesst_sich()
    {
        string? angelegt = null;
        var liste = new List<string>(TYPEN);

        var cut = Aufbauen(typen: () => liste,
                           neu: n => { angelegt = n; liste.Add(n); return true; },
                           existiert: liste.Contains);

        Knopf(cut, "Neu").Click();
        cut.FindAll(".epos-ueberlagerung input[type=text]").First().Input("Frischer Typ");
        cut.FindAll(".epos-ueberlagerung button").First(b => b.TextContent.Trim() == "OK").Click();

        Assert.False(cut.Instance.Namensfrage);
        Assert.Equal("Frischer Typ", angelegt);
        Assert.Contains("Frischer Typ", cut.Instance.Typliste);
    }

    // =================================================================================
    // Hilfen
    // =================================================================================

    /// <summary>
    /// Liest den Rumpf einer Regel aus <c>EPOS.UI/wwwroot/epos-ui.css</c> — derselbe
    /// Weg wie in <c>ListenrahmenTests</c>: Eine bunit-Probe sieht eine Stilregel nicht
    /// (Lehre W6‑B‑1), also wird das Blatt gelesen.
    /// </summary>
    private static string Stilblock(string selektor)
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);
        string css = File.ReadAllText(Path.Combine(d!.FullName, "EPOS.UI", "wwwroot", "epos-ui.css"));

        int a = css.IndexOf(selektor, StringComparison.Ordinal);
        Assert.True(a >= 0, $"Regel {selektor} steht nicht im Stilblatt");
        int e = css.IndexOf('}', a);
        Assert.True(e > a);
        return css.Substring(a + selektor.Length, e - a - selektor.Length);
    }
}

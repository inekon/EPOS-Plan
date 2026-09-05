using System.Globalization;
using AngleSharp.Dom;
using Bunit;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// Der Dialog „BHKW-Wirtschaftlichkeit" (Etappe B5b) — das FELDKARTEN-NETZ von Hand.
///
/// <para>Diese Tests sind der Ersatz fuer den Stapellauf des Formular-Generators: Die
/// Maske hat keine <c>Designer.cs</c> mehr, aus der eine Feldkarte zu ziehen waere.
/// Geprueft wird deshalb hier, Gruppe fuer Gruppe, dass die Felder der Feldkarte
/// <c>b5_feldkarte.md</c> § 1 vollstaendig da sind — Anzahl UND Beschriftung —, dazu
/// die festen Entscheide K1, K3 und K6, die Warn- und Kohaerenzzeilen an einem
/// praeparierten Datenstand und das Verhalten der Speichernleiste.</para>
///
/// <para>Gruppenreihenfolge im Aufbau (der Index in <see cref="Gruppe"/>):
/// 0 Anlagen · 1 Angaben der gewaehlten Anlage · 2 KWK-Zuschlag · 3 Energiesteuer ·
/// 4 Stromsteuer · 5 Kohaerenzpruefung · 6 Hilfsstrom · 7 Vorschau.</para>
/// </summary>
public class BhkwWirtschaftlichkeitDialogTests : BunitContext
{
    private const int STAMM = 1030;

    public BhkwWirtschaftlichkeitDialogTests()
    {
        // Die Beschriftungen kommen aus dem Ressourcenkatalog des Kerns; die
        // neutrale Datei ist deutsch, en-US liegt als Satellit daneben. Damit die
        // Erwartungen unabhaengig vom Rechner gelten, wird die Anzeigesprache
        // ausdruecklich auf Deutsch gestellt.
        var de = CultureInfo.GetCultureInfo("de-DE");
        CultureInfo.DefaultThreadCurrentCulture = de;
        CultureInfo.DefaultThreadCurrentUICulture = de;
        CultureInfo.CurrentCulture = de;
        CultureInfo.CurrentUICulture = de;

        // QuickGrid (im Raster) laedt beim ersten Zeichnen ein JS-Modul.
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    // =====================================================================
    // Pruefstand
    // =====================================================================

    private static KwkgAnlagenAngabe Anlage(int id, string bezeichner, double pel,
                                            string projekt = "Stamm", int idProjekt = STAMM,
                                            bool heizoel = false, string brennstoff = "Erdgas E")
        => new KwkgAnlagenAngabe
        {
            IdAnlage = id,
            IdProjekt = idProjekt,
            Projektname = projekt,
            Bezeichner = bezeichner,
            PelKW = pel,
            Brennstoffname = brennstoff,
            Heizoel = heizoel
        };

    private static List<KwkgAnlagenAngabe> ZweiAnlagen() => new List<KwkgAnlagenAngabe>
    {
        Anlage(14920, "BHKW EW M 50 S [K] Erdgas", 50),
        Anlage(14921, "EC-POWER XRGI 9", 9)
    };

    /// <summary>Ein Katalog, der genau die beiden Grenzwerte der Warnzeilen kennt.</summary>
    private static Func<string, int, GesetzParameter> Katalog(double ausschreibung, double stromsteuer)
        => (schluessel, jahr) =>
        {
            if (schluessel == DbWerte.GESETZ_KWKG_AUSSCHREIBUNG_GRENZE)
                return new GesetzParameter(1, schluessel, "KWKG", 2020, ausschreibung, "kW", "", "");
            if (schluessel == DbWerte.GESETZ_STROMST_GRENZE_BEFREIUNG)
                return new GesetzParameter(2, schluessel, "STROMST", 2020, stromsteuer, "kW", "", "");
            return null!;
        };

    private IRenderedComponent<BhkwWirtschaftlichkeitDialog> Aufbauen(
        IList<KwkgAnlagenAngabe>? anlagen = null,
        WirtschaftlichkeitParameter? parameter = null,
        Action<BhkwWirtschaftlichkeitErgebnis>? beimSchliessen = null,
        Func<int>? speichern = null,
        bool hatHeizkessel = false,
        IReadOnlyList<KohaerenzHinweis>? doppelpflege = null,
        IReadOnlyList<WirtschaftlichkeitErgebnis>? ausLauf = null,
        Func<string, int, GesetzParameter>? katalog = null)
    {
        return Render<BhkwWirtschaftlichkeitDialog>(p => p
            .Add(x => x.IdStamm, STAMM)
            .Add(x => x.StammName, "Musterprojekt")
            .Add(x => x.Anlagen, anlagen ?? ZweiAnlagen())
            .Add(x => x.Parameter, parameter ?? new WirtschaftlichkeitParameter())
            .Add(x => x.HatHeizkessel, hatHeizkessel)
            .Add(x => x.Doppelpflege, doppelpflege ?? Array.Empty<KohaerenzHinweis>())
            .Add(x => x.ErgebnisseAusLauf, ausLauf ?? Array.Empty<WirtschaftlichkeitErgebnis>())
            .Add(x => x.Katalog, katalog)
            .Add(x => x.Speichern, speichern)
            .Add(x => x.Geschlossen, beimSchliessen ?? (_ => { })));
    }

    private static IElement Gruppe(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, int nr)
        => cut.FindAll("section.epos-gruppenkopf")[nr];

    private static IElement Koerper(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, int nr)
        => Gruppe(cut, nr).QuerySelector("div.epos-gruppenkopf-koerper")!;

    private static List<string> Beschriftungen(IElement bereich)
    {
        var l = new List<string>();
        foreach (IElement e in bereich.QuerySelectorAll("span.epos-feld-text")) l.Add(e.TextContent);
        return l;
    }

    private static int Zahlenfelder(IElement bereich)
        => bereich.QuerySelectorAll("input[inputmode=decimal]").Length;

    private static int Auswahlfelder(IElement bereich) => bereich.QuerySelectorAll("select").Length;

    private static int Datumsfelder(IElement bereich)
        => bereich.QuerySelectorAll("input[type=date]").Length;

    private static int Schalter(IElement bereich)
        => bereich.QuerySelectorAll("input[type=checkbox]").Length;

    // =====================================================================
    // Rahmen
    // =====================================================================

    [Fact]
    public void Der_Titel_nennt_die_Maske_und_das_Stammprojekt()
    {
        var cut = Aufbauen();

        Assert.Equal("BHKW-Wirtschaftlichkeit — Musterprojekt",
                     cut.Find(".epos-dialog-titel").TextContent);
    }

    [Fact]
    public void Der_Hilfeknopf_traegt_den_Schluessel_der_Maske()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Aufbauen();
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_BhkwWirtschaftlichkeit.btn_Help" }, hilfe.Geoeffnet);
    }

    [Fact]
    public void Die_acht_Abschnitte_der_Feldkarte_stehen_in_der_Reihenfolge_des_Aufbaus()
    {
        var cut = Aufbauen();
        var titel = new List<string>();
        foreach (IElement e in cut.FindAll("h2.epos-gruppenkopf-titel")) titel.Add(e.TextContent);

        Assert.Equal(new[]
        {
            "Anlagen",
            "Angaben der gewählten Anlage — leer bzw. 0 = Projektvorgabe",
            "KWK-Zuschlag (Projektvorgabe)",
            "Energiesteuer (Projektvorgabe)",
            "Stromsteuer (Projektvorgabe)",
            "Kohärenzprüfung (Energie- und Stromsteuer)",
            "Hilfsstrom",
            "Vorschau — zuletzt gebuchter Lauf"
        }, titel);
    }

    // =====================================================================
    // Gruppe 1 — Anlagen (Feldkarte 1.1 bis 1.6, dazu A-1 „Projekt")
    // =====================================================================

    [Fact]
    public void Gruppe1_fuehrt_die_sieben_Spalten_der_Feldkarte_und_die_Wahlspalte()
    {
        var cut = Aufbauen();
        var kopf = new List<string>();
        foreach (IElement e in Koerper(cut, 0).QuerySelectorAll("thead th")) kopf.Add(e.TextContent.Trim());

        Assert.Equal(new[]
        {
            "Wahl", "Projekt", "Anlage", "P_el [kW]", "Brennstoff",
            "Stichtag", "Inbetriebnahme", "Anlagenart"
        }, kopf);
    }

    [Fact]
    public void Gruppe1_zeigt_jede_Anlage_als_Zeile_mit_ihren_Werten()
    {
        var anlagen = ZweiAnlagen();
        anlagen[0].Stichtag = new DateTime(2026, 3, 17);
        anlagen[0].Anlagenart = DbWerte.KWKG_ANLAGENART_MODERNISIERT;
        var cut = Aufbauen(anlagen);

        var zeilen = Koerper(cut, 0).QuerySelectorAll("tbody tr");
        Assert.Equal(2, zeilen.Length);

        string erste = zeilen[0].TextContent;
        Assert.Contains("Stamm", erste);
        Assert.Contains("BHKW EW M 50 S [K] Erdgas", erste);
        Assert.Contains("50", erste);
        Assert.Contains("Erdgas E", erste);
        Assert.Contains("17.03.2026", erste);
        Assert.Contains("modernisiert (§ 8 Abs. 2)", erste);
        // Ohne Inbetriebnahme steht der Gedankenstrich, nicht eine leere Zelle.
        Assert.Contains("—", erste);
    }

    [Fact]
    public void Die_erste_Anlage_ist_vorgewaehlt_und_die_Wahl_laesst_sich_umstellen()
    {
        var anlagen = ZweiAnlagen();
        var cut = Aufbauen(anlagen);

        Assert.Same(anlagen[0], cut.Instance.Aktuelle);

        cut.FindAll("button.epos-anlagenwahl")[1].Click();

        Assert.Same(anlagen[1], cut.Instance.Aktuelle);
    }

    [Fact]
    public void Ohne_Anlagen_bleibt_die_Feldgruppe_leer_und_sagt_es()
    {
        var cut = Aufbauen(new List<KwkgAnlagenAngabe>());

        Assert.Null(cut.Instance.Aktuelle);
        Assert.Contains("Keine Anlage gewählt.", Koerper(cut, 1).TextContent);
        Assert.Equal(0, Zahlenfelder(Koerper(cut, 1)));
    }

    [Fact]
    public void Die_drei_Warnzeilen_der_Gruppe1_erscheinen_am_praeparierten_Stand()
    {
        var anlagen = new List<KwkgAnlagenAngabe>
        {
            Anlage(1, "Grosses BHKW", 600),
            Anlage(2, "Sehr grosses BHKW", 2500, heizoel: true, brennstoff: "Heizöl Bio 10")
        };
        anlagen[1].Inbetriebnahme = new DateTime(2025, 1, 1);

        var cut = Aufbauen(anlagen, katalog: Katalog(500, 2000));

        var banner = new List<string>();
        foreach (IElement e in Koerper(cut, 0).QuerySelectorAll(".epos-warnbanner-text"))
            banner.Add(e.TextContent);

        Assert.Equal(3, banner.Count);
        Assert.Equal("Ausschreibung nach § 8a KWKG: Grosses BHKW, Sehr grosses BHKW über 500 kW.",
                     banner[0]);
        Assert.Equal("Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 entfällt: Sehr grosses BHKW über 2.000 kW.",
                     banner[1]);
        Assert.Equal("Heizöl-Ausschluss ab Inbetriebnahme 2025: Sehr grosses BHKW.", banner[2]);
    }

    [Fact]
    public void Ohne_Katalog_gelten_die_Rueckfallgrenzen_500_und_2000()
    {
        var anlagen = new List<KwkgAnlagenAngabe> { Anlage(1, "Grosses BHKW", 600) };
        var cut = Aufbauen(anlagen);   // Katalog = null

        Assert.Contains("über 500 kW.", Koerper(cut, 0).QuerySelector(".epos-warnbanner-text")!.TextContent);
    }

    // =====================================================================
    // Gruppe 1b — die elf Angaben der Anlage (Feldkarte 1.7 bis 1.17)
    // =====================================================================

    [Fact]
    public void Gruppe1b_fuehrt_genau_die_elf_Felder_der_Feldkarte()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 1);

        Assert.Equal(2, Datumsfelder(g));     // 1.7  1.8
        Assert.Equal(4, Auswahlfelder(g));    // 1.9  1.10  1.15  1.16
        Assert.Equal(5, Zahlenfelder(g));     // 1.11 1.12  1.13  1.14  1.17
        Assert.Equal(0, Schalter(g));

        Assert.Equal(new[]
        {
            "Stichtag (Bestellung/Genehmigung):",
            "Inbetriebnahme:",
            "Anlagenart:",
            "Eigenstrom nach § 6 Abs. 3:",
            "Satz Einspeisung [ct/kWh] (0 = Projektsatz):",
            "Satz Eigenstrom [ct/kWh] (0 = Projektsatz):",
            "Vbh-Kontingent [h] (0 = Projektwert):",
            "Vbh-Jahresdeckel [h/a] (0 = Staffel):",
            "Energiesteuerentlastung (Anlage):",
            "Brennstoff auf Strom/Wärme (Anlage):",
            "Hilfsenergieanteil [% des Endenergiebedarfs] (0 = keine):"
        }, Beschriftungen(g));
    }

    [Fact]
    public void Die_Anlagenlisten_tragen_ihre_Steuerwerte_und_den_Leereintrag()
    {
        var cut = Aufbauen();
        var listen = Koerper(cut, 1).QuerySelectorAll("select");

        // 1.9 Anlagenart: leer = "(nicht erfasst — gilt als Neuanlage)"
        Assert.Equal("(nicht erfasst — gilt als Neuanlage)",
                     listen[0].QuerySelectorAll("option")[0].TextContent);
        Assert.Equal(4, listen[0].QuerySelectorAll("option").Length);

        // 1.10 Eigenstromfall: KEIN Leereintrag an der Anlage
        Assert.Equal("kein Tatbestand (kein Eigenstromzuschlag)",
                     listen[1].QuerySelectorAll("option")[0].TextContent);

        // 1.15 / 1.16 an der Anlage: leer heisst "(Projektwert)" (B3a)
        Assert.Equal("(Projektwert)", listen[2].QuerySelectorAll("option")[0].TextContent);
        Assert.Equal("(Projektwert)", listen[3].QuerySelectorAll("option")[0].TextContent);
    }

    [Fact]
    public void Eine_Eingabe_landet_in_der_Anlagenzeile_und_0_heisst_Projektwert()
    {
        var anlagen = ZweiAnlagen();
        var cut = Aufbauen(anlagen);
        var zahlen = Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]");

        zahlen[0].Input("5,57");                       // Satz Einspeisung
        Assert.Equal(5.57, anlagen[0].SatzEinspCt);

        zahlen[0].Input("0");                          // 0 = kein eigener Wert
        Assert.Null(anlagen[0].SatzEinspCt);

        // Beim Hilfsenergieanteil ist 0 ein GUELTIGER Wert (BF4).
        zahlen[4].Input("3,5");
        Assert.Equal(3.5, anlagen[0].HilfsenergieAnteil);
        zahlen[4].Input("0");
        Assert.Equal(0.0, anlagen[0].HilfsenergieAnteil);
    }

    [Fact]
    public void Datum_und_Auswahl_der_Anlage_werden_uebernommen()
    {
        var anlagen = ZweiAnlagen();
        var cut = Aufbauen(anlagen);
        IElement g = Koerper(cut, 1);

        g.QuerySelectorAll("input[type=date]")[0].Change("2026-03-17");
        Assert.Equal(new DateTime(2026, 3, 17), anlagen[0].Stichtag);

        g.QuerySelectorAll("input[type=date]")[0].Change("");
        Assert.Null(anlagen[0].Stichtag);

        Koerper(cut, 1).QuerySelectorAll("select")[0].Change("2");
        Assert.Equal(DbWerte.KWKG_ANLAGENART_MODERNISIERT, anlagen[0].Anlagenart);

        Koerper(cut, 1).QuerySelectorAll("select")[2].Change("3");
        Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_53A, anlagen[0].EnergiesteuerWahl);
    }

    // =====================================================================
    // Gruppe 2 — KWK-Zuschlag (Feldkarte 2.1 bis 2.11)
    // =====================================================================

    [Fact]
    public void Gruppe2_fuehrt_genau_die_elf_Projektfelder_der_Feldkarte()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 2);

        Assert.Equal(6, Zahlenfelder(g));      // 2.1 2.2 2.3 2.4 2.5 2.8
        Assert.Equal(2, Auswahlfelder(g));     // 2.6 2.7
        Assert.Equal(1, Schalter(g));          // 2.9
        Assert.Equal(2, Datumsfelder(g));      // 2.10 2.11

        Assert.Equal(new[]
        {
            "Bonus Eigenstrom [ct/kWh] (0 = aus):",
            "Bonus Einspeisung [ct/kWh]:",
            "Vbh-Deckel-Override [h/a]:",
            "Vbh-Kontingent gesamt [h] (0 = automatisch):",
            "Abschlag Negativstunden [%]:",
            "Eigenstrom-Tatbestand (§ 6 Abs. 3):",
            "Anlagenart (§ 8):",
            "Anteil Neuherstellungskosten [%]:",
            "Pauschale § 9 KWKG (nur bis 2 kWel, einmalig)",
            "Stichtag, Vorgabe je Anlage:",
            "Inbetriebnahme, Vorgabe je Anlage:"
        }, Beschriftungen(g));
    }

    [Fact]
    public void Die_Projektlisten_tragen_den_Leereintrag_nicht_angegeben()
    {
        var cut = Aufbauen();
        var listen = Koerper(cut, 2).QuerySelectorAll("select");

        Assert.Equal("(nicht angegeben)", listen[0].QuerySelectorAll("option")[0].TextContent);
        Assert.Equal("(nicht angegeben)", listen[1].QuerySelectorAll("option")[0].TextContent);
    }

    [Fact]
    public void Gruppe2_zeigt_die_Herleitung_des_Katalogvorschlags_und_uebernimmt_ihn_auf_Knopfdruck()
    {
        var anlagen = new List<KwkgAnlagenAngabe> { Anlage(1, "BHKW 50", 50) };
        anlagen[0].Inbetriebnahme = new DateTime(2027, 5, 4);
        anlagen[0].Anlagenart = DbWerte.KWKG_ANLAGENART_NEU;

        // Ein Katalog, der jeden Schluessel beantwortet - der Vorschlag kommt dann
        // ohne Luecke zustande.
        Func<string, int, GesetzParameter> katalog = (schluessel, jahr) =>
            new GesetzParameter(1, schluessel, "KWKG", 2020, 16.0, "ct/kWh", "", "");

        var cut = Aufbauen(anlagen, katalog: katalog);

        string text = Koerper(cut, 2).TextContent;
        Assert.Contains("Einspeisung", text);
        Assert.Contains("Eigenstrom", text);

        Assert.Null(anlagen[0].SatzEinspCt);
        cut.Find("button.epos-vorschlag").Click();

        // Die Zahl gehoert dem Bestandsrechner, nicht dem Dialog: Erwartet wird genau
        // das, was KwkgSatzRechner mit denselben Angaben liefert.
        KwkgSatzVorschlag soll = KwkgSatzRechner.Vorschlag(
            50, 2027, DbWerte.KWKG_ANLAGENART_NEU, "", katalog,
            CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal(soll.SatzEinspeisungCt, anlagen[0].SatzEinspCt);
        // Ohne Tatbestand nach § 6 Abs. 3 ist der Eigenstromsatz 0 - und 0 heisst
        // an der Anlage "kein eigener Wert".
        Assert.Equal(0.0, soll.SatzEigenCt);
        Assert.Null(anlagen[0].SatzEigenCt);
        Assert.True(cut.Instance.Geaendert);
    }

    [Fact]
    public void Ohne_gewaehlte_Anlage_ist_der_Vorschlagsknopf_gesperrt()
    {
        var cut = Aufbauen(new List<KwkgAnlagenAngabe>());

        Assert.True(cut.Find("button.epos-vorschlag").HasAttribute("disabled"));
        Assert.Empty(Koerper(cut, 2).QuerySelectorAll("p.epos-herleitung"));
    }

    // =====================================================================
    // Gruppe 3 — Energiesteuer (Feldkarte 3.1 bis 3.3)
    // =====================================================================

    [Fact]
    public void Gruppe3_fuehrt_genau_die_drei_Felder_der_Feldkarte()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 3);

        Assert.Equal(2, Auswahlfelder(g));
        Assert.Equal(1, Zahlenfelder(g));
        Assert.Equal(new[]
        {
            "Energiesteuerentlastung:",
            "Brennstoff auf Strom/Wärme:",
            "Jahresnutzungsgrad [%] (0 = nicht erfasst):"
        }, Beschriftungen(g));

        // Die Projektlisten haben KEINEN Leereintrag (K5/B3a: hier gilt immer ein Wert).
        var listen = g.QuerySelectorAll("select");
        Assert.Equal("keine", listen[0].QuerySelectorAll("option")[0].TextContent);
        Assert.Equal("voller BHKW-Brennstoff (§ 53 Abs. 2)",
                     listen[1].QuerySelectorAll("option")[0].TextContent);
    }

    [Fact]
    public void Ohne_Lauf_sagt_Gruppe3_dass_kein_Satz_verwendet_wurde()
    {
        var cut = Aufbauen();

        Assert.Contains("Keine Gutschrift im zuletzt gebuchten Lauf",
                        Koerper(cut, 3).TextContent);
    }

    [Fact]
    public void Mit_Lauf_zeigt_Gruppe3_die_Satzherkunft_des_Laufs()
    {
        var lauf = new[]
        {
            new WirtschaftlichkeitErgebnis
            {
                IdProjekt = STAMM,
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                SteuerHerkunft = "§ 53a Abs. 5 · Erdgas 4,42 €/MWh · gültig ab 2024"
            }
        };
        var cut = Aufbauen(ausLauf: lauf);

        Assert.Contains("§ 53a Abs. 5 · Erdgas 4,42 €/MWh · gültig ab 2024",
                        Koerper(cut, 3).TextContent);
    }

    // =====================================================================
    // Gruppe 4 — Stromsteuer (Feldkarte 4.1 bis 4.5) und K3
    // =====================================================================

    [Fact]
    public void Gruppe4_fuehrt_die_vier_Felder_und_beide_Sprungknoepfe()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 4);

        Assert.Equal(2, Auswahlfelder(g));     // 4.1 und 4.4
        Assert.Equal(2, Schalter(g));          // 4.2 und 4.3
        Assert.Equal(new[]
        {
            "Unternehmensart:",
            "Räumlicher Zusammenhang (4,5 km) gegeben",
            "Hocheffizienz nachgewiesen",
            "Modus § 9 Abs. 1 Nr. 3:"
        }, Beschriftungen(g));

        var sprung = g.QuerySelectorAll("button.epos-sprung");
        Assert.Equal(2, sprung.Length);
        Assert.Equal("Strombezug…", sprung[0].TextContent.Trim());
        Assert.Equal("BHKW-Tarif…", sprung[1].TextContent.Trim());
    }

    [Fact]
    public void K3_das_Modusfeld_ist_sichtbar_aber_gesperrt_und_traegt_den_B6_Vermerk()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 4);
        IElement modus = g.QuerySelectorAll("select")[1];

        Assert.True(modus.HasAttribute("disabled"));
        var eintraege = modus.QuerySelectorAll("option");
        Assert.Equal(2, eintraege.Length);
        Assert.Equal("Ausweis (nicht im Kapitalwert)", eintraege[0].TextContent);
        Assert.Equal("Erlös (im Kapitalwert)", eintraege[1].TextContent);
        Assert.Contains("ab B6 — bis dahin gilt fest „Ausweis“ (nicht im Kapitalwert).", g.TextContent);
    }

    [Fact]
    public void Die_Stromsteuerfelder_schreiben_in_den_Parametersatz()
    {
        var p = new WirtschaftlichkeitParameter();
        var cut = Aufbauen(parameter: p);
        IElement g = Koerper(cut, 4);

        g.QuerySelectorAll("select")[0].Change("1");
        Assert.Equal(DbWerte.UNTERNEHMENSART_PROD_GEWERBE, p.Unternehmensart);

        g.QuerySelectorAll("input[type=checkbox]")[0].Change(true);
        Assert.True(p.RaeumlicherZusammenhang);

        g.QuerySelectorAll("input[type=checkbox]")[1].Change(true);
        Assert.True(p.HocheffizienzNachweis);
    }

    // =====================================================================
    // Kohaerenzpruefung (A-2) und Gruppe 5 — Hilfsstrom
    // =====================================================================

    [Fact]
    public void Ohne_Auffaelligkeit_meldet_der_Kohaerenzblock_Entwarnung()
    {
        var cut = Aufbauen();

        Assert.Contains("Keine Auffälligkeit im zuletzt gebuchten Lauf.", Koerper(cut, 5).TextContent);
        Assert.NotNull(Koerper(cut, 5).QuerySelector(".epos-kohaerenz--ok"));
    }

    [Fact]
    public void Die_steuerlichen_Zeilen_des_Laufs_stehen_im_Kohaerenzblock_die_Doppelpflege_in_Gruppe5()
    {
        const string doppel = "Hilfsenergie doppelt gepflegt (Menge an der Anlage und Kostenposition).";
        var lauf = new[]
        {
            new WirtschaftlichkeitErgebnis
            {
                IdProjekt = STAMM,
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                KohaerenzHinweise = new List<KohaerenzHinweis>
                {
                    new KohaerenzHinweis { Schwere = KohaerenzSchwere.WARNUNG,
                                           Text = "Die Energiesteuer-Gutschrift von 6.330,30 €/a …" },
                    new KohaerenzHinweis { Schwere = KohaerenzSchwere.HINWEIS,
                                           Text = "Der Schalter Aufschläge ist aus." },
                    // dieselbe Zeile, die auch die laufunabhaengige Pruefung liefert
                    new KohaerenzHinweis { Schwere = KohaerenzSchwere.WARNUNG, Text = doppel }
                }
            }
        };
        var pruefung = new[] { new KohaerenzHinweis { Schwere = KohaerenzSchwere.WARNUNG, Text = doppel } };

        var cut = Aufbauen(doppelpflege: pruefung, ausLauf: lauf);

        var koh = new List<string>();
        foreach (IElement e in Koerper(cut, 5).QuerySelectorAll(".epos-warnbanner-text"))
            koh.Add(e.TextContent);
        Assert.Equal(new[]
        {
            "Die Energiesteuer-Gutschrift von 6.330,30 €/a …",
            "Der Schalter Aufschläge ist aus."
        }, koh);

        // Die Doppelpflege steht GENAU EINMAL, und zwar in Gruppe 5.
        var hilfs = new List<string>();
        foreach (IElement e in Koerper(cut, 6).QuerySelectorAll(".epos-warnbanner-text"))
            hilfs.Add(e.TextContent);
        Assert.Equal(new[] { doppel }, hilfs);
    }

    [Fact]
    public void K1_es_gibt_kein_Feld_Deckung_je_Modul()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain("Deckung", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void K6_der_Anteil_ist_nur_am_BHKW_pflegbar_und_der_Kessel_bekommt_den_Hinweis()
    {
        Assert.True(BhkwWirtschaftlichkeitDialog.AnteilPflegbar(WizardItemClass.BHKW_TYP));
        Assert.False(BhkwWirtschaftlichkeitDialog.AnteilPflegbar(WizardItemClass.KESSEL_TYP));
        Assert.False(BhkwWirtschaftlichkeitDialog.AnteilPflegbar(1));

        Assert.False(BhkwWirtschaftlichkeitDialog.AnteilHinweis(WizardItemClass.BHKW_TYP));
        Assert.True(BhkwWirtschaftlichkeitDialog.AnteilHinweis(WizardItemClass.KESSEL_TYP));
        Assert.False(BhkwWirtschaftlichkeitDialog.AnteilHinweis(1));

        var ohneKessel = Aufbauen();
        Assert.True(ohneKessel.Instance.AnteilSichtbar);
        Assert.False(ohneKessel.Instance.Kesselhinweis);
        Assert.DoesNotContain("Heizkessel der Gruppe", Koerper(ohneKessel, 6).TextContent);

        var mitKessel = Aufbauen(hatHeizkessel: true);
        Assert.True(mitKessel.Instance.Kesselhinweis);
        Assert.Contains("Heizkessel der Gruppe", Koerper(mitKessel, 6).TextContent);
    }

    [Fact]
    public void Gruppe5_erklaert_die_Bemessungsbasis_und_zeigt_ohne_Lauf_den_Hinweis()
    {
        var cut = Aufbauen();
        string text = Koerper(cut, 6).TextContent;

        Assert.Contains("ENDENERGIEBEDARF", text);
        Assert.Contains("Mengenkette: noch kein gebuchtes Ergebnis", text);
    }

    [Fact]
    public void Gruppe5_zeigt_die_Mengenkette_des_gebuchten_Laufs()
    {
        var anlagen = ZweiAnlagen();
        var lauf = new[]
        {
            new WirtschaftlichkeitErgebnis
            {
                IdProjekt = STAMM,
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                KwkgModule = new List<KwkgModulNachweis>
                {
                    new KwkgModulNachweis
                    {
                        Bezeichner = "BHKW EW M 50 S [K] Erdgas",
                        StromBruttoMWh = 373.78, HilfsstromMWh = 0, StromNettoMWh = 373.78,
                        EigenMWh = 373.78, EinspeisungMWh = 0
                    }
                }
            }
        };
        var cut = Aufbauen(anlagen, ausLauf: lauf);

        string text = Koerper(cut, 6).TextContent;
        Assert.Contains("Stromerzeugung brutto 373,780 MWh/a − Hilfsstrom 0,000 MWh/a = " +
                        "Nettostromerzeugung 373,780 MWh/a", text);
        Assert.Contains("davon Eigenverbrauch 373,780 MWh/a, Einspeisung 0,000 MWh/a", text);
    }

    // =====================================================================
    // Gruppe 6 — Vorschau (BW8: gebuchter Stand, keine Zweitrechnung)
    // =====================================================================

    [Fact]
    public void Ohne_Lauf_sagt_die_Vorschau_dass_zu_rechnen_ist()
    {
        var cut = Aufbauen();

        Assert.Contains("Noch kein gebuchtes Ergebnis", Koerper(cut, 7).TextContent);
    }

    [Fact]
    public void Die_Vorschau_zeigt_die_fuenf_Jahr1_Zeilen_des_gebuchten_Laufs()
    {
        var lauf = new[]
        {
            new WirtschaftlichkeitErgebnis
            {
                IdProjekt = STAMM,
                Szenario = WirtschaftlichkeitSzenario.ERWARTET,
                KwkgErloesJahr1 = 7316,
                EnergiesteuerJahr1 = 5119,
                StromsteuerBefreiungJahr1 = 86000,
                StromsteuerEntlastungJahr1 = 906,
                EinspeiseerloesKwkJahr = 1234,
                VermiedenGesamtJahr = 4321,
                Zeitstempel = new DateTime(2026, 9, 3, 12, 3, 0)
            }
        };
        var cut = Aufbauen(ausLauf: lauf);

        var zeilen = new List<string>();
        foreach (IElement e in Koerper(cut, 7).QuerySelectorAll("p.epos-herleitung"))
            zeilen.Add(e.TextContent.Trim());

        Assert.Equal(6, zeilen.Count);
        Assert.Contains("KWK-Zuschlag p. a.", zeilen[0]);
        Assert.Contains("7.316 €", zeilen[0]);
        Assert.Contains("5.119 €", zeilen[1]);
        // Befreiung + Entlastung stehen in EINER Zeile (Bestandsverhalten).
        Assert.Contains("86.906 €", zeilen[2]);
        Assert.Contains("1.234 €", zeilen[3]);
        Assert.Contains("4.321 €", zeilen[4]);
        Assert.Contains("nach dem Speichern neu berechnen", zeilen[5]);
    }

    // =====================================================================
    // Speichernleiste
    // =====================================================================

    [Fact]
    public void Die_Leiste_zeigt_Speichern_und_Schliessen_aber_kein_Abbrechen()
    {
        var cut = Aufbauen();
        var knoepfe = cut.FindAll(".epos-leiste button");

        Assert.Equal(2, knoepfe.Count);
        Assert.Equal("Speichern", knoepfe[0].TextContent);
        Assert.Equal("Schließen", knoepfe[1].TextContent);
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassName);
    }

    [Fact]
    public void Speichern_ist_erst_nach_einer_Aenderung_moeglich()
    {
        var cut = Aufbauen();

        Assert.True(cut.FindAll(".epos-leiste button")[0].HasAttribute("disabled"));

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");

        Assert.False(cut.FindAll(".epos-leiste button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_gelungenes_Speichern_meldet_sich_in_der_Statuszeile_und_schliesst_nicht()
    {
        int aufrufe = 0;
        bool geschlossen = false;
        var cut = Aufbauen(speichern: () => { aufrufe++; return 0; },
                           beimSchliessen: _ => geschlossen = true);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        cut.FindAll(".epos-leiste button")[0].Click();

        Assert.Equal(1, aufrufe);
        Assert.False(geschlossen);
        Assert.True(cut.Instance.Gespeichert);
        Assert.False(cut.Instance.Geaendert);
        Assert.Contains("gespeichert", cut.Find(".epos-status").TextContent,
                        StringComparison.OrdinalIgnoreCase);
        // Nach dem Speichern gibt es nichts mehr zu speichern.
        Assert.True(cut.FindAll(".epos-leiste button")[0].HasAttribute("disabled"));
    }

    [Fact]
    public void Ein_gescheitertes_Speichern_zeigt_das_Warnbanner_statt_einer_MessageBox()
    {
        var cut = Aufbauen(speichern: () => 2);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        cut.FindAll(".epos-leiste button")[0].Click();

        Assert.False(cut.Instance.Gespeichert);
        Assert.Equal("2 Angabe(n) konnten nicht gespeichert werden.",
                     cut.FindAll(".epos-warnbanner-text")[^1].TextContent);
        Assert.Contains("epos-warnbanner--fehler", cut.FindAll(".epos-warnbanner")[^1].ClassName);
        Assert.True(cut.Find(".epos-status--fehler") is not null);
    }

    // =====================================================================
    // Schliessen und Sprung
    // =====================================================================

    [Fact]
    public void Schliessen_meldet_das_Ergebnis_ohne_Sprung()
    {
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(beimSchliessen: e => ergebnis = e);

        cut.FindAll(".epos-leiste button")[1].Click();

        Assert.NotNull(ergebnis);
        Assert.False(ergebnis!.Gespeichert);
        Assert.Equal(BhkwSprung.Keiner, ergebnis.Sprung);
    }

    [Fact]
    public void Esc_schliesst_ebenfalls()
    {
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(beimSchliessen: e => ergebnis = e);

        cut.Find("div.epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.NotNull(ergebnis);
        Assert.Equal(BhkwSprung.Keiner, ergebnis!.Sprung);
    }

    [Fact]
    public void Die_beiden_Sprungknoepfe_melden_ihr_Ziel_an_die_Huelle()
    {
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(beimSchliessen: e => ergebnis = e);

        cut.FindAll("button.epos-sprung")[0].Click();
        Assert.Equal(BhkwSprung.Strombezug, ergebnis!.Sprung);

        var cut2 = Aufbauen(beimSchliessen: e => ergebnis = e);
        cut2.FindAll("button.epos-sprung")[1].Click();
        Assert.Equal(BhkwSprung.BhkwTarif, ergebnis!.Sprung);
    }

    [Fact]
    public void Nach_dem_Speichern_traegt_das_Ergebnis_die_Speichermeldung()
    {
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(speichern: () => 0, beimSchliessen: e => ergebnis = e);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        cut.FindAll(".epos-leiste button")[0].Click();
        cut.FindAll(".epos-leiste button")[1].Click();

        Assert.True(ergebnis!.Gespeichert);
    }

    [Fact]
    public void Das_Wurzelelement_nimmt_den_Fokus_auf()
    {
        var cut = Aufbauen();

        Assert.Equal("-1", cut.Find("div.epos-dialog").GetAttribute("tabindex"));
    }

    // =====================================================================
    //  Das Formularraster — Anwenderwunsch iU8-E-2 / W14a-E-7, Paket P2
    //  (Windows-Abnahme 05.09.2026)
    // =====================================================================


    /// <summary>
    /// <b>iU8-E-2 / W14a-E-7 (Paket P2):</b> Die vier Parameterblöcke (Angaben der
    /// gewählten Anlage, KWK-Zuschlag, Energiesteuer, Stromsteuer) stehen im
    /// <c>Formularraster</c>. Das Anlagenraster der Gruppe 1 bleibt ein
    /// DATENraster — seine Felder stehen in Tabellenzellen und gehören nicht in
    /// einen Formularblock; Herleitungszeilen und Sprungknöpfe bleiben ausserhalb.
    /// </summary>
    [Fact]
    public void Die_Parameterbloecke_stehen_im_Formularraster()
    {
        var cut = Aufbauen();

        Assert.True(cut.FindAll(".epos-formularraster").Count >= 3);
        Assert.True(cut.FindAll(".epos-formularraster .epos-feld--kurz").Count > 0);

        // ct/kWh, h, h/a, %: die Einheit steht in der Feldzeile des kurzen Feldes.
        Assert.Contains(cut.FindAll(".epos-formularraster .epos-feld--kurz"),
                        f => f.QuerySelector(".epos-feld-zeile .epos-einheit") is not null);
    }
}

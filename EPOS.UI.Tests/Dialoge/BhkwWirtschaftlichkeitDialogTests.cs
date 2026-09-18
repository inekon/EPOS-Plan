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
public class BhkwWirtschaftlichkeitDialogTests : EposBunitContext
{
    private const int STAMM = 1030;

    public BhkwWirtschaftlichkeitDialogTests()
    {
        // Die Beschriftungen kommen aus dem Ressourcenkatalog des Kerns; die
        // neutrale Datei ist deutsch, en-US liegt als Satellit daneben. Damit die
        // Erwartungen unabhaengig vom Rechner gelten, wird die Anzeigesprache
        // ausdruecklich auf Deutsch gestellt.
        var de = CultureInfo.GetCultureInfo("de-DE");

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
        Func<KwkgAnlagenAngabe, bool>? speichereAnlage = null,
        Func<WirtschaftlichkeitParameter, bool>? speichereVorgaben = null,
        bool hatHeizkessel = false,
        IReadOnlyList<KohaerenzHinweis>? doppelpflege = null,
        IReadOnlyList<WirtschaftlichkeitErgebnis>? ausLauf = null,
        Func<IReadOnlyList<int>, IReadOnlyList<WirtschaftlichkeitErgebnis>>? ergebnisseLaden = null,
        Func<string, int, GesetzParameter>? katalog = null,
        bool titelAnzeigen = true)
    {
        return Render<BhkwWirtschaftlichkeitDialog>(p => p
            .Add(x => x.IdStamm, STAMM)
            .Add(x => x.StammName, "Musterprojekt")
            .Add(x => x.Anlagen, anlagen ?? ZweiAnlagen())
            .Add(x => x.Parameter, parameter ?? new WirtschaftlichkeitParameter())
            .Add(x => x.HatHeizkessel, hatHeizkessel)
            .Add(x => x.Doppelpflege, doppelpflege ?? Array.Empty<KohaerenzHinweis>())
            .Add(x => x.ErgebnisseAusLauf, ausLauf ?? Array.Empty<WirtschaftlichkeitErgebnis>())
            .Add(x => x.ErgebnisseLaden, ergebnisseLaden)
            .Add(x => x.Katalog, katalog)
            .Add(x => x.SpeichereAnlage, speichereAnlage)
            .Add(x => x.SpeichereVorgaben, speichereVorgaben)
            .Add(x => x.TitelAnzeigen, titelAnzeigen)
            .Add(x => x.Geschlossen, beimSchliessen ?? (_ => { })));
    }

    /// <summary>
    /// Ein SCHREIBZAEHLER: Er merkt sich jeden Aufruf der beiden Schreibwege in der
    /// Reihenfolge, in der er kam. Die Abnahme dieses Dialogs misst den
    /// DATENBANKSTAND, nicht die Anzeige — also wird gezaehlt, was geschrieben wurde.
    /// </summary>
    private sealed class Schreibzaehler
    {
        private readonly List<string> _wege = new List<string>();

        /// <summary>Was die Anlagenzeile antworten soll; <c>null</c> = immer gelungen.</summary>
        internal Func<KwkgAnlagenAngabe, bool>? AnlageAntwort { get; set; }

        /// <summary>Was die Projektvorgaben antworten sollen; <c>null</c> = gelungen.</summary>
        internal Func<WirtschaftlichkeitParameter, bool>? VorgabenAntwort { get; set; }

        /// <summary>Die Schreibzugriffe in ihrer Reihenfolge („Anlage:BHKW 50", „Vorgaben").</summary>
        internal IReadOnlyList<string> Wege => _wege;

        /// <summary>Die Zahl der Schreibzugriffe.</summary>
        internal int Zugriffe => _wege.Count;

        internal bool Anlage(KwkgAnlagenAngabe a)
        {
            _wege.Add("Anlage:" + a.Bezeichner);
            return AnlageAntwort is null || AnlageAntwort(a);
        }

        internal bool Vorgaben(WirtschaftlichkeitParameter p)
        {
            _wege.Add("Vorgaben");
            return VorgabenAntwort is null || VorgabenAntwort(p);
        }
    }

    private static IElement OkKnopf(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut)
        => cut.FindAll(".epos-leiste button")[1];

    private static IElement AbbrechenKnopf(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut)
        => cut.FindAll(".epos-leiste button")[0];

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

    /// <summary>
    /// Ein Titel, eine Stelle (W11b‑B‑9): Zeigt der Wirt schon einen — die
    /// Überlagerung der Wirtschaftlichkeitsseite tut es —, bleibt der eigene Kopf
    /// weg; der Hilfeknopf bleibt.
    /// </summary>
    [Fact]
    public void Ohne_TitelAnzeigen_bleibt_der_eigene_Kopf_weg_und_der_Hilfeknopf_steht()
    {
        var cut = Aufbauen(titelAnzeigen: false);

        Assert.Empty(cut.FindAll("h1.epos-dialog-titel"));
        Assert.Contains("epos-dialog-kopf--ohnetitel", cut.Find("div.epos-dialog-kopf").ClassName);
        Assert.NotNull(cut.Find(".epos-infoknopf"));
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
            "Angaben der gewählten Anlage",
            "Projektweite KWK-Angaben",
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

    /// <summary>
    /// ETAPPE BK1 (Entscheid BK-E-1 a): ZWOELF Felder — der Anteil an den
    /// Neuherstellungskosten ist dazugekommen, weil § 8 Abs. 2/3 die Kontingentstufe
    /// aus IHM und der Anlagenart waehlt (Schemaschritt 89). Und drei Beschriftungen
    /// sagen nicht mehr „Projektsatz"/„Projektwert": Es gibt keinen Rueckfall mehr.
    /// </summary>
    [Fact]
    public void Gruppe1b_fuehrt_genau_die_zwoelf_Felder_der_Feldkarte()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 1);

        Assert.Equal(2, Datumsfelder(g));     // 1.7  1.8
        Assert.Equal(4, Auswahlfelder(g));    // 1.9  1.10  1.15  1.16
        Assert.Equal(6, Zahlenfelder(g));     // 1.11 1.12  1.13  1.14  BK1  1.17
        Assert.Equal(0, Schalter(g));

        Assert.Equal(new[]
        {
            "Stichtag (Bestellung/Genehmigung):",
            "Inbetriebnahme:",
            "Anlagenart:",
            "Eigenstrom nach § 6 Abs. 3:",
            "Satz Einspeisung [ct/kWh] (0 = kein Zuschlag):",
            "Satz Eigenstrom [ct/kWh] (0 = kein Zuschlag):",
            "Vbh-Kontingent [h] (0 = nach § 8 abgeleitet):",
            "Vbh-Jahresdeckel [h/a] (0 = Staffel):",
            "Anteil Neuherstellungskosten [%] (§ 8 Abs. 2/3):",
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
    public void Eine_Eingabe_landet_im_Arbeitsstand_und_0_heisst_kein_eigener_Wert()
    {
        var anlagen = ZweiAnlagen();
        var cut = Aufbauen(anlagen);
        var zahlen = Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]");

        zahlen[0].Input("5,57");                       // Satz Einspeisung
        Assert.Equal(5.57, cut.Instance.AktuellerStand!.SatzEinspCt);

        zahlen[0].Input("0");                          // 0 = kein eigener Wert
        Assert.Null(cut.Instance.AktuellerStand!.SatzEinspCt);

        // Beim Hilfsenergieanteil ist 0 ein GUELTIGER Wert (BF4). BK1: Er steht als
        // SECHSTES Dezimalfeld der Gruppe - davor liegt der neue Kostenanteil.
        zahlen[5].Input("3,5");
        Assert.Equal(3.5, cut.Instance.AktuellerStand!.HilfsenergieAnteil);
        zahlen[5].Input("0");
        Assert.Equal(0.0, cut.Instance.AktuellerStand!.HilfsenergieAnteil);

        // Die geladene Zeile bleibt dabei unberuehrt — geschrieben wird im OK-Weg.
        Assert.Null(anlagen[0].SatzEinspCt);
        Assert.Null(anlagen[0].HilfsenergieAnteil);
    }

    [Fact]
    public void Datum_und_Auswahl_der_Anlage_werden_uebernommen()
    {
        var anlagen = ZweiAnlagen();
        var cut = Aufbauen(anlagen);
        IElement g = Koerper(cut, 1);

        g.QuerySelectorAll("input[type=date]")[0].Change("2026-03-17");
        Assert.Equal(new DateTime(2026, 3, 17), cut.Instance.AktuellerStand!.Stichtag);

        g.QuerySelectorAll("input[type=date]")[0].Change("");
        Assert.Null(cut.Instance.AktuellerStand!.Stichtag);

        Koerper(cut, 1).QuerySelectorAll("select")[0].Change("2");
        Assert.Equal(DbWerte.KWKG_ANLAGENART_MODERNISIERT, cut.Instance.AktuellerStand!.Anlagenart);

        Koerper(cut, 1).QuerySelectorAll("select")[2].Change("3");
        Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_53A, cut.Instance.AktuellerStand!.EnergiesteuerWahl);

        // Und die geladene Zeile steht unveraendert da.
        Assert.Null(anlagen[0].Stichtag);
        Assert.Equal("", anlagen[0].Anlagenart);
        Assert.Equal("", anlagen[0].EnergiesteuerWahl);
    }

    // =====================================================================
    // Gruppe 2 — KWK-Zuschlag (Feldkarte 2.1 bis 2.11)
    // =====================================================================

    /// <summary>
    /// ETAPPE BK1 (Entscheid BK-E-1 a) und BK1b (Entscheid BK1-4 a) — die Gruppe
    /// heisst „Projektweite KWK-Angaben" und fuehrt nur noch, was WIRKLICH projektweit
    /// ist: den Abschlag fuer Negativstunden, die Pauschale des § 9, den Stichtag des
    /// § 6 und den Foerderbeginn — dazu die Einspeiseverguetung aus Auftrag #325, die
    /// keine Zuschlagsgroesse ist.
    ///
    /// <para><b>Fuenf Felder, und KEINE Klappliste mehr.</b> Satz Eigenstrom, Satz
    /// Einspeisung, Vbh-Deckel-Override, Vbh-Kontingent gesamt, Eigenstrom-Tatbestand,
    /// Anlagenart § 8 und der Anteil an den Neuherstellungskosten sind hier weg — sie
    /// stehen an der Anlage, wo § 7 und § 8 KWKG sie bemessen. Genau das war die
    /// doppelte Wahrheit samt Rueckfallkette, die BK1 aufloest.</para>
    /// </summary>
    [Fact]
    public void Gruppe2_fuehrt_genau_die_fuenf_projektweiten_Angaben()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 2);

        Assert.Equal(2, Zahlenfelder(g));      // #325, Abschlag
        Assert.Equal(0, Auswahlfelder(g));     // BK1: Tatbestand und Anlagenart sind weg
        Assert.Equal(1, Schalter(g));          // Pauschale § 9
        Assert.Equal(2, Datumsfelder(g));      // Stichtag § 6, Foerderbeginn

        Assert.Equal(new[]
        {
            "Einspeisevergütung KWK-Strom [€/kWh]:",
            "Abschlag Negativstunden [%]:",
            "Pauschale § 9 KWKG (nur bis 2 kWel, einmalig)",
            "Stichtag (Bestellung/Genehmigung, § 6):",
            "Förderbeginn (Startjahr der Reihen):"
        }, Beschriftungen(g));
    }

    /// <summary>
    /// ETAPPE BK1b (Anwenderentscheid BK1-4 a): Der Anteil an den Neuherstellungskosten
    /// steht NUR NOCH an der Anlage (Gruppe 1b) — § 8 Abs. 2/3 KWKG leitet das
    /// Kontingent aus IHREM Kostenanteil ab. Die Projektvorgabe hatte keinen
    /// Rechenleser mehr und stand dennoch als Feld da; ihre Spalte faellt mit
    /// Schemaschritt 91.
    /// </summary>
    [Fact]
    public void Der_Kostenanteil_steht_nur_noch_an_der_Anlage()
    {
        var cut = Aufbauen(new List<KwkgAnlagenAngabe> { Anlage(1, "BHKW 50", 50) });

        // Gruppe 2 fuehrt die Beschriftung nicht mehr ...
        Assert.DoesNotContain(Beschriftungen(Koerper(cut, 2)),
                              b => b.Contains("Neuherstellungskosten", StringComparison.Ordinal));

        // ... aber die leise Zeile NENNT ihn: sie sagt, wo er jetzt steht.
        Assert.Contains("Neuherstellungskosten", Koerper(cut, 2).TextContent);

        // ... und das Anlagenfeld in Gruppe 1b steht unveraendert.
        Assert.Contains(Beschriftungen(Koerper(cut, 1)),
                        b => b.Contains("Neuherstellungskosten", StringComparison.Ordinal));
    }

    // =====================================================================
    // AUFTRAG #325 — die Einspeiseverguetung des KWK-Stroms
    // =====================================================================

    /// <summary>
    /// <b>Der Satz steht hier und schreibt auf dieselbe Modelleigenschaft.</b>
    /// Anwenderwunsch 17.09.2026: „heraus nehmen aus Parameter Dialog: … Strom —
    /// Einspeisung und Bezug und in BHKW-Dialog". Es ist unveraendert
    /// <c>WirtschaftlichkeitParameter.EinspeiseverguetungKWK</c> in EUR/kWh — der
    /// Dialog hat nur die Eingabestelle uebernommen.
    ///
    /// <para>Und er schreibt wie jede andere Projektvorgabe dieses Dialogs ERST IM
    /// OK-WEG: Bis dahin bleibt der hereingereichte Satz unangetastet.</para>
    /// </summary>
    [Fact]
    public void Der_KWK_Einspeisesatz_steht_in_Gruppe_2_und_schreibt_den_Parametersatz()
    {
        var satz = new WirtschaftlichkeitParameter { EinspeiseverguetungKWK = 0.09 };
        var zaehler = new Schreibzaehler();
        var cut = Aufbauen(parameter: satz, speichereVorgaben: zaehler.Vorgaben);

        // Das erste Dezimalfeld der Gruppe ist der Einspeisesatz, und es zeigt den
        // geladenen Wert.
        IElement feld = Koerper(cut, 2).QuerySelectorAll("input[inputmode=decimal]")[0];
        Assert.Equal("0,0900", feld.GetAttribute("value"));

        feld.Input("0,1234");

        // Bis zum OK steht der geladene Satz unveraendert da.
        Assert.Equal(0.09, satz.EinspeiseverguetungKWK);

        OkKnopf(cut).Click();

        Assert.Equal(0.1234, satz.EinspeiseverguetungKWK);
        Assert.Contains("Vorgaben", zaehler.Wege);
    }

    /// <summary>
    /// <b>0 heisst „nicht gepflegt"</b> — dieselbe Nullsemantik, die der Satz im
    /// Parameterdialog hatte, und dieselbe, mit der <c>StromPreisCtrl</c> ihn liest.
    /// Eine gepflegte 0 waere die Aussage „bringt nichts ein" und von einem nie
    /// angefassten Feld an dieser Zahl nicht zu unterscheiden.
    /// </summary>
    [Fact]
    public void Ein_KWK_Einspeisesatz_von_null_heisst_nicht_gepflegt()
    {
        var satz = new WirtschaftlichkeitParameter { EinspeiseverguetungKWK = 0.09 };
        var cut = Aufbauen(parameter: satz, speichereVorgaben: _ => true);

        Koerper(cut, 2).QuerySelectorAll("input[inputmode=decimal]")[0].Input("0");
        OkKnopf(cut).Click();

        Assert.Null(satz.EinspeiseverguetungKWK);
    }

    /// <summary>
    /// Wer den Satz aendert, aendert die Speicherrechnung mit — und erfaehrt es an
    /// Ort und Stelle (SP-E-5, sinngemaess je Satz).
    /// </summary>
    [Fact]
    public void Der_KWK_Einspeisesatz_sagt_was_er_sonst_noch_bewegt()
    {
        var cut = Aufbauen();

        string text = Koerper(cut, 2).TextContent;
        Assert.Contains("v_bhkw", text);
        Assert.Contains("nicht gepflegt", text);
    }

    [Fact]
    public void Die_projektweiten_KWK_Angaben_fuehren_keine_Satzfelder_mehr()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 2);

        Assert.Empty(g.QuerySelectorAll("select"));

        // Geprueft werden die BESCHRIFTUNGEN, nicht der ganze Text: Die leise Zeile
        // darunter NENNT die ausgezogenen Angaben ja - sie sagt, wo sie jetzt stehen.
        string[] weg = { "Bonus Eigenstrom", "Bonus Einspeisung", "Vbh-Deckel-Override",
                         "Vbh-Kontingent gesamt", "Eigenstrom-Tatbestand", "Anlagenart (§ 8)" };
        foreach (string s in weg)
            Assert.DoesNotContain(Beschriftungen(g), b => b.Contains(s, StringComparison.Ordinal));

        Assert.Contains("stehen an der Anlage", g.TextContent);
    }

    // =====================================================================
    // ETAPPE BK1 — die drei Vorschlagsknoepfe AM FELD (Entscheid BK-E-1 a)
    // =====================================================================

    /// <summary>
    /// Ein Katalog, der jeden Schluessel beantwortet — der Vorschlag kommt dann ohne
    /// Luecke zustande.
    /// </summary>
    private static Func<string, int, GesetzParameter> VollerKatalog(double wert = 16.0)
        => (schluessel, jahr) =>
            new GesetzParameter(1, schluessel, "KWKG", 2020, wert, "ct/kWh", "", "");

    /// <summary>
    /// DER ANWENDERWUNSCH vom 17.09.2026: „Werte … sollen direkt mittels Button an
    /// der Stelle des Wertes uebernommen werden koennen (mit Hinweis auf die
    /// Grundlage)". Drei Knoepfe stehen in Gruppe 1b — am Satz Einspeisung, am Satz
    /// Eigenstrom und am Vbh-Kontingent —, jeder mit seiner Grundlage daneben.
    /// </summary>
    [Fact]
    public void Gruppe1b_traegt_drei_Vorschlagsknoepfe_mit_ihrer_Grundlage()
    {
        var anlagen = new List<KwkgAnlagenAngabe> { Anlage(1, "BHKW 50", 50) };
        anlagen[0].Inbetriebnahme = new DateTime(2027, 5, 4);
        anlagen[0].Anlagenart = DbWerte.KWKG_ANLAGENART_NEU;

        var cut = Aufbauen(anlagen, katalog: VollerKatalog());
        IElement g = Koerper(cut, 1);

        Assert.Equal(3, g.QuerySelectorAll("button.epos-vorschlag").Length);

        var zeilen = g.QuerySelectorAll("p.epos-vorschlagszeile");
        Assert.Equal(3, zeilen.Length);
        Assert.Contains("Einspeisung", zeilen[0].TextContent);
        Assert.Contains("Eigenstrom", zeilen[1].TextContent);
        Assert.Contains("Kontingent", zeilen[2].TextContent);

        // Und der alte Sammelknopf der Gruppe 2 ist weg.
        Assert.Empty(Koerper(cut, 2).QuerySelectorAll("button.epos-vorschlag"));
    }

    /// <summary>
    /// Der Knopf am EINSPEISESATZ schreibt genau das, was <c>KwkgSatzRechner</c> mit
    /// denselben Angaben liefert — die Zahl gehoert dem Bestandsrechner, nicht dem
    /// Dialog —, und er schreibt nur SEIN Feld: Der Eigenstromsatz daneben bleibt
    /// unberuehrt. Genau das konnte der Sammelknopf nicht.
    /// </summary>
    [Fact]
    public void Der_Knopf_am_Einspeisesatz_trifft_nur_sein_Feld()
    {
        var anlagen = new List<KwkgAnlagenAngabe> { Anlage(1, "BHKW 50", 50) };
        anlagen[0].Inbetriebnahme = new DateTime(2027, 5, 4);
        anlagen[0].Anlagenart = DbWerte.KWKG_ANLAGENART_NEU;

        Func<string, int, GesetzParameter> katalog = VollerKatalog();
        var cut = Aufbauen(anlagen, katalog: katalog);

        Assert.Null(cut.Instance.AktuellerStand!.SatzEinspCt);
        Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[0].Click();

        KwkgSatzVorschlag soll = KwkgSatzRechner.Vorschlag(
            50, 2027, DbWerte.KWKG_ANLAGENART_NEU, "", katalog,
            CultureInfo.GetCultureInfo("de-DE"));
        Assert.Equal(soll.SatzEinspeisungCt, cut.Instance.AktuellerStand!.SatzEinspCt);
        Assert.Null(cut.Instance.AktuellerStand!.SatzEigenCt);   // nicht mitgeschrieben
        Assert.True(cut.Instance.Geaendert);

        // Uebernommen wird in den ARBEITSSTAND; geschrieben wird erst beim OK.
        Assert.Null(anlagen[0].SatzEinspCt);
    }

    /// <summary>
    /// ZWEI ANLAGEN VERSCHIEDENER LEISTUNGSKLASSE bekommen verschiedene Saetze und
    /// verschiedene Grundlagen — der Beweis, dass der Vorschlag an DER ANLAGE haengt
    /// und nicht am Projekt. Der Katalog staffelt dafuer nach Schluessel.
    /// </summary>
    [Fact]
    public void Zwei_Anlagen_verschiedener_Klasse_bekommen_verschiedene_Saetze()
    {
        var anlagen = new List<KwkgAnlagenAngabe>
        {
            Anlage(1, "BHKW klein", 40),
            Anlage(2, "BHKW gross", 300)
        };
        foreach (KwkgAnlagenAngabe a in anlagen)
        {
            a.Inbetriebnahme = new DateTime(2027, 1, 1);
            a.Anlagenart = DbWerte.KWKG_ANLAGENART_MODERNISIERT;   // nicht § 7 Abs. 3a
        }

        // Eine echte Staffel: 8 / 6 / 5 / 4,4 ct/kWh, Grenzen 50/100/250/2000 kW.
        Func<string, int, GesetzParameter> katalog = (schluessel, jahr) =>
        {
            double w = 0;
            if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1) w = 50;
            else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_2) w = 100;
            else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_3) w = 250;
            else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_4) w = 2000;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW) w = 8.0;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW) w = 6.0;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW) w = 5.0;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW) w = 4.4;
            return w > 0 ? new GesetzParameter(1, schluessel, "KWKG", 2020, w, "ct/kWh", "", "") : null!;
        };

        var cut = Aufbauen(anlagen, katalog: katalog);

        // Anlage 1 (40 kW): eine Tranche zu 8,00 ct/kWh.
        string grundlage1 = Koerper(cut, 1).QuerySelectorAll("p.epos-vorschlagszeile")[0].TextContent;
        Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[0].Click();
        Assert.Equal(8.0, cut.Instance.AktuellerStand!.SatzEinspCt);

        // Anlage 2 (300 kW): 50x8 + 50x6 + 150x5 + 50x4,4 = 1670 / 300 = 5,5667.
        Koerper(cut, 0).QuerySelectorAll("button.epos-anlagenwahl")[1].Click();
        string grundlage2 = Koerper(cut, 1).QuerySelectorAll("p.epos-vorschlagszeile")[0].TextContent;
        Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[0].Click();
        Assert.Equal(1670.0 / 300.0, cut.Instance.AktuellerStand!.SatzEinspCt!.Value, 6);

        Assert.NotEqual(grundlage1, grundlage2);
    }

    // =====================================================================
    // AUFTRAG #341 (U26) — VIER NACHKOMMASTELLEN IN DEN BEIDEN SATZFELDERN
    //
    // Der Vorschlagsknopf schreibt den gerechneten Satz UNGERUNDET in den
    // Arbeitsstand, und der Kern rechnet ohne Rückfall mit genau diesem Feld
    // (WirtschaftlichkeitCtrl: a.SatzEinspCt ?? 0, SatzEigenDerAnlage). Zeigte
    // das Feld zwei Stellen, machte die nächste Berührung aus 5,5667 ein
    // 5,57 — die Anzeige wurde zum Wert. Beide Fälle stehen deshalb hier:
    // die Anzeige nach dem Vorschlag und der Rundweg eines Handwertes.
    // =====================================================================

    /// <summary>
    /// ANZEIGE NACH DEM VORSCHLAG: 50x8 + 50x6 + 150x5 + 50x4,4 = 1670 / 300 ergibt
    /// 5,56666… ct/kWh, der Eigenstromsatz entsprechend 835 / 300 = 2,78333…. Die
    /// Felder müssen 5,5667 und 2,7833 zeigen — mit zwei Stellen stünde dort 5,57
    /// und 2,78, und der Anwender sähe eine Zahl, die der Arbeitsstand nicht führt.
    /// </summary>
    [Fact]
    public void Der_uebernommene_Vorschlag_steht_mit_vier_Stellen_im_Satzfeld()
    {
        var anlagen = new List<KwkgAnlagenAngabe> { Anlage(2, "BHKW gross", 300) };
        anlagen[0].Inbetriebnahme = new DateTime(2027, 1, 1);
        anlagen[0].Anlagenart = DbWerte.KWKG_ANLAGENART_MODERNISIERT;   // nicht § 7 Abs. 3a
        anlagen[0].Eigenfall = DbWerte.KWKG_EIGENFALL_NR2;

        var cut = Aufbauen(anlagen, katalog: Satzstaffel());

        Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[0].Click();
        Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[1].Click();

        Assert.Equal("5,5667", Satzfeld(cut, 0).GetAttribute("value"));   // Einspeisung
        Assert.Equal("2,7833", Satzfeld(cut, 1).GetAttribute("value"));   // Eigenstrom
    }

    /// <summary>
    /// DER RUNDWEG EINES HANDWERTES: getippt, mit OK geschrieben, neu geladen — vier
    /// Stellen müssen unverändert wieder im Feld stehen. Die Spalten
    /// KWKG_Satz_Einspeisung/_Eigen sind REAL, es schneidet unterwegs nichts ab.
    /// </summary>
    [Fact]
    public void Ein_Handwert_mit_vier_Stellen_ueberlebt_Schreiben_und_Laden()
    {
        var anlagen = ZweiAnlagen();
        var cut = Aufbauen(anlagen);

        Satzfeld(cut, 0).Input("5,5667");
        Satzfeld(cut, 1).Input("4,1234");
        OkKnopf(cut).Click();

        Assert.Equal(5.5667, anlagen[0].SatzEinspCt);
        Assert.Equal(4.1234, anlagen[0].SatzEigenCt);

        // Neu geladen: derselbe Dialog über denselben Anlagenstand.
        var wieder = Aufbauen(anlagen);
        Assert.Equal("5,5667", Satzfeld(wieder, 0).GetAttribute("value"));
        Assert.Equal("4,1234", Satzfeld(wieder, 1).GetAttribute("value"));
    }

    /// <summary>Das Satzfeld Nr. <paramref name="nr"/> der Gruppe 1 — frisch gesucht,
    /// weil jede Bedienung neu zeichnet (0 = Einspeisung, 1 = Eigenstrom).</summary>
    private static IElement Satzfeld(IRenderedComponent<BhkwWirtschaftlichkeitDialog> cut, int nr)
        => Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[nr];

    /// <summary>Die Staffel dieser Abnahme: 8 / 6 / 5 / 4,4 ct/kWh Einspeisung, halb so
    /// viel Eigenstrom nach § 6 Abs. 3 Nr. 2, Grenzen 50/100/250/2000 kW.</summary>
    private static Func<string, int, GesetzParameter> Satzstaffel()
        => (schluessel, jahr) =>
        {
            double w = 0;
            if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_1) w = 50;
            else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_2) w = 100;
            else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_3) w = 250;
            else if (schluessel == DbWerte.GESETZ_KWKG_LEISTUNGSSTUFE_4) w = 2000;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW) w = 8.0;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW) w = 6.0;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW) w = 5.0;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW) w = 4.4;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS50KW) w = 4.0;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS100KW) w = 3.0;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS250KW) w = 2.5;
            else if (schluessel == DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N2_BIS2MW) w = 2.2;
            return w > 0 ? new GesetzParameter(1, schluessel, "KWKG", 2020, w, "ct/kWh", "", "") : null!;
        };

    /// <summary>
    /// DAS KONTINGENT KOMMT AUS DER ANLAGE: Anlagenart und Kostenanteil DIESER Anlage
    /// waehlen die Stufe des § 8 — nicht mehr die projektweite Angabe. Ohne
    /// Anlagenart ist der Knopf gesperrt und sagt warum.
    /// </summary>
    [Fact]
    public void Das_Kontingent_kommt_aus_Anlagenart_und_Kostenanteil_der_Anlage()
    {
        var anlagen = new List<KwkgAnlagenAngabe> { Anlage(1, "BHKW 50", 50) };
        anlagen[0].Inbetriebnahme = new DateTime(2027, 1, 1);

        Func<string, int, GesetzParameter> katalog = (schluessel, jahr) =>
        {
            double w = 0;
            if (schluessel == DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_50) w = 50;
            else if (schluessel == DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_25) w = 25;
            else if (schluessel == DbWerte.GESETZ_KWKG_KOSTENSCHWELLE_10) w = 10;
            else if (schluessel == DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_25) w = 15000;
            else if (schluessel == DbWerte.GESETZ_KWKG_VBH_MODERNISIERT_50) w = 30000;
            else if (schluessel == DbWerte.GESETZ_KWKG_VBH_NEUANLAGE) w = 30000;
            return w > 0 ? new GesetzParameter(1, schluessel, "KWKG", 2020, w, "h", "", "") : null!;
        };

        var cut = Aufbauen(anlagen, katalog: katalog);

        // Ohne Anlagenart: gesperrt, und der Grund steht am Knopf (WEICHE Sperre —
        // ein disabled-Knopf zeigt seinen title nie, Hausregel EPOS.UI).
        IElement knopf = Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[2];
        Assert.Equal("true", knopf.GetAttribute("aria-disabled"));
        Assert.Contains("Ohne Anlagenart", knopf.GetAttribute("title"));
        knopf.Click();
        Assert.Null(cut.Instance.AktuellerStand!.VbhKontingent);

        // Modernisiert mit 30 % Kostenanteil: Stufe 25 % -> 15.000 Vbh.
        Koerper(cut, 1).QuerySelectorAll("select")[0].Change("2");
        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[4].Input("30");
        Assert.Equal(DbWerte.KWKG_ANLAGENART_MODERNISIERT, cut.Instance.AktuellerStand!.Anlagenart);
        Assert.Equal(30.0, cut.Instance.AktuellerStand!.Kostenanteil);

        Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[2].Click();
        Assert.Equal(15000.0, cut.Instance.AktuellerStand!.VbhKontingent);

        // Mit 60 % springt dieselbe Anlage auf die 50-%-Stufe.
        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[4].Input("60");
        Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[2].Click();
        Assert.Equal(30000.0, cut.Instance.AktuellerStand!.VbhKontingent);
    }

    /// <summary>
    /// GESPERRT HEISST BEGRUENDET. Ohne elektrische Nennleistung gibt es keine
    /// Leistungsstaffel; ohne Tatbestand des § 6 Abs. 3 keinen Zuschlag auf selbst
    /// genutzten Strom. Beides steht am Knopf, und beides sperrt WEICH.
    /// </summary>
    [Fact]
    public void Ein_gesperrter_Vorschlagsknopf_nennt_seinen_Grund()
    {
        var anlagen = new List<KwkgAnlagenAngabe> { Anlage(1, "BHKW ohne Pel", 0) };
        var cut = Aufbauen(anlagen, katalog: VollerKatalog());

        var knoepfe = Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag");
        Assert.Equal("true", knoepfe[0].GetAttribute("aria-disabled"));
        Assert.Contains("keine elektrische Nennleistung", knoepfe[0].GetAttribute("title"));
        Assert.Equal("true", knoepfe[1].GetAttribute("aria-disabled"));
        Assert.Contains("keine elektrische Nennleistung", knoepfe[1].GetAttribute("title"));

        knoepfe[0].Click();
        Assert.Null(cut.Instance.AktuellerStand!.SatzEinspCt);
        Assert.False(cut.Instance.Geaendert);

        // Mit Leistung, aber ohne Tatbestand: nur der EIGENSTROM-Knopf bleibt
        // gesperrt, und sein Grund wechselt.
        var mitPel = new List<KwkgAnlagenAngabe> { Anlage(2, "BHKW 50", 50) };
        var cut2 = Aufbauen(mitPel, katalog: VollerKatalog());
        var k2 = Koerper(cut2, 1).QuerySelectorAll("button.epos-vorschlag");
        Assert.Equal("false", k2[0].GetAttribute("aria-disabled"));
        Assert.Equal("true", k2[1].GetAttribute("aria-disabled"));
        Assert.Contains("§ 6 Abs. 3", k2[1].GetAttribute("title"));
    }

    /// <summary>
    /// BEIDE SPRACHEN: Gruppentitel, Knopfbeschriftung und Sperrgrund kommen aus den
    /// Ressourcen, nicht aus einem deutschen Literal im Markup. Ohne diesen Fall
    /// bliebe unbemerkt, dass ein neuer Text nur deutsch nachgetragen wurde.
    /// </summary>
    [Theory]
    [InlineData("de-DE", "Projektweite KWK-Angaben", "Vorschlag übernehmen",
                "keine elektrische Nennleistung")]
    [InlineData("en-US", "Project-wide CHP settings", "Apply proposal",
                "no electrical rated output")]
    public void Die_BK1_Texte_stehen_in_beiden_Sprachen(
        string kultur, string gruppe, string knopf, string sperrgrund)
    {
        using var _ = new Kulturvorrichtung(kultur);

        var anlagen = new List<KwkgAnlagenAngabe> { Anlage(1, "BHKW ohne Pel", 0) };
        var cut = Aufbauen(anlagen, katalog: VollerKatalog());

        var titel = new List<string>();
        foreach (IElement e in cut.FindAll("h2.epos-gruppenkopf-titel")) titel.Add(e.TextContent);
        Assert.Contains(gruppe, titel);

        IElement k = Koerper(cut, 1).QuerySelectorAll("button.epos-vorschlag")[0];
        Assert.Equal(knopf, k.TextContent.Trim());
        Assert.Contains(sperrgrund, k.GetAttribute("title"));
    }

    [Fact]
    public void Ohne_gewaehlte_Anlage_steht_keine_Vorschlagszeile()
    {
        var cut = Aufbauen(new List<KwkgAnlagenAngabe>());

        Assert.Empty(cut.FindAll("button.epos-vorschlag"));

        // AUFTRAG #325 / BK1: Die Gruppe 2 fuehrt ZWEI stehende Herleitungszeilen —
        // den Hinweis zum Einspeisesatz (SP-E-5) und den Verweis darauf, wo die
        // uebrigen KWK-Angaben jetzt stehen.
        var zeilen = Koerper(cut, 2).QuerySelectorAll("p.epos-herleitung");
        Assert.Equal(2, zeilen.Length);
        Assert.Contains("v_bhkw", zeilen[0].TextContent);
        Assert.Contains("stehen an der Anlage", zeilen[1].TextContent);
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

    /// <summary>
    /// ETAPPE B6 — <b>das Modusfeld des § 9 Abs. 1 Nr. 3 ist offen.</b> Bis B5b stand es
    /// gesperrt da, weil es keine Spalte gab, in die es geschrieben worden waere; mit dem
    /// Schemaschritt 88 gibt es sie. Die Vorgabe ist AUSWEIS — der erste Eintrag, und der
    /// Wert, den eine leere Zelle bedeutet.
    /// </summary>
    [Fact]
    public void Das_Modusfeld_ist_offen_und_steht_auf_der_Vorgabe_Ausweis()
    {
        var cut = Aufbauen();
        IElement g = Koerper(cut, 4);
        IElement modus = g.QuerySelectorAll("select")[1];

        Assert.False(modus.HasAttribute("disabled"));
        var eintraege = modus.QuerySelectorAll("option");
        Assert.Equal(2, eintraege.Length);
        Assert.Equal("Ausweis (nicht im Kapitalwert)", eintraege[0].TextContent);
        Assert.Equal("Erlös (im Kapitalwert)", eintraege[1].TextContent);
        Assert.Equal(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS,
                     cut.Instance.Vorgabenstand.StromsteuerBefreiungModus);
    }

    /// <summary>
    /// Die Wahl geht in den Arbeitsstand — und NUR dorthin. Der hereingereichte
    /// Parametersatz bleibt bis zum OK unberührt; das ist die Hausregel des Dialogs und
    /// gilt für den Modus wie für jedes andere Feld der Gruppe.
    /// </summary>
    [Fact]
    public void Das_Modusfeld_schreibt_die_Wahl_in_den_Arbeitsstand()
    {
        var p = new WirtschaftlichkeitParameter();
        var cut = Aufbauen(parameter: p);
        IElement g = Koerper(cut, 4);

        g.QuerySelectorAll("select")[1].Change("1");
        Assert.Equal(DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES,
                     cut.Instance.Vorgabenstand.StromsteuerBefreiungModus);

        g.QuerySelectorAll("select")[1].Change("0");
        Assert.Equal(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS,
                     cut.Instance.Vorgabenstand.StromsteuerBefreiungModus);

        Assert.Equal(DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS, p.StromsteuerBefreiungModus);
    }

    /// <summary>
    /// Die Herleitungszeile unter dem Feld sagt, was die beiden Modi bedeuten — <b>in
    /// beiden Sprachen und ohne Etappenkürzel</b>. Der frühere Text nannte „B6"; ein
    /// Anwender kennt keine Etappen, und die Zeile stünde sonst für immer als Versprechen
    /// da, das längst eingelöst ist.
    /// </summary>
    [Theory]
    [InlineData("de-DE", "Ausweis: Die Befreiung wird gezeigt und nicht im Kapitalwert gerechnet.")]
    [InlineData("en-US", "Disclosure: the exemption is shown and not included in the net present value.")]
    public void Die_Herleitungszeile_erklaert_die_beiden_Modi_ohne_Etappenkuerzel(
        string kultur, string erwartet)
    {
        using var _ = new Kulturvorrichtung(kultur);

        var cut = Aufbauen();
        string text = Koerper(cut, 4).TextContent;

        Assert.Contains(erwartet, text);
        Assert.DoesNotContain("B6", text);
    }

    [Fact]
    public void Die_Stromsteuerfelder_schreiben_in_den_Arbeitsstand_der_Vorgaben()
    {
        var p = new WirtschaftlichkeitParameter();
        var cut = Aufbauen(parameter: p);
        IElement g = Koerper(cut, 4);

        g.QuerySelectorAll("select")[0].Change("1");
        Assert.Equal(DbWerte.UNTERNEHMENSART_PROD_GEWERBE, cut.Instance.Vorgabenstand.Unternehmensart);

        g.QuerySelectorAll("input[type=checkbox]")[0].Change(true);
        Assert.True(cut.Instance.Vorgabenstand.RaeumlicherZusammenhang);

        g.QuerySelectorAll("input[type=checkbox]")[1].Change(true);
        Assert.True(cut.Instance.Vorgabenstand.HocheffizienzNachweis);

        // Der hereingereichte Parametersatz bleibt bis zum OK unveraendert.
        Assert.Equal(DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE, p.Unternehmensart);
        Assert.False(p.RaeumlicherZusammenhang);
        Assert.False(p.HocheffizienzNachweis);
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

    /// <summary>
    /// ETAPPE B7P — DIE MENGENKETTE AUS DEM GEBUCHTEN STAND. Ohne frischen Lauf faellt
    /// der Dialog auf <c>ErgebnisseLaden</c> zurueck. Bis B7P trug ein geladenes
    /// Ergebnis keinen Modulnachweis, und die Gruppe sagte "noch kein gebuchtes
    /// Ergebnis" — obwohl eines gebucht war. Seit der Nachweisumschlag persistiert
    /// wird, findet <c>ModulNachweis</c> seine Zeile auch dort: zwei Zeilen statt des
    /// Hinweises.
    /// </summary>
    [Fact]
    public void Gruppe5_zeigt_die_Mengenkette_aus_dem_gebuchten_Stand()
    {
        var gebucht = new List<WirtschaftlichkeitErgebnis>
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
                        StromBruttoMWh = 373.78, HilfsstromMWh = 1.5, StromNettoMWh = 372.28,
                        EigenMWh = 300.0, EinspeisungMWh = 72.28
                    }
                }
            }
        };

        // KEIN Lauf in dieser Sitzung — nur der gebuchte Stand.
        var cut = Aufbauen(ZweiAnlagen(), ergebnisseLaden: _ => gebucht);

        string text = Koerper(cut, 6).TextContent;
        Assert.DoesNotContain("Mengenkette: noch kein gebuchtes Ergebnis", text);
        Assert.Contains("Stromerzeugung brutto 373,780 MWh/a − Hilfsstrom 1,500 MWh/a = " +
                        "Nettostromerzeugung 372,280 MWh/a", text);
        Assert.Contains("davon Eigenverbrauch 300,000 MWh/a, Einspeisung 72,280 MWh/a", text);
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

    /// <summary>
    /// ETAPPE B7 — Gruppe 6 zeigt die ERLOESRUBRIK, aus derselben Quelle wie Reiter,
    /// Word und Excel (<c>WirtschaftlichkeitZeilen</c>).
    ///
    /// <para><b>Was sich gegenueber der Handliste geaendert hat</b>, die hier bis B7
    /// gemessen wurde: Die Stromsteuer steht nicht mehr als EINE Summe aus Befreiung
    /// und Entlastung da. Die beiden sind verschiedene Groessen — die Entlastung nach
    /// § 9b ist eine Rueckzahlung und geht in den Kapitalwert (Block A), die Befreiung
    /// nach § 9 Abs. 1 Nr. 3 ist im Modus AUSWEIS (Vorgabe seit B6) gar keine Zahlung
    /// und steht in Block B. Ihre Summe war eine Zahl ohne Bedeutung.</para>
    /// </summary>
    [Fact]
    public void Die_Vorschau_zeigt_die_Erloesrubrik_des_gebuchten_Laufs()
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
                // Der GESAMTerloes traegt den KWK-Anteil; ein Lauf setzt beide, und
                // die Rubrik weist A8 als Gesamterloes aus (Konzept § 2.6).
                EinspeiseerloesJahr = 1234,
                EinspeiseerloesKwkJahr = 1234,
                VermiedenGesamtJahr = 4321,
                Zeitstempel = new DateTime(2026, 9, 3, 12, 3, 0)
            }
        };
        var cut = Aufbauen(ausLauf: lauf);

        var zeilen = new List<string>();
        foreach (IElement e in Koerper(cut, 7).QuerySelectorAll("p.epos-herleitung"))
            zeilen.Add(e.TextContent.Trim());

        string ganz = string.Join(" | ", zeilen);

        // Block A: die beiden Ueberschriften trennen Zahlung von Ausweis.
        Assert.Contains("Erlöse und Vorteile", ganz);
        Assert.Contains("Ausweis", ganz);

        // Die vier zahlungswirksamen Positionen — jede mit ihrer Rechtsgrundlage.
        Assert.Contains("§ 7 KWKG", ganz);
        Assert.Contains("7.316", ganz);
        Assert.Contains("EnergieStG", ganz);
        Assert.Contains("5.119", ganz);
        Assert.Contains("§ 9b StromStG", ganz);
        Assert.Contains("906", ganz);
        Assert.Contains("1.234", ganz);

        // Die Befreiung nach § 9 Abs. 1 Nr. 3 steht im AUSWEIS, nicht in der Summe —
        // und vor allem nicht mehr mit der Entlastung zu 86.906 € verrechnet.
        Assert.Contains("86.000", ganz);
        Assert.DoesNotContain("86.906", ganz);

        // Die Summe des Blocks A: 7.316 + 5.119 + 906 + 1.234 = 14.575 €/a.
        // Die 86.000 € der Befreiung stecken NICHT darin — das ist die Aussage der
        // Teilung, und sie wird hier gemessen, nicht behauptet.
        Assert.Contains("14.575", ganz);

        Assert.Contains("4.321", ganz);
        Assert.Contains("nach dem Speichern neu berechnen", zeilen[zeilen.Count - 1]);
    }

    // =====================================================================
    //  Die Fussleiste — OK und Abbrechen, geschrieben wird im OK-Weg
    //  (Auftrag #286; die Abnahme misst den Datenbankstand, nicht die Anzeige)
    // =====================================================================

    [Fact]
    public void Die_Leiste_traegt_Abbrechen_und_Speichern_und_keinen_dritten_Knopf()
    {
        var cut = Aufbauen();
        var knoepfe = cut.FindAll(".epos-leiste button");

        Assert.Equal(2, knoepfe.Count);
        Assert.Equal("Abbrechen", knoepfe[0].TextContent);
        Assert.Equal("Speichern", knoepfe[1].TextContent);
        Assert.Contains("epos-knopf--primaer", knoepfe[1].ClassName);
    }

    [Fact]
    public void OK_ist_von_Anfang_an_anklickbar()
    {
        var cut = Aufbauen();

        // Es gibt keinen nicht schliessenden Speichern-Knopf mehr, den eine
        // fehlende Aenderung sperren muesste — OK verlaesst den Dialog immer.
        Assert.False(OkKnopf(cut).HasAttribute("disabled"));
    }

    [Fact]
    public void OK_schreibt_erst_die_Anlagenzeilen_dann_die_Projektvorgaben()
    {
        var anlagen = ZweiAnlagen();
        var p = new WirtschaftlichkeitParameter();
        var z = new Schreibzaehler();
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;

        var cut = Aufbauen(anlagen, p, e => ergebnis = e, z.Anlage, z.Vorgaben);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        Koerper(cut, 4).QuerySelectorAll("input[type=checkbox]")[0].Change(true);

        Assert.Equal(0, z.Zugriffe);          // bis hierher ist nichts geschrieben
        OkKnopf(cut).Click();

        Assert.Equal(new[] { "Anlage:BHKW EW M 50 S [K] Erdgas", "Anlage:EC-POWER XRGI 9", "Vorgaben" },
                     z.Wege);
        Assert.Equal(5.57, anlagen[0].SatzEinspCt);
        Assert.True(p.RaeumlicherZusammenhang);
        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Gespeichert);
        Assert.Equal(BhkwSprung.Keiner, ergebnis.Sprung);
    }

    /// <summary>
    /// Der Kern der Abnahme: Nach JEDER aendernden Bedienung im selben Durchgang
    /// ergibt Abbrechen null Schreibzugriffe — und die hereingereichten Objekte
    /// stehen unveraendert da.
    /// </summary>
    [Fact]
    public void Abbrechen_nach_jeder_aendernden_Bedienung_schreibt_nichts()
    {
        var anlagen = ZweiAnlagen();
        var p = new WirtschaftlichkeitParameter();
        var z = new Schreibzaehler();
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;

        var cut = Aufbauen(anlagen, p, e => ergebnis = e, z.Anlage, z.Vorgaben,
                           katalog: Katalog(500, 2000));

        // Zeilenwahl, Zahlenfeld, Datumsfeld, Auswahlfeld, Schalter, Vorschlagsknopf.
        Koerper(cut, 0).QuerySelectorAll("button.epos-anlagenwahl")[1].Click();
        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("7,25");
        Koerper(cut, 1).QuerySelectorAll("input[type=date]")[0].Change("2026-03-17");
        Koerper(cut, 1).QuerySelectorAll("select")[0].Change("2");
        Koerper(cut, 2).QuerySelectorAll("input[inputmode=decimal]")[0].Input("4,5");
        Koerper(cut, 2).QuerySelectorAll("input[type=checkbox]")[0].Change(true);
        Koerper(cut, 3).QuerySelectorAll("select")[0].Change("1");
        Koerper(cut, 4).QuerySelectorAll("input[type=checkbox]")[1].Change(true);
        cut.Find("button.epos-vorschlag").Click();

        AbbrechenKnopf(cut).Click();

        Assert.Equal(0, z.Zugriffe);
        Assert.NotNull(ergebnis);
        Assert.False(ergebnis!.Gespeichert);
        Assert.Equal(BhkwSprung.Keiner, ergebnis.Sprung);

        // Nichts ist an den hereingereichten Objekten haengen geblieben.
        Assert.Null(anlagen[0].SatzEinspCt);
        Assert.Null(anlagen[1].SatzEinspCt);
        Assert.Null(anlagen[1].Stichtag);
        Assert.Equal("", anlagen[1].Anlagenart);
        Assert.False(p.KwkgPauschalmodus);
        Assert.False(p.HocheffizienzNachweis);
        Assert.Equal(DbWerte.ENERGIESTEUER_WAHL_KEINE, p.EnergiesteuerWahl);
    }

    [Fact]
    public void Esc_verwirft_wie_Abbrechen()
    {
        var anlagen = ZweiAnlagen();
        var z = new Schreibzaehler();
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(anlagen, beimSchliessen: e => ergebnis = e,
                           speichereAnlage: z.Anlage, speichereVorgaben: z.Vorgaben);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        cut.Find("div.epos-dialog").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.Equal(0, z.Zugriffe);
        Assert.NotNull(ergebnis);
        Assert.False(ergebnis!.Gespeichert);
        Assert.Equal(BhkwSprung.Keiner, ergebnis.Sprung);
        Assert.Null(anlagen[0].SatzEinspCt);
    }

    /// <summary>
    /// Das ✕ der Überlagerung schließt den Dialog von außen — der Wirt ruft dabei
    /// keinen Rückruf der Komponente. Geprüft wird deshalb, was zählt: dass bis zu
    /// diesem Augenblick kein Schreibzugriff stattgefunden hat.
    /// </summary>
    [Fact]
    public void Das_Kreuz_des_Wirts_findet_einen_ungeschriebenen_Stand_vor()
    {
        var anlagen = ZweiAnlagen();
        var p = new WirtschaftlichkeitParameter();
        var z = new Schreibzaehler();
        var cut = Aufbauen(anlagen, p, speichereAnlage: z.Anlage, speichereVorgaben: z.Vorgaben);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        Koerper(cut, 2).QuerySelectorAll("input[inputmode=decimal]")[0].Input("4,5");

        Assert.Equal(0, z.Zugriffe);
        Assert.Null(anlagen[0].SatzEinspCt);
        Assert.Equal(0.0, p.KwkgAbschlagNegativ);
    }

    /// <summary>
    /// Das eigene ✕ im Dialogkopf — es steht nur dort, wo der Dialog seinen Titel
    /// selbst trägt — geht GENAU den Weg von Esc: verwerfen, ohne einen einzigen
    /// Schreibzugriff. Das Kreuz ist Abbrechen, nicht OK.
    /// </summary>
    [Fact]
    public void Das_Kreuz_im_Kopf_verwirft_wie_Abbrechen()
    {
        var anlagen = ZweiAnlagen();
        var z = new Schreibzaehler();
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(anlagen, beimSchliessen: e => ergebnis = e,
                           speichereAnlage: z.Anlage, speichereVorgaben: z.Vorgaben);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        cut.Find(".epos-dialog-zu").Click();

        Assert.Equal(0, z.Zugriffe);
        Assert.NotNull(ergebnis);
        Assert.False(ergebnis!.Gespeichert);
        Assert.Equal(BhkwSprung.Keiner, ergebnis.Sprung);
        Assert.Null(anlagen[0].SatzEinspCt);
    }

    /// <summary>Titel-bedingter Kopf: ohne Titel zeigt der Kopf weder Titel noch Kreuz.</summary>
    [Fact]
    public void Ohne_Titel_zeigt_der_Kopf_weder_Titel_noch_Kreuz()
    {
        var cut = Aufbauen(titelAnzeigen: false);

        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        // Der Hilfeknopf bleibt - er haengt nicht am Titel.
        Assert.NotEmpty(cut.FindAll(".epos-dialog-kopf"));
    }

    [Fact]
    public void Ein_gescheiterter_Schritt_haelt_den_Dialog_offen_und_nennt_die_Zahl()
    {
        var z = new Schreibzaehler { VorgabenAntwort = _ => false };
        bool geschlossen = false;
        var cut = Aufbauen(beimSchliessen: _ => geschlossen = true,
                           speichereAnlage: z.Anlage, speichereVorgaben: z.Vorgaben);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        OkKnopf(cut).Click();

        Assert.False(geschlossen);
        Assert.False(cut.Instance.Gespeichert);
        Assert.Equal("1 Angabe(n) konnten nicht gespeichert werden.",
                     cut.FindAll(".epos-warnbanner-text")[^1].TextContent);
        Assert.Contains("epos-warnbanner--fehler", cut.FindAll(".epos-warnbanner")[^1].ClassName);
        Assert.NotNull(cut.Find(".epos-status--fehler"));
    }

    [Fact]
    public void Ein_zweites_OK_wiederholt_das_bereits_Geschriebene_nicht()
    {
        var z = new Schreibzaehler { VorgabenAntwort = _ => false };
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(beimSchliessen: e => ergebnis = e,
                           speichereAnlage: z.Anlage, speichereVorgaben: z.Vorgaben);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        OkKnopf(cut).Click();

        Assert.Equal(new[] { "Anlage:BHKW EW M 50 S [K] Erdgas", "Anlage:EC-POWER XRGI 9", "Vorgaben" },
                     z.Wege);
        Assert.Null(ergebnis);

        // Der zweite Anlauf gelingt — und ruehrt die zwei geschriebenen Zeilen nicht
        // noch einmal an.
        z.VorgabenAntwort = null;
        OkKnopf(cut).Click();

        Assert.Equal(new[] { "Anlage:BHKW EW M 50 S [K] Erdgas", "Anlage:EC-POWER XRGI 9",
                             "Vorgaben", "Vorgaben" }, z.Wege);
        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Gespeichert);
    }

    [Fact]
    public void Ohne_Schreibwege_schliesst_OK_und_meldet_gespeichert()
    {
        // Jede Seite muss AUCH OHNE GABEN zeichnen und bedienbar sein.
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(beimSchliessen: e => ergebnis = e);

        OkKnopf(cut).Click();

        Assert.NotNull(ergebnis);
        Assert.True(ergebnis!.Gespeichert);
    }

    // =====================================================================
    // Schliessen und Sprung
    // =====================================================================

    /// <summary>
    /// ANWENDERENTSCHEID 15.09.2026, Fall (a): Wer nur nachschlaegt und nichts
    /// aendert, loest mit dem Sprung KEINEN Schreibzugriff aus. Gesprungen wird
    /// trotzdem — ein Sprung ist kein Abbruch.
    /// </summary>
    [Fact]
    public void Ohne_Aenderung_springt_der_Knopf_ohne_einen_Schreibzugriff()
    {
        var z = new Schreibzaehler();
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(beimSchliessen: e => ergebnis = e,
                           speichereAnlage: z.Anlage, speichereVorgaben: z.Vorgaben);

        cut.FindAll("button.epos-sprung")[0].Click();

        Assert.Equal(BhkwSprung.Strombezug, ergebnis!.Sprung);
        Assert.Equal(0, z.Zugriffe);
        Assert.False(ergebnis.Gespeichert);   // nichts geschrieben, nichts neu zu rechnen

        var z2 = new Schreibzaehler();
        var cut2 = Aufbauen(beimSchliessen: e => ergebnis = e,
                            speichereAnlage: z2.Anlage, speichereVorgaben: z2.Vorgaben);
        cut2.FindAll("button.epos-sprung")[1].Click();

        Assert.Equal(BhkwSprung.BhkwTarif, ergebnis!.Sprung);
        Assert.Equal(0, z2.Zugriffe);
    }

    /// <summary>
    /// Fall (b): Geaendert — dann schreibt der Sprung, aber GENAU das betroffene
    /// Ziel. Eine geaenderte Anlagenzeile zieht weder die zweite Zeile noch die
    /// Projektvorgaben mit.
    /// </summary>
    [Fact]
    public void Eine_geaenderte_Anlagenzeile_schreibt_nur_diese_Zeile_und_springt()
    {
        var anlagen = ZweiAnlagen();
        var p = new WirtschaftlichkeitParameter();
        var z = new Schreibzaehler();
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(anlagen, p, e => ergebnis = e, z.Anlage, z.Vorgaben);

        Koerper(cut, 1).QuerySelectorAll("input[inputmode=decimal]")[0].Input("5,57");
        cut.FindAll("button.epos-sprung")[0].Click();

        Assert.Equal(new[] { "Anlage:BHKW EW M 50 S [K] Erdgas" }, z.Wege);
        Assert.Equal(5.57, anlagen[0].SatzEinspCt);
        Assert.Null(anlagen[1].SatzEinspCt);
        Assert.Equal(BhkwSprung.Strombezug, ergebnis!.Sprung);
        Assert.True(ergebnis.Gespeichert);
    }

    /// <summary>Fall (b), die andere Seite: nur die Projektvorgaben geaendert.</summary>
    [Fact]
    public void Geaenderte_Projektvorgaben_schreiben_nur_die_Vorgaben_und_springen()
    {
        var anlagen = ZweiAnlagen();
        var p = new WirtschaftlichkeitParameter();
        var z = new Schreibzaehler();
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(anlagen, p, e => ergebnis = e, z.Anlage, z.Vorgaben);

        Koerper(cut, 4).QuerySelectorAll("input[type=checkbox]")[0].Change(true);
        cut.FindAll("button.epos-sprung")[1].Click();

        Assert.Equal(new[] { "Vorgaben" }, z.Wege);
        Assert.True(p.RaeumlicherZusammenhang);
        Assert.Equal(BhkwSprung.BhkwTarif, ergebnis!.Sprung);
        Assert.True(ergebnis.Gespeichert);
    }

    /// <summary>
    /// Fall (c), Muster Ae25: Schlaegt das Schreiben fehl, findet der Sprung
    /// NICHT statt und die Maske bleibt offen — mit demselben Fehlerband wie im
    /// OK-Weg.
    /// </summary>
    [Fact]
    public void Scheitert_das_Schreiben_findet_der_Sprung_nicht_statt()
    {
        var z = new Schreibzaehler { VorgabenAntwort = _ => false };
        BhkwWirtschaftlichkeitErgebnis? ergebnis = null;
        var cut = Aufbauen(beimSchliessen: e => ergebnis = e,
                           speichereAnlage: z.Anlage, speichereVorgaben: z.Vorgaben);

        Koerper(cut, 4).QuerySelectorAll("input[type=checkbox]")[0].Change(true);
        cut.FindAll("button.epos-sprung")[0].Click();

        Assert.Null(ergebnis);                 // kein Sprung, die Maske bleibt offen
        Assert.False(cut.Instance.Gespeichert);
        Assert.Equal("1 Angabe(n) konnten nicht gespeichert werden.",
                     cut.FindAll(".epos-warnbanner-text")[^1].TextContent);
    }

    /// <summary>
    /// Der Satz unter den Knoepfen kuendigt das Schreiben an — er steht deshalb
    /// nur, wenn ein Sprung jetzt wirklich schreiben wuerde.
    /// </summary>
    [Fact]
    public void Die_Zeile_unter_den_Sprungknoepfen_steht_nur_bei_geaendertem_Stand()
    {
        var cut = Aufbauen();

        Assert.DoesNotContain("Der Sprung speichert die Eingaben", Koerper(cut, 4).TextContent);

        Koerper(cut, 4).QuerySelectorAll("input[type=checkbox]")[0].Change(true);

        Assert.Contains("Der Sprung speichert die Eingaben", Koerper(cut, 4).TextContent);
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

using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Spaltenwahl und Verwendungsmarke der Katalogliste</b> (Konzept Projektdialoge mit
/// Katalogauswahl 4.10, Anwenderentscheid 10.10.2026): Die Verwendung im Projekt steht als
/// Marke am Bezeichner und als Toenung der Zeile, die Spalte „im Projekt verwendet“ ist
/// standardmaessig aus und waehlbar; die Wahl der Spalten wird je Dialog unter
/// <c>Katalogauswahl.Spalten.&lt;Dialogname&gt;</c> gemerkt, „Standard“ loescht sie.
/// </summary>
public class KataloglisteSpaltenwahlTests : EposBunitContext
{
    private const string NAME = "Probe";
    private const string SCHLUESSEL = Katalogliste.SPALTEN_PRAEFIX + NAME;

    public KataloglisteSpaltenwahlTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private static Katalogfilterprofil Profil() =>
        Katalogfilterprofil.MitVerwendung(Anlagenart.Heizkessel, s => s);

    private static Katalogfilterzeile Zeile(int id, string name, string firma, double pth) =>
        new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpHersteller, firma)
            .MitText(Katalogfilterprofil.SpBrennstoff, "Erdgas E")
            .MitZahl(Katalogfilterprofil.SpPtherm, pth);

    private static List<Katalogfilterzeile> Zeilen(params string[] imProjekt)
    {
        var zeilen = new List<Katalogfilterzeile>
        {
            Zeile(1, "Alpha", "Werk A", 15),
            Zeile(2, "Beta", "Werk B", 80),
            Zeile(3, "Gamma", "Werk A", 40)
        };
        Katalogverwendung.Stempeln(zeilen, imProjekt);
        return zeilen;
    }

    private IRenderedComponent<Katalogliste> Aufbauen(IReadOnlyList<Katalogfilterzeile> zeilen,
                                                      string? name = NAME)
        => Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, zeilen)
            .Add(x => x.Filterstand, new Katalogfilterstand())
            .Add(x => x.Spaltenwahlname, name));

    private static List<string> Kopf(IRenderedComponent<Katalogliste> cut)
        => cut.FindAll("thead th").Select(e => e.TextContent.Trim()).ToList();

    /// <summary>Setzt eine fluechtige Ablage fuer <c>Dienste.Einstellungen</c> und stellt sie zurueck.</summary>
    private static void MitAblage(Action<IEinstellungen> lauf)
    {
        IEinstellungen alt = WindowsFormsApplication1.Dienste.Einstellungen;
        var ablage = new FluechtigeEinstellungen();
        WindowsFormsApplication1.Dienste.Einstellungen = ablage;
        try { lauf(ablage); }
        finally { WindowsFormsApplication1.Dienste.Einstellungen = alt; }
    }

    private static void Oeffnen(IRenderedComponent<Katalogliste> cut)
        => cut.Find("button.epos-katalog-spaltenknopf").Click();

    private static void Umschalten(IRenderedComponent<Katalogliste> cut, string spalte)
        => cut.Find($".epos-spaltenwahl-eintrag[data-spalte='{spalte}'] input").Change(true);

    // =====================================================================
    //  Die Verwendungsmarke
    // =====================================================================

    [Fact]
    public void Die_verwendete_Zeile_traegt_Marke_Hinweis_und_Toenung_statt_einer_Spalte()
    {
        var cut = Aufbauen(Zeilen("Beta"));

        var marken = cut.FindAll("tbody .epos-verwendet-marke");
        Assert.Single(marken);
        Assert.Equal(Resource.KFLT_VERWENDET_HINWEIS, marken[0].GetAttribute("title"));

        var zeilen = cut.FindAll("tbody tr");
        var getoent = zeilen.Where(z => (z.ClassName ?? "").Contains(Katalogliste.VERWENDET_KLASSE)).ToList();
        Assert.Single(getoent);
        Assert.Contains("Beta", getoent[0].TextContent);

        // Die Spalte steht nicht da und nimmt keinem Rang den Platz.
        Assert.DoesNotContain(Kopf(cut), k => k.StartsWith("KFLT_SP_VERWENDET"));
        Assert.DoesNotContain(Katalogfilterprofil.SpVerwendet, cut.Instance.AngezeigteSpalten);
        Assert.False(cut.Instance.Stufen.ContainsKey(Katalogfilterprofil.SpVerwendet));
    }

    [Fact]
    public void Die_Legende_steht_nur_wenn_eine_Zeile_verwendet_ist()
    {
        Assert.Empty(Aufbauen(Zeilen()).FindAll(".epos-katalog-verwendetlegende"));

        var legende = Aufbauen(Zeilen("Alpha")).Find(".epos-katalog-verwendetlegende");
        Assert.Contains(Resource.KFLT_VERWENDET_LEGENDE, legende.TextContent);
        Assert.Equal(Resource.KFLT_VERWENDET_HINWEIS, legende.GetAttribute("title"));
        Assert.NotNull(legende.QuerySelector(".epos-verwendet-marke"));
    }

    [Fact]
    public void Stempeln_setzt_Kennzeichen_und_Wert_zusammen()
    {
        var zeilen = Zeilen("gamma");      // gross/klein egal
        Assert.Equal(new[] { false, false, true }, zeilen.Select(z => z.ImProjekt));
        Katalogverwendung.Stempeln(zeilen, Array.Empty<string>());
        Assert.All(zeilen, z => Assert.False(z.ImProjekt));
    }

    // =====================================================================
    //  Die Spaltenwahl
    // =====================================================================

    [Fact]
    public void Ohne_Dialogname_gibt_es_keinen_Spaltenknopf()
    {
        var cut = Aufbauen(Zeilen(), name: null);
        Assert.Empty(cut.FindAll("button.epos-katalog-spaltenknopf"));
    }

    [Fact]
    public void Die_Auswahl_traegt_ein_Kaestchen_je_waehlbarer_Spalte_in_zwei_Spalten()
    {
        MitAblage(_ =>
        {
            var cut = Aufbauen(Zeilen());
            var knopf = cut.Find("button.epos-katalog-spaltenknopf");
            Assert.Equal(Resource.KFLT_SPALTEN_HINWEIS, knopf.GetAttribute("title"));
            Assert.Equal("false", knopf.GetAttribute("aria-expanded"));

            Oeffnen(cut);
            Assert.Equal("true", cut.Find("button.epos-katalog-spaltenknopf").GetAttribute("aria-expanded"));

            var eintraege = cut.FindAll(".epos-spaltenwahl-eintrag");
            Assert.Equal(Profil().Spalten.Count - 1, eintraege.Count);     // ohne Bezeichner
            Assert.DoesNotContain(eintraege, e => e.GetAttribute("data-spalte") == Katalogfilterprofil.SpBezeichner);
            Assert.False(cut.Find($".epos-spaltenwahl-eintrag[data-spalte='{Katalogfilterprofil.SpVerwendet}'] input")
                            .HasAttribute("checked"));
            Assert.True(cut.Find($".epos-spaltenwahl-eintrag[data-spalte='{Katalogfilterprofil.SpHersteller}'] input")
                           .HasAttribute("checked"));
            Assert.NotNull(cut.Find(".epos-spaltenwahl-liste--zwei"));
            Assert.True(cut.Find("button.epos-spaltenwahl-standard").HasAttribute("disabled"));
        });
    }

    [Fact]
    public void Abwaehlen_blendet_aus_und_merkt_die_Wahl_je_Dialog()
    {
        MitAblage(ablage =>
        {
            var cut = Aufbauen(Zeilen());
            Oeffnen(cut);
            Umschalten(cut, Katalogfilterprofil.SpHersteller);

            Assert.DoesNotContain(Kopf(cut), k => k.StartsWith("KFLT_SP_HERSTELLER"));
            Assert.True(cut.Instance.SpaltenGewaehlt);
            string gemerkt = ablage.Lies(SCHLUESSEL);
            Assert.DoesNotContain(Katalogfilterprofil.SpHersteller, gemerkt.Split(','));
            Assert.Contains(Katalogfilterprofil.SpPtherm, gemerkt.Split(','));

            // Mit gemerkter Wahl weicht keine Spalte nach der Breite.
            Assert.Contains("epos-katalogliste--spaltenwahl", cut.Find(".epos-katalogliste").ClassName);
            Assert.DoesNotContain("epos-katalogliste--raenge", cut.Find(".epos-katalogliste").ClassName);
            Assert.Empty(cut.FindAll("th[class*='epos-spalte-ab-']"));

            // Wieder waehlen: die Spalte steht wieder da.
            Umschalten(cut, Katalogfilterprofil.SpHersteller);
            Assert.Contains(Kopf(cut), k => k.StartsWith("KFLT_SP_HERSTELLER"));
        });
    }

    [Fact]
    public void Die_gemerkte_Wahl_gilt_beim_naechsten_Aufbau_auch_fuer_die_Verwendungsspalte()
    {
        MitAblage(ablage =>
        {
            ablage.Schreib(SCHLUESSEL, "VERWENDET, PTHERM,UNBEKANNT");
            var cut = Aufbauen(Zeilen("Alpha"));

            Assert.Equal(new[] { Katalogfilterprofil.SpBezeichner, Katalogfilterprofil.SpPtherm,
                                 Katalogfilterprofil.SpVerwendet },
                         cut.Instance.AngezeigteSpalten);
            Assert.Contains(Kopf(cut), k => k.StartsWith("KFLT_SP_VERWENDET"));
            // Die Marke bleibt auch mit Spalte.
            Assert.Single(cut.FindAll("tbody .epos-verwendet-marke"));
        });
    }

    [Fact]
    public void Standard_loescht_die_gemerkte_Wahl()
    {
        MitAblage(ablage =>
        {
            ablage.Schreib(SCHLUESSEL, "PTHERM");
            var cut = Aufbauen(Zeilen());
            Oeffnen(cut);
            var standard = cut.Find("button.epos-spaltenwahl-standard");
            Assert.False(standard.HasAttribute("disabled"));
            standard.Click();

            Assert.Null(ablage.Lies(SCHLUESSEL));
            Assert.False(cut.Instance.SpaltenGewaehlt);
            Assert.Contains(Katalogfilterprofil.SpHersteller, cut.Instance.AngezeigteSpalten);
            Assert.DoesNotContain(Katalogfilterprofil.SpVerwendet, cut.Instance.AngezeigteSpalten);
        });
    }

    [Fact]
    public void Alle_abgewaehlt_ist_eine_Wahl_und_nicht_der_Standard()
    {
        MitAblage(ablage =>
        {
            var cut = Aufbauen(Zeilen());
            Oeffnen(cut);
            foreach (string s in cut.Instance.AngezeigteSpalten.Where(s => s != Katalogfilterprofil.SpBezeichner).ToList())
                Umschalten(cut, s);

            Assert.Equal(new[] { Katalogfilterprofil.SpBezeichner }, cut.Instance.AngezeigteSpalten);
            var neu = Aufbauen(Zeilen());
            Assert.Equal(new[] { Katalogfilterprofil.SpBezeichner }, neu.Instance.AngezeigteSpalten);
        });
    }

    [Fact]
    public void Esc_schliesst_die_Auswahl()
    {
        MitAblage(_ =>
        {
            var cut = Aufbauen(Zeilen());
            Oeffnen(cut);
            cut.Find(".epos-spaltenwahl-auswahl").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });
            Assert.Empty(cut.FindAll(".epos-spaltenwahl-auswahl"));
            Assert.False(cut.Instance.SpaltenwahlOffen);
        });
    }

    [Fact]
    public void In_der_Katalogauswahl_gilt_der_Dialogname_des_Bausteins()
    {
        MitAblage(_ =>
        {
            RenderFragment liste = b =>
            {
                b.OpenComponent<Katalogliste>(0);
                b.AddAttribute(1, nameof(Katalogliste.Profil), Profil());
                b.AddAttribute(2, nameof(Katalogliste.Zeilen), (IReadOnlyList<Katalogfilterzeile>)Zeilen("Beta"));
                b.AddAttribute(3, nameof(Katalogliste.Filterstand), new Katalogfilterstand());
                b.CloseComponent();
            };
            var cut = Render<Zweispaltenauswahl>(p => p
                .Add(x => x.Dialogname, "Heizkessel")
                .Add(x => x.Rechts, liste));

            var kl = cut.FindComponent<Katalogliste>();
            Assert.Equal("Katalogauswahl.Spalten.Heizkessel", kl.Instance.Spaltenschluessel);
            // Knopf und Legende stehen in der Kopfleiste des Katalogs.
            var kopfleiste = cut.Find(".epos-zweispalten-bereich--katalog .epos-zweispalten-kopfleiste");
            Assert.NotNull(kopfleiste.QuerySelector("button.epos-katalog-spaltenknopf"));
            Assert.NotNull(kopfleiste.QuerySelector(".epos-katalog-verwendetlegende"));
        });
    }
}

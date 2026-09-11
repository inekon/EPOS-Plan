using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Bunit;
using EPOS.UI.Bausteine;
using Microsoft.AspNetCore.Components.Web;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// <b>Der VERGLEICH zweier bis dreier Katalogsätze</b> (Frage <b>Q3</b> des
/// Anwenderentscheids <b>W14a‑E‑10</b> vom 07.09.2026, Konzept_Katalogfilter 5.5,
/// Stufe <b>S3.3</b>).
///
/// <para><b>Was hier festgehalten wird.</b> Der Vergleich sitzt im BAUSTEIN und
/// gilt damit in allen fünfzehn Wirten auf einen Schlag: Strg- oder
/// Umschalt-Klick markiert eine Zeile (und lässt die Wahl in Ruhe), zwei bis drei
/// Markierungen machen den Knopf frei, die VIERTE wird abgewiesen — mit Hinweis,
/// und die drei bestehenden bleiben stehen. Die Überlagerung trägt EINE Zeile je
/// Parameter und EINE Spalte je Gerät, abweichende Zeilen sind gekennzeichnet —
/// mit WORTEN, nicht nur mit Farbe.</para>
///
/// <para><b>Zwei Quellen, eine Tabelle.</b> Mit <c>Vergleichsparameter</c> kommen
/// die Zeilen aus der Parameterübersicht (W14a‑E‑8, also aus
/// <c>ParameterVerwendung</c>); ohne den Delegaten stehen die Spalten des Profils.
/// Beide Wege sind hier belegt — der Rückfall trägt die Bedarfs- und
/// Zeitreihenkataloge aus S3.1/S3.2, die keine Parameterübersicht haben.</para>
///
/// <para>Kultur gepinnt (Hausregel seit iU9‑W8).</para>
/// </summary>
public class KatalogVergleichTests : EposBunitContext
{
    public KatalogVergleichTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    // =====================================================================
    //  Probedaten - vier Heizkessel, damit die GRENZE prüfbar ist
    // =====================================================================

    private static Katalogfilterprofil Profil() =>
        Katalogfilterprofil.Finde(Anlagenart.Heizkessel, s => s);

    private static Katalogfilterzeile Zeile(int id, string name, string firma, string brennstoff,
                                            double? pth, double? eta, bool brennwert)
    {
        return new Katalogfilterzeile(id, name)
            .MitText(Katalogfilterprofil.SpBezeichner, name)
            .MitText(Katalogfilterprofil.SpHersteller, firma)
            .MitText(Katalogfilterprofil.SpBrennstoff, brennstoff)
            .MitZahl(Katalogfilterprofil.SpPtherm, pth)
            .MitZahl(Katalogfilterprofil.SpEta, eta, 3)
            .MitKennzeichen(Katalogfilterprofil.SpBrennwert, brennwert);
    }

    private static List<Katalogfilterzeile> Zeilen() => new()
    {
        Zeile(1, "Alpha", "Vaillant", "Erdgas E",  15.0, 0.97, true),
        Zeile(2, "Beta",  "Buderus",  "Heizöl EL", 80.0, 0.90, false),
        Zeile(3, "Gamma", "Vaillant", "Stadtgas",  40.0, 0.98, false),
        Zeile(4, "Delta", "Viessmann", "Erdgas E", 15.0, 0.97, true)
    };

    /// <summary>
    /// Ein Parametersatz wie ihn <c>ParameterUebersichtCtrl.Werte</c> liefert —
    /// hier von Hand, damit der Fall ohne Datenbank läuft.
    /// </summary>
    private static IReadOnlyList<Parameterwert> Uebersicht(string bezeichner)
    {
        var stufe = new[] { Verwendung.Simulation };
        string leistung = bezeichner == "Beta" ? "80" : "15";
        return new[]
        {
            new Parameterwert(
                new ParameterEintrag("Bezeichner", "Bezeichner", "", stufe, "x"), bezeichner),
            new Parameterwert(
                new ParameterEintrag("Pth", "Thermische Leistung", "kW", stufe, "x"), leistung),
            new Parameterwert(
                new ParameterEintrag("Regelbereich", "Regelbereich", "%", stufe, "x"), "30")
        };
    }

    private IRenderedComponent<Katalogliste> Aufbauen(
        Func<string, IReadOnlyList<Parameterwert>>? uebersicht = null)
    {
        return Render<Katalogliste>(p =>
        {
            p.Add(x => x.Profil, Profil());
            p.Add(x => x.Zeilen, Zeilen());
            p.Add(x => x.Filterstand, new Katalogfilterstand());
            if (uebersicht is not null) p.Add(x => x.Vergleichsparameter, uebersicht);
        });
    }

    /// <summary>Markiert die Zeile <paramref name="index"/> mit Strg-Klick.</summary>
    private static void Markieren(IRenderedComponent<Katalogliste> cut, int index,
                                  bool umschalt = false)
    {
        cut.FindAll(".epos-anlagenwahl")[index]
           .Click(new MouseEventArgs { CtrlKey = !umschalt, ShiftKey = umschalt });
    }

    // =====================================================================
    //  Der Knopf und die Markierung
    // =====================================================================

    /// <summary>
    /// Der Knopf steht IMMER da — wäre er nur bei Markierung sichtbar, fände den
    /// Weg dorthin niemand. Ohne Markierung ist er gesperrt und sagt im Kurztext,
    /// WIE man markiert; Strg-Klick sieht man sonst nicht.
    /// </summary>
    [Fact]
    public void Ohne_Markierung_steht_der_Knopf_da_und_ist_gesperrt()
    {
        var cut = Aufbauen();

        var knopf = cut.Find(".epos-katalog-vergleichknopf");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KFLT_VERGLEICH_HINWEIS, knopf.GetAttribute("title"));
        Assert.Empty(cut.Instance.Markiert);
    }

    /// <summary>
    /// EINE Markierung genügt nicht — verglichen wird ab ZWEI. Der Knopf nennt
    /// dabei die Zahl, damit man sieht, dass der Klick angekommen ist.
    /// </summary>
    [Fact]
    public void Eine_Markierung_laesst_den_Knopf_gesperrt()
    {
        var cut = Aufbauen();

        Markieren(cut, 0);

        Assert.Single(cut.Instance.Markiert);
        var knopf = cut.Find(".epos-katalog-vergleichknopf");
        Assert.True(knopf.HasAttribute("disabled"));
        Assert.Contains("1", knopf.TextContent);
        Assert.False(cut.Instance.VergleichOffen);
    }

    /// <summary>
    /// <b>Der Strg-Klick markiert und WÄHLT NICHT.</b> Das ist der Kern der
    /// Bedienung: Ein blanker Klick füllt weiterhin den Detailblock des Wirtes,
    /// die Zusatztaste legt die Zeile daneben. <c>Zeilenwahl</c> meldet
    /// <c>Tastenwahl</c> VOR <c>Gewaehltwerden</c> — sonst wäre beides nicht zu
    /// trennen.
    /// </summary>
    [Fact]
    public void Der_Strg_Klick_markiert_und_laesst_die_Wahl_in_Ruhe()
    {
        string gewaehlt = "?";
        var cut = Render<Katalogliste>(p => p
            .Add(x => x.Profil, Profil())
            .Add(x => x.Zeilen, Zeilen())
            .Add(x => x.Filterstand, new Katalogfilterstand())
            .Add(x => x.GewaehltChanged,
                 Microsoft.AspNetCore.Components.EventCallback.Factory
                     .Create<string>(this, w => gewaehlt = w)));

        Markieren(cut, 0);

        Assert.Equal(new[] { "Alpha" }, cut.Instance.Markiert);
        Assert.Equal("?", gewaehlt);

        // Ein BLANKER Klick waehlt weiterhin - und markiert nichts.
        cut.FindAll(".epos-anlagenwahl")[1].Click();
        Assert.Equal("Beta", gewaehlt);
        Assert.Equal(new[] { "Alpha" }, cut.Instance.Markiert);
    }

    /// <summary>
    /// Umschalt tut dasselbe wie Strg und wählt KEINEN Bereich: Bei höchstens drei
    /// Geräten wäre ein Bereich in aller Regel schon zu groß.
    /// </summary>
    [Fact]
    public void Umschalt_markiert_wie_Strg_und_waehlt_keinen_Bereich()
    {
        var cut = Aufbauen();

        Markieren(cut, 0);
        Markieren(cut, 2, umschalt: true);

        Assert.Equal(new[] { "Alpha", "Gamma" }, cut.Instance.Markiert);
    }

    /// <summary>Ein zweiter Strg-Klick nimmt die Markierung wieder weg.</summary>
    [Fact]
    public void Ein_zweiter_Klick_nimmt_die_Markierung_zurueck()
    {
        var cut = Aufbauen();

        Markieren(cut, 0);
        Markieren(cut, 0);

        Assert.Empty(cut.Instance.Markiert);
    }

    /// <summary>
    /// Eine markierte Zeile ist zu SEHEN — am Wahlknopf, weil
    /// <c>ColumnBase.Class</c> der ganzen Spalte gilt — und für die Sprachausgabe
    /// steht sie im Kurztext.
    /// </summary>
    [Fact]
    public void Eine_markierte_Zeile_ist_erkennbar()
    {
        var cut = Aufbauen();

        Markieren(cut, 1);

        var knopf = cut.FindAll(".epos-anlagenwahl")[1];
        Assert.Contains("epos-anlagenwahl--markiert", knopf.GetAttribute("class"));
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.KFLT_VERGLEICH_TITEL, knopf.GetAttribute("title"));

        Assert.DoesNotContain("epos-anlagenwahl--markiert",
                              cut.FindAll(".epos-anlagenwahl")[0].GetAttribute("class") ?? "");
    }

    // =====================================================================
    //  Die GRENZE (5.5: "bei vier Markierungen: Hinweis")
    // =====================================================================

    /// <summary>
    /// <b>Die VIERTE Markierung fällt, nicht die älteste.</b> Wer drei Geräte
    /// nebeneinandergelegt hat, hat sie ausgesucht — ihm still das erste
    /// wegzunehmen wäre die überraschendere Antwort. Stattdessen steht ein
    /// Hinweis da, und die drei bleiben.
    /// </summary>
    [Fact]
    public void Die_vierte_Markierung_wird_abgewiesen_und_die_drei_bleiben()
    {
        var cut = Aufbauen();

        Markieren(cut, 0);
        Markieren(cut, 1);
        Markieren(cut, 2);
        Assert.Equal(Katalogliste.HOECHSTENS, cut.Instance.Markiert.Count);
        Assert.Empty(cut.FindAll(".epos-katalog-markierhinweis"));

        Markieren(cut, 3);

        Assert.Equal(new[] { "Alpha", "Beta", "Gamma" }, cut.Instance.Markiert);
        Assert.Equal(WindowsFormsApplication1.MyResource.Resource.KFLT_VERGLEICH_GRENZE,
                     cut.Find(".epos-katalog-markierhinweis").TextContent);
        Assert.Equal("alert", cut.Find(".epos-katalog-markierhinweis").GetAttribute("role"));

        // Der Knopf bleibt FREI - drei sind ein gueltiger Vergleich.
        Assert.False(cut.Find(".epos-katalog-vergleichknopf").HasAttribute("disabled"));
    }

    // =====================================================================
    //  Die Überlagerung
    // =====================================================================

    /// <summary>
    /// Zwei Markierungen machen den Knopf frei; der Vergleich steht dann als
    /// Überlagerung im selben Fenster (R2 — kein zweites Fenster), mit EINER
    /// Spalte je Gerät plus der Parameterspalte.
    /// </summary>
    [Fact]
    public void Zwei_Markierungen_oeffnen_den_Vergleich_mit_zwei_Geraetespalten()
    {
        var cut = Aufbauen(Uebersicht);

        Markieren(cut, 0);
        Markieren(cut, 1);

        var knopf = cut.Find(".epos-katalog-vergleichknopf");
        Assert.False(knopf.HasAttribute("disabled"));
        knopf.Click();

        Assert.True(cut.Instance.VergleichOffen);

        var koepfe = cut.FindAll(".epos-vergleich thead th");
        Assert.Equal(3, koepfe.Count);                       // Parameter + zwei Geraete
        Assert.Equal("Alpha", koepfe[1].TextContent);
        Assert.Equal("Beta", koepfe[2].TextContent);

        // EINE Zeile je Parameter - hier die drei aus der Uebersicht.
        Assert.Equal(3, cut.FindAll(".epos-vergleich tbody tr").Count);
        Assert.Equal(3, cut.Instance.Vergleich.Count);
    }

    /// <summary>
    /// Drei Markierungen ergeben drei Gerätespalten — die Höchstzahl aus 5.5.
    /// </summary>
    [Fact]
    public void Drei_Markierungen_ergeben_drei_Geraetespalten()
    {
        var cut = Aufbauen(Uebersicht);

        Markieren(cut, 0);
        Markieren(cut, 1);
        Markieren(cut, 2);
        cut.Find(".epos-katalog-vergleichknopf").Click();

        Assert.Equal(4, cut.FindAll(".epos-vergleich thead th").Count);
        Assert.Equal(3, cut.FindAll(".epos-vergleich tbody tr:first-child td").Count);
    }

    /// <summary>
    /// <b>Abweichende Zeilen sind hervorgehoben — und tragen WORTE.</b> Dieselbe
    /// Auflage wie beim Trichter: Wer nur Farbe nimmt, trägt in Graustufen und in
    /// der Sprachausgabe nichts. „Regelbereich" ist bei beiden 30 % und damit
    /// stimmig, die Leistung 15 gegen 80 kW ist es nicht.
    /// </summary>
    [Fact]
    public void Abweichende_Zeilen_sind_hervorgehoben_und_benannt()
    {
        var cut = Aufbauen(Uebersicht);

        Markieren(cut, 0);
        Markieren(cut, 1);
        cut.Find(".epos-katalog-vergleichknopf").Click();

        var zeilen = cut.FindAll(".epos-vergleich tbody tr");

        // Bezeichner und Leistung weichen ab, der Regelbereich nicht.
        Assert.Contains("epos-vergleich--abweichend", zeilen[0].GetAttribute("class"));
        Assert.Contains("epos-vergleich--abweichend", zeilen[1].GetAttribute("class"));
        Assert.Contains("epos-vergleich--stimmig", zeilen[2].GetAttribute("class"));

        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.KFLT_VERGLEICH_ABWEICHEND, zeilen[1].TextContent);
        Assert.Contains(WindowsFormsApplication1.MyResource.Resource.KFLT_VERGLEICH_STIMMIG, zeilen[2].TextContent);

        Assert.True(cut.Instance.Vergleich[1].Abweichend);
        Assert.False(cut.Instance.Vergleich[2].Abweichend);
    }

    /// <summary>
    /// <b>Die Zeilen kommen aus der PARAMETERÜBERSICHT</b>, nicht aus den
    /// Listenspalten: Der Regelbereich steht in keiner Spalte des Profils und
    /// trotzdem im Vergleich — das ist der ganze Zweck von <c>ParameterVerwendung</c>
    /// (W14a‑E‑8). Die Einheit steht dabei am Namen.
    /// </summary>
    [Fact]
    public void Die_Zeilen_kommen_aus_der_Parameteruebersicht()
    {
        var cut = Aufbauen(Uebersicht);

        Markieren(cut, 0);
        Markieren(cut, 1);
        cut.Find(".epos-katalog-vergleichknopf").Click();

        string[] namen = cut.Instance.Vergleich.Select(v => v.Name).ToArray();
        Assert.Equal(new[] { "Bezeichner", "Thermische Leistung [kW]", "Regelbereich [%]" }, namen);

        // "Regelbereich" fuehrt KEINE Spalte des Profils.
        Assert.DoesNotContain(Profil().Spalten, s => s.Kopftext.Contains("Regelbereich"));
    }

    /// <summary>
    /// <b>Ohne Delegat vergleicht die Liste ihre eigenen SPALTEN.</b> Das ist der
    /// Rückfall für die Bedarfs- und Zeitreihenkataloge aus S3.1/S3.2 — sie sind
    /// keine Anlagen und haben keine Parameterübersicht. Er ist immer noch mehr,
    /// als der Bestand zeigte.
    /// </summary>
    [Fact]
    public void Ohne_Delegat_stehen_die_Spalten_des_Profils()
    {
        var cut = Aufbauen();

        Markieren(cut, 0);
        Markieren(cut, 1);
        cut.Find(".epos-katalog-vergleichknopf").Click();

        Assert.Equal(Profil().Spalten.Count, cut.Instance.Vergleich.Count);
        Assert.Equal(Profil().Spalten.Select(s => s.Kopftext).ToArray(),
                     cut.Instance.Vergleich.Select(v => v.Name).ToArray());

        // Und die Werte sind die der Liste - Alpha 15 kW gegen Beta 80 kW.
        Katalogspalte pth = Profil().Spalten.First(
            s => s.Schluessel == Katalogfilterprofil.SpPtherm);
        Katalogliste.Vergleichzeile leistung =
            cut.Instance.Vergleich.First(v => v.Name == pth.Kopftext);
        Assert.True(leistung.Abweichend);
    }

    /// <summary>
    /// Ein Klick auf den gesperrten Knopf öffnet nichts — die Sperre ist echt und
    /// nicht nur Anmutung.
    /// </summary>
    [Fact]
    public void Der_gesperrte_Knopf_oeffnet_nichts()
    {
        var cut = Aufbauen(Uebersicht);

        Markieren(cut, 0);
        cut.Find(".epos-katalog-vergleichknopf").Click();

        Assert.False(cut.Instance.VergleichOffen);
        Assert.Empty(cut.FindAll(".epos-vergleich"));
    }
}

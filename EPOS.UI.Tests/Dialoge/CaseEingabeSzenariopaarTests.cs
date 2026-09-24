using System.Globalization;
using System.Reflection;
using Bunit;
using EPOS.UI.Dialoge.Kosten;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

/// <summary>
/// ETAPPE E9b (Konzept § 2.11.5 „Pflege"; Entscheid E9b‑Q1, Lesart a) — der
/// <c>CaseEingabeDialog</c> als <b>allgemeiner Baustein „Szenariopaar"</b>: Mit dem
/// Parametersatz aus <see cref="Szenariopaar.Gaben"/> pflegt er das Best/Worst-Paar eines
/// Trägerpreises oder Erlössatzes statt einer Kostenposition.
///
/// <para>Die Kostenposition selbst hält <see cref="CaseEingabeDialogTests"/> unverändert —
/// jeder Fall dort läuft ohne Anpassung weiter. Hier steht, was im Szenariopaar ANDERS
/// ist: nur Best und Worst (ohne Nutzungsdauer, Startjahr, Zuschuss), Einheit und Stellen
/// des Wirts, die Erwartet-Zeile, die Warnung ohne Erwartet-Wert (E9b‑Q4: warnen, nicht
/// verweigern), die Kohärenzzeilen, „leer heißt wie Erwartet", der eigene Hilfeschlüssel
/// und die drei Auskunftsfelder des Assistenten.</para>
///
/// <para>Der Wirt splattet den Satz wie die echten Wirte (<c>@attributes</c>) und setzt
/// <c>TitelText=""</c> rechts davon — Titel und Kreuz trägt die Überlagerung.</para>
/// </summary>
public class CaseEingabeSzenariopaarTests : EposBunitContext
{
    public CaseEingabeSzenariopaarTests()
    {
        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
    }

    /// <summary>
    /// Ein Wirt wie Trägerkarte, Parameterdialog, BHKW- und PV-Dialog: der Parametersatz
    /// als Attributsatz, <c>TitelText=""</c> rechts davon. <c>public</c>, weil bunit die
    /// <c>[Parameter]</c> über Reflexion belegt.
    /// </summary>
    public sealed class Wirt : ComponentBase
    {
        [Parameter] public IReadOnlyDictionary<string, object> Gaben { get; set; }
            = new Dictionary<string, object>();

        [Parameter] public EventCallback<CaseEingabeErgebnis?> Geschlossen { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<CaseEingabeDialog>(0);
            builder.AddMultipleAttributes(1, Gaben);
            builder.AddAttribute(2, nameof(CaseEingabeDialog.TitelText), "");
            builder.AddAttribute(3, nameof(CaseEingabeDialog.Geschlossen), Geschlossen);
            builder.CloseComponent();
        }
    }

    /// <summary>Der Satz der Trägerkarte für einen Arbeitspreis in €/Nm³ (vier Stellen).</summary>
    private static IReadOnlyDictionary<string, object> Arbeitspreis(
        double? erwartet = 0.65, double? best = null, double? worst = null,
        bool warnen = true, IEnumerable<string>? hinweise = null)
        => Szenariopaar.Gaben("Arbeitspreis", "Arbeitspreis Erdgas H", "€/Nm³", erwartet,
                              best, worst, 4, 10_000_000, hinweise, warnen);

    private CaseEingabeErgebnis? _ergebnis;
    private int _gemeldet;

    private IRenderedComponent<Wirt> Zeige(IReadOnlyDictionary<string, object> gaben)
        => Render<Wirt>(p => p
            .Add(x => x.Gaben, gaben)
            .Add(x => x.Geschlossen, e => { _ergebnis = e; _gemeldet++; }));

    private static CaseEingabeDialog Blatt(IRenderedComponent<Wirt> cut)
        => cut.FindComponent<CaseEingabeDialog>().Instance;

    // =====================================================================
    //  Feldbestand
    // =====================================================================

    [Fact]
    public void Das_Szenariopaar_fuehrt_nur_Best_und_Worst_in_der_Einheit_des_Wirts()
    {
        var cut = Zeige(Arbeitspreis());

        Assert.Equal(2, cut.FindAll("input[inputmode=decimal]").Count);
        Assert.Empty(cut.FindAll("input[inputmode=numeric]"));      // kein Startjahr
        Assert.Empty(cut.FindAll("input[type=checkbox]"));          // kein Zuschuss
        Assert.Equal(2, cut.FindAll("input[type=radio]").Count);    // absolut / %

        Assert.Equal(new[] { "Arbeitspreis" },
                     cut.FindAll(".epos-gruppenkopf-titel").Select(e => e.TextContent).ToArray());

        var texte = cut.FindAll(".epos-feld-text").Select(e => e.TextContent).ToArray();
        Assert.Equal(new[] { "Eingabe absolut [€/Nm³]", "Eingabe in % vom Erwartet-Wert",
                             "Best (Günstig):", "Worst (Ungünstig):" }, texte);
        Assert.All(cut.FindAll(".epos-formularraster .epos-einheit"),
                   e => Assert.Equal("€/Nm³", e.TextContent));

        // Ein Titel, eine Stelle: Titel und Kreuz trägt der Wirt.
        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
    }

    [Fact]
    public void Die_Erwartet_Zeile_steht_zuerst_und_die_Leerregel_darunter()
    {
        var cut = Zeige(Arbeitspreis());

        var zeilen = cut.FindAll(".epos-herleitung-text").Select(e => e.TextContent).ToList();
        Assert.Equal("Erwartet: 0,6500 €/Nm³", zeilen[0]);
        Assert.Contains("Leer oder 0 heißt „wie Erwartet“; ein Wert gleich dem Erwartet-Wert ist keine Pflege.",
                        zeilen);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
        Assert.False(Blatt(cut).OhneErwartet);
    }

    // =====================================================================
    //  Werte und Nullregel
    // =====================================================================

    /// <summary>
    /// „Nicht gepflegt" ist LEER, nicht 0 — eine 0 sähe wie ein Preis aus. OK mit leeren
    /// Feldern meldet 0, und <see cref="Szenariopaar.Wert"/> macht daraus das
    /// <c>null</c> der Datenhaltung („wie Erwartet").
    /// </summary>
    [Fact]
    public void Leere_Felder_heissen_wie_Erwartet_und_gehen_als_null_zurueck()
    {
        var cut = Zeige(Arbeitspreis());
        var felder = cut.FindAll("input[inputmode=decimal]");
        Assert.Equal("", felder[0].GetAttribute("value") ?? "");
        Assert.Equal("", felder[1].GetAttribute("value") ?? "");

        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(1, _gemeldet);
        Assert.NotNull(_ergebnis);
        Assert.Equal(0, _ergebnis!.BestCase);
        Assert.Equal(0, _ergebnis.WorstCase);
        Assert.Null(Szenariopaar.Wert(_ergebnis.BestCase));
        Assert.Null(Szenariopaar.Wert(_ergebnis.WorstCase));
    }

    [Fact]
    public void Ein_gepflegtes_Paar_steht_mit_den_Stellen_des_Wirts_und_geht_unveraendert_zurueck()
    {
        var cut = Zeige(Arbeitspreis(best: 0.6, worst: 0.7));
        var felder = cut.FindAll("input[inputmode=decimal]");
        Assert.Equal("0,6000", felder[0].GetAttribute("value"));
        Assert.Equal("0,7000", felder[1].GetAttribute("value"));

        cut.FindAll("input[inputmode=decimal]")[1].Input("0,7250");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0.6, _ergebnis!.BestCase);
        Assert.Equal(0.725, _ergebnis.WorstCase);
    }

    /// <summary>
    /// Der Prozentmodus rechnet gegen den Erwartet-Wert und rundet auf die STELLEN DES
    /// PAARS (ein Arbeitspreis in €/Nm³ verlöre auf Cent gerundet seine Aussage); ein
    /// leeres Feld bleibt leer und heißt in der Umrechnungszeile „—".
    /// </summary>
    [Fact]
    public void Der_Prozentmodus_rechnet_mit_den_Stellen_des_Paars()
    {
        var cut = Zeige(Arbeitspreis(best: 0.60));

        cut.FindAll("input[type=radio]")[1].Change(true);

        Assert.True(Blatt(cut).ProzentModus);
        var felder = cut.FindAll("input[inputmode=decimal]");
        Assert.Equal("-7,7", felder[0].GetAttribute("value"));      // (0,60 − 0,65) / 0,65
        Assert.Equal("", felder[1].GetAttribute("value") ?? "");     // bleibt leer
        Assert.All(cut.FindAll(".epos-formularraster .epos-einheit"), e => Assert.Equal("%", e.TextContent));

        cut.FindAll("input[inputmode=decimal]")[0].Input("-10");
        Assert.Contains(cut.FindAll(".epos-herleitung-text"),
                        e => e.TextContent == "ergibt: Best 0,5850 €/Nm³ · Worst —");

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(0.585, _ergebnis!.BestCase, 9);                // Preis, nicht Prozent
        Assert.Equal(0, _ergebnis.WorstCase);
    }

    // =====================================================================
    //  Ohne Erwartet-Wert (E9b-Q4, Lesart a: warnen, nicht verweigern)
    // =====================================================================

    [Fact]
    public void Ohne_Erwartet_Wert_warnt_das_Paar_sperrt_den_Prozentmodus_und_speichert_trotzdem()
    {
        var cut = Zeige(Arbeitspreis(erwartet: null));

        Assert.True(Blatt(cut).OhneErwartet);
        Assert.Equal("Erwartet: kein Wert gepflegt", cut.FindAll(".epos-herleitung-text")[0].TextContent);
        Assert.Equal("Kein Erwartet-Wert gepflegt: Günstig und Ungünstig rechnen mit ihrem Szenariowert, "
                     + "Erwartet zeigt die Datenlücke. Gespeichert wird trotzdem.",
                     cut.Find(".epos-warnbanner-text").TextContent);
        Assert.True(cut.FindAll("input[type=radio]")[1].HasAttribute("disabled"));

        cut.FindAll("input[inputmode=decimal]")[0].Input("0,5000");
        cut.Find(".epos-knopf--primaer").Click();

        Assert.Equal(0.5, _ergebnis!.BestCase);                     // nicht verweigert
    }

    /// <summary>Der Grundpreis und die Erlössätze warnen nicht — 0 ist dort keine Lücke.</summary>
    [Fact]
    public void Ohne_Warnwunsch_des_Wirts_bleibt_die_Warnung_weg()
    {
        var cut = Zeige(Arbeitspreis(erwartet: null, warnen: false));

        Assert.Equal("Erwartet: kein Wert gepflegt", cut.FindAll(".epos-herleitung-text")[0].TextContent);
        Assert.Empty(cut.FindAll(".epos-warnbanner"));
    }

    // =====================================================================
    //  Kohärenzzeilen, Hilfe, Abbrechen
    // =====================================================================

    /// <summary>Die Kohärenzzeilen des Wirts stehen im Blatt — leere fallen weg.</summary>
    [Fact]
    public void Die_Kohaerenzzeilen_des_Wirts_stehen_im_Blatt()
    {
        var cut = Zeige(Arbeitspreis(hinweise: new[] { "Staffel gilt auch im Szenario.", " ", "Zweiter Hinweis." }));

        Assert.Equal(new[] { "Staffel gilt auch im Szenario.", "Zweiter Hinweis." },
                     cut.FindAll(".epos-kohaerenz-text").Select(e => e.TextContent).ToArray());
        Assert.All(cut.FindAll(".epos-kohaerenz"), e => Assert.Contains("epos-kohaerenz--abweichend", e.ClassName));
    }

    [Fact]
    public void Der_Hilfeknopf_fuehrt_auf_die_Szenariowerte_der_Wirtschaftlichkeit()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Zeige(Arbeitspreis());
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { Szenariopaar.HILFESCHLUESSEL }, hilfe.Geoeffnet);
        Assert.Equal("Form_WirtschaftlichkeitSzenariowerte.btn_Help", Szenariopaar.HILFESCHLUESSEL);
    }

    [Fact]
    public void Abbrechen_meldet_null()
    {
        _ergebnis = new CaseEingabeErgebnis(1, 2, 3, 4, 5, true);
        var cut = Zeige(Arbeitspreis(best: 0.6));

        cut.FindAll("button.epos-knopf").First(b => b.TextContent == "Abbrechen").Click();

        Assert.Equal(1, _gemeldet);
        Assert.Null(_ergebnis);
    }

    // =====================================================================
    //  Der Assistent (KI-Sicht): drei Auskunftsfelder, die Kostenfelder abgelehnt
    // =====================================================================

    [Fact]
    public void Der_Assistent_liest_Groesse_Erwartet_und_Einheit_nur()
    {
        Zeige(Arbeitspreis());

        KiFeldzugang groesse = KiMaskenbruecke.Feldzugang(KiMaskennamen.CASE_EINGABE, "groesse");
        KiFeldzugang erwartet = KiMaskenbruecke.Feldzugang(KiMaskennamen.CASE_EINGABE, "erwartet");
        KiFeldzugang einheit = KiMaskenbruecke.Feldzugang(KiMaskennamen.CASE_EINGABE, "einheit");
        Assert.NotNull(groesse);
        Assert.NotNull(erwartet);
        Assert.NotNull(einheit);

        Assert.False(groesse.Setzbar);
        Assert.False(erwartet.Setzbar);
        Assert.False(einheit.Setzbar);
        Assert.Equal("Arbeitspreis Erdgas H", groesse.Lesen());
        Assert.Equal(0.65, Convert.ToDouble(erwartet.Lesen(), CultureInfo.InvariantCulture), 9);
        Assert.Equal("€/Nm³", einheit.Lesen());
    }

    /// <summary>
    /// Im Szenariopaar gibt es weder Nutzungsdauer noch Startjahr noch Zuschuss — der
    /// Assistent bekommt beim Setzen den GRUND (benannt abgelehnt statt still übergangen);
    /// gelesen wird 0 bzw. „nein". Best und Worst setzt er wie in der Kostenposition.
    /// </summary>
    [Fact]
    public void Der_Assistent_setzt_Best_und_Worst_und_bekommt_fuer_die_Kostenfelder_einen_Grund()
    {
        var cut = Zeige(Arbeitspreis());

        KiFeldzugang jahr = KiMaskenbruecke.Feldzugang(KiMaskennamen.CASE_EINGABE, "startjahr");
        Assert.NotNull(jahr);
        var fehler = Assert.Throws<InvalidOperationException>(() => jahr.Setzen(3));
        Assert.Equal("Im Szenariopaar führt die Maske weder Nutzungsdauer noch Startjahr noch Zuschuss — "
                     + "sie pflegt nur den Best- und den Worst-Wert.", fehler.Message);
        Assert.Equal(0, Convert.ToInt32(jahr.Lesen(), CultureInfo.InvariantCulture));

        KiFeldzugang best = KiMaskenbruecke.Feldzugang(KiMaskennamen.CASE_EINGABE, "best_case");
        Assert.NotNull(best);
        KiFeldumsetzung neu = KiFeldwandler.Wandle(best, "0,61");
        Assert.True(neu.Ok, neu.Grund);
        best.Setzen(neu.Wert);
        cut.Render();

        cut.Find(".epos-knopf--primaer").Click();
        Assert.Equal(0.61, _ergebnis!.BestCase, 9);
    }

    /// <summary>In der Kostenposition sagt die Auskunft „Kosten" in Euro.</summary>
    [Fact]
    public void In_der_Kostenposition_nennt_die_Auskunft_Kosten_in_Euro()
    {
        Render<CaseEingabeDialog>(p => p
            .Add(x => x.Betrag, 10000)
            .Add(x => x.BestCase, 8000)
            .Add(x => x.WorstCase, 12000));

        Assert.Equal("Kosten", KiMaskenbruecke.Feldzugang(KiMaskennamen.CASE_EINGABE, "groesse").Lesen());
        Assert.Equal("€", KiMaskenbruecke.Feldzugang(KiMaskennamen.CASE_EINGABE, "einheit").Lesen());
        Assert.Equal(10000.0, Convert.ToDouble(
            KiMaskenbruecke.Feldzugang(KiMaskennamen.CASE_EINGABE, "erwartet").Lesen(),
            CultureInfo.InvariantCulture));
    }

    // =====================================================================
    //  Der Parametersatz und die Regeln aller Wirte
    // =====================================================================

    /// <summary>
    /// Hausregel „Ein Parametersatz aus einer Hülle trifft nur <c>[Parameter]</c>": Ein
    /// fremder Schlüssel bräche beim ersten Zeichnen im Blazor-Verteiler, ohne Namen.
    /// </summary>
    [Fact]
    public void Jeder_Schluessel_des_Parametersatzes_trifft_einen_Parameter_des_Dialogs()
    {
        var fremd = Arbeitspreis().Keys
            .Where(k => typeof(CaseEingabeDialog).GetProperty(k)?.GetCustomAttribute<ParameterAttribute>() is null)
            .ToList();

        Assert.True(fremd.Count == 0, "Kein [Parameter] des CaseEingabeDialog: " + string.Join(", ", fremd));
        Assert.True((bool)Arbeitspreis()["Szenariopaar"]);
        Assert.Equal(Szenariopaar.HILFESCHLUESSEL, Arbeitspreis()["HilfeSchluessel"]);
    }

    /// <summary>
    /// Die Nullregel des Kerns (<see cref="SzenarioSatz.Gepflegt"/>): leer oder 0 heißt „wie
    /// Erwartet", und ein Wert, der sich nicht um mehr als 1e−9 vom Erwartet-Wert
    /// unterscheidet, ist keine Pflege — Kennzeichen und Kurztext der Knöpfe folgen ihr.
    /// </summary>
    [Fact]
    public void Kennzeichen_Wert_und_Kurztext_folgen_der_Nullregel_des_Kerns()
    {
        Assert.False(Szenariopaar.Gepflegt(null, null, 0.65));
        Assert.False(Szenariopaar.Gepflegt(0, 0, 0.65));
        Assert.False(Szenariopaar.Gepflegt(0.65, null, 0.65));
        Assert.False(Szenariopaar.Gepflegt(0.65 + 1e-12, null, 0.65));
        Assert.True(Szenariopaar.Gepflegt(null, 0.7, 0.65));
        Assert.True(Szenariopaar.Gepflegt(0.5, null, 0));           // ohne Erwartet-Wert

        Assert.Null(Szenariopaar.Wert(0));
        Assert.Equal(0.7, Szenariopaar.Wert(0.7));

        Assert.Equal("Szenariowerte Best/Worst pflegen", Szenariopaar.Kurztext(null, null, 0.65, "€/Nm³", 4));
        Assert.Equal("Szenariowerte gepflegt — Best 0,6000 €/Nm³ · Worst wie Erwartet",
                     Szenariopaar.Kurztext(0.6, null, 0.65, "€/Nm³", 4));
        Assert.Equal("Szenariowerte — Arbeitspreis Erdgas H", Szenariopaar.Titel("Arbeitspreis Erdgas H"));
    }
}

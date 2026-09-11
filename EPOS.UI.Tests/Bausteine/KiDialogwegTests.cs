using System;
using System.Collections.Generic;
using System.Linq;
using Bunit;
using EPOS.UI.Bausteine;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Bausteine;

/// <summary>
/// Die SERIELLE Sammlung der Fälle, die <c>Dienste.Navigation</c> und
/// <c>KiVerfuegbarkeit.Haken</c> tauschen (Auftrag #199).
/// </summary>
/// <remarks>
/// Beides ist prozessweiter Zustand, und xunit trennt nur INNERHALB einer Sammlung
/// — zwei verschiedene liefen nebeneinander. Dieselbe Lehre wie
/// <c>[Collection("Testdatenbank")]</c> in <c>EPOS.Kern.Tests</c> (Befund iU5‑O‑1).
/// </remarks>
[CollectionDefinition("KiDialogweg", DisableParallelization = true)]
public sealed class KiDialogwegSammlung { }

/// <summary>
/// Grundlage der Fälle: pinnt die Kultur, legt eine <see cref="TestNavigation"/> ein
/// und gibt beides beim Verwerfen zurück.
/// </summary>
public abstract class KiDialogwegBasis : EposBunitContext
{
    private readonly INavigation _navigationVorher;
    private readonly Func<bool> _verfuegbarVorher;

    protected KiDialogwegBasis()
    {
        _navigationVorher = WindowsFormsApplication1.Dienste.Navigation;
        _verfuegbarVorher = KiVerfuegbarkeit.Haken;

        Navigation = new TestNavigation();
        WindowsFormsApplication1.Dienste.Navigation = Navigation;

        // Der Assistent ist MOEGLICH, solange ein Fall nichts anderes sagt: Das ist
        // der Regelzustand beim Anwender (KI-D-Q1 - Netz, Schluessel und
        // Einwilligung entscheiden nicht ueber die SICHTBARKEIT).
        KiVerfuegbarkeit.Haken = () => true;

        Services.AddSingleton<IHilfeDienst>(new KeineHilfe());
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>Die Mitschrift der Maskenaufrufe.</summary>
    protected TestNavigation Navigation { get; }

    /// <summary>Der Assistent ist auf dieser „Installation" nicht möglich.</summary>
    protected static void AssistentAbschalten() => KiVerfuegbarkeit.Haken = () => false;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            WindowsFormsApplication1.Dienste.Navigation = _navigationVorher;
            KiVerfuegbarkeit.Haken = _verfuegbarVorher;
            KiChatKontext.AufrufMelden(null);
        }

        base.Dispose(disposing);
    }
}

/// <summary>
/// WEG 1 — der KI-Knopf im Dialogkopf (Auftrag #199, Stufe S1 des Konzepts
/// „Der Hilfe-Assistent im Dialog", 3.1).
///
/// <para>Geprüft wird, was der Baustein zusagt: Er steht neben dem Fragezeichen in
/// JEDEM Dialog, er verschwindet nur da, wo die Installation die KI abgeschaltet hat
/// oder der Dialog ihn ausdrücklich nicht will, und sein Klick öffnet den Assistenten
/// mit dem Bereich, der aus dem Hilfeschlüssel folgt.</para>
/// </summary>
[Collection("KiDialogweg")]
public class InfoKnopfAssistentTests : KiDialogwegBasis
{
    private IRenderedComponent<InfoKnopf> Zeigen(string schluessel = "Form_Heizkessel.btn_Help",
                                                 bool mitAssistent = true,
                                                 string dialogname = "",
                                                 bool aktiv = false)
        => Render<InfoKnopf>(p => p
               .Add(x => x.Schluessel, schluessel)
               .Add(x => x.MitAssistent, mitAssistent)
               .Add(x => x.Dialogname, dialogname)
               .Add(x => x.Aktiv, aktiv));

    /// <summary>
    /// Seit Auftrag #218 EIN Element "epos-hilfepille" mit zwei Feldern — kein
    /// Knopfpaar mehr. Das rechte Feld traegt die EPOS-Plan-Marke als Inline-SVG,
    /// keine Beschriftung "KI" (Anwenderentscheid 11.09.2026, Variante C + D).
    /// </summary>
    [Fact]
    public void Neben_dem_Fragezeichen_steht_der_KI_Knopf()
    {
        var cut = Zeigen();

        Assert.True(cut.Instance.AssistentSichtbar);
        Assert.Single(cut.FindAll(".epos-kiknopf"));

        // BEIDE Felder stehen in EINER Pille.
        Assert.Single(cut.FindAll(".epos-hilfepille"));
        var pille = cut.Find(".epos-hilfepille");

        // REIHENFOLGE: erst der Infoknopf, dann der Assistent.
        var knoepfe = pille.QuerySelectorAll("button");
        Assert.Equal(2, knoepfe.Length);
        Assert.Contains("epos-infoknopf", knoepfe[0].ClassName);
        Assert.Contains("epos-hilfepille__feld", knoepfe[0].ClassName);
        Assert.Contains("epos-kiknopf", knoepfe[1].ClassName);
        Assert.Contains("epos-hilfepille__feld", knoepfe[1].ClassName);

        // KEIN Text "KI" mehr — nur noch das Inline-SVG der Marke.
        Assert.Equal("", knoepfe[1].TextContent.Trim());
        Assert.NotEmpty(knoepfe[1].QuerySelectorAll("svg"));
    }

    /// <summary>Der Dialog, der ihn nicht will, setzt <c>MitAssistent="false"</c> — dann
    /// bleibt nur das linke Feld der Pille stehen.</summary>
    [Fact]
    public void Ohne_MitAssistent_steht_er_nicht()
    {
        var cut = Zeigen(mitAssistent: false);

        Assert.False(cut.Instance.AssistentSichtbar);
        Assert.Empty(cut.FindAll(".epos-kiknopf"));
        Assert.Single(cut.FindAll("button"));
        Assert.Single(cut.FindAll(".epos-hilfepille"));
    }

    /// <summary>
    /// "Assistent offen" (Parameter <c>Aktiv</c>, Auftrag #218): das rechte Feld
    /// traegt die Zustandsklasse und fuellt sich mit dem Blau der Marke.
    /// </summary>
    [Fact]
    public void Aktiv_setzt_die_Zustandsklasse_am_rechten_Feld()
    {
        var cut = Zeigen(aktiv: true);

        var assistentfeld = cut.Find(".epos-kiknopf");
        Assert.Contains("epos-hilfepille__feld--aktiv", assistentfeld.ClassName);
    }

    /// <summary>Ohne <c>Aktiv</c> (Vorgabe) traegt das Feld die Zustandsklasse nicht.</summary>
    [Fact]
    public void Ohne_Aktiv_bleibt_die_Zustandsklasse_weg()
    {
        var cut = Zeigen();

        var assistentfeld = cut.Find(".epos-kiknopf");
        Assert.DoesNotContain("epos-hilfepille__feld--aktiv", assistentfeld.ClassName);
    }

    /// <summary>Die Installation hat die KI abgeschaltet — dann gibt es den Weg nicht.</summary>
    [Fact]
    public void Ohne_moeglichen_Assistenten_steht_er_nicht()
    {
        AssistentAbschalten();
        var cut = Zeigen();

        Assert.False(cut.Instance.AssistentSichtbar);
        Assert.Empty(cut.FindAll(".epos-kiknopf"));
    }

    /// <summary>
    /// Auftrag #227: Kennt die Pille den Bildschirm (Parameter <c>Dialogname</c>),
    /// trägt das rechte Feld seinen NAMEN im Tooltip — „Simulation vom
    /// Hilfe-Assistenten erklären lassen" statt des allgemeinen Satzes.
    /// </summary>
    [Fact]
    public void Mit_Dialogname_nennt_der_Tooltip_des_rechten_Feldes_den_Bildschirm()
    {
        var cut = Zeigen(dialogname: "Simulation");

        var feld = cut.Find(".epos-kiknopf");
        Assert.Equal("Simulation vom Hilfe-Assistenten erklären lassen", feld.GetAttribute("title"));
        Assert.Equal("Simulation vom Hilfe-Assistenten erklären lassen", feld.GetAttribute("aria-label"));
    }

    /// <summary>Ohne Dialogname bleibt der allgemeine Tooltip stehen.</summary>
    [Fact]
    public void Ohne_Dialogname_bleibt_der_allgemeine_Tooltip()
    {
        var cut = Zeigen();

        var feld = cut.Find(".epos-kiknopf");
        Assert.Equal("Diesen Dialog vom Hilfe-Assistenten erklären lassen", feld.GetAttribute("title"));
        Assert.Equal("Diesen Dialog vom Hilfe-Assistenten erklären lassen", feld.GetAttribute("aria-label"));
    }

    /// <summary>Der Klick öffnet <c>Masken.KiAssistent</c> mit dem Bereich aus dem Schlüssel.</summary>
    [Fact]
    public void Der_Klick_oeffnet_den_Assistenten_mit_dem_Bereich_aus_dem_Schluessel()
    {
        var cut = Zeigen(dialogname: "Heizkessel bearbeiten");

        cut.Find(".epos-kiknopf").Click();

        Assert.Equal(new[] { Masken.KiAssistent }, Navigation.Masken);

        KiAufrufkontext? kontext = Navigation.LetzterKontext;
        Assert.NotNull(kontext);
        Assert.Equal(KiChatKontext.B_HEIZKESSEL, kontext!.Bereich);
        Assert.Equal("Form_Heizkessel.btn_Help", kontext.Hilfeschluessel);
        Assert.Equal("Heizkessel bearbeiten", kontext.Dialogname);

        // WEG 1 belegt KEINE Frage: Der Anwender hat noch keine gestellt.
        Assert.Equal("", kontext.Frage);
        Assert.Equal("", kontext.Kennung);
    }

    /// <summary>Der Aufruf setzt den Haken im Kern — damit weiss auch iOS den Bereich.</summary>
    [Fact]
    public void Der_Klick_meldet_den_Aufruf_im_Kern()
    {
        var cut = Zeigen("Form_PV.btn_Help");

        cut.Find(".epos-kiknopf").Click();

        Assert.NotNull(KiChatKontext.Aufruf);
        Assert.Equal(KiChatKontext.B_PHOTOVOLTAIK, KiChatKontext.AktuellerBereich());
    }

    /// <summary>Ein unbekannter Schlüssel öffnet trotzdem — nur eben ohne Bereich.</summary>
    [Fact]
    public void Ein_unbekannter_Schluessel_oeffnet_ohne_Bereich()
    {
        var cut = Zeigen("Gibt_Es_Nicht.btn_Help");

        cut.Find(".epos-kiknopf").Click();

        Assert.Equal(new[] { Masken.KiAssistent }, Navigation.Masken);
        Assert.Equal(KiChatKontext.BEREICH_UNBEKANNT, Navigation.LetzterKontext!.Bereich);
    }

    /// <summary>Der Klick auf das Fragezeichen bleibt der Hilfeweg — er ruft keine Navigation.</summary>
    [Fact]
    public void Der_Infoknopf_bleibt_der_Hilfeweg()
    {
        var hilfe = new TestHilfe();
        Services.AddSingleton<IHilfeDienst>(hilfe);

        var cut = Render<InfoKnopf>(p => p.Add(x => x.Schluessel, "Form_Heizkessel.btn_Help"));
        cut.Find(".epos-infoknopf").Click();

        Assert.Equal(new[] { "Form_Heizkessel.btn_Help" }, hilfe.Geoeffnet);
        Assert.Empty(Navigation.Masken);
    }
}

/// <summary>
/// WEG 2 — „erklären lassen" am Warnbanner (Auftrag #199, Konzept 3.2).
/// </summary>
[Collection("KiDialogweg")]
public class WarnbannerErklaerenTests : KiDialogwegBasis
{
    private IRenderedComponent<Warnbanner> Zeigen(string? kennung,
                                                  string text = "Die Flotte hat nichts getan.")
        => Render<Warnbanner>(p =>
        {
            p.Add(x => x.Stufe, WarnStufe.Warnung).Add(x => x.Text, text);
            if (kennung is not null) p.Add(x => x.Kennung, kennung);
            p.Add(x => x.Hilfeschluessel, "Form_SpeicherOptimierung.btn_Help")
             .Add(x => x.Dialogname, "Stromspeicher-Auslegung");
        });

    [Fact]
    public void Mit_Kennung_steht_der_Link()
    {
        var cut = Zeigen(KiMeldungskennung.FLOTTE_ARBEITSLOS);

        Assert.True(cut.Instance.ErklaerenSichtbar);
        Assert.Single(cut.FindAll(".epos-warnbanner-erklaeren"));
    }

    [Fact]
    public void Ohne_Kennung_steht_kein_Link()
    {
        var cut = Zeigen(null);

        Assert.False(cut.Instance.ErklaerenSichtbar);
        Assert.Empty(cut.FindAll(".epos-warnbanner-erklaeren"));
    }

    /// <summary>Ohne möglichen Assistenten steht er auch mit Kennung nicht.</summary>
    [Fact]
    public void Ohne_moeglichen_Assistenten_steht_kein_Link()
    {
        AssistentAbschalten();
        var cut = Zeigen(KiMeldungskennung.FLOTTE_ARBEITSLOS);

        Assert.False(cut.Instance.ErklaerenSichtbar);
        Assert.Empty(cut.FindAll(".epos-warnbanner-erklaeren"));
    }

    /// <summary>Der Link ruft denselben Weg — mit Kennung und mit vorbelegter Frage.</summary>
    [Fact]
    public void Der_Link_oeffnet_mit_Kennung_und_Frage()
    {
        var cut = Zeigen(KiMeldungskennung.FLOTTE_ARBEITSLOS);

        cut.Find(".epos-warnbanner-erklaeren").Click();

        Assert.Equal(new[] { Masken.KiAssistent }, Navigation.Masken);

        KiAufrufkontext? kontext = Navigation.LetzterKontext;
        Assert.NotNull(kontext);
        Assert.Equal(KiMeldungskennung.FLOTTE_ARBEITSLOS, kontext!.Kennung);
        Assert.Equal(KiChatKontext.B_STROMSPEICHER, kontext.Bereich);
        Assert.Equal("Stromspeicher-Auslegung", kontext.Dialogname);
        Assert.NotEqual("", kontext.Frage);

        // Die EIGENE Frage der Kennung, nicht der allgemeine Satz mit dem Bannertext.
        Assert.DoesNotContain("Die Flotte hat nichts getan.", kontext.Frage, StringComparison.Ordinal);
    }

    /// <summary>
    /// Ohne eigene Ressource gilt der allgemeine Satz — mit dem Bannertext darin.
    /// </summary>
    [Fact]
    public void Ohne_eigene_Frage_traegt_der_allgemeine_Satz_den_Bannertext()
    {
        var cut = Zeigen("GIBT_ES_NICHT", "Der Wirkungsgrad liegt über 100 %.");

        cut.Find(".epos-warnbanner-erklaeren").Click();

        Assert.Contains("Der Wirkungsgrad liegt über 100 %.",
                        Navigation.LetzterKontext!.Frage, StringComparison.Ordinal);
    }

    /// <summary>Der Selbstverfall bleibt unberührt: Ein verfallenes Banner zeigt nichts.</summary>
    [Fact]
    public void Ein_verfallenes_Banner_zeigt_auch_keinen_Link()
    {
        var cut = Render<Warnbanner>(p => p
            .Add(x => x.Text, "Kurzhinweis")
            .Add(x => x.Kennung, KiMeldungskennung.FLOTTE_ARBEITSLOS)
            .Add(x => x.Verfaellt, TimeSpan.FromSeconds(3))
            .Add(x => x.Uhr, (TimeSpan _, System.Threading.CancellationToken _) => Task.CompletedTask));

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".epos-warnbanner")));
        Assert.Empty(cut.FindAll(".epos-warnbanner-erklaeren"));
    }
}

/// <summary>
/// WEG 2 am DIAGNOSEBANNER der Speicherflotte (Auftrag #199, Konzept 3.2).
///
/// <para>Das Banner ist kein <c>Warnbanner</c> — es trägt drei Abhilfeknöpfe und eine
/// Hinweisliste. Der Erklärlink steht trotzdem darin: an der Diagnose selbst und je
/// Prüfhinweis, weil die fünf Hinweise fünf Ursachen und fünf Abhilfen haben.</para>
/// </summary>
[Collection("KiDialogweg")]
public class FlottenDiagnoseErklaerenTests : KiDialogwegBasis
{
    private static SpeicherEngine.FlottenDiagnose Arbeitslos()
        => new() { IntervalleGesamt = 35040, Arbeitslos = true, IntervalleLastUeberPeakZiel = 35040 };

    private static FlottenHinweis Hinweis(FlottenHinweisKennung kennung)
        => new() { Kennung = kennung, Stufe = FlottenHinweisStufe.Warnung, Text = "Peak-Ziel prüfen." };

    private IRenderedComponent<EPOS.UI.Seiten.Strom.FlottenDiagnosebanner> Zeigen(
        SpeicherEngine.FlottenDiagnose? diagnose = null,
        IReadOnlyList<FlottenHinweis>? hinweise = null)
        => Render<EPOS.UI.Seiten.Strom.FlottenDiagnosebanner>(p => p
               .Add(x => x.Diagnose, diagnose)
               .Add(x => x.Hinweise, hinweise ?? Array.Empty<FlottenHinweis>())
               .Add(x => x.Hilfeschluessel, "Form_SpeicherOptimierung.btn_Help")
               .Add(x => x.Dialogname, "Stromspeicher-Auslegung"));

    [Fact]
    public void Die_arbeitslose_Flotte_laesst_sich_erklaeren()
    {
        var cut = Zeigen(Arbeitslos());

        cut.Find(".epos-warnbanner-erklaeren").Click();

        Assert.Equal(new[] { Masken.KiAssistent }, Navigation.Masken);
        Assert.Equal(KiMeldungskennung.FLOTTE_ARBEITSLOS, Navigation.LetzterKontext!.Kennung);
        Assert.Equal(KiChatKontext.B_STROMSPEICHER, Navigation.LetzterKontext.Bereich);
    }

    [Fact]
    public void Jeder_Pruefhinweis_traegt_seinen_eigenen_Link()
    {
        var cut = Zeigen(hinweise: new[]
        {
            Hinweis(FlottenHinweisKennung.PeakZielUnterTagesminimum),
            Hinweis(FlottenHinweisKennung.StartSoCAufMinimum)
        });

        var links = cut.FindAll(".epos-warnbanner-erklaeren");
        Assert.Equal(2, links.Count);

        links[1].Click();
        Assert.Equal(KiMeldungskennung.FLOTTE_START_SOC_AUF_MINIMUM,
                     Navigation.LetzterKontext!.Kennung);
    }

    [Fact]
    public void Ohne_moeglichen_Assistenten_steht_kein_Link()
    {
        AssistentAbschalten();
        var cut = Zeigen(Arbeitslos(), new[] { Hinweis(FlottenHinweisKennung.StartSoCAufMinimum) });

        Assert.False(cut.Instance.ErklaerenMoeglich);
        Assert.Empty(cut.FindAll(".epos-warnbanner-erklaeren"));
    }
}

/// <summary>
/// Der WÄCHTER über die Einbaustellen: Kein <c>InfoKnopf</c> ohne Hilfeschlüssel.
/// </summary>
/// <remarks>
/// <para>Seit Auftrag #199 hängt am Schlüssel mehr als die Wiki-Seite: Der Bereich
/// des Assistenten folgt aus ihm (<c>KiChatKontext.BereichFuerHilfeschluessel</c>).
/// Ein Info-Knopf ohne Schlüssel öffnet also weder Hilfe noch einen Assistenten, der
/// weiss, wovon der Anwender spricht.</para>
/// <para>Geprüft wird das MARKUP der Bibliothek — eine bunit-Probe sieht eine fehlende
/// Einbaustelle nicht (Lehre W6‑B‑1).</para>
/// </remarks>
public class InfoknopfSchluesselWacheTests
{
    [Fact]
    public void Kein_Infoknopf_ohne_Schluessel()
    {
        string[] dateien = System.IO.Directory
            .GetFiles(Quellordner(), "*.razor", System.IO.SearchOption.AllDirectories);

        Assert.True(dateien.Length > 100, "Nur " + dateien.Length + " .razor-Dateien gefunden.");

        var ohne = new List<string>();
        var einbaustellen = 0;

        foreach (string datei in dateien)
        {
            string[] zeilen = System.IO.File.ReadAllLines(datei);
            bool imKommentar = false;

            for (int i = 0; i < zeilen.Length; i++)
            {
                string zeile = zeilen[i];

                // RAZOR-KOMMENTARE ueberspringen (@* … *@). Ohne das meldete der
                // Waechter den Klassenkopf von KiKnopf.razor, der die Kollisionsregel
                // mit "<KiKnopf /> vor den <InfoKnopf />" erklaert.
                if (imKommentar)
                {
                    int endeK = zeile.IndexOf("*@", StringComparison.Ordinal);
                    if (endeK < 0) continue;
                    imKommentar = false;
                    zeile = zeile.Substring(endeK + 2);
                }

                int beginnK = zeile.IndexOf("@*", StringComparison.Ordinal);
                if (beginnK >= 0)
                {
                    int endeK = zeile.IndexOf("*@", beginnK + 2, StringComparison.Ordinal);
                    if (endeK < 0) { imKommentar = true; zeile = zeile.Substring(0, beginnK); }
                    else zeile = zeile.Substring(0, beginnK) + zeile.Substring(endeK + 2);
                }

                // ZWEI Bauarten: das Markup <InfoKnopf …/> und der Bauplan
                // builder.OpenComponent<InfoKnopf>(…) samt AddComponentParameter.
                bool bauplan = zeile.Contains("OpenComponent<InfoKnopf>", StringComparison.Ordinal);
                int stelle = bauplan ? -1 : zeile.IndexOf("<InfoKnopf", StringComparison.Ordinal);
                if (stelle < 0 && !bauplan) continue;

                einbaustellen++;

                // Die Einbaustelle darf ueber mehrere Zeilen gehen; gelesen wird bis
                // zum schliessenden Winkel bzw. bis CloseComponent().
                string ende = bauplan ? "CloseComponent" : ">";
                string rumpf = bauplan ? zeile : zeile.Substring(stelle);
                int weiter = i;
                while (!rumpf.Contains(ende, StringComparison.Ordinal) && ++weiter < zeilen.Length)
                    rumpf += " " + zeilen[weiter].Trim();

                bool traegt = bauplan
                    ? rumpf.Contains("InfoKnopf.Schluessel", StringComparison.Ordinal)
                    : rumpf.Contains("Schluessel=", StringComparison.Ordinal);

                if (!traegt) ohne.Add(System.IO.Path.GetFileName(datei) + ":" + (i + 1));
            }
        }

        Assert.True(einbaustellen > 50, "Nur " + einbaustellen + " InfoKnopf-Einbaustellen gefunden.");
        Assert.True(ohne.Count == 0,
                    "Diese InfoKnopf-Einbaustellen tragen keinen Schluessel:"
                    + Environment.NewLine + string.Join(Environment.NewLine, ohne));
    }

    /// <summary>
    /// Die VIER Ausnahmen der Hausregel „jeder Dialog bietet den Assistenten"
    /// (Auftrag #199): der Chat selbst, seine Werkzeugliste und die zwei
    /// Lizenzmasken.
    /// </summary>
    /// <remarks>
    /// Sie stehen hier NAMENTLICH, damit eine fünfte nicht unbemerkt dazukommt: Ein
    /// „false" ist schnell gesetzt und nimmt dem Anwender einen Weg, den das Konzept
    /// ihm in jedem Dialog zusagt (Zielbild, Abschnitt 2).
    /// </remarks>
    [Theory]
    [InlineData("Dialoge/Hilfe/KiChatDialog.razor")]
    [InlineData("Dialoge/Hilfe/KiWerkzeugliste.razor")]
    [InlineData("Dialoge/Lizenz/LizenzDialog.razor")]
    [InlineData("Dialoge/Lizenz/LizenzVerwaltungDialog.razor")]
    public void Die_vier_Ausnahmen_setzen_MitAssistent_false(string datei)
    {
        string inhalt = System.IO.File.ReadAllText(System.IO.Path.Combine(Quellordner(), datei));

        Assert.Contains("MitAssistent=\"false\"", inhalt, StringComparison.Ordinal);
    }

    /// <summary>
    /// UND SONST NIRGENDS: Jedes weitere <c>MitAssistent="false"</c> ist eine
    /// Ausnahme, die niemand beschlossen hat.
    /// </summary>
    [Fact]
    public void Es_gibt_keine_fuenfte_Ausnahme()
    {
        var erlaubt = new HashSet<string>(StringComparer.Ordinal)
        {
            "KiChatDialog.razor", "KiWerkzeugliste.razor",
            "LizenzDialog.razor", "LizenzVerwaltungDialog.razor",

            // Der Baustein SELBST: Sein Klassenkopf nennt die Regel und den Ausweg.
            "InfoKnopf.razor"
        };

        var weitere = System.IO.Directory
            .GetFiles(Quellordner(), "*.razor", System.IO.SearchOption.AllDirectories)
            .Where(d => System.IO.File.ReadAllText(d)
                            .Contains("MitAssistent=\"false\"", StringComparison.Ordinal))
            .Select(System.IO.Path.GetFileName)
            .Where(n => !erlaubt.Contains(n!))
            .ToList();

        Assert.True(weitere.Count == 0,
                    "Diese Masken schalten den Assistenten ab, ohne dass es beschlossen waere:"
                    + Environment.NewLine + string.Join(Environment.NewLine, weitere));
    }

    /// <summary>Der Quellordner von <c>EPOS.UI</c> — vom Testausgabeordner aufwärts gesucht.</summary>
    private static string Quellordner()
    {
        var ordner = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null &&
               !System.IO.Directory.Exists(System.IO.Path.Combine(ordner.FullName, "EPOS.UI", "Bausteine")))
            ordner = ordner.Parent;

        Assert.True(ordner is not null, "Der Quellordner EPOS.UI ist nicht zu finden.");
        return System.IO.Path.Combine(ordner!.FullName, "EPOS.UI");
    }
}

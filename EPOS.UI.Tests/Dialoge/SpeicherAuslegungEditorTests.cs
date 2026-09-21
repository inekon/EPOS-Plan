using System.Text;
using System.Threading.Tasks;
using Bunit;
using EPOS.UI.Dialoge.Strom;
using EPOS.UI.Seiten.Strom;
using EPOS.UI.Tests.Seiten.Strom;
using EPOS.UI.Dienste;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.UI.Tests.Dialoge;

public sealed class SpeicherAuslegungEditorTests : EposBunitContext
{
    public SpeicherAuslegungEditorTests() => Services.AddSingleton<IHilfeDienst>(new KeineHilfe());

    [Fact]
    public void Ein_Profil_wird_als_unabhaengige_Kopie_geladen()
    {
        SpeicherOptimierungEingaben? gemeldet = null;
        var original = new SpeicherOptimierungEingaben { LeistungspreisEurProKwA = 77, Auslegung = new SpeicherAuslegungKonfiguration() };
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.Auslegungsprofile, new[] { new SpeicherAuslegungProfil { Name = "Industrie", Eingaben = original } })
            .Add(x => x.WertChanged, x => gemeldet = x));

        cut.FindAll("select").Single(x => x.TextContent.Contains("Industrie")).Change("0");

        Assert.Equal(77, gemeldet!.LeistungspreisEurProKwA);
        Assert.NotSame(original, gemeldet);
        Assert.NotSame(original.Auslegung, gemeldet.Auslegung);
    }

    [Fact]
    public void Fehler_des_Speicherwegs_bleibt_sichtbar()
    {
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.EinstellungenSpeichern, _ => Task.FromResult("Schreibfehler")));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Einstellungen speichern")).Click();

        Assert.Contains("Schreibfehler", cut.Markup);
    }

    [Fact]
    public void Csv_und_Preisprofil_verlangen_die_ausdrueckliche_Modelljahrzuordnung()
    {
        var eingaben = new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Lastquelle = SpeicherAuslegungQuelle.Datei,
                PvQuelle = SpeicherAuslegungQuelle.Datei,
                Preisquelle = SpeicherAuslegungQuelle.Preisprofil
            }
        };

        var cut = Render<SpeicherAuslegungEditor>(p => p.Add(x => x.Wert, eingaben));

        Assert.Contains("EPOS-Modelljahr auf die CSV-Zeitachse zuordnen", cut.Markup);
        Assert.Contains("29. Februar", cut.Markup);
        Assert.Contains("CSV-Zeitachse bleibt vollständig erhalten", cut.Markup);
    }

    [Fact]
    public void Manuelle_Kosteneingaben_markieren_die_beiden_Kostengruppen_als_vorhanden()
    {
        SpeicherOptimierungEingaben? gemeldet = null;
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.WertChanged, x => gemeldet = x));

        cut.FindAll("label").Single(x => x.TextContent.Contains("Investition Leistung"))
            .QuerySelector("input")!.Input("125");
        cut.FindAll("label").Single(x => x.TextContent.Contains("Betrieb Leistung"))
            .QuerySelector("input")!.Input("3,5");

        Assert.True(gemeldet!.Auslegung.DirekteKosten.InvestVorhanden);
        Assert.True(gemeldet.Auslegung.DirekteKosten.BetriebVorhanden);
        Assert.Equal(125, gemeldet.Auslegung.DirekteKosten.InvestEurProKw);
        Assert.Equal(3.5, gemeldet.Auslegung.DirekteKosten.BetriebEurProKwJahr);
    }

    [Fact]
    public void Eine_Ausnahme_des_Speicherwegs_bleibt_im_Dialog_sichtbar()
    {
        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, new SpeicherOptimierungEingaben())
            .Add(x => x.EinstellungenSpeichern, _ => throw new InvalidOperationException("Datei gesperrt")));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Einstellungen speichern")).Click();

        Assert.Contains("Datei gesperrt", cut.Markup);
    }

    [Fact]
    public void Ein_neu_gespeichertes_Profil_bleibt_nach_dem_Eltern_Rerender_waehlbar()
    {
        var cut = Render<StromspeicherAuslegungSeite>(p => p
            .Add(x => x.PlanerVerfuegbar, true)
            .Add(x => x.Dienste, new StromspeicherAuslegungDienste
            {
                Vorgaben = () => new SpeicherOptimierungVorgaben
                {
                    Eingaben = new SpeicherOptimierungEingaben()
                },
                ProfilSpeichern = (eingaben, name) => Task.FromResult(
                    new SpeicherOptimierungVorgaben
                    {
                        Eingaben = eingaben,
                        Auslegungsprofile = new[]
                        {
                            new SpeicherAuslegungProfil { Name = name, Eingaben = eingaben.Kopie() }
                        }
                    })
            }));

        // Der Auslegungseditor steht auf Blatt 2 der Ablaufleiste.
        Auslegungshilfe.Schritt(cut, AuslegungSchritt.Daten);

        cut.FindAll("label").Single(x => x.TextContent.Contains("Profilname"))
            .QuerySelector("input")!.Input("Neue Auslegung");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Profil speichern")).Click();

        Assert.Contains(cut.FindAll("select option"),
            x => x.TextContent.Trim() == "Neue Auslegung");
    }

    [Fact]
    public void Csv_Dialog_zeigt_die_begrenzt_zerlegte_Vorschau()
    {
        byte[] csv = Encoding.UTF8.GetBytes("Zeit;Wert\n2026-01-01 00:00;1,5\n2026-01-01 00:15;2,0\n");
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, new SpeicherImportDatei { Dateiname = "last.csv", Inhalt = csv })
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        Assert.Contains("last.csv", cut.Markup);
        Assert.Contains("2026-01-01 00:15", cut.Markup);
        Assert.False(cut.FindAll("button").Single(x => x.TextContent.Contains("Übernehmen")).HasAttribute("disabled"));
    }

    // =====================================================================
    //  „Das Kreuz steht beim Titel" (Anwenderentscheid 15.09.2026)
    // =====================================================================

    /// <summary>
    /// Der Zeitreihendialog steht ausschließlich als Inhalt der BETITELTEN Überlagerung
    /// des Auslegungseditors — die trägt Titel und ✕. Sein eigener Kopf ist deshalb
    /// ersatzlos gestrichen; den Dateinamen nennt weiterhin die Herleitungszeile.
    /// </summary>
    [Fact]
    public void Der_Zeitreihendialog_zeichnet_keinen_eigenen_Kopf()
    {
        byte[] csv = Encoding.UTF8.GetBytes("Zeit;Wert\n2026-01-01 00:00;1,5\n");
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, new SpeicherImportDatei { Dateiname = "last.csv", Inhalt = csv })
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        Assert.Empty(cut.FindAll(".epos-dialog-kopf"));
        Assert.Empty(cut.FindAll(".epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-dialog-titel"));
        // Der Dateiname bleibt sichtbar - in der Herleitungszeile.
        Assert.Contains("last.csv", cut.Markup);
    }

    /// <summary>
    /// Und in der Überlagerung selbst steht genau EIN ✕ — das der Überlagerung.
    /// Befund des Anwenders: „Doppeltes Kreuz dürfen nicht sein!"
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Zeitreihenimport_zeigt_nur_ein_Kreuz()
    {
        byte[] csv = Encoding.UTF8.GetBytes("Zeit;Wert\n2026-01-01 00:00;1,5\n");
        var eingaben = new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Lastquelle = SpeicherAuslegungQuelle.Datei
            }
        };

        var cut = Render<SpeicherAuslegungEditor>(p => p
            .Add(x => x.Wert, eingaben)
            .Add(x => x.DateiWaehlen, () => Task.FromResult(
                new SpeicherImportDatei { Dateiname = "last.csv", Inhalt = csv })));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Lastdatei")).Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    /// <summary>
    /// Dasselbe über dem eingebetteten <c>KostenprofilDialog</c>: Die Überlagerung
    /// „Preisprofil bearbeiten" trägt Titel und ✕, das Blatt zeigt beides nicht
    /// (<c>TitelText=""</c> am Tag der Einbettung).
    /// </summary>
    [Fact]
    public void Die_Ueberlagerung_Kostenprofil_zeigt_nur_ein_Kreuz()
    {
        var eingaben = new SpeicherOptimierungEingaben
        {
            Auslegung = new SpeicherAuslegungKonfiguration
            {
                Preisquelle = SpeicherAuslegungQuelle.Preisprofil
            }
        };

        var cut = Render<SpeicherAuslegungEditor>(p => p.Add(x => x.Wert, eingaben));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Preisprofil")).Click();

        Assert.Single(cut.FindAll(".epos-ueberlagerung-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt .epos-dialog-zu"));
        Assert.Empty(cut.FindAll(".epos-ueberlagerung-inhalt h1.epos-dialog-titel"));
    }

    // =====================================================================
    //  Der Hilfe-Assistent an den LESEREGELN (Welle KI-F6)
    // =====================================================================

    /// <summary>
    /// <b>Der ZEUGE der Maske „Zeitreihe einlesen" an der Maskenbrücke.</b> Gesetzt
    /// wird das Trennzeichen über seinen angezeigten Text — und die Maske liest ihre
    /// Vorschau daraufhin neu: Mit dem Komma zerfällt dieselbe Zeile in andere
    /// Spalten.
    /// </summary>
    [Fact]
    public void Die_Leseregeln_melden_sich_beim_Assistenten_an_und_ziehen_die_Vorschau_nach()
    {
        byte[] csv = Encoding.UTF8.GetBytes("Zeit;Wert\n2026-01-01 00:00;1,5\n2026-01-01 00:15;2,0\n");
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, new SpeicherImportDatei { Dateiname = "last.csv", Inhalt = csv })
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Bezug));

        Assert.True(KiMaskenbruecke.IstAngemeldet(KiMaskennamen.SPEICHER_ZEITREIHEN));

        // Die ROLLE kommt vom Wirt und bleibt lesbar; sie bestimmt die Einheiten.
        KiFeldzugang rolle = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SPEICHER_ZEITREIHEN, "rolle");
        Assert.False(rolle.Setzbar);
        Assert.Equal(nameof(SpeicherZeitreihenRolle.Bezug), rolle.Lesen());

        KiFeldzugang einheit = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SPEICHER_ZEITREIHEN, "einheit");
        Assert.Equal(3, einheit.Wahleintraege().Count);

        // Das TRENNZEICHEN über seinen Text setzen: aus einer Spalte werden zwei.
        KiFeldzugang trenner = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SPEICHER_ZEITREIHEN, "trennzeichen");
        Assert.True(trenner.Setzbar);

        KiFeldumsetzung komma = KiFeldwandler.Wandle(trenner, "Komma");
        Assert.True(komma.Ok, komma.Grund);
        trenner.Setzen(komma.Wert);
        cut.Render();

        Assert.Equal(1, Convert.ToInt32(trenner.Lesen(),
                                        System.Globalization.CultureInfo.InvariantCulture));

        // Mit dem Komma zerfällt „2026-01-01 00:00;1,5" anders — die Vorschau ist
        // neu gelesen und zeigt die ungeteilte Spalte.
        Assert.Contains("2026-01-01 00:00;1", cut.Markup);
    }

    /// <summary>
    /// <b>Die ZEITANGABE zieht drei Spaltennummern gleich</b> — genau wie der Griff
    /// in die Klappliste. Ein Setzer, der nur den Schalter legte, ließe die Maske mit
    /// widersprüchlichen Spalten stehen.
    /// </summary>
    [Fact]
    public void Die_Zeitangabe_stellt_die_drei_Spalten_mit_um()
    {
        byte[] csv = Encoding.UTF8.GetBytes("Datum;Zeit;Wert\n2026-01-01;00:00;1,5\n");
        var cut = Render<SpeicherZeitreihenDialog>(p => p
            .Add(x => x.Datei, new SpeicherImportDatei { Dateiname = "last.csv", Inhalt = csv })
            .Add(x => x.Rolle, SpeicherZeitreihenRolle.Last));

        KiFeldzugang art = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SPEICHER_ZEITREIHEN, "zeitangabe");
        KiFeldumsetzung getrennt = KiFeldwandler.Wandle(art, "Getrennte");
        Assert.True(getrennt.Ok, getrennt.Grund);
        art.Setzen(getrennt.Wert);
        cut.Render();

        KiFeldzugang stempel = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SPEICHER_ZEITREIHEN, "zeitstempelspalte");
        KiFeldzugang datum = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SPEICHER_ZEITREIHEN, "datumsspalte");
        KiFeldzugang uhrzeit = KiMaskenbruecke.Feldzugang(
            KiMaskennamen.SPEICHER_ZEITREIHEN, "uhrzeitspalte");

        Assert.Equal(-1, Convert.ToInt32(stempel.Lesen(),
                                         System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(0, Convert.ToInt32(datum.Lesen(),
                                        System.Globalization.CultureInfo.InvariantCulture));
        Assert.Equal(1, Convert.ToInt32(uhrzeit.Lesen(),
                                        System.Globalization.CultureInfo.InvariantCulture));
    }
}
